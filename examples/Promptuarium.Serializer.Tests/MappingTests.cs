using Promptuarium;
using Promptuarium.Serializer;

namespace Promptuarium.Serializer.Tests;

/// <summary>How .NET values map to the types of the format, and back.</summary>
[TestClass]
public sealed class MappingTests
{
    public enum Color
    {
        Red,
        Green,
        Blue,
    }

    [Flags]
    public enum Access
    {
        None = 0,
        Read = 1,
        Write = 2,
    }

    public sealed class Everything
    {
        public bool Flag { get; set; }
        public int Int { get; set; }
        public long Long { get; set; }
        public double Double { get; set; }
        public float Float { get; set; }
        public decimal Decimal { get; set; }
        public byte Byte { get; set; }
        public int? NullableInt { get; set; }
        public string Text { get; set; } = "";
        public string? Missing { get; set; }
        public char Character { get; set; }
        public Color Color { get; set; }
        public Access Access { get; set; }
        public DateTime When { get; set; }
        public DateTimeOffset WhenOffset { get; set; }
        public DateOnly Day { get; set; }
        public TimeOnly Time { get; set; }
        public TimeSpan Duration { get; set; }
        public Guid Id { get; set; }
        public byte[] Payload { get; set; } = [];
        public int[] Numbers { get; set; } = [];
        public List<string> Names { get; set; } = [];
        public IReadOnlyList<double> ReadOnlyValues { get; set; } = [];
        public HashSet<string> Tags { get; set; } = [];
        public Dictionary<string, int> Counts { get; set; } = [];
        public IReadOnlyDictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
        public Point Location { get; set; }
        public Everything? Child { get; set; }
    }

    public struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    private static Everything Sample() => new()
    {
        Flag = true,
        Int = -42,
        Long = 1L << 40,
        Double = 523.25,
        Float = 0.5f,
        Decimal = 12.75m,
        Byte = 255,
        NullableInt = 7,
        Text = "árvíztűrő tükörfúrógép",
        Character = 'Q',
        Color = Color.Blue,
        Access = Access.Read | Access.Write,
        When = new DateTime(2026, 10, 28, 19, 30, 15, DateTimeKind.Utc),
        WhenOffset = new DateTimeOffset(2026, 10, 28, 19, 30, 15, TimeSpan.FromHours(1)),
        Day = new DateOnly(2026, 10, 28),
        Time = new TimeOnly(19, 30, 15),
        Duration = TimeSpan.FromMinutes(90),
        Id = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
        Payload = [0, 1, 2, 0xff],
        Numbers = [3, 1, 2],
        Names = ["a", "b"],
        ReadOnlyValues = [1.5, 2.5],
        Tags = ["x", "y"],
        Counts = new() { ["one"] = 1, ["two"] = 2 },
        Labels = new Dictionary<string, string> { ["k"] = "v" },
        Location = new Point { X = 3, Y = 4 },
        Child = new Everything { Text = "child", Names = ["c"] },
    };

