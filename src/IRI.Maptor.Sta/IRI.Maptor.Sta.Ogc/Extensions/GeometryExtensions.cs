using IRI.Maptor.Sta.KmlFormat;
using IRI.Maptor.Sta.Common.Abstractions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using System;
using System.Linq;


namespace IRI.Maptor.Extensions;

/// <summary>
/// Extension methods for converting geometries to KML format
/// </summary>
public static class Sta_KmlExtensions
{
    #region Point Extensions

    /// <summary>
    /// Converts a point to KML string
    /// </summary>
    /// <param name="point">Point to convert</param>
    /// <param name="srid">SRID of the point (default: 4326 - WGS84)</param>
    /// <param name="name">Feature name</param>
    /// <param name="description">Feature description</param>
    /// <returns>KML string</returns>
    public static string AsKml<T>(
        this T point,
        int srid = 4326,
        string? name = null,
        string? description = null) where T : IPoint, new()
    {
        if (point == null)
            return string.Empty;

        var geometry = Geometry<Point>.Create(point.X, point.Y, srid);

        return KmlWriter.ToKml(geometry, name, description);
    }

    #endregion

    #region Geometry Extensions

    /// <summary>
    /// Converts a geometry to KML string
    /// </summary>
    /// <param name="geometry">Geometry to convert</param>
    /// <param name="name">Feature name</param>
    /// <param name="description">Feature description</param>
    /// <returns>KML string</returns>
    public static string AsKml<T>(
        this Geometry<T> geometry,
        string? name = null,
        string? description = null) where T : IPoint, new()
    {
        if (geometry == null || geometry.IsNullOrEmpty())
            return string.Empty;

        // Convert to Point-based geometry if needed
        var pointGeometry = ConvertToPointGeometry(geometry);

        return KmlWriter.ToKml(pointGeometry, name, description);
    }
     
    /// <summary>
    /// Converts a geometry to KML and saves it to a file asynchronously
    /// </summary>
    /// <param name="geometry">Geometry to convert</param>
    /// <param name="filePath">Output file path</param>
    /// <param name="name">Feature name</param>
    /// <param name="description">Feature description</param>
    public static async System.Threading.Tasks.Task SaveAsKmlAsync<T>(
        this Geometry<T> geometry,
        string filePath,
        string? name = null,
        string? description = null) where T : IPoint, new()
    {
        var pointGeometry = ConvertToPointGeometry(geometry);

        await KmlWriter.WriteToFileAsync(
            new System.Collections.Generic.List<Geometry<Point>> { pointGeometry },
            filePath,
            name);
    }

    #endregion

    #region Collection Extensions

    /// <summary>
    /// Converts a list of geometries to KML string
    /// </summary>
    /// <param name="geometries">List of geometries to convert</param>
    /// <param name="documentName">Document name</param>
    /// <returns>KML string</returns>
    public static string AsKml<T>(
        this System.Collections.Generic.List<Geometry<T>> geometries,
        string? documentName = null) where T : IPoint, new()
    {
        if (geometries == null || geometries.Count == 0)
            return string.Empty;

        var pointGeometries = geometries
            .Select(g => ConvertToPointGeometry(g))
            .ToList();

        return KmlWriter.ToKml(pointGeometries, documentName);
    }
     
    /// <summary>
    /// Converts a list of geometries to KML and saves to a file asynchronously
    /// </summary>
    /// <param name="geometries">List of geometries to convert</param>
    /// <param name="filePath">Output file path</param>
    /// <param name="documentName">Document name</param>
    public static async System.Threading.Tasks.Task SaveAsKmlAsync<T>(
        this System.Collections.Generic.List<Geometry<T>> geometries,
        string filePath,
        string? documentName = null) where T : IPoint, new()
    {
        var pointGeometries = geometries
            .Select(g => ConvertToPointGeometry(g))
            .ToList();

        await KmlWriter.WriteToFileAsync(pointGeometries, filePath, documentName);
    }

    #endregion

