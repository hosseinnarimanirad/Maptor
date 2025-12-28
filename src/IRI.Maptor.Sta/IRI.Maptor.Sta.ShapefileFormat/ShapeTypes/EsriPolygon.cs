// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;


namespace IRI.Maptor.Sta.ShapefileFormat.EsriType;


// A polygon consists of one or more rings.
// A ring is a connected sequence of four or more points that form a closed, non-self-intersecting loop.
// The rings are closed (the first and last vertex of a ring MUST be the same).
// A polygon may contain multiple outer rings.
// The order of vertices or orientation for a ring indicates which side of the ring is the interior of the polygon.
// The neighborhood to the right of an observer walking along the ring in vertex order is the neighborhood inside the polygon.
// Vertices of rings defining holes in polygons are in a counterclockwise direction.
// Vertices for a single, ringed polygon are, therefore, always in clockwise order.
// The rings of a polygon are referred to as its parts.
public class EsriPolygon : EsriPointCollection
{ 
    public override EsriShapeType EsriType => EsriShapeType.EsriPolygon;
     
    public override int ContentLength => 22 + 2 * NumberOfParts + 8 * NumberOfPoints;

    public EsriPolygon() : this(Array.Empty<EsriPoint>()) { }

    public EsriPolygon(EsriPoint[] points) : this(points, [0]) { }

    internal EsriPolygon(BoundingBox boundingBox, int[] parts, EsriPoint[] points)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));
        if (parts is null)
            throw new ArgumentNullException(nameof(parts));

        this.Srid = points.Length == 0 ? 0 : points.First().Srid;

        this.boundingBox = boundingBox;

        this.parts = parts;

        this.points = points;
    }

    public EsriPolygon(EsriPoint[] points, int[] parts)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));
        if (parts is null)
            throw new ArgumentNullException(nameof(parts));

        this.Srid = points.Length == 0 ? 0 : points.First().Srid;

        this.boundingBox = BoundingBox.CalculateBoundingBox(points);

        this.parts = parts;

        this.points = points;
    }

    public EsriPolygon(EsriPoint[][] points)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));

        this.points = points.Where(i => i.Length > 3).SelectMany(i => i).ToArray();

        this.Srid = this.points.Length == 0 ? 0 : this.points.First().Srid;

        this.parts = new int[points.Length];

        for (int i = 1; i < points.Length; i++)
        {
            parts[i] = points.Where((array, index) => index < i).Sum(array => array.Length);
        }

        var boundingBoxes = points.Select(i => BoundingBox.CalculateBoundingBox(i/*.Cast<IPoint>()*/));

        this.boundingBox = BoundingBox.GetMergedBoundingBox(boundingBoxes);
    }

    public override bool IsRingBase() => true;
     

    public override byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriPolygon), 0, ShapeConstants.IntegerSize);

        result.Write(Writer.ShpWriter.WriteBoundingBoxToByte(this), 0, 4 * ShapeConstants.DoubleSize);

        result.Write(System.BitConverter.GetBytes(this.NumberOfParts), 0, ShapeConstants.IntegerSize);

        result.Write(System.BitConverter.GetBytes(this.NumberOfPoints), 0, ShapeConstants.IntegerSize);

        foreach (int item in this.parts)
        {
            result.Write(System.BitConverter.GetBytes(item), 0, ShapeConstants.IntegerSize);
        }

        byte[] tempPoints = Writer.ShpWriter.WritePointsToByte(this.points);

        result.Write(tempPoints, 0, tempPoints.Length);

        return result.ToArray();
    }
     
    /// <summary>
    /// Returs Kml representation of the polygon. Note: Polygon must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    public override IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        if (this.NumberOfParts == 0 || this.NumberOfPoints == 0)
        {
            return new IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType();
        }

        var outerRing = ShapeHelper.GetPoints(this, this.Parts[0]);
        var innerRings = this.NumberOfParts > 1 
            ? Enumerable.Range(1, this.NumberOfParts - 1).Select(i => ShapeHelper.GetPoints(this, this.Parts[i]))
            : null;

        return KmlPlacemarkHelper.CreatePolygonPlacemark(outerRing, innerRings, projectToGeodeticFunc, color);
    }
     
    public override EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        return new EsriPolygon(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray(), this.Parts);
    }


    public override Geometry<Point> AsGeometry() => CreatePolygonOrMultiPolygonGeometry();
}
