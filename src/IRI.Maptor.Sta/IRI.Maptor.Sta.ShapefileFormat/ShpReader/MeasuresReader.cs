// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj


using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.ShapefileFormat.Reader;

public abstract class MeasuresReader<T> : PointsReader<T> where T : IEsriShape
{
    public MeasuresReader(string fileName, EsriShapeType type, int srid)
        : base(fileName, type, srid)
    {

    }

    protected void ReadMeasures(int numberOfPoints, out double minMeasure, out double maxMeasure, out double[] measures)
    {
        minMeasure = shpReader.ReadDouble();

        maxMeasure = shpReader.ReadDouble();

        measures = new double[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            measures[i] = shpReader.ReadDouble();
        }
    }         
}
