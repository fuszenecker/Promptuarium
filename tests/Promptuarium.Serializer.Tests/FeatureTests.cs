using Promptuarium;
using Promptuarium.Serializer;

namespace Promptuarium.Serializer.Tests;

/// <summary>Layering, constructors, attributes, options, streams and the values that can not be written.</summary>
[TestClass]
public sealed class FeatureTests
{
    public sealed class Settings
    {
        public Audio Audio { get; set; } = new();
        public Dictionary<string, string> CharacterSets { get; set; } = [];
        public List<string> Recent { get; set; } = [];
        public Audio ReadOnlyAudio { get; } = new();
    }

    public sealed class Audio
    {
        public int SampleRate { get; set; } = 44100;
        public double Frequency { get; set; } = 600;
        public string Device { get; set; } = "default";
    }

    public sealed record Station(string Call, double Frequency, string Mode = "CW");

    public sealed record Log(string Owner, IReadOnlyList<Station> Stations)
    {
        public string Comment { get; set; } = "";
    }

    public sealed class Annotated
    {
        [PromptuariumName("sample-rate")]
        public int SampleRate { get; set; }

        [PromptuariumIgnore]
        public string Secret { get; set; } = "hidden";

        public int Computed => SampleRate * 2;
    }

    public sealed class Node
    {
        public Node? Next { get; set; }
    }

    // ---------------------------------------------------------------- layering

    [TestMethod]
    public void Populate_LayersAFileOnTopOfTheDefaults()
    {
        var defaults = new Settings
        {
            Audio = new Audio { SampleRate = 44100, Frequency = 600, Device = "default" },
            CharacterSets = new() { ["Letters"] = "ABC", ["Digits"] = "123" },
            Recent = ["a", "b", "c"],
        };

        Element user = Nodes.Root(
            Nodes.Object("audio", Nodes.Member("FREQUENCY", 0x63, Data.FromDouble(700)), Nodes.Member("Device", 0x00)),
            Nodes.Object("CharacterSets", Nodes.Member("Mine", 0x80, Data.FromUtf8String("XYZ")), Nodes.Member("Digits", 0x80, Data.FromUtf8String("0"))),
            Nodes.Member("Recent", 0x21));

        PromptuariumSerializer.Populate(user, defaults);

        Assert.AreEqual(44100, defaults.Audio.SampleRate, "a member that is not in the file is kept");
        Assert.AreEqual(700, defaults.Audio.Frequency, "names match regardless of case");
        Assert.AreEqual("default", defaults.Audio.Device, "NULL keeps the value");
        Assert.AreEqual("ABC", defaults.CharacterSets["Letters"], "dictionaries merge member by member");
        Assert.AreEqual("0", defaults.CharacterSets["Digits"]);
        Assert.AreEqual("XYZ", defaults.CharacterSets["Mine"]);
        Assert.AreEqual(0, defaults.Recent.Count, "arrays are replaced");
    }

    [TestMethod]
    public void Populate_FillsAGetOnlyObjectInPlace()
    {
        var settings = new Settings();
        Element file = Nodes.Root(Nodes.Object("ReadOnlyAudio", Nodes.Member("SampleRate", 0x63, Data.FromDouble(8000))));

        PromptuariumSerializer.Populate(file.ToBytes(), settings);

        Assert.AreEqual(8000, settings.ReadOnlyAudio.SampleRate);
    }

    [TestMethod]
    public void Populate_ValueType_IsRejected()
    {
        Assert.ThrowsExactly<ArgumentException>(() => PromptuariumSerializer.Populate(Nodes.Root(), (object)42));
    }

    // ---------------------------------------------------------------- constructors

