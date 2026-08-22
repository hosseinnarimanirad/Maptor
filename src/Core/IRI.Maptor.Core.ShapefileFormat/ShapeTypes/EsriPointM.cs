// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.ShapefileFormat.ShapeTypes.Abstractions;


namespace IRI.Maptor.Core.ShapefileFormat.EsriType;


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
        set { this.measure = value; }
    }

    public EsriPointM() { }

    public EsriPointM(double x, double y, double measure, int srid)
    {
        this.Srid = srid;

        this.x = x;

        this.y = y;

        this.measure = measure;
    }


    public override BoundingBox MinimumBoundingBox => new BoundingBox(this.X, this.Y, this.X, this.Y);

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

    public bool HaveTheSameXY(object obj)
    {
        var point = obj as Point;

        if (point is null)
            return false;

        return this.X == point.X && this.Y == point.Y;
    }


    /// <summary>
    /// Returns Kml representation of the point. Note: M values are ignored. Point must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    public override IRI.Maptor.Core.Ogc.Kml.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        return KmlPlacemarkHelper.CreatePointPlacemark(new Point(this.X, this.Y), projectToGeodeticFunc, color);
    }

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
        Span<byte> buffer = stackalloc byte[24];  // Stack-allocated, no heap allocation - X(8) + Y(8) + M(8)

        BitConverter.TryWriteBytes(buffer.Slice(0, 8), X);

        BitConverter.TryWriteBytes(buffer.Slice(8, 8), Y);

        BitConverter.TryWriteBytes(buffer.Slice(16, 8), M);

        return buffer.ToArray();  // Only allocates when creating final array
    }


    public string AsDelimited(char delimiter, int precision, bool useThousandSeparator)
    {
        var xFormatted = FormatHelper.FormatWithPrecision(X, precision, useThousandSeparator);

        var yFormatted = FormatHelper.FormatWithPrecision(Y, precision, useThousandSeparator);

        var mFormatted = FormatHelper.FormatWithPrecision(M, precision, useThousandSeparator);

        return $"{xFormatted}{delimiter}{yFormatted}{delimiter}{mFormatted}";
    }
}
