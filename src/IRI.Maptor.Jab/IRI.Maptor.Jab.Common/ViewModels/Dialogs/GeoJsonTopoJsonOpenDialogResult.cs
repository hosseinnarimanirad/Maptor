namespace IRI.Maptor.Jab.Common.ViewModels.Dialogs;

/// <summary>
/// Result of the GeoJSON/TopoJSON open dialog.
/// </summary>
public record GeoJsonTopoJsonOptions(
    bool IsFileSelected,
    string? FilePath,
    string RawJson,
    int SelectedSrid,
    bool IsGeoJson,
    bool IsLongitudeFirst);
