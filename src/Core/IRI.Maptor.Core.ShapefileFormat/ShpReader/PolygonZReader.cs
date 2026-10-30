// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Primitives;

using IRI.Maptor.Core.ShapefileFormat.EsriType;
using IRI.Maptor.Core.ShapefileFormat.ShpReader;
using IRI.Maptor.Core.Spatial.IO.EsriJson;


namespace IRI.Maptor.Core.ShapefileFormat.Reader;

public class PolygonZReader : zReader<EsriPolygonZ>
{
    public PolygonZReader(string fileName, int srid)
        : base(fileName, EsriShapeType.EsriPolygonZM, srid)
    {

    }

    protected override EsriPolygonZ ReadElement()
    {
        int shapeType = shpReader.ReadInt32();

        if ((EsriShapeType)shapeType != EsriShapeType.EsriPolygonZM)
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

        double minZ, maxZ;

        double[] zValues;

        this.ReadZValues(numPoints, out minZ, out maxZ, out zValues);

        double minMeasure = EsriConstants.NoDataValue, maxMeasure = EsriConstants.NoDataValue;

        double[] measures = new double[numPoints];

        if (shpReader.BaseStream.Position != shpReader.BaseStream.Length)
        {
            this.ReadMeasures(numPoints, out minMeasure, out maxMeasure, out measures);
        }

        return new EsriPolygonZ(boundingBox, parts, points, minZ, maxZ, zValues, minMeasure, maxMeasure, measures);

    }

    public static EsriPolygonZ Read(System.IO.BinaryReader reader, int offset, int contentLength, int srid)
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

        double minZ, maxZ;

        double[] zValues;

        ShpBinaryReader.ReadValues(reader, numPoints, out minZ, out maxZ, out zValues);

        double minMeasure = EsriConstants.NoDataValue, maxMeasure = EsriConstants.NoDataValue;

        double[] measures = new double[numPoints];

        if (contentLength > reader.BaseStream.Position * 2 - offset)
        {
            ShpBinaryReader.ReadValues(reader, numPoints, out minMeasure, out maxMeasure, out measures);
        }

        return new EsriPolygonZ(boundingBox, parts, points, minZ, maxZ, zValues, minMeasure, maxMeasure, measures);
    }

    public static EsriPolygonZ ParseGdbRecord(byte[] bytes, int srid, bool hasM)
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
        offset += numPoints * 2 * ShapeConstants.DoubleSize;

        // z values
        double minZ, maxZ;

        double[] zValues;

        ShpBinaryReader.ReadValues(bytes, offset, numPoints, out minZ, out maxZ, out zValues);
        offset += (numPoints + 2) * ShapeConstants.DoubleSize;

        // measure values
        double minMeasure = EsriConstants.NoDataValue, maxMeasure = EsriConstants.NoDataValue;

        double[] measures = new double[numPoints];

        if (hasM)
        {
            ShpBinaryReader.ReadValues(bytes, offset, numPoints, out minMeasure, out maxMeasure, out measures);
        }

        return new EsriPolygonZ(boundingBox, parts, points, minZ, maxZ, zValues, minMeasure, maxMeasure, measures);
    }
}
