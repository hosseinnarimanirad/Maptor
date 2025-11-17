using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.GeoParquet;

/// <summary>
/// Extension methods for GeoParquet conversions
/// </summary>
public static class GeoParquetExtensions
{
    /// <summary>
    /// Converts a Feature to WKB bytes for GeoParquet
    /// </summary>
    public static byte[]? ToGeoParquetWkb(this Feature<Point> feature)
    {
        return feature.TheGeometry?.AsWkb();
    }

    /// <summary>
    /// Converts a Geometry to WKB bytes for GeoParquet
    /// </summary>
    public static byte[]? ToGeoParquetWkb(this Geometry<Point> geometry)
    {
        return geometry.AsWkb();
    }

    /// <summary>
    /// Converts WKB bytes to a Geometry
    /// </summary>
    public static Geometry<Point>? FromGeoParquetWkb(this byte[] wkbBytes, int srid)
    {
        return WkbReader.Parse(wkbBytes, srid) as Geometry<Point>;
    }

    /// <summary>
    /// Writes a FeatureSet to a GeoParquet file
    /// </summary>
    public static void WriteToGeoParquet(this FeatureSet<Point> featureSet, string filePath, GeoParquetOptions? options = null)
    {
        GeoParquetWriter.WriteFeatureSet(filePath, featureSet, options);
    }

    /// <summary>
    /// Writes features to a GeoParquet file
    /// </summary>
    public static void WriteToGeoParquet(this IEnumerable<Feature<Point>> features, string filePath, GeoParquetOptions? options = null)
    {
        GeoParquetWriter.WriteFeatures(filePath, features, options);
    }

    /// <summary>
    /// Reads a FeatureSet from a GeoParquet file
    /// </summary>
    public static FeatureSet<Point> ReadFromGeoParquet(string filePath, GeoParquetOptions? options = null)
    {
        return GeoParquetReader.ReadFeatureSet(filePath, options);
    }

    /// <summary>
    /// Reads features from a GeoParquet file
    /// </summary>
    public static IEnumerable<Feature<Point>> ReadFeaturesFromGeoParquet(string filePath, GeoParquetOptions? options = null)
    {
        return GeoParquetReader.ReadFeatures(filePath, options);
    }
}

