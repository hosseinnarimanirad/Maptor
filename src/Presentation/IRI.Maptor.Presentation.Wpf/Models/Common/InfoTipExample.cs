namespace IRI.Maptor.Presentation.Wpf.Models;

/// <summary>
/// One row of an <see cref="Controls.InfoTip"/>: a short label in the UI language ("Decimal degrees")
/// and the example itself ("35.6892, 51.3890"). The example is shown left to right in the Latin face,
/// so it reads the same in a Persian UI; when the InfoTip has an ExampleCommand, clicking the row
/// runs it with <see cref="Value"/>.
/// </summary>
public class InfoTipExample
{
    public InfoTipExample()
    {
    }

    public InfoTipExample(string label, string value)
    {
        Label = label;
        Value = value;
    }

    /// <summary>What the example is, in the UI language. Optional.</summary>
    public string? Label { get; set; }

    /// <summary>The example text; also the ExampleCommand parameter.</summary>
    public string Value { get; set; } = string.Empty;
}
