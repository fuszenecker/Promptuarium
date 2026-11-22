using Promptuarium;
using Promptuarium.Serializer;
using PStudio.Models;
using PStudio.Services;

namespace PStudio.Tests.Services;

[TestClass]
public sealed class SerializerLayoutTests
{
    // A tree of Promptuarium.Serializer: every member is a NAME holder with a TYPE node and a VALUE node
    private static readonly Element Tree = PromptuariumSerializer.ToElement(new Dictionary<string, object>
    {
        ["rate"] = 1.5,
        ["name"] = "x",
        ["on"] = true,
        ["key"] = new byte[] { 1, 2 },
        ["list"] = new List<object> { "a" },
    });

    [TestMethod]
    public void OneByteMetadata_IsATag()
    {
        Element rate = Tree.Children[0];

        Assert.AreEqual(ContentForm.SerializerTag, SerializerLayout.MetaDataForm(rate, Tree));
        Assert.AreEqual(ContentForm.SerializerTag, SerializerLayout.MetaDataForm(rate.Children[0], rate));
        Assert.IsNull(SerializerLayout.MetaDataForm(Tree, null), "the signature of the root");
    }

    [TestMethod]
    public void AnEmptyMetadata_InTheLayout_IsATagToBeTyped()
    {
        var empty = new Element();

        Assert.AreEqual(ContentForm.SerializerTag, SerializerLayout.MetaDataForm(empty, Tree.Children[0]), "a child of a holder");
        Assert.AreEqual(ContentForm.SerializerTag, SerializerLayout.MetaDataForm(empty, Tree), "a member of a signed root");
        Assert.AreEqual(ContentForm.SerializerTag, SerializerLayout.MetaDataForm(empty, new Element(null, null, new Element(null, new byte[] { 0x00 }))), "a sibling of a NAME");
        Assert.IsNull(SerializerLayout.MetaDataForm(empty, new Element(null, null, new Element(null, new byte[] { 0x07 }))), "a sibling of a byte that is not a tag");
        Assert.IsNull(SerializerLayout.MetaDataForm(empty, new Element()), "a plain tree");
    }

    [TestMethod]
    public void TheData_FollowsTheTag()
    {
        Element rate = Tree.Children[0];

        Assert.AreEqual(ContentForm.Utf8, SerializerLayout.DataForm(rate, Tree), "NAME");
        Assert.AreEqual(ContentForm.SerializerType, SerializerLayout.DataForm(rate.Children[0], rate), "TYPE");
        Assert.IsNull(SerializerLayout.DataForm(Tree, null));
    }

    [TestMethod]
    [DataRow(0, ContentForm.Double)]
    [DataRow(1, ContentForm.Utf8)]
    [DataRow(2, ContentForm.Bool)]
    [DataRow(3, ContentForm.Hex)]
    public void AValue_FollowsTheTypeOfItsHolder(int member, ContentForm expected)
    {
        Element holder = Tree.Children[member];

        Assert.AreEqual(expected, SerializerLayout.DataForm(holder.Children[1], holder));
    }

    [TestMethod]
    public void AValue_OfAnArrayItem_FollowsItsType()
    {
        Element item = Tree.Children[4].Children[1];

        Assert.AreEqual(ContentForm.Utf8, SerializerLayout.DataForm(item.Children[1], item));
    }

    [TestMethod]
    public void AComment_IsUtf8_AndDoesNotHideTheValueType()
    {
        Element tree = PromptuariumSerializer.ToElement(new Commented());
        Element rate = tree.Children[0];

        Assert.AreEqual(ContentForm.Utf8, SerializerLayout.DataForm(rate.Children[1], rate), "COMMENT");
        Assert.AreEqual(ContentForm.Double, SerializerLayout.DataForm(rate.Children[2], rate), "the VALUE after the comment");
    }

    public sealed class Nesting
    {
        public Element Inner { get; set; } = PromptuariumSerializer.ToElement(new Commented());
    }

    [TestMethod]
    public void AContainer_IsRawBytes_AndItsValueIsATree()
    {
        Element tree = PromptuariumSerializer.ToElement(new Nesting());
        Element inner = tree.Children[0];

        Assert.AreEqual("CONTAINER", SerializerLayout.Types[inner.Children[0].Data.Span[0]]);
        Assert.AreEqual(ContentForm.Hex, SerializerLayout.DataForm(inner.Children[1], inner));
        Assert.IsTrue(new Nesting().Inner.StructurallyEquals(Element.FromBytes(inner.Children[1].Data)));
    }

    public sealed class Commented
    {
        [PromptuariumComment("In Hz")]
        public double Rate { get; set; } = 8000;
    }

    [TestMethod]
    public void AValue_WithoutATypeNode_HasNoDefault()
    {
        var value = new Element(Data.FromInt(1), new byte[] { 0x03 });
        var holder = new Element(null, new byte[] { 0x00 }, value);

        Assert.IsNull(SerializerLayout.DataForm(value, holder));
    }

