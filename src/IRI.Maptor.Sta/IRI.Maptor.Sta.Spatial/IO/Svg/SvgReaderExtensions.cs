using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.Svg;

/// <summary>
/// Extension methods for reading SVG files and converting to Geometry
/// </summary>
public static class SvgReaderExtensions
{
    /// <summary>
    /// Parses an SVG string and converts it to a Geometry object
    /// </summary>
    /// <param name="svgContent">The SVG file content as string</param>
    /// <param name="srid">The spatial reference system identifier (default: 0)</param>
    /// <returns>Geometry object</returns>
    public static Geometry<Point> FromSvg(this string svgContent, int srid = 0)
    {
        if (string.IsNullOrWhiteSpace(svgContent))
            throw new ArgumentException("SVG content cannot be null or empty", nameof(svgContent));

        return SvgReader.Read(svgContent, srid);
    }

    /// <summary>
    /// Reads an SVG file and converts it to a Geometry object
    /// </summary>
    /// <param name="fileInfo">The SVG file to read</param>
    /// <param name="srid">The spatial reference system identifier (default: 0)</param>
    /// <returns>Geometry object</returns>
    public static Geometry<Point> ReadSvg(this FileInfo fileInfo, int srid = 0)
    {
        if (fileInfo == null)
            throw new ArgumentNullException(nameof(fileInfo));

        if (!fileInfo.Exists)
            throw new FileNotFoundException("SVG file not found", fileInfo.FullName);

        return SvgReader.ReadFromFile(fileInfo.FullName, srid);
    }

    /// <summary>
    /// Parses an SVG string and converts it to a Feature object
    /// </summary>
    /// <param name="svgContent">The SVG file content as string</param>
    /// <param name="srid">The spatial reference system identifier (default: 0)</param>
    /// <param name="preserveAttributes">Whether to preserve SVG attributes as Feature attributes (default: true)</param>
    /// <returns>Feature object</returns>
    public static Feature<Point> FromSvgFeature(this string svgContent, int srid = 0, bool preserveAttributes = true)
    {
        if (string.IsNullOrWhiteSpace(svgContent))
            throw new ArgumentException("SVG content cannot be null or empty", nameof(svgContent));

        return SvgReader.ReadFeature(svgContent, srid, preserveAttributes);
    }

    /// <summary>
    /// Reads an SVG file and converts it to a Feature object
    /// </summary>
    /// <param name="fileInfo">The SVG file to read</param>
    /// <param name="srid">The spatial reference system identifier (default: 0)</param>
    /// <param name="preserveAttributes">Whether to preserve SVG attributes as Feature attributes (default: true)</param>
    /// <returns>Feature object</returns>
    public static Feature<Point> ReadSvgFeature(this FileInfo fileInfo, int srid = 0, bool preserveAttributes = true)
    {
        if (fileInfo == null)
            throw new ArgumentNullException(nameof(fileInfo));

        if (!fileInfo.Exists)
            throw new FileNotFoundException("SVG file not found", fileInfo.FullName);

        return SvgReader.ReadFeatureFromFile(fileInfo.FullName, srid, preserveAttributes);
    }
}



