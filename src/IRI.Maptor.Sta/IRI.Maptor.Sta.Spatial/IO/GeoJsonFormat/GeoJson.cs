using System.Text.Json;
using System.Text.Json.Nodes;

using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Analysis;

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

    public static string Serialize(IGeoJsonGeometry geoJson, bool indented, bool removeSpaces = false)
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


    /// <summary>
    /// Creates a point instance from coordinate array based on dimension.
    /// 2D coordinates [longitude, latitude] → Point
    /// 3D coordinates [longitude, latitude, elevation] → PointZ
    /// 4D coordinates [longitude, latitude, elevation, measure] → PointZM
    /// </summary>
    /// <param name="coords">Coordinate array with 2, 3, or 4 elements.</param>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <returns>An IPoint instance (Point, PointZ, or PointZM).</returns>
    //internal static IPoint CreatePointFromCoordinates(double[] coords, bool isLongitudeFirst = true)
    //{
    //    if (coords == null || coords.Length < 2)
    //        throw new ArgumentException("Coordinates must have at least 2 elements.", nameof(coords));

    //    double x = isLongitudeFirst ? coords[0] : coords[1];
    //    double y = isLongitudeFirst ? coords[1] : coords[0];

    //    return coords.Length switch
    //    {
    //        2 => new Point(x, y),
    //        3 => new PointZ { X = x, Y = y, Z = coords[2] },
    //        4 => new PointZM { X = x, Y = y, Z = coords[2], M = coords[3] },
    //        _ => throw new ArgumentException($"Unsupported coordinate dimension: {coords.Length}. Expected 2, 3, or 4.", nameof(coords))
    //    };
    //}

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
    /// </summary>
    /// <param name="coordinates">Single coordinate array.</param>
    /// <param name="geometryType">The type of geometry to create.</param>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude].</param>
    /// <param name="srid">The spatial reference system identifier.</param>
    /// <returns>An IGeometry instance with appropriate point type.</returns>
    //internal static Geometry<T> CreatePointGeometryFromCoordinates<T>(
    //    double[] coordinates,
    //    IPointFactory<T> pointFactory,
    //    GeometryType geometryType,
    //    bool isLongitudeFirst = true,
    //    int srid = 0) where T : IPoint, new()
    //{
    //    var point = CreatePointFromCoordinates(coordinates, pointFactory, isLongitudeFirst);

    //    return new Geometry<T>([point], geometryType, srid);

    //    //return point switch
    //    //{
    //    //    PointZM pzm => new Geometry<PointZM>(new List<PointZM> { pzm }, geometryType, srid),
    //    //    PointZ pz => new Geometry<PointZ>(new List<PointZ> { pz }, geometryType, srid),
    //    //    Point p => new Geometry<Point>(new List<Point> { p }, geometryType, srid),
    //    //    _ => throw new NotSupportedException($"Unsupported point type: {point.GetType()}")
    //    //};
    //}

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

        //return maxDim switch
        //{
        //    2 => new Geometry<Point>(coordinates.Select(c => CreatePointFromCoordinates(c, pointFactory, isLongitudeFirst)).ToList(), geometryType, srid),
        //    3 => new Geometry<PointZ>(coordinates.Select(c => CreatePointFromCoordinates(c, pointZFactory, isLongitudeFirst)).ToList(), geometryType, srid),
        //    4 => new Geometry<PointZM>(coordinates.Select(c => CreatePointFromCoordinates(c, pointZMFactory, isLongitudeFirst)).ToList(), geometryType, srid),
        //    _ => throw new NotSupportedException($"Unsupported coordinate dimension: {maxDim}")
        //};
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

        return new Geometry<T>(ringGeometries, geometryType, srid);

        //// All rings should have the same point type, so use the first one to determine the type
        //return ringGeometries[0] switch
        //{
        //    Geometry<Point> => new Geometry<Point>(ringGeometries.Cast<Geometry<Point>>().ToList(), geometryType, srid),
        //    Geometry<PointZ> => new Geometry<PointZ>(ringGeometries.Cast<Geometry<PointZ>>().ToList(), geometryType, srid),
        //    Geometry<PointZM> => new Geometry<PointZM>(ringGeometries.Cast<Geometry<PointZM>>().ToList(), geometryType, srid),
        //    _ => throw new NotSupportedException($"Unsupported geometry type: {ringGeometries[0].GetType()}")
        //};
    }

    /// <summary>
    /// Creates an empty geometry with the specified type and SRID.
    /// </summary>
    /// <param name="geometryType">The type of geometry.</param>
    /// <param name="srid">The spatial reference system identifier.</param>
    /// <returns>An empty IGeometry instance.</returns>
    //internal static IGeometry CreateEmptyGeometry(GeometryType geometryType, int srid = 0)
    //{
    //    // Default to Point for empty geometries
    //    return Geometry<Point>.CreateEmpty(geometryType, srid);
    //}

    #region Polygon Ring Orientation Validation (RFC 7946 Section 3.1.6)

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
