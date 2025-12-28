// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Linq;

using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Sta.Spatial.Primitives.Esri;


namespace IRI.Maptor.Sta.ShapefileFormat.EsriType;

public class EsriMultiPointZ : EsriPointZCollection
{ 
    public override EsriShapeType EsriType => EsriShapeType.EsriMultiPointZM;
     
    public override int ContentLength => 20 + 8 * NumberOfPoints + 2 * (8 + 4 * NumberOfPoints);

    public EsriMultiPointZ() : this(Array.Empty<EsriPoint>(), Array.Empty<double>(), Array.Empty<double>()) { }

    public EsriMultiPointZ(EsriPoint[] points, double[] zValues, double[] measures)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));
        if (zValues is null)
            throw new ArgumentNullException(nameof(zValues));
        if (measures is null)
            throw new ArgumentNullException(nameof(measures));
        if (points.Length != zValues.Length)
            throw new ArgumentException("Points array length must match zValues array length.", nameof(zValues));
        if (points.Length != measures.Length)
            throw new ArgumentException("Points array length must match measures array length.", nameof(measures));

        this.Srid = points.Length == 0 ? 0 : points.First().Srid;

        this.boundingBox = BoundingBox.CalculateBoundingBox(points/*.Cast<IPoint>()*/);

        this.points = points;

        this.measures = measures;

        this.zValues = zValues;

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

        if (zValues?.Count() > 0)
        {
            this.minZ = zValues.Min();

            this.maxZ = zValues.Max();
        }
        else
        {
            this.minZ = EsriConstants.NoDataValue;

            this.maxZ = EsriConstants.NoDataValue;
        }
    }

    internal EsriMultiPointZ(BoundingBox boundingBox,
                            EsriPoint[] points,
                            double minZ,
                            double maxZ,
                            double[] zValues,
                            double minMeasure,
                            double maxMeasure,
                            double[] measures)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));
        if (zValues is null)
            throw new ArgumentNullException(nameof(zValues));
        if (measures is null)
            throw new ArgumentNullException(nameof(measures));
        if (points.Length != zValues.Length)
            throw new ArgumentException("Points array length must match zValues array length.", nameof(zValues));
        if (points.Length != measures.Length)
            throw new ArgumentException("Points array length must match measures array length.", nameof(measures));

        this.Srid = points.Length == 0 ? 0 : points.First().Srid;

        this.boundingBox = boundingBox;

        this.points = points;

        this.minZ = minZ;

        this.maxZ = maxZ;

        this.zValues = zValues;

        this.minMeasure = minMeasure;

        this.maxMeasure = maxMeasure;

        this.measures = measures;
    }

    public EsriMultiPointZ(EsriPointZ[] points)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));
        if (points.Length < 1)
            throw new ArgumentException("Points array must contain at least one point.", nameof(points));

        this.boundingBox = BoundingBox.CalculateBoundingBox(points/*.Cast<IPoint>()*/);

        this.Srid = points.First().Srid;

        this.points = new EsriPoint[points.Length];

        this.measures = new double[points.Length];

        this.zValues = new double[points.Length];

        this.minMeasure = points[0].Measure;

        this.maxMeasure = points[0].Measure;

        this.minZ = points[0].Z;

        this.maxZ = points[0].Z;

        for (int i = 0; i < points.Length; i++)
        {
            this.points[i] = new EsriPoint(points[i].X, points[i].Y, points[i].Srid);

            this.measures[i] = points[i].Measure;

            this.zValues[i] = points[i].Z;

            if (this.minMeasure > points[i].Measure)
            {
                this.minMeasure = points[i].Measure;
            }

            if (this.maxMeasure < points[i].Measure)
            {
                this.maxMeasure = points[i].Measure;
            }

            if (this.minZ > points[i].Z)
            {
                this.minZ = points[i].Z;
            }

            if (this.maxZ < points[i].Z)
            {
                this.maxZ = points[i].Z;
            }
        }
    }

    public override bool IsRingBase() => false;

    //public override bool IsNullOrEmpty() => Points == null || Points.Length < 1;

    public override byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriMultiPointZM), 0, ShapeConstants.IntegerSize);

        result.Write(Writer.ShpWriter.WriteBoundingBoxToByte(this), 0, 4 * ShapeConstants.DoubleSize);

        result.Write(System.BitConverter.GetBytes(this.NumberOfPoints), 0, ShapeConstants.IntegerSize);

        byte[] tempPoints = Writer.ShpWriter.WritePointsToByte(this.points);

        result.Write(tempPoints, 0, tempPoints.Length);

        byte[] tempZ = Writer.ShpWriter.WriteAdditionalData(this.MinZ, this.MaxZ, this.ZValues);

        result.Write(tempZ, 0, tempZ.Length);

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
    /// Returns Kml representation of the multipoint. Note: Z,M values are ignored. Points must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    public override IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        var points = this.Points.Select(p => new Point(p.X, p.Y));
        return KmlPlacemarkHelper.CreateMultiPointPlacemark(points, projectToGeodeticFunc, color);
    }
     
    public override EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        return new EsriMultiPointZ(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray(), this.ZValues, this.Measures);
    }

    public override Geometry<Point> AsGeometry() => CreateMultiPointGeometry();
}
