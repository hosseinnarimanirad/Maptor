// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.IO.EsriJson;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Core.Common.Helpers;


namespace IRI.Maptor.Core.ShapefileFormat.EsriType;

public class EsriPointZ : EsriShapeBase, IPoint, IHasZ
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
        set { this.z = value; }
    }

    public double Measure
    {
        get { return this.measure; }
        set { this.measure = value; }
    }

    public EsriPointZ() { }

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

    public bool HaveTheSameXY(object obj)
    {
        var point = obj as Point;

        if (point is null)
            return false;

        return this.X == point.X && this.Y == point.Y;
    }

    #region IShape Members


    public override BoundingBox MinimumBoundingBox => new BoundingBox(this.X, this.Y, this.X, this.Y);

    public override byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriPointZM), 0, ShapeConstants.IntegerSize);

        result.Write(Writer.ShpWriter.CheckNoDataAndGetByteValue(this.X), 0, ShapeConstants.DoubleSize);

        result.Write(Writer.ShpWriter.CheckNoDataAndGetByteValue(this.Y), 0, ShapeConstants.DoubleSize);

        result.Write(Writer.ShpWriter.CheckNoDataAndGetByteValue(this.Z), 0, ShapeConstants.DoubleSize);

        result.Write(Writer.ShpWriter.CheckNoDataAndGetByteValue(this.Measure), 0, ShapeConstants.DoubleSize);

        return result.ToArray();
    }

    public override int ContentLength => ShapeConstants.PointZContentLengthInWords;

    public override EsriShapeType EsriType => EsriShapeType.EsriPointZM;

    /// <summary>
    /// Returns Kml representation of the point. Note: Z,M values are ignored. Point must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    public override IRI.Maptor.Core.Ogc.Kml.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        return KmlPlacemarkHelper.CreatePointPlacemark(new Point(this.X, this.Y), projectToGeodeticFunc, color);
    }

    public override EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        var result = transform(this);

        return new EsriPointZ(result.X, result.Y, this.Z, this.Measure, newSrid);
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

    #endregion

    public bool IsNaN()
    {
        return double.IsNaN(X) || double.IsNaN(Y);
    }

    public byte[] AsByteArray()
    {
        // Option #3
        Span<byte> buffer = stackalloc byte[32];  // Stack-allocated, no heap allocation - X(8) + Y(8) + Z(8) + Measure(8)

        BitConverter.TryWriteBytes(buffer.Slice(0, 8), X);

        BitConverter.TryWriteBytes(buffer.Slice(8, 8), Y);

        BitConverter.TryWriteBytes(buffer.Slice(16, 8), Z);

        BitConverter.TryWriteBytes(buffer.Slice(24, 8), Measure);

        return buffer.ToArray();  // Only allocates when creating final array
    }

    public string AsDelimited(char delimiter, int precision, bool useThousandSeparator)
    {
        var xFormatted = FormatHelper.FormatWithPrecision(X, precision, useThousandSeparator);

        var yFormatted = FormatHelper.FormatWithPrecision(Y, precision, useThousandSeparator);

        var zFormatted = FormatHelper.FormatWithPrecision(Z, precision, useThousandSeparator);

        return $"{xFormatted}{delimiter}{yFormatted}{delimiter}{zFormatted}";
    }
}
