using PStudio.Models;
using PStudio.Services;
using Promptuarium;

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
        Assert.AreEqual(ContentForm.Utf8, ContentFormat.Suggest([]));
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
    public void RoundTrips_InEveryFormThatCanShowIt()
    {
        byte[] text = "Promptuarium"u8.ToArray();

        foreach (ContentForm form in Enum.GetValues<ContentForm>().Where(form => ContentFormat.CanShow(text, form)))
        {
            CollectionAssert.AreEqual(text, ContentFormat.Parse(ContentFormat.Format(text, form), form), form.ToString());
        }
    }

    [TestMethod]
    public void TypedForms_UseTheConversionsOfTheDataClass()
    {
        var guid = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 10, 25, 14, 30, 0, TimeSpan.FromHours(2));

        AssertForm(ContentForm.Bool, "true", Data.FromBool(true));
        AssertForm(ContentForm.Byte, "200", Data.FromByte(200));
        AssertForm(ContentForm.Char, "ő", Data.FromChar('ő'));
        AssertForm(ContentForm.Int16, "-2", Data.FromShort(-2));
        AssertForm(ContentForm.UInt16, "65535", Data.FromUShort(65535));
        AssertForm(ContentForm.Int32, "-123456789", Data.FromInt(-123456789));
        AssertForm(ContentForm.UInt32, "4000000000", Data.FromUInt(4_000_000_000));
        AssertForm(ContentForm.Int64, "-9223372036854775808", Data.FromLong(long.MinValue));
        AssertForm(ContentForm.UInt64, "18446744073709551615", Data.FromULong(ulong.MaxValue));
        AssertForm(ContentForm.VarInt, "-12345", Data.FromVarInt(-12345));
        AssertForm(ContentForm.VarUInt, "300", Data.FromVarUInt(300));
        AssertForm(ContentForm.Single, "1.5", Data.FromFloat(1.5f));
        AssertForm(ContentForm.Double, "3.141592653589793", Data.FromDouble(Math.PI));
        AssertForm(ContentForm.Decimal, "-1.978", Data.FromDecimal(-1.978m));
        AssertForm(ContentForm.Guid, guid.ToString(), Data.FromGuid(guid));
        AssertForm(ContentForm.DateTime, "2026-10-25T14:30:00.0000000", Data.FromDateTime(new DateTime(2026, 10, 25, 14, 30, 0)));
        AssertForm(ContentForm.DateTimeOffset, "2026-10-25T14:30:00.0000000+02:00", Data.FromDateTimeOffset(now));
        AssertForm(ContentForm.TimeSpan, "01:30:00", Data.FromTimeSpan(TimeSpan.FromMinutes(90)));
        AssertForm(ContentForm.Utf16, "árvíz", Data.FromUtf16String("árvíz"));
        AssertForm(ContentForm.Utf32, "🌳", Data.FromUtf32String("🌳"));
    }

    [TestMethod]
    public void TypedForms_ShowOnlyContentsOfTheirSize()
    {
        Assert.IsFalse(ContentFormat.CanShow([1, 2], ContentForm.Int32));
        Assert.IsTrue(ContentFormat.CanShow([1, 2, 3, 4], ContentForm.Int32));
        Assert.IsFalse(ContentFormat.CanShow([2], ContentForm.Bool));
        Assert.IsFalse(ContentFormat.CanShow([1, 2, 3], ContentForm.Utf16));
        Assert.IsTrue(ContentFormat.CanShow([], ContentForm.Guid));
        Assert.ThrowsExactly<FormatException>(() => ContentFormat.Parse("256", ContentForm.Byte));
        Assert.ThrowsExactly<FormatException>(() => ContentFormat.Parse("ab", ContentForm.Char));
        Assert.AreEqual(0, ContentFormat.Parse("  ", ContentForm.Int32).Length);
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
    public void SerializerForms_AreTheNamesOfTheCodes()
    {
        AssertForm(ContentForm.SerializerTag, "NAME", new byte[] { 0x00 });
        AssertForm(ContentForm.SerializerTag, "VALUE", new byte[] { 0x03 });
        AssertForm(ContentForm.SerializerTag, "COMMENT", new byte[] { 0x04 });
        AssertForm(ContentForm.SerializerType, "FLOAT64", new byte[] { 0x63 });
        AssertForm(ContentForm.SerializerType, "UTF8_STRING", new byte[] { 0x80 });
        AssertForm(ContentForm.SerializerType, "INT32", new byte[] { 0x42 });
        CollectionAssert.AreEqual(new byte[] { 0xF0 }, ContentFormat.Parse("blob", ContentForm.SerializerType));

        Assert.IsFalse(ContentFormat.CanShow([0x05], ContentForm.SerializerTag));
        Assert.IsFalse(ContentFormat.CanShow([0x02], ContentForm.SerializerType));
        Assert.IsFalse(ContentFormat.CanShow([0x00, 0x00], ContentForm.SerializerTag));
        Assert.ThrowsExactly<FormatException>(() => ContentFormat.Parse("DOUBLE", ContentForm.SerializerType));
    }

    private static void AssertForm(ContentForm form, string text, ReadOnlyMemory<byte> bytes)
    {
        Assert.AreEqual(text, ContentFormat.Format(bytes.Span, form), form.ToString());
        CollectionAssert.AreEqual(bytes.ToArray(), ContentFormat.Parse(text, form), form.ToString());
        Assert.IsTrue(ContentFormat.CanShow(bytes.Span, form), form.ToString());
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
