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
    /// Unit tests for the Promptuarium tree
    /// </summary>
    [TestClass]
    public class UnitTests
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void ParameterlessConstructor()
        {
            Element node = new Element();

            Assert.IsNull(node.Data);
            Assert.IsNull(node.Data);
            Assert.IsNull(node.MetaData);
            Assert.IsNotNull(node.Children);
            Assert.IsNull(node.Parent);
        }

        [TestMethod]
        public void CopyConstructor()
        {
            Element tree = new Element();
            Element child = new Element();
            Element parent = new Element();

            tree.Data = new MemoryStream(new byte[] {2, 3, 5, 7, 11});
            tree.Add(child);
            tree.Parent = parent;

            Assert.AreEqual(2, tree.Data.ReadByte());
            Assert.AreEqual(3, tree.Data.ReadByte());
            Assert.AreEqual(5, tree.Data.ReadByte());
            Assert.AreEqual(7, tree.Data.ReadByte());
            Assert.AreEqual(11, tree.Data.ReadByte());

            Assert.IsNotNull(tree.Children.FirstOrDefault(c => c == child));
            Assert.AreEqual(parent, tree.Parent);
        }

        [TestMethod]
        public void ComplexConstructor()
        {
            Element child1 = new Element();
            Element child2 = new Element();
            Element parent = new Element();
            byte[] data = new byte[] { 2, 3, 5, 7, 11 };

            Element tree = new Element(null, new MemoryStream(data), child1, child2)
            {
                Parent = parent
            };

            Assert.AreEqual(2, tree.MetaData.ReadByte());
            Assert.AreEqual(3, tree.MetaData.ReadByte());
            Assert.AreEqual(5, tree.MetaData.ReadByte());
            Assert.AreEqual(7, tree.MetaData.ReadByte());
            Assert.AreEqual(11, tree.MetaData.ReadByte());

            Assert.IsNotNull(tree.Children.FirstOrDefault(c => c == child1));
            Assert.IsNotNull(tree.Children.FirstOrDefault(c => c == child2));
            Assert.AreEqual(parent, tree.Parent);
        }

        [TestMethod]
        public void LinqFriendlyConstructor()
        {
            Element child1 = new Element();
            Element child2 = new Element();
            Element parent = new Element();
            byte[] data = new byte[] { 2, 3, 5, 7, 11 };

            Element tree = new Element(new MemoryStream(data), new MemoryStream(data),
                from child in new byte[] { 1, 1, 2, 3, 5, 8 } select new Element(new MemoryStream(new byte[] { child, child, child }), new MemoryStream(new byte[] { 5 }))
                )
            {
                Parent = parent
            };

            Assert.AreEqual(2, tree.Data.ReadByte());
            Assert.AreEqual(3, tree.Data.ReadByte());
            Assert.AreEqual(5, tree.Data.ReadByte());
            Assert.AreEqual(7, tree.Data.ReadByte());
            Assert.AreEqual(11, tree.Data.ReadByte());

            Assert.AreEqual(5, tree.Children.ToArray()[4].Data.ReadByte());
            Assert.AreEqual(8, tree.Children.ToArray()[5].Data.ReadByte());
            Assert.AreEqual(parent, tree.Parent);
        }

        [TestMethod]
        [ExpectedException(typeof(PromptuariumException))]
        public void Indexer()
        {
            Element child1 = new Element();
            Element child2 = new Element();
            Element parent = new Element();
            byte[] data = new byte[] { 2, 3, 5, 7, 11 };

            Element tree = new Element(new MemoryStream(data), null, child1, child2)
            {
                Parent = parent
            };

            Assert.AreEqual(2, tree.Data.ReadByte());
            Assert.AreEqual(3, tree.Data.ReadByte());
            Assert.AreEqual(5, tree.Data.ReadByte());
            Assert.AreEqual(7, tree.Data.ReadByte());
            Assert.AreEqual(11, tree.Data.ReadByte());

            Assert.AreEqual(parent, tree.Parent);
            Assert.AreEqual(child1, tree[0]);
            Assert.AreEqual(child2, tree[1]);
            Assert.AreEqual(child2, tree[2]);
        }

        [TestMethod]
        [ExpectedException(typeof(PromptuariumException))]
        public void Add()
        {
            Element child1 = new Element(new MemoryStream(new byte[] { 1 }), null);
            Element child2 = new Element(new MemoryStream(new byte[] { 2 }), null);
            Element child3 = new Element(new MemoryStream(new byte[] { 3 }), null);

            Element tree = new Element(null, null);

            tree += child1;
            tree.Add(child2, child3);

            Assert.AreEqual(null, tree.Parent);
            Assert.AreEqual(child1, tree[0]);
            Assert.AreEqual(child2, tree[1]);
            Assert.AreEqual(child3, tree[2]);

            Assert.AreEqual(3, tree.Children.Count);

            tree.Add(null);
            Assert.AreEqual(3, tree.Children.Count);

            tree.Add(child1);
            Assert.AreEqual(3, tree.Children.Count);
        }

        [TestMethod]
        public void AddLinq()
        {
            Element tree = new Element(null, null);

            tree.Add(
                new byte[] {5, 9, 3, 6, 7}.Select(n => new Element
                                                       (
                                                           new MemoryStream(new[] { n }), new MemoryStream(new[] { n }),
                                                           new Element(new MemoryStream(new byte[] {66}), null)
                                                       ))
                );

            Assert.AreEqual(null, tree.Parent);

            Assert.AreEqual(5, tree[0].Data.ReadByte());
            Assert.AreEqual(9, tree[1].Data.ReadByte());
            Assert.AreEqual(7, tree[4].Data.ReadByte());
        }

        [TestMethod]
        public void Remove()
        {
            Element tree = CreateTestTree();
            var nodes = tree.Statistics.Nodes;

            tree.Remove(tree[2]);
            tree.Remove(null);
            tree -= tree[0];

            Assert.AreEqual(6, nodes - tree.Statistics.Nodes);
        }

        [TestMethod]
        public void Detach()
        {
            Element tree = CreateTestTree();
            Element reference = tree[1];

            Element child2 = tree.Detach(tree[1]);

            Assert.AreEqual(reference, child2);
            Assert.AreEqual(240, child2.MetaData.ReadByte());
            Assert.AreEqual(3, tree.Children.Count);
        }

        [TestMethod]
        public async Task Save()
        {
            Element tree = CreateTestTree();
            MemoryStream stream = new MemoryStream();

            await tree.SaveAsync(stream).ConfigureAwait(false);
            await tree.SaveAsync("ut0.p").ConfigureAwait(false);

            Assert.AreEqual(stream.Length, new FileInfo("ut0.p").Length);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
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

            MemoryStream stream = new MemoryStream(data);

            Element tree = await Element.LoadAsync(stream).ConfigureAwait(false);

            Assert.AreEqual(0x80, tree.Data.AsByte());
            Assert.AreEqual(0x7819, tree[0].Data.AsShort());
            Assert.AreEqual(0x86197819, tree[0].Data.AsUInt());
            Assert.AreEqual((uint)0x44434241, tree[0][0].MetaData.AsUInt());
            Assert.AreEqual(true, tree[2].Data.AsBool());
            Assert.AreEqual(false, tree[2].MetaData.AsBool());
        }

        [TestMethod]
        public async Task TreeToStringTest()
        {
            Element tree = CreateTestTree();
            await tree.SaveAsync("ut2.p").ConfigureAwait(false);

            Element tree2 = await Element.LoadAsync("ut2.p").ConfigureAwait(false);

            string treeString = tree.TreeToString();
            string treeString2 = tree2.TreeToString();

            Assert.AreEqual(treeString2, treeString);
        }

        [TestMethod]
        public async Task FromInt()
        {
            Element tree = CreateTestTree();

            Element subNode = new Element
            {
                Data = Data.FromInt(0x55aa00ff)
            };

            tree.Add(subNode);

            await tree.SaveAsync("ut3a.p").ConfigureAwait(false);
            Element tree2 = await Element.LoadAsync("ut3a.p").ConfigureAwait(false);

            Assert.AreEqual(0x55aa00ff, tree[tree.Children.Count - 1].Data.AsInt());
            Assert.AreEqual(0x55aa00ff, tree2[tree2.Children.Count - 1].Data.AsInt());
        }

        [TestMethod]
        public async Task TreeToString()
        {
            Element tree = CreateTestTree();

            await tree.SaveAsync("ut3b.p").ConfigureAwait(false);
            Element tree2 = await Element.LoadAsync("ut3b.p").ConfigureAwait(false);

            string treeString = tree.TreeToString();
            string treeString2 = tree2.TreeToString();

            Assert.AreEqual(treeString2, treeString);
        }

        [TestMethod]
        public async Task Long()
        {
            Element tree = CreateTestTree();

            Element numericNode = new Element
            {
                Data = Data.FromLong(0x55aa00ff19781986)
            };

            tree[0].Add(numericNode);

            await tree.SaveAsync("ut4a.p").ConfigureAwait(false);
            Element tree2 = await Element.LoadAsync("ut4a.p").ConfigureAwait(false);

            Assert.AreEqual(0x55aa00ff19781986, tree2[0][1].Data.AsLong());
        }

        [TestMethod]
        public async Task Double()
        {
            Element tree = CreateTestTree();

            Element numericNode = new Element
            {
                MetaData = Data.FromDouble(3.14159265359)
            };

            tree[0].Add(numericNode);

            await tree.SaveAsync("ut4b.p").ConfigureAwait(false);
            Element tree2 = await Element.LoadAsync("ut4b.p").ConfigureAwait(false);

            double value = tree2[0][1].MetaData.AsDouble();
            Assert.IsTrue(value > 3.1415926 && value < 3.1415927);
        }

        [TestMethod]
        public async Task GuidTest()
        {
            Guid testGuid = Guid.NewGuid();

            Element tree = new Element();

            Element guidNode = new Element
            {
                Data = Data.FromGuid(testGuid)
            };

            tree.Add(guidNode);

            await tree.SaveAsync("ut5a.p").ConfigureAwait(false);
            Element tree2 = await Element.LoadAsync("ut5a.p").ConfigureAwait(false);

            Assert.AreEqual(0, testGuid.CompareTo(tree2[0].Data.AsGuid()));
        }

        [TestMethod]
        public async Task Decimal()
        {
            Element tree = new Element();

            Element decimalNode = new Element
            {
                MetaData = Data.FromDecimal(1.978M)
            };

            tree.Add(decimalNode);

            await tree.SaveAsync("ut5b.p").ConfigureAwait(false);
            Element tree2 = await Element.LoadAsync("ut5b.p").ConfigureAwait(false);

            Assert.AreEqual(1.978M, tree2[0].MetaData.AsDecimal());
        }

        [TestMethod]
        public async Task ByteArray()
        {
            byte[] testBytes = new byte[] {1, 1, 2, 3, 5, 8, 13, 21};

            Element tree = new Element();

            Element byteArrayNode = new Element
            {
                Data = Data.FromByteArray(testBytes)
            };

            tree.Add(byteArrayNode);

            await tree.SaveAsync("ut6.p").ConfigureAwait(false);
            Element tree2 = await Element.LoadAsync("ut6.p").ConfigureAwait(false);

            byte[] testBytes2 = tree2[0].Data.AsByteArray();

            for (int i = 0; i < testBytes.Length; i++)
            {
                Assert.AreEqual(testBytes[i], testBytes2[i]);
            }
        }

        [TestMethod]
        public async Task Unicode()
        {
            Element tree = new Element();

            Element unicodeNode = new Element
            {
                Data = Data.FromUtf32String("Hello, UTF-32LE world!"),
                MetaData = Data.FromUtf16String("Hello, UTF-16LE world!")
            };

            tree.Add(unicodeNode);

            await tree.SaveAsync("ut7.p").ConfigureAwait(false);
            Element tree2 = await Element.LoadAsync("ut7.p").ConfigureAwait(false);

            Assert.AreEqual("Hello, UTF-32LE world!", tree2[0].Data.AsUtf32String());
            Assert.AreEqual("Hello, UTF-16LE world!", tree2[0].MetaData.AsUtf16String());
        }

        [TestMethod]
        public async Task ElementTest()
        {
            Element outerTree = new Element();
            Element innerTree = new Element();

            innerTree += new Element() { Data = Data.FromInt(0x55aa0044) };
            innerTree += new Element() { MetaData = Data.FromInt(0x55aa00ff) };

            outerTree.MetaData = Data.FromShort(1000);
            outerTree.Data = await Data.FromElementAsync(innerTree).ConfigureAwait(false);

            await outerTree.SaveAsync("ut8.p").ConfigureAwait(false);

            Element outerTree2 = await Element.LoadAsync("ut8.p").ConfigureAwait(false);
            Element innerTree2 = await outerTree2.Data.AsElementAsync().ConfigureAwait(false);

            Assert.AreEqual(1000, outerTree2.MetaData.AsShort());
            Assert.AreEqual(0x55aa00ff, innerTree2[1].MetaData.AsInt());
        }

        [TestMethod]
        public async Task DateTimeTimeSpan()
        {
            Element tree = new Element();

            DateTime birthDay = new DateTime(1978, 10, 27, 8, 0, 0, 230);
            TimeSpan testTimeSpan = new TimeSpan(1978, 12, 27, 8, 239);

            tree += new Element
                {
                    Data = Data.FromDateTime(birthDay),
                    MetaData = Data.FromTimeSpan(testTimeSpan)
                };

            await tree.SaveAsync("ut9a.p").ConfigureAwait(false);
            Element tree2 = await Element.LoadAsync("ut9a.p").ConfigureAwait(false);

            Assert.AreEqual(0, birthDay.CompareTo(tree2[0].Data.AsDateTime()));
            Assert.AreEqual(0, testTimeSpan.CompareTo(tree2[0].MetaData.AsTimeSpan()));

            string treeString = tree.TreeToString();
            string tree2String = tree2.TreeToString();
            Assert.AreEqual(treeString, tree2String);
        }

        [TestMethod]
        public async Task Char()
        {
            Element tree = new Element();

            Element unicodeNode = new Element
            {
                Data = Data.FromChar('Ä'),
                MetaData = Data.FromChar('Ő')
            };

            tree.Add(unicodeNode);

            await tree.SaveAsync("ut9b.p").ConfigureAwait(false);
            Element tree2 = await Element.LoadAsync("ut9b.p").ConfigureAwait(false);

            Assert.AreEqual('Ä', tree2[0].Data.AsChar());
            Assert.AreEqual('Ő', tree2[0].MetaData.AsChar());

            string treeString = tree.TreeToString();
            string tree2String = tree2.TreeToString();
            Assert.AreEqual(treeString, tree2String);
        }

        [TestMethod]
        public async Task StringPerformance()
        {
            Element tree = new Element();

            for (int i = 0; i < 4000; ++i)
            {
                tree.Add(
                    new Element
                        {
                            Data = Data.FromUtf8String("+36702815816")
                        }
                );
            }

            await tree.SaveAsync("pt0.p").ConfigureAwait(false);
            Element tree2 = await Element.LoadAsync("pt0.p").ConfigureAwait(false);

            Assert.AreEqual(tree.TreeToString(), tree2.TreeToString());
        }

        [TestMethod]
        public async Task IntPerformance()
        {
            Element tree = new Element();

            for (int i = 0; i < 4000; ++i)
            {
                tree.Add(
                    new Element
                    {
                        Data = Data.FromInt(367028158)
                    }
                );
            }

            await tree.SaveAsync("pt1.p").ConfigureAwait(false);
            Element tree2 = await Element.LoadAsync("pt1.p").ConfigureAwait(false);
            Assert.AreEqual(tree.TreeToString(), tree2.TreeToString());
        }

        [TestMethod]
        public async Task StreamCompatibility()
        {
            using MemoryStream stream = new MemoryStream();

            Element treeA = new Element() { Data = Data.FromAsciiString("A") };

            treeA.Add(
                new Element { Data = Data.FromAsciiString("A1") },
                new Element { Data = Data.FromAsciiString("A2") }
            );

            Element treeB = new Element() { Data = Data.FromAsciiString("B") };

            treeB.Add(
                new Element { Data = Data.FromAsciiString("B1") },
                new Element { Data = Data.FromAsciiString("B2") }
            );

            await treeA.SaveAsync(stream).ConfigureAwait(false);
            await treeB.SaveAsync(stream).ConfigureAwait(false);

            stream.Position = 0;

            Element tree2A = await Element.LoadAsync(stream).ConfigureAwait(false);
            Element tree2B = await Element.LoadAsync(stream).ConfigureAwait(false);

            string sTreeA = treeA.TreeToString();
            string sTreeB = treeB.TreeToString();

            string sTree2A = tree2A.TreeToString();
            string sTree2B = tree2B.TreeToString();

            Assert.AreEqual(sTreeA, sTree2A);
            Assert.AreEqual(sTreeB, sTree2B);
        }

        [TestMethod]
        public void VarInt100()
        {
            Stream stream = Data.FromVarInt(100);

            stream.Position = 0;

            byte[] buffer = new byte[sizeof(long)];
            stream.Read(buffer, 0, sizeof(long));

            Assert.AreEqual(1, stream.Length);
            Assert.AreEqual(200, buffer[0]);
            Assert.AreEqual(100, stream.AsVarInt());
        }

        [TestMethod]
        public void VarIntMinus100()
        {
            var stream = Data.FromVarInt(-100);

            stream.Position = 0;

            var buffer = new byte[sizeof(long)];
            stream.Read(buffer, 0, sizeof(long));

            Assert.AreEqual(1, stream.Length);
            Assert.AreEqual(201, buffer[0]);
            Assert.AreEqual(-100, stream.AsVarInt());
        }

        [TestMethod]
        public void VarInt65537()
        {
            var stream = Data.FromVarInt(65537);

            stream.Position = 0;

            var buffer = new byte[sizeof(long)];
            stream.Read(buffer, 0, sizeof(long));

            Assert.AreEqual(3, stream.Length);
            Assert.AreEqual(2, buffer[2]);
            Assert.AreEqual(0, buffer[1]);
            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(65537, stream.AsVarInt());
        }

        [TestMethod]
        public void VarIntMinus65537()
        {
            var stream = Data.FromVarInt(-65537);

            stream.Position = 0;

            var buffer = new byte[sizeof(long)];
            stream.Read(buffer, 0, sizeof(long));

            Assert.AreEqual(3, stream.Length);
            Assert.AreEqual(2, buffer[2]);
            Assert.AreEqual(0, buffer[1]);
            Assert.AreEqual(3, buffer[0]);
            Assert.AreEqual(-65537, stream.AsVarInt());
        }

        [TestMethod]
        public void VarInt0()
        {
            var stream = Data.FromVarInt(0);

            stream.Position = 0;

            var buffer = new byte[sizeof(long)];
            stream.Read(buffer, 0, sizeof(long));

            Assert.AreEqual(1, stream.Length);
            Assert.AreEqual(0, buffer[0]);
            Assert.AreEqual(0, stream.AsVarInt());
        }

        [TestMethod]
        public void VarUInt100()
        {
            Stream stream = Data.FromVarInt(100);

            stream.Position = 0;

            byte[] buffer = new byte[sizeof(long)];
            stream.Read(buffer, 0, sizeof(long));

            Assert.AreEqual(1, stream.Length);
            Assert.AreEqual(200, buffer[0]);
            Assert.AreEqual(100, stream.AsVarInt());
        }

        [TestMethod]
        public void VarUInt65537()
        {
            var stream = Data.FromVarInt(65537);

            stream.Position = 0;

            var buffer = new byte[sizeof(long)];
            stream.Read(buffer, 0, sizeof(long));

            Assert.AreEqual(3, stream.Length);
            Assert.AreEqual(2, buffer[2]);
            Assert.AreEqual(0, buffer[1]);
            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(65537, stream.AsVarInt());
        }

        [TestMethod]
        public void VarUInt0()
        {
            var stream = Data.FromVarInt(0);

            stream.Position = 0;

            var buffer = new byte[sizeof(long)];
            stream.Read(buffer, 0, sizeof(long));

            Assert.AreEqual(1, stream.Length);
            Assert.AreEqual(0, buffer[0]);
            Assert.AreEqual(0, stream.AsVarInt());
        }

        [TestMethod]
        public async Task Base64EventExample()
        {
            Element tree = new Element(Data.FromUtf8String("ROOT NODE"), Data.FromUtf8String("UTF-8 STRING"),
                from i in new int[3] { 2, 3, 5 } select new Element(
                    Data.FromDateTime(DateTime.Now),
                    Data.FromUtf8String("DATETIME.NOW()"))
                );

            tree.OnDataSaving += (s, _) => s.Data = Data.FromUtf8String("ROOT NODE MODIFIED");
            tree.OnDataSaved += (s, _) => s.Data = Data.FromUtf8String("ROOT NODE SAVED");

            using (StreamWriter fs = new StreamWriter("base64.p"))
            {
                using MemoryStream ms = new MemoryStream();
                await tree.SaveAsync(ms).ConfigureAwait(false);
                fs.Write(Convert.ToBase64String(ms.AsByteArray()));
            }

            Assert.AreEqual("ROOT NODE SAVED", tree.Data.AsUtf8String());
        }

        [TestMethod]
        public async Task Events()
        {
            // Extra null nodes are not added to the child list
            Element tree = new Element(Data.FromUtf8String("ROOT"), null,
                new Element(Data.FromUtf8String("NODE1"), null, null, null),
                new Element(Data.FromUtf8String("NODE2"), null, null),
                new Element(Data.FromUtf8String("NODE3"), null)
                );

            int savingEventFired = 0;
            int savedEventFired = 0;

            tree.OnDataSaving += (_, __) => savingEventFired++;
            tree.OnDataSaved += (_, __) => savedEventFired++;

            await tree.SaveAsync("ut10.p").ConfigureAwait(false);

            List<string> loadingNodes = new List<string>();
            List<string> loadedNodes = new List<string>();

            Element.OnDataLoading += (s, _) => loadingNodes.Add(s.Data.AsUtf8String());
            Element.OnDataLoaded += (s, _) => loadedNodes.Add(s.Data.AsUtf8String());

            Element tree2 = await Element.LoadAsync("ut10.p").ConfigureAwait(false);

            Assert.AreEqual(1, savingEventFired);
            Assert.AreEqual(1, savedEventFired);
            Assert.AreEqual(4, loadingNodes.Count);
            Assert.AreEqual(4, loadedNodes.Count);

            Assert.AreEqual("ROOT", loadedNodes[0]);
            Assert.AreEqual("NODE1", loadedNodes[1]);
            Assert.AreEqual("NODE2", loadedNodes[2]);
            Assert.AreEqual("NODE3", loadedNodes[3]);

            Assert.AreEqual(string.Empty, loadingNodes[0]);
            Assert.AreEqual(string.Empty, loadingNodes[1]);
            Assert.AreEqual(string.Empty, loadingNodes[2]);
            Assert.AreEqual(string.Empty, loadingNodes[3]);
        }

        [TestMethod]
        public async Task CleanupStream()
        {
            Element tree = new Element() { MetaData = Data.FromAsciiString("root") };
            Element child1 = new Element() { MetaData = Data.FromAsciiString("child1") };
            Element child2 = new Element() { MetaData = Data.FromAsciiString("child2") };
            Element child3 = new Element() { MetaData = Data.FromAsciiString("child3") };

            child2 += child3;
            child1 += child2;
            tree += child1;

            MemoryStream ms = new MemoryStream();
            await tree.SaveAsync(ms).ConfigureAwait(false);

            Assert.AreEqual(27, ms.Length);
        }

        [TestMethod]
        public async Task EmptyNode()
        {
            Element tree = new Element();

            await tree.SaveAsync("empty.p").ConfigureAwait(false);

            Assert.AreEqual(1, new FileInfo("empty.p").Length);
        }

        [TestMethod]
        public void Statistics()
        {
            Element tree = new Element(Data.FromInt(0x0), null);

            Element ch_A = new Element(Data.FromInt(0x100), null);
            Element ch_B = new Element(Data.FromInt(0x200), null);
            Element ch_C = new Element(Data.FromInt(0x300), null);

            tree.Add(ch_A, ch_B, ch_C);

            Element ch_A_1 = new Element(Data.FromInt(0x110), null);
            Element ch_A_2 = new Element(Data.FromInt(0x120), null);

            ch_A.Add(ch_A_1, ch_A_2);

            Element ch_C_1 = new Element(Data.FromInt(0x310), null);
            Element ch_C_2 = new Element(Data.FromInt(0x320), null);
            Element ch_C_3 = new Element(Data.FromInt(0x330), null);
            Element ch_C_4 = new Element(Data.FromInt(0x340), Data.FromAsciiString("123456789"));

            ch_C.Add(ch_C_1, ch_C_2, ch_C_3, ch_C_4);

            Element ch_C_3_I = new Element(Data.FromInt(0x331), null);

            ch_C_3.Add(ch_C_3_I);

            Statistics stats = tree.Statistics;

            Assert.AreEqual(4, stats.Depth);
            Assert.AreEqual(11, stats.Nodes);
            Assert.AreEqual(0, stats.MinChildren);
            Assert.AreEqual(4, stats.MaxChildren);
            Assert.AreEqual(4, stats.LongestData);
            Assert.AreEqual(9, stats.LongestMetaData);
            Assert.AreEqual(0, stats.ShortestData);
            Assert.AreEqual(0, stats.ShortestMetaData);
            Assert.AreEqual(10, stats.NodesWithData);
            Assert.AreEqual(1, stats.NodesWithDataAndMetaData);
            Assert.AreEqual(7, stats.NodesWithoutChildren);
        }

        [TestMethod]
        public async Task Base64Test()
        {
            Element tree1 = new Element(Data.FromInt(0x0), null);

            Element ch_A = new Element(Data.FromInt(0x100), null);
            Element ch_B = new Element(Data.FromInt(0x200), null);
            Element ch_C = new Element(Data.FromInt(0x300), null);

            tree1.Add(ch_A, ch_B, ch_C);

            Element ch_A_1 = new Element(Data.FromInt(0x110), null);
            Element ch_A_2 = new Element(Data.FromInt(0x120), null);

            ch_A.Add(ch_A_1, ch_A_2);

            Element ch_C_1 = new Element(Data.FromInt(0x310), null);
            Element ch_C_2 = new Element(Data.FromInt(0x320), null);
            Element ch_C_3 = new Element(Data.FromInt(0x330), null);
            Element ch_C_4 = new Element(Data.FromInt(0x340), Data.FromAsciiString("123456789"));

            ch_C.Add(ch_C_1, ch_C_2, ch_C_3, ch_C_4);

            Element ch_C_3_I = new Element(Data.FromInt(0x331), null);

            ch_C_3.Add(ch_C_3_I);

            string base64String = await tree1.ToBase64StringAsync().ConfigureAwait(false);

            Element tree2 = await Element.FromBase64StringAsync(base64String).ConfigureAwait(false);

            Assert.AreEqual(tree1.TreeToString(), tree2.TreeToString());
        }

        #region Private methods
        private Element CreateTestTree()
        {
            byte[] longData = new byte[150];
            byte[] veryLongData = new byte[7 * 512*1024 + 5 * 16 + 2];

            longData[0] = 0xf0;
            veryLongData[0] = 0xf1;
            veryLongData[10] = 0xf2;

            Element node = new Element
                (
                    new MemoryStream(new byte[] {128, 96}), null,

                    new Element(null, new MemoryStream(new byte[] {5, 6} )),
                    new Element(null, new MemoryStream(longData )),
                    new Element(null, new MemoryStream(new byte[] {8, 8} ),
                        new Element(new MemoryStream(longData), null,
                            new Element(new MemoryStream(new byte[] {18, 28}), null,
                                new Element(new MemoryStream(new byte[] {1, 7}), null)
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
                new MemoryStream(veryLongData),
                new MemoryStream(longData))
            );

            return node;
        }
        #endregion
    }
}
