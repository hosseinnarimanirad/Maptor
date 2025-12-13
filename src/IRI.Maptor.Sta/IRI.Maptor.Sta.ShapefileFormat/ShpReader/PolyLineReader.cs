// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Sta.Common.Primitives;

using IRI.Maptor.Sta.ShapefileFormat.EsriType;
using IRI.Maptor.Sta.ShapefileFormat.ShpReader;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.ShapefileFormat.Reader;

public class PolyLineReader : PointsReader<EsriPolyline>
{
    //Because this specification does not forbid consecutive points with identical coordinates,
    //shapefile readers must handle such cases. On the other hand, the degenerate, zero length
    //parts that might result are not allowed.
    public PolyLineReader(string fileName, int srid)
        : base(fileName, EsriShapeType.EsriPolyLine, srid)
    {

    }

    protected override EsriPolyline ReadElement()
    {
        int shapeType = shpReader.ReadInt32();

        if ((EsriShapeType)shapeType != EsriShapeType.EsriPolyLine)
        {
            throw new NotImplementedException();
        }

        BoundingBox boundingBox = this.ReadBoundingBox();

        int numParts = shpReader.ReadInt32();

        int numPoints = shpReader.ReadInt32();

        int[] parts = new int[numParts];

        for (int i = 0; i < numParts; i++)
        {
            parts[i] = shpReader.ReadInt32();
        }

        EsriPoint[] points = this.ReadPoints(numPoints, this._srid);

        return new EsriPolyline(boundingBox, parts, points);
    }

    public static EsriPolyline Read(System.IO.BinaryReader reader, int offset, int contentLength, int srid)
    {
        //+8: pass the record header; +4 pass the shapeType
        reader.BaseStream.Position = offset * 2 + 8 + 4;

        //var byteArray = reader.ReadBytes(contentLength * 2 - 8);

        var boundingBox = ShpBinaryReader.ReadBoundingBox(reader);

        int numParts = reader.ReadInt32();

        int numPoints = reader.ReadInt32();

        int[] parts = new int[numParts];

        for (int i = 0; i < numParts; i++)
        {
            parts[i] = reader.ReadInt32();
        }

        var points = ShpBinaryReader.ReadPoints(reader, numPoints, srid);

        return new EsriPolyline(boundingBox, parts, points);
    }


    public static EsriPolyline ParseGdbRecord(byte[] bytes, int srid)
    {
        // 4: shape type
        var offset = 4;

        var boundingBox = ShpBinaryReader.ReadBoundingBox(bytes, offset);
        offset += 4 * ShapeConstants.DoubleSize;

        var numParts = BitConverter.ToInt32(bytes, offset);
        offset += ShapeConstants.IntegerSize;

        var numPoints = BitConverter.ToInt32(bytes, offset);
        offset += ShapeConstants.IntegerSize;

        int[] parts = new int[numParts];

        for (int i = 0; i < numParts; i++)
        {
            parts[i] = BitConverter.ToInt32(bytes, offset);
            offset += ShapeConstants.IntegerSize;
        }

        var points = ShpBinaryReader.ReadPoints(bytes, offset, numPoints, srid);

        return new EsriPolyline(boundingBox, parts, points);
    }
}
