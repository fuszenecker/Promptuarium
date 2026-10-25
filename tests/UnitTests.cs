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
    /// Unit tests for the Promptuarium tree
    /// </summary>
    [TestClass]
    public class UnitTests
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext? TestContext { get; set; }

        private string TestFileName = CreateTestFileName();

        #region Additional test attributes
        // Use TestInitialize to run code before running each test
        [TestInitialize()]
        public void Initialize()
        {
            TestFileName = CreateTestFileName();
        }

        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
        }
        #endregion

        [TestMethod]
        public void ParameterlessConstructor()
        {
            var node = new Element();

            Assert.IsTrue(node.Data.IsEmpty);
            Assert.IsTrue(node.Data.IsEmpty);
            Assert.IsTrue(node.MetaData.IsEmpty);
            Assert.IsNotNull(node.Children);
            Assert.IsNull(node.Parent);
        }

        [TestMethod]
        public void CopyConstructor()
        {
            var tree = new Element();
            var child = new Element();
            var parent = new Element();

            tree.Data = new byte[] { 2, 3, 5, 7, 11 };
            tree.Add(child);
            parent.Add(tree);

            Assert.AreEqual(2, tree.Data.Span[0]);
            Assert.AreEqual(3, tree.Data.Span[1]);
            Assert.AreEqual(5, tree.Data.Span[2]);
            Assert.AreEqual(7, tree.Data.Span[3]);
            Assert.AreEqual(11, tree.Data.Span[4]);

            Assert.IsNotNull(tree.Children.FirstOrDefault(c => c == child));
            Assert.AreEqual(parent, tree.Parent);
        }

        [TestMethod]
        public void ComplexConstructor()
        {
            var child1 = new Element();
            var child2 = new Element();
            var parent = new Element();
            byte[] data = new byte[] { 2, 3, 5, 7, 11 };

            var tree = new Element(null, data, child1, child2);
            parent.Add(tree);

            Assert.AreEqual(2, tree.MetaData.Span[0]);
            Assert.AreEqual(3, tree.MetaData.Span[1]);
            Assert.AreEqual(5, tree.MetaData.Span[2]);
            Assert.AreEqual(7, tree.MetaData.Span[3]);
            Assert.AreEqual(11, tree.MetaData.Span[4]);

            Assert.IsNotNull(tree.Children.FirstOrDefault(c => c == child1));
            Assert.IsNotNull(tree.Children.FirstOrDefault(c => c == child2));
            Assert.AreEqual(parent, tree.Parent);
        }

        [TestMethod]
        public void LinqFriendlyConstructor()
        {
            var child1 = new Element();
            var child2 = new Element();
            var parent = new Element();
            byte[] data = new byte[] { 2, 3, 5, 7, 11 };

            var tree = new Element(data, data,
                from child in new byte[] { 1, 1, 2, 3, 5, 8 } select new Element(new byte[] { child, child, child }, new byte[] { 5 })
                );
            parent.Add(tree);

            Assert.AreEqual(2, tree.Data.Span[0]);
            Assert.AreEqual(3, tree.Data.Span[1]);
            Assert.AreEqual(5, tree.Data.Span[2]);
            Assert.AreEqual(7, tree.Data.Span[3]);
            Assert.AreEqual(11, tree.Data.Span[4]);

            Assert.AreEqual(5, tree.Children.ToArray()[4].Data.Span[0]);
            Assert.AreEqual(8, tree.Children.ToArray()[5].Data.Span[0]);
            Assert.AreEqual(parent, tree.Parent);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Indexer()
        {
            var child1 = new Element();
            var child2 = new Element();
            var parent = new Element();
            byte[] data = new byte[] { 2, 3, 5, 7, 11 };

            var tree = new Element(data, null, child1, child2);
            parent.Add(tree);

            Assert.AreEqual(2, tree.Data.Span[0]);
            Assert.AreEqual(3, tree.Data.Span[1]);
            Assert.AreEqual(5, tree.Data.Span[2]);
            Assert.AreEqual(7, tree.Data.Span[3]);
            Assert.AreEqual(11, tree.Data.Span[4]);

            Assert.AreEqual(parent, tree.Parent);
            Assert.AreEqual(child1, tree[0]);
            Assert.AreEqual(child2, tree[1]);
            Assert.AreEqual(child2, tree[2]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddNullThrowsException()
        {
            new Element().Add(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(PromptuariumException))]
        public void Add()
        {
            var child1 = new Element(new byte[] { 1 }, null);
            var child2 = new Element(new byte[] { 2 }, null);
            var child3 = new Element(new byte[] { 3 }, null);

            var tree = new Element(null, null);

            tree.Add(child1);
            tree.Add(child2, child3);

            Assert.AreEqual(null, tree.Parent);
            Assert.AreEqual(child1, tree[0]);
            Assert.AreEqual(child2, tree[1]);
            Assert.AreEqual(child3, tree[2]);

            Assert.AreEqual(3, tree.Children.Count);

            // tree.Add(null);
            // Assert.AreEqual(3, tree.Children.Count);

            tree.Add(child1);
            Assert.AreEqual(3, tree.Children.Count);
        }

        [TestMethod]
        public void AddLinq()
        {
            var tree = new Element(null, null);

            tree.Add(
                new byte[] { 5, 9, 3, 6, 7 }.Select(n => new Element
                                                         (
                                                             new[] { n }, new[] { n },
                                                             new Element(new byte[] { 66 }, null)
                                                         ))
                );

            Assert.AreEqual(null, tree.Parent);

            Assert.AreEqual(5, tree[0].Data.Span[0]);
            Assert.AreEqual(9, tree[1].Data.Span[0]);
            Assert.AreEqual(7, tree[4].Data.Span[0]);
        }

        [TestMethod]
        public void AddCollectionInitializer()
        {
            var tree = new Element()
            {
                new Element(new byte[] { 1 }, null),
                new Element(new byte[] { 2 }, null),
                new Element(new byte[] { 3 }, null)
            };

            Assert.AreEqual(null, tree.Parent);
            Assert.AreEqual(3, tree.Children.Count);
        }

        [TestMethod]
        public void CheckEnumerability()
        {
            var tree = new Element(null, null);

            tree.Add(
                new byte[] { 5, 9, 3, 6, 7 }.Select(n => new Element
                                                         (
                                                             new[] { n }, new[] { n },
                                                             new Element(new byte[] { 66 }, null)
                                                         ))
                );

            Assert.AreEqual(null, tree.Parent);

            Assert.AreEqual(5, tree[0].Data.Span[0]);
            Assert.AreEqual(9, tree[1].Data.Span[0]);
            Assert.AreEqual(7, tree[4].Data.Span[0]);

            int i = 0;
            foreach (var child in tree)
            {
                Assert.AreEqual(tree[i++], child);
            }
        }

        [TestMethod]
        public void Remove()
        {
            var tree = CreateTestTree();
            var nodes = tree.GetStatistics().Nodes;

            tree.Remove(tree[2]);
            // tree.Remove(null);
            tree.Remove(tree[0]);

            Assert.AreEqual(6, nodes - tree.GetStatistics().Nodes);
        }

        [TestMethod]
        public void Detach()
        {
            var tree = CreateTestTree();
            var reference = tree[1];

            var child2 = tree[1].Detach();

            Assert.AreEqual(reference, child2);
            Assert.AreEqual(240, child2.MetaData.Span[0]);
            Assert.AreEqual(3, tree.Children.Count);
        }

        [TestMethod]
        public async Task Save()
        {
            var tree = CreateTestTree();
            using var stream = new MemoryStream();

            await tree.SaveAsync(stream).ConfigureAwait(false);
            await tree.SaveAsync(TestFileName).ConfigureAwait(false);

            Assert.AreEqual(stream.Length, new FileInfo(TestFileName).Length);
        }

        [TestMethod]
        public async Task Load()
        {
            byte[] data = new byte[]
                              {
                                  0x41, 0x80,
                                  0x42, 0x19, 0x78, 0x02, 0x19, 0x86, 0x21, 0xaa,
                                  0x65, 0x41, 0x42, 0x43, 0x44, 0x45,
                                  0x81, 0x13,
                                  0xc1, 0x55,
                                  0x00
                              };

            using var stream = new MemoryStream(data);

            var tree = await Element.LoadAsync(stream).ConfigureAwait(false);

            Assert.AreEqual((byte)0x80, tree.Data.AsByte());
            Assert.AreEqual((short)0x7819, tree[0].Data.AsShort());
            Assert.AreEqual((uint)0x86197819, tree[0].Data.AsUInt());
            Assert.AreEqual((uint)0x44434241, tree[0][0].MetaData.AsUInt());
            Assert.AreEqual(true, tree[2].Data.AsBool());
            Assert.IsTrue(tree[2].MetaData.IsEmpty);
        }

        [TestMethod]
        public async Task EmptyFirstChild()
        {
            var tree = new Element(Data.FromAsciiString("P"), null,
                new Element(),
                new Element(Data.FromAsciiString("M"), null));

            using var stream = new MemoryStream();
            await tree.SaveAsync(stream).ConfigureAwait(false);
            stream.Position = 0;

            var tree2 = await Element.LoadAsync(stream).ConfigureAwait(false);

            // The empty node is kept, so the positions of the siblings do not change
            Assert.IsTrue(tree.StructurallyEquals(tree2));
            Assert.AreEqual(2, tree2.Children.Count);
            Assert.IsTrue(tree2[0].Data.IsEmpty);
            Assert.AreEqual("M", tree2[1].Data.AsAsciiString());
        }

        [TestMethod]
        public async Task EmptyLastChildAfterSubtree()
        {
            var tree = new Element(Data.FromAsciiString("R"), null,
                new Element(Data.FromAsciiString("X"), null,
                    new Element(Data.FromAsciiString("C"), null,
                        new Element(Data.FromAsciiString("D"), null)),
                    new Element()),
                new Element(Data.FromAsciiString("Y"), null));

            using var stream = new MemoryStream();
            await tree.SaveAsync(stream).ConfigureAwait(false);
            stream.Position = 0;

            var tree2 = await Element.LoadAsync(stream).ConfigureAwait(false);

            Assert.IsTrue(tree.StructurallyEquals(tree2));
            Assert.AreEqual(2, tree2[0].Children.Count);
            Assert.AreEqual("D", tree2[0][0][0].Data.AsAsciiString());
            Assert.AreEqual(0, tree2[0][1].Children.Count);
            Assert.AreEqual("Y", tree2[1].Data.AsAsciiString());
        }

        [TestMethod]
        public async Task TreeToStringTest()
        {
            var tree = CreateTestTree();
            await tree.SaveAsync(TestFileName).ConfigureAwait(false);

            var tree2 = await Element.LoadAsync(TestFileName).ConfigureAwait(false);

            string treeString = tree.TreeToString();
            string treeString2 = tree2.TreeToString();

            Assert.AreEqual(treeString2, treeString);
        }

        [TestMethod]
        public async Task FromInt()
        {
            var tree = CreateTestTree();

            var subNode = new Element
            {
                Data = Data.FromInt(0x55aa00ff)
            };

            tree.Add(subNode);

            await tree.SaveAsync(TestFileName).ConfigureAwait(false);
            var tree2 = await Element.LoadAsync(TestFileName).ConfigureAwait(false);

            Assert.AreEqual(0x55aa00ff, tree[tree.Children.Count - 1].Data.AsInt());
            Assert.AreEqual(0x55aa00ff, tree2[tree2.Children.Count - 1].Data.AsInt());
        }

        [TestMethod]
        public async Task RoundTrip()
        {
            var tree = CreateTestTree();

            await tree.SaveAsync(TestFileName).ConfigureAwait(false);
            var tree2 = await Element.LoadAsync(TestFileName).ConfigureAwait(false);

            Assert.IsTrue(tree.StructurallyEquals(tree2));
        }

        [TestMethod]
        public async Task Long()
        {
            var tree = CreateTestTree();

            var numericNode = new Element
            {
                Data = Data.FromLong(0x55aa00ff19781986)
            };

            tree[0].Add(numericNode);

            await tree.SaveAsync(TestFileName).ConfigureAwait(false);
            var tree2 = await Element.LoadAsync(TestFileName).ConfigureAwait(false);

            Assert.AreEqual(0x55aa00ff19781986, tree2[0][1].Data.AsLong());
        }

        [TestMethod]
        public async Task Double()
        {
            var tree = CreateTestTree();

            var numericNode = new Element
            {
                MetaData = Data.FromDouble(3.14159265359)
            };

            tree[0].Add(numericNode);

            await tree.SaveAsync(TestFileName).ConfigureAwait(false);
            var tree2 = await Element.LoadAsync(TestFileName).ConfigureAwait(false);

            double? value = tree2[0][1].MetaData.AsDouble();
            Assert.IsTrue(value > 3.1415926 && value < 3.1415927);
        }

        [TestMethod]
        public async Task GuidTest()
        {
            Guid testGuid = Guid.NewGuid();

            var tree = new Element();

            var guidNode = new Element
            {
                Data = Data.FromGuid(testGuid)
            };

            tree.Add(guidNode);

            await tree.SaveAsync(TestFileName).ConfigureAwait(false);
            var tree2 = await Element.LoadAsync(TestFileName).ConfigureAwait(false);

            Assert.AreEqual(0, testGuid.CompareTo(tree2[0].Data.AsGuid()));
        }

        [TestMethod]
        public async Task Decimal()
        {
            var tree = new Element();

            var decimalNode = new Element
            {
                MetaData = Data.FromDecimal(1.978M)
            };

            tree.Add(decimalNode);

            await tree.SaveAsync(TestFileName).ConfigureAwait(false);
            var tree2 = await Element.LoadAsync(TestFileName).ConfigureAwait(false);

            Assert.AreEqual(1.978M, tree2[0].MetaData.AsDecimal());
        }

        [TestMethod]
        public async Task ByteArray()
        {
            byte[] testBytes = new byte[] { 1, 1, 2, 3, 5, 8, 13, 21 };

            var tree = new Element();

            var byteArrayNode = new Element
            {
                Data = Data.FromByteArray(testBytes)
            };

            tree.Add(byteArrayNode);

            await tree.SaveAsync(TestFileName).ConfigureAwait(false);
            var tree2 = await Element.LoadAsync(TestFileName).ConfigureAwait(false);

            byte[]? testBytes2 = tree2[0].Data.AsByteArray();

            for (int i = 0; i < testBytes.Length; i++)
            {
                Assert.AreEqual(testBytes[i], testBytes2?[i]);
            }
        }

        [TestMethod]
        public async Task Unicode()
        {
            var tree = new Element();

            var unicodeNode = new Element
            {
                Data = Data.FromUtf32String("Hello, UTF-32LE world!"),
                MetaData = Data.FromUtf16String("Hello, UTF-16LE world!")
            };

            tree.Add(unicodeNode);

            await tree.SaveAsync(TestFileName).ConfigureAwait(false);
            var tree2 = await Element.LoadAsync(TestFileName).ConfigureAwait(false);

            Assert.AreEqual("Hello, UTF-32LE world!", tree2[0].Data.AsUtf32String());
            Assert.AreEqual("Hello, UTF-16LE world!", tree2[0].MetaData.AsUtf16String());
        }

        [TestMethod]
        public async Task ElementTest()
        {
            var outerTree = new Element();
            var innerTree = new Element();

            innerTree.Add(new Element() { Data = Data.FromInt(0x55aa0044) });
            innerTree.Add(new Element() { MetaData = Data.FromInt(0x55aa00ff) });

            outerTree.MetaData = Data.FromShort(1000);
            outerTree.Data = await Data.FromElementAsync(innerTree).ConfigureAwait(false);

            await outerTree.SaveAsync(TestFileName).ConfigureAwait(false);

            var outerTree2 = await Element.LoadAsync(TestFileName).ConfigureAwait(false);
            var innerTree2 = await outerTree2.Data!.AsElementAsync().ConfigureAwait(false);

            Assert.AreEqual((short)1000, outerTree2.MetaData.AsShort());
            Assert.AreEqual(0x55aa00ff, innerTree2[1].MetaData.AsInt());
        }

        [TestMethod]
        public async Task DateTimeTimeSpan()
        {
            var tree = new Element();

            var birthDay = new DateTime(1978, 10, 27, 8, 0, 0, 230);
            var testTimeSpan = new TimeSpan(1978, 12, 27, 8, 239);

            tree.Add(new Element
            {
                Data = Data.FromDateTime(birthDay),
                MetaData = Data.FromTimeSpan(testTimeSpan)
            });

            await tree.SaveAsync(TestFileName).ConfigureAwait(false);
            var tree2 = await Element.LoadAsync(TestFileName).ConfigureAwait(false);

            Assert.AreEqual(0, birthDay.CompareTo(tree2[0].Data.AsDateTime()));
            Assert.AreEqual(0, testTimeSpan.CompareTo(tree2[0].MetaData.AsTimeSpan()));

            Assert.IsTrue(tree.StructurallyEquals(tree2));
        }

        [TestMethod]
        public async Task DateTimeOffset()
        {
            var tree = new Element();

            var testTimeSpan = new TimeSpan(3, 0, 0);
            var birthDay = new DateTimeOffset(1978, 10, 27, 8, 0, 0, 230, testTimeSpan);

            tree.Add(new Element
            {
                Data = Data.FromDateTimeOffset(birthDay),
            });

            await tree.SaveAsync(TestFileName).ConfigureAwait(false);
            var tree2 = await Element.LoadAsync(TestFileName).ConfigureAwait(false);

            Assert.AreEqual(0, birthDay.CompareTo(tree2[0].Data.AsDateTimeOffset()));

            Assert.IsTrue(tree.StructurallyEquals(tree2));
        }

        [TestMethod]
        public async Task Char()
        {
            var tree = new Element();

            var unicodeNode = new Element
            {
                Data = Data.FromChar('Ä'),
                MetaData = Data.FromChar('Ő')
            };

            tree.Add(unicodeNode);

            await tree.SaveAsync(TestFileName).ConfigureAwait(false);
            var tree2 = await Element.LoadAsync(TestFileName).ConfigureAwait(false);

            Assert.AreEqual('Ä', tree2[0].Data.AsChar());
            Assert.AreEqual('Ő', tree2[0].MetaData.AsChar());

            Assert.IsTrue(tree.StructurallyEquals(tree2));
        }

        [TestMethod]
        public async Task StringPerformance()
        {
            var tree = new Element();

            for (int i = 0; i < 4000; ++i)
            {
                tree.Add(
                    new Element
                    {
                        Data = Data.FromUtf8String("+36702815816")
                    }
                );
            }

            await tree.SaveAsync(TestFileName).ConfigureAwait(false);
            var tree2 = await Element.LoadAsync(TestFileName).ConfigureAwait(false);

            Assert.IsTrue(tree.StructurallyEquals(tree2));
        }

        [TestMethod]
        public async Task IntPerformance()
        {
            var tree = new Element();

            for (int i = 0; i < 4000; ++i)
            {
                tree.Add(
                    new Element
                    {
                        Data = Data.FromInt(367028158)
                    }
                );
            }

            await tree.SaveAsync(TestFileName).ConfigureAwait(false);
            var tree2 = await Element.LoadAsync(TestFileName).ConfigureAwait(false);
            Assert.IsTrue(tree.StructurallyEquals(tree2));
        }

        [TestMethod]
        public async Task StreamCompatibility()
        {
            using var stream = new MemoryStream();

            var treeA = new Element() { Data = Data.FromAsciiString("A") };

            treeA.Add(
                new Element { Data = Data.FromAsciiString("A1") },
                new Element { Data = Data.FromAsciiString("A2") }
            );

            var treeB = new Element() { Data = Data.FromAsciiString("B") };

            treeB.Add(
                new Element { Data = Data.FromAsciiString("B1") },
                new Element { Data = Data.FromAsciiString("B2") }
            );

            await treeA.SaveAsync(stream).ConfigureAwait(false);
            await treeB.SaveAsync(stream).ConfigureAwait(false);

            stream.Position = 0;

            var tree2A = await Element.LoadAsync(stream).ConfigureAwait(false);
            var tree2B = await Element.LoadAsync(stream).ConfigureAwait(false);

            Assert.IsTrue(treeA.StructurallyEquals(tree2A));
            Assert.IsTrue(treeB.StructurallyEquals(tree2B));
        }

        [TestMethod]
        public void VarInt100()
        {
            ReadOnlyMemory<byte> content = Data.FromVarInt(100);

            ReadOnlySpan<byte> buffer = content.Span;

            Assert.AreEqual(1, content.Length);
            Assert.AreEqual(200, buffer[0]);
            Assert.AreEqual(100, content.AsVarInt());
        }

        [TestMethod]
        public void VarIntMinus100()
        {
            ReadOnlyMemory<byte> content = Data.FromVarInt(-100);

            ReadOnlySpan<byte> buffer = content.Span;

            Assert.AreEqual(1, content.Length);
            Assert.AreEqual(201, buffer[0]);
            Assert.AreEqual(-100, content.AsVarInt());
        }

        [TestMethod]
        public void VarInt65537()
        {
            ReadOnlyMemory<byte> content = Data.FromVarInt(65537);

            ReadOnlySpan<byte> buffer = content.Span;

            Assert.AreEqual(3, content.Length);
            Assert.AreEqual(2, buffer[2]);
            Assert.AreEqual(0, buffer[1]);
            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(65537, content.AsVarInt());
        }

        [TestMethod]
        public void VarIntMinus65537()
        {
            ReadOnlyMemory<byte> content = Data.FromVarInt(-65537);

            ReadOnlySpan<byte> buffer = content.Span;

            Assert.AreEqual(3, content.Length);
            Assert.AreEqual(2, buffer[2]);
            Assert.AreEqual(0, buffer[1]);
            Assert.AreEqual(3, buffer[0]);
            Assert.AreEqual(-65537, content.AsVarInt());
        }

        [TestMethod]
        public void VarInt0()
        {
            ReadOnlyMemory<byte> content = Data.FromVarInt(0);

            ReadOnlySpan<byte> buffer = content.Span;

            Assert.AreEqual(1, content.Length);
            Assert.AreEqual(0, buffer[0]);
            Assert.AreEqual(0, content.AsVarInt());
        }

        [TestMethod]
        public void VarUInt100()
        {
            ReadOnlyMemory<byte> content = Data.FromVarInt(100);

            ReadOnlySpan<byte> buffer = content.Span;

            Assert.AreEqual(1, content.Length);
            Assert.AreEqual(200, buffer[0]);
            Assert.AreEqual(100, content.AsVarInt());
        }

        [TestMethod]
        public void VarUInt65537()
        {
            ReadOnlyMemory<byte> content = Data.FromVarInt(65537);

            ReadOnlySpan<byte> buffer = content.Span;

            Assert.AreEqual(3, content.Length);
            Assert.AreEqual(2, buffer[2]);
            Assert.AreEqual(0, buffer[1]);
            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(65537, content.AsVarInt());
        }

        [TestMethod]
        public void VarUInt0()
        {
            ReadOnlyMemory<byte> content = Data.FromVarInt(0);

            ReadOnlySpan<byte> buffer = content.Span;

            Assert.AreEqual(1, content.Length);
            Assert.AreEqual(0, buffer[0]);
            Assert.AreEqual(0, content.AsVarInt());
        }

        [TestMethod]
        public void Base64RoundTrip()
        {
            var tree = new Element(Data.FromUtf8String("ROOT NODE"), Data.FromUtf8String("UTF-8 STRING"),
                            from i in new int[3] { 2, 3, 5 }
                            select new Element(
                                Data.FromInt(i),
                                Data.FromUtf8String("CHILD")));

            string base64 = tree.ToBase64String();

            Assert.AreEqual(Convert.ToBase64String(tree.ToBytes()), base64);
            Assert.IsTrue(tree.StructurallyEquals(Element.FromBase64String(base64)));
        }

        [TestMethod]
        public async Task CleanupStream()
        {
            var tree = new Element() { MetaData = Data.FromAsciiString("root") };
            var child1 = new Element() { MetaData = Data.FromAsciiString("child1") };
            var child2 = new Element() { MetaData = Data.FromAsciiString("child2") };
            var child3 = new Element() { MetaData = Data.FromAsciiString("child3") };

            child2.Add(child3);
            child1.Add(child2);
            tree.Add(child1);

            using var ms = new MemoryStream();
            await tree.SaveAsync(ms).ConfigureAwait(false);

            Assert.AreEqual(27, ms.Length);
        }

        [TestMethod]
        public async Task EmptyNode()
        {
            var tree = new Element();

            await tree.SaveAsync(TestFileName).ConfigureAwait(false);

            // The empty root (60) and the terminator (00)
            Assert.AreEqual(2, new FileInfo(TestFileName).Length);
            Assert.IsTrue(tree.StructurallyEquals(await Element.LoadAsync(TestFileName).ConfigureAwait(false)));
        }

        [TestMethod]
        public void Statistics()
        {
            var tree = new Element(Data.FromInt(0x0), null);

            var ch_A = new Element(Data.FromInt(0x100), null);
            var ch_B = new Element(Data.FromInt(0x200), null);
            var ch_C = new Element(Data.FromInt(0x300), null);

            tree.Add(ch_A, ch_B, ch_C);

            var ch_A_1 = new Element(Data.FromInt(0x110), null);
            var ch_A_2 = new Element(Data.FromInt(0x120), null);

            ch_A.Add(ch_A_1, ch_A_2);

            var ch_C_1 = new Element(Data.FromInt(0x310), null);
            var ch_C_2 = new Element(Data.FromInt(0x320), null);
            var ch_C_3 = new Element(Data.FromInt(0x330), null);
            var ch_C_4 = new Element(Data.FromInt(0x340), Data.FromAsciiString("123456789"));

            ch_C.Add(ch_C_1, ch_C_2, ch_C_3, ch_C_4);

            var ch_C_3_I = new Element(Data.FromInt(0x331), null);

            ch_C_3.Add(ch_C_3_I);

            Statistics stats = tree.GetStatistics();

            Assert.AreEqual(4, stats.Depth);
            Assert.AreEqual(11, stats.Nodes);
            Assert.AreEqual(0, stats.MinChildren);
            Assert.AreEqual(4, stats.MaxChildren);
            Assert.AreEqual(4, stats.LongestData);
            Assert.AreEqual(9, stats.LongestMetaData);
            Assert.AreEqual(4, stats.ShortestData);
            Assert.AreEqual(9, stats.ShortestMetaData);
            Assert.AreEqual(10, stats.NodesWithData);
            Assert.AreEqual(1, stats.NodesWithDataAndMetaData);
            Assert.AreEqual(7, stats.NodesWithoutChildren);
        }

        [TestMethod]
        public async Task Base64Test()
        {
            var tree1 = new Element(Data.FromInt(0x0), null);

            var ch_A = new Element(Data.FromInt(0x100), null);
            var ch_B = new Element(Data.FromInt(0x200), null);
            var ch_C = new Element(Data.FromInt(0x300), null);

            tree1.Add(ch_A, ch_B, ch_C);

            var ch_A_1 = new Element(Data.FromInt(0x110), null);
            var ch_A_2 = new Element(Data.FromInt(0x120), null);

            ch_A.Add(ch_A_1, ch_A_2);

            var ch_C_1 = new Element(Data.FromInt(0x310), null);
            var ch_C_2 = new Element(Data.FromInt(0x320), null);
            var ch_C_3 = new Element(Data.FromInt(0x330), null);
            var ch_C_4 = new Element(Data.FromInt(0x340), Data.FromAsciiString("123456789"));

            ch_C.Add(ch_C_1, ch_C_2, ch_C_3, ch_C_4);

            var ch_C_3_I = new Element(Data.FromInt(0x331), null);

            ch_C_3.Add(ch_C_3_I);

            string base64String = await tree1.ToBase64StringAsync().ConfigureAwait(false);

            var tree2 = await Element.FromBase64StringAsync(base64String).ConfigureAwait(false);

            Assert.IsTrue(tree1.StructurallyEquals(tree2));
        }

        #region Private methods
        private static Element CreateTestTree()
        {
            byte[] longData = new byte[150];
            byte[] veryLongData = new byte[7 * 512 * 1024 + 5 * 16 + 2];

            longData[0] = 0xf0;
            veryLongData[0] = 0xf1;
            veryLongData[10] = 0xf2;

            var node = new Element
                            (
                                new byte[] { 128, 96 }, null,

                                new Element(null, new byte[] { 5, 6 }),
                                new Element(null, longData),
                                new Element(null, new byte[] { 8, 8 },
                                    new Element(longData, null,
                                        new Element(new byte[] { 18, 28 }, null,
                                            new Element(new byte[] { 1, 7 }, null)
                                        )
                                    )
                                ),

                                new Element
                                {
                                    Data = Data.FromUtf8String("Hello World"),
                                    MetaData = Data.FromGuid(Guid.NewGuid())
                                }
                            );

            node[0].Add(new Element(
                veryLongData,
                longData)
            );

            return node;
        }

        private static string CreateTestFileName()
        {
            if (!Directory.Exists("test-data"))
            {
                Directory.CreateDirectory("test-data");
            }

            return $"test-data{Path.DirectorySeparatorChar}{Guid.NewGuid()}.prm";
        }
        #endregion
    }
}
