using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.Eps;

/// <summary>
/// Extension methods for reading EPS files and converting to Geometry
/// </summary>
public static class EpsReaderExtensions
{
    /// <summary>
    /// Parses an EPS string and converts it to a Geometry object
    /// </summary>
    /// <param name="epsContent">The EPS file content as string</param>
    /// <param name="srid">The spatial reference system identifier (default: 0)</param>
    /// <returns>Geometry object</returns>
    public static Geometry<Point> FromEps(this string epsContent, int srid = 0)
    {
        if (string.IsNullOrWhiteSpace(epsContent))
            throw new ArgumentException("EPS content cannot be null or empty", nameof(epsContent));

        return EpsReader.Read(epsContent, srid);
    }

    /// <summary>
    /// Reads an EPS file and converts it to a Geometry object
    /// </summary>
    /// <param name="fileInfo">The EPS file to read</param>
    /// <param name="srid">The spatial reference system identifier (default: 0)</param>
    /// <returns>Geometry object</returns>
    public static Geometry<Point> ReadEps(this FileInfo fileInfo, int srid = 0)
    {
        if (fileInfo == null)
            throw new ArgumentNullException(nameof(fileInfo));

        if (!fileInfo.Exists)
            throw new FileNotFoundException("EPS file not found", fileInfo.FullName);

        return EpsReader.ReadFromFile(fileInfo.FullName, srid);
    }

    /// <summary>
    /// Parses an EPS string and converts it to a Feature object
    /// </summary>
    /// <param name="epsContent">The EPS file content as string</param>
    /// <param name="srid">The spatial reference system identifier (default: 0)</param>
    /// <param name="preserveAttributes">Whether to preserve EPS metadata as Feature attributes (default: true)</param>
    /// <returns>Feature object</returns>
    public static Feature<Point> FromEpsFeature(this string epsContent, int srid = 0, bool preserveAttributes = true)
    {
        if (string.IsNullOrWhiteSpace(epsContent))
            throw new ArgumentException("EPS content cannot be null or empty", nameof(epsContent));

        return EpsReader.ReadFeature(epsContent, srid, preserveAttributes);
    }

    /// <summary>
    /// Reads an EPS file and converts it to a Feature object
    /// </summary>
    /// <param name="fileInfo">The EPS file to read</param>
    /// <param name="srid">The spatial reference system identifier (default: 0)</param>
    /// <param name="preserveAttributes">Whether to preserve EPS metadata as Feature attributes (default: true)</param>
    /// <returns>Feature object</returns>
    public static Feature<Point> ReadEpsFeature(this FileInfo fileInfo, int srid = 0, bool preserveAttributes = true)
    {
        if (fileInfo == null)
            throw new ArgumentNullException(nameof(fileInfo));

        if (!fileInfo.Exists)
            throw new FileNotFoundException("EPS file not found", fileInfo.FullName);

        return EpsReader.ReadFeatureFromFile(fileInfo.FullName, srid, preserveAttributes);
    }
}
