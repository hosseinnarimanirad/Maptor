// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Sta.Common.Primitives;

using IRI.Maptor.Sta.ShapefileFormat.EsriType;
using IRI.Maptor.Sta.ShapefileFormat.ShpReader;

namespace IRI.Maptor.Sta.ShapefileFormat.Reader;

public class PolygonReader : PointsReader<EsriPolygon>
{
    //A polygon consists of one or more rings. A ring is a connected sequence of four or more
    //points that form a closed, non-self-intersecting loop. A polygon may contain multiple
    //outer rings. The order of vertices or orientation for a ring indicates which side of the ring
    //is the interior of the polygon. The neighborhood to the right of an observer walking along
    //the ring in vertex order is the neighborhood inside the polygon. Vertices of rings defining
    //holes in polygons are in a counterclockwise direction. Vertices for a single, ringed
    //polygon are, therefore, always in clockwise order. The rings of a polygon are referred to
    //as its parts.
    //Because this specification does not forbid consecutive points with identical coordinates,
    //shapefile readers must handle such cases. On the other hand, the degenerate, zero length
    //or zero area parts that might result are not allowed.
    public PolygonReader(string fileName, int srid)
        : base(fileName, EsriShapeType.EsriPolygon, srid)
    {

    }


    protected override EsriPolygon ReadElement()
    {
        int shapeType = shpReader.ReadInt32();

        if ((EsriShapeType)shapeType != EsriShapeType.EsriPolygon)
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

        return new EsriPolygon(boundingBox, parts, points);
    }

    public static EsriPolygon Read(System.IO.BinaryReader reader, int offset, int contentLength, int srid)
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

        return new EsriPolygon(boundingBox, parts, points);
    }


    public static EsriPolygon ParseGdbRecord(byte[] bytes, int srid)
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
         
        return new EsriPolygon(boundingBox, parts, points);
    }
}
