using System;

using Microsoft.SqlServer.Types;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;
using System.Data.SqlTypes;

namespace IRI.Maptor.Tst.Standards.OGC.SFA
{
    
    public class GeometryTypesTest
    {
        double x = -10.87453087523;

        double y = 10.2344245235;

        double z = 324.98743523;

        double m = Math.Sin(23) / Math.Pow(Math.PI, 3);

        public GeometryTypesTest()
        {
            //SqlServerTypes.Utilities.LoadNativeAssembliesv14();
        }

        [Fact]
        public void TestPoint()
        {
            SqlGeometry geometry = SqlGeometry.STPointFromText(
                                        new SqlChars(new SqlString(string.Format("POINT({0} {1})", x, y))), 0);
            
            IRI.Maptor.Sta.Ogc.SFA.OgcGenericPoint<IRI.Maptor.Sta.Ogc.SFA.OgcPoint> point =
                new IRI.Maptor.Sta.Ogc.SFA.OgcGenericPoint<IRI.Maptor.Sta.Ogc.SFA.OgcPoint>(geometry.AsBinaryZM().Buffer);
            Assert.Equal(point.X, geometry.STX);
            Assert.Equal(point.Y, geometry.STY);


            SqlGeometry geometryZ = SqlGeometry.STPointFromText(
                                        new SqlChars(new SqlString(string.Format("POINT({0} {1} {2})", x, y, z))), 0);
            IRI.Maptor.Sta.Ogc.SFA.OgcGenericPoint<IRI.Maptor.Sta.Ogc.SFA.OgcPointZ> pointZ =
                new IRI.Maptor.Sta.Ogc.SFA.OgcGenericPoint<IRI.Maptor.Sta.Ogc.SFA.OgcPointZ>(geometryZ.AsBinaryZM().Buffer);
            Assert.Equal(pointZ.X, geometryZ.STX);
            Assert.Equal(pointZ.Y, geometryZ.STY);


            SqlGeometry geometryZM = SqlGeometry.STPointFromText(
                                        new SqlChars(new SqlString(string.Format("POINT({0} {1} {2} {3})", x, y, z, m))), 0);
            IRI.Maptor.Sta.Ogc.SFA.OgcGenericPoint<IRI.Maptor.Sta.Ogc.SFA.OgcPointZM> pointZM =
                new IRI.Maptor.Sta.Ogc.SFA.OgcGenericPoint<IRI.Maptor.Sta.Ogc.SFA.OgcPointZM>(geometryZM.AsBinaryZM().Buffer);
            Assert.Equal(pointZM.X, geometryZM.STX);
            Assert.Equal(pointZM.Y, geometryZM.STY);
        }

        [Fact]
        public void TestLineString()
        {
            SqlGeometry geometry =
                SqlGeometry.STLineFromText(
                    new SqlChars(
                        new SqlString(
                            string.Format("LINESTRING({0} {1}, {2} {3}, {0} {3}, {1} {2}, {3} {1})", x, y, z, m))), 0);
            IRI.Maptor.Sta.Ogc.SFA.OgcLineString<IRI.Maptor.Sta.Ogc.SFA.OgcPoint> linestring =
                new IRI.Maptor.Sta.Ogc.SFA.OgcLineString<IRI.Maptor.Sta.Ogc.SFA.OgcPoint>(geometry.AsBinaryZM().Buffer);
            Assert.Equal(linestring.ToWkb(), geometry.AsBinaryZM().Buffer);


            SqlGeometry geometryZ =
                SqlGeometry.STLineFromText(
                    new SqlChars(
                        new SqlString(
                            string.Format("LINESTRING({0} {1} {1}, {2} {3} {1}, {0} {3} {2}, {1} {2} {2}, {3} {1} {2})", x, y, z, m))), 0);
            IRI.Maptor.Sta.Ogc.SFA.OgcLineString<IRI.Maptor.Sta.Ogc.SFA.OgcPointZ> linestringZ =
                new IRI.Maptor.Sta.Ogc.SFA.OgcLineString<IRI.Maptor.Sta.Ogc.SFA.OgcPointZ>(geometryZ.AsBinaryZM().Buffer);
            Assert.Equal(linestringZ.ToWkb(), geometryZ.AsBinaryZM().Buffer);

            SqlGeometry geometryZM =
                SqlGeometry.STLineFromText(
                    new SqlChars(
                        new SqlString(
                            string.Format("LINESTRING({0} {1} {1} {0}, {2} {3} {1} {0}, {0} {3} {2} {0}, {1} {2} {2} {0}, {3} {1} {2} {2})", x, y, z, m))), 0);
            IRI.Maptor.Sta.Ogc.SFA.OgcLineString<IRI.Maptor.Sta.Ogc.SFA.OgcPointZM> linestringZM =
                new IRI.Maptor.Sta.Ogc.SFA.OgcLineString<IRI.Maptor.Sta.Ogc.SFA.OgcPointZM>(geometryZM.AsBinaryZM().Buffer);
            Assert.Equal(linestringZM.ToWkb(), geometryZM.AsBinaryZM().Buffer);
        }

        [Fact]
        public void TestPolygon()
        {
            SqlGeometry geometry =
                SqlGeometry.STPolyFromText(
                    new SqlChars(
                        new SqlString(
                            string.Format("POLYGON(({0} {1}, {2} {3}, {0} {3}, {1} {2}, {3} {1}, {0} {1}))", x, y, z, m))), 0);
            IRI.Maptor.Sta.Ogc.SFA.OgcPolygon<IRI.Maptor.Sta.Ogc.SFA.OgcPoint> polygon =
                new IRI.Maptor.Sta.Ogc.SFA.OgcPolygon<IRI.Maptor.Sta.Ogc.SFA.OgcPoint>(geometry.AsBinaryZM().Buffer);
            Assert.Equal(polygon.ToWkb(), geometry.AsBinaryZM().Buffer);


            SqlGeometry geometryZ =
                SqlGeometry.STPolyFromText(
                    new SqlChars(
                        new SqlString(
                            string.Format("POLYGON(({0} {1} {1}, {2} {3} {1}, {0} {3} {2}, {1} {2} {2}, {3} {1} {2}, {0} {1} {1}))", x, y, z, m))), 0);
            IRI.Maptor.Sta.Ogc.SFA.OgcPolygon<IRI.Maptor.Sta.Ogc.SFA.OgcPointZ> polygonZ =
                new IRI.Maptor.Sta.Ogc.SFA.OgcPolygon<IRI.Maptor.Sta.Ogc.SFA.OgcPointZ>(geometryZ.AsBinaryZM().Buffer);
            Assert.Equal(polygonZ.ToWkb(), geometryZ.AsBinaryZM().Buffer);


            SqlGeometry geometryZM =
                SqlGeometry.STPolyFromText(
                    new SqlChars(
                        new SqlString(
                            string.Format("POLYGON(({0} {1} {1} {0}, {2} {3} {1} {0}, {0} {3} {2} {0}, {1} {2} {2} {0}, {3} {1} {2} {2}, {0} {1} {1} {0}))", x, y, z, m))), 0);
            IRI.Maptor.Sta.Ogc.SFA.OgcPolygon<IRI.Maptor.Sta.Ogc.SFA.OgcPointZM> polygonZM =
                new IRI.Maptor.Sta.Ogc.SFA.OgcPolygon<IRI.Maptor.Sta.Ogc.SFA.OgcPointZM>(geometryZM.AsBinaryZM().Buffer);
            Assert.Equal(polygonZM.ToWkb(), geometryZM.AsBinaryZM().Buffer);
        }


    }
}
