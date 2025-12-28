using System;
namespace IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;

public abstract class EsriPointZCollection : EsriPointMCollection
{
    protected double minZ, maxZ;
    protected double[] zValues;

    public double MinZ => this.minZ;

    public double MaxZ => this.maxZ;

    public double[] ZValues => this.zValues;
}
