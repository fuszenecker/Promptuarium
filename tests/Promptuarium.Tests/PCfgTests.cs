using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCfg;
using Promptuarium;

namespace PromptuariumTests
{
    /// <summary>
    /// Tests for the pcfg command line tool
    /// </summary>
    [TestClass]
    public sealed class PCfgTests : IDisposable
    {
        // The example of SERIALIZER_FORMAT.md, section 6
        private const string ExampleJson = """
            {
                // comments and trailing commas are allowed
                "key1": "value1",
                "key2": 1.5,
                "key3": true,
                "key4": [ "value41", "value42" ],
                "key5": { "key51": "value51" },
                "key6": null,
            }
            """;

        // The test vector of SERIALIZER_FORMAT.md, section 7
        private const string TestVector =
            "6c50524d543a434647312d4c45446b657931210041802101c676616c7565312103846b657932210041632101c8000000000000f83f2103" +
            "846b657933210041012101c1012103846b657934210041212101e10241802101c776616c756534312103a10241802101c776616c75653432" +
            "210380846b657935210041202101c56b65793531210041802101c776616c75653531210380846b65793621004100210100";

        private readonly string directory = Path.Combine(Path.GetTempPath(), $"pcfg-{Guid.NewGuid():N}");
        private readonly string file;
        private readonly string json;

