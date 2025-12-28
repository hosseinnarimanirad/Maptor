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


public class EsriPointM : EsriShapeBase, IPoint, IHasM
{
    private double x, y, measure;

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

    public double M
    {
        get { return this.measure; }
    }

    //public PointType Type => PointType.PointM;

    //public bool HasM() => true;

    //public bool HasZ() => false;

    //public int Srid { get; set; }

    public EsriPointM() { }

    public EsriPointM(double x, double y, double measure, int srid)
    {
        this.Srid = srid;

        this.x = x;

        this.y = y;

        this.measure = measure;
    }


    public override BoundingBox MinimumBoundingBox => new BoundingBox(this.X, this.Y, this.X, this.Y);

    //public byte[] WriteContentsToByte()
    //{
    //    System.IO.MemoryStream result = new System.IO.MemoryStream();

    //    result.Write(System.BitConverter.GetBytes((int)ShapeType.PointM), 0, ShapeConstants.IntegerSize);

    //    result.Write(System.BitConverter.GetBytes(this.X), 0, ShapeConstants.DoubleSize);

    //    result.Write(System.BitConverter.GetBytes(this.Y), 0, ShapeConstants.DoubleSize);

    //    result.Write(System.BitConverter.GetBytes(this.Measure), 0, ShapeConstants.DoubleSize);

    //    return result.ToArray();
    //}

    public override byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriPointM), 0, ShapeConstants.IntegerSize);

        result.Write(Writer.ShpWriter.CheckNoDataAndGetByteValue(this.X), 0, ShapeConstants.DoubleSize);

        result.Write(Writer.ShpWriter.CheckNoDataAndGetByteValue(this.Y), 0, ShapeConstants.DoubleSize);

        result.Write(Writer.ShpWriter.CheckNoDataAndGetByteValue(this.M), 0, ShapeConstants.DoubleSize);

        return result.ToArray();
    }

    public override int ContentLength => ShapeConstants.PointMContentLengthInWords;

    public override EsriShapeType EsriType => EsriShapeType.EsriPointM;

    public bool AreExactlyTheSame(object obj)
    {
        if (obj.GetType() != typeof(EsriPointM))
        {
            return false;
        }

        return this.AsExactString() == ((EsriPointM)obj).AsExactString();
    }

    //public double DistanceTo(IPoint point)
    //{
    //    //return Point.GetDistance(new Point(this.X, this.Y), new Point(point.X, point.Y));

    //    return IRI.Maptor.Sta.Spatial.Analysis.SpatialUtility.GetEuclideanLength(this, point);
    //}


    //public string AsSqlServerWkt()
    //{
    //    return string.Format(System.Globalization.CultureInfo.InvariantCulture, "POINT({0:G17} {1:G17} NULL {2:G17})", this.X, this.Y, this.M);
    //}

    //public byte[] AsWkb()
    //{
    //    return OgcWkbMapFunctions.ToWkbPointM(this, this.M);
    //}

    /// <summary>
    /// Returs Kml representation of the point. Note: M values are ignored. Point must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    public override IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectFunc = null, byte[] color = null)
    {
        IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType placemark = new IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType();

        IRI.Maptor.Sta.KmlFormat.Primitives.PointType point = new IRI.Maptor.Sta.KmlFormat.Primitives.PointType();

        Point coordinates = new Point(this.X, this.Y);

        if (projectFunc != null)
        {
            coordinates = projectFunc(coordinates);
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

    //public override string AsKml(Func<Point, Point> projectToGeodeticFunc = null)
    //{
    //    return OgcKmlMapFunctions.AsKml(this.AsPlacemark(projectToGeodeticFunc));
    //}

    public string AsExactString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17} {1:G17} {2:G17}", this.X, this.Y, this.M);
    }

    public override EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        var result = transform(this);

        return new EsriPointM(result.X, result.Y, this.M, newSrid);
    }

    public override Geometry<Point> AsGeometry()
    {
        return Geometry<Point>.Create(X, Y, Srid);
    }

    public override bool IsNullOrEmpty()
    {
        return false;
    }

    public override bool IsRingBase() => false;

    public bool IsNaN()
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
