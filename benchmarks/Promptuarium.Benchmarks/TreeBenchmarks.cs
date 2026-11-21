using BenchmarkDotNet.Attributes;
using Promptuarium;

namespace Promptuarium.Benchmarks;

/// <summary>Saving and loading trees, in memory and through streams, and the operations on whole trees.</summary>
[MemoryDiagnoser]
public class TreeBenchmarks
{
    private Element tree = null!;
    private Element copy = null!;
    private byte[] bytes = null!;
    private MemoryStream output = null!;

    [Params(TreeShape.Wide, TreeShape.Balanced, TreeShape.Deep)]
    public TreeShape Shape { get; set; }

    [Params(1_000, 100_000)]
    public int Nodes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        tree = Trees.Create(Shape, Nodes);
        copy = Trees.Create(Shape, Nodes);
        bytes = tree.ToBytes();
        output = new MemoryStream(bytes.Length);
    }

    [Benchmark]
    public Element Build() => Trees.Create(Shape, Nodes);

    [Benchmark]
    public byte[] ToBytes() => tree.ToBytes();

    [Benchmark]
    public Element FromBytes() => Element.FromBytes(bytes);

    [Benchmark]
    public async Task SaveAsync()
    {
        output.Position = 0;
        await tree.SaveAsync(output);
    }

    [Benchmark]
    public Task<Element> LoadAsync() => Element.LoadAsync(new MemoryStream(bytes, writable: false));

    [Benchmark]
    public Statistics GetStatistics() => tree.GetStatistics();

    [Benchmark]
    public bool StructurallyEquals() => tree.StructurallyEquals(copy);

    [Benchmark]
    public long Walk()
    {
        long length = 0;
        tree.Walk((node, _) => length += node.Data.Length);
        return length;
    }

    [Benchmark]
    public Element Clone() => new(tree);
}
