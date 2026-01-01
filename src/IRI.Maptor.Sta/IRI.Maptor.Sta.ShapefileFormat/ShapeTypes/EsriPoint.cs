// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;

using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Sta.Spatial.DigitalTerrainModeling;


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
     
    public EsriPoint() { }

    public EsriPoint(double x, double y, int srid)
    {
        this.Srid = srid;

        this.x = x;

        this.y = y;
    }


    public override BoundingBox MinimumBoundingBox => new BoundingBox(this.X, this.Y, this.X, this.Y);
     
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
     
    public string AsExactString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17} {1:G17}", this.X, this.Y);
    }

    public bool AreExactlyTheSame(object obj)
    {
        //if (obj.GetType() != typeof(EsriPoint))
        //{
        //    return false;
        //}

        //return this.AsExactString() == ((EsriPoint)obj).AsExactString();

        var point = obj as Point;

        if (point is null)
            return false;

        return this.X == point.X && this.Y == point.Y;
    }

    public bool HaveTheSameXY(object obj) => AreExactlyTheSame(obj);

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
     
    /// <summary>
    /// Returs Kml representation of the point. Note: Point must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    public override IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        return KmlPlacemarkHelper.CreatePointPlacemark(new Point(this.X, this.Y), projectToGeodeticFunc, color);
    }
     
    public override EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid) /*where TPoint : IPoint, new()*/
    {
        var result = transform(this);

        return new EsriPoint(result.X, result.Y, newSrid);
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
