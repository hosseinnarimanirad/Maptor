using System.Text.Json;
using System.Text.Json.Nodes;

using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Common.Enums;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;

/// <summary>
/// Provides static methods for working with GeoJSON format (RFC 7946).
/// </summary>
public static class GeoJson
{
    public static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
        WriteIndented = false,
        // default; can be overridden per call
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowOutOfOrderMetadataProperties = true,
    };

    public const string Point = "Point";
    public const string MultiPoint = "MultiPoint";
    public const string LineString = "LineString";
    public const string MultiLineString = "MultiLineString";
    public const string Polygon = "Polygon";
    public const string MultiPolygon = "MultiPolygon";
    public const string Feature = "Feature";
    public const string FeatureCollection = "FeatureCollection";
    public const string FeatureSet = "FeatureSet";

    // Static readonly factory instances for point creation
    internal static readonly PointFactory PointFactory = new PointFactory();
    internal static readonly PointZFactory PointZFactory = new PointZFactory();
    internal static readonly PointZMFactory PointZMFactory = new PointZMFactory();

    /// <summary>
    /// Reads GeoJSON features from a file.
    /// </summary>
    /// <param name="fileName">The path to the GeoJSON file.</param>
    /// <returns>An enumerable collection of GeoJSON features.</returns>
    /// <exception cref="ArgumentNullException">Thrown when fileName is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="JsonException">Thrown when the file contains invalid JSON or cannot be parsed.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while reading the file.</exception>
    public static IEnumerable<GeoJsonFeature> LoadFromFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName), "File name cannot be null or empty.");

        if (!File.Exists(fileName))
            throw new FileNotFoundException($"The GeoJSON file was not found: {fileName}", fileName);

        try
        {
            var geoJsonString = File.ReadAllText(fileName);

            return ParseToGeoJsonFeatures(geoJsonString) ?? [];

            //var parsedObject = JsonNode.Parse(geoJsonString);

            //if (parsedObject == null)
            //    return Enumerable.Empty<GeoJsonFeature>();

            //var featuresArray = parsedObject["features"]?.AsArray();

            //if (featuresArray == null)
            //    return Enumerable.Empty<GeoJsonFeature>();

            //return featuresArray
            //    .Select(JsonHelper.Deserialize<GeoJsonFeature>)
            //    .OfType<GeoJsonFeature>();
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
        var content = JsonHelper.Serialize(features, DefaultOptions);

        File.WriteAllText(fileName, content);
    }

    /// <summary>
    /// Parses a GeoJSON FeatureCollection string and returns the features.
    /// </summary>
    /// <param name="geoJsonFeatureSet">The GeoJSON FeatureCollection string to parse.</param>
    /// <returns>An enumerable collection of GeoJSON features.</returns>
    public static IEnumerable<GeoJsonFeature>? ParseToGeoJsonFeatures(string geoJsonFeatureSet)
        => GeoJsonFeatureSet.Parse(geoJsonFeatureSet)?.Features;

    public static string SerializeGeometry(IGeoJsonGeometry geoJson, bool indented, bool removeSpaces = false)
    {
        DefaultOptions.WriteIndented = indented;

        var result = JsonHelper.Serialize(geoJson, DefaultOptions);

        return removeSpaces ? result.Replace(" ", string.Empty) : result;
    }

    /// <summary>
    /// Deserializes a GeoJSON geometry string to an IGeoJsonGeometry instance.
    /// </summary>
    /// <param name="geoJsonString">The GeoJSON geometry string to deserialize.</param>
    /// <returns>An IGeoJsonGeometry instance representing the parsed geometry.</returns>
    public static IGeoJsonGeometry? DeserializeGeometry(string geoJsonString)
        => JsonHelper.Deserialize<IGeoJsonGeometry>(geoJsonString, DefaultOptions);
     
    #region Helper methods


    /// <summary>
    /// Detects the coordinate dimension from a single coordinate array.
    /// </summary>
    /// <param name="coordinates">Single coordinate array.</param>
    /// <returns>The dimension found (2, 3, or 4). Defaults to 2 if null or empty.</returns>
    internal static int DetectCoordinateDimension(double[]? coordinates) => coordinates?.Length ?? 2;

    /// <summary>
    /// Detects the coordinate dimension from a collection of coordinate arrays.
    /// Assumes all coordinates have the same dimension, so checks only the first coordinate.
    /// </summary>
    /// <param name="coordinates">Collection of coordinate arrays.</param>
    /// <returns>The dimension found (2, 3, or 4). Defaults to 2 if null or empty.</returns>
    internal static int DetectCoordinateDimension(double[][]? coordinates) => coordinates?[0]?.Length ?? 2;

    /// <summary>
    /// Detects the coordinate dimension from a nested collection of coordinate arrays.
    /// Assumes all coordinates have the same dimension, so checks only the first coordinate.
    /// </summary>
    /// <param name="coordinates">Nested collection of coordinate arrays.</param>
    /// <returns>The dimension found (2, 3, or 4). Defaults to 2 if null or empty.</returns>
    internal static int DetectCoordinateDimension(double[][][]? coordinates) => coordinates?[0]?[0]?.Length ?? 2;

    /// <summary>
    /// Detects the coordinate dimension from a deeply nested collection of coordinate arrays.
    /// Assumes all coordinates have the same dimension, so checks only the first coordinate.
    /// </summary>
    /// <param name="coordinates">Deeply nested collection of coordinate arrays.</param>
    /// <returns>The dimension found (2, 3, or 4). Defaults to 2 if null or empty.</returns>
    internal static int DetectCoordinateDimension(double[][][][]? coordinates) => coordinates?[0]?[0]?[0]?.Length ?? 2;


    #endregion


    #region Helper methods for Geometry<T> and IPoint types conversions

    internal static T CreatePointFromCoordinates<T>(double[] coords, IPointFactory<T> pointFactory, bool isLongitudeFirst = true) where T : IPoint
    {
        if (coords == null || coords.Length < 2)
            throw new ArgumentException("Coordinates must have at least 2 elements.", nameof(coords));

        double x = isLongitudeFirst ? coords[0] : coords[1];
        double y = isLongitudeFirst ? coords[1] : coords[0];

        return pointFactory.Create(x, y, coords);
    }

    internal static List<T> CreatePointListFromCoordinates<T>(double[][] coords, IPointFactory<T> pointFactory, bool isLongitudeFirst = true) where T : IPoint
    {
        return coords?.Select(c => CreatePointFromCoordinates(c, pointFactory, isLongitudeFirst)).ToList() ?? new List<T>();
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
    internal static Geometry<T> CreateGeometryFromLineCoordinates<T>(
        double[][] coordinates,
        IPointFactory<T> pointFactory,
        GeometryType geometryType,
        bool isRing = false,
        bool isLongitudeFirst = true,
        int srid = 0) where T : IPoint, new()
    {
        if (coordinates == null || coordinates.Length == 0)
            return Geometry<T>.CreateEmpty(geometryType, srid);

        var pointsToUse = isRing && coordinates.Length > 0 ? coordinates.Take(coordinates.Length - 1) : coordinates;

        return Geometry<T>.Create(pointsToUse.Select(c => CreatePointFromCoordinates(c, pointFactory, isLongitudeFirst)).ToList(), geometryType, srid); 
    }

    /// <summary>
    /// Creates a Geometry instance with the appropriate point type based on coordinate dimensions for polygons.
    /// </summary>
    /// <param name="rings">Array of rings (each ring is an array of coordinate arrays).</param>
    /// <param name="geometryType">The type of geometry to create.</param>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude].</param>
    /// <param name="srid">The spatial reference system identifier.</param>
    /// <returns>An IGeometry instance with appropriate point type.</returns>
    internal static Geometry<T> CreateGeometryFromPolygonCoordinates<T>(
        double[][][] rings,
        IPointFactory<T> pointFactory,
        GeometryType geometryType,
        bool isLongitudeFirst = true,
        int srid = 0) where T : IPoint, new()
    {
        if (rings == null || rings.Length == 0)
            return Geometry<T>.CreateEmpty(geometryType, srid);

        var ringGeometries = rings.Select(ring => CreateGeometryFromLineCoordinates(ring, pointFactory, GeometryType.LineString, isRing: true, isLongitudeFirst, srid)).ToList();

        return Geometry<T>.Create(ringGeometries, geometryType, srid);
         
    }


    /// <summary>
    /// Validates that a polygon ring follows the correct orientation per RFC 7946.
    /// External rings must be counterclockwise (positive signed area).
    /// Internal rings (holes) must be clockwise (negative signed area).
    /// </summary>
    /// <param name="ring">The ring coordinates as an array of [longitude, latitude] arrays.</param>
    /// <param name="isExternalRing">True if this is an external ring, false if it's an internal ring (hole).</param>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <returns>True if the ring orientation is correct per RFC 7946, false otherwise.</returns>
    internal static bool ValidateRingOrientation(double[][] ring, bool isExternalRing, bool isLongitudeFirst = true)
    {
        if (ring == null || ring.Length < 3)
            return true; // Empty or invalid rings are considered valid

        // Convert ring to Point list for area calculation
        // GeoJSON rings are closed (first point equals last point), but GetSignedEuclideanArea expects non-closed
        var points = new List<Point>(ring.Length);

        for (int i = 0; i < ring.Length - 1; i++)
        {
            var coords = ring[i];
            if (coords == null || coords.Length < 2)
                return true; // Invalid coordinate

            double x = isLongitudeFirst ? coords[0] : coords[1];
            double y = isLongitudeFirst ? coords[1] : coords[0];

            points.Add(new Point(x, y));
        }

        if (points.Count < 3)
            return true; // Not enough points for a valid ring

        var isClockwize = SpatialUtility.IsClockwise(points);

        // External rings must be counterclockwise (positive area)
        // Internal rings must be clockwise (negative area)
        return isExternalRing ^ isClockwize;
    }

    /// <summary>
    /// Validates polygon ring orientations according to RFC 7946 Section 3.1.6.
    /// The first ring must be counterclockwise (external ring).
    /// Subsequent rings must be clockwise (internal rings/holes).
    /// </summary>
    /// <param name="rings">Array of rings, where each ring is an array of coordinate arrays.</param>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <returns>A tuple indicating if validation passed and a list of validation errors (if any).</returns>
    internal static (bool IsValid, List<string> Errors) ValidatePolygonRingOrientations(double[][][] rings, bool isLongitudeFirst = true)
    {
        var errors = new List<string>();

        if (rings == null || rings.Length == 0)
        {
            return (true, errors); // Empty polygon is valid
        }

        // Validate first ring (external ring) - must be counterclockwise
        if (rings[0] != null && rings[0].Length >= 3)
        {
            if (!ValidateRingOrientation(rings[0], isExternalRing: true, isLongitudeFirst))
            {
                errors.Add("External ring (first ring) must be counterclockwise per RFC 7946 Section 3.1.6.");
            }
        }

        // Validate subsequent rings (internal rings/holes) - must be clockwise
        for (int i = 1; i < rings.Length; i++)
        {
            if (rings[i] != null && rings[i].Length >= 3)
            {
                if (!ValidateRingOrientation(rings[i], isExternalRing: false, isLongitudeFirst))
                {
                    errors.Add($"Internal ring (hole) at index {i} must be clockwise per RFC 7946 Section 3.1.6.");
                }
            }
        }

        return (errors.Count == 0, errors);
    }

    #endregion
     
}
