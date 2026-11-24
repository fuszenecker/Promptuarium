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
}
