using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Ogc.SFA;

public static class WkbGeometryTypeFactory
{
    public static Dictionary<Type, WkbGeometryType> WkbTypeMap;

    static WkbGeometryTypeFactory()
    {
        WkbTypeMap = new Dictionary<Type, WkbGeometryType>
        {
            //WkbTypeMap.Add(typeof(WkbGeometry<Point>), WkbGeometryType.Geometry);
            { typeof(OgcGenericPoint<OgcPoint>), WkbGeometryType.Point },
            { typeof(OgcLineString<OgcPoint>), WkbGeometryType.LineString },
            { typeof(OgcPolygon<OgcPoint>), WkbGeometryType.Polygon },
            { typeof(OgcMultiPoint<OgcPoint>), WkbGeometryType.MultiPoint },
            { typeof(OgcMultiLineString<OgcPoint>), WkbGeometryType.MultiLineString },
            { typeof(OgcMultiPolygon<OgcPoint>), WkbGeometryType.MultiPolygon },
            //WkbTypeMap.Add(typeof(WkbGeometryCollection<Point>), WkbGeometryType.GeometryCollection);
            //WkbTypeMap.Add(typeof(WkbCircularString<Point>), WkbGeometryType.CircularString);
            //WkbTypeMap.Add(typeof(WkbCompoundCurve<Point>), WkbGeometryType.CompoundCurve);
            //WkbTypeMap.Add(typeof(WkbCurvePolygon<Point>), WkbGeometryType.CurvePolygon);
            //WkbTypeMap.Add(typeof(WkbMultiCurve<Point>), WkbGeometryType.MultiCurve);
            //WkbTypeMap.Add(typeof(WkbMultiSurface<Point>), WkbGeometryType.MultiSurface);
            //WkbTypeMap.Add(typeof(WkbCurve<Point>), WkbGeometryType.Curve);
            //WkbTypeMap.Add(typeof(WkbSurface<Point>), WkbGeometryType.Surface);
            //WkbTypeMap.Add(typeof(WkbPolyhedralSurface<Point>), WkbGeometryType.PolyhedralSurface);
            //WkbTypeMap.Add(typeof(WkbTIN<Point>), WkbGeometryType.TIN);

            //WkbTypeMap.Add(typeof(WkbGeometry<PointZ>), WkbGeometryType.GeometryZ);
            { typeof(OgcGenericPoint<OgcPointZ>), WkbGeometryType.PointZ },
            { typeof(OgcLineString<OgcPointZ>), WkbGeometryType.LineStringZ },
            { typeof(OgcPolygon<OgcPointZ>), WkbGeometryType.PolygonZ },
            { typeof(OgcMultiPoint<OgcPointZ>), WkbGeometryType.MultiPointZ },
            { typeof(OgcMultiLineString<OgcPointZ>), WkbGeometryType.MultiLineStringZ },
            { typeof(OgcMultiPolygon<OgcPointZ>), WkbGeometryType.MultiPolygonZ },
            //WkbTypeMap.Add(typeof(WkbGeometryCollection<PointZ>), WkbGeometryType.GeometryCollectionZ);
            //WkbTypeMap.Add(typeof(WkbCircularString<PointZ>), WkbGeometryType.CircularStringZ);
            //WkbTypeMap.Add(typeof(WkbCompoundCurve<PointZ>), WkbGeometryType.CompoundCurveZ);
            //WkbTypeMap.Add(typeof(WkbCurvePolygon<PointZ>), WkbGeometryType.CurvePolygonZ);
            //WkbTypeMap.Add(typeof(WkbMultiCurve<PointZ>), WkbGeometryType.MultiCurveZ);
            //WkbTypeMap.Add(typeof(WkbMultiSurface<PointZ>), WkbGeometryType.MultiSurfaceZ);
            //WkbTypeMap.Add(typeof(WkbCurve<PointZ>), WkbGeometryType.CurveZ);
            //WkbTypeMap.Add(typeof(WkbSurface<PointZ>), WkbGeometryType.SurfaceZ);
            //WkbTypeMap.Add(typeof(WkbPolyhedralSurface<PointZ>), WkbGeometryType.PolyhedralSurfaceZ);
            //WkbTypeMap.Add(typeof(WkbTIN<PointZ>), WkbGeometryType.TINZ);

            //WkbTypeMap.Add(typeof(WkbGeometry<PointM>), WkbGeometryType.GeometryM);
            { typeof(OgcGenericPoint<OgcPointM>), WkbGeometryType.PointM },
            { typeof(OgcLineString<OgcPointM>), WkbGeometryType.LineStringM },
            { typeof(OgcPolygon<OgcPointM>), WkbGeometryType.PolygonM },
            { typeof(OgcMultiPoint<OgcPointM>), WkbGeometryType.MultiPointM },
            { typeof(OgcMultiLineString<OgcPointM>), WkbGeometryType.MultiLineStringM },
            { typeof(OgcMultiPolygon<OgcPointM>), WkbGeometryType.MultiPolygonM },
            //WkbTypeMap.Add(typeof(WkbGeometryCollection<PointM>), WkbGeometryType.GeometryCollectionM);
            //WkbTypeMap.Add(typeof(WkbCircularString<PointM>), WkbGeometryType.CircularStringM);
            //WkbTypeMap.Add(typeof(WkbCompoundCurve<PointM>), WkbGeometryType.CompoundCurveM);
            //WkbTypeMap.Add(typeof(WkbCurvePolygon<PointM>), WkbGeometryType.CurvePolygonM);
            //WkbTypeMap.Add(typeof(WkbMultiCurve<PointM>), WkbGeometryType.MultiCurveM);
            //WkbTypeMap.Add(typeof(WkbMultiSurface<PointM>), WkbGeometryType.MultiSurfaceM);
            //WkbTypeMap.Add(typeof(WkbCurve<PointM>), WkbGeometryType.CurveM);
            //WkbTypeMap.Add(typeof(WkbSurface<PointM>), WkbGeometryType.SurfaceM);
            //WkbTypeMap.Add(typeof(WkbPolyhedralSurface<PointM>), WkbGeometryType.PolyhedralSurfaceM);
            //WkbTypeMap.Add(typeof(WkbTIN<PointM>), WkbGeometryType.TIN);

            //WkbTypeMap.Add(typeof(WkbGeometry<PointZM>), WkbGeometryType.GeometryZM);
            { typeof(OgcGenericPoint<OgcPointZM>), WkbGeometryType.PointZM },
            { typeof(OgcLineString<OgcPointZM>), WkbGeometryType.LineStringZM },
            { typeof(OgcPolygon<OgcPointZM>), WkbGeometryType.PolygonZM },
            { typeof(OgcMultiPoint<OgcPointZM>), WkbGeometryType.MultiPointZM },
            { typeof(OgcMultiLineString<OgcPointZM>), WkbGeometryType.MultiLineStringZM },
            { typeof(OgcMultiPolygon<OgcPointZM>), WkbGeometryType.MultiPolygonZM }
        };
        //WkbTypeMap.Add(typeof(WkbGeometryCollection<PointZM>), WkbGeometryType.GeometryCollectionZM);
        //WkbTypeMap.Add(typeof(WkbCircularString<PointZM>), WkbGeometryType.CircularStringZM);
        //WkbTypeMap.Add(typeof(WkbCompoundCurve<PointZM>), WkbGeometryType.CompoundCurveZM);
        //WkbTypeMap.Add(typeof(WkbCurvePolygon<PointZM>), WkbGeometryType.CurvePolygonZM);
        //WkbTypeMap.Add(typeof(WkbMultiCurve<PointZM>), WkbGeometryType.MultiCurveZM);
        //WkbTypeMap.Add(typeof(WkbMultiSurface<PointZM>), WkbGeometryType.MultiSurfaceZM);
        //WkbTypeMap.Add(typeof(WkbCurve<PointZM>), WkbGeometryType.CurveZM);
        //WkbTypeMap.Add(typeof(WkbSurface<PointZM>), WkbGeometryType.SurfaceZM);
        //WkbTypeMap.Add(typeof(WkbPolyhedralSurface<PointZM>), WkbGeometryType.PolyhedralSurfaceZM);
        //WkbTypeMap.Add(typeof(WkbTIN<PointZM>), WkbGeometryType.TINZM);

    }
}