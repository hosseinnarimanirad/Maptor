using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.ShapefileFormat.EsriType;
using Microsoft.SqlServer.Types;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Extensions;

public static class BoundingBoxExtensions
{
    public static SqlGeometry AsSqlGeometry(this BoundingBox boundingBox, int srid = 0)
    {
        //var result =
        //    SqlGeometry.Parse(
        //         string.Format(
        //                System.Globalization.CultureInfo.InvariantCulture,
        //                "POLYGON(({0} {1}, {0} {2}, {3} {2}, {3} {1}, {0} {1}))",
        //                boundingBox.XMin,
        //                boundingBox.YMin,
        //                boundingBox.YMax,
        //                boundingBox.XMax));

        //result.STSrid = srid;

        //return result;

        return SqlGeometry.STPolyFromText(new System.Data.SqlTypes.SqlChars(boundingBox.AsWkt()), srid);
    }

    public static bool Intersects(this BoundingBox boundingBox, SqlGeometry geometry)
    {
        if (geometry.IsNullOrEmpty())
        {
            return false;
        }

        var geometryBoundingBox = geometry.GetBoundingBox();

        if (boundingBox.Intersects(geometryBoundingBox))
        {
            SqlGeometry boundary = boundingBox.AsSqlGeometry(geometry.GetSrid());

            return geometry.STIntersects(boundary).IsTrue;
        }

        return false;
    }

    public static List<SqlGeometry> Tessellate(this BoundingBox boundingBox, int numberOfColumns, int srid)
    {
        var size = boundingBox.Width / numberOfColumns;

        var numberOfRows = Math.Ceiling(boundingBox.Height / size);

        List<SqlGeometry> result = new List<SqlGeometry>();

        var minX = boundingBox.XMin;

        var minY = boundingBox.YMin;

        for (int i = 0; i < numberOfColumns; i++)
        {
            for (int j = 0; j < numberOfRows; j++)
            {
                result.Add(new BoundingBox(minX + i * size, minY + j * size, minX + (i + 1) * size, minY + (j + 1) * size).AsSqlGeometry(srid));
            }
        }

        return result;
    }
}
