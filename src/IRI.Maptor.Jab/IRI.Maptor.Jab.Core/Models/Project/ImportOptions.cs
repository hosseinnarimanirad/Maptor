using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Jab.Core.Models.Project;

/// <summary>
/// Union of the per-format import settings needed to replay an import.
/// Properties that do not apply to the format are null.
/// </summary>
public class ImportOptions
{
    /// <summary>
    /// Source SRID (DXF, CSV/TSV, GeoJSON, TopoJSON, worldfile).
    /// </summary>
    public int? SourceSrid { get; set; }

    /// <summary>
    /// CSV/TSV, GeoJSON.
    /// </summary>
    public bool? IsLongitudeFirst { get; set; }

    /// <summary>
    /// CSV/TSV.
    /// </summary>
    public bool? UseFirstLineAsHeader { get; set; }

    /// <summary>
    /// Target geometry type when importing CSV/TSV points as line/polygon.
    /// </summary>
    public GeometryType? GeometryType { get; set; }

    /// <summary>
    /// The pasted content when the data was imported from raw text
    /// (CSV/TSV/GeoJSON/TopoJSON) instead of a file.
    /// </summary>
    public string? RawText { get; set; }
}
