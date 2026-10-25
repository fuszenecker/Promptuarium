// Configuration: settings in sections, with typed values.
//
// Every setting is a node: the metadata is its name, the data is its value. A section is a node with a name
// and no data; its children are its settings (and its subsections). Unlike a text format, the values keep
// their types and need no escaping, and a value can be anything: a number, a date, a binary key.

using Promptuarium;

var settings = new Element(null, Data.FromAsciiString("settings"),
    Section("server",
        Setting("host", Data.FromUtf8String("example.com")),
        Setting("port", Data.FromInt(8443)),
        Setting("timeout", Data.FromTimeSpan(TimeSpan.FromSeconds(30))),
        Section("tls",
            Setting("enabled", Data.FromBool(true)),
            Setting("key", new byte[] { 0x3A, 0x9F, 0x01, 0xC4 }))),
    Section("ui",
        Setting("theme", Data.FromUtf8String("dark")),
        Setting("scale", Data.FromDouble(1.25))));

// Store and load (e.g. a file in the home directory; here, in memory)
Element loaded = Element.FromBytes(settings.ToBytes());

string host = Get(loaded, "server/host").AsUtf8String();
int port = Get(loaded, "server/port").AsInt();
TimeSpan timeout = Get(loaded, "server/timeout").AsTimeSpan();
bool tls = Get(loaded, "server/tls/enabled").AsBool();
double scale = Get(loaded, "ui/scale").AsDouble();

Console.WriteLine($"https://{host}:{port}, timeout {timeout.TotalSeconds} s, TLS {(tls ? "on" : "off")}, UI scale {scale}");
Console.WriteLine($"TLS key: {Convert.ToHexString(Get(loaded, "server/tls/key").Span)}");

// Changing a setting is setting its data
Find(loaded, "ui/theme")!.Data = Data.FromUtf8String("light");
Console.WriteLine($"Theme: {Get(loaded, "ui/theme").AsUtf8String()}");

return host == "example.com" && port == 8443 && tls && scale == 1.25 ? 0 : 1;

static Element Section(string name, params Element[] settings) => new(null, Data.FromAsciiString(name), settings);

static Element Setting(string name, ReadOnlyMemory<byte> value) => new(value, Data.FromAsciiString(name));

// Finds a setting by its path of names ("server/tls/enabled")
static Element? Find(Element root, string path)
{
    Element? node = root;

    foreach (string name in path.Split('/'))
    {
        node = node?.Children.FirstOrDefault(child => child.MetaData.AsAsciiString() == name);
    }

    return node;
}

static ReadOnlyMemory<byte> Get(Element root, string path) => Find(root, path)?.Data ?? throw new KeyNotFoundException(path);
