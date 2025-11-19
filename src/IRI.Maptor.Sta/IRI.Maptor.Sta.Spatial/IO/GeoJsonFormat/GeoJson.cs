using System.Text.Json;
using System.Text.Json.Nodes;

using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;

/// <summary>
/// Provides static methods for working with GeoJSON format (RFC 7946).
/// </summary>
public static class GeoJson
{
    public const string Point = "Point";
    public const string MultiPoint = "MultiPoint";
    public const string LineString = "LineString";
    public const string MultiLineString = "MultiLineString";
    public const string Polygon = "Polygon";
    public const string MultiPolygon = "MultiPolygon";
    public const string Feature = "Feature";
    public const string FeatureCollection = "FeatureCollection";
    public const string FeatureSet = "FeatureSet";

    /// <summary>
    /// Reads GeoJSON features from a file.
    /// </summary>
    /// <param name="fileName">The path to the GeoJSON file.</param>
    /// <returns>An enumerable collection of GeoJSON features.</returns>
    /// <exception cref="ArgumentNullException">Thrown when fileName is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="JsonException">Thrown when the file contains invalid JSON or cannot be parsed.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while reading the file.</exception>
    public static IEnumerable<GeoJsonFeature> ReadFeatures(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName), "File name cannot be null or empty.");

        if (!File.Exists(fileName))
            throw new FileNotFoundException($"The GeoJSON file was not found: {fileName}", fileName);

        try
        {
            var geoJsonString = File.ReadAllText(fileName);
            var parsedObject = JsonNode.Parse(geoJsonString);

            if (parsedObject == null)
                return Enumerable.Empty<GeoJsonFeature>();

            var featuresArray = parsedObject["features"]?.AsArray();
            if (featuresArray == null)
                return Enumerable.Empty<GeoJsonFeature>();

            return featuresArray
                .Select(featureNode => JsonHelper.Deserialize<GeoJsonFeature>(featureNode))
                .OfType<GeoJsonFeature>();
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Failed to parse GeoJSON file: {fileName}. {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new IOException($"An I/O error occurred while reading the GeoJSON file: {fileName}. {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Saves GeoJSON features to a file.
    /// </summary>
    /// <param name="fileName">The path where the GeoJSON file will be saved.</param>
    /// <param name="features">The collection of GeoJSON features to save.</param>
    public static void SaveFeatures(string fileName, IEnumerable<GeoJsonFeature> features)
    {
        var content = JsonHelper.Serialize(features);

        File.WriteAllText(fileName, content);
    }

    /// <summary>
    /// Parses a GeoJSON FeatureCollection string and returns the features.
    /// </summary>
    /// <param name="geoJsonFeatureSet">The GeoJSON FeatureCollection string to parse.</param>
    /// <returns>An enumerable collection of GeoJSON features.</returns>
    public static IEnumerable<GeoJsonFeature>? ParseToGeoJsonFeatures(string geoJsonFeatureSet)
    {
        return GeoJsonFeatureSet.Parse(geoJsonFeatureSet)?.Features;
    }

    internal static string Serialize(IGeoJsonGeometry geoJson, bool indented, bool removeSpaces = false)
    {
        var result = JsonHelper.Serialize(geoJson, indented);

        return removeSpaces ? result.Replace(" ", string.Empty) : result;
    }

    /// <summary>
    /// Deserializes a GeoJSON geometry string to an IGeoJsonGeometry instance.
    /// </summary>
    /// <param name="geoJsonString">The GeoJSON geometry string to deserialize.</param>
    /// <returns>An IGeoJsonGeometry instance representing the parsed geometry.</returns>
    public static IGeoJsonGeometry? Deserialize(string geoJsonString)
    {
        return JsonHelper.Deserialize<IGeoJsonGeometry>(geoJsonString);
    }

    internal static GeoJsonFeature AsFeature(IGeoJsonGeometry geometry)
    {
        return GeoJsonFeature.Create(geometry);
    }

    internal static GeoJsonFeatureSet AsFeatureSet(IGeoJsonGeometry geometry)
    {
        return new GeoJsonFeatureSet() { Features = new List<GeoJsonFeature>() { AsFeature(geometry) }, TotalFeatures = 1 };
    }

    /// <summary>
    /// Creates a point instance from coordinate array based on dimension.
    /// 2D coordinates [longitude, latitude] → Point
    /// 3D coordinates [longitude, latitude, elevation] → PointZ
    /// 4D coordinates [longitude, latitude, elevation, measure] → PointZM
    /// </summary>
    /// <param name="coords">Coordinate array with 2, 3, or 4 elements.</param>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <returns>An IPoint instance (Point, PointZ, or PointZM).</returns>
    internal static IPoint CreatePointFromCoordinates(double[] coords, bool isLongitudeFirst = true)
    {
        if (coords == null || coords.Length < 2)
            throw new ArgumentException("Coordinates must have at least 2 elements.", nameof(coords));

        double x, y;
        if (isLongitudeFirst)
        {
            x = coords[0];
            y = coords[1];
        }
        else
        {
            x = coords[1];
            y = coords[0];
        }

        return coords.Length switch
        {
            2 => new Point(x, y),
            3 => new PointZ { X = x, Y = y, Z = coords[2] },
            4 => new PointZM { X = x, Y = y, Z = coords[2], M = coords[3] },
            _ => throw new ArgumentException($"Unsupported coordinate dimension: {coords.Length}. Expected 2, 3, or 4.", nameof(coords))
        };
    }

    /// <summary>
    /// Detects the maximum coordinate dimension from a collection of coordinate arrays.
    /// </summary>
    /// <param name="coordinates">Collection of coordinate arrays.</param>
    /// <returns>The maximum dimension found (2, 3, or 4).</returns>
    internal static int DetectMaxCoordinateDimension(double[][]? coordinates)
    {
        if (coordinates == null || coordinates.Length == 0)
            return 2; // Default to 2D

        return coordinates.Max(c => c?.Length ?? 2);
    }

    /// <summary>
    /// Detects the maximum coordinate dimension from a nested collection of coordinate arrays.
    /// </summary>
    /// <param name="coordinates">Nested collection of coordinate arrays.</param>
    /// <returns>The maximum dimension found (2, 3, or 4).</returns>
    internal static int DetectMaxCoordinateDimension(double[][][]? coordinates)
    {
        if (coordinates == null || coordinates.Length == 0)
            return 2; // Default to 2D

        return coordinates.Max(ring => ring?.Max(c => c?.Length ?? 2) ?? 2);
    }

    /// <summary>
    /// Detects the maximum coordinate dimension from a deeply nested collection of coordinate arrays.
    /// </summary>
    /// <param name="coordinates">Deeply nested collection of coordinate arrays.</param>
    /// <returns>The maximum dimension found (2, 3, or 4).</returns>
    internal static int DetectMaxCoordinateDimension(double[][][][]? coordinates)
    {
        if (coordinates == null || coordinates.Length == 0)
            return 2; // Default to 2D

        return coordinates.Max(polygon => polygon?.Max(ring => ring?.Max(c => c?.Length ?? 2) ?? 2) ?? 2);
    }

    /// <summary>
    /// Creates a Geometry instance with the appropriate point type based on coordinate dimensions.
    /// </summary>
    /// <param name="coordinates">Single coordinate array.</param>
    /// <param name="geometryType">The type of geometry to create.</param>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude].</param>
    /// <param name="srid">The spatial reference system identifier.</param>
    /// <returns>An IGeometry instance with appropriate point type.</returns>
    internal static IGeometry CreateGeometryFromPointCoordinates(double[] coordinates, GeometryType geometryType, bool isLongitudeFirst = true, int srid = 0)
    {
        var point = CreatePointFromCoordinates(coordinates, isLongitudeFirst);
        return point switch
        {
            PointZM pzm => new Geometry<PointZM>(new List<PointZM> { pzm }, geometryType, srid),
            PointZ pz => new Geometry<PointZ>(new List<PointZ> { pz }, geometryType, srid),
            Point p => new Geometry<Point>(new List<Point> { p }, geometryType, srid),
            _ => throw new NotSupportedException($"Unsupported point type: {point.GetType()}")
        };
    }

    /// <summary>
    /// Creates a Geometry instance with the appropriate point type based on coordinate dimensions.
    /// All coordinates are normalized to the maximum dimension found.
    /// </summary>
    /// <param name="coordinates">Array of coordinate arrays.</param>
    /// <param name="geometryType">The type of geometry to create.</param>
    /// <param name="isRing">Whether the coordinates form a ring (closed loop).</param>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude].</param>
    /// <param name="srid">The spatial reference system identifier.</param>
    /// <returns>An IGeometry instance with appropriate point type.</returns>
    internal static IGeometry CreateGeometryFromLineCoordinates(double[][] coordinates, GeometryType geometryType, bool isRing = false, bool isLongitudeFirst = true, int srid = 0)
    {
        if (coordinates == null || coordinates.Length == 0)
            return CreateEmptyGeometry(geometryType, srid);

        var maxDim = DetectMaxCoordinateDimension(coordinates);
        var pointsToUse = isRing && coordinates.Length > 0 ? coordinates.Take(coordinates.Length - 1) : coordinates;

        // Normalize all coordinates to the maximum dimension
        var normalizedCoords = pointsToUse.Select(c =>
        {
            if (c == null) return new double[maxDim];
            var normalized = new double[maxDim];
            Array.Copy(c, normalized, Math.Min(c.Length, maxDim));
            // Fill missing dimensions with 0
            for (int i = c.Length; i < maxDim; i++)
                normalized[i] = 0;
            return normalized;
        }).ToArray();

        return maxDim switch
        {
            2 => new Geometry<Point>(normalizedCoords.Select(c => CreatePointFromCoordinates(c, isLongitudeFirst)).Cast<Point>().ToList(), geometryType, srid),
            3 => new Geometry<PointZ>(normalizedCoords.Select(c => CreatePointFromCoordinates(c, isLongitudeFirst)).Cast<PointZ>().ToList(), geometryType, srid),
            4 => new Geometry<PointZM>(normalizedCoords.Select(c => CreatePointFromCoordinates(c, isLongitudeFirst)).Cast<PointZM>().ToList(), geometryType, srid),
            _ => throw new NotSupportedException($"Unsupported coordinate dimension: {maxDim}")
        };
    }

    /// <summary>
    /// Creates a Geometry instance with the appropriate point type based on coordinate dimensions for polygons.
    /// </summary>
    /// <param name="rings">Array of rings (each ring is an array of coordinate arrays).</param>
    /// <param name="geometryType">The type of geometry to create.</param>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude].</param>
    /// <param name="srid">The spatial reference system identifier.</param>
    /// <returns>An IGeometry instance with appropriate point type.</returns>
    internal static IGeometry CreateGeometryFromPolygonCoordinates(double[][][] rings, GeometryType geometryType, bool isLongitudeFirst = true, int srid = 0)
    {
        if (rings == null || rings.Length == 0)
            return CreateEmptyGeometry(geometryType, srid);

        var maxDim = DetectMaxCoordinateDimension(rings);
        var ringGeometries = rings.Select(ring => CreateGeometryFromLineCoordinates(ring, GeometryType.LineString, isRing: true, isLongitudeFirst, srid)).ToList();

        // All rings should have the same point type, so use the first one to determine the type
        return ringGeometries[0] switch
        {
            Geometry<Point> => new Geometry<Point>(ringGeometries.Cast<Geometry<Point>>().ToList(), geometryType, srid),
            Geometry<PointZ> => new Geometry<PointZ>(ringGeometries.Cast<Geometry<PointZ>>().ToList(), geometryType, srid),
            Geometry<PointZM> => new Geometry<PointZM>(ringGeometries.Cast<Geometry<PointZM>>().ToList(), geometryType, srid),
            _ => throw new NotSupportedException($"Unsupported geometry type: {ringGeometries[0].GetType()}")
        };
    }

    /// <summary>
    /// Creates an empty geometry with the specified type and SRID.
    /// </summary>
    /// <param name="geometryType">The type of geometry.</param>
    /// <param name="srid">The spatial reference system identifier.</param>
    /// <returns>An empty IGeometry instance.</returns>
    internal static IGeometry CreateEmptyGeometry(GeometryType geometryType, int srid = 0)
    {
        // Default to Point for empty geometries
        return Geometry<Point>.CreateEmpty(geometryType, srid);
    }

}
