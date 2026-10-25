using PStudio.Models;
using PStudio.Services;

namespace PStudio.Tests.Services;

[TestClass]
public sealed class ContentFormatTests
{
    [TestMethod]
    public void Suggest_PrintableTextIsUtf8_OtherBytesAreHex()
    {
        Assert.AreEqual(ContentForm.Utf8, ContentFormat.Suggest("árvíztűrő\ttükör\n"u8));
        Assert.AreEqual(ContentForm.Hex, ContentFormat.Suggest([0x00, 0x41]));
        Assert.AreEqual(ContentForm.Hex, ContentFormat.Suggest([0xC3]));
        Assert.AreEqual(ContentForm.Hex, ContentFormat.Suggest([]));
    }

    [TestMethod]
    public void Hex_Has16BytesPerLine_AndParsesWithSeparators()
    {
        byte[] bytes = Enumerable.Range(0, 17).Select(i => (byte)i).ToArray();

        string text = ContentFormat.Format(bytes, ContentForm.Hex);

        Assert.AreEqual("00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F\n10", text);
        CollectionAssert.AreEqual(bytes, ContentFormat.Parse(text, ContentForm.Hex));
        CollectionAssert.AreEqual(new byte[] { 0xAB, 0xCD }, ContentFormat.Parse("ab-cd", ContentForm.Hex));
    }

    [TestMethod]
    public void RoundTrips_InEveryForm()
    {
        byte[] text = "Promptuarium"u8.ToArray();

        foreach (ContentForm form in Enum.GetValues<ContentForm>())
        {
            CollectionAssert.AreEqual(text, ContentFormat.Parse(ContentFormat.Format(text, form), form), form.ToString());
        }
    }

    [TestMethod]
    public void Parse_RejectsInvalidText()
    {
        Assert.ThrowsExactly<FormatException>(() => ContentFormat.Parse("ABC", ContentForm.Hex));
        Assert.ThrowsExactly<FormatException>(() => ContentFormat.Parse("ő", ContentForm.Ascii));
        Assert.ThrowsExactly<FormatException>(() => ContentFormat.Parse("!!", ContentForm.Base64));
    }

    [TestMethod]
    public void CanShow_FalseIfTheFormLosesBytes()
    {
        Assert.IsFalse(ContentFormat.CanShow([0xFF], ContentForm.Utf8));
        Assert.IsFalse(ContentFormat.CanShow([0xC3, 0xA1], ContentForm.Ascii));
        Assert.IsTrue(ContentFormat.CanShow([0xC3, 0xA1], ContentForm.Utf8));
        Assert.IsTrue(ContentFormat.CanShow([0xFF], ContentForm.Hex));
        Assert.IsTrue(ContentFormat.CanShow([0xFF], ContentForm.Base64));
    }

    [TestMethod]
    public void Preview_IsShort_AndDoesNotSplitCharacters()
    {
        Assert.AreEqual(string.Empty, ContentFormat.Preview([]));
        Assert.AreEqual("\"abc\"", ContentFormat.Preview("abc"u8));
        Assert.AreEqual("0001", ContentFormat.Preview([0, 1]));
        Assert.AreEqual("\"ab…\"", ContentFormat.Preview("abcdef"u8, 2));

        // 'á' is 2 bytes: a cut after its first byte drops it
        Assert.AreEqual("\"a…\"", ContentFormat.Preview("aáb"u8, 2));
    }
}
