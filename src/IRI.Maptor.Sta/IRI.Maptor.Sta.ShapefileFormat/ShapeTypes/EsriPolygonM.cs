// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.Common.Abstractions;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Sta.Spatial.IO.EsriJson;


namespace IRI.Maptor.Sta.ShapefileFormat.EsriType;

public class EsriPolygonM : EsriPointMCollection
{ 
    public override EsriShapeType EsriType => EsriShapeType.EsriPolygonM;
     
    public override int ContentLength => 22 + 2 * NumberOfParts + 8 * NumberOfPoints + 8 + 4 * NumberOfPoints;

    public EsriPolygonM() : this(Array.Empty<EsriPoint>(), Array.Empty<int>(), Array.Empty<double>()) { }

    public EsriPolygonM(EsriPoint[] points, int[] parts, double[] measures)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));
        if (parts is null)
            throw new ArgumentNullException(nameof(parts));
        if (measures is null)
            throw new ArgumentNullException(nameof(measures));
        if (points.Length != measures.Length)
            throw new ArgumentException("Points array length must match measures array length.", nameof(measures));

        this.Srid = points.Length == 0 ? 0 : points.First().Srid;

        this.boundingBox = BoundingBox.CalculateBoundingBox(points);

        this.parts = parts;

        this.points = points;

        this.measures = measures;

        if (measures?.Count() > 0)
        {
            this.minMeasure = measures.Min();

            this.maxMeasure = measures.Max();
        }
        else
        {
            this.minMeasure = EsriConstants.NoDataValue;

            this.maxMeasure = EsriConstants.NoDataValue;
        }
    }

    internal EsriPolygonM(BoundingBox boundingBox, int[] parts, EsriPoint[] points, double minMeasure, double maxMeasure, double[] measures)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));
        if (parts is null)
            throw new ArgumentNullException(nameof(parts));
        if (measures is null)
            throw new ArgumentNullException(nameof(measures));
        if (points.Length != measures.Length)
            throw new ArgumentException("Points array length must match measures array length.", nameof(measures));

        this.Srid = points.Length == 0 ? 0 : points.First().Srid;

        this.boundingBox = boundingBox;

        this.parts = parts;

        this.points = points;

        this.minMeasure = minMeasure;

        this.maxMeasure = maxMeasure;

        this.measures = measures;
    }

    public override bool IsRingBase() => true;
     
    public override byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriPolygonM), 0, ShapeConstants.IntegerSize);

        result.Write(Writer.ShpWriter.WriteBoundingBoxToByte(this), 0, 4 * ShapeConstants.DoubleSize);

        result.Write(System.BitConverter.GetBytes(this.NumberOfParts), 0, ShapeConstants.IntegerSize);

        result.Write(System.BitConverter.GetBytes(this.NumberOfPoints), 0, ShapeConstants.IntegerSize);

        foreach (int item in this.parts)
        {
            result.Write(System.BitConverter.GetBytes(item), 0, ShapeConstants.IntegerSize);
        }

        byte[] tempPoints = Writer.ShpWriter.WritePointsToByte(this.points);

        result.Write(tempPoints, 0, tempPoints.Length);

        byte[] tempMeasures = Writer.ShpWriter.WriteAdditionalData(this.MinMeasure, this.MaxMeasure, this.Measures);

        result.Write(tempMeasures, 0, tempMeasures.Length);

        return result.ToArray();
    }
     

    /// <summary>
    /// Returns Kml representation of the polygon. Note: M values are ignored. Polygon must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    public override IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        if (this.NumberOfParts == 0 || this.NumberOfPoints == 0)
        {
            return new IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType();
        }

        var outerRing = ShapeHelper.GetPoints(this, this.Parts[0]);
        var innerRings = this.NumberOfParts > 1 
            ? Enumerable.Range(1, this.NumberOfParts - 1).Select(i => ShapeHelper.GetPoints(this, this.Parts[i]))
            : null;

        return KmlPlacemarkHelper.CreatePolygonPlacemark(outerRing, innerRings, projectToGeodeticFunc, color);
    }
     
    public override EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        return new EsriPolygonM(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray(), this.Parts, this.Measures);
    }

    public override Geometry<Point> AsGeometry() => CreatePolygonOrMultiPolygonGeometry();
}
