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
}
