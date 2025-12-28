using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using System;

namespace IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;

public abstract class EsriPointMCollection : EsriPointCollection
{
    protected double minMeasure, maxMeasure;
    protected double[] measures;

    public double MinMeasure => this.minMeasure;
    public double MaxMeasure => this.maxMeasure;
    public double[] Measures => this.measures;
}
