// The type control is experimental: a caller opts in like this
#pragma warning disable PRMTSER001

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Numerics;
using Promptuarium;
using Promptuarium.Serializer;

namespace Promptuarium.Serializer.Tests;

/// <summary>The experimental <see cref="PromptuariumTypeAttribute"/>.</summary>
[TestClass]
public sealed class TypeAttributeTests
{
    public sealed class Declared
    {
        [PromptuariumType(PromptuariumType.Float64)]
        public int Count { get; set; }

        [PromptuariumType(PromptuariumType.Blob)]
        public byte[]? Payload { get; set; }

        [PromptuariumType(PromptuariumType.Utf8String)]
        public string? Name { get; set; }

        [PromptuariumType(PromptuariumType.Float64)]
        public object? Anything { get; set; }
    }

    public sealed class Reserved
    {
        [PromptuariumType(PromptuariumType.Float8)]
        public long Ticks { get; set; }
    }

    public sealed class Mismatch
    {
        [PromptuariumType(PromptuariumType.Boolean)]
        public string Text { get; set; } = "not a boolean";
    }

    [TestMethod]
    public void TypeCodes_AreTheOnesOfTheFormat()
    {
        Assert.AreEqual(0x43, (byte)PromptuariumType.Int64);
        Assert.AreEqual(0x63, (byte)PromptuariumType.Float64);
        Assert.AreEqual(0x80, (byte)PromptuariumType.Utf8String);
        Assert.AreEqual(0xF0, (byte)PromptuariumType.Blob);
    }

