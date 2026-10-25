using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Promptuarium;

namespace PromptuariumTests
{
    /// <summary>
    /// Tests for the binary format, edge cases and big or malformed trees
    /// </summary>
    [TestClass]
    public class RobustnessTests
    {
        private const int Depth = 50_000;

        #region Binary format

        [TestMethod]
        public async Task WorkedExampleOfTheSpecification()
        {
            // See docs/BinaryFormat.md
            var tree = Node("R",
                Node("X",
                    new Element(null, null,
                        Node("d"))),
                new Element(Data.FromAsciiString("Y"), Data.FromAsciiString("m")));

            CollectionAssert.AreEqual(
                new byte[] { 0x41, 0x52, 0x41, 0x58, 0x40, 0x41, 0x64, 0x80, 0x81, 0x59, 0x21, 0x6D, 0x00 },
                await SaveAsync(tree).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task NodeWithOnlyEmptyChildren()
        {
            var tree = Node("R",
                Node("X",
                    new Element(null, null, new Element())),
                Node("Y"));

            // 40: placeholder of the node without content, 60: the empty node, 80 81: two levels up
            byte[] bytes = await SaveAsync(tree).ConfigureAwait(false);
            CollectionAssert.AreEqual(new byte[] { 0x41, 0x52, 0x41, 0x58, 0x40, 0x60, 0x80, 0x81, 0x59, 0x00 }, bytes);

            var loaded = await LoadAsync(bytes).ConfigureAwait(false);
            Assert.IsTrue(tree.StructurallyEquals(loaded));
        }

        [TestMethod]
        public async Task EmptyNodesAreKept()
        {
            var tree = Node("P", new Element(), Node("M"), new Element(null, null, new Element(), new Element()));

            // 60 (Down, empty), C1 4D (New, "M"), C0 (New, placeholder), 60 E0 (Down and New, empty)
            byte[] bytes = await SaveAsync(tree).ConfigureAwait(false);
            CollectionAssert.AreEqual(new byte[] { 0x41, 0x50, 0x60, 0xC1, 0x4D, 0xC0, 0x60, 0xE0, 0x00 }, bytes);

            var loaded = await LoadAsync(bytes).ConfigureAwait(false);
            Assert.IsTrue(tree.StructurallyEquals(loaded));
            Assert.AreEqual(3, loaded.Children.Count);
            Assert.AreEqual(2, loaded[2].Children.Count);
        }

        [TestMethod]
        public async Task EmptyNodeAfterClimbingUp()
        {
            var tree = Node("R", Node("A", Node("B")), new Element());

            // A0: an empty node reached by Up (kept)
            byte[] bytes = await SaveAsync(tree).ConfigureAwait(false);
            CollectionAssert.AreEqual(new byte[] { 0x41, 0x52, 0x41, 0x41, 0x41, 0x42, 0xA0, 0x00 }, bytes);

            Assert.IsTrue(tree.StructurallyEquals(await LoadAsync(bytes).ConfigureAwait(false)));
        }

        [TestMethod]
        public async Task ZeroLengthContentIsAnEmptyNode()
        {
            var tree = Node("R", new Element(new MemoryStream(), new MemoryStream()), Node("A"));

            byte[] bytes = await SaveAsync(tree).ConfigureAwait(false);
            CollectionAssert.AreEqual(new byte[] { 0x41, 0x52, 0x60, 0xC1, 0x41, 0x00 }, bytes);

            var loaded = await LoadAsync(bytes).ConfigureAwait(false);
            Assert.IsTrue(tree.StructurallyEquals(loaded));
            Assert.AreEqual(2, loaded.Children.Count);
        }

        [TestMethod]
        public async Task EmptyNodeAfterUpMarkers()
        {
            var tree = Node("R", Node("A", Node("B", Node("C"))), new Element());

            // 80: Up marker (one extra level), A0: the empty node reached by Up
            byte[] bytes = await SaveAsync(tree).ConfigureAwait(false);
            CollectionAssert.AreEqual(new byte[] { 0x41, 0x52, 0x41, 0x41, 0x41, 0x42, 0x41, 0x43, 0x80, 0xA0, 0x00 }, bytes);

            var loaded = await LoadAsync(bytes).ConfigureAwait(false);
            Assert.IsTrue(tree.StructurallyEquals(loaded));
            Assert.AreEqual(2, loaded.Children.Count);
        }

        [TestMethod]
        public async Task EmptyNodeCanReceiveContent()
        {
            // 60 creates an empty node, the Append chunk 01 61 gives it data: it is an ordinary node then
            var loaded = await LoadAsync(new byte[] { 0x41, 0x52, 0x60, 0x01, 0x61, 0x00 }).ConfigureAwait(false);

            Assert.IsTrue(Node("R", Node("a")).StructurallyEquals(loaded));
        }

        [TestMethod]
        public async Task ManyEmptyNodesAreKept()
        {
            const int count = 10_000;
            byte[] bytes = new byte[] { 0x41, 0x52, 0x60 }
                .Concat(Enumerable.Repeat((byte)0xE0, count - 1))
                .Append((byte)0x00)
                .ToArray();

            var loaded = await LoadAsync(bytes).ConfigureAwait(false);

            Assert.AreEqual(count, loaded.Children.Count);
            Assert.IsTrue(loaded.Children.All(child => child.Data == null && child.MetaData == null && child.Children.Count == 0));
            CollectionAssert.AreEqual(bytes, await SaveAsync(loaded).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task OldPlaceholdersAreStillRemoved()
        {
            // Streams of older encoders: the empty Data type chunks (40, C0, 80) only create placeholders
            var loaded = await LoadAsync(new byte[] { 0x41, 0x52, 0x40, 0xC0, 0xC1, 0x41, 0x00 }).ConfigureAwait(false);

            Assert.IsTrue(Node("R", Node("A")).StructurallyEquals(loaded));
        }

        [TestMethod]
        public async Task ChunkSizes()
        {
            int[] lengths = { 1, 15, 16, 17, 31, 32, 511, 512, 513, 524_287, 524_288, 524_289, 1_048_576, 1_500_000 };
            var random = new Random(7);

            foreach (int length in lengths)
            {
                byte[] data = new byte[length];
                byte[] metaData = new byte[length / 3 + 1];
                random.NextBytes(data);
                random.NextBytes(metaData);

                var loaded = await LoadAsync(await SaveAsync(new Element(new MemoryStream(data), new MemoryStream(metaData))).ConfigureAwait(false)).ConfigureAwait(false);

                CollectionAssert.AreEqual(data, loaded.Data?.AsByteArray(), $"Length: {length}");
                CollectionAssert.AreEqual(metaData, loaded.MetaData?.AsByteArray(), $"Length: {length}");
            }
        }

        [TestMethod]
        public async Task LoadedTreeIsReadyToUse()
        {
            var loaded = await LoadAsync(await SaveAsync(Node("R", Node("A"))).ConfigureAwait(false)).ConfigureAwait(false);

            Assert.IsNull(loaded.Parent);
            Assert.AreEqual(loaded, loaded[0].Parent);

            // The streams are rewound, so they can be read directly
            Assert.AreEqual('R', loaded.Data?.ReadByte());
            Assert.AreEqual('A', loaded[0].Data?.ReadByte());
        }

        [TestMethod]
        public async Task LoadFromReadOnlyFile()
        {
            string fileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.prm");

            try
            {
                await Node("R").SaveAsync(fileName).ConfigureAwait(false);
                File.SetAttributes(fileName, FileAttributes.ReadOnly);

                if (!OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(fileName, UnixFileMode.UserRead);
                }

                var loaded = await Element.LoadAsync(fileName).ConfigureAwait(false);
                Assert.AreEqual("R", loaded.Data?.AsAsciiString());
            }
            finally
            {
                File.SetAttributes(fileName, FileAttributes.Normal);
                File.Delete(fileName);
            }
        }

        #endregion

        #region Deep and malformed trees

        [TestMethod]
        public async Task DeepTree()
        {
            var root = Node("0");
            Element current = root;

            for (int i = 1; i < Depth; i++)
            {
                var child = new Element(Data.FromVarInt(i), null);
                current.Add(child);
                current = child;
            }

            var loaded = await LoadAsync(await SaveAsync(root).ConfigureAwait(false)).ConfigureAwait(false);

            Assert.AreEqual(Depth, loaded.GetStatistics().Depth);

            long expected = 0;
            loaded.Walk((node, ancestors) =>
            {
                if (ancestors.Count > 0)
                {
                    Assert.AreEqual(++expected, node.Data?.AsVarInt());
                }
            });
            Assert.AreEqual(Depth - 1, expected);
            Assert.AreEqual(Depth, new Element(loaded).GetStatistics().Nodes);

            loaded.Remove(loaded[0]);
            Assert.AreEqual(0, loaded.Children.Count);
        }

        [TestMethod]
        public async Task DeepStreamOfPlaceholders()
        {
            // A malformed (or malicious) stream: only placeholder nodes going down
            byte[] bytes = Enumerable.Repeat((byte)0x40, Depth).Append((byte)0x00).ToArray();

            var loaded = await LoadAsync(bytes).ConfigureAwait(false);

            Assert.IsNull(loaded.Data);
            Assert.AreEqual(0, loaded.Children.Count);
        }

        [TestMethod]
        public async Task ManyEmptySiblings()
        {
            byte[] bytes = new byte[] { 0x41, 0x52, 0x41, 0x41 }
                .Concat(Enumerable.Repeat((byte)0xC0, Depth))
                .Concat(new byte[] { 0xC1, 0x42, 0x00 })
                .ToArray();

            var loaded = await LoadAsync(bytes).ConfigureAwait(false);

            Assert.AreEqual(2, loaded.Children.Count);
            Assert.AreEqual("A", loaded[0].Data?.AsAsciiString());
            Assert.AreEqual("B", loaded[1].Data?.AsAsciiString());
        }

        [TestMethod]
        public async Task NavigatingAboveTheRoot()
        {
            await Assert.ThrowsExceptionAsync<PromptuariumException>(() => LoadAsync(new byte[] { 0x81, 0x52, 0x80, 0x00 })).ConfigureAwait(false);
        }

        #endregion

        #region Tree operations

        [TestMethod]
        public void AddMovesNodeFromItsPreviousParent()
        {
            var child = Node("C");
            var tree1 = Node("1", child);
            var tree2 = Node("2");

            tree2.Add(child);

            Assert.AreEqual(0, tree1.Children.Count);
            Assert.AreEqual(1, tree2.Children.Count);
            Assert.AreEqual(tree2, child.Parent);
        }

        [TestMethod]
        public void ConstructorMovesNodeFromItsPreviousParent()
        {
            var child = Node("C");
            var tree1 = Node("1", child);
            var tree2 = Node("2", child);

            Assert.AreEqual(0, tree1.Children.Count);
            Assert.AreEqual(tree2, child.Parent);
        }

        [TestMethod]
        public void AddRefusesCycles()
        {
            var grandChild = Node("G");
            var child = Node("C", grandChild);
            var tree = Node("R", child);

            Assert.ThrowsException<PromptuariumException>(() => grandChild.Add(tree));
            Assert.ThrowsException<PromptuariumException>(() => tree.Add(tree));
            Assert.ThrowsException<PromptuariumException>(() => tree.Add(child));
            Assert.AreEqual(child, grandChild.Parent);
            Assert.AreEqual(1, tree.Children.Count);
        }

        [TestMethod]
        public void AddRefusesNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => Node("R").Add((Element)null!));
        }

        [TestMethod]
        public void CopyConstructorCreatesDeepCopy()
        {
            var child = Node("C", Node("G"));
            var original = new Element(Data.FromAsciiString("R"), Data.FromInt(42), child);

            var copy = new Element(original);

            Assert.IsTrue(original.StructurallyEquals(copy));
            Assert.IsNull(copy.Parent);
            Assert.AreNotSame(child, copy[0]);
            Assert.AreEqual(copy, copy[0].Parent);

            // The original is untouched
            Assert.AreEqual(original, child.Parent);
            Assert.AreEqual(1, original.Children.Count);

            // The content is copied, too
            copy.Data!.WriteByte((byte)'X');
            Assert.AreEqual("R", original.Data?.AsAsciiString());
        }

        [TestMethod]
        public void WalkVisitsNodesInPreOrder()
        {
            var tree = Node("R", Node("A", Node("A1"), Node("A2")), Node("B"));
            var visited = new List<string>();

            tree.Walk((node, ancestors) => visited.Add($"{node.Data?.AsAsciiString()}:{string.Join("/", ancestors.Select(a => a.Data?.AsAsciiString()))}"));

            CollectionAssert.AreEqual(new[] { "R:", "A:R", "A1:R/A", "A2:R/A", "B:R" }, visited);
        }

        [TestMethod]
        public void StatisticsOfLeaf()
        {
            Statistics stats = new Element().GetStatistics();

            Assert.AreEqual(1, stats.Nodes);
            Assert.AreEqual(0, stats.MinChildren);
            Assert.AreEqual(0, stats.ShortestData);
            Assert.AreEqual(0, stats.ShortestMetaData);
        }

        [TestMethod]
        public void StatisticsMinimums()
        {
            var tree = Node("Root", Node("ab", Node("abc"), Node("abcd")), Node("abcde", Node("x"), Node("y")));

            Statistics stats = tree.GetStatistics();

            Assert.AreEqual(0, stats.MinChildren);
            Assert.AreEqual(1, stats.ShortestData);
            Assert.AreEqual(5, stats.LongestData);
        }

        #endregion

        #region Data conversions

        [TestMethod]
        public void VarIntLimits()
        {
            long[] values = { 0, 1, -1, 127, -128, 300, -300, int.MaxValue, int.MinValue, long.MaxValue, long.MinValue + 1, long.MinValue };

            foreach (long value in values)
            {
                Assert.AreEqual(value, Data.FromVarInt(value).AsVarInt(), $"Value: {value}");
            }

            ulong[] unsignedValues = { 0, 1, 255, 256, 300, uint.MaxValue, ulong.MaxValue };

            foreach (ulong value in unsignedValues)
            {
                Assert.AreEqual(value, Data.FromVarUInt(value).AsVarUInt(), $"Value: {value}");
            }

            CollectionAssert.AreEqual(new byte[] { 0x07 }, Data.FromVarInt(-3).AsByteArray());
            CollectionAssert.AreEqual(new byte[] { 0x2C, 0x01 }, Data.FromVarUInt(300).AsByteArray());
            CollectionAssert.AreEqual(new byte[] { 0x00 }, Data.FromVarUInt(0).AsByteArray());
        }

        [TestMethod]
        public void DecimalFormatIsCompatibleWithBinaryWriter()
        {
            decimal[] values = { 0m, 1.978m, -1.978m, decimal.MaxValue, decimal.MinValue, 0.0000000000000000000000000001m };

            foreach (decimal value in values)
            {
                using var expected = new MemoryStream();
                using (var writer = new BinaryWriter(expected, System.Text.Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(value);
                }

                CollectionAssert.AreEqual(expected.ToArray(), Data.FromDecimal(value).AsByteArray(), $"Value: {value}");
                Assert.AreEqual(value, Data.FromDecimal(value).AsDecimal());
            }
        }

        [TestMethod]
        public async Task ElementAsData()
        {
            var inner = Node("Inner", Node("Child"));

            using Stream stream = await Data.FromElementAsync(inner).ConfigureAwait(false);
            Assert.AreEqual(0, stream.Position);

            var outer = new Element(stream, null);
            var loaded = await LoadAsync(await SaveAsync(outer).ConfigureAwait(false)).ConfigureAwait(false);
            var innerLoaded = await loaded.Data!.AsElementAsync().ConfigureAwait(false);

            Assert.IsTrue(inner.StructurallyEquals(innerLoaded));
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

        private static async Task<Element> LoadAsync(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            return await Element.LoadAsync(stream).ConfigureAwait(false);
        }

        #endregion
    }
}