    // SERIALIZER_FORMAT.md, section 9.2: the members of the test vector of version 2, their forms and their texts
    private static readonly (string Name, ContentForm Form, string Text)[] Version2Values =
    [
        ("i8", ContentForm.SByte, "-5"),
        ("i32", ContentForm.Int32, "-2"),
        ("i64", ContentForm.Int64, "9007199254740993"),
        ("i128", ContentForm.Int128, "18446744073709551616"),
        ("u16", ContentForm.UInt16, "4660"),
        ("u64", ContentForm.UInt64, "18446744073709551615"),
        ("f16", ContentForm.Half, "1.5"),
        ("f32", ContentForm.Single, "0.1"),
        ("nan", ContentForm.Double, "NaN"),
        ("dec", ContentForm.Utf8, "12.750"),
        ("big", ContentForm.BigInteger, "-18446744073709551617"),
        ("id", ContentForm.Uuid, "00112233-4455-6677-8899-aabbccddeeff"),
        ("date", ContentForm.Date, "2026-11-22"),
        ("time", ContentForm.TimeOfDay, "19:30:15.5"),
        ("dt", ContentForm.UnixDateTime, "2026-11-22T19:30:15"),
        ("ts", ContentForm.Timestamp, "2026-11-22T19:30:15+01:00"),
        ("dur", ContentForm.TimeSpan, "-01:30:00"),
    ];

    private static Element Version2Tree() => PromptuariumSerializer.ToElement(new Dictionary<string, object>
    {
        ["i8"] = (sbyte)-5,
        ["i32"] = -2,
        ["i64"] = (1L << 53) + 1,
        ["i128"] = (Int128)ulong.MaxValue + 1,
        ["u16"] = (ushort)0x1234,
        ["u64"] = ulong.MaxValue,
        ["f16"] = (Half)1.5,
        ["f32"] = 0.1f,
        ["nan"] = double.NaN,
        ["dec"] = 12.750m,
        ["big"] = -(System.Numerics.BigInteger.One << 64) - 1,
        ["id"] = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"),
        ["date"] = new DateOnly(2026, 11, 22),
        ["time"] = new TimeOnly(19, 30, 15, 500),
        ["dt"] = new DateTime(2026, 11, 22, 19, 30, 15, DateTimeKind.Unspecified),
        ["ts"] = new DateTimeOffset(2026, 11, 22, 19, 30, 15, TimeSpan.FromHours(1)),
        ["dur"] = -TimeSpan.FromMinutes(90),
    });

    [TestMethod]
    public void ValuesOfVersion2_AreShownInTheFormOfTheirType()
    {
        Element tree = Version2Tree();

        Assert.AreEqual("PRMT:GEN2-LE", tree.MetaData.AsAsciiString());

        foreach ((string name, ContentForm form, string text) in Version2Values)
        {
            Element holder = tree.Children.Single(member => member.Data.AsUtf8String() == name);
            Element value = holder.Children[1];

            Assert.AreEqual(form, SerializerLayout.DataForm(value, holder), name);
            Assert.AreEqual(text, ContentFormat.Format(value.Data.Span, form), name);
            Assert.IsTrue(ContentFormat.CanShow(value.Data.Span, form), name + ": the text gives back the bytes");
        }
    }

    [TestMethod]
    public void TypeCodesOfVersion2_HaveNames()
    {
        Element tree = Version2Tree();

        foreach ((string name, _, _) in Version2Values)
        {
            Element type = tree.Children.Single(member => member.Data.AsUtf8String() == name).Children[0];

            Assert.AreEqual(ContentForm.SerializerType, SerializerLayout.DataForm(type, type.Parent));
            Assert.IsTrue(ContentFormat.CanShow(type.Data.Span, ContentForm.SerializerType), name);
        }

        Assert.AreEqual("TIMESTAMP", ContentFormat.Format(new byte[] { 0xD3 }, ContentForm.SerializerType));
        CollectionAssert.AreEqual(new byte[] { 0x78 }, ContentFormat.Parse("bigint", ContentForm.SerializerType));
    }

    [TestMethod]
    public void ARootOfVersion2_IsInTheLayout()
    {
        Element root = Version2Tree();

        Assert.AreEqual(ContentForm.SerializerTag, SerializerLayout.MetaDataForm(new Element(), root));
    }

    [TestMethod]
    public void InvalidValues_AreNotShownInTheFormsOfVersion2()
    {
        Assert.IsFalse(ContentFormat.CanShow(new byte[] { 0x7F, 0x00 }, ContentForm.BigInteger), "not the shortest form");
        Assert.IsFalse(ContentFormat.CanShow(BitConverter.GetBytes(TimeSpan.TicksPerDay), ContentForm.TimeOfDay), "out of a day");
        Assert.IsFalse(ContentFormat.CanShow(BitConverter.GetBytes(double.NaN), ContentForm.Double), "the negative NaN of .NET is not the canonical one");
        Assert.ThrowsExactly<FormatException>(() => ContentFormat.Parse("2026-11-22T19:30:15", ContentForm.Timestamp), "a timestamp has an offset");
    }
}
