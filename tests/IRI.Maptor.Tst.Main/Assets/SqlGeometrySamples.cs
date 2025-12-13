using System.Data.SqlTypes;
using System.Collections.Generic;

using Microsoft.SqlServer.Types;

namespace IRI.Maptor.Tst.Assets
{
    public static class SqlGeometrySamples
    {
        public static SqlGeometry Point = SqlGeometry.Parse(new SqlString("Point(1 2)"));

        public static SqlGeometry PointZ = SqlGeometry.Parse(new SqlString("Point(1 2 1)"));

        public static SqlGeometry PointZM = SqlGeometry.Parse(new SqlString("Point(1 2 2 3)"));

        public static SqlGeometry MultipointComplex = SqlGeometry.Parse(new SqlString("MULTIPOINT((0 0), (0 3), (3 3), (3 0), (1 1 1), (9 9 1 2), (9 10), (10 9))"));

        public static SqlGeometry Multipoint = SqlGeometry.Parse(new SqlString("MULTIPOINT((2 3), (7 8))"));


        public static SqlGeometry Linestring = SqlGeometry.Parse(new SqlString("LINESTRING(1 1, 2 0,  2 4, 3 3)"));

        public static SqlGeometry LinestringZM = SqlGeometry.Parse(new SqlString("LINESTRING( 4 4 4 4, 9 0 4 4 )"));

        public static SqlGeometry MultiLineString = SqlGeometry.Parse(new SqlString("MULTILINESTRING((1 1, 3 5), (-5 3, -8 -2))"));

        public static SqlGeometry Polygon = SqlGeometry.Parse(new SqlString("POLYGON((0 0 9, 30 0 9, 30 30 9, 0 30 9, 0 0 9))"));

        public static SqlGeometry PolygonWithHole = SqlGeometry.Parse(new SqlString("POLYGON((-20 -20, 20 -20, 20 20, -20 20, -20 -20), (10 0, 0 -10, 0 10, 10 0))"));

        public static SqlGeometry PolygonWithTwoHole = SqlGeometry.Parse(new SqlString("POLYGON((-20 -20, 20 -20, 20 20, -20 20, -20 -20), (10 0, 0 -10, 0 10, 10 0), (-10 0, -15 0, -10 10, -10 0))"));

        public static SqlGeometry MultiPolygon01 = SqlGeometry.Parse(new SqlString("MULTIPOLYGON(((0 0, 3 0, 3 3, 0 3, 0 0), (2 1, 1 1, 1 2, 2 1)), ((9 9, 10 9, 9 10, 9 9)))"));

        public static SqlGeometry MultiPolygon02 = SqlGeometry.Parse(new SqlString("MULTIPOLYGON(((0 0, 6 0, 6 6, 0 6, 0 0), (1 5, 5 5, 5 1, 1 1, 1 5)), ((4 4, 2 4, 2 2, 4 2, 4 4),(3.5 3.5, 3.5 2.5, 2.5 2.5, 2.5 3.5, 3.5 3.5)))"));

        public static List<SqlGeometry> AllGeometries = new List<SqlGeometry>()
        {
            Point,PointZ, PointZM,
            MultipointComplex,Multipoint,
            Linestring,LinestringZM,
            MultiLineString,
            Polygon,PolygonWithHole,PolygonWithTwoHole,
            MultiPolygon01,MultiPolygon02
        };


        // line string with two 90 angle change
        public static SqlGeometry LineString_WithTwo90DegreeAngleChange = SqlGeometry.Parse(new SqlString("LINESTRING(1 1, 3 3, 2 4, 2 0, 1 1)"));

        // multi linestring with two linestring owning two 90 angle change
        public static SqlGeometry MultiLineString_WithTwo90DegreeAngleChange = SqlGeometry.Parse(new SqlString("MULTILINESTRING((1 1, 3 3, 2 4, 2 0, 1 1), (10 10, 30 30, 20 40, 20 00, 10 10))"));

        // total angle change: 505.3
        // mean angle change: 505.3/(9-2) = 72.1857
        public static SqlGeometry LineString_ForAngularChange = SqlGeometry.Parse(new SqlString("LINESTRING(-15 -3, -14 -5, -13 -3, -12 -4, -12 -6, -11 -6, -11 -5, -10 -4, -9 -3)"));


        public static SqlGeometry Polygon_ForAngularChange = SqlGeometry.Parse(new SqlString("POLYGON((-15 -3, -12 -2, -9 -3, -10 -4, -11 -5, -11 -6, -12 -6, -12 -4, -13 -3, -14 -5, -15 -3))"));
         
        // total vector displacement
        public static SqlGeometry LineString_ForVectorDisplacement_Original = SqlGeometry.Parse(new SqlString("LINESTRING(-4.5 -2, -4 0, -4 1.5, -2 4, -2 5, 0 6, 0 4, 4 4, 4.5 3, 2 0, 8 -2, 4 -4, -2 -8, -4 -6, -4 -4)"));
        public static SqlGeometry LineString_ForVectorDisplacement_Simplified = SqlGeometry.Parse(new SqlString("LINESTRING(-4.5 -2, -2 4, 0 4, 4 4, 2 0, 8 -2, -2 -8, -4 -4)"));

    }
}
