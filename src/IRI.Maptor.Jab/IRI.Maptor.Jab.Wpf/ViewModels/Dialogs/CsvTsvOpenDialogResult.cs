using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Jab.Wpf.ViewModels.Dialogs;

/// <summary>
/// Result of the CSV/TSV open dialog.
/// </summary>
/// <param name="FilePath">File path when a file was selected, or null when data was pasted.</param>
/// <param name="RawText">File content or pasted text to import.</param>
/// <param name="SelectedSrid">Selected spatial reference system ID.</param>
/// <param name="IsCsv">True for CSV (comma-delimited), false for TSV (tab-delimited).</param>
/// <param name="IsLongitudeFirst">True for X,Y (longitude,latitude) order, false for Y,X.</param>
/// <param name="UseFirstLineAsHeader">True if first line contains attribute names.</param>
public record CsvTsvOptions(
    bool IsFileSelected,
    string? FilePath,
    string RawText,
    int SelectedSrid,
    bool IsCsv,
    bool IsLongitudeFirst,
    bool UseFirstLineAsHeader,
    GeometryType GeometryType);
