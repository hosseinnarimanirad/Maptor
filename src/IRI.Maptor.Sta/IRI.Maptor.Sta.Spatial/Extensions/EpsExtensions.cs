using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.Eps;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Extensions;

/// <summary>
/// Extension methods for converting Geometry and Feature to/from EPS format
/// </summary>
public static class EpsExtensions
{
    /// <summary>
    /// Converts Geometry to EPS string
    /// </summary>
    /// <param name="geometry">The geometry to convert</param>
    /// <param name="options">EPS options for styling and formatting</param>
    /// <returns>EPS string</returns>
    public static string ToEps(this Geometry<Point> geometry, EpsOptions? options = null)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        return EpsWriter.Write(geometry, options);
    }

    /// <summary>
    /// Saves Geometry to EPS file
    /// </summary>
    /// <param name="geometry">The geometry to save</param>
    /// <param name="filePath">The path to save the EPS file</param>
    /// <param name="options">EPS options for styling and formatting</param>
    /// <returns>The path to the saved file</returns>
    public static string SaveAsEps(this Geometry<Point> geometry, string filePath, EpsOptions? options = null)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        return EpsWriter.WriteToFile(geometry, filePath, options);
    }

    /// <summary>
    /// Converts Feature to EPS string
    /// </summary>
    /// <param name="feature">The feature to convert</param>
    /// <param name="options">EPS options for styling and formatting</param>
    /// <returns>EPS string</returns>
    public static string ToEps(this Feature<Point> feature, EpsOptions? options = null)
    {
        if (feature == null)
            throw new ArgumentNullException(nameof(feature));

        return EpsWriter.Write(feature, options);
    }

    /// <summary>
    /// Saves Feature to EPS file
    /// </summary>
    /// <param name="feature">The feature to save</param>
    /// <param name="filePath">The path to save the EPS file</param>
    /// <param name="options">EPS options for styling and formatting</param>
    /// <returns>The path to the saved file</returns>
    public static string SaveAsEps(this Feature<Point> feature, string filePath, EpsOptions? options = null)
    {
        if (feature == null)
            throw new ArgumentNullException(nameof(feature));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        return EpsWriter.WriteToFile(feature, filePath, options);
    }

    /// <summary>
    /// Parses EPS string and converts to Geometry
    /// </summary>
    /// <param name="epsContent">The EPS content as string</param>
    /// <param name="srid">The spatial reference system identifier (default: 0)</param>
    /// <returns>Geometry object</returns>
    public static Geometry<Point> ToGeometry(this string epsContent, int srid = 0)
    {
        if (string.IsNullOrWhiteSpace(epsContent))
            throw new ArgumentException("EPS content cannot be null or empty", nameof(epsContent));

        return EpsReader.Read(epsContent, srid);
    }

    /// <summary>
    /// Parses EPS string and converts to Feature
    /// </summary>
    /// <param name="epsContent">The EPS content as string</param>
    /// <param name="srid">The spatial reference system identifier (default: 0)</param>
    /// <param name="preserveAttributes">Whether to preserve EPS metadata as Feature attributes (default: true)</param>
    /// <returns>Feature object</returns>
    public static Feature<Point> ToFeature(this string epsContent, int srid = 0, bool preserveAttributes = true)
    {
        if (string.IsNullOrWhiteSpace(epsContent))
            throw new ArgumentException("EPS content cannot be null or empty", nameof(epsContent));

        return EpsReader.ReadFeature(epsContent, srid, preserveAttributes);
    }
}
