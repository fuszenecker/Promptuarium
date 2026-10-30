using Promptuarium;
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
    public void TheFile_HasTheSignature()
    {
        var forms = new FileForms();
        forms.Data["/"] = ContentForm.Hex;

        new FormMemory(SettingsPath).Remember("tree.prmt", forms);

        Assert.AreEqual(FormMemory.Signature, Element.FromBytes(File.ReadAllBytes(SettingsPath)).MetaData.AsAsciiString());
        Assert.AreEqual("PRMT:PStNT1", FormMemory.Signature);
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
}
