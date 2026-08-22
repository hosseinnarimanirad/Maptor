// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj


using IRI.Maptor.Core.ShapefileFormat.EsriType;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.ShapefileFormat.Reader;

public class PointMReader : ShpReader<EsriPointM>
{
    public PointMReader(string fileName, int srid)
        : base(fileName, EsriShapeType.EsriPointM, srid)
    {
    }

    protected override EsriPointM ReadElement()
    {
        int shapeType = shpReader.ReadInt32();

        if ((EsriShapeType)shapeType != EsriShapeType.EsriPointM)
        {
            throw new NotImplementedException();
        }

        double x = shpReader.ReadDouble();

        double y = shpReader.ReadDouble();

        double m = shpReader.ReadDouble();

        return new EsriPointM(x, y, m, this._srid);
    }


    public static EsriPointM Read(System.IO.BinaryReader reader, int offset, int contentLength, int srid)
    {
        //+8: pass the record header; +4 pass the shapeType
        reader.BaseStream.Position = offset * 2 + 8 + 4;

        //var byteArray = reader.ReadBytes(contentLength * 2 - 8);

        double x = reader.ReadDouble();

        double y = reader.ReadDouble();

        double m = reader.ReadDouble();

        return new EsriPointM(x, y, m, srid);
    }

    public static EsriPointM ParseGdbRecord(byte[] bytes, int srid)
    {
        // 4: shape type
        var offset = 4;

        double x = BitConverter.ToDouble(bytes, offset);

        double y = BitConverter.ToDouble(bytes, offset + ShapeConstants.DoubleSize);

        double m = BitConverter.ToDouble(bytes, offset + 2 * ShapeConstants.DoubleSize);

        return new EsriPointM(x, y, m, srid);
    }
}
