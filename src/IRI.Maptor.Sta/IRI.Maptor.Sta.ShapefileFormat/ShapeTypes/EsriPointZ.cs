// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;

using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Sta.Spatial.Primitives.Esri;

namespace IRI.Maptor.Sta.ShapefileFormat.EsriType;

public struct EsriPointZ : IPoint, IEsriShape, IHasZ
{
    private double x, y, z, measure;

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

    public double Z
    {
        get { return this.z; }
    }

    public double Measure
    {
        get { return this.measure; }
    }

    //public PointType Type => PointType.PointM | PointType.PointZ;

    //public bool HasM() => true;

    //public bool HasZ() => true;

    public int Srid { get; set; }

    public EsriPointZ(double x, double y, double z, int srid)
        : this(x, y, z, EsriConstants.NoDataValue, srid) { }

    public EsriPointZ(double x, double y, double z, double measure, int srid)
    {
        this.Srid = srid;

        this.x = x;

        this.y = y;

        this.z = z;

        this.measure = measure;
    }


    public string AsExactString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17} {1:G17} {2:G17} {3:G17}", this.X, this.Y, this.Z, this.Measure);
    }

    public bool AreExactlyTheSame(object obj)
    {
        if (obj.GetType() != typeof(EsriPointZ))
        {
            return false;
        }

        return this.AsExactString() == ((EsriPointZ)obj).AsExactString();
    }

    public double DistanceTo(IPoint point)
    {
        if (point is EsriPointZ)
        {
            return PointZM.GetDistance(new PointZM(this.X, this.Y, this.Z), new PointZM(point.X, point.Y, ((EsriPointZ)point).Z));
        }
        else
        {
            return new PointZM(this.X, this.Y, this.Z).DistanceTo(point);
        }

    }

    #region IShape Members


    public BoundingBox MinimumBoundingBox
    {
        get { return new BoundingBox(this.X, this.Y, this.X, this.Y); }
    }

    //public byte[] WriteContentsToByte()
    //{
    //    System.IO.MemoryStream result = new System.IO.MemoryStream();

    //    result.Write(System.BitConverter.GetBytes((int)ShapeType.PointZ), 0, ShapeConstants.IntegerSize);

    //    result.Write(System.BitConverter.GetBytes(this.X), 0, ShapeConstants.DoubleSize);

    //    result.Write(System.BitConverter.GetBytes(this.Y), 0, ShapeConstants.DoubleSize);

    //    result.Write(System.BitConverter.GetBytes(this.Z), 0, ShapeConstants.DoubleSize);

    //    result.Write(System.BitConverter.GetBytes(this.Measure), 0, ShapeConstants.DoubleSize);

    //    return result.ToArray();
    //}

    public byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriPointZM), 0, ShapeConstants.IntegerSize);

        result.Write(Writer.ShpWriter.CheckNoDataAndGetByteValue(this.X), 0, ShapeConstants.DoubleSize);

        result.Write(Writer.ShpWriter.CheckNoDataAndGetByteValue(this.Y), 0, ShapeConstants.DoubleSize);

        result.Write(Writer.ShpWriter.CheckNoDataAndGetByteValue(this.Z), 0, ShapeConstants.DoubleSize);

        result.Write(Writer.ShpWriter.CheckNoDataAndGetByteValue(this.Measure), 0, ShapeConstants.DoubleSize);

        return result.ToArray();
    }

    public int ContentLength
    {
        get { return ShapeConstants.PointZContentLengthInWords; }
    }

    public EsriShapeType EsriType
    {
        get { return EsriShapeType.EsriPointZM; }
    }

    public string AsSqlServerWkt()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "POINT({0:G17} {1:G17} {2:G17} {3})", this.X, this.Y, this.Z, this.Measure == EsriConstants.NoDataValue ? "NULL" : this.Measure.ToString("G17"));
    }

    public byte[] AsWkb()
    {
        return OgcWkbMapFunctions.ToWkbPointZM(this, this.Z, this.Measure);
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
        var result = transform(this);

        return new EsriPointZ(result.X, result.Y, this.Z, this.Measure, newSrid);
    }

    public Geometry<Point> AsGeometry()
    {
        return new Geometry<Point>(new List<Point>() { new Point(X, Y) }, GeometryType.Point, Srid);
    }

    public bool IsNullOrEmpty()
    {
        return false;
    }

    public bool IsRingBase() => false;

    #endregion

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