    [TestMethod]
    public void Record_IsCreatedThroughItsConstructor()
    {
        var log = new Log("HA8LHS", [new Station("DL1ABC", 7.012), new Station("OK2XYZ", 14.05, "SSB")]) { Comment = "contest" };

        Log copy = PromptuariumSerializer.Deserialize<Log>(PromptuariumSerializer.Serialize(log));

        Assert.AreEqual("HA8LHS", copy.Owner);
        Assert.AreEqual("contest", copy.Comment, "a settable member that is no parameter is set after the constructor");
        CollectionAssert.AreEqual(log.Stations.ToList(), copy.Stations.ToList());
    }

    [TestMethod]
    public void Record_MissingParameter_GetsItsDefault()
    {
        Element root = Nodes.Root(Nodes.Member("Call", 0x80, Data.FromUtf8String("DL1ABC")));

        Station station = PromptuariumSerializer.Deserialize<Station>(root);

        Assert.AreEqual(new Station("DL1ABC", 0, "CW"), station);
    }

    // ---------------------------------------------------------------- attributes

    [TestMethod]
    public void Attributes_RenameAndIgnoreMembers()
    {
        Element root = PromptuariumSerializer.ToElement(new Annotated { SampleRate = 8000, Secret = "s3cr3t" });
        string[] names = [.. root.Children.Select(member => member.Data.AsUtf8String())];

        CollectionAssert.AreEqual(new[] { "sample-rate", "Computed" }, names);

        Annotated copy = PromptuariumSerializer.Deserialize<Annotated>(root);
        Assert.AreEqual(8000, copy.SampleRate);
        Assert.AreEqual("hidden", copy.Secret);
        Assert.AreEqual(16000, copy.Computed, "a computed member is written, and ignored when read");
    }

    // ---------------------------------------------------------------- options

