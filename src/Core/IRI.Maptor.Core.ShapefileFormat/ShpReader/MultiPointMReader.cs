// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Primitives;

using IRI.Maptor.Core.ShapefileFormat.EsriType;
using IRI.Maptor.Core.ShapefileFormat.ShpReader;

namespace IRI.Maptor.Core.ShapefileFormat.Reader;

public class MultiPointMReader : MeasuresReader<EsriMultiPointM>
{
    public MultiPointMReader(string fileName, int srid)
        : base(fileName, EsriShapeType.EsriMultiPointM, srid)
    {
    }

    protected override EsriMultiPointM ReadElement()
    {
        int shapeType = shpReader.ReadInt32();

        if ((EsriShapeType)shapeType != EsriShapeType.EsriMultiPointM)
        {
            throw new NotImplementedException();
        }

        BoundingBox boundingBox = this.ReadBoundingBox();

        int numPoints = shpReader.ReadInt32();

        EsriPoint[] points = this.ReadPoints(numPoints, this._srid);

        double minMeasure, maxMeasure;

        double[] measures;

        this.ReadMeasures(numPoints, out minMeasure, out maxMeasure, out measures);

        return new EsriMultiPointM(boundingBox, points, minMeasure, maxMeasure, measures);
    }


    public static EsriMultiPointM Read(System.IO.BinaryReader reader, int offset, int contentLength, int srid)
    {
        //+8: pass the record header; +4 pass the shapeType
        reader.BaseStream.Position = offset * 2 + 8 + 4;

        var boundingBox = ShpBinaryReader.ReadBoundingBox(reader);

        var numPoints = reader.ReadInt32();

        var points = ShpBinaryReader.ReadPoints(reader, numPoints, srid);

        double minMeasure, maxMeasure;

        double[] measures;

        ShpBinaryReader.ReadValues(reader, numPoints, out minMeasure, out maxMeasure, out measures);

        return new EsriMultiPointM(boundingBox, points, minMeasure, maxMeasure, measures);
    }

    public static EsriMultiPointM ParseGdbRecord(byte[] bytes, int srid)
    {
        // 4: shape type
        var offset = 4;

        var boundingBox = ShpBinaryReader.ReadBoundingBox(bytes, offset);
        offset += 4 * ShapeConstants.DoubleSize;

        var numPoints = BitConverter.ToInt32(bytes, offset);
        offset += ShapeConstants.IntegerSize;

        var points = ShpBinaryReader.ReadPoints(bytes, offset, numPoints, srid);
        offset += numPoints * 2 * ShapeConstants.DoubleSize;

        double minMeasure, maxMeasure;

        double[] measures;

        ShpBinaryReader.ReadValues(bytes, offset, numPoints, out minMeasure, out maxMeasure, out measures);

        return new EsriMultiPointM(boundingBox, points, minMeasure, maxMeasure, measures);
    }
}
