using Promptuarium;
using Promptuarium.Serializer;

namespace Promptuarium.Serializer.Tests;

/// <summary>PromptuariumSerializer.Validate: the structure of a tree, without its signature.</summary>
[TestClass]
public sealed class ValidationTests
{
    public sealed class Sample
    {
        public string Name { get; set; } = "x";

        public int Count { get; set; } = 3;

        public List<double> Values { get; set; } = [1.5, double.NaN];

        public Dictionary<string, bool> Flags { get; set; } = new() { ["on"] = true };

        public Guid Id { get; set; } = Guid.NewGuid();

        public byte[] Blob { get; set; } = [1, 2, 3];

        public Element Nested { get; set; } = new Element(Data.FromUtf8String("n"), ReadOnlyMemory<byte>.Empty);
    }

    [TestMethod]
    public void WrittenTree_IsValid()
    {
        Element root = PromptuariumSerializer.ToElement(new Sample());

        Assert.IsEmpty(PromptuariumSerializer.Validate(root));
        Assert.IsTrue(PromptuariumSerializer.IsValid(root));
    }

    [TestMethod]
    public void EmptyRoot_IsAnEmptyObject()
    {
        Assert.IsTrue(PromptuariumSerializer.IsValid(new Element()));
    }

    [TestMethod]
    public void Signature_IsNotChecked()
    {
        Element root = PromptuariumSerializer.ToElement(new Sample());
        root.MetaData = Data.FromAsciiString("SOMETHING-ELSE");

        Assert.IsTrue(PromptuariumSerializer.IsValid(root));

        root.MetaData = ReadOnlyMemory<byte>.Empty;
        Assert.IsTrue(PromptuariumSerializer.IsValid(root));
    }

    [TestMethod]
    public void RootWithData_IsAnError()
    {
        var root = new Element(Data.FromByte(1), ReadOnlyMemory<byte>.Empty);

        ValidationProblem problem = PromptuariumSerializer.Validate(root).Single();

        Assert.IsTrue(problem.IsError);
        Assert.AreEqual("/", problem.Path);
    }

    [TestMethod]
    public void EveryProblem_IsReported_WithThePathOfItsNode()
    {
        Element root = Nodes.Root(
            Nodes.Member("flag", 0x01, new byte[] { 2 }),
            Nodes.Member("count", 0x42, new byte[] { 1, 2 }),
            new Element(Data.FromUtf8String("raw"), ReadOnlyMemory<byte>.Empty),
            Nodes.Member("type", 0x60, new byte[] { 0 }));

        // Without the warning of the INT32 in a tree signed with a signature of version 1
        List<ValidationProblem> problems = [.. PromptuariumSerializer.Validate(root).Where(problem => problem.IsError)];

        CollectionAssert.AreEqual(new[] { "/0/1", "/1/1", "/2", "/3/0" }, problems.Select(problem => problem.Path).ToArray());
        StringAssert.Contains(problems[0].Message, "BOOLEAN");
        StringAssert.Contains(problems[1].Message, "INT32");
        StringAssert.Contains(problems[3].Message, "reserved");
    }

    [TestMethod]
    public void HolderStructure_IsChecked()
    {
        Element noType = new(Data.FromUtf8String("a"), Data.FromByte(0x00));
        Element twoValues = Nodes.Member("b", 0x80, Data.FromUtf8String("x"));
        twoValues.Add(new Element(Data.FromUtf8String("y"), Data.FromByte(0x03)));
        Element valueInObject = Nodes.Object("c", new Element(Data.FromUtf8String("z"), Data.FromByte(0x03)));
        Element itemInObject = Nodes.Object("d", Nodes.Holder(0x02, ReadOnlyMemory<byte>.Empty, 0x00));

        Element root = Nodes.Root(noType, twoValues, valueInObject, itemInObject);
        List<ValidationProblem> problems = [.. PromptuariumSerializer.Validate(root)];

        CollectionAssert.AreEqual(new[] { "/0", "/1", "/2/1", "/3/1" }, problems.Select(problem => problem.Path).ToArray());
    }

    [TestMethod]
    public void RepeatedName_IsAWarning()
    {
        Element root = Nodes.Root(Nodes.Member("a", 0x00), Nodes.Member("A", 0x00));

        ValidationProblem problem = PromptuariumSerializer.Validate(root).Single();

        Assert.IsFalse(problem.IsError);
        Assert.IsTrue(PromptuariumSerializer.IsValid(root));
    }

    [TestMethod]
    public void Version2Type_UnderAVersion1Signature_IsAWarning()
    {
        Element root = Nodes.Root(Nodes.Member("n", 0x42, new byte[] { 1, 0, 0, 0 }));

        ValidationProblem problem = PromptuariumSerializer.Validate(root).Single();

        Assert.IsFalse(problem.IsError);
        Assert.AreEqual("/0/0", problem.Path);
        StringAssert.Contains(problem.Message, PromptuariumSerializerOptions.DefaultSignature);
    }

    [TestMethod]
    public void Comments_AreValidated()
    {
        Element badComment = new(new byte[] { 0xFF }, Data.FromByte(0x04));
        Element root = Nodes.Root(badComment);

        Assert.AreEqual("/0", PromptuariumSerializer.Validate(root).Single().Path);
    }

    [TestMethod]
    public void BrokenContainer_IsAnError()
    {
        Element root = Nodes.Root(Nodes.Member("tree", 0xF1, new byte[] { 0x60 }));

        Assert.IsFalse(PromptuariumSerializer.IsValid(root));
    }

    [TestMethod]
    public void TooDeep_IsAnError()
    {
        Element inner = Nodes.Member("x", 0x00);

        for (int i = 0; i < 5; i++)
        {
            inner = Nodes.Object("o", inner);
        }

        Element root = Nodes.Root(inner);

        Assert.IsTrue(PromptuariumSerializer.IsValid(root));
        Assert.IsFalse(PromptuariumSerializer.IsValid(root, new PromptuariumSerializerOptions { MaxDepth = 3 }));
    }
}
