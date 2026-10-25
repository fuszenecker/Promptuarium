using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Promptuarium;

namespace PromptuariumTests
{
    /// <summary>
    /// Tests for the contents as ReadOnlyMemory, and for the byte array functions
    /// </summary>
    [TestClass]
    public class MemoryContentTests
    {
        [TestMethod]
        public void NullAndEmptyAreTheSame()
        {
            var node = new Element(null, null);

            Assert.IsTrue(node.Data.IsEmpty);
            Assert.IsTrue(node.MetaData.IsEmpty);
            Assert.IsTrue(node.StructurallyEquals(new Element()));
            Assert.IsTrue(node.StructurallyEquals(new Element(Array.Empty<byte>(), ReadOnlyMemory<byte>.Empty)));
        }

        [TestMethod]
        public void ByteArraysCanBeAssigned()
        {
            byte[] bytes = { 1, 2, 3 };
            var node = new Element { Data = bytes };

            // Assigning an array does not copy it; Data.FromByteArray does
            bytes[0] = 9;
            Assert.AreEqual(9, node.Data.Span[0]);

            node.Data = Data.FromByteArray(bytes);
            bytes[0] = 1;
            Assert.AreEqual(9, node.Data.Span[0]);
        }

        [TestMethod]
        public void SlicesAsContents()
        {
            byte[] buffer = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
            var tree = new Element(buffer.AsMemory(10, 20), buffer.AsMemory(50, 3));

            var loaded = Element.FromBytes(tree.ToBytes());

            CollectionAssert.AreEqual(buffer[10..30], loaded.Data.ToArray());
            CollectionAssert.AreEqual(buffer[50..53], loaded.MetaData.ToArray());
        }

        [TestMethod]
        public async Task ToBytesIsTheSameAsSaveAsync()
        {
            Element tree = SampleTree();

            using var stream = new MemoryStream();
            await tree.SaveAsync(stream).ConfigureAwait(false);

            CollectionAssert.AreEqual(stream.ToArray(), tree.ToBytes());
            Assert.IsTrue(tree.StructurallyEquals(Element.FromBytes(tree.ToBytes())));
        }

        [TestMethod]
        public async Task LargeContentsAreWrittenCorrectly()
        {
            // Payloads larger than the write buffer are written directly from the content
            byte[] large = Enumerable.Range(0, 3_000_000).Select(i => (byte)(i * 31)).ToArray();
            var tree = new Element(Data.FromAsciiString("R"), null,
                new Element(large, Data.FromInt(1)),
                new Element(Data.FromAsciiString("after"), large.AsMemory(1000, 70_000)));

            using var stream = new MemoryStream();
            await tree.SaveAsync(stream).ConfigureAwait(false);
            stream.Position = 0;

            Assert.IsTrue(tree.StructurallyEquals(await Element.LoadAsync(stream).ConfigureAwait(false)));
        }

        [TestMethod]
        public void FromBytesTellsWhereTheTreeEnds()
        {
            byte[] first = new Element(Data.FromAsciiString("A"), null).ToBytes();
            byte[] second = new Element(Data.FromAsciiString("B"), null).ToBytes();
            byte[] both = first.Concat(second).ToArray();

            Element a = Element.FromBytes(both, null, out int consumed);
            Element b = Element.FromBytes(both.AsMemory(consumed));

            Assert.AreEqual(first.Length, consumed);
            Assert.AreEqual("A", a.Data.AsAsciiString());
            Assert.AreEqual("B", b.Data.AsAsciiString());
        }

        [TestMethod]
        public void FromBytesWithLimits()
        {
            byte[] bytes = SampleTree().ToBytes();

            Element.FromBytes(bytes, new LoadOptions { MaxBytes = bytes.Length });
            Assert.ThrowsException<PromptuariumException>(() => Element.FromBytes(bytes, new LoadOptions { MaxBytes = bytes.Length - 1 }));
            Assert.ThrowsException<PromptuariumException>(() => Element.FromBytes(bytes, new LoadOptions { MaxNodes = 2 }));
        }

        [TestMethod]
        public void FromBytesOfInvalidStreams()
        {
            Assert.ThrowsException<PromptuariumException>(() => Element.FromBytes(new byte[] { 0x42, 0x61 }));
            Assert.ThrowsException<PromptuariumException>(() => Element.FromBytes(new byte[] { 0x81, 0x52, 0x00 }));
            Assert.IsTrue(Element.FromBytes(ReadOnlyMemory<byte>.Empty).StructurallyEquals(new Element()));
        }

