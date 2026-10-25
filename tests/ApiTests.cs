using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Promptuarium;

namespace PromptuariumTests
{
    /// <summary>
    /// Tests for the tree operations, load options, events and buffering
    /// </summary>
    [TestClass]
    public class ApiTests
    {
        #region Tree operations

        [TestMethod]
        public void ChildrenCanNotBeModifiedDirectly()
        {
            var tree = Node("R", Node("A"));

            Assert.IsNotInstanceOfType(tree.Children, typeof(List<Element>));
            Assert.IsTrue(((ICollection<Element>)tree.Children).IsReadOnly);
        }

        [TestMethod]
        public void DetachReturnsTheDetachedNode()
        {
            var child = Node("A");
            var tree = Node("R", child, Node("B"));

            Assert.AreSame(child, tree.Detach(child));
            Assert.IsNull(child.Parent);
            Assert.AreEqual(1, tree.Children.Count);
        }

        [TestMethod]
        public void DetachRefusesNodeOfAnotherParent()
        {
            var child = Node("A");
            var tree = Node("R", Node("X", child));

            Assert.ThrowsException<PromptuariumException>(() => tree.Detach(child));
            Assert.IsNotNull(child.Parent);
        }

        [TestMethod]
        public void RemoveRefusesNodeOfAnotherParent()
        {
            var child = Node("A");
            var tree = Node("R", Node("X", child));

            Assert.ThrowsException<PromptuariumException>(() => tree.Remove(child));
            Assert.IsNotNull(child.Parent);
        }

        [TestMethod]
        public void DetachOfRootDoesNothing()
        {
            var tree = Node("R");

            Assert.AreSame(tree, tree.Detach());
            Assert.IsNull(tree.Parent);
        }

        [TestMethod]
        public void InsertAtPosition()
        {
            var a = Node("A");
            var b = Node("B");
            var c = Node("C");
            var tree = Node("R", a, c);

            tree.Insert(1, b);
            tree.Insert(3, Node("D"));
            tree.Insert(0, Node("0"));

            CollectionAssert.AreEqual(new[] { "0", "A", "B", "C", "D" }, tree.Children.Select(child => child.Data?.AsAsciiString()).ToArray());
            Assert.AreSame(tree, b.Parent);
        }

        [TestMethod]
        public void InsertChecks()
        {
            var child = Node("A");
            var tree = Node("R", child);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => tree.Insert(2, Node("X")));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => tree.Insert(-1, Node("X")));
            Assert.ThrowsException<PromptuariumException>(() => tree.Insert(0, child));
            Assert.ThrowsException<PromptuariumException>(() => child.Insert(0, tree));

