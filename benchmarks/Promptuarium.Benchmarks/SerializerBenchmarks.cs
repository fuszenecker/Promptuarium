using BenchmarkDotNet.Attributes;
using Promptuarium.Serializer;

namespace Promptuarium.Benchmarks;

/// <summary>Promptuarium.Serializer: a configuration-like object and a list of records, both ways.</summary>
[MemoryDiagnoser]
public class SerializerBenchmarks
{
    private Settings settings = null!;
    private Catalog catalog = null!;
    private byte[] settingsBytes = null!;
    private byte[] catalogBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        settings = new Settings();
        catalog = new Catalog
        {
            Items = Enumerable.Range(0, 1_000)
                .Select(i => new Item(i, $"item {i}", i * 1.5, i % 2 == 0, [(byte)i, (byte)(i >> 8)]))
                .ToList(),
        };

        settingsBytes = PromptuariumSerializer.Serialize(settings);
        catalogBytes = PromptuariumSerializer.Serialize(catalog);
    }

    [Benchmark]
    public byte[] SerializeSettings() => PromptuariumSerializer.Serialize(settings);

    [Benchmark]
    public Settings DeserializeSettings() => PromptuariumSerializer.Deserialize<Settings>(settingsBytes);

    [Benchmark]
    public byte[] SerializeCatalog() => PromptuariumSerializer.Serialize(catalog);

    [Benchmark]
    public Catalog DeserializeCatalog() => PromptuariumSerializer.Deserialize<Catalog>(catalogBytes);

    public sealed class Settings
    {
        [PromptuariumComment("The server the application connects to")]
        public string Host { get; set; } = "example.com";

        public int Port { get; set; } = 8443;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        public bool Tls { get; set; } = true;

        public byte[] Key { get; set; } = [0x3A, 0x9F, 0x01, 0xC4];

        public Dictionary<string, string> Levels { get; set; } = new()
        {
            ["Default"] = "Information",
            ["Microsoft"] = "Warning",
        };

        public List<string> Hosts { get; set; } = ["a.example.com", "b.example.com", "c.example.com"];
    }

    public sealed class Catalog
    {
        public List<Item> Items { get; set; } = [];
    }

    public sealed record Item(int Id, string Name, double Price, bool Available, byte[] Code);
}
