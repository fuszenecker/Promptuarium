using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PEdit;
using Promptuarium;

namespace PromptuariumTests
{
    /// <summary>
    /// Tests for the pedit command line tool
    /// </summary>
    [TestClass]
    public sealed class PEditTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), $"pedit-{Guid.NewGuid():N}");
        private readonly string file;

        public PEditTests()
        {
            Directory.CreateDirectory(directory);
            file = Path.Combine(directory, "tree.prm");
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        [TestMethod]
        public async Task BuildTheWorkedExample()
        {
            await Ok("new", file).ConfigureAwait(false);
            await Ok("set", file, "/", "--data", "ascii:R").ConfigureAwait(false);
            await Ok("add", file, "/", "--data", "ascii:X").ConfigureAwait(false);
            await Ok("add", file, "/0").ConfigureAwait(false);
            await Ok("add", file, "/0/0", "--data", "ascii:d").ConfigureAwait(false);
            string added = await Ok("add", file, "/", "--data", "ascii:Y", "--metadata", "ascii:m").ConfigureAwait(false);

            // See BINARY_FORMAT.md, 9.1
            Assert.AreEqual("added /1", added.Trim());
            CollectionAssert.AreEqual(
                new byte[] { 0x41, 0x52, 0x41, 0x58, 0x40, 0x41, 0x64, 0x80, 0x81, 0x59, 0x21, 0x6D, 0x00 },
                await File.ReadAllBytesAsync(file).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task NewCreatesAnEmptyRoot()
        {
            await Ok("new", file).ConfigureAwait(false);

            CollectionAssert.AreEqual(new byte[] { 0x60, 0x00 }, await File.ReadAllBytesAsync(file).ConfigureAwait(false));
            Assert.AreEqual(ExitCodes.Failed, (await Run("new", file).ConfigureAwait(false)).ExitCode);
            await Ok("new", file, "--force").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task NewWithSignature()
        {
            await Ok("new", file, "--signature", "ascii:PRMT").ConfigureAwait(false);
            await Ok("add", file, "/", "--data", "ascii:a").ConfigureAwait(false);

            // See BINARY_FORMAT.md, 10.2
            CollectionAssert.AreEqual(new byte[] { 0x64, 0x50, 0x52, 0x4D, 0x54, 0x41, 0x61, 0x00 }, await File.ReadAllBytesAsync(file).ConfigureAwait(false));
            Assert.AreEqual(ExitCodes.Usage, (await Run("new", file, "--force", "--signature", "ascii:0123456789ABCDEF").ConfigureAwait(false)).ExitCode);
        }

        [TestMethod]
        public async Task AddAtIndexSetAndDelete()
        {
            await Ok("new", file).ConfigureAwait(false);
            await Ok("add", file, "/", "--data", "ascii:A").ConfigureAwait(false);
            await Ok("add", file, "/", "--data", "ascii:C").ConfigureAwait(false);
            Assert.AreEqual("added /1", (await Ok("add", file, "/", "--index", "1", "--data", "ascii:B").ConfigureAwait(false)).Trim());
            await Ok("add", file, "/").ConfigureAwait(false);

            await Ok("set", file, "/0", "--data", "hex:41 41", "--metadata", "utf8:é").ConfigureAwait(false);
            await Ok("delete", file, "/2").ConfigureAwait(false);

            var tree = await Element.LoadAsync(file).ConfigureAwait(false);
            var expected = new Element(null, null,
                new Element(Data.FromAsciiString("AA"), Data.FromUtf8String("é")),
                new Element(Data.FromAsciiString("B"), null),
                new Element());

            Assert.IsTrue(expected.StructurallyEquals(tree));
        }

        [TestMethod]
        public async Task SetEmptyClearsTheContent()
        {
            await Ok("new", file).ConfigureAwait(false);
            await Ok("set", file, "/", "--data", "ascii:R", "--metadata", "ascii:m").ConfigureAwait(false);
            await Ok("set", file, "/", "--data", "empty").ConfigureAwait(false);

            Assert.IsTrue(new Element(null, Data.FromAsciiString("m")).StructurallyEquals(await Element.LoadAsync(file).ConfigureAwait(false)));
        }

        [TestMethod]
        public async Task OutputIntoAnotherFile()
        {
            string other = Path.Combine(directory, "other.prm");
            await Ok("new", file).ConfigureAwait(false);
            await Ok("add", file, "/", "--data", "ascii:A", "--output", other).ConfigureAwait(false);

            CollectionAssert.AreEqual(new byte[] { 0x60, 0x00 }, await File.ReadAllBytesAsync(file).ConfigureAwait(false));
            Assert.AreEqual(1, (await Element.LoadAsync(other).ConfigureAwait(false)).Children.Count);
        }

        [TestMethod]
        public async Task Get()
        {
            await Ok("new", file).ConfigureAwait(false);
            await Ok("add", file, "/", "--data", "utf8:héllo", "--metadata", "base64:AAEC").ConfigureAwait(false);

            Assert.AreEqual("68C3A96C6C6F", (await Ok("get", file, "/0", "data").ConfigureAwait(false)).Trim());
            Assert.AreEqual("héllo", (await Ok("get", file, "/0", "data", "--format", "utf8").ConfigureAwait(false)).Trim());
            Assert.AreEqual("AAEC", (await Ok("get", file, "/0", "metadata", "--format", "base64").ConfigureAwait(false)).Trim());

            string raw = Path.Combine(directory, "raw.bin");
            await Ok("get", file, "/0", "metadata", "--out", raw).ConfigureAwait(false);
            CollectionAssert.AreEqual(new byte[] { 0, 1, 2 }, await File.ReadAllBytesAsync(raw).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task ContentFromFile()
        {
            string content = Path.Combine(directory, "content.bin");
            byte[] bytes = Enumerable.Range(0, 1000).Select(i => (byte)i).ToArray();
            await File.WriteAllBytesAsync(content, bytes).ConfigureAwait(false);

            await Ok("new", file).ConfigureAwait(false);
            await Ok("set", file, "/", "--data", $"file:{content}").ConfigureAwait(false);

            CollectionAssert.AreEqual(bytes, (await Element.LoadAsync(file).ConfigureAwait(false)).Data.AsByteArray());
        }

        [TestMethod]
        public async Task DumpShowsTheMetadataFirst()
        {
            await Ok("new", file).ConfigureAwait(false);
            await Ok("add", file, "/", "--metadata", "hex:00", "--data", "ascii:rate").ConfigureAwait(false);

            string dump = await Ok("dump", file).ConfigureAwait(false);
            using var json = JsonDocument.Parse(await Ok("dump", file, "--json").ConfigureAwait(false));
            JsonElement node = json.RootElement.GetProperty("children")[0];

            StringAssert.Contains(dump, "/0  metadata(1)=00 \".\"  data(4)=72617465 \"rate\"", StringComparison.Ordinal);
            CollectionAssert.AreEqual(new[] { "path", "metadata", "data" }, node.EnumerateObject().Select(property => property.Name).ToArray());
        }

        [TestMethod]
        public async Task DumpAsJson()
        {
            await Ok("new", file).ConfigureAwait(false);
            await Ok("add", file, "/", "--data", "ascii:A").ConfigureAwait(false);
            await Ok("add", file, "/0", "--metadata", "hex:FF00").ConfigureAwait(false);
            await Ok("add", file, "/").ConfigureAwait(false);

            using var json = JsonDocument.Parse(await Ok("dump", file, "--json").ConfigureAwait(false));
            JsonElement root = json.RootElement;
            JsonElement[] children = root.GetProperty("children").EnumerateArray().ToArray();
            JsonElement grandchild = children[0].GetProperty("children")[0];

            Assert.AreEqual("/", root.GetProperty("path").GetString());
            Assert.IsFalse(root.TryGetProperty("data", out _));
            CollectionAssert.AreEqual(new[] { "/0", "/1" }, children.Select(n => n.GetProperty("path").GetString()).ToArray());
            Assert.AreEqual("A", children[0].GetProperty("data").GetProperty("utf8").GetString());
            Assert.AreEqual("/0/0", grandchild.GetProperty("path").GetString());
            Assert.AreEqual("FF00", grandchild.GetProperty("metadata").GetProperty("hex").GetString());
            Assert.AreEqual(JsonValueKind.Null, grandchild.GetProperty("metadata").GetProperty("utf8").ValueKind);
            Assert.IsFalse(children[1].TryGetProperty("children", out _));
        }

        [TestMethod]
        public async Task DumpTellsOnlyAShortenedContent()
        {
            await Ok("new", file).ConfigureAwait(false);
            await Ok("add", file, "/", "--data", "ascii:ABCD").ConfigureAwait(false);

            using var whole = JsonDocument.Parse(await Ok("dump", file, "--json").ConfigureAwait(false));
            using var shortened = JsonDocument.Parse(await Ok("dump", file, "--json", "--max-bytes", "2").ConfigureAwait(false));
            JsonElement data = shortened.RootElement.GetProperty("children")[0].GetProperty("data");

            Assert.IsFalse(whole.RootElement.GetProperty("children")[0].GetProperty("data").TryGetProperty("truncated", out _));
            Assert.IsTrue(data.GetProperty("truncated").GetBoolean());
            Assert.AreEqual(4, data.GetProperty("length").GetInt32());
            Assert.AreEqual("4142", data.GetProperty("hex").GetString());
        }

        [TestMethod]
        public async Task DumpOfADeepTree()
        {
            // Deeper than the default limits of the JSON writer and reader
            Element root = new ElementBuilder().Build();
            Element last = root;

            for (int depth = 0; depth < 1100; depth++)
            {
                var child = new ElementBuilder().Build();
                last.Add(child);
                last = child;
            }

            await root.SaveAsync(file).ConfigureAwait(false);

            string yaml = Path.Combine(directory, "deep.yaml");
            StringAssert.Contains(await Ok("dump", file, "--json").ConfigureAwait(false), "children");
            await File.WriteAllTextAsync(yaml, await Ok("dump", file, "--yaml").ConfigureAwait(false)).ConfigureAwait(false);

            byte[] before = await File.ReadAllBytesAsync(file).ConfigureAwait(false);
            await Ok("set", file, "/", "--tree", yaml).ConfigureAwait(false);
            CollectionAssert.AreEqual(before, await File.ReadAllBytesAsync(file).ConfigureAwait(false));
        }

        [TestMethod]
        [DataRow("--yaml", "part.yaml")]
        [DataRow("--json", "part.json")]
        public async Task ADumpCanBeWrittenBack(string format, string name)
        {
            string part = Path.Combine(directory, name);
            string copy = Path.Combine(directory, "copy.prm");
            await Ok("new", file).ConfigureAwait(false);
            await Ok("add", file, "/", "--metadata", "hex:00FF", "--data", "utf8:árvíz").ConfigureAwait(false);
            await Ok("add", file, "/0", "--data", "hex:FF00").ConfigureAwait(false);
            await Ok("add", file, "/0").ConfigureAwait(false);
            await Ok("add", file, "/", "--data", "utf8:a\u0000\n\"b").ConfigureAwait(false);

            await File.WriteAllTextAsync(part, await Ok("dump", file, format).ConfigureAwait(false)).ConfigureAwait(false);
            await Ok("new", copy).ConfigureAwait(false);
            await Ok("set", copy, "/", "--tree", part).ConfigureAwait(false);

            CollectionAssert.AreEqual(await File.ReadAllBytesAsync(file).ConfigureAwait(false), await File.ReadAllBytesAsync(copy).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task SetReplacesASubtree()
        {
            await Ok("new", file).ConfigureAwait(false);
            await Ok("add", file, "/", "--data", "ascii:old").ConfigureAwait(false);
            await Ok("add", file, "/0", "--data", "ascii:gone").ConfigureAwait(false);
            await Ok("add", file, "/", "--data", "ascii:next").ConfigureAwait(false);
            string part = Path.Combine(directory, "part.yaml");
            await File.WriteAllTextAsync(part, """
                # a comment
                data: {utf8: "a"}
                children:
                  - metadata: {hex: "00"}
                    data: {utf8: "hello"}
                  - data: {hex: "FF00"}
                """).ConfigureAwait(false);

            Assert.AreEqual("set /0", (await Ok("set", file, "/0", "--tree", part).ConfigureAwait(false)).Trim());

            Element root = await Element.LoadAsync(file).ConfigureAwait(false);
            Assert.AreEqual(2, root.Children.Count);
            Assert.AreEqual(2, root.Children[0].Children.Count);
            Assert.AreEqual("hello", System.Text.Encoding.UTF8.GetString(root.Children[0].Children[0].Data.Span));
            Assert.AreEqual("next", System.Text.Encoding.UTF8.GetString(root.Children[1].Data.Span));
        }

        [TestMethod]
        public async Task AddTakesASubtreeFromTheStandardInput()
        {
            await Ok("new", file).ConfigureAwait(false);
            using var input = new StringReader("""{ "data": { "utf8": "a" }, "children": [ { "data": { "hex": "41" } } ] }""");
            using var output = new StringWriter();
            using var error = new StringWriter();

            int exitCode = await Commands.RunAsync(["add", file, "/", "--tree", "-"], output, error, input).ConfigureAwait(false);

            Assert.AreEqual(ExitCodes.Success, exitCode, error.ToString());
            Assert.AreEqual(1, (await Element.LoadAsync(file).ConfigureAwait(false)).Children[0].Children.Count);
        }

        [TestMethod]
        [DataRow("data: {hex: \"41\", truncated: true}\n", "truncated")]
        [DataRow("data: {hex: \"41\", utf8: \"B\"}\n", "differ")]
        [DataRow("data: {hex: \"XY\"}\n", "invalid")]
        [DataRow("data: {length: 1}\n", "neither")]
        [DataRow("data: text\n", "mapping with hex")]
        [DataRow("unknown: 1\n", "a node is a mapping")]
        [DataRow("- a\n", "a node is a mapping")]
        [DataRow("children: [1]\n", "a node is a mapping")]
        [DataRow("data: {hex: \"41\"\n", "not valid")]
        [DataRow("data: {utf8: a}\n---\ndata: {utf8: b}\n", "more than one")]
        [DataRow("", "a node is a mapping")]
        public async Task TreeRefusesAnInvalidSubtree(string text, string message)
        {
            await Ok("new", file).ConfigureAwait(false);
            byte[] before = await File.ReadAllBytesAsync(file).ConfigureAwait(false);
            string part = Path.Combine(directory, "bad.yaml");
            await File.WriteAllTextAsync(part, text).ConfigureAwait(false);

            (int exitCode, _, string error) = await Run("set", file, "/", "--tree", part).ConfigureAwait(false);

            Assert.AreEqual(ExitCodes.Failed, exitCode);
            StringAssert.Contains(error, message);
            CollectionAssert.AreEqual(before, await File.ReadAllBytesAsync(file).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task TreeIsNotMixedWithContents()
        {
            await Ok("new", file).ConfigureAwait(false);

            Assert.AreEqual(ExitCodes.Usage, (await Run("set", file, "/", "--tree", "x", "--data", "empty").ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(ExitCodes.NotFound, (await Run("set", file, "/", "--tree", Path.Combine(directory, "missing.yaml")).ConfigureAwait(false)).ExitCode);
        }

        [TestMethod]
        public async Task DumpAndReportsAsYaml()
        {
            await Ok("new", file).ConfigureAwait(false);

            Assert.AreEqual("added: \"/0\"", (await Ok("add", file, "/", "--data", "hex:4152", "--yaml").ConfigureAwait(false)).Trim());

            string yaml = (await Ok("dump", file, "--yaml").ConfigureAwait(false)).ReplaceLineEndings("\n");

            Assert.AreEqual(
                """
                path: "/"
                children:
                  - path: "/0"
                    data: {length: 2, hex: "4152", utf8: "AR"}

                """.ReplaceLineEndings("\n"),
                yaml);
            Assert.AreEqual(ExitCodes.Usage, (await Run("dump", file, "--yaml", "--json").ConfigureAwait(false)).ExitCode);
        }

        [TestMethod]
        public async Task ChunksAsYaml()
        {
            await Ok("new", file).ConfigureAwait(false);
            string yaml = (await Ok("chunks", file, "--yaml").ConfigureAwait(false)).ReplaceLineEndings("\n");

            StringAssert.StartsWith(yaml, "chunks:\n");
            StringAssert.Contains(yaml, "trailing: 0\n");
        }

        [TestMethod]
        public async Task ChunksAsJson()
        {
            await File.WriteAllBytesAsync(file, new byte[] { 0x41, 0x52, 0x40, 0x60, 0x80, 0xA0, 0x00, 0x42 }).ConfigureAwait(false);

            using var json = JsonDocument.Parse(await Ok("chunks", file, "--json").ConfigureAwait(false));
            string?[] kinds = json.RootElement.GetProperty("chunks").EnumerateArray().Select(c => c.GetProperty("kind").GetString()).ToArray();

            CollectionAssert.AreEqual(new[] { "node", "auxiliary", "empty node", "auxiliary", "empty node", "terminator" }, kinds);
            Assert.AreEqual(7, json.RootElement.GetProperty("consumed").GetInt64());
            Assert.AreEqual(1, json.RootElement.GetProperty("trailing").GetInt64());
        }

        [TestMethod]
        public async Task ChunksOfAnInvalidStream()
        {
            await File.WriteAllBytesAsync(file, new byte[] { 0x42, 0x52 }).ConfigureAwait(false);

            (int exitCode, _, string error) = await Run("chunks", file).ConfigureAwait(false);

            Assert.AreEqual(ExitCodes.Failed, exitCode);
            StringAssert.Contains(error, "Unexpected end of stream");
        }

        [TestMethod]
        public async Task Errors()
        {
            await Ok("new", file).ConfigureAwait(false);

            Assert.AreEqual(ExitCodes.Usage, (await Run().ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(ExitCodes.Usage, (await Run("frobnicate").ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(ExitCodes.Usage, (await Run("dump", file, "--unknown").ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(ExitCodes.Usage, (await Run("add", file, "0").ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(ExitCodes.Usage, (await Run("add", file, "/", "--data", "R").ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(ExitCodes.Usage, (await Run("add", file, "/", "--data", "hex:XY").ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(ExitCodes.Usage, (await Run("add", file, "/", "--index", "5").ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(ExitCodes.Usage, (await Run("set", file, "/").ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(ExitCodes.NotFound, (await Run("dump", Path.Combine(directory, "missing.prm")).ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(ExitCodes.NotFound, (await Run("set", file, "/3", "--data", "ascii:x").ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(ExitCodes.Failed, (await Run("delete", file, "/").ConfigureAwait(false)).ExitCode);

            await File.WriteAllBytesAsync(file, new byte[] { 0x81, 0x52, 0x00 }).ConfigureAwait(false);
            Assert.AreEqual(ExitCodes.Failed, (await Run("dump", file).ConfigureAwait(false)).ExitCode);
        }

        [TestMethod]
        public async Task Help()
        {
            StringAssert.Contains(await Ok("help").ConfigureAwait(false), "pedit chunks FILE");
        }

        private static async Task<(int ExitCode, string Output, string Error)> Run(params string[] args)
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            int exitCode = await Commands.RunAsync(args, output, error).ConfigureAwait(false);
            return (exitCode, output.ToString(), error.ToString());
        }

        private static async Task<string> Ok(params string[] args)
        {
            (int exitCode, string output, string error) = await Run(args).ConfigureAwait(false);
            Assert.AreEqual(ExitCodes.Success, exitCode, $"pedit {string.Join(' ', args)}: {error}");
            return output;
        }
    }
}