    [TestMethod]
    public void RoundTrip_KeepsEveryValue()
    {
        Everything original = Sample();

        Everything copy = PromptuariumSerializer.Deserialize<Everything>(PromptuariumSerializer.Serialize(original));

        Assert.AreEqual(original.Flag, copy.Flag);
        Assert.AreEqual(original.Int, copy.Int);
        Assert.AreEqual(original.Long, copy.Long);
        Assert.AreEqual(original.Double, copy.Double);
        Assert.AreEqual(original.Float, copy.Float);
        Assert.AreEqual(original.Decimal, copy.Decimal);
        Assert.AreEqual(original.Byte, copy.Byte);
        Assert.AreEqual(original.NullableInt, copy.NullableInt);
        Assert.AreEqual(original.Text, copy.Text);
        Assert.IsNull(copy.Missing);
        Assert.AreEqual(original.Character, copy.Character);
        Assert.AreEqual(original.Color, copy.Color);
        Assert.AreEqual(original.Access, copy.Access);
        Assert.AreEqual(original.When, copy.When);
        Assert.AreEqual(DateTimeKind.Utc, copy.When.Kind);
        Assert.AreEqual(original.WhenOffset, copy.WhenOffset);
        Assert.AreEqual(original.WhenOffset.Offset, copy.WhenOffset.Offset);
        Assert.AreEqual(original.Day, copy.Day);
        Assert.AreEqual(original.Time, copy.Time);
        Assert.AreEqual(original.Duration, copy.Duration);
        Assert.AreEqual(original.Id, copy.Id);
        CollectionAssert.AreEqual(original.Payload, copy.Payload);
        CollectionAssert.AreEqual(original.Numbers, copy.Numbers);
        CollectionAssert.AreEqual(original.Names, copy.Names);
        CollectionAssert.AreEqual(original.ReadOnlyValues.ToList(), copy.ReadOnlyValues.ToList());
        Assert.IsTrue(original.Tags.SetEquals(copy.Tags));
        CollectionAssert.AreEquivalent(original.Counts, copy.Counts);
        Assert.AreEqual("v", copy.Labels["k"]);
        Assert.AreEqual(original.Location, copy.Location);
        Assert.AreEqual("child", copy.Child!.Text);
        CollectionAssert.AreEqual(new[] { "c" }, copy.Child.Names);
        Assert.IsNull(copy.Child.Child);
    }

    [TestMethod]
    public void Serialize_UsesTheTypeCodesOfTheFormat()
    {
        Element root = PromptuariumSerializer.ToElement(Sample());

        Assert.AreEqual(0x01, TypeOf(root, "Flag"));
        Assert.AreEqual(0x63, TypeOf(root, "Int"));
        Assert.AreEqual(0x63, TypeOf(root, "Decimal"));
        Assert.AreEqual(0x80, TypeOf(root, "Text"));
        Assert.AreEqual(0x00, TypeOf(root, "Missing"));
        Assert.AreEqual(0x80, TypeOf(root, "Color"));
        Assert.AreEqual(0x80, TypeOf(root, "When"));
        Assert.AreEqual(0xF0, TypeOf(root, "Payload"));
        Assert.AreEqual(0x21, TypeOf(root, "Numbers"));
        Assert.AreEqual(0x20, TypeOf(root, "Counts"));
        Assert.AreEqual(0x20, TypeOf(root, "Location"));
        Assert.AreEqual(0x20, TypeOf(root, "Child"));
    }

    [TestMethod]
    public void Blob_IsTheBytesAsTheyAre()
    {
        Element root = PromptuariumSerializer.ToElement(new Everything { Payload = [0xde, 0xad, 0xbe, 0xef] });

        Element payload = root.Children.Single(member => member.Data.AsUtf8String() == "Payload");
        CollectionAssert.AreEqual(new byte[] { 0xde, 0xad, 0xbe, 0xef }, payload.Children[1].Data.ToArray());
    }

    [TestMethod]
    public void Enum_IsWrittenByName()
    {
        Element root = PromptuariumSerializer.ToElement(new Everything { Color = Color.Green, Access = Access.Read | Access.Write });

        Assert.AreEqual("Green", ValueOf(root, "Color").AsUtf8String());
        Assert.AreEqual("Read, Write", ValueOf(root, "Access").AsUtf8String());
    }

    [TestMethod]
    public void Enum_IsReadFromANameInAnyCaseOrANumber()
    {
        Element byName = Nodes.Root(Nodes.Member("Color", 0x80, Data.FromUtf8String("blue")));
        Element byNumber = Nodes.Root(Nodes.Member("Color", 0x63, Data.FromDouble(1)));

        Assert.AreEqual(Color.Blue, PromptuariumSerializer.Deserialize<Everything>(byName).Color);
        Assert.AreEqual(Color.Green, PromptuariumSerializer.Deserialize<Everything>(byNumber).Color);
    }

