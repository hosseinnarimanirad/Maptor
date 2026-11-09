using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Dialogs;

/// <summary>
/// What the import options dialog hands to the reader when the user presses Open.
/// </summary>
/// <param name="Kind">The format, as detected or as the user changed it.</param>
/// <param name="FilePath">The file, or null for pasted text.</param>
/// <param name="RawText">The pasted text (CSV/TSV with space-separated XY already delimited), or null for a file.</param>
/// <param name="SourceSrid">The CRS of the coordinates.</param>
/// <param name="IsLongitudeFirst">X,Y (true) or Y,X order; used by GeoJSON and CSV/TSV.</param>
/// <param name="UseFirstLineAsHeader">CSV/TSV: the first line holds the column names.</param>
/// <param name="GeometryType">CSV/TSV: what the points make (point, polyline, polygon).</param>
public record ImportOptionsDialogResult(
    DataSourceKind Kind,
    string? FilePath,
    string? RawText,
    int SourceSrid,
    bool IsLongitudeFirst,
    bool UseFirstLineAsHeader,
    GeometryType GeometryType);
