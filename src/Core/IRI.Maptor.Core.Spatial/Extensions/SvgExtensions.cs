using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.IO.Svg;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Extensions;

/// <summary>
/// Extension methods for converting Geometry and Feature to/from SVG format
/// </summary>
public static class SvgExtensions
{
    /// <summary>
    /// Converts Geometry to SVG string
    /// </summary>
    /// <param name="geometry">The geometry to convert</param>
    /// <param name="options">SVG options for styling and formatting</param>
    /// <returns>SVG XML string</returns>
    public static string ToSvg(this Geometry<Point> geometry, SvgOptions? options = null)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        return SvgWriter.Write(geometry, options);
    }

    /// <summary>
    /// Saves Geometry to SVG file
    /// </summary>
    /// <param name="geometry">The geometry to save</param>
    /// <param name="filePath">The path to save the SVG file</param>
    /// <param name="options">SVG options for styling and formatting</param>
    /// <returns>The path to the saved file</returns>
    public static string SaveAsSvg(this Geometry<Point> geometry, string filePath, SvgOptions? options = null)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        return SvgWriter.WriteToFile(geometry, filePath, options);
    }

    /// <summary>
    /// Converts Feature to SVG string
    /// </summary>
    /// <param name="feature">The feature to convert</param>
    /// <param name="options">SVG options for styling and formatting</param>
    /// <returns>SVG XML string</returns>
    public static string ToSvg(this Feature<Point> feature, SvgOptions? options = null)
    {
        if (feature == null)
            throw new ArgumentNullException(nameof(feature));

        return SvgWriter.Write(feature, options);
    }

    /// <summary>
    /// Saves Feature to SVG file
    /// </summary>
    /// <param name="feature">The feature to save</param>
    /// <param name="filePath">The path to save the SVG file</param>
    /// <param name="options">SVG options for styling and formatting</param>
    /// <returns>The path to the saved file</returns>
    public static string SaveAsSvg(this Feature<Point> feature, string filePath, SvgOptions? options = null)
    {
        if (feature == null)
            throw new ArgumentNullException(nameof(feature));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        return SvgWriter.WriteToFile(feature, filePath, options);
    }

    /// <summary>
    /// Parses SVG string and converts to Geometry
    /// </summary>
    /// <param name="svgContent">The SVG content as string</param>
    /// <param name="srid">The spatial reference system identifier (default: 0)</param>
    /// <returns>Geometry object</returns>
    public static Geometry<Point> ToGeometry(this string svgContent, int srid = 0)
    {
        if (string.IsNullOrWhiteSpace(svgContent))
            throw new ArgumentException("SVG content cannot be null or empty", nameof(svgContent));

        return SvgReader.Read(svgContent, srid);
    }

    /// <summary>
    /// Parses SVG string and converts to Feature
    /// </summary>
    /// <param name="svgContent">The SVG content as string</param>
    /// <param name="srid">The spatial reference system identifier (default: 0)</param>
    /// <param name="preserveAttributes">Whether to preserve SVG attributes as Feature attributes (default: true)</param>
    /// <returns>Feature object</returns>
    public static Feature<Point> ToFeature(this string svgContent, int srid = 0, bool preserveAttributes = true)
    {
        if (string.IsNullOrWhiteSpace(svgContent))
            throw new ArgumentException("SVG content cannot be null or empty", nameof(svgContent));

        return SvgReader.ReadFeature(svgContent, srid, preserveAttributes);
    }
}