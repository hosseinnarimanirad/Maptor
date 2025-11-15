using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Tst.Assets.SqlSpatialSamples
{
    static class SqlGeometrySamples
    {
        static SqlGeometry Point =
               SqlGeometry.STPointFromText(
                   new System.Data.SqlTypes.SqlChars("POINT ( 3 4.2)"), 0);

        static SqlGeometry PointM =
            SqlGeometry.STPointFromText(
                new System.Data.SqlTypes.SqlChars("POINT ( 3 4.2 NULL 2.5)"), 0);

        static SqlGeometry PointZ =
            SqlGeometry.STPointFromText(
                new System.Data.SqlTypes.SqlChars("POINT ( 3 4.2 10 NULL)"), 0);

        static SqlGeometry PointZM =
            SqlGeometry.STPointFromText(
                new System.Data.SqlTypes.SqlChars("POINT ( 3 4.2 10 2.5)"), 0);

        static SqlGeometry LineStringEmpty =
            SqlGeometry.STLineFromText(
                new System.Data.SqlTypes.SqlChars("LINESTRING EMPTY"), 0);

        static SqlGeometry LineStringTwoPoints =
            SqlGeometry.STLineFromText(
                new System.Data.SqlTypes.SqlChars("LINESTRING(1 1, 3 3)"), 0);

        static SqlGeometry LineStringFourPoints =
            SqlGeometry.STLineFromText(
                new System.Data.SqlTypes.SqlChars("LINESTRING(1 1, 3 3, 2 4, 2 0)"), 0);

        static SqlGeometry LineStringWithMOrZ =
            SqlGeometry.STLineFromText(
                new System.Data.SqlTypes.SqlChars("LINESTRING(1 1 NULL 1, 3 3 10 NULL, 2 4 1.1 1.2, 2 0 NULL NULL)"), 0);

        static SqlGeometry LineStringClosed =
            SqlGeometry.STLineFromText(
                new System.Data.SqlTypes.SqlChars("LINESTRING(1 1, 3 3, 2 4, 2 0, 1 1)"), 0);
    }
}
