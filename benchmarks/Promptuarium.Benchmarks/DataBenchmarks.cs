using BenchmarkDotNet.Attributes;
using Promptuarium;

namespace Promptuarium.Benchmarks;

/// <summary>The conversions of the Data class, both ways.</summary>
[MemoryDiagnoser]
public class DataBenchmarks
{
    private const string Text = "Promptuarium, a tree-based container — árvíztűrő tükörfúrógép";

    private readonly ReadOnlyMemory<byte> intContent = Data.FromInt(123_456_789);
    private readonly ReadOnlyMemory<byte> varIntContent = Data.FromVarInt(-123_456_789);
    private readonly ReadOnlyMemory<byte> varUIntContent = Data.FromVarUInt(123_456_789);
    private readonly ReadOnlyMemory<byte> decimalContent = Data.FromDecimal(12_345.6789m);
    private readonly ReadOnlyMemory<byte> utf8Content = Data.FromUtf8String(Text);
    private readonly ReadOnlyMemory<byte> dateTimeOffsetContent = Data.FromDateTimeOffset(new DateTimeOffset(2026, 11, 21, 12, 0, 0, TimeSpan.FromHours(1)));

    [Benchmark]
    public ReadOnlyMemory<byte> FromInt() => Data.FromInt(123_456_789);

    [Benchmark]
    public int AsInt() => intContent.AsInt();

    [Benchmark]
    public ReadOnlyMemory<byte> FromVarInt() => Data.FromVarInt(-123_456_789);

    [Benchmark]
    public long AsVarInt() => varIntContent.AsVarInt();

    [Benchmark]
    public ReadOnlyMemory<byte> FromVarUInt() => Data.FromVarUInt(123_456_789);

    [Benchmark]
    public ulong AsVarUInt() => varUIntContent.AsVarUInt();

    [Benchmark]
    public ReadOnlyMemory<byte> FromDecimal() => Data.FromDecimal(12_345.6789m);

    [Benchmark]
    public decimal AsDecimal() => decimalContent.AsDecimal();

    [Benchmark]
    public ReadOnlyMemory<byte> FromUtf8String() => Data.FromUtf8String(Text);

    [Benchmark]
    public string AsUtf8String() => utf8Content.AsUtf8String();

    [Benchmark]
    public ReadOnlyMemory<byte> FromDateTimeOffset() => Data.FromDateTimeOffset(new DateTimeOffset(2026, 11, 21, 12, 0, 0, TimeSpan.FromHours(1)));

    [Benchmark]
    public DateTimeOffset AsDateTimeOffset() => dateTimeOffsetContent.AsDateTimeOffset();
}
