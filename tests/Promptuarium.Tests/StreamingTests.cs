using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Promptuarium;

namespace PromptuariumTests
{
    /// <summary>
    /// Tests for loading and saving trees through non-seekable streams (e.g. network streams)
    /// </summary>
    [TestClass]
    public class StreamingTests
    {
        [TestMethod]
        public async Task LoadFromNonSeekableStream()
        {
            var tree = CreateTree();
            using var memoryStream = new MemoryStream();
            await tree.SaveAsync(memoryStream).ConfigureAwait(false);

            using var stream = new TrickleStream(memoryStream.ToArray());
            var tree2 = await Element.LoadAsync(stream).ConfigureAwait(false);

            Assert.IsTrue(tree.StructurallyEquals(tree2));
        }

        [TestMethod]
        public async Task SaveToNonSeekableStream()
        {
            var tree = CreateTree();
            using var memoryStream = new MemoryStream();
            await tree.SaveAsync(memoryStream).ConfigureAwait(false);

            using var stream = new TrickleStream(Array.Empty<byte>());
            await tree.SaveAsync(stream).ConfigureAwait(false);

            CollectionAssert.AreEqual(memoryStream.ToArray(), stream.Written.ToArray());
        }

        [TestMethod]
        public async Task ConcatenatedTrees()
        {
            var tree1 = CreateTree();
            var tree2 = new Element(Data.FromUtf8String("Second"), Data.FromInt(2),
                new Element(Data.FromUtf8String("Child"), null));

            using var memoryStream = new MemoryStream();
            await tree1.SaveAsync(memoryStream).ConfigureAwait(false);
            await tree2.SaveAsync(memoryStream).ConfigureAwait(false);
            memoryStream.WriteByte(0x42);

            using var stream = new TrickleStream(memoryStream.ToArray());
            var loaded1 = await Element.LoadAsync(stream).ConfigureAwait(false);
            var loaded2 = await Element.LoadAsync(stream).ConfigureAwait(false);

            Assert.IsTrue(tree1.StructurallyEquals(loaded1));
            Assert.IsTrue(tree2.StructurallyEquals(loaded2));

            // The reader must not consume anything beyond the terminator of the second tree
            Assert.AreEqual(0x42, stream.ReadByte());
        }

        [TestMethod]
        public async Task TruncatedStream()
        {
            var tree = CreateTree();
            using var memoryStream = new MemoryStream();
            await tree.SaveAsync(memoryStream).ConfigureAwait(false);

            byte[] data = memoryStream.ToArray();
            using var stream = new TrickleStream(data.AsSpan(0, data.Length / 2).ToArray());

            await Assert.ThrowsExceptionAsync<PromptuariumException>(() => Element.LoadAsync(stream)).ConfigureAwait(false);
        }

        private static Element CreateTree()
        {
            var random = new Random(1234);
            byte[] longData = new byte[3 * 512 * 1024 + 1000];
            random.NextBytes(longData);

            return new Element(Data.FromUtf8String("Root"), Data.FromGuid(Guid.NewGuid()),
                new Element(longData, null,
                    new Element(null, null,
                        new Element(Data.FromVarInt(-12345), null))),
                new Element(null, Data.FromUtf8String("MetaData only")),
                new Element(Data.FromDouble(Math.PI), Data.FromAsciiString("pi")));
        }

        /// <summary>
        /// Non-seekable stream that returns at most one byte per read, like a slow network connection
        /// </summary>
        private sealed class TrickleStream : Stream
        {
            private readonly byte[] source;
            private int position;

            public TrickleStream(byte[] source)
            {
                this.source = source;
            }

            public MemoryStream Written { get; } = new MemoryStream();

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (count == 0 || position >= source.Length)
                {
                    return 0;
                }

                buffer[offset] = source[position++];
                return 1;
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return Task.FromResult(Read(buffer, offset, count));
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                Written.Write(buffer, offset, count);
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Written.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
