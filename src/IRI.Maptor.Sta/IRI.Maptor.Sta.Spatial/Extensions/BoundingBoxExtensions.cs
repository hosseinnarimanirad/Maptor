using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Topology;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Extensions;

public static class BoundingBoxExtensions
{
    public static Geometry<Point> GeodeticWgs84MbbToUtmGeometry(this BoundingBox boundingBox, int? zone = null)
    {
        zone = zone ?? MapProjects.FindUtmZone(boundingBox.Center.X);
        //var zone = CoordinateSystem.MapProjection.MapProjects.FindZone(boundingBox.Center.X);

        var isNorthHemisphere = boundingBox.Center.Y > 0;

        return boundingBox.TransofrmBy8Point(p => MapProjects.GeodeticToUTM(p, Ellipsoids.WGS84, zone.Value, isNorthHemisphere));
    }

    public static BoundingBox GeodeticWgs84MbbToUtmMbb(this BoundingBox boundingBox, int? zone = null)
    {
        return GeodeticWgs84MbbToUtmGeometry(boundingBox, zone).GetBoundingBox();
    }

    public static Geometry<Point> UtmMbbToGeodeticWgs84Geometry(this BoundingBox boundingBox, int zone)
    {
        return boundingBox.TransofrmBy8Point(p => MapProjects.UTMToGeodetic(p, zone));
    }

    public static BoundingBox UtmMbbToGeodeticWgs84Mbb(BoundingBox boundingBox, int zone)
    {
        return UtmMbbToGeodeticWgs84Geometry(boundingBox, zone).GetBoundingBox();
    }


    public static Geometry<Point> TransofrmBy4Point(this BoundingBox boundingBox, Func<Point, Point> func)
    {
        var p1 = func(boundingBox.BottomLeft);

        var p3 = func(boundingBox.TopLeft);

        var p5 = func(boundingBox.TopRight);

        var p7 = func(boundingBox.BottomRight);

        return Geometry<Point>.Create(new List<Point>() { p1, p3, p5, p7 }, GeometryType.Polygon, 0);
    }

    public static Geometry<Point> TransofrmBy8Point(this BoundingBox boundingBox, Func<Point, Point> func)
    {
        var p1 = func(boundingBox.BottomLeft);

        var p2 = func(boundingBox.MiddleLeft);

        var p3 = func(boundingBox.TopLeft);

        var p4 = func(boundingBox.MiddleTop);

        var p5 = func(boundingBox.TopRight);

        var p6 = func(boundingBox.MiddleRight);

        var p7 = func(boundingBox.BottomRight);

        var p8 = func(boundingBox.MiddleBottom);

        return Geometry<Point>.Create(new List<Point>() { p1, p2, p3, p4, p5, p6, p7, p8 }, GeometryType.Polygon, 0);
    }


    public static Geometry<T> AsGeometry<T>(this BoundingBox boundingBox, int srid) where T : IPoint, new()
    {
        return Geometry<T>.Create(boundingBox.GetClockWiseOrderOfEsriPoints<T>()/*.ToArray()*/, GeometryType.Polygon, srid);
    }


    public static bool IntersectsLineSegment<T>(this BoundingBox boundingBox, T p1, T p2) where T : IPoint, new()
    {
        if (boundingBox.Covers(p1) || boundingBox.Covers(p2))
            return true;

        var lineSegmentBoundingBox = BoundingBox.Create(p1, p2);

        if (!boundingBox.Intersects(lineSegmentBoundingBox))
            return false;

        Point intersection;

        Point tempP1 = new Point(p1.X, p1.Y);
        Point tempP2 = new Point(p2.X, p2.Y);

        var i1 = TopologyUtility.LineSegmentsIntersects(tempP1, tempP2, boundingBox.BottomLeft, boundingBox.BottomRight, out intersection);

        if (i1 == LineLineSegmentRelation.Intersect)
            return true;

        var i2 = TopologyUtility.LineSegmentsIntersects(tempP1, tempP2, boundingBox.BottomRight, boundingBox.TopRight, out intersection);

        if (i2 == LineLineSegmentRelation.Intersect)
            return true;

        var i3 = TopologyUtility.LineSegmentsIntersects(tempP1, tempP2, boundingBox.TopRight, boundingBox.TopLeft, out intersection);

        if (i3 == LineLineSegmentRelation.Intersect)
            return true;

        var i4 = TopologyUtility.LineSegmentsIntersects(tempP1, tempP2, boundingBox.TopLeft, boundingBox.BottomLeft, out intersection);

        if (i4 == LineLineSegmentRelation.Intersect)
            return true;

        return false;
    }
}
