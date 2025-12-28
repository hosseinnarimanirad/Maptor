// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;


namespace IRI.Maptor.Sta.ShapefileFormat.EsriType;

public class EsriPolyline : EsriPointCollection
{ 
    public override EsriShapeType EsriType => EsriShapeType.EsriPolyLine;
     
    public override int ContentLength => 22 + 2 * NumberOfParts + 8 * NumberOfPoints;

    public EsriPolyline() : this(Array.Empty<EsriPoint>()) { }

    internal EsriPolyline(BoundingBox boundingBox, int[] parts, EsriPoint[] points)
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

    public EsriPolyline(EsriPoint[] points) : this(points, new int[] { 0 }) { }

    public EsriPolyline(EsriPoint[] points, int[] parts)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));
        if (parts is null)
            throw new ArgumentNullException(nameof(parts));

        this.Srid = points.Length == 0 ? 0 : points.First().Srid;

        this.boundingBox = BoundingBox.CalculateBoundingBox(points/*.Cast<IPoint>()*/);

        this.points = points;

        this.parts = parts;
    }

    public EsriPolyline(EsriPoint[][] points)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));
        if (points.Length < 1)
            throw new ArgumentException("Points array must contain at least one part.", nameof(points));

        this.points = points.Where(i => i.Length > 1).SelectMany(i => i).ToArray();

        this.Srid = this.points.First().Srid;

        this.parts = new int[points.Length];

        for (int i = 1; i < points.Length; i++)
        {
            parts[i] = points.Where((array, index) => index < i).Sum(array => array.Length);
        }

        var boundingBoxes = points.Select(i => BoundingBox.CalculateBoundingBox(i/*.Cast<IPoint>()*/));

        this.boundingBox = BoundingBox.GetMergedBoundingBox(boundingBoxes);
    }

    public override bool IsRingBase() => false;
     
    public override byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriPolyLine), 0, ShapeConstants.IntegerSize);

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
     
    public override IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        var coordinateStrings = this.Parts.Select(partIndex =>
        {
            var points = ShapeHelper.GetEsriPoints(this, partIndex)
                .Select(p => new Point(p.X, p.Y));
            return KmlPlacemarkHelper.FormatRingCoordinates(points, projectToGeodeticFunc, closeRing: false);
        });

        return KmlPlacemarkHelper.CreateLineStringPlacemark(coordinateStrings, color);
    }
     
    public override EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        return new EsriPolyline(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray(), this.Parts);
    }

    public override Geometry<Point> AsGeometry() => CreateLineStringOrMultiLineStringGeometry();
}
