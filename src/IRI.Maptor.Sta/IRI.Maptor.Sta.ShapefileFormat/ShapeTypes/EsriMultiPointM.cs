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

public class EsriMultiPointM : EsriPointMCollection
{
    //public int Srid { get; set; }

    public override EsriShapeType EsriType => EsriShapeType.EsriMultiPointM;


    //private BoundingBox boundingBox;
    //public BoundingBox MinimumBoundingBox => boundingBox;


    //private EsriPoint[] points;
    //public EsriPoint[] Points => this.points;

    //public int NumberOfPoints => this.points?.Length ?? 0;


    //public int[] Parts => [0];

    //public int NumberOfParts => this.Parts.Length;


    //private double minMeasure, maxMeasure;
    //private double[] measures;

    //public double MinMeasure => this.minMeasure;
    //public double MaxMeasure => this.maxMeasure;
    //public double[] Measures => this.measures;


    public override int ContentLength => 20 + 8 * NumberOfPoints + 8 + 4 * NumberOfPoints;

    public EsriMultiPointM() : this(Array.Empty<EsriPoint>(), Array.Empty<double>()) { }

    public EsriMultiPointM(EsriPoint[] points, double[] measures)
    {
        if (points is null || points.Length != measures.Length)
            throw new NotImplementedException();

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
        if (points is null || points.Length != measures.Length)
            throw new NotImplementedException();

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
        if (points is null || points.Length < 1)
            throw new NotImplementedException();

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

    public override bool IsNullOrEmpty() => Points == null || Points.Length < 1;

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

    //public string AsSqlServerWkt()
    //{
    //    return string.Format("MULTIPOINT{0}", SqlServerWktHelper.PointMGroupElementToWkt(this.Points, this.Measures));
    //}

    //public byte[] AsWkb()
    //{
    //    return OgcWkbMapFunctions.ToWkbMultiPointM(this.points, this.measures);
    //}

    /// <summary>
    /// Returns Kml representation of the multipoint. Note: M values are ignored. Points must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    public override IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectFunc = null, byte[] color = null)
    {
        IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType placemark = new IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType();

        if (this.NumberOfPoints == 0)
        {
            return placemark;
        }

        IRI.Maptor.Sta.KmlFormat.Primitives.MultiGeometryType multiGeometry = new IRI.Maptor.Sta.KmlFormat.Primitives.MultiGeometryType();

        foreach (var point in this.Points)
        {
            IRI.Maptor.Sta.KmlFormat.Primitives.PointType kmlPoint = new IRI.Maptor.Sta.KmlFormat.Primitives.PointType();
            
            Point coordinates = new Point(point.X, point.Y);
            
            if (projectFunc != null)
            {
                coordinates = projectFunc(coordinates);
            }
            
            kmlPoint.Coordinates.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17},{1:G17}", coordinates.X, coordinates.Y));
            
            multiGeometry.AbstractGeometryGroup.Add(kmlPoint);
        }

        placemark.AbstractGeometryGroup = multiGeometry;

        if (color != null)
        {
            IRI.Maptor.Sta.KmlFormat.Primitives.StyleType style = new IRI.Maptor.Sta.KmlFormat.Primitives.StyleType();
            IRI.Maptor.Sta.KmlFormat.Primitives.IconStyleType iconStyle = new IRI.Maptor.Sta.KmlFormat.Primitives.IconStyleType();
            iconStyle.Color = color;
            style.IconStyle = iconStyle;
            placemark.AbstractStyleSelectorGroup.Add(style);
        }

        return placemark;
    }

    //public string AsKml(Func<Point, Point> projectToGeodeticFunc = null)
    //{
    //    return OgcKmlMapFunctions.AsKml(this.AsPlacemark(projectToGeodeticFunc));
    //}

    public override EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        return new EsriMultiPointM(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray(), this.measures);
    }

    public override Geometry<Point> AsGeometry()
    {
        return new Geometry<Point>(points.Select(p => new Point(p.X, p.Y)).ToList(), GeometryType.MultiPoint, Srid);
    }
}
