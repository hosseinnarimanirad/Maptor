using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using IRI.Maptor.Presentation.Wpf.Models;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// An educational tip: a small lightbulb button that opens a pop-up explaining how to use the input
/// next to it: a title, a short text, a list of examples and a footer line. Use it where a tooltip
/// would be too long (a how-to, a list of accepted formats); a tooltip stays a short name or hint.
/// </summary>
/// <remarks>
/// <para>The pop-up opens on click and stays open until the user clicks elsewhere, presses Esc or picks
/// an example, so it can be read at leisure (a tooltip vanishes when the mouse moves).</para>
/// <para>The pop-up follows the UI language's direction even when the InfoTip itself sits in a
/// left-to-right block (a Latin input). Example values are always shown left to right in the Latin face,
/// as their own elements, so bidi cannot reorder them inside a Persian sentence.</para>
/// <para>With <see cref="ExampleCommand"/> set, each example is a button: clicking runs the command with
/// <see cref="InfoTipExample.Value"/> and closes the pop-up (e.g. to fill the input). Without it the
/// examples are plain text.</para>
/// <para>The lightbulb, not an "i": an "i" is the Identify tool's symbol.</para>
/// </remarks>
public partial class InfoTip : UserControl
{
    public InfoTip()
    {
        InitializeComponent();
    }

    #region Dependency properties

    /// <summary>Heading of the pop-up; also the button's tooltip and accessible name.</summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(InfoTip), new PropertyMetadata(null));

    /// <summary>A sentence or two under the title. Optional.</summary>
    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(InfoTip), new PropertyMetadata(null));

    /// <summary>The examples, as <see cref="InfoTipExample"/> items. Optional.</summary>
    public IEnumerable? Examples
    {
        get => (IEnumerable?)GetValue(ExamplesProperty);
        set => SetValue(ExamplesProperty, value);
    }

    public static readonly DependencyProperty ExamplesProperty =
        DependencyProperty.Register(nameof(Examples), typeof(IEnumerable), typeof(InfoTip), new PropertyMetadata(null));

    /// <summary>Run with the example's <see cref="InfoTipExample.Value"/> when an example is clicked. Optional.</summary>
    public ICommand? ExampleCommand
    {
        get => (ICommand?)GetValue(ExampleCommandProperty);
        set => SetValue(ExampleCommandProperty, value);
    }

    public static readonly DependencyProperty ExampleCommandProperty =
        DependencyProperty.Register(nameof(ExampleCommand), typeof(ICommand), typeof(InfoTip), new PropertyMetadata(null));

    /// <summary>A closing note under the examples (e.g. "Persian digits work too"). Optional.</summary>
    public string? Footer
    {
        get => (string?)GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    public static readonly DependencyProperty FooterProperty =
        DependencyProperty.Register(nameof(Footer), typeof(string), typeof(InfoTip), new PropertyMetadata(null));

    /// <summary>Whether the pop-up is open.</summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(InfoTip),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    #endregion

    private void Example_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not InfoTipExample example)
            return;

        var command = ExampleCommand;

        if (command is null || !command.CanExecute(example.Value))
            return;

        IsOpen = false;

        command.Execute(example.Value);
    }

    private void Popup_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        IsOpen = false;
        toggle.Focus();
        e.Handled = true;
    }
}
