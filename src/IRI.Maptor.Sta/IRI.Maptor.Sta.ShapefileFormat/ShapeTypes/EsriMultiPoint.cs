// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Linq;

using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives; 
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Sta.Common.Abstractions;


namespace IRI.Maptor.Sta.ShapefileFormat.EsriType;

public class EsriMultiPoint : EsriPointCollection
{
    public override EsriShapeType EsriType => EsriShapeType.EsriMultiPoint;

    public override int ContentLength => 20 + 8 * NumberOfPoints;

    public EsriMultiPoint() : this(Array.Empty<EsriPoint>()) { }

    public EsriMultiPoint(EsriPoint[] points)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));

        this.Srid = points.Length == 0 ? 0 : points.First().Srid;

        this.boundingBox = BoundingBox.CalculateBoundingBox(points);

        this.points = points;
    }

    internal EsriMultiPoint(BoundingBox boundingBox, EsriPoint[] points)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));
        if (points.Length < 1)
            throw new ArgumentException("Points array must contain at least one point.", nameof(points));

        this.boundingBox = boundingBox;

        this.points = points;

        this.Srid = points.First().Srid;
    }

    public override bool IsRingBase() => false;

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
    /// Returns Kml representation of the multipoint. Note: Points must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    public override IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        var points = this.Points.Select(p => new Point(p.X, p.Y));
        return KmlPlacemarkHelper.CreateMultiPointPlacemark(points, projectToGeodeticFunc, color);
    }

    public override EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid) 
    {
        return new EsriMultiPoint(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray());
    }

    public override Geometry<Point> AsGeometry() => CreateMultiPointGeometry();
}