        public PCfgTests()
        {
            Directory.CreateDirectory(directory);
            file = Path.Combine(directory, "app.cfg");
            json = Path.Combine(directory, "app.json");
            File.WriteAllText(json, ExampleJson);
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        [TestMethod]
        public async Task ImportWritesTheTestVector()
        {
            await Ok("import", file, json).ConfigureAwait(false);

            Assert.AreEqual(TestVector.ToUpperInvariant(), Convert.ToHexString(await File.ReadAllBytesAsync(file).ConfigureAwait(false)));
        }

        [TestMethod]
        public async Task SetWritesTheBlobAndTheCommentOfTheTestVector()
        {
            await Ok("import", file, json).ConfigureAwait(false);
            await Ok("set", file, "key7", "hex:DE AD BE EF").ConfigureAwait(false);
            await Ok("set", file, "key8", "value8", "--comment", "the eighth key").ConfigureAwait(false);

            // Section 7: the BLOB member (18 bytes) and the commented member (37 bytes), before the final 00
            string expected = TestVector[..^2]
                + "846b6579372100" + "41f02101c4deadbeef2103"
                + "846b657938210041802101ce7468652065696768746820" + "6b6579210" + "4c676616c7565382103" + "00";
            Assert.AreEqual(expected.ToUpperInvariant(), Convert.ToHexString(await File.ReadAllBytesAsync(file).ConfigureAwait(false)));
        }

        [TestMethod]
        public async Task ShowsATreeAndFlatLines()
        {
            await Ok("import", file, json).ConfigureAwait(false);
            await Ok("comment", file, "", "The example").ConfigureAwait(false);
            await Ok("comment", file, "key5:key51", "first line\nsecond line").ConfigureAwait(false);
            await Ok("set", file, "odd name", "hex:00").ConfigureAwait(false);

            Assert.AreEqual(
                """
                # The example
                key1 = "value1"
                key2 = 1.5
                key3 = true
                key4 [
                  0 = "value41"
                  1 = "value42"
                ]
                key5 {
                  # first line
                  # second line
                  key51 = "value51"
                }
                key6 = null
                "odd name" = hex:00

                """.ReplaceLineEndings(),
                await Ok("show", file).ConfigureAwait(false));

            Assert.AreEqual(
                """
                key4:0 = "value41"
                key4:1 = "value42"

                """.ReplaceLineEndings(),
                await Ok("show", file, "key4", "--flat").ConfigureAwait(false));
        }

        [TestMethod]
        public async Task SetKeepsTheTypeOrInfersIt()
        {
            await Ok("import", file, json).ConfigureAwait(false);
            await Ok("set", file, "key1", "42").ConfigureAwait(false);
            await Ok("set", file, "key2", "8080").ConfigureAwait(false);
            await Ok("set", file, "key3", "false").ConfigureAwait(false);
            await Ok("set", file, "Port", "443").ConfigureAwait(false);
            await Ok("set", file, "Version", "1.0.3").ConfigureAwait(false);
            await Ok("set", file, "Text", "string:true").ConfigureAwait(false);

            Assert.AreEqual("\"42\"", await Show("key1").ConfigureAwait(false));
            Assert.AreEqual("8080", await Show("key2").ConfigureAwait(false));
            Assert.AreEqual("false", await Show("key3").ConfigureAwait(false));
            Assert.AreEqual("443", await Show("Port").ConfigureAwait(false));
            Assert.AreEqual("\"1.0.3\"", await Show("Version").ConfigureAwait(false));
            Assert.AreEqual("\"true\"", await Show("Text").ConfigureAwait(false));
            Assert.AreEqual(ExitCodes.Usage, (await Run("set", file, "key2", "many").ConfigureAwait(false)).ExitCode);
        }

        [TestMethod]
        public async Task SetCreatesTheObjectsOnTheWayAndKeepsTheComments()
        {
            await Ok("new", file).ConfigureAwait(false);
            await Ok("set", file, "Logging:LogLevel:Default", "Information", "--comment", "the default level").ConfigureAwait(false);
            await Ok("set", file, "Logging:LogLevel:Default", "Debug").ConfigureAwait(false);
            await Ok("set", file, "Hosts", "[]").ConfigureAwait(false);
            await Ok("set", file, "Hosts:0", "a.example").ConfigureAwait(false);
            await Ok("set", file, "Hosts:1", "json:{\"Name\": \"b\", \"Ports\": [80, 443]}").ConfigureAwait(false);

            Assert.AreEqual("Debug", (await Ok("get", file, "Logging:LogLevel:Default").ConfigureAwait(false)).Trim());
            Assert.AreEqual("443", (await Ok("get", file, "Hosts:1:Ports:1").ConfigureAwait(false)).Trim());
            StringAssert.Contains(await Ok("show", file).ConfigureAwait(false), "# the default level");

            Assert.AreEqual(ExitCodes.Failed, (await Run("set", file, "Hosts:5", "x").ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(ExitCodes.Failed, (await Run("set", file, "Hosts:0:Name", "x").ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(ExitCodes.NotFound, (await Run("get", file, "Hosts:2").ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(ExitCodes.Success, (await Run("check", file).ConfigureAwait(false)).ExitCode);
        }

        [TestMethod]
        public async Task DeleteRenameAndExport()
        {
            await Ok("import", file, json).ConfigureAwait(false);
            await Ok("delete", file, "key4:0").ConfigureAwait(false);
            await Ok("delete", file, "key6").ConfigureAwait(false);
            await Ok("rename", file, "key5", "section").ConfigureAwait(false);
            await Ok("set", file, "data", "base64:3q2+7w==").ConfigureAwait(false);

            Assert.AreEqual(ExitCodes.Failed, (await Run("rename", file, "key1", "key2").ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(
                """{"key1":"value1","key2":1.5,"key3":true,"key4":["value42"],"section":{"key51":"value51"},"data":"3q2+7w=="}""",
                string.Concat((await Ok("export", file).ConfigureAwait(false)).Where(c => !char.IsWhiteSpace(c))));
        }

        [TestMethod]
        public async Task CheckReportsEveryProblem()
        {
            // A reserved type (INT32), a BOOLEAN that is not 00 or 01, a repeated name and a stray TYPE node
            Element root = new ElementBuilder().WithSignature(System.Text.Encoding.ASCII.GetBytes("PRMT:CFG1-LE")).Build();
            root.Add(
                Holder("a", 0x42, new byte[] { 1, 0, 0, 0 }),
                Holder("b", 0x01, new byte[] { 2 }),
                Holder("c", 0x01, new byte[] { 1 }),
                Holder("c", 0x01, new byte[] { 0 }),
                new Element(new byte[] { 0x80 }, new byte[] { 0x01 }));
            await root.SaveAsync(file).ConfigureAwait(false);

            (int exitCode, string output, string error) = await Run("check", file).ConfigureAwait(false);

            Assert.AreEqual(ExitCodes.Failed, exitCode);
            StringAssert.Contains(output, "3 errors, 1 warnings");
            StringAssert.Contains(error, "a: the type INT32 (0x42) is reserved");
            StringAssert.Contains(error, "b: the BOOLEAN value 02 is not 00 or 01");
            StringAssert.Contains(error, "warning: c: the name is repeated 2 times");
            Assert.AreEqual(ExitCodes.Failed, (await Run("show", file).ConfigureAwait(false)).ExitCode);
        }

        [TestMethod]
        public async Task TheSignatureIsChecked()
        {
            await Ok("new", file, "--signature", "PRMT:GEN1-LE").ConfigureAwait(false);

            Assert.AreEqual(ExitCodes.Failed, (await Run("show", file).ConfigureAwait(false)).ExitCode);
            await Ok("show", file, "--signature", "PRMT:GEN1-LE").ConfigureAwait(false);
            Assert.AreEqual(ExitCodes.Usage, (await Run("new", file, "--force", "--signature", "PRMT:0123456789AB").ConfigureAwait(false)).ExitCode);
        }

        [TestMethod]
        public async Task KeysEscapeTheColon()
        {
            await Ok("new", file).ConfigureAwait(false);
            await Ok("set", file, @"a\:b", "1").ConfigureAwait(false);

            Assert.AreEqual(@"a\:b = 1" + Environment.NewLine, await Ok("show", file, "--flat").ConfigureAwait(false));
            Assert.AreEqual(ExitCodes.Usage, (await Run("get", file, @"a\b").ConfigureAwait(false)).ExitCode);
        }

        private static Element Holder(string name, byte type, byte[] value)
        {
            return new Element(
                System.Text.Encoding.UTF8.GetBytes(name),
                new byte[] { 0x00 },
                new Element(new byte[] { type }, new byte[] { 0x01 }),
                new Element(value, new byte[] { 0x03 }));
        }

        private async Task<string> Show(string key) => (await Ok("show", file, key).ConfigureAwait(false)).Split(" = ")[1].Trim();

        private static async Task<(int ExitCode, string Output, string Error)> Run(params string[] args)
        {
            using var input = new StringReader(string.Empty);
            using var output = new StringWriter();
            using var error = new StringWriter();
            int exitCode = await Commands.RunAsync(args, input, output, error).ConfigureAwait(false);
            return (exitCode, output.ToString(), error.ToString());
        }

        private static async Task<string> Ok(params string[] args)
        {
            (int exitCode, string output, string error) = await Run(args).ConfigureAwait(false);
            Assert.AreEqual(ExitCodes.Success, exitCode, $"pcfg {string.Join(' ', args)}: {error}");
            return output;
        }
    }
}
