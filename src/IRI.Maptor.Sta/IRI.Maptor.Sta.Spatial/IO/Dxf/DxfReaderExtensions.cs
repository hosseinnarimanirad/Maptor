using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.Dxf;

/// <summary>
/// Extension methods for reading DXF files and converting to Geometry
/// </summary>
public static class DxfReaderExtensions
{
    /// <summary>
    /// Parses a DXF string and converts it to a Geometry object
    /// </summary>
    /// <param name="dxfContent">The DXF file content as string</param>
    /// <param name="srid">The spatial reference system identifier (default: 0)</param>
    /// <returns>Geometry object</returns>
    public static Geometry<Point> FromDxf(this string dxfContent, int srid = 0)
    {
        if (string.IsNullOrWhiteSpace(dxfContent))
            throw new ArgumentException("DXF content cannot be null or empty", nameof(dxfContent));
            
        return DxfReader.Read(dxfContent, srid);
    }
}

