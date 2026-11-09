using System.Collections.Generic;
using System.Windows;

namespace IRI.Maptor.Presentation.Wpf.Models;

/// <summary>
/// One row of an <see cref="Controls.InfoTip"/>: a short label in the UI language ("Decimal degrees")
/// and the example itself ("35.6892, 51.3890"). The example is shown left to right in the Latin face,
/// so it reads the same in a Persian UI; when the InfoTip has an ExampleCommand, clicking the row
/// runs it with <see cref="Value"/>.
/// </summary>
/// <remarks>
/// A DependencyObject so a view can declare its examples in XAML with the label bound to a
/// localization key (<c>Label="{Binding [key], Source={StaticResource Localization}}"</c>), which
/// then follows a language switch.
/// </remarks>
public class InfoTipExample : DependencyObject
{
    public InfoTipExample()
    {
    }

    public InfoTipExample(string? label, string value)
    {
        Label = label;
        Value = value;
    }

    /// <summary>What the example is, in the UI language. Optional.</summary>
    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(InfoTipExample), new PropertyMetadata(null));

    /// <summary>The example text; also the ExampleCommand parameter.</summary>
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(InfoTipExample), new PropertyMetadata(string.Empty));
}

/// <summary>A list of <see cref="InfoTipExample"/> that XAML can create for <c>InfoTip.Examples</c>.</summary>
public class InfoTipExampleCollection : List<InfoTipExample>
{
}