    [TestMethod]
    public void Options_CustomSignature_IsWrittenAndRequired()
    {
        var options = new PromptuariumSerializerOptions { Signature = "PRMT:TEST1-LE" };

        byte[] bytes = PromptuariumSerializer.Serialize(new Audio(), options);

        Assert.AreEqual("PRMT:TEST1-LE", Element.FromBytes(bytes).MetaData.AsAsciiString());
        Assert.AreEqual(44100, PromptuariumSerializer.Deserialize<Audio>(bytes, options).SampleRate);
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Audio>(bytes));
    }

    [TestMethod]
    public void Options_UnexpectedSignature_GetsTheRootMetadata()
    {
        byte[] bytes = PromptuariumSerializer.Serialize(new Audio { SampleRate = 8000 }, new() { Signature = "PRMT:TEST1-LE" });
        var seen = new List<string>();
        var options = new PromptuariumSerializerOptions
        {
            UnexpectedSignature = signature =>
            {
                seen.Add(signature);
                return SignatureAction.Accept;
            },
        };

        Assert.AreEqual(8000, PromptuariumSerializer.Deserialize<Audio>(bytes, options).SampleRate);
        PromptuariumSerializer.Deserialize<Audio>(PromptuariumSerializer.Serialize(new Audio()), options);
        CollectionAssert.AreEqual(new[] { "PRMT:TEST1-LE" }, seen, "a file with the signature does not ask");
    }

    [TestMethod]
    public void Options_UnexpectedSignature_Skip_ReadsNoMembers()
    {
        byte[] bytes = PromptuariumSerializer.Serialize(new Audio { SampleRate = 8000, Device = "hw:1" }, new() { Signature = "PRMT:TEST1-LE" });
        var options = new PromptuariumSerializerOptions { UnexpectedSignature = _ => SignatureAction.Skip };
        var target = new Audio { Device = "mine" };

        PromptuariumSerializer.Populate(bytes, target, options);

        Assert.AreEqual(44100, PromptuariumSerializer.Deserialize<Audio>(bytes, options).SampleRate);
        Assert.AreEqual("CW", PromptuariumSerializer.Deserialize<Station>(bytes, options).Mode, "a record gets its default values");
        Assert.AreEqual("mine", target.Device);
        Assert.AreEqual(44100, target.SampleRate);
    }

    [TestMethod]
    public void Options_UnexpectedSignature_RejectOrThrow()
    {
        byte[] bytes = PromptuariumSerializer.Serialize(new Audio(), new() { Signature = "PRMT:TEST1-LE" });

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Audio>(bytes, new() { UnexpectedSignature = _ => SignatureAction.Reject }));
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Deserialize<Audio>(bytes, new() { UnexpectedSignature = _ => throw new NotSupportedException() }));
    }

    [TestMethod]
    public void Options_UnexpectedSignature_IsAskedForARootWithData()
    {
        var root = new Element(Data.FromByte(1), Data.FromAsciiString(PromptuariumSerializerOptions.DefaultSignature));
        var options = new PromptuariumSerializerOptions { UnexpectedSignature = _ => SignatureAction.Accept };

        Assert.AreEqual(0, PromptuariumSerializer.Deserialize<Dictionary<string, object?>>(root, options).Count);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("PRMT:THIS-IS-TOO-LONG")]
    [DataRow("PRMT:ÁRVÍZ")]
    public void Options_InvalidSignature_IsRejected(string signature)
    {
        Assert.ThrowsExactly<ArgumentException>(() => new PromptuariumSerializerOptions { Signature = signature });
    }

    [TestMethod]
    public void Options_CaseSensitiveNames_IgnoreOtherCases()
    {
        var options = new PromptuariumSerializerOptions { PropertyNameCaseInsensitive = false };
        Element root = Nodes.Root(Nodes.Member("samplerate", 0x63, Data.FromDouble(8000)), Nodes.Member("Frequency", 0x63, Data.FromDouble(700)));

        Audio audio = PromptuariumSerializer.Deserialize<Audio>(root, options);

        Assert.AreEqual(44100, audio.SampleRate);
        Assert.AreEqual(700, audio.Frequency);
    }

    [TestMethod]
    public void Options_MaxDepth_StopsCyclesWhenWriting()
    {
        var node = new Node();
        node.Next = node;

        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(node));
    }

    [TestMethod]
    public void Options_MaxDepth_LimitsReading()
    {
        var chain = new Node();

        for (int i = 0; i < 10; i++)
        {
            chain = new Node { Next = chain };
        }

        byte[] bytes = PromptuariumSerializer.Serialize(chain);

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Node>(bytes, new PromptuariumSerializerOptions { MaxDepth = 5 }));
    }

    [TestMethod]
    public void Options_LoadLimits_AreApplied()
    {
        byte[] bytes = PromptuariumSerializer.Serialize(new Settings());
        var options = new PromptuariumSerializerOptions { LoadOptions = new LoadOptions { MaxNodes = 3 } };

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Settings>(bytes, options));
    }

    // ---------------------------------------------------------------- streams

    [TestMethod]
    public async Task Streams_RoundTrip()
    {
        using var stream = new MemoryStream();

        await PromptuariumSerializer.SerializeAsync(stream, new Audio { Frequency = 523.25 });
        stream.Position = 0;
        Audio copy = await PromptuariumSerializer.DeserializeAsync<Audio>(stream);

        Assert.AreEqual(523.25, copy.Frequency);
    }

    [TestMethod]
    public async Task Streams_NotATree_IsRejected()
    {
        using var stream = new MemoryStream("not a tree"u8.ToArray());

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => PromptuariumSerializer.DeserializeAsync<Audio>(stream));
    }

    // ---------------------------------------------------------------- what can not be written

    [TestMethod]
    public void Serialize_ScalarOrArrayRoot_IsRejected()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(42));
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new List<int> { 1 }));
    }

    [TestMethod]
    public void Serialize_NonFiniteNumber_IsRejected()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new Audio { Frequency = double.NaN }));
    }

    [TestMethod]
    public void Serialize_DictionaryWithNonStringKeys_IsRejected()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new Dictionary<int, string> { [1] = "one" }));
    }

    [TestMethod]
    public void Serialize_NumberTypeWithoutAMapping_IsRejected()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new Dictionary<string, object> { ["key"] = Int128.MaxValue }));
    }
}