    #region Feature Extensions

    /// <summary>
    /// Converts a KML feature to KML string
    /// </summary>
    /// <param name="feature">Feature to convert</param>
    /// <returns>KML string</returns>
    public static string AsKml(
        this KmlFeature feature)
    {
        if (feature == null)
            return string.Empty;

        return KmlWriter.ToKml(
            new System.Collections.Generic.List<KmlFeature> { feature },
            null);
    }

    /// <summary>
    /// Converts a list of KML features to KML string
    /// </summary>
    /// <param name="features">List of features to convert</param>
    /// <param name="documentName">Document name</param>
    /// <returns>KML string</returns>
    public static string AsKml(
        this System.Collections.Generic.List<KmlFeature> features,
        string? documentName = null)
    {
        if (features == null || features.Count == 0)
            return string.Empty;

        return KmlWriter.ToKml(features, documentName);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Converts a geometry with any point type to a geometry with Point type
    /// </summary>
    private static Geometry<Point> ConvertToPointGeometry<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry is null)
            return Geometry<Point>.Empty;

        // If already Point-based geometry, cast or convert
        if (typeof(T) == typeof(Point))
        {
            return geometry as Geometry<Point> ?? Geometry<Point>.Empty;
        }

        // Convert points
        if (geometry.IsNonEmptyLeafGeometry())
        {
            var points = geometry.Points
                .Select(p => new Point(p.X, p.Y))
                .ToList();

            return Geometry<Point>.Create(points, geometry.Type, geometry.Srid);
        }

        if (geometry.HasGeometry())
        {
            var geometries = geometry.Geometries
                .Select(g => ConvertToPointGeometry(g))
                .ToList();

            return Geometry<Point>.Create(geometries, geometry.Type, geometry.Srid);
        }

        return Geometry<Point>.CreateEmpty(geometry.Type, geometry.Srid);
    }

    #endregion

    #region KMZ Extensions
      
    /// <summary>
    /// Converts a geometry to KMZ and saves it to a file asynchronously
    /// </summary>
    /// <param name="geometry">Geometry to convert</param>
    /// <param name="filePath">Output KMZ file path</param>
    /// <param name="name">Feature name</param>
    /// <param name="description">Feature description</param>
    public static async System.Threading.Tasks.Task SaveAsKmzAsync<T>(
        this Geometry<T> geometry,
        string filePath,
        string? name = null,
        string? description = null) where T : IPoint, new()
    {
        var pointGeometry = ConvertToPointGeometry(geometry);

        await KmzWriter.WriteToFileAsync(
            new System.Collections.Generic.List<Geometry<Point>> { pointGeometry },
            filePath,
            name);
    }
     

    /// <summary>
    /// Converts a list of geometries to KMZ and saves to a file asynchronously
    /// </summary>
    /// <param name="geometries">List of geometries to convert</param>
    /// <param name="filePath">Output KMZ file path</param>
    /// <param name="documentName">Document name</param>
    public static async System.Threading.Tasks.Task SaveAsKmzAsync<T>(
        this System.Collections.Generic.List<Geometry<T>> geometries,
        string filePath,
        string? documentName = null) where T : IPoint, new()
    {
        var pointGeometries = geometries
            .Select(g => ConvertToPointGeometry(g))
            .ToList();

        await KmzWriter.WriteToFileAsync(pointGeometries, filePath, documentName);
    }


    /// <summary>
    /// Converts a list of KML features to KMZ and saves to a file asynchronously
    /// </summary>
    /// <param name="features">List of features to convert</param>
    /// <param name="filePath">Output KMZ file path</param>
    /// <param name="documentName">Document name</param>
    public static async System.Threading.Tasks.Task SaveAsKmzAsync(
        this System.Collections.Generic.List<KmlFeature> features,
        string filePath,
        string? documentName = null)
    {
        if (features == null || features.Count == 0)
            return;

        await KmzWriter.WriteToFileAsync(features, filePath, documentName);
    }

    #endregion
}
