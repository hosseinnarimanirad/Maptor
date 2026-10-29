namespace IRI.Maptor.Core.FileGdb.Table;

/// <summary>
/// Spatial reference, coordinate quantization grid and extent of a geometry (or raster) field.
/// Stored coordinates are integers; the real value is <c>stored / scale + origin</c>.
/// </summary>
internal class GdbSpatialFieldInfo
{
    /// <summary>Esri WKT of the spatial reference, or <c>null</c> when the field has none.</summary>
    public string? Wkt { get; set; }

    public bool HasZ { get; set; }

    public bool HasM { get; set; }

    public double XOrigin { get; set; }

    public double YOrigin { get; set; }

    public double XYScale { get; set; }

    public double ZOrigin { get; set; }

    public double ZScale { get; set; }

    public double MOrigin { get; set; }

    public double MScale { get; set; }

    public double XYTolerance { get; set; }

    public double ZTolerance { get; set; }

    public double MTolerance { get; set; }
}

internal sealed class GdbGeometryFieldInfo : GdbSpatialFieldInfo
{
    /// <summary>Layer extent; any value may be NaN for an empty layer.</summary>
    public double XMin { get; set; }

    public double YMin { get; set; }

    public double XMax { get; set; }

    public double YMax { get; set; }

    public double ZMin { get; set; } = double.NaN;

    public double ZMax { get; set; } = double.NaN;

    public double MMin { get; set; } = double.NaN;

    public double MMax { get; set; } = double.NaN;

    /// <summary>Spatial index grid sizes (1 to 3 levels).</summary>
    public IReadOnlyList<double> SpatialIndexGridSizes { get; set; } = Array.Empty<double>();
}

internal sealed class GdbRasterFieldInfo : GdbSpatialFieldInfo
{
    public string RasterColumnName { get; set; } = string.Empty;

    public Enums.GdbRasterStorage Storage { get; set; }
}
