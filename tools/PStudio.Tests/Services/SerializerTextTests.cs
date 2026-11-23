using PStudio.Services;
using Promptuarium;
using Promptuarium.Serializer;

namespace PStudio.Tests.Services;

[TestClass]
public sealed class SerializerTextTests
{
    [PromptuariumComment("The settings")]
    public sealed class Sample
    {
        public string Title { get; set; } = "demo";

        public string Number { get; set; } = "42";

        public int Count { get; set; } = 3;

        public bool Enabled { get; set; } = true;

        public string? Missing { get; set; }

        public List<string> Tags { get; set; } = ["a", "b"];

        public List<int> Empty { get; set; } = [];

        public double Rate { get; set; } = double.NaN;

        public DateTimeOffset Since { get; set; } = new(2026, 11, 22, 19, 30, 15, TimeSpan.FromHours(1));

        public byte[] Blob { get; set; } = [1, 2, 3];
    }

    [TestMethod]
    public void Yaml_KeepsTheTypes_AndTheComments()
    {
        string yaml = SerializerText.Yaml(PromptuariumSerializer.ToElement(new Sample()));

        Assert.AreEqual("""
            # The settings
            Title: demo
            Number: "42"
            Count: 3
            Enabled: true
            Missing: null
            Tags:
              - a
              - b
            Empty: []
            Rate: !float64 "NaN"
            Since: !timestamp "2026-11-22T19:30:15+01:00"
            Blob: !!binary AQID
            """, yaml);
    }

    [TestMethod]
    public void Json_HasNumbers_AndTexts()
    {
        string json = SerializerText.Json(PromptuariumSerializer.ToElement(new Sample()));

        StringAssert.Contains(json, "\"Count\": 3,");
        StringAssert.Contains(json, "\"Number\": \"42\",");
        StringAssert.Contains(json, "\"Rate\": \"NaN\",");
        StringAssert.Contains(json, "\"Blob\": \"AQID\"");
        StringAssert.Contains(json, "\"Empty\": []");
    }

    [TestMethod]
    public void EmptyTree_IsAnEmptyObject()
    {
        Assert.AreEqual("{}", SerializerText.Yaml(new Element()));
        Assert.AreEqual("{}", SerializerText.Json(new Element()));
    }
}