        [TestMethod]
        public void ConversionsAreLittleEndian()
        {
            CollectionAssert.AreEqual(new byte[] { 0x78, 0x56, 0x34, 0x12 }, Data.FromInt(0x12345678).ToArray());
            CollectionAssert.AreEqual(new byte[] { 0xFE, 0xFF }, Data.FromShort(-2).ToArray());
            CollectionAssert.AreEqual(new byte[] { 0xE9, 0x00 }, Data.FromChar('é').ToArray());
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0xC0, 0x3F }, Data.FromFloat(1.5f).ToArray());
            CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0, 0, 0, 0xF8, 0x3F }, Data.FromDouble(1.5).ToArray());
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x40, 0xE4, 0x47, 0x02, 0x22, 0xC1, 0x08 }, Data.FromDateTime(new DateTime(2000, 1, 1)).ToArray());
            CollectionAssert.AreEqual(
                new byte[] { 0x00, 0x20, 0x19, 0xDD, 0x66, 0x22, 0xC1, 0x08, 0x00, 0xD0, 0x88, 0xC3, 0x10, 0x00, 0x00, 0x00 },
                Data.FromDateTimeOffset(new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.FromHours(2))).ToArray());
            CollectionAssert.AreEqual(
                new byte[] { 0x33, 0x22, 0x11, 0x00, 0x55, 0x44, 0x77, 0x66, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF },
                Data.FromGuid(Guid.Parse("00112233-4455-6677-8899-aabbccddeeff")).ToArray());
        }

        [TestMethod]
        public void RoundTripsOfTheConversions()
        {
            var now = new DateTimeOffset(2026, 10, 25, 14, 30, 0, TimeSpan.FromHours(1));
            var guid = Guid.NewGuid();

            Assert.IsTrue(Data.FromBool(true).AsBool());
            Assert.IsFalse(Data.FromBool(false).AsBool());
            Assert.AreEqual((byte)200, Data.FromByte(200).AsByte());
            Assert.AreEqual('ő', Data.FromChar('ő').AsChar());
            Assert.AreEqual((short)-12345, Data.FromShort(-12345).AsShort());
            Assert.AreEqual((ushort)54321, Data.FromUShort(54321).AsUShort());
            Assert.AreEqual(-123456789, Data.FromInt(-123456789).AsInt());
            Assert.AreEqual(3_000_000_000u, Data.FromUInt(3_000_000_000u).AsUInt());
            Assert.AreEqual(long.MinValue, Data.FromLong(long.MinValue).AsLong());
            Assert.AreEqual(ulong.MaxValue, Data.FromULong(ulong.MaxValue).AsULong());
            Assert.AreEqual(3.25f, Data.FromFloat(3.25f).AsFloat());
            Assert.AreEqual(Math.PI, Data.FromDouble(Math.PI).AsDouble());
            Assert.AreEqual(-1.978m, Data.FromDecimal(-1.978m).AsDecimal());
            Assert.AreEqual(guid, Data.FromGuid(guid).AsGuid());
            Assert.AreEqual(now.DateTime, Data.FromDateTime(now.DateTime).AsDateTime());
            Assert.AreEqual(now, Data.FromDateTimeOffset(now).AsDateTimeOffset());
            Assert.AreEqual(TimeSpan.FromMinutes(90), Data.FromTimeSpan(TimeSpan.FromMinutes(90)).AsTimeSpan());
            Assert.AreEqual("árvíztűrő", Data.FromUtf8String("árvíztűrő").AsUtf8String());
            Assert.AreEqual("árvíztűrő", Data.FromUtf16String("árvíztűrő").AsUtf16String());
            Assert.AreEqual("árvíztűrő 🌳", Data.FromUtf32String("árvíztűrő 🌳").AsUtf32String());
            Assert.AreEqual("plain", Data.FromAsciiString("plain").AsAsciiString());
            CollectionAssert.AreEqual(new byte[] { 1, 2 }, Data.FromBytes(new byte[] { 1, 2 }).AsByteArray());
        }

        [TestMethod]
        public void ShortContentsArePaddedWithZeros()
        {
            ReadOnlyMemory<byte> twoBytes = new byte[] { 0x01, 0x02 };

            Assert.AreEqual(0x0201, twoBytes.AsInt());
            Assert.AreEqual(0x0201L, twoBytes.AsLong());
            Assert.AreEqual(0, ReadOnlyMemory<byte>.Empty.AsDouble());
        }

        [TestMethod]
        public void NestedTrees()
        {
            Element inner = SampleTree();
            var outer = new Element(Data.FromElement(inner), Data.FromAsciiString("nested"));

            Element loaded = Element.FromBytes(outer.ToBytes());

            Assert.IsTrue(inner.StructurallyEquals(loaded.Data.AsElement()));
        }

        private static Element SampleTree()
        {
            return new Element(Data.FromUtf8String("Root"), Data.FromGuid(Guid.NewGuid()),
                new Element(new byte[600_000], null,
                    new Element(null, null,
                        new Element(Data.FromVarInt(-12345), null))),
                new Element(null, Data.FromUtf8String("MetaData only")),
                new Element(),
                new Element(Data.FromDouble(Math.PI), Data.FromAsciiString("pi")));
        }
    }
}
