using Avalonia.Controls;
using PStudio.ViewModels;

namespace PStudio.Views;

/// <summary>
/// A message with buttons.
/// </summary>
public partial class MessageDialog : Window
{
    /// <summary>
    /// Creates the dialog (for the designer).
    /// </summary>
    public MessageDialog()
        : this(new MessageDialogViewModel("Message", string.Empty, [new DialogButton("OK", 0, IsDefault: true)]))
    {
    }

    /// <summary>
    /// Creates the dialog; it closes with the result of the pressed button.
    /// </summary>
    public MessageDialog(MessageDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += result => Close(result);
    }
}
