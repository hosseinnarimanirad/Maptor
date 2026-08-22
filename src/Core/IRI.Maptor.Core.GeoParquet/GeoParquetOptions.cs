using Parquet;
using CompressionMethod = Parquet.CompressionMethod;

namespace IRI.Maptor.Core.GeoParquet;

/// <summary>
/// Options for reading and writing GeoParquet files
/// </summary>
public class GeoParquetOptions
{
    /// <summary>
    /// Name of the geometry column. Default is "geometry"
    /// </summary>
    public string GeometryColumnName { get; set; } = "geometry";

    /// <summary>
    /// Compression method for Parquet file. Default is Snappy
    /// </summary>
    public CompressionMethod CompressionMethod { get; set; } = CompressionMethod.Snappy;

    /// <summary>
    /// Whether to include bounding box in metadata. Default is true
    /// </summary>
    public bool IncludeBbox { get; set; } = true;

    /// <summary>
    /// Whether to include geometry types in metadata. Default is true
    /// </summary>
    public bool IncludeGeometryTypes { get; set; } = true;

    /// <summary>
    /// Default SRID to use when reading files without CRS information. Default is 4326 (WGS84)
    /// </summary>
    public int DefaultSrid { get; set; } = 4326;
}

