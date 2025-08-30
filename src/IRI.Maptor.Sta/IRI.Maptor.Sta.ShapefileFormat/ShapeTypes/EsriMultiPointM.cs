// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Linq;

using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Sta.Spatial.Primitives.Esri;

namespace IRI.Maptor.Sta.ShapefileFormat.EsriType;

public struct EsriMultiPointM : IEsriPointsWithMeasure
{

    /// <summary>
    /// MinX, MinY, MaxX, MaxY
    /// </summary>
    private BoundingBox boundingBox;

    public int Srid { get; set; }

    private EsriPoint[] points;

    public int NumberOfPoints
    {
        get { return this.points.Length; }
    }

    public int NumberOfParts
    {
        get { return this.Parts.Length; }
    }

    public EsriPoint[] Points
    {
        get { return this.points; }
    }

    public int[] Parts
    {
        get { return new int[] { 0 }; }
    }

    private double minMeasure, maxMeasure;

    private double[] measures;

    public double MinMeasure
    {
        get { return this.minMeasure; }
    }

    public double MaxMeasure
    {
        get { return this.maxMeasure; }
    }

    public double[] Measures
    {
        get { return this.measures; }
    }

    public EsriMultiPointM(EsriPoint[] points, double[] measures)
    {
        if (points == null || points.Length != measures.Length)
        {
            throw new NotImplementedException();
        }

        if (points.Length == 0)
        {
            this.Srid = 0;
        }
        else
        {
            this.Srid = points.First().Srid;
        }

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
        if (points == null || points.Length != measures.Length)
        {
            throw new NotImplementedException();
        }

        if (points.Length == 0)
        {
            this.Srid = 0;
        }
        else
        {
            this.Srid = points.First().Srid;
        }

        this.boundingBox = boundingBox;
         
        this.points = points;

        this.minMeasure = minMeasure;

        this.maxMeasure = maxMeasure;

        this.measures = measures;
    }

    public EsriMultiPointM(EsriPointM[] points)
    {
        if (points == null || points.Length < 1)
        {
            throw new NotImplementedException();
        }

        this.boundingBox = BoundingBox.CalculateBoundingBox(points/*.Cast<IPoint>()*/);

        this.Srid = points.First().Srid;

        this.points = new EsriPoint[points.Length];

        this.measures = new double[points.Length];

        this.minMeasure = points[0].Measure;

        this.maxMeasure = points[0].Measure;

        for (int i = 0; i < points.Length; i++)
        {
            this.points[i] = new EsriPoint(points[i].X, points[i].Y, this.Srid);

            this.measures[i] = points[i].Measure;

            if (this.minMeasure > points[i].Measure)
            {
                this.minMeasure = points[i].Measure;
            }

            if (this.maxMeasure < points[i].Measure)
            {
                this.maxMeasure = points[i].Measure;
            }
        }
    }

    #region IShape Members

    public byte[] WriteContentsToByte()
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

    public int ContentLength
    {
        get { return 20 + 8 * NumberOfPoints + 8 + 4 * NumberOfPoints; }
    }

    public EsriShapeType Type
    {
        get { return EsriShapeType.EsriMultiPointM; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="partNo">this parameter will be ignored</param>
    /// <returns></returns>
    public EsriPoint[] GetPart(int partNo)
    {
        return this.Points;
    }


    public BoundingBox MinimumBoundingBox
    {
        get { return boundingBox; }
    }

    public string AsSqlServerWkt()
    { 
        return string.Format("MULTIPOINT{0}", SqlServerWktHelper.PointMGroupElementToWkt(this.Points, this.Measures));
    }

    public byte[] AsWkb() 
    { 
        return OgcWkbMapFunctions.ToWkbMultiPointM(this.points, this.measures);
    }

    /// <summary>
    /// Returs Kml representation of the point. Note: Point must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    public IRI.Maptor.Ket.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectFunc = null, byte[] color = null)
    {
        throw new NotImplementedException();
    }

    public string AsKml(Func<Point, Point> projectToGeodeticFunc = null)
    {
        return OgcKmlMapFunctions.AsKml(this.AsPlacemark(projectToGeodeticFunc));
    }

    public IEsriShape Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        return new EsriMultiPointM(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray(), this.measures);
    }

    public Geometry<Point> AsGeometry()
    {
        return new Geometry<Point>(points.Select(p => new Point(p.X, p.Y)).ToList(), GeometryType.MultiPoint, Srid);
    }

    public bool IsNullOrEmpty()
    {
        return Points == null || Points.Length < 1;
    }

    public bool IsRingBase() => false;

    #endregion
}
