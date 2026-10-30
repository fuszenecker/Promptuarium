// Configuration: settings in sections, with typed values, stored with Promptuarium.Serializer as configuration files
// (PRMT:CFG1-LE, SERIALIZER_FORMAT.md).
//
// The settings are plain classes, with comments that are written into the file (for the people and the editors that
// read it, e.g. PStudio). The defaults are one file, the user's changes another one on top of them; a file of another
// kind is skipped, not an error. Unlike a text format, the values keep their types and need no escaping, and a value
// can be anything: a number, a date, a binary key.

using Promptuarium;
using Promptuarium.Serializer;

var options = new PromptuariumSerializerOptions { Signature = PromptuariumSerializerOptions.ConfigurationSignature };

// Store (e.g. files next to the application and in the home directory; here, in memory)
byte[] defaultsFile = PromptuariumSerializer.Serialize(new Settings(), options);

// The user's file has only what the user changed
byte[] userFile = PromptuariumSerializer.Serialize(new Dictionary<string, object>
{
    ["Ui"] = new Dictionary<string, object> { ["Theme"] = "light" },
}, options);

// A file of another kind, e.g. a general one of the serializer (PRMT:GEN1-LE)
byte[] otherFile = PromptuariumSerializer.Serialize(new Dictionary<string, object>
{
    ["Ui"] = new Dictionary<string, object> { ["Theme"] = "neon" },
});

// Load: the defaults, then the layers on top of them; a layer of another kind is skipped
Settings settings = PromptuariumSerializer.Deserialize<Settings>(defaultsFile, options);

var layers = new PromptuariumSerializerOptions
{
    Signature = PromptuariumSerializerOptions.ConfigurationSignature,
    UnexpectedSignature = signature =>
    {
        Console.WriteLine($"Skipped a layer: {signature}");
        return SignatureAction.Skip;
    },
};

foreach (byte[] layer in new[] { userFile, otherFile })
{
    PromptuariumSerializer.Populate(layer, settings, layers);
}

Console.WriteLine($"Signature: {Element.FromBytes(defaultsFile).MetaData.AsAsciiString()}");
Console.WriteLine($"https://{settings.Server.Host}:{settings.Server.Port}, timeout {settings.Server.Timeout.TotalSeconds} s, TLS {(settings.Server.Tls.Enabled ? "on" : "off")}, UI scale {settings.Ui.Scale}");
Console.WriteLine($"TLS key: {Convert.ToHexString(settings.Server.Tls.Key)}");
Console.WriteLine($"Theme: {settings.Ui.Theme}");

// The comments are COMMENT nodes (metadata 0x04) in the file; reading ignores them
foreach (string comment in Comments(Element.FromBytes(defaultsFile)))
{
    Console.WriteLine($"Comment: {comment}");
}

// Without the callback, a file of another kind is rejected
bool rejected = false;

try
{
    PromptuariumSerializer.Deserialize<Settings>(otherFile, options);
}
catch (InvalidDataException exception)
{
    Console.WriteLine($"Rejected: {exception.Message}");
    rejected = true;
}

return settings.Server.Port == 8443 && settings.Ui.Theme == "light" && rejected && Comments(Element.FromBytes(defaultsFile)).Count() == 7 ? 0 : 1;

static IEnumerable<string> Comments(Element node)
{
    if (node.MetaData.Length == 1 && node.MetaData.Span[0] == 0x04)
    {
        yield return node.Data.AsUtf8String();
    }

    foreach (string comment in node.Children.SelectMany(Comments))
    {
        yield return comment;
    }
}

[PromptuariumComment("The settings of the example application")]
internal sealed class Settings
{
    public Server Server { get; } = new();

    public Ui Ui { get; } = new();
}

[PromptuariumComment("The server to connect to")]
internal sealed class Server
{
    public string Host { get; set; } = "example.com";

    [PromptuariumComment("The TCP port of HTTPS")]
    public int Port { get; set; } = 8443;

    [PromptuariumComment("How long to wait for an answer")]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public Tls Tls { get; } = new();
}

internal sealed class Tls
{
    public bool Enabled { get; set; } = true;

    [PromptuariumComment("The key, as it is (a BLOB, not base64)")]
    public byte[] Key { get; set; } = [0x3A, 0x9F, 0x01, 0xC4];
}

[PromptuariumComment("The look of the user interface")]
internal sealed class Ui
{
    [PromptuariumComment("dark or light")]
    public string Theme { get; set; } = "dark";

    public double Scale { get; set; } = 1.25;
}
