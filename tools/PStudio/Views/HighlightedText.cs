using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using PStudio.Services;

namespace PStudio.Views;

/// <summary>
/// The YAML or the JSON of the tree, colored, selectable: its pieces are runs with the class of their kind (key,
/// string, number, keyword, tag, comment, punctuation), colored by the styles of the window.
/// </summary>
public sealed class HighlightedText : SelectableTextBlock
{
    public static readonly StyledProperty<string?> SourceProperty = AvaloniaProperty.Register<HighlightedText, string?>(nameof(Source));

    public static readonly StyledProperty<bool> IsYamlProperty = AvaloniaProperty.Register<HighlightedText, bool>(nameof(IsYaml), true);

    /// <summary>The text to show.</summary>
    public string? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>Whether the text is YAML (otherwise JSON).</summary>
    public bool IsYaml
    {
        get => GetValue(IsYamlProperty);
        set => SetValue(IsYamlProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(SelectableTextBlock);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceProperty || change.Property == IsYamlProperty)
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        var inlines = new InlineCollection();
        string text = Source ?? string.Empty;

        foreach (SyntaxSpan span in IsYaml ? SyntaxHighlighter.Yaml(text) : SyntaxHighlighter.Json(text))
        {
            var run = new Run(span.Text);

            if (span.Kind != SyntaxKind.Plain)
            {
                run.Classes.Add(span.Kind.ToString().ToLowerInvariant());
            }

            inlines.Add(run);
        }

        Inlines = inlines;
    }
}
