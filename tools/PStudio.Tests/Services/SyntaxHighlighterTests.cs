using PStudio.Services;

namespace PStudio.Tests.Services;

[TestClass]
public sealed class SyntaxHighlighterTests
{
    private static SyntaxKind KindOf(IReadOnlyList<SyntaxSpan> spans, string text) => spans.Single(span => span.Text == text).Kind;

    [TestMethod]
    public void Yaml_ColorsKeysValuesTagsAndComments()
    {
        const string yaml = "# The settings\nTitle: demo\n\"a: b\": \"x\"\nCount: 3\nEnabled: true\nTags:\n  - a\n  - null\nSince: !timestamp \"2026\"\nEmpty: {}";
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Yaml(yaml);

        Assert.AreEqual(yaml, string.Concat(spans.Select(span => span.Text)));
        Assert.AreEqual(SyntaxKind.Comment, KindOf(spans, "# The settings"));
        Assert.AreEqual(SyntaxKind.Key, KindOf(spans, "Title"));
        Assert.AreEqual(SyntaxKind.String, KindOf(spans, "demo"));
        Assert.AreEqual(SyntaxKind.Key, KindOf(spans, "\"a: b\""));
        Assert.AreEqual(SyntaxKind.String, KindOf(spans, "\"x\""));
        Assert.AreEqual(SyntaxKind.Number, KindOf(spans, "3"));
        Assert.AreEqual(SyntaxKind.Keyword, KindOf(spans, "true"));
        Assert.AreEqual(SyntaxKind.String, KindOf(spans, "a"));
        Assert.AreEqual(SyntaxKind.Keyword, KindOf(spans, "null"));
        Assert.AreEqual(SyntaxKind.Tag, KindOf(spans, "!timestamp"));
        Assert.AreEqual(SyntaxKind.String, KindOf(spans, "\"2026\""));
    }

    [TestMethod]
    public void Json_ColorsKeysAndValues()
    {
        const string json = "{\n  \"Title\": \"demo\",\n  \"Count\": -3.5,\n  \"On\": false,\n  \"Tags\": [\n    \"a\\\"b\"\n  ]\n}";
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Json(json);

        Assert.AreEqual(json, string.Concat(spans.Select(span => span.Text)));
        Assert.AreEqual(SyntaxKind.Key, KindOf(spans, "\"Title\""));
        Assert.AreEqual(SyntaxKind.String, KindOf(spans, "\"demo\""));
        Assert.AreEqual(SyntaxKind.Number, KindOf(spans, "-3.5"));
        Assert.AreEqual(SyntaxKind.Keyword, KindOf(spans, "false"));
        Assert.AreEqual(SyntaxKind.String, KindOf(spans, "\"a\\\"b\""));
    }

    private static string Concat(IReadOnlyList<SyntaxSpan> spans) => string.Concat(spans.Select(span => span.Text));

    [TestMethod]
    public void EmptyText_HasNoPieces()
    {
        Assert.AreEqual(0, SyntaxHighlighter.Yaml(string.Empty).Count);
        Assert.AreEqual(0, SyntaxHighlighter.Json(string.Empty).Count);
    }

