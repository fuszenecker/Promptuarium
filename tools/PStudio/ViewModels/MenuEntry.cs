using System.Windows.Input;

namespace PStudio.ViewModels;

/// <summary>
/// An item of a menu built by the view model (e.g. File / Open Recent); the header "-" is a separator.
/// </summary>
/// <param name="Header">The text (an underscore marks the access key).</param>
/// <param name="Command">The command.</param>
/// <param name="Parameter">The parameter of the command.</param>
public sealed record MenuEntry(string Header, ICommand? Command = null, object? Parameter = null);