    [TestMethod]
    public void Untyped_GivesDictionariesListsAndPlainValues()
    {
        var decoded = PromptuariumSerializer.Deserialize<Dictionary<string, object?>>(PromptuariumSerializer.Serialize(Sample()));

        Assert.IsInstanceOfType<bool>(decoded["Flag"]);
        Assert.AreEqual(-42.0, decoded["Int"]);
        Assert.AreEqual("Blue", decoded["Color"]);
        Assert.IsInstanceOfType<byte[]>(decoded["Payload"]);
        Assert.IsInstanceOfType<List<object?>>(decoded["Numbers"]);
        Assert.IsInstanceOfType<Dictionary<string, object?>>(decoded["Location"]);
    }

    [TestMethod]
    public void Deserialize_FractionIntoAnInteger_IsRejected()
    {
        Element root = Nodes.Root(Nodes.Member("Int", 0x63, Data.FromDouble(1.5)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Everything>(root));
    }

    [TestMethod]
    public void Deserialize_NumberOutOfRange_IsRejected()
    {
        Element root = Nodes.Root(Nodes.Member("Byte", 0x63, Data.FromDouble(256)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Everything>(root));
    }

    [TestMethod]
    [DataRow("Text", (byte)0x63, DisplayName = "FLOAT64 into a string")]
    [DataRow("Int", (byte)0x80, DisplayName = "UTF8_STRING into an int")]
    [DataRow("Payload", (byte)0x80, DisplayName = "UTF8_STRING into a byte[]")]
    [DataRow("Names", (byte)0x20, DisplayName = "OBJECT into a list")]
    [DataRow("Counts", (byte)0x21, DisplayName = "ARRAY into a dictionary")]
    [DataRow("Flag", (byte)0x21, DisplayName = "ARRAY into a bool")]
    public void Deserialize_TypeThatDoesNotFit_IsRejected(string member, byte type)
    {
        ReadOnlyMemory<byte>? value = type switch
        {
            0x63 => Data.FromDouble(1),
            0x80 => Data.FromUtf8String("text"),
            _ => null,
        };

        Element root = Nodes.Root(Nodes.Member(member, type, value));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Everything>(root));
    }

    [TestMethod]
    public void Deserialize_InvalidText_IsRejected()
    {
        Element root = Nodes.Root(Nodes.Member("Id", 0x80, Data.FromUtf8String("not a guid")));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Everything>(root));
    }

    [TestMethod]
    public void Deserialize_NullElementOfValueType_IsRejected()
    {
        Element numbers = Nodes.Member("Numbers", 0x21);
        numbers.Add(Nodes.Holder(0x02, ReadOnlyMemory<byte>.Empty, 0x00));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Everything>(Nodes.Root(numbers)));
    }

    [TestMethod]
    public void Deserialize_UnknownMembers_AreIgnored()
    {
        Element root = Nodes.Root(
            Nodes.Member("Unknown", 0x80, Data.FromUtf8String("ignored")),
            Nodes.Object("UnknownObject", Nodes.Member("x", 0x01, Data.FromBool(true))),
            Nodes.Member("Text", 0x80, Data.FromUtf8String("kept")));

        Assert.AreEqual("kept", PromptuariumSerializer.Deserialize<Everything>(root).Text);
    }

    [TestMethod]
    public void Deserialize_UnknownMembers_AreStillValidated()
    {
        Element root = Nodes.Root(Nodes.Member("Unknown", 0x42, Data.FromInt(1)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Everything>(root));
    }

    [TestMethod]
    public void Deserialize_NullForANonNullableMember_KeepsItsValue()
    {
        Element root = Nodes.Root(Nodes.Member("Int", 0x00), Nodes.Member("Text", 0x00));

        Everything result = PromptuariumSerializer.Deserialize<Everything>(root);

        Assert.AreEqual(0, result.Int);
        Assert.AreEqual("", result.Text);
    }

    private static byte TypeOf(Element root, string name)
    {
        return root.Children.Single(member => member.Data.AsUtf8String() == name).Children[0].Data.Span[0];
    }

    private static ReadOnlyMemory<byte> ValueOf(Element root, string name)
    {
        return root.Children.Single(member => member.Data.AsUtf8String() == name).Children[1].Data;
    }
}
