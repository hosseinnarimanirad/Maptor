
//using IRI.Maptor.Jab.Common.Models.CoordinateEditor;
//using IRI.Maptor.Jab.Controls.Models;
//using IRI.Maptor.Sta.Common.Primitives;
//using IRI.Maptor.Sta.Spatial.Primitives;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks; 

//namespace IRI.Maptor.Extensions;

//public static class Jab_GeometryExtensions
//{
//    public static CoordinateEditor? AsCoordinateEditor(this Geometry<Point> geometry)
//    {
//        if (geometry == null)
//            return null;

//        switch (geometry.Type)
//        {
//            case GeometryType.LineString:
//                return new LineStringEditorModel(geometry);

//            case GeometryType.Polygon:
//                return new PolygonEditorModel(geometry);

//            case GeometryType.Point:
//                return new PointEditorModel(geometry);

//            case GeometryType.MultiPoint:
//                return new MultiPointEditorModel(geometry);

//            case GeometryType.MultiLineString:
//                return new MultiLineStringEditorModel(geometry);

//            case GeometryType.MultiPolygon:
//                return new MultiPolygonEditorModel(geometry);

//            case GeometryType.GeometryCollection:
//            case GeometryType.CircularString:
//            case GeometryType.CompoundCurve:
//            case GeometryType.CurvePolygon:
//            default:
//                throw new NotImplementedException();
//        }
//    }
//}