    [TestMethod]
    public void Declared_TypesThatFit_AreWrittenAndRead()
    {
        byte[] bytes = PromptuariumSerializer.Serialize(new Declared { Count = 3, Payload = [1, 2], Name = "x" });

        Declared copy = PromptuariumSerializer.Deserialize<Declared>(bytes);

        Assert.AreEqual(3, copy.Count);
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, copy.Payload);
        Assert.AreEqual("x", copy.Name);
    }

    [TestMethod]
    public void Declared_NullIsAlwaysAllowed()
    {
        byte[] bytes = PromptuariumSerializer.Serialize(new Declared { Payload = null, Name = null });

        Assert.IsNull(PromptuariumSerializer.Deserialize<Declared>(bytes).Payload);
    }

    [TestMethod]
    public void Declared_ReservedType_IsRejectedWhenWriting()
    {
        var exception = Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new Reserved { Ticks = 1 }));

        StringAssert.Contains(exception.Message, "Float8");
    }

    [TestMethod]
    public void Declared_TypeTheValueDoesNotMapTo_IsRejectedWhenWriting()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new Mismatch()));
    }

    [TestMethod]
    public void Declared_OtherTypeInTheFile_IsRejectedWhenReading()
    {
        // An object member takes any value, so only the declaration rejects the string
        Element untyped = Nodes.Root(Nodes.Member("Anything", 0x80, Data.FromUtf8String("text")));
        Element declared = Nodes.Root(Nodes.Member("Anything", 0x63, Data.FromDouble(2.5)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Declared>(untyped));
        Assert.AreEqual(2.5, PromptuariumSerializer.Deserialize<Declared>(declared).Anything);
    }

    // ---------------------------------------------------------------- Utf8String: the text of IParsable types

    public sealed class Host
    {
        [PromptuariumType(PromptuariumType.Utf8String)]
        public IPAddress? Address { get; set; }

        [PromptuariumType(PromptuariumType.Utf8String)]
        public BigInteger Big { get; set; }

        [PromptuariumType(PromptuariumType.Utf8String)]
        public int Port { get; set; }

        [PromptuariumType(PromptuariumType.Utf8String)]
        public int? Spare { get; set; }
    }

    /// <summary>Declared as text on the type: stored so everywhere.</summary>
    [PromptuariumType(PromptuariumType.Utf8String)]
    public readonly record struct Rgb(byte R, byte G, byte B) : IParsable<Rgb>
    {
        public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";

        public static Rgb Parse(string s, IFormatProvider? provider) => TryParse(s, provider, out Rgb result) ? result : throw new FormatException();

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Rgb result)
        {
            result = default;

            if (s is not { Length: 7 } || s[0] != '#' || !uint.TryParse(s.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value))
            {
                return false;
            }

            result = new Rgb((byte)(value >> 16), (byte)(value >> 8), (byte)value);
            return true;
        }
    }

    public sealed class Palette
    {
        public Rgb Background { get; set; }

        public Rgb? Accent { get; set; }

        public List<Rgb> Colors { get; set; } = [];

        public Dictionary<string, Rgb> Named { get; set; } = [];
    }

    /// <summary>Its text loses the second part, so it does not give back the same value.</summary>
    public sealed record Lossy(int First, int Second) : IParsable<Lossy>
    {
        public override string ToString() => First.ToString(CultureInfo.InvariantCulture);

        public static Lossy Parse(string s, IFormatProvider? provider) => new(int.Parse(s, provider), 0);

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Lossy result)
        {
            result = int.TryParse(s, provider, out int first) ? new Lossy(first, 0) : null;
            return result is not null;
        }
    }

    public sealed class WithLossy
    {
        [PromptuariumType(PromptuariumType.Utf8String)]
        public Lossy? Value { get; set; }
    }

    public sealed class NotParsable
    {
        [PromptuariumType(PromptuariumType.Utf8String)]
        public List<string> Names { get; set; } = [];
    }

    [PromptuariumType(PromptuariumType.Float64)]
    public sealed class DeclaredAsNumber
    {
        public int Value { get; set; }
    }

    public sealed class HoldsDeclaredAsNumber
    {
        public DeclaredAsNumber Inner { get; set; } = new();
    }

    private static string StoredText(Element root, string name)
    {
        Element holder = root.Children.Single(child => child.Data.AsUtf8String() == name);

        Assert.AreEqual(0x80, holder.Children[0].Data.Span[0], "the type of " + name);
        return holder.Children[1].Data.AsUtf8String();
    }

    [TestMethod]
    public void Utf8String_OnAParsableProperty_StoresItsText()
    {
        var host = new Host { Address = IPAddress.Parse("192.168.0.1"), Big = BigInteger.Pow(10, 30), Port = 8443 };

        Element root = PromptuariumSerializer.ToElement(host);

        Assert.AreEqual("192.168.0.1", StoredText(root, "Address"));
        Assert.AreEqual("1000000000000000000000000000000", StoredText(root, "Big"));
        Assert.AreEqual("8443", StoredText(root, "Port"));
        Assert.AreEqual(0x00, root.Children.Single(child => child.Data.AsUtf8String() == "Spare").Children[0].Data.Span[0]);
    }

    [TestMethod]
    [DataRow("192.168.0.1")]
    [DataRow("2001:db8::1")]
    [DataRow("fe80::1%3")]
    [DataRow("::ffff:192.168.0.1")]
    public void Utf8String_OnAParsableProperty_RoundTrips(string address)
    {
        var host = new Host { Address = IPAddress.Parse(address), Big = BigInteger.Parse("-123456789012345678901234567890", CultureInfo.InvariantCulture), Port = 1, Spare = 2 };

        Host copy = PromptuariumSerializer.Deserialize<Host>(PromptuariumSerializer.Serialize(host));

        Assert.AreEqual(host.Address, copy.Address);
        Assert.AreEqual(host.Big, copy.Big);
        Assert.AreEqual(1, copy.Port);
        Assert.AreEqual(2, copy.Spare);
    }

    [TestMethod]
    public void Utf8String_OnAParsableProperty_NullIsNull()
    {
        Host copy = PromptuariumSerializer.Deserialize<Host>(PromptuariumSerializer.Serialize(new Host()));

        Assert.IsNull(copy.Address);
        Assert.IsNull(copy.Spare);
    }

    [TestMethod]
    public void Utf8String_InvalidTextInTheFile_IsRejectedWhenReading()
    {
        Element root = Nodes.Root(Nodes.Member("Address", 0x80, Data.FromUtf8String("not an address")));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Host>(root));
    }

    [TestMethod]
    public void Utf8String_OtherTypeInTheFile_IsRejectedWhenReading()
    {
        Element root = Nodes.Root(Nodes.Member("Port", 0x63, Data.FromDouble(8443)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Host>(root));
    }

    [TestMethod]
    public void Utf8String_TextThatDoesNotGiveTheValueBack_IsRejectedWhenWriting()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new WithLossy { Value = new Lossy(1, 2) }));
        Assert.AreEqual(new Lossy(1, 0), PromptuariumSerializer.Deserialize<WithLossy>(PromptuariumSerializer.Serialize(new WithLossy { Value = new Lossy(1, 0) })).Value);
    }

    [TestMethod]
    public void Utf8String_OnATypeThatIsNotParsable_IsRejected()
    {
        byte[] bytes = PromptuariumSerializer.Serialize(new Dictionary<string, object> { ["Names"] = new List<string> { "a" } });

        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new NotParsable()));
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Deserialize<NotParsable>(bytes));
    }

    [TestMethod]
    public void Utf8String_OnAType_StoresItsValuesAsTextEverywhere()
    {
        var palette = new Palette
        {
            Background = new Rgb(0x10, 0x20, 0x30),
            Accent = new Rgb(0xFF, 0x80, 0x00),
            Colors = [new Rgb(1, 2, 3), new Rgb(4, 5, 6)],
            Named = new() { ["red"] = new Rgb(0xFF, 0, 0) },
        };

        Element root = PromptuariumSerializer.ToElement(palette);
        Palette copy = PromptuariumSerializer.Deserialize<Palette>(root);

        Assert.AreEqual("#102030", StoredText(root, "Background"));
        Assert.AreEqual("#FF8000", StoredText(root, "Accent"));
        Assert.AreEqual(palette.Background, copy.Background);
        Assert.AreEqual(palette.Accent, copy.Accent);
        CollectionAssert.AreEqual(palette.Colors, copy.Colors);
        Assert.AreEqual(new Rgb(0xFF, 0, 0), copy.Named["red"]);
    }

    [TestMethod]
    public void Utf8String_OnAType_CanNotBeTheRoot()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new Rgb(1, 2, 3)));
    }

    [TestMethod]
    public void OtherTypeThanUtf8String_OnAType_IsRejected()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new HoldsDeclaredAsNumber()));
    }

    // ---------------------------------------------------------------- AddTextType: text types of the options

    public sealed class Release
    {
        public Version? Version { get; set; }

        public Uri? Home { get; set; }

        public List<Version> History { get; set; } = [];

        public Dictionary<string, Uri> Links { get; set; } = [];

        [PromptuariumType(PromptuariumType.Utf8String)]
        public Version? Declared { get; set; }
    }

    private static PromptuariumSerializerOptions ReleaseOptions() => new PromptuariumSerializerOptions()
        .AddTextType<Version>(version => version.ToString(), Version.Parse)
        .AddTextType<Uri>(uri => uri.OriginalString, text => new Uri(text, UriKind.RelativeOrAbsolute));

    [TestMethod]
    public void AddTextType_StoresTheTypeAsTextEverywhere()
    {
        var release = new Release
        {
            Version = new Version(8, 2, 2),
            Home = new Uri("https://example.com/promptuarium"),
            History = [new Version(8, 0), new Version(8, 1, 0, 3)],
            Links = new() { ["docs"] = new Uri("docs/README.md", UriKind.Relative) },
            Declared = new Version(1, 0),
        };
        PromptuariumSerializerOptions options = ReleaseOptions();

        Element root = PromptuariumSerializer.ToElement(release, options);
        Release copy = PromptuariumSerializer.Deserialize<Release>(root, options);

        Assert.AreEqual("8.2.2", StoredText(root, "Version"));
        Assert.AreEqual("https://example.com/promptuarium", StoredText(root, "Home"));
        Assert.AreEqual("1.0", StoredText(root, "Declared"));
        Assert.AreEqual(release.Version, copy.Version);
        Assert.AreEqual(release.Home, copy.Home);
        CollectionAssert.AreEqual(release.History, copy.History);
        Assert.AreEqual(release.Links["docs"], copy.Links["docs"]);
        Assert.AreEqual(release.Declared, copy.Declared);
    }

    [TestMethod]
    public void AddTextType_TextThatDoesNotParse_IsRejectedWhenReading()
    {
        Element root = Nodes.Root(Nodes.Member("Version", 0x80, Data.FromUtf8String("not a version")));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Release>(root, ReleaseOptions()));
    }

    [TestMethod]
    public void AddTextType_TextThatDoesNotGiveTheValueBack_IsRejectedWhenWriting()
    {
        PromptuariumSerializerOptions options = new PromptuariumSerializerOptions()
            .AddTextType<Version>(version => version.ToString(2), Version.Parse);

        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new Release { Version = new Version(8, 2, 2) }, options));
    }

    [TestMethod]
    public void AddTextType_WinsOverTheDeclarationOnTheType()
    {
        PromptuariumSerializerOptions options = new PromptuariumSerializerOptions()
            .AddTextType<Rgb>(rgb => $"{rgb.R},{rgb.G},{rgb.B}", text => text.Split(',') is [var r, var g, var b]
                ? new Rgb(byte.Parse(r, CultureInfo.InvariantCulture), byte.Parse(g, CultureInfo.InvariantCulture), byte.Parse(b, CultureInfo.InvariantCulture))
                : throw new FormatException());

        Element root = PromptuariumSerializer.ToElement(new Palette { Background = new Rgb(1, 2, 3) }, options);

        Assert.AreEqual("1,2,3", StoredText(root, "Background"));
        Assert.AreEqual(new Rgb(1, 2, 3), PromptuariumSerializer.Deserialize<Palette>(root, options).Background);
    }

    [TestMethod]
    public void AddTextType_TypesStoredAnyway_AreRejected()
    {
        var options = new PromptuariumSerializerOptions();

        Assert.ThrowsExactly<ArgumentException>(() => options.AddTextType<int>(value => "", int.Parse));
        Assert.ThrowsExactly<ArgumentException>(() => options.AddTextType<string>(value => value, text => text));
        Assert.ThrowsExactly<ArgumentException>(() => options.AddTextType<DateTime?>(value => "", text => null));
        Assert.ThrowsExactly<ArgumentException>(() => options.AddTextType<byte[]>(value => "", text => []));
    }

    [TestMethod]
    public void AddTextType_SameTypeTwice_IsRejected()
    {
        PromptuariumSerializerOptions options = ReleaseOptions();

        Assert.ThrowsExactly<ArgumentException>(() => options.AddTextType<Version>(version => version.ToString(), Version.Parse));
    }

    [TestMethod]
    public void AddTextType_OnTheDefaultOrOnUsedOptions_IsRejected()
    {
        PromptuariumSerializerOptions options = ReleaseOptions();
        PromptuariumSerializer.Serialize(new Release(), options);

        Assert.ThrowsExactly<InvalidOperationException>(() => PromptuariumSerializerOptions.Default.AddTextType<Version>(version => version.ToString(), Version.Parse));
        Assert.ThrowsExactly<InvalidOperationException>(() => options.AddTextType<IPAddress>(address => address.ToString(), IPAddress.Parse));
    }

    [TestMethod]
    public void Utf8String_OnAPropertyOfATypeThatIsNotParsable_NeedsAddTextType()
    {
        PromptuariumSerializerOptions options = new PromptuariumSerializerOptions().ClearDefaultTypes();

        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new Release { Declared = new Version(1, 0) }, options));
    }

    // ---------------------------------------------------------------- AddBlobType: binary types of the options

    public readonly record struct Point3(short X, short Y, short Z)
    {
        public byte[] ToBytes() => [.. BitConverter.GetBytes(X), .. BitConverter.GetBytes(Y), .. BitConverter.GetBytes(Z)];

        public static Point3 FromBytes(byte[] bytes) => bytes.Length == 6
            ? new Point3(BitConverter.ToInt16(bytes, 0), BitConverter.ToInt16(bytes, 2), BitConverter.ToInt16(bytes, 4))
            : throw new ArgumentException("A Point3 is 6 bytes.", nameof(bytes));
    }

    public sealed class Mesh
    {
        public Point3 Origin { get; set; }

        public Point3? Pivot { get; set; }

        public List<Point3> Vertices { get; set; } = [];

        public byte[] Raw { get; set; } = [];
    }

    public sealed class DeclaredBlob
    {
        [PromptuariumType(PromptuariumType.Blob)]
        public Point3 Value { get; set; }
    }

    private static PromptuariumSerializerOptions MeshOptions() => new PromptuariumSerializerOptions()
        .AddBlobType<Point3>(point => point.ToBytes(), Point3.FromBytes);

    private static byte[] StoredBytes(Element root, string name)
    {
        Element holder = root.Children.Single(child => child.Data.AsUtf8String() == name);

        Assert.AreEqual(0xF0, holder.Children[0].Data.Span[0], "the type of " + name);
        return holder.Children[1].Data.ToArray();
    }

    [TestMethod]
    public void AddBlobType_StoresTheTypeAsBytesEverywhere()
    {
        var mesh = new Mesh { Origin = new Point3(1, -2, 3), Pivot = new Point3(0, 0, 7), Vertices = [new Point3(1, 1, 1), new Point3(-1, -1, -1)], Raw = [9, 8] };
        PromptuariumSerializerOptions options = MeshOptions();

        Element root = PromptuariumSerializer.ToElement(mesh, options);
        Mesh copy = PromptuariumSerializer.Deserialize<Mesh>(root, options);

        CollectionAssert.AreEqual(new Point3(1, -2, 3).ToBytes(), StoredBytes(root, "Origin"));
        Assert.AreEqual(mesh.Origin, copy.Origin);
        Assert.AreEqual(mesh.Pivot, copy.Pivot);
        CollectionAssert.AreEqual(mesh.Vertices, copy.Vertices);
        CollectionAssert.AreEqual(mesh.Raw, copy.Raw);
    }

    [TestMethod]
    public void AddBlobType_ServesAPropertyDeclaredAsBlob()
    {
        PromptuariumSerializerOptions options = MeshOptions();

        Element root = PromptuariumSerializer.ToElement(new DeclaredBlob { Value = new Point3(4, 5, 6) }, options);

        Assert.AreEqual(new Point3(4, 5, 6), PromptuariumSerializer.Deserialize<DeclaredBlob>(root, options).Value);
    }

    [TestMethod]
    public void Blob_OnAPropertyOfAnUnregisteredType_IsRejected()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new DeclaredBlob()));
    }

    [TestMethod]
    public void AddBlobType_BytesThatDoNotDecode_AreRejectedWhenReading()
    {
        Element root = Nodes.Root(Nodes.Member("Origin", 0xF0, new byte[] { 1, 2, 3 }));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Mesh>(root, MeshOptions()));
    }

    [TestMethod]
    public void AddBlobType_BytesThatDoNotGiveTheValueBack_AreRejectedWhenWriting()
    {
        PromptuariumSerializerOptions options = new PromptuariumSerializerOptions()
            .AddBlobType<Point3>(point => new Point3(point.X, point.Y, 0).ToBytes(), Point3.FromBytes);

        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new Mesh { Origin = new Point3(1, 2, 3) }, options));
    }

    [TestMethod]
    public void AddBlobType_TypeRegisteredAsTextAlready_IsRejected()
    {
        var options = new PromptuariumSerializerOptions();
        options.AddTextType<Point3>(point => $"{point.X},{point.Y},{point.Z}", text => default);

        Assert.ThrowsExactly<ArgumentException>(() => options.AddBlobType<Point3>(point => point.ToBytes(), Point3.FromBytes));
        Assert.ThrowsExactly<ArgumentException>(() => options.AddBlobType<byte[]>(bytes => bytes, bytes => bytes));
    }

    // ---------------------------------------------------------------- the default text types

    public sealed class Network
    {
        public Version? Version { get; set; }

        public Uri? Home { get; set; }

        public Uri? Relative { get; set; }

        public IPAddress? Gateway { get; set; }

        public IPNetwork Subnet { get; set; }

        public List<IPAddress> Servers { get; set; } = [];

        public Dictionary<string, Uri> Links { get; set; } = [];
    }

    private static Network SampleNetwork() => new()
    {
        Version = new Version(8, 2, 2),
        Home = new Uri("https://example.com/promptuarium?tab=1#top"),
        Relative = new Uri("docs/README.md", UriKind.Relative),
        Gateway = IPAddress.Parse("192.168.0.1"),
        Subnet = IPNetwork.Parse("10.0.0.0/8"),
        Servers = [IPAddress.Parse("2001:db8::1"), IPAddress.Parse("fe80::1%3")],
        Links = new() { ["docs"] = new Uri("https://example.com/docs") },
    };

    [TestMethod]
    public void DefaultTypes_AreStoredAsTextWithoutARegistration()
    {
        Network network = SampleNetwork();

        Element root = PromptuariumSerializer.ToElement(network);
        Network copy = PromptuariumSerializer.Deserialize<Network>(root);

        Assert.AreEqual("8.2.2", StoredText(root, "Version"));
        Assert.AreEqual("https://example.com/promptuarium?tab=1#top", StoredText(root, "Home"));
        Assert.AreEqual("docs/README.md", StoredText(root, "Relative"));
        Assert.AreEqual("192.168.0.1", StoredText(root, "Gateway"));
        Assert.AreEqual("10.0.0.0/8", StoredText(root, "Subnet"));
        Assert.AreEqual(network.Version, copy.Version);
        Assert.AreEqual(network.Home!.OriginalString, copy.Home!.OriginalString);
        Assert.AreEqual(network.Relative!.OriginalString, copy.Relative!.OriginalString);
        Assert.AreEqual(network.Gateway, copy.Gateway);
        Assert.AreEqual(network.Subnet, copy.Subnet);
        CollectionAssert.AreEqual(network.Servers, copy.Servers);
        Assert.AreEqual(network.Links["docs"], copy.Links["docs"]);
    }

    [TestMethod]
    public void DefaultTypes_ARegistrationWins()
    {
        PromptuariumSerializerOptions options = new PromptuariumSerializerOptions()
            .AddTextType<Version>(version => "v" + version, text => text.StartsWith('v') ? Version.Parse(text[1..]) : throw new FormatException());

        Element root = PromptuariumSerializer.ToElement(new Network { Version = new Version(1, 2) }, options);

        Assert.AreEqual("v1.2", StoredText(root, "Version"));
        Assert.AreEqual(new Version(1, 2), PromptuariumSerializer.Deserialize<Network>(root, options).Version);
    }

    public sealed class VersionOnly
    {
        public Version? Version { get; set; }
    }

    [TestMethod]
    public void ClearDefaultTypes_RemovesThem()
    {
        PromptuariumSerializerOptions options = new PromptuariumSerializerOptions().ClearDefaultTypes();

        Element root = PromptuariumSerializer.ToElement(new VersionOnly { Version = new Version(1, 2) }, options);
        Element version = root.Children.Single(child => child.Data.AsUtf8String() == "Version");

        Assert.AreEqual(0x20, version.Children[0].Data.Span[0], "an object of its properties");
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<VersionOnly>(Nodes.Root(Nodes.Member("Version", 0x80, Data.FromUtf8String("1.2"))), new PromptuariumSerializerOptions().ClearDefaultTypes()));
    }

    [TestMethod]
    public void ClearDefaultTypes_OnTheDefaultOrOnUsedOptions_IsRejected()
    {
        var options = new PromptuariumSerializerOptions();
        PromptuariumSerializer.Serialize(new Network(), options);

        Assert.ThrowsExactly<InvalidOperationException>(() => PromptuariumSerializerOptions.Default.ClearDefaultTypes());
        Assert.ThrowsExactly<InvalidOperationException>(() => options.ClearDefaultTypes());
    }
}
