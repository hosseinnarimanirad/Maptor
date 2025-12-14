// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;

using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;


namespace IRI.Maptor.Sta.ShapefileFormat.EsriType;

public class EsriPoint : EsriShapeBase, IPoint
{
    private double x, y;

    public double X
    {
        get { return this.x; }
        set { this.x = value; }
    }

    public double Y
    {
        get { return this.y; }
        set { this.y = value; }
    }

    public int Srid { get; set; }

    //public PointType Type => PointType.Normal;
     
    //public bool HasM() => false;

    //public bool HasZ() => false;

    public EsriPoint() { }

    public EsriPoint(double x, double y, int srid)
    {
        this.Srid = srid;

        this.x = x;

        this.y = y;
    }


    public override BoundingBox MinimumBoundingBox => new BoundingBox(this.X, this.Y, this.X, this.Y);

    //public byte[] WriteContentsToByte()
    //{
    //    System.IO.MemoryStream result = new System.IO.MemoryStream();

    //    result.Write(System.BitConverter.GetBytes((int)ShapeType.Point), 0, ShapeConstants.IntegerSize);

    //    result.Write(System.BitConverter.GetBytes(this.X), 0, ShapeConstants.DoubleSize);

    //    result.Write(System.BitConverter.GetBytes(this.Y), 0, ShapeConstants.DoubleSize);

    //    return result.ToArray();
    //}
    public override byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriPoint), 0, ShapeConstants.IntegerSize);

        result.Write(Writer.ShpWriter.CheckNoDataAndGetByteValue(this.X), 0, ShapeConstants.DoubleSize);

        result.Write(Writer.ShpWriter.CheckNoDataAndGetByteValue(this.Y), 0, ShapeConstants.DoubleSize);

        return result.ToArray();
    }

    /// <summary>
    /// content length in 16bit words
    /// </summary>
    public override int ContentLength => ShapeConstants.PointContentLengthInWords;

    public override EsriShapeType EsriType => EsriShapeType.EsriPoint;

    //public static explicit operator EsriPoint(Point value)
    //{
    //    return new EsriPoint(value.X, value.Y);
    //}

    public string AsExactString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17} {1:G17}", this.X, this.Y);
    }

    public bool AreExactlyTheSame(object obj)
    {
        if (obj.GetType() != typeof(EsriPoint))
        {
            return false;
        }

        return this.AsExactString() == ((EsriPoint)obj).AsExactString();
    }

    public double DistanceTo(IPoint point)
    {
        //return Point.GetDistance(new Point(this.X, this.Y), new Point(point.X, point.Y));
        return IRI.Maptor.Sta.Spatial.Analysis.SpatialUtility.GetEuclideanLength(this, point);
    }

    public override string ToString()
    {
        return $"X: {X}, Y:{Y}";
    }

    public override bool Equals(object obj)
    {
        if (obj.GetType() != typeof(EsriPoint))
        {
            return false;
        }

        return ((EsriPoint)obj).AsExactString() == this.AsExactString();
    }

    public override int GetHashCode()
    {
        return this.AsExactString().GetHashCode();
    }


    //public string AsSqlServerWkt()
    //{
    //    return string.Format(System.Globalization.CultureInfo.InvariantCulture, "POINT({0:G17} {1:G17})", this.X, this.Y);
    //}

    //public byte[] AsWkb()
    //{
    //    return OgcWkbMapFunctions.ToWkbPoint(this);
    //}

    /// <summary>
    /// Returs Kml representation of the point. Note: Point must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    public override IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType placemark = new IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType();

        IRI.Maptor.Sta.KmlFormat.Primitives.PointType point = new IRI.Maptor.Sta.KmlFormat.Primitives.PointType();

        Point coordinates = new Point(this.x, this.Y);

        if (projectToGeodeticFunc != null)
        {
            coordinates = projectToGeodeticFunc(new Point(this.X, this.Y));
        }

        point.Coordinates.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17},{1:G17}", coordinates.X, coordinates.Y));

        placemark.AbstractGeometryGroup = point;

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
    //    //IRI.Maptor.Sta.KmlFormat.Primitives.KmlType result = new IRI.Maptor.Sta.KmlFormat.Primitives.KmlType();

    //    //IRI.Maptor.Sta.KmlFormat.Primitives.DocumentType document = new IRI.Maptor.Sta.KmlFormat.Primitives.DocumentType();

    //    //document.AbstractFeature = new IRI.Maptor.Sta.KmlFormat.Primitives.AbstractFeatureType[] { this.AsPlacemark(projectToGeodeticFunc) };

    //    //result.KmlObjectExtensionGroup = new IRI.Maptor.Sta.KmlFormat.Primitives.AbstractObjectType[] { document };

    //    //return IRI.Maptor.Ket.IO.XmlStream.Parse(result);
    //    return OgcKmlMapFunctions.AsKml(this.AsPlacemark(projectToGeodeticFunc));
    //}

    public override EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid) /*where TPoint : IPoint, new()*/
    {
        var result = transform(this);

        return new EsriPoint(result.X, result.Y, newSrid);
    }

    public override Geometry<Point> AsGeometry()
    {
        return new Geometry<Point>(new List<Point> { new Point(X, Y) }, GeometryType.Point, Srid);
    }

    public override bool IsNullOrEmpty()
    {
        return false;
    }

    public override bool IsRingBase() => false;

    public  bool IsNaN()
    {
        return double.IsNaN(X) || double.IsNaN(Y);
    }

    public byte[] AsByteArray()
    {
        // Option #3
        Span<byte> buffer = stackalloc byte[16];  // Stack-allocated, no heap allocation

        BitConverter.TryWriteBytes(buffer.Slice(0, 8), X);

        BitConverter.TryWriteBytes(buffer.Slice(8, 8), Y);

        return buffer.ToArray();  // Only allocates when creating final array
    }
}
