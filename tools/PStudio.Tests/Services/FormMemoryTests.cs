using Promptuarium;
using Promptuarium.Serializer;
using PStudio.Models;
using PStudio.Services;

namespace PStudio.Tests.Services;

[TestClass]
public sealed class FormMemoryTests
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "pstudio-forms-" + Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(directory, "PStudio", "forms.prmt");

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void RememberedForms_AreRecalledByANewMemory()
    {
        var forms = new FileForms();
        forms.Data["/"] = ContentForm.Utf8;
        forms.Data["/0/2"] = ContentForm.Int32;
        forms.MetaData["/0"] = ContentForm.Guid;

        new FormMemory(SettingsPath).Remember("tree.prmt", forms);
        FileForms recalled = new FormMemory(SettingsPath).Recall(Path.GetFullPath("tree.prmt"));

        Assert.AreEqual(ContentForm.Int32, recalled.Data["/0/2"]);
        Assert.AreEqual(ContentForm.Utf8, recalled.Data["/"]);
        Assert.AreEqual(ContentForm.Guid, recalled.MetaData["/0"]);
        Assert.IsTrue(new FormMemory(SettingsPath).Recall("other.prmt").IsEmpty);
    }

    [TestMethod]
    public void TheForms_OfNestedTrees_AreRecalledByANewMemory()
    {
        var deeper = new FileForms();
        deeper.MetaData["/1"] = ContentForm.Base64;
        var nested = new FileForms();
        nested.Data["/0"] = ContentForm.Hex;
        nested.Nested["/0"] = deeper;
        var forms = new FileForms();
        forms.Nested["/2"] = nested;
        Assert.IsFalse(forms.IsEmpty);

        new FormMemory(SettingsPath).Remember("tree.prmt", forms);
        FileForms recalled = new FormMemory(SettingsPath).Recall("tree.prmt");

        Assert.AreEqual(ContentForm.Hex, recalled.Nested["/2"].Data["/0"]);
        Assert.AreEqual(ContentForm.Base64, recalled.Nested["/2"].Nested["/0"].MetaData["/1"]);
        Assert.AreEqual(0, recalled.Data.Count);
    }

    [TestMethod]
    public void Clone_IsDeep()
    {
        var nested = new FileForms();
        nested.Data["/0"] = ContentForm.Hex;
        var forms = new FileForms();
        forms.Data["/"] = ContentForm.Utf8;
        forms.MetaData["/0"] = ContentForm.Guid;
        forms.Nested["/1"] = nested;

        FileForms copy = forms.Clone();
        nested.Data["/0"] = ContentForm.Base64;
        forms.Data.Clear();

        Assert.AreEqual(ContentForm.Utf8, copy.Data["/"]);
        Assert.AreEqual(ContentForm.Guid, copy.MetaData["/0"]);
        Assert.AreEqual(ContentForm.Hex, copy.Nested["/1"].Data["/0"]);
    }

    [TestMethod]
    public void AFileOfAnEarlierVersion_WithoutNestedTrees_IsStillRead()
    {
        var old = new OldMemory();
        old.Files[Path.GetFullPath("tree.prmt")] = new OldFileForms();
        old.Files[Path.GetFullPath("tree.prmt")].Data["/0"] = ContentForm.Int32;
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllBytes(SettingsPath, PromptuariumSerializer.Serialize(old, new PromptuariumSerializerOptions { Signature = FormMemory.Signature }));

        FileForms recalled = new FormMemory(SettingsPath).Recall("tree.prmt");

        Assert.AreEqual(ContentForm.Int32, recalled.Data["/0"]);
        Assert.AreEqual(0, recalled.Nested.Count);
    }

    [TestMethod]
    public void TheFile_HasTheSignature()
    {
        var forms = new FileForms();
        forms.Data["/"] = ContentForm.Hex;

        new FormMemory(SettingsPath).Remember("tree.prmt", forms);

        Assert.AreEqual(FormMemory.Signature, Element.FromBytes(File.ReadAllBytes(SettingsPath)).MetaData.AsAsciiString());
        Assert.AreEqual("PRMT:PSTNT1", FormMemory.Signature);
    }

    [TestMethod]
    public void TheFile_HoldsOnlyTheTypesOfVersion1()
    {
        var forms = new FileForms();
        forms.Data["/"] = ContentForm.Hex;

        new FormMemory(SettingsPath).Remember("tree.prmt", forms);

        // The earlier versions of PStudio read it: the time is text, not a TIMESTAMP of version 2
        var pending = new Stack<Element>([Element.FromBytes(File.ReadAllBytes(SettingsPath))]);

        while (pending.Count > 0)
        {
            Element node = pending.Pop();

            if (node.MetaData.Length == 1 && node.MetaData.Span[0] == 0x01)
            {
                CollectionAssert.Contains(new byte[] { 0x00, 0x01, 0x20, 0x21, 0x63, 0x80, 0xF0 }, node.Data.Span[0]);
            }

            foreach (Element child in node.Children)
            {
                pending.Push(child);
            }
        }
    }

    [TestMethod]
    public void ABrokenFile_IsAnEmptyMemory()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "not a tree");

        Assert.IsTrue(new FormMemory(SettingsPath).Recall("tree.prmt").IsEmpty);
    }

    [TestMethod]
    public void EmptyForms_ForgetTheFile()
    {
        var forms = new FileForms();
        forms.Data["/"] = ContentForm.Hex;
        var memory = new FormMemory(SettingsPath);

        memory.Remember("tree.prmt", forms);
        memory.Remember("tree.prmt", new FileForms());

        Assert.IsTrue(new FormMemory(SettingsPath).Recall("tree.prmt").IsEmpty);
    }

    [TestMethod]
    public void TheLeastRecentlyUsedFiles_AreForgotten()
    {
        var memory = new FormMemory(SettingsPath);

        for (int i = 0; i <= FormMemory.MaxFiles; i++)
        {
            var forms = new FileForms();
            forms.Data["/"] = ContentForm.Hex;
            memory.Remember($"tree{i}.prmt", forms);
        }

        var recalled = new FormMemory(SettingsPath);
        Assert.IsTrue(recalled.Recall("tree0.prmt").IsEmpty);
        Assert.IsFalse(recalled.Recall($"tree{FormMemory.MaxFiles}.prmt").IsEmpty);
    }

    // The file as 8.2.1 and earlier wrote it
    private sealed class OldMemory
    {
        public Dictionary<string, OldFileForms> Files { get; } = new(StringComparer.Ordinal);
    }

    // The earlier versions wrote the time as text (version 1 of the layout)
    private sealed class OldFileForms
    {
#pragma warning disable PRMTSER001
        [PromptuariumType(PromptuariumType.Utf8String)]
#pragma warning restore PRMTSER001
        public DateTime LastUsed { get; set; }

        public Dictionary<string, ContentForm> Data { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, ContentForm> MetaData { get; } = new(StringComparer.Ordinal);
    }
}