    [TestMethod]
    public void NullText_IsRefused()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => SyntaxHighlighter.Yaml(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => SyntaxHighlighter.Json(null!));
    }

    [TestMethod]
    public void LineEndings_AreNewLines()
    {
        Assert.AreEqual("a: 1\nb: 2", Concat(SyntaxHighlighter.Yaml("a: 1\r\nb: 2")));
        Assert.AreEqual("{\n}", Concat(SyntaxHighlighter.Json("{\r\n}")));
    }

    [TestMethod]
    public void AdjacentPiecesOfAKind_AreMerged()
    {
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Yaml("# a\n# b");

        Assert.AreEqual(3, spans.Count);
        Assert.AreEqual(new SyntaxSpan("# a", SyntaxKind.Comment), spans[0]);
        Assert.AreEqual(new SyntaxSpan("\n", SyntaxKind.Plain), spans[1]);
        Assert.AreEqual(new SyntaxSpan("# b", SyntaxKind.Comment), spans[2]);

        for (int i = 1; i < spans.Count; i++)
        {
            Assert.AreNotEqual(spans[i - 1].Kind, spans[i].Kind);
        }
    }

    [TestMethod]
    public void NoPieceIsEmpty()
    {
        foreach (string text in new[] { "a:\n  - \n  -\n", "\n\n", "{}", "  # x", "- - 1" })
        {
            Assert.IsTrue(SyntaxHighlighter.Yaml(text).All(span => span.Text.Length > 0), text);
            Assert.IsTrue(SyntaxHighlighter.Json(text).All(span => span.Text.Length > 0), text);
        }
    }

    [TestMethod]
    [DataRow("x: 0", "0", SyntaxKind.Number)]
    [DataRow("x: -5", "-5", SyntaxKind.Number)]
    [DataRow("x: 1.5e-3", "1.5e-3", SyntaxKind.Number)]
    [DataRow("x: .5", ".5", SyntaxKind.Number)]
    [DataRow("x: +7", "+7", SyntaxKind.Number)]
    [DataRow("x: true", "true", SyntaxKind.Keyword)]
    [DataRow("x: false", "false", SyntaxKind.Keyword)]
    [DataRow("x: null", "null", SyntaxKind.Keyword)]
    [DataRow("x: {}", "{}", SyntaxKind.Punctuation)]
    [DataRow("x: []", "[]", SyntaxKind.Punctuation)]
    [DataRow("x: hello world", "hello world", SyntaxKind.String)]
    [DataRow("x: \"42\"", "\"42\"", SyntaxKind.String)]
    [DataRow("x: \"a\\\"b\"", "\"a\\\"b\"", SyntaxKind.String)]
    [DataRow("x: !!binary AAEC", "!!binary", SyntaxKind.Tag)]
    [DataRow("x: !!binary AAEC", "AAEC", SyntaxKind.String)]
    [DataRow("x: !container 00FF", "!container", SyntaxKind.Tag)]
    [DataRow("x: !uuid \"0190\"", "!uuid", SyntaxKind.Tag)]
    [DataRow("x: -", "-", SyntaxKind.String)]
    public void YamlValues(string line, string piece, SyntaxKind kind)
    {
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Yaml(line);

        Assert.AreEqual(line, Concat(spans));
        Assert.AreEqual(SyntaxKind.Key, spans[0].Kind);
        Assert.AreEqual("x", spans[0].Text);
        Assert.AreEqual(kind, KindOf(spans, piece));
    }

    [TestMethod]
    public void YamlKey_ThatIsQuoted_MayHaveAColon()
    {
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Yaml("\"a: \\\"b\": 1");

        Assert.AreEqual(new SyntaxSpan("\"a: \\\"b\"", SyntaxKind.Key), spans[0]);
        Assert.AreEqual(SyntaxKind.Number, KindOf(spans, "1"));
    }

    [TestMethod]
    public void YamlKey_OfANestedValue_EndsTheLine()
    {
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Yaml("  Audio:");

        CollectionAssert.AreEqual(
            new[] { new SyntaxSpan("  ", SyntaxKind.Plain), new SyntaxSpan("Audio", SyntaxKind.Key), new SyntaxSpan(":", SyntaxKind.Punctuation) },
            spans.ToArray());
    }

    [TestMethod]
    public void YamlElements_HaveTheirDash()
    {
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Yaml("  - -5");

        CollectionAssert.AreEqual(
            new[] { new SyntaxSpan("  ", SyntaxKind.Plain), new SyntaxSpan("-", SyntaxKind.Punctuation), new SyntaxSpan(" ", SyntaxKind.Plain), new SyntaxSpan("-5", SyntaxKind.Number) },
            spans.ToArray());
    }

    [TestMethod]
    public void YamlElement_OfANestedValue_IsADashAlone()
    {
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Yaml("-");

        CollectionAssert.AreEqual(new[] { new SyntaxSpan("-", SyntaxKind.Punctuation) }, spans.ToArray());
    }

    [TestMethod]
    public void YamlElement_ThatIsAMember_HasItsKey()
    {
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Yaml("- name: x");

        Assert.AreEqual(SyntaxKind.Punctuation, spans[0].Kind);
        Assert.AreEqual(SyntaxKind.Key, KindOf(spans, "name"));
        Assert.AreEqual(SyntaxKind.String, KindOf(spans, "x"));
    }

    [TestMethod]
    public void YamlComment_IsIndented_AndMayLookLikeAMember()
    {
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Yaml("    # key: value");

        CollectionAssert.AreEqual(new[] { new SyntaxSpan("    ", SyntaxKind.Plain), new SyntaxSpan("# key: value", SyntaxKind.Comment) }, spans.ToArray());
    }

    [TestMethod]
    public void YamlEmptyComment_IsAComment()
    {
        Assert.AreEqual(SyntaxKind.Comment, SyntaxHighlighter.Yaml("#").Single().Kind);
    }

    [TestMethod]
    public void YamlEmptyObject_IsPunctuation()
    {
        Assert.AreEqual(new SyntaxSpan("{}", SyntaxKind.Punctuation), SyntaxHighlighter.Yaml("{}").Single());
    }

    [TestMethod]
    public void YamlValue_WithAColonInIt_IsNotAKey()
    {
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Yaml("t: \"12:30\"");

        Assert.AreEqual(SyntaxKind.Key, KindOf(spans, "t"));
        Assert.AreEqual(SyntaxKind.String, KindOf(spans, "\"12:30\""));
    }

    [TestMethod]
    public void YamlTimestamp_OfATag_IsNotAKey()
    {
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Yaml("- !time \"12:30:00\"");

        Assert.AreEqual(SyntaxKind.Tag, KindOf(spans, "!time"));
        Assert.IsFalse(spans.Any(span => span.Kind == SyntaxKind.Key));
    }

    [TestMethod]
    public void JsonPunctuation_AndEmptyStructures()
    {
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Json("{\"a\": {}, \"b\": []}");

        Assert.AreEqual(SyntaxKind.Key, KindOf(spans, "\"a\""));
        Assert.AreEqual(SyntaxKind.Key, KindOf(spans, "\"b\""));
        Assert.IsTrue(spans.Where(span => span.Text.Trim().Length > 0 && span.Kind != SyntaxKind.Key).All(span => span.Kind == SyntaxKind.Punctuation));
    }

    [TestMethod]
    public void JsonKey_WithSpacesBeforeItsColon_IsAKey()
    {
        Assert.AreEqual(SyntaxKind.Key, KindOf(SyntaxHighlighter.Json("\"k\"   : 1"), "\"k\""));
    }

    [TestMethod]
    public void JsonString_WithAColonOrAnEscapedBackslash()
    {
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Json("[\"a:b\", \"c\\\\\", 1e-5, -0]");

        Assert.AreEqual(SyntaxKind.String, KindOf(spans, "\"a:b\""));
        Assert.AreEqual(SyntaxKind.String, KindOf(spans, "\"c\\\\\""));
        Assert.AreEqual(SyntaxKind.Number, KindOf(spans, "1e-5"));
        Assert.AreEqual(SyntaxKind.Number, KindOf(spans, "-0"));
    }

    [TestMethod]
    public void JsonUnknownWord_IsPlain()
    {
        Assert.AreEqual(SyntaxKind.Plain, KindOf(SyntaxHighlighter.Json("[nope]"), "nope"));
    }

    [TestMethod]
    public void JsonUnclosedString_RunsToTheEndOfTheLine()
    {
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Json("\"abc\n1");

        Assert.AreEqual(new SyntaxSpan("\"abc", SyntaxKind.String), spans[0]);
        Assert.AreEqual(SyntaxKind.Number, spans[^1].Kind);
    }

    [TestMethod]
    public void YamlOfATree_HasEveryMemberAsAKey()
    {
        var tree = Promptuarium.Serializer.PromptuariumSerializer.ToElement(new SerializerTextTests.Sample());
        string yaml = SerializerText.Yaml(tree);
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Yaml(yaml);

        Assert.AreEqual(yaml, Concat(spans));
        string[] keys = spans.Where(span => span.Kind == SyntaxKind.Key).Select(span => span.Text).ToArray();
        CollectionAssert.IsSubsetOf(new[] { "Title", "Number", "Count", "Enabled" }, keys);
        Assert.AreEqual(SyntaxKind.String, KindOf(spans, "\"42\""));
        Assert.IsTrue(spans.Any(span => span.Kind == SyntaxKind.Comment && span.Text.Contains("The settings")));
    }

    [TestMethod]
    public void JsonOfATree_HasEveryMemberAsAKey()
    {
        var tree = Promptuarium.Serializer.PromptuariumSerializer.ToElement(new SerializerTextTests.Sample());
        string json = SerializerText.Json(tree);
        IReadOnlyList<SyntaxSpan> spans = SyntaxHighlighter.Json(json);

        Assert.AreEqual(json, Concat(spans));
        CollectionAssert.IsSubsetOf(new[] { "\"Title\"", "\"Number\"", "\"Count\"", "\"Enabled\"" }, spans.Where(span => span.Kind == SyntaxKind.Key).Select(span => span.Text).ToArray());
        Assert.AreEqual(SyntaxKind.Keyword, KindOf(spans, "true"));
    }
}