            // A node of another parent is moved
            var other = Node("O");
            other.Insert(0, child);
            Assert.AreEqual(0, tree.Children.Count);
            Assert.AreSame(other, child.Parent);
        }

        [TestMethod]
        public void StatisticsIgnoreEmptyContents()
        {
            var tree = new Element(new MemoryStream(), new MemoryStream(), Node("A"));

            Statistics stats = tree.GetStatistics();

            Assert.AreEqual(1, stats.NodesWithData);
            Assert.AreEqual(1, stats.NodesWithoutDataAndMetaData);
            Assert.AreEqual(1, stats.ShortestData);
        }

        [TestMethod]
        public void StructurallyEqualsComparesStructureAndContent()
        {
            Element Tree() => Node("R", Node("A", Node("A1")), new Element(null, Data.FromInt(7)), new Element());

            Assert.IsTrue(Tree().StructurallyEquals(Tree()));
            Assert.IsFalse(Tree().StructurallyEquals(null));

            // Different content, metadata, number and order of children
            Assert.IsFalse(Tree().StructurallyEquals(Node("X", Node("A", Node("A1")), new Element(null, Data.FromInt(7)), new Element())));
            Assert.IsFalse(Tree().StructurallyEquals(Node("R", Node("A", Node("A1")), new Element(null, Data.FromInt(8)), new Element())));
            Assert.IsFalse(Tree().StructurallyEquals(Node("R", Node("A", Node("A1")), new Element(null, Data.FromInt(7)))));
            Assert.IsFalse(Tree().StructurallyEquals(Node("R", new Element(null, Data.FromInt(7)), Node("A", Node("A1")), new Element())));
            Assert.IsFalse(Tree().StructurallyEquals(Node("R", Node("A", Node("A2")), new Element(null, Data.FromInt(7)), new Element())));

            // Absent and empty contents are equal, the parent is not compared
            Assert.IsTrue(new Element(null, null).StructurallyEquals(new Element(new MemoryStream(), new MemoryStream())));
            Assert.IsTrue(Tree()[0].StructurallyEquals(Node("A", Node("A1"))));
        }

        [TestMethod]
        public void StructurallyEqualsComparesLongContents()
        {
            byte[] bytes = new byte[200_000];
            new Random(3).NextBytes(bytes);
            byte[] other = (byte[])bytes.Clone();
            other[199_999] ^= 1;

            Assert.IsTrue(new Element(Data.FromByteArray(bytes), null).StructurallyEquals(new Element(Data.FromByteArray(bytes), null)));
            Assert.IsFalse(new Element(Data.FromByteArray(bytes), null).StructurallyEquals(new Element(Data.FromByteArray(other), null)));
        }

        #endregion

        #region No side effects

        [TestMethod]
        public void ReadingDoesNotChangeThePositions()
        {
            var tree = new Element(Data.FromAsciiString("Hello"), Data.FromInt(42), Node("A"));
            tree.Data!.Position = 3;
            tree.MetaData!.Position = 1;

            _ = tree.ToString();
            _ = tree.TreeToString();
            _ = new Element(tree);
            _ = tree.StructurallyEquals(new Element(tree));
            _ = tree.Data.AsAsciiString();
            _ = tree.MetaData.AsInt();
            _ = tree.Data.AsByteArray();

            Assert.AreEqual(3, tree.Data.Position);
            Assert.AreEqual(1, tree.MetaData.Position);
        }

        [TestMethod]
        public async Task AsElementDoesNotChangeThePosition()
        {
            using Stream content = await Data.FromElementAsync(Node("R")).ConfigureAwait(false);
            content.Position = 1;

            var tree = await content.AsElementAsync().ConfigureAwait(false);

            Assert.AreEqual("R", tree.Data?.AsAsciiString());
            Assert.AreEqual(1, content.Position);
        }

        #endregion

        #region Saving and loading

        [TestMethod]
        public async Task SavingKeepsThePositionOfTheContent()
        {
            var tree = Node("Hello");
            tree.Data!.Position = 2;

            await SaveAsync(tree).ConfigureAwait(false);

            Assert.AreEqual(2, tree.Data.Position);
        }

        [TestMethod]
        public async Task ManySmallChunksAreWrittenAtOnce()
        {
            var tree = new Element(Data.FromAsciiString("R"), null, Enumerable.Range(0, 1000).Select(i => new Element(Data.FromInt(i), null)));

            using var stream = new CountingStream();
            await tree.SaveAsync(stream).ConfigureAwait(false);

            Assert.AreEqual(1, stream.Writes);
            Assert.AreEqual(3 + 1000 * 5, stream.Length);
        }

        [TestMethod]
        public async Task LoadingFromSeekableStreamStopsAfterTheTerminator()
        {
            using var stream = new MemoryStream();
            await Node("A").SaveAsync(stream).ConfigureAwait(false);
            await Node("B").SaveAsync(stream).ConfigureAwait(false);
            stream.Position = 0;

            var first = await Element.LoadAsync(stream).ConfigureAwait(false);
            Assert.AreEqual(3, stream.Position);

            var second = await Element.LoadAsync(stream).ConfigureAwait(false);
            Assert.AreEqual(6, stream.Position);

            Assert.AreEqual("A", first.Data?.AsAsciiString());
            Assert.AreEqual("B", second.Data?.AsAsciiString());
        }

        [TestMethod]
        public async Task LoadEventsAreFiredOncePerNode()
        {
            // 37 bytes of data and 20 bytes of metadata: 2 chunks each
            var tree = new Element(new MemoryStream(new byte[37]), new MemoryStream(new byte[20]),
                new Element(Data.FromAsciiString("A"), null));

            var events = new List<string>();
            var options = new LoadOptions();
            options.OnDataLoading += (sender, _) => events.Add($"data loading {((Element)sender!).Data!.Length}");
            options.OnDataLoaded += (sender, _) => events.Add($"data loaded {((Element)sender!).Data!.Length}");
            options.OnMetaDataLoading += (sender, _) => events.Add($"metadata loading {((Element)sender!).MetaData!.Length}");
            options.OnMetaDataLoaded += (sender, _) => events.Add($"metadata loaded {((Element)sender!).MetaData!.Length}");

            using var stream = new MemoryStream(await SaveAsync(tree).ConfigureAwait(false));
            await Element.LoadAsync(stream, options, CancellationToken.None).ConfigureAwait(false);

            CollectionAssert.AreEqual(new[]
            {
                "data loading 0", "metadata loading 0", "data loaded 37", "metadata loaded 20",
                "data loading 0", "data loaded 1"
            }, events);
        }

        [TestMethod]
        public async Task MaxBytes()
        {
            byte[] bytes = await SaveAsync(Node("R", Node("A"), Node("B"))).ConfigureAwait(false);

            await LoadAsync(bytes, new LoadOptions { MaxBytes = bytes.Length }).ConfigureAwait(false);
            await Assert.ThrowsExceptionAsync<PromptuariumException>(() => LoadAsync(bytes, new LoadOptions { MaxBytes = bytes.Length - 1 })).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MaxNodes()
        {
            byte[] bytes = await SaveAsync(Node("R", Node("A"), Node("B"))).ConfigureAwait(false);

            await LoadAsync(bytes, new LoadOptions { MaxNodes = 3 }).ConfigureAwait(false);
            await Assert.ThrowsExceptionAsync<PromptuariumException>(() => LoadAsync(bytes, new LoadOptions { MaxNodes = 2 })).ConfigureAwait(false);
        }

        #endregion

        #region Data conversions

        [TestMethod]
        public void FromByteArrayCopiesTheArray()
        {
            byte[] bytes = { 1, 2, 3 };
            Stream content = Data.FromByteArray(bytes);

            bytes[0] = 42;

            Assert.AreEqual(1, content.AsByte());
        }

        [TestMethod]
        public void AsByteOfEmptyContentIsZero()
        {
            Assert.AreEqual(0, new MemoryStream().AsByte());
        }

        [TestMethod]
        public void VarIntLongerThanEightBytes()
        {
            var content = new MemoryStream(new byte[9]);

            Assert.ThrowsException<PromptuariumException>(() => content.AsVarInt());
            Assert.ThrowsException<PromptuariumException>(() => content.AsVarUInt());
        }

        #endregion

        #region Private methods

        private static Element Node(string data, params Element[] children)
        {
            return new Element(Data.FromAsciiString(data), null, children);
        }

        private static async Task<byte[]> SaveAsync(Element tree)
        {
            using var stream = new MemoryStream();
            await tree.SaveAsync(stream).ConfigureAwait(false);
            return stream.ToArray();
        }

        private static async Task<Element> LoadAsync(byte[] bytes, LoadOptions options)
        {
            using var stream = new MemoryStream(bytes);
            return await Element.LoadAsync(stream, options, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Memory stream counting the write calls (the asynchronous ones, which call the synchronous ones internally)
        /// </summary>
        private sealed class CountingStream : MemoryStream
        {
            public int Writes { get; private set; }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                Writes++;
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                Writes++;
                return base.WriteAsync(buffer, cancellationToken);
            }

            public override void WriteByte(byte value)
            {
                Writes++;
                base.WriteByte(value);
            }
        }

        #endregion
    }
}
