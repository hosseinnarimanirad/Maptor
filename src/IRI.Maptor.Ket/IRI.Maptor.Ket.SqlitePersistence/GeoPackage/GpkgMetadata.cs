using System;
using System.Collections.Generic;

namespace IRI.Maptor.Ket.SqlitePersistence.GeoPackage;

/// <summary>
/// Represents metadata for a GeoPackage layer
/// Based on OGC GeoPackage Encoding Standard
/// </summary>
public class GpkgLayerMetadata
{
    /// <summary>
    /// Name of the actual content (e.g. table name)
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Type of data: features, tiles, attributes, etc.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable identifier (e.g. short name) for the content
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>
    /// Human-readable description for the content
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Timestamp when content was last changed
    /// </summary>
    public DateTime? LastChange { get; set; }

    /// <summary>
    /// Minimum bounding rectangle for content (minX, minY, maxX, maxY)
    /// </summary>
    public double MinX { get; set; }
    public double MinY { get; set; }
    public double MaxX { get; set; }
    public double MaxY { get; set; }

    /// <summary>
    /// Spatial Reference System ID
    /// </summary>
    public int Srs_Id { get; set; }

    public override string ToString()
    {
        return $"GeoPackage Layer: {TableName} ({DataType})";
    }
}

/// <summary>
/// Represents geometry column information in GeoPackage
/// </summary>
public class GpkgGeometryColumn
{
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string GeometryTypeName { get; set; } = string.Empty;
    public int SrsId { get; set; }
    public bool HasZ { get; set; }
    public bool HasM { get; set; }

    public override string ToString()
    {
        return $"{TableName}.{ColumnName} ({GeometryTypeName})";
    }
}

/// <summary>
/// Represents a Spatial Reference System in GeoPackage
/// </summary>
public class GpkgSpatialReferenceSystem
{
    public int SrsId { get; set; }
    public string? Organization { get; set; }
    public int OrganizationCoordsysId { get; set; }
    public string? Definition { get; set; }
    public string? Description { get; set; }

    public override string ToString()
    {
        return $"SRS {SrsId}: {Organization}:{OrganizationCoordsysId}";
    }
}

/// <summary>
/// Represents tile matrix set information
/// </summary>
public class GpkgTileMatrixSet
{
    public string TableName { get; set; } = string.Empty;
    public int SrsId { get; set; }
    public double MinX { get; set; }
    public double MinY { get; set; }
    public double MaxX { get; set; }
    public double MaxY { get; set; }

    public override string ToString()
    {
        return $"TileMatrixSet: {TableName}";
    }
}

/// <summary>
/// Represents a tile matrix (zoom level information)
/// </summary>
public class GpkgTileMatrix
{
    public string TableName { get; set; } = string.Empty;
    public int ZoomLevel { get; set; }
    public int MatrixWidth { get; set; }
    public int MatrixHeight { get; set; }
    public int TileWidth { get; set; }
    public int TileHeight { get; set; }
    public double PixelXSize { get; set; }
    public double PixelYSize { get; set; }

    public override string ToString()
    {
        return $"TileMatrix: {TableName} Zoom={ZoomLevel} ({MatrixWidth}x{MatrixHeight})";
    }
}

