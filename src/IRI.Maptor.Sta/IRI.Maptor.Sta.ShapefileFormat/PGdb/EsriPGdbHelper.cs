using System;
using IRI.Maptor.Sta.ShapefileFormat;
using IRI.Maptor.Sta.ShapefileFormat.Reader;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;


namespace IRI.Maptor.Sta.PersonalGdb;

public static class EsriPGdbHelper
{
    public static EsriShapeBase ParseToEsriShape(byte[] bytes, int srid)
    {
        if (bytes is null)
            return null;

        EsriShapeType type = (EsriShapeType)BitConverter.ToInt32(bytes, 0);

        switch (type)
        {
            case EsriShapeType.NullShape:
                return null;

            case EsriShapeType.EsriPoint:
                return PointReader.ParseGdbRecord(bytes, srid);

            case EsriShapeType.EsriPointZM:
                return PointZReader.ParseGdbRecord(bytes, srid, hasM: true);

            case EsriShapeType.EsriPointM:
                return PointMReader.ParseGdbRecord(bytes, srid);

            case EsriShapeType.EsriPointZ:
                return PointZReader.ParseGdbRecord(bytes, srid, hasM: false);

            case EsriShapeType.EsriPolyLine:
                return PolyLineReader.ParseGdbRecord(bytes, srid);

            case EsriShapeType.EsriPolyLineZM:
                return PolyLineZReader.ParseGdbRecord(bytes, srid, hasM: true);

            case EsriShapeType.EsriPolyLineM:
                return PolyLineMReader.ParseGdbRecord(bytes, srid);

            case EsriShapeType.EsriPolyLineZ:
                return PolyLineZReader.ParseGdbRecord(bytes, srid, hasM: false);

            case EsriShapeType.EsriPolygon:
                return PolygonReader.ParseGdbRecord(bytes, srid);

            case EsriShapeType.EsriPolygonZM:
                return PolygonZReader.ParseGdbRecord(bytes, srid, hasM: true);

            case EsriShapeType.EsriPolygonM:
                return PolygonMReader.ParseGdbRecord(bytes, srid);

            case EsriShapeType.EsriPolygonZ:
                return PolygonZReader.ParseGdbRecord(bytes, srid, hasM: false);

            case EsriShapeType.EsriMultiPoint:
                return MultiPointReader.ParseGdbRecord(bytes, srid);

            case EsriShapeType.EsriMultiPointZM:
                return MultiPointZReader.ParseGdbRecord(bytes, srid, hasM: true);

            case EsriShapeType.EsriMultiPointM:
                return MultiPointMReader.ParseGdbRecord(bytes, srid);

            case EsriShapeType.EsriMultiPointZ:
                return MultiPointZReader.ParseGdbRecord(bytes, srid, hasM: false);

            case EsriShapeType.EsriMultiPatch:                    
            default:
                throw new NotImplementedException("EsriPGdbHelper > Parse");
        } 
    }       
}
