// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Linq;

using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.Common.Abstractions;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Sta.Spatial.IO.EsriJson;


namespace IRI.Maptor.Sta.ShapefileFormat.EsriType;

public class EsriMultiPointM : EsriPointMCollection
{ 
    public override EsriShapeType EsriType => EsriShapeType.EsriMultiPointM;
      
    public override int ContentLength => 20 + 8 * NumberOfPoints + 8 + 4 * NumberOfPoints;

    public EsriMultiPointM() : this(Array.Empty<EsriPoint>(), Array.Empty<double>()) { }

    public EsriMultiPointM(EsriPoint[] points, double[] measures)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));
        if (measures is null)
            throw new ArgumentNullException(nameof(measures));
        if (points.Length != measures.Length)
            throw new ArgumentException("Points array length must match measures array length.", nameof(measures));

        this.Srid = points.Length == 0 ? 0 : points.First().Srid;

        this.boundingBox = BoundingBox.CalculateBoundingBox(points/*.Cast<IPoint>()*/);

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

    internal EsriMultiPointM(BoundingBox boundingBox, EsriPoint[] points, double minMeasure, double maxMeasure, double[] measures)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));
        if (measures is null)
            throw new ArgumentNullException(nameof(measures));
        if (points.Length != measures.Length)
            throw new ArgumentException("Points array length must match measures array length.", nameof(measures));

        this.Srid = points.Length == 0 ? 0 : points.First().Srid;

        this.boundingBox = boundingBox;

        this.points = points;

        this.minMeasure = minMeasure;

        this.maxMeasure = maxMeasure;

        this.measures = measures;
    }

    public EsriMultiPointM(EsriPointM[] points)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));
        if (points.Length < 1)
            throw new ArgumentException("Points array must contain at least one point.", nameof(points));

        this.boundingBox = BoundingBox.CalculateBoundingBox(points/*.Cast<IPoint>()*/);

        this.Srid = points.First().Srid;

        this.points = new EsriPoint[points.Length];

        this.measures = new double[points.Length];

        this.minMeasure = points[0].M;

        this.maxMeasure = points[0].M;

        for (int i = 0; i < points.Length; i++)
        {
            this.points[i] = new EsriPoint(points[i].X, points[i].Y, this.Srid);

            this.measures[i] = points[i].M;

            if (this.minMeasure > points[i].M)
            {
                this.minMeasure = points[i].M;
            }

            if (this.maxMeasure < points[i].M)
            {
                this.maxMeasure = points[i].M;
            }
        }
    }

    public override bool IsRingBase() => false;

    public override bool IsNullOrEmpty() => Points is null || Points.Length < 1;

    public override byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriMultiPointM), 0, ShapeConstants.IntegerSize);

        result.Write(Writer.ShpWriter.WriteBoundingBoxToByte(this), 0, 4 * ShapeConstants.DoubleSize);

        result.Write(System.BitConverter.GetBytes(this.NumberOfPoints), 0, ShapeConstants.IntegerSize);

        byte[] tempPoints = Writer.ShpWriter.WritePointsToByte(this.points);

        result.Write(tempPoints, 0, tempPoints.Length);

        byte[] tempMeasures = Writer.ShpWriter.WriteAdditionalData(this.MinMeasure, this.MaxMeasure, this.Measures);

        result.Write(tempMeasures, 0, tempMeasures.Length);

        return result.ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="partNo">this parameter will be ignored</param>
    /// <returns></returns>
    public override EsriPoint[] GetPart(int partNo) => this.Points;
     
    /// <summary>
    /// Returns Kml representation of the multipoint. Note: M values are ignored. Points must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    public override IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        var points = this.Points.Select(p => new Point(p.X, p.Y));
        return KmlPlacemarkHelper.CreateMultiPointPlacemark(points, projectToGeodeticFunc, color);
    }

    //public string AsKml(Func<Point, Point> projectToGeodeticFunc = null)
    //{
    //    return OgcKmlMapFunctions.AsKml(this.AsPlacemark(projectToGeodeticFunc));
    //}

    public override EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        return new EsriMultiPointM(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray(), this.measures);
    }

    public override Geometry<Point> AsGeometry() => CreateMultiPointGeometry();
}
