using System;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.ShapefileFormat.EsriType;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;

namespace IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;

public abstract class IEsriSimplePoints : IEsriShape
{
    protected BoundingBox boundingBox;
    public override BoundingBox MinimumBoundingBox => boundingBox;

    //EsriPoint[] Points { get; }

    //int[] Parts { get; }

    //int NumberOfPoints { get; }

    //int NumberOfParts { get; }


    protected EsriPoint[] points;
    /// <summary>
    /// Points for All Parts
    /// </summary>
    public EsriPoint[] Points => this.points;

    public int NumberOfPoints => this.Points?.Length ?? 0;


    protected int[] parts;
    /// <summary>
    /// Index to First Point in Part
    /// </summary>
    public virtual int[] Parts => this.parts;

    public int NumberOfParts => this.Parts?.Length ?? 0;
 

    //EsriPoint[] GetPart(int partNo);
    public virtual EsriPoint[] GetPart(int partNo) => ShapeHelper.GetEsriPoints(this, Parts[partNo]);
     
    public override bool IsNullOrEmpty() => NumberOfPoints <= 0;


    //public override byte[] AsWkb()
    //{
    //    return OgcWkbMapFunctions.ToWkbMultiPoint(this.points);
    //}
}
