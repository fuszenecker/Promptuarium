using Avalonia.Controls;
using Avalonia.Input;
using PStudio.ViewModels;

namespace PStudio.Views;

/// <summary>
/// A row of the serializer view. While it is edited, the keys that its boxes leave unhandled (the arrows, Home, End,
/// Page Up, Page Down, Space, Insert, Delete, +, -, *) stop here: they do not reach the tree, so they neither move its
/// selection nor expand, collapse, add, delete or move its values. The keys with Ctrl or Alt, and the function keys,
/// go on (Ctrl+S, F7).
/// </summary>
public sealed class SerializerRow : Grid
{
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!e.Handled && e.Source != this && DataContext is SerializerNodeViewModel { IsEditing: true } && StopsHere(e))
        {
            e.Handled = true;
        }
    }

    internal static bool StopsHere(KeyEventArgs e)
    {
        return (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) == 0 && e.Key is not (>= Key.F1 and <= Key.F24);
    }
}
