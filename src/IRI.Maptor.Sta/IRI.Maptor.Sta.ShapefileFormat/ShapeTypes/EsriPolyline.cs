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
    //public int Srid { get; set; }

    public override EsriShapeType EsriType => EsriShapeType.EsriPolyLine;


    //private BoundingBox boundingBox;
    //public BoundingBox MinimumBoundingBox => boundingBox;


    //private EsriPoint[] points;
    ///// <summary>
    ///// Points for All Parts
    ///// </summary>
    //public EsriPoint[] Points => this.points;

    //public int NumberOfPoints => this.Points == null ? 0 : this.points.Length;


    //private int[] parts;
    ///// <summary>
    ///// Index to First Point in Part
    ///// </summary>
    //public int[] Parts => this.parts;

    //public int NumberOfParts => this.parts?.Length ?? 0;


    public override int ContentLength => 22 + 2 * NumberOfParts + 8 * NumberOfPoints;

    public EsriPolyline() : this(Array.Empty<EsriPoint>()) { }

    internal EsriPolyline(BoundingBox boundingBox, int[] parts, EsriPoint[] points)
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

        this.boundingBox = boundingBox;

        this.parts = parts;

        this.points = points;
    }

    public EsriPolyline(EsriPoint[] points) : this(points, new int[] { 0 }) { }

    public EsriPolyline(EsriPoint[] points, int[] parts)
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

        this.parts = parts;
    }

    public EsriPolyline(EsriPoint[][] points)
    {
        if (points is null || points.Length < 1)
            throw new NotImplementedException();

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

    //public bool IsNullOrEmpty() => Points == null || Points.Length < 1;

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

    //public EsriPoint[] GetPart(int partNo) => ShapeHelper.GetEsriPoints(this, Parts[partNo]);

    //public string AsSqlServerWkt()
    //{
    //    if (this.NumberOfParts > 1)
    //    {
    //        StringBuilder result = new StringBuilder("MULTILINESTRING(");

    //        for (int i = 0; i < NumberOfParts; i++)
    //        {
    //            result.Append(string.Format("{0},", SqlServerWktHelper.PointGroupElementToWkt(ShapeHelper.GetEsriPoints(this, this.Parts[i]))));
    //        }

    //        return result.Remove(result.Length - 1, 1).Append(")").ToString();
    //    }
    //    else
    //    {
    //        return string.Format("LINESTRING{0}", SqlServerWktHelper.PointGroupElementToWkt(ShapeHelper.GetEsriPoints(this, this.Parts[0])));
    //    }
    //}

    ///// <summary>
    ///// Changed but not tested. 93.03.21
    ///// </summary>
    ///// <returns></returns>
    //public byte[] AsWkb()
    //{
    //    List<byte> result = new List<byte>();

    //    if (this.Parts.Count() == 1)
    //    {
    //        result.AddRange(OgcWkbMapFunctions.ToWkbLineString(ShapeHelper.GetEsriPoints(this, 0)));
    //    }
    //    else
    //    {
    //        result.Add((byte)WkbByteOrder.WkbNdr);

    //        result.AddRange(BitConverter.GetBytes((uint)WkbGeometryType.MultiLineString));

    //        result.AddRange(BitConverter.GetBytes((uint)this.parts.Length));

    //        for (int i = 0; i < this.parts.Length; i++)
    //        {
    //            result.AddRange(OgcWkbMapFunctions.ToWkbLineString(ShapeHelper.GetEsriPoints(this, this.Parts[i])));
    //        }
    //    }

    //    return result.ToArray();
    //}

    public override IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        return AsPlacemark(this, projectToGeodeticFunc, color);
    }

    /// <summary>
    /// Returs Kml representation of the point. Note: Point must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    static IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(EsriPolyline polyline, Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType placemark =
            new IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType();

        List<IRI.Maptor.Sta.KmlFormat.Primitives.LineStringType> linestrings =
            new List<IRI.Maptor.Sta.KmlFormat.Primitives.LineStringType>();

        IRI.Maptor.Sta.KmlFormat.Primitives.MultiGeometryType multiGeometry =
            new IRI.Maptor.Sta.KmlFormat.Primitives.MultiGeometryType();

        IEnumerable<string> coordinates;

        if (projectToGeodeticFunc != null)
        {
            coordinates = polyline.parts
                .Select(i =>
                    string.Join(" ", ShapeHelper.GetEsriPoints(polyline, i)
                    .Select(j =>
                    {
                        var temp = projectToGeodeticFunc(new Point(j.X, j.Y));
                        return string.Format("{0},{1}", temp.X, temp.Y);
                    }).ToArray()));
        }
        else
        {
            coordinates = polyline.parts
                .Select(i =>
                    string.Join(" ", ShapeHelper.GetEsriPoints(polyline, i)
                    .Select(j => string.Format("{0},{1}", j.X, j.Y))
                    .ToArray()));
        }

        foreach (string item in coordinates)
        {
            IRI.Maptor.Sta.KmlFormat.Primitives.LineStringType linestring = new IRI.Maptor.Sta.KmlFormat.Primitives.LineStringType();

            linestring.Coordinates.Add(item);

            linestrings.Add(linestring);
        }

        foreach (var line in linestrings)
        {
            multiGeometry.AbstractGeometryGroup.Add(line);
        }

        //placemark.AbstractFeatureObjectExtensionGroup = new IRI.Maptor.Sta.KmlFormat.Primitives.AbstractObjectType[] { multiGeometry };
        placemark.AbstractGeometryGroup = multiGeometry;
        //IRI.Maptor.Sta.KmlFormat.Primitives.MultiGeometryType t = new IRI.Maptor.Sta.KmlFormat.Primitives.MultiGeometryType();

        if (color == null)
        {
            return placemark;
        }

        IRI.Maptor.Sta.KmlFormat.Primitives.StyleType style =
            new IRI.Maptor.Sta.KmlFormat.Primitives.StyleType();

        IRI.Maptor.Sta.KmlFormat.Primitives.LineStyleType lineStyle = new IRI.Maptor.Sta.KmlFormat.Primitives.LineStyleType();
        lineStyle.Color = color;

        style.LineStyle = lineStyle;
        placemark.AbstractStyleSelectorGroup.Add(style);

        return placemark;
    }

    //public string AsKml(Func<Point, Point> projectToGeodeticFunc = null)
    //{
    //    return OgcKmlMapFunctions.AsKml(this.AsPlacemark(projectToGeodeticFunc));
    //}

    public override EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        return new EsriPolyline(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray(), this.Parts);
    }

    public override Geometry<Point> AsGeometry()
    {
        if (this.NumberOfParts > 1)
        {
            List<Geometry<Point>> parts = new List<Geometry<Point>>(this.NumberOfParts);

            for (int i = 0; i < NumberOfParts; i++)
            {
                //parts[i] = new Geometry<Point>(ShapeHelper.GetPoints(this, Parts[i]), GeometryType.LineString, Srid);
                parts.Add(new Geometry<Point>(ShapeHelper.GetPoints(this, Parts[i]), GeometryType.LineString, Srid));
            }

            return new Geometry<Point>(parts, GeometryType.MultiLineString, Srid);
        }
        else if (this.NumberOfParts == 1)
        {
            return new Geometry<Point>(ShapeHelper.GetPoints(this, Parts[0]), GeometryType.LineString, Srid);
        }
        else
        {
            return Geometry<Point>.CreateEmpty(GeometryType.LineString, Srid);
        }
    }
}
