// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj


using IRI.Maptor.Sta.ShapefileFormat.EsriType;
using IRI.Maptor.Sta.Spatial.IO.EsriJson;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.ShapefileFormat.Reader;

public class PointZReader : ShpReader<EsriPointZ>
{
    public PointZReader(string fileName, int srid)
        : base(fileName, EsriShapeType.EsriPointZM, srid)
    {

    }

    protected override EsriPointZ ReadElement()
    {
        int shapeType = shpReader.ReadInt32();

        if ((EsriShapeType)shapeType != EsriShapeType.EsriPointZM)
        {
            throw new NotImplementedException();
        }

        double x = shpReader.ReadDouble();

        double y = shpReader.ReadDouble();

        double z = shpReader.ReadDouble();

        double m = shpReader.ReadDouble();

        return new EsriPointZ(x, y, z, m, this._srid);
    }

    public static EsriPointZ Read(System.IO.BinaryReader reader, int offset, int contentLength, int srid)
    {
        //+8: pass the record header; +4 pass the shapeType
        reader.BaseStream.Position = offset * 2 + 8 + 4;

        //var byteArray = reader.ReadBytes(contentLength * 2 - 8);

        double x = reader.ReadDouble();

        double y = reader.ReadDouble();

        double z = reader.ReadDouble();

        double m = reader.ReadDouble();

        return new EsriPointZ(x, y, z, m, srid);
    }


    public static EsriPointZ ParseGdbRecord(byte[] bytes, int srid, bool hasM)
    {
        // 4: shape type
        var offset = 4;

        double x = BitConverter.ToDouble(bytes, offset);

        double y = BitConverter.ToDouble(bytes, offset + ShapeConstants.DoubleSize);

        double z = BitConverter.ToDouble(bytes, offset + 2 * ShapeConstants.DoubleSize);

        double m = EsriConstants.NoDataValue;

        if (hasM)
        {
            m = BitConverter.ToDouble(bytes, offset + 3 * ShapeConstants.DoubleSize);
        }

        return new EsriPointZ(x, y, z, m, srid);
    }
}
