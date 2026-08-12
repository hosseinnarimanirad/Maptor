namespace IRI.Maptor.Jab.Wpf.Models.DxfOpenDialog;

/// <summary>
/// Represents a spatial reference system option for the DXF open dialog.
/// </summary>
public class SrsOption
{
    /// <summary>
    /// Fixed SRID when not UTM. Null when IsUtm is true.
    /// </summary>
    public int? FixedSrid { get; init; }

    /// <summary>
    /// Display name for the combobox.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// When true, user inputs zone and SRID is computed from zone + hemisphere.
    /// </summary>
    public bool IsUtm { get; init; }
}
