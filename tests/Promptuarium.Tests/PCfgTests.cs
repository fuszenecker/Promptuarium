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
        // The example of SERIALIZER_FORMAT.md, section 8
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

        // The test vector of SERIALIZER_FORMAT.md, section 9.1
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
        public async Task Version()
        {
            foreach (string command in new[] { "version", "--version", "-V" })
            {
                StringAssert.StartsWith(await Ok(command).ConfigureAwait(false), "pcfg 8.");
            }
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
                string.Concat((await Ok("export", file, "--json").ConfigureAwait(false)).Where(c => !char.IsWhiteSpace(c))));
        }

        [TestMethod]
        public async Task ExportIsYamlByDefault()
        {
            await Ok("import", file, json).ConfigureAwait(false);
            await Ok("set", file, "data", "base64:3q2+7w==").ConfigureAwait(false);
            await Ok("comment", file, "key1", "the first\nkey").ConfigureAwait(false);

            Assert.AreEqual(
                """
                # the first
                # key
                key1: value1
                key2: 1.5
                key3: true
                key4:
                  - value41
                  - value42
                key5:
                  key51: value51
                key6: null
                data: !!binary 3q2+7w==

                """.ReplaceLineEndings("\n"),
                (await Ok("export", file).ConfigureAwait(false)).ReplaceLineEndings("\n"));
        }

        [TestMethod]
        public async Task ExportToAJsonFileIsJson()
        {
            await Ok("import", file, json).ConfigureAwait(false);
            string target = Path.Combine(directory, "out.json");
            await Ok("export", file, "key5", "--output", target).ConfigureAwait(false);

            Assert.AreEqual("""{"key51":"value51"}""", string.Concat((await File.ReadAllTextAsync(target).ConfigureAwait(false)).Where(c => !char.IsWhiteSpace(c))));
        }

        [TestMethod]
        public async Task YamlKeepsTheFileByteForByte()
        {
            await Ok("new", file, "--comment", "the file").ConfigureAwait(false);

            foreach (string[] value in new[]
            {
                new[] { "text", "string:plain text" },
                new[] { "looksLikeANumber", "string:0123" },
                new[] { "hexText", "string:0x10" },
                new[] { "word", "string:no" },
                new[] { "nullText", "string:null" },
                new[] { "tilde", "string:~" },
                new[] { "empty", "string:" },
                new[] { "special", "string:a: b # c\n\"d\"\t\u0001 árvíz 😀" },
                new[] { "number", "number:-1.5e300" },
                new[] { "yes", "bool:false" },
                new[] { "nothing", "null" },
                new[] { "blob", "hex:00FF" },
                new[] { "emptyBlob", "hex:" },
                new[] { "emptyObject", "{}" },
                new[] { "emptyArray", "[]" },
                new[] { "123", "string:numeric name" },
                new[] { "nested:list", "yaml:[1, [two], {three: 3}]" },
            })
            {
                await Ok("set", file, value[0], value[1]).ConfigureAwait(false);
            }

            await Ok("comment", file, "number", "about the number").ConfigureAwait(false);
            await Ok("comment", file, "nested", "about nested").ConfigureAwait(false);
            await Ok("comment", file, "nested:list", "the first member").ConfigureAwait(false);
            await Ok("comment", file, "nested:list:0", "the first item").ConfigureAwait(false);
            await Ok("comment", file, "nested:list:2", "the object").ConfigureAwait(false);
            await Ok("comment", file, "nested:list:2:three", "the first key of an item").ConfigureAwait(false);

            string yaml = Path.Combine(directory, "app.yaml");
            string copy = Path.Combine(directory, "copy.cfg");
            await Ok("export", file, "--output", yaml).ConfigureAwait(false);
            await Ok("import", copy, yaml).ConfigureAwait(false);

            CollectionAssert.AreEqual(await File.ReadAllBytesAsync(file).ConfigureAwait(false), await File.ReadAllBytesAsync(copy).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task ImportReadsTheCoreSchemaAndTheComments()
        {
            string yaml = Path.Combine(directory, "app.yml");
            await File.WriteAllTextAsync(yaml, """
                # the document
                ---
                # before a
                # on two lines
                a: 1 # after a

                # before b
                b: # after the key
                  # before c
                  c: 0x10
                d: [yes, ~, 0o17, TRUE, .5, "1", !!str 2, !!float 3]
                e: |
                  block
                # before f
                f:
                """).ConfigureAwait(false);

            await Ok("import", file, yaml).ConfigureAwait(false);
            string shown = await Ok("show", file).ConfigureAwait(false);

            StringAssert.Contains(shown, "# the document");
            StringAssert.Contains(shown, "# before a\n# on two lines\n# after a\na = 1".ReplaceLineEndings());
            StringAssert.Contains(shown, "# before b\n# after the key\nb {\n  # before c\n  c = 16".ReplaceLineEndings());
            StringAssert.Contains(shown, "# before f\nf = null".ReplaceLineEndings());
            Assert.AreEqual(
                """["yes",null,15,true,0.5,"1","2",3]""",
                string.Concat((await Ok("get", file, "d", "--json").ConfigureAwait(false)).Where(c => !char.IsWhiteSpace(c))));
            Assert.AreEqual("block\n", (await Ok("get", file, "e").ConfigureAwait(false)).ReplaceLineEndings("\n")[..^1]);
            Assert.AreEqual(ExitCodes.Success, (await Run("check", file, "--strict").ConfigureAwait(false)).ExitCode);
        }

        [TestMethod]
        [DataRow("a: 1\n  b: 2\n", "not valid YAML")]
        [DataRow("- a\n", "is not an object")]
        [DataRow("a: 1\n---\nb: 2\n", "more than one document")]
        [DataRow("a: &x 1\nb: *x\n", "aliases")]
        [DataRow("? [k]\n: v\n", "not a scalar")]
        [DataRow("a: .inf\n", "not a finite double")]
        [DataRow("a: 1e999\n", "not a finite double")]
        [DataRow("a: !custom b\n", "not supported")]
        [DataRow("a: !!binary '*'\n", "can not be read")]
        [DataRow("# only a comment\n", "no document")]
        [DataRow("", "no document")]
        public async Task ImportRefusesWhatIsNotAConfiguration(string text, string message)
        {
            string yaml = Path.Combine(directory, "bad.yaml");
            await File.WriteAllTextAsync(yaml, text).ConfigureAwait(false);

            (int exitCode, _, string error) = await Run("import", file, yaml).ConfigureAwait(false);

            Assert.AreEqual(ExitCodes.Failed, exitCode);
            StringAssert.Contains(error, message);
            Assert.IsFalse(File.Exists(file));
        }

        [TestMethod]
        public async Task ImportRefusesATooDeepDocument()
        {
            string yaml = Path.Combine(directory, "deep.yaml");
            await File.WriteAllTextAsync(yaml, "a: " + new string('[', 300) + new string(']', 300)).ConfigureAwait(false);

            Assert.AreEqual(ExitCodes.Failed, (await Run("import", file, yaml).ConfigureAwait(false)).ExitCode);
        }

        [TestMethod]
        public async Task GetPrintsYaml()
        {
            await Ok("import", file, json).ConfigureAwait(false);

            Assert.AreEqual("key51: value51", (await Ok("get", file, "key5", "--yaml").ConfigureAwait(false)).Trim());
            Assert.AreEqual(ExitCodes.Usage, (await Run("get", file, "key5", "--yaml", "--json").ConfigureAwait(false)).ExitCode);
        }

        [TestMethod]
        public async Task CheckReportsEveryProblem()
        {
            // A reserved type (FLOAT8), a BOOLEAN that is not 00 or 01, a repeated name and a stray TYPE node
            Element root = new ElementBuilder().WithSignature(System.Text.Encoding.ASCII.GetBytes("PRMT:CFG1-LE")).Build();
            root.Add(
                Holder("a", 0x60, new byte[] { 1 }),
                Holder("b", 0x01, new byte[] { 2 }),
                Holder("c", 0x01, new byte[] { 1 }),
                Holder("c", 0x01, new byte[] { 0 }),
                new Element(new byte[] { 0x80 }, new byte[] { 0x01 }));
            await root.SaveAsync(file).ConfigureAwait(false);

            (int exitCode, string output, string error) = await Run("check", file).ConfigureAwait(false);

            Assert.AreEqual(ExitCodes.Failed, exitCode);
            StringAssert.Contains(output, "3 errors, 1 warnings");
            StringAssert.Contains(error, "a: the type FLOAT8 (0x60) is reserved");
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

        // SERIALIZER_FORMAT.md, section 9.2: a general object of version 2 with every new kind of value
        private const string Version2Vector =
            "6c50524d543a47454e322d4c4542693821004140210" + "1c1fb2103836933322100414221" + "01c4feffffff2103836936342100414321" +
            "01c80100000000002000210384693132382100414421" + "01d0000000000000000001000000000000002103837531362100415121" +
            "01c234122103837536342100415321" + "01c8ffffffffffffffff2103836631362100416121" + "01c2003e2103836633322100416221" +
            "01c4cdcccc3d2103836e616e2100416321" + "01c8000000000000f87f2103836465632100417021" + "01c631322e3735302103836269672100417821" +
            "01c9fffffffffffffffffe2103826964210041902101d000112233445566778899aabbccddeeff210384646174652100" +
            "41d02101c42b51000021038474696d652100" + "41d12101c8c008b37ba30000002103826474210041d22101c880fde910d9c83f00" +
            "2103827473210041d32101ca809525afd0c83f003c0021038364757221004" + "1d42101c80064596df3ffffff210300";

        [TestMethod]
        public async Task ShowsTheTestVectorOfVersion2()
        {
            await File.WriteAllBytesAsync(file, Convert.FromHexString(Version2Vector)).ConfigureAwait(false);

            string shown = await Ok("show", file, "--signature", "PRMT:GEN2-LE").ConfigureAwait(false);

            string expected = string.Join(Environment.NewLine,
                "i8 = int8:-5",
                "i32 = int32:-2",
                "i64 = int64:9007199254740993",
                "i128 = int128:18446744073709551616",
                "u16 = uint16:4660",
                "u64 = uint64:18446744073709551615",
                "f16 = float16:1.5",
                "f32 = float32:0.1",
                "nan = float64:NaN",
                "dec = decimal:12.750",
                "big = bigint:-18446744073709551617",
                "id = uuid:00112233-4455-6677-8899-aabbccddeeff",
                "date = date:2026-11-22",
                "time = time:19:30:15.5",
                "dt = datetime:2026-11-22T19:30:15",
                "ts = timestamp:2026-11-22T19:30:15+01:00",
                "dur = duration:-01:30:00") + Environment.NewLine;
            Assert.AreEqual(expected, shown);
            await Ok("check", file, "--signature", "PRMT:GEN1-LE", "--strict").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TypedValuesAreSetAndTheVersionFollowsTheContent()
        {
            await Ok("new", file).ConfigureAwait(false);
            await Ok("set", file, "Name", "x").ConfigureAwait(false);
            Assert.AreEqual("PRMT:CFG1-LE", Element.FromBytes(await File.ReadAllBytesAsync(file).ConfigureAwait(false)).MetaData.AsAsciiString());

            await Ok("set", file, "Port", "int32:8080").ConfigureAwait(false);
            await Ok("set", file, "When", "timestamp:2026-11-22T19:30:15+01:00").ConfigureAwait(false);
            Assert.AreEqual("PRMT:CFG2-LE", Element.FromBytes(await File.ReadAllBytesAsync(file).ConfigureAwait(false)).MetaData.AsAsciiString());
            Assert.AreEqual("int32:8080", await Show("Port").ConfigureAwait(false));
            Assert.AreEqual("2026-11-22T19:30:15+01:00" + Environment.NewLine, await Ok("get", file, "When").ConfigureAwait(false));

            // Without a form, the type of the existing value is kept
            await Ok("set", file, "Port", "9090").ConfigureAwait(false);
            Assert.AreEqual("int32:9090", await Show("Port").ConfigureAwait(false));
            Assert.AreEqual(ExitCodes.Usage, (await Run("set", file, "Port", "x").ConfigureAwait(false)).ExitCode);
            Assert.AreEqual(ExitCodes.Usage, (await Run("set", file, "Port", "int8:300").ConfigureAwait(false)).ExitCode);

            // Without a value of version 2, the file is of version 1 again
            await Ok("delete", file, "Port").ConfigureAwait(false);
            await Ok("delete", file, "When").ConfigureAwait(false);
            Assert.AreEqual("PRMT:CFG1-LE", Element.FromBytes(await File.ReadAllBytesAsync(file).ConfigureAwait(false)).MetaData.AsAsciiString());
        }

        [TestMethod]
        public async Task TypedValuesAreExportedAndImportedByteForByte()
        {
            await File.WriteAllBytesAsync(file, Convert.FromHexString(Version2Vector)).ConfigureAwait(false);
            string yaml = Path.Combine(directory, "app.yaml");
            string copy = Path.Combine(directory, "copy.cfg");

            await Ok("export", file, "--output", yaml, "--signature", "PRMT:GEN2-LE").ConfigureAwait(false);
            await Ok("import", copy, yaml, "--signature", "PRMT:GEN2-LE").ConfigureAwait(false);

            StringAssert.Contains(await File.ReadAllTextAsync(yaml).ConfigureAwait(false), "ts: !timestamp 2026-11-22T19:30:15+01:00");
            CollectionAssert.AreEqual(await File.ReadAllBytesAsync(file).ConfigureAwait(false), await File.ReadAllBytesAsync(copy).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task TypedValuesAreExportedToJson()
        {
            await File.WriteAllBytesAsync(file, Convert.FromHexString(Version2Vector)).ConfigureAwait(false);

            string exported = await Ok("export", file, "--json", "--signature", "PRMT:GEN2-LE").ConfigureAwait(false);

            StringAssert.Contains(exported, "\"i64\": 9007199254740993,");
            StringAssert.Contains(exported, "\"big\": -18446744073709551617,");
            StringAssert.Contains(exported, "\"dec\": 12.750,");
            StringAssert.Contains(exported, "\"nan\": \"NaN\",");
            StringAssert.Contains(exported, "\"ts\": \"2026-11-22T19:30:15+01:00\",");
            StringAssert.Contains(exported, "\"id\": \"00112233-4455-6677-8899-aabbccddeeff\",");
        }

        [TestMethod]
        public async Task AFileOfVersion1WithATypeOfVersion2_IsAnError()
        {
            Element root = new ElementBuilder().WithSignature(System.Text.Encoding.ASCII.GetBytes("PRMT:CFG1-LE")).Build();
            root.Add(Holder("a", 0x42, new byte[] { 1, 0, 0, 0 }), Holder("b", 0x63, BitConverter.GetBytes(double.NaN)));
            await root.SaveAsync(file).ConfigureAwait(false);

            (int exitCode, _, string error) = await Run("check", file).ConfigureAwait(false);

            Assert.AreEqual(ExitCodes.Failed, exitCode);
            StringAssert.Contains(error, "a: the type INT32 (0x42) is of version 2, but the file is signed 'PRMT:CFG1-LE'");
            StringAssert.Contains(error, "b: the FLOAT64 value is NaN or infinite");
        }

        [TestMethod]
        public async Task InvalidTypedValues_AreErrors()
        {
            Element root = new ElementBuilder().WithSignature(System.Text.Encoding.ASCII.GetBytes("PRMT:CFG2-LE")).Build();
            root.Add(
                Holder("a", 0x42, new byte[] { 1, 0, 0 }),
                Holder("b", 0x78, new byte[] { 0x7F, 0x00 }),
                Holder("c", 0x70, System.Text.Encoding.UTF8.GetBytes("-0")),
                Holder("d", 0xD1, BitConverter.GetBytes(TimeSpan.TicksPerDay)));
            await root.SaveAsync(file).ConfigureAwait(false);

            (int exitCode, string output, string error) = await Run("check", file).ConfigureAwait(false);

            Assert.AreEqual(ExitCodes.Failed, exitCode);
            StringAssert.Contains(output, "4 errors");
            StringAssert.Contains(error, "a: the INT32 value has 3 bytes, not 4");
            StringAssert.Contains(error, "b: the BIGINT value is not in its shortest form");
            StringAssert.Contains(error, "c: the DECIMAL value \"-0\" is not in the canonical form");
            StringAssert.Contains(error, "d: the TIME value is out of a day");
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
