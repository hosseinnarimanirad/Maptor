// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj


using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.ShapefileFormat.Reader;

public abstract class zReader<T> : MeasuresReader<T> where T : IEsriShape
{
    public zReader(string fileName, EsriShapeType type, int srid)
        : base(fileName, type, srid)
    {

    }

    protected void ReadZValues(int numberOfPoints, out double minZ, out double maxZ, out double[] zValues)
    {
        minZ = shpReader.ReadDouble();

        maxZ = shpReader.ReadDouble();

        zValues = new double[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            zValues[i] = shpReader.ReadDouble();
        }

    }
}
