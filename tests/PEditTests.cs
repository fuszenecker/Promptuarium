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
        public async Task DumpAsJson()
        {
            await Ok("new", file).ConfigureAwait(false);
            await Ok("add", file, "/", "--data", "ascii:A").ConfigureAwait(false);
            await Ok("add", file, "/0", "--metadata", "hex:FF00").ConfigureAwait(false);
            await Ok("add", file, "/").ConfigureAwait(false);

            using var json = JsonDocument.Parse(await Ok("dump", file, "--json").ConfigureAwait(false));
            JsonElement[] nodes = json.RootElement.GetProperty("nodes").EnumerateArray().ToArray();

            CollectionAssert.AreEqual(new[] { "/", "/0", "/0/0", "/1" }, nodes.Select(n => n.GetProperty("path").GetString()).ToArray());
            Assert.AreEqual(JsonValueKind.Null, nodes[0].GetProperty("data").ValueKind);
            Assert.AreEqual("A", nodes[1].GetProperty("data").GetProperty("utf8").GetString());
            Assert.AreEqual("FF00", nodes[2].GetProperty("metadata").GetProperty("hex").GetString());
            Assert.AreEqual(JsonValueKind.Null, nodes[2].GetProperty("metadata").GetProperty("utf8").ValueKind);
            Assert.AreEqual(2, nodes[2].GetProperty("depth").GetInt32());
            Assert.AreEqual(0, nodes[3].GetProperty("children").GetInt32());
        }

        [TestMethod]
        public async Task DumpOfASubtree()
        {
            await Ok("new", file).ConfigureAwait(false);
            await Ok("add", file, "/", "--data", "ascii:A").ConfigureAwait(false);
            await Ok("add", file, "/0", "--data", "ascii:B").ConfigureAwait(false);
            await Ok("add", file, "/", "--data", "ascii:C").ConfigureAwait(false);

            string dump = await Ok("dump", file, "--path", "/0").ConfigureAwait(false);

            StringAssert.Contains(dump, "/0/0");
            Assert.IsFalse(dump.Contains("/1", StringComparison.Ordinal));
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
