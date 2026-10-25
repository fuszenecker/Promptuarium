using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace PStudio.ViewModels;

/// <summary>
/// A button of a message dialog.
/// </summary>
/// <param name="Text">The caption</param>
/// <param name="Result">The result of the dialog when the button is pressed</param>
/// <param name="IsDefault">Pressed by Enter</param>
/// <param name="IsCancel">Pressed by Escape</param>
public sealed record DialogButton(string Text, int Result, bool IsDefault = false, bool IsCancel = false);

/// <summary>
/// A message with buttons; the dialog closes with the result of the pressed button.
/// </summary>
public sealed partial class MessageDialogViewModel : ViewModelBase
{
    /// <summary>
    /// Creates the view model.
    /// </summary>
    public MessageDialogViewModel(string title, string message, IReadOnlyList<DialogButton> buttons)
    {
        Title = title;
        Message = message;
        Buttons = buttons;
    }

    /// <summary>Carries the result; the dialog closes with it.</summary>
    public event Action<int>? CloseRequested;

    /// <summary>The title of the dialog.</summary>
    public string Title { get; }

    /// <summary>The message.</summary>
    public string Message { get; }

    /// <summary>The buttons, from left to right.</summary>
    public IReadOnlyList<DialogButton> Buttons { get; }

    [RelayCommand]
    private void Press(DialogButton button) => CloseRequested?.Invoke(button.Result);
}
