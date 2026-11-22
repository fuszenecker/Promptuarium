#pragma warning disable PRMTSER001

namespace Promptuarium.Serializer.Tests;

/// <summary>An enum by its name (the default) or by its number ([PromptuariumType] on the property or on the enum).</summary>
[TestClass]
public sealed class EnumFormTests
{
    public enum Color
    {
        Red,
        Green,
        Blue,
    }

    [PromptuariumType(PromptuariumType.UInt8)]
    public enum Level : byte
    {
        Low = 1,
        High = 200,
    }

    [Flags]
    public enum Access
    {
        None = 0,
        Read = 1,
        Write = 2,
    }

    public enum Huge : ulong
    {
        Max = ulong.MaxValue,
    }

    [PromptuariumType(PromptuariumType.Blob)]
    public enum Wrong
    {
        One,
    }

    public sealed class Holder
    {
        public Color ByName { get; set; } = Color.Blue;

        [PromptuariumType(PromptuariumType.Int32)]
        public Color ByNumber { get; set; } = Color.Green;

        [PromptuariumType(PromptuariumType.Float64)]
        public Color ByFloat { get; set; } = Color.Blue;

        public Level Level { get; set; } = Level.High;

        [PromptuariumType(PromptuariumType.Utf8String)]
        public Level LevelByName { get; set; } = Level.Low;

        [PromptuariumType(PromptuariumType.Int16)]
        public Level LevelAsInt16 { get; set; } = Level.High;

        [PromptuariumType(PromptuariumType.Int32)]
        public Access Access { get; set; } = Access.Read | Access.Write;

        [PromptuariumType(PromptuariumType.UInt64)]
        public Huge Huge { get; set; } = Huge.Max;

        public List<Level> Levels { get; set; } = [Level.Low, Level.High];

        [PromptuariumType(PromptuariumType.Int32)]
        public Color? Nothing { get; set; }
    }

    [TestMethod]
    public void Forms_AreWrittenAsDeclared()
    {
        Element root = PromptuariumSerializer.ToElement(new Holder());

        Assert.AreEqual("PRMT:GEN2-LE", root.MetaData.AsAsciiString());
        AssertMember(root, "ByName", 0x80, "Blue"u8.ToArray());
        AssertMember(root, "ByNumber", 0x42, [1, 0, 0, 0]);
        AssertMember(root, "ByFloat", 0x63, BitConverter.GetBytes(2.0));
        AssertMember(root, "Level", 0x50, [200]);
        AssertMember(root, "LevelByName", 0x80, "Low"u8.ToArray());
        AssertMember(root, "LevelAsInt16", 0x41, [200, 0]);
        AssertMember(root, "Access", 0x42, [3, 0, 0, 0]);
        AssertMember(root, "Huge", 0x53, [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);

        Element levels = Member(root, "Levels");
        Assert.AreEqual(0x50, levels.Children[1].Children[0].Data.Span[0]);
        CollectionAssert.AreEqual(new byte[] { 200 }, levels.Children[2].Children[1].Data.ToArray());
        Assert.AreEqual(0x00, Member(root, "Nothing").Children[0].Data.Span[0]);
    }

    [TestMethod]
    public void Forms_RoundTrip()
    {
        var holder = new Holder();

        Holder copy = PromptuariumSerializer.Deserialize<Holder>(PromptuariumSerializer.Serialize(holder));

        Assert.AreEqual(holder.ByName, copy.ByName);
        Assert.AreEqual(holder.ByNumber, copy.ByNumber);
        Assert.AreEqual(holder.ByFloat, copy.ByFloat);
        Assert.AreEqual(holder.Level, copy.Level);
        Assert.AreEqual(holder.LevelByName, copy.LevelByName);
        Assert.AreEqual(holder.LevelAsInt16, copy.LevelAsInt16);
        Assert.AreEqual(holder.Access, copy.Access);
        Assert.AreEqual(holder.Huge, copy.Huge);
        CollectionAssert.AreEqual(holder.Levels, copy.Levels);
        Assert.IsNull(copy.Nothing);
    }

    public sealed class Version1Holder
    {
        [PromptuariumType(PromptuariumType.Float64)]
        public Color Color { get; set; } = Color.Green;
    }

    [TestMethod]
    public void Float64_GivesAFileOfVersion1()
    {
        Assert.AreEqual("PRMT:GEN1-LE", PromptuariumSerializer.ToElement(new Version1Holder()).MetaData.AsAsciiString());
    }

    public sealed class TooSmall
    {
        [PromptuariumType(PromptuariumType.Int8)]
        public Level Level { get; set; } = Level.High;
    }

    public sealed class WrongHolder
    {
        public Wrong Value { get; set; }
    }

    [TestMethod]
    public void ANumberThatDoesNotFit_OrAWrongForm_IsRejected()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.ToElement(new TooSmall()));
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.ToElement(new WrongHolder()));
    }

    private static Element Member(Element root, string name) => root.Children.Single(child => child.Data.AsUtf8String() == name);

    private static void AssertMember(Element root, string name, byte type, byte[] value)
    {
        Element member = Member(root, name);
        Assert.AreEqual(type, member.Children[0].Data.Span[0], name);
        CollectionAssert.AreEqual(value, member.Children[1].Data.ToArray(), name);
    }
}
