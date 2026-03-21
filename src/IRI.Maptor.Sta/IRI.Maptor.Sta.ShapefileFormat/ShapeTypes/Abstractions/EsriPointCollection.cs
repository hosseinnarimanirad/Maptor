using System;
using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.ShapefileFormat.EsriType;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;

public abstract class EsriPointCollection : EsriShapeBase
{
    protected BoundingBox boundingBox;
    public override BoundingBox MinimumBoundingBox => boundingBox;

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

    public virtual EsriPoint[] GetPart(int partNo)
    {
        if (Parts is null)
            throw new InvalidOperationException("Parts array is null.");
        if (partNo < 0)
            throw new ArgumentOutOfRangeException(nameof(partNo), "Part number cannot be negative.");
        if (partNo >= NumberOfParts)
            throw new ArgumentOutOfRangeException(nameof(partNo), $"Part number {partNo} is out of range. Number of parts: {NumberOfParts}.");
        return ShapeHelper.GetEsriPoints(this, Parts[partNo]);
    }

    public override bool IsNullOrEmpty() => NumberOfPoints <= 0;

    /// <summary>
    /// Converts EsriPoint array to List of Point
    /// </summary>
    protected static List<Point> ConvertEsriPointsToPoints(EsriPoint[] esriPoints)
    {
        if (esriPoints is null)
            return new List<Point>();
        return esriPoints.Select(p => new Point(p.X, p.Y)).ToList();
    }

    /// <summary>
    /// Creates a MultiPoint geometry from the points array
    /// </summary>
    protected Geometry<Point> CreateMultiPointGeometry()
    {
        if (this.NumberOfPoints == 0)
            return Geometry<Point>.CreateEmpty(GeometryType.MultiPoint, this.Srid);

        return Geometry<Point>.Create(ConvertEsriPointsToPoints(this.points), GeometryType.MultiPoint, this.Srid);
    }

    /// <summary>
    /// Creates a LineString or MultiLineString geometry from parts
    /// </summary>
    protected Geometry<Point> CreateLineStringOrMultiLineStringGeometry()
    {
        if (this.NumberOfParts == 0)
            return Geometry<Point>.CreateEmpty(GeometryType.LineString, this.Srid);

        if (this.NumberOfParts == 1)
        {
            return Geometry<Point>.Create(ShapeHelper.GetPoints(this, this.Parts[0]), GeometryType.LineString, this.Srid);
        }

        var parts = new List<Geometry<Point>>(this.NumberOfParts);
        for (int i = 0; i < this.NumberOfParts; i++)
        {
            parts.Add(Geometry<Point>.Create(ShapeHelper.GetPoints(this, this.Parts[i]), GeometryType.LineString, this.Srid));
        }

        return Geometry<Point>.Create(parts, GeometryType.MultiLineString, this.Srid);
    }

    /// <summary>
    /// Creates a Polygon or MultiPolygon geometry from parts
    /// </summary>
    protected Geometry<Point> CreatePolygonOrMultiPolygonGeometry()
    {
        if (this.NumberOfParts == 0 || this.NumberOfPoints == 0)
            return Geometry<Point>.CreateEmpty(GeometryType.Polygon, this.Srid);

        var parts = new List<Geometry<Point>>(this.NumberOfParts);

        for (int i = 0; i < this.NumberOfParts; i++)
        {
            //if (NumberOfParts > 1000)
            //{
            //    var geo = Geometry<Point>.Create(ShapeHelper.GetPoints(this, this.Parts[i]), GeometryType.LineString, this.Srid);
            //    geo.AsGeoJsonFeatureSet().Save("e:\\polygonWith1kRing.json", false);
            //}
            parts.Add(Geometry<Point>.Create(ShapeHelper.GetPoints(this, this.Parts[i]), GeometryType.LineString, this.Srid));
        }

        return Geometry<Point>.CreatePolygonOrMultiPolygon(parts, this.Srid);
    }
}
