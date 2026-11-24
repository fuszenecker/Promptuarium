using Avalonia.Input;
using PStudio.Views;

namespace PStudio.Tests.Views;

/// <summary>The keys that stop in an edited row of the serializer view, and the ones that go on to the window.</summary>
[TestClass]
public sealed class SerializerRowTests
{
    [TestMethod]
    [DataRow(Key.Up)]
    [DataRow(Key.Down)]
    [DataRow(Key.Left)]
    [DataRow(Key.Right)]
    [DataRow(Key.Home)]
    [DataRow(Key.End)]
    [DataRow(Key.PageUp)]
    [DataRow(Key.PageDown)]
    [DataRow(Key.Space)]
    [DataRow(Key.Insert)]
    [DataRow(Key.Delete)]
    [DataRow(Key.Add)]
    [DataRow(Key.Subtract)]
    [DataRow(Key.Multiply)]
    [DataRow(Key.A)]
    public void KeysWithoutModifiers_StopInTheRow(Key key)
    {
        Assert.IsTrue(SerializerRow.StopsHere(new KeyEventArgs { Key = key }));
        Assert.IsTrue(SerializerRow.StopsHere(new KeyEventArgs { Key = key, KeyModifiers = KeyModifiers.Shift }));
    }

    [TestMethod]
    [DataRow(Key.S, KeyModifiers.Control)]
    [DataRow(Key.S, KeyModifiers.Control | KeyModifiers.Shift)]
    [DataRow(Key.O, KeyModifiers.Control)]
    [DataRow(Key.Up, KeyModifiers.Alt)]
    [DataRow(Key.Down, KeyModifiers.Meta)]
    [DataRow(Key.F1, KeyModifiers.None)]
    [DataRow(Key.F6, KeyModifiers.None)]
    [DataRow(Key.F7, KeyModifiers.None)]
    [DataRow(Key.F24, KeyModifiers.Shift)]
    public void KeysWithCtrlOrAlt_AndTheFunctionKeys_GoOn(Key key, KeyModifiers modifiers)
    {
        Assert.IsFalse(SerializerRow.StopsHere(new KeyEventArgs { Key = key, KeyModifiers = modifiers }));
    }
}
