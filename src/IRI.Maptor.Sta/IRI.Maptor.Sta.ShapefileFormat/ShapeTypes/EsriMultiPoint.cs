// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Linq;

using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Sta.Common.Abstrations;


namespace IRI.Maptor.Sta.ShapefileFormat.EsriType;

public class EsriMultiPoint : EsriPointCollection
{
    //public int Srid { get; set; }

    public override EsriShapeType EsriType => EsriShapeType.EsriMultiPoint;


    //private BoundingBox boundingBox;
    //public BoundingBox MinimumBoundingBox => boundingBox;


    //private EsriPoint[] points;
    //public EsriPoint[] Points => this.points;
     
    //public int NumberOfPoints => this.points?.Length ?? 0;


    //public int[] Parts => [0];

    //public int NumberOfParts => this.Parts.Length;

    public override int ContentLength => 20 + 8 * NumberOfPoints;

    public EsriMultiPoint() : this(Array.Empty<EsriPoint>()) { }

    public EsriMultiPoint(EsriPoint[] points)
    {
        if (points is null)
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
    }

    internal EsriMultiPoint(BoundingBox boundingBox, EsriPoint[] points)
    {
        if (points == null || points.Length < 1)
        {
            throw new NotImplementedException();
        }

        this.boundingBox = boundingBox;

        this.points = points;

        this.Srid = points.First().Srid;
    }

    public override bool IsRingBase() => false;

    //public override bool IsNullOrEmpty() => Points == null || Points.Length < 1;

    public override byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriMultiPoint), 0, ShapeConstants.IntegerSize);

        result.Write(Writer.ShpWriter.WriteBoundingBoxToByte(this), 0, 4 * ShapeConstants.DoubleSize);

        result.Write(System.BitConverter.GetBytes(this.NumberOfPoints), 0, ShapeConstants.IntegerSize);

        byte[] tempPoints = Writer.ShpWriter.WritePointsToByte(this.points);

        result.Write(tempPoints, 0, tempPoints.Length);

        return result.ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="partNo">this parameter will be ignored</param>
    /// <returns></returns>
    //public EsriPoint[] GetPart(int partNo) => this.Points;

    //public override string AsSqlServerWkt()
    //{
    //    return string.Format(
    //        "MULTIPOINT({0})",
    //        string.Join(",", this.points.Select(i => string.Format("({0})", SqlServerWktHelper.SinglePointElementToWkt(i))).ToArray()));
    //}

    //public override byte[] AsWkb()
    //{
    //    return OgcWkbMapFunctions.ToWkbMultiPoint(this.points);
    //}

    /// <summary>
    /// Returns Kml representation of the multipoint. Note: Points must be in Lat/Long System
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

    //public override string AsKml(Func<Point, Point> projectToGeodeticFunc = null)
    //{
    //    return OgcKmlMapFunctions.AsKml(this.AsPlacemark(projectToGeodeticFunc));
    //}

    public override EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid) 
    {
        return new EsriMultiPoint(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray());
    }

    public override Geometry<Point> AsGeometry()
    {
        return new Geometry<Point>(points.Select(p => p.AsGeometry()).ToList(), GeometryType.MultiPoint, Srid);
    }
}
