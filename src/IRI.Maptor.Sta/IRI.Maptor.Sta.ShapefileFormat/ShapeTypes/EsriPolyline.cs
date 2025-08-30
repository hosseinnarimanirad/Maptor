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

public struct EsriPolyline : IEsriSimplePoints
{
    /// <summary>
    /// MinX, MinY, MaxX, MaxY
    /// </summary>
    private BoundingBox boundingBox;

    public int Srid { get; set; }

    private EsriPoint[] points;

    public int[] Parts
    {
        get { return this.parts; }
    }

    public int NumberOfPoints
    {
        get { return this.Points == null ? 0 : this.points.Length; }
    }

    /// <summary>
    /// Points for All Parts
    /// </summary>
    public EsriPoint[] Points
    {
        get { return this.points; }
    }


    /// <summary>
    /// Index to First Point in Part
    /// </summary>
    private int[] parts;

    public int NumberOfParts
    {
        get { return this.parts.Length; }
    }

    internal EsriPolyline(BoundingBox boundingBox, int[] parts, EsriPoint[] points)
    {
        if (points == null)
        {
            throw new NotImplementedException();
        }

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

    public EsriPolyline(EsriPoint[] points)
        : this(points, new int[] { 0 })
    {

    }

    public EsriPolyline(EsriPoint[] points, int[] parts)
    {
        if (points == null)
        {
            throw new NotImplementedException();
        }

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
        if (points == null || points.Length < 1)
        {
            throw new NotImplementedException();
        }

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

    public byte[] WriteContentsToByte()
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

    public int ContentLength
    {
        get { return 22 + 2 * NumberOfParts + 8 * NumberOfPoints; }
    }

    public EsriShapeType Type
    {
        get { return EsriShapeType.EsriPolyLine; }
    }

    public EsriPoint[] GetPart(int partNo)
    {
        return ShapeHelper.GetEsriPoints(this, Parts[partNo]);
    }


    public BoundingBox MinimumBoundingBox
    {
        get { return boundingBox; }
    }

    public string AsSqlServerWkt()
    {
        if (this.NumberOfParts > 1)
        {
            StringBuilder result = new StringBuilder("MULTILINESTRING(");

            for (int i = 0; i < NumberOfParts; i++)
            {
                result.Append(string.Format("{0},", SqlServerWktHelper.PointGroupElementToWkt(ShapeHelper.GetEsriPoints(this, this.Parts[i]))));
            }

            return result.Remove(result.Length - 1, 1).Append(")").ToString();
        }
        else
        {
            return string.Format("LINESTRING{0}", SqlServerWktHelper.PointGroupElementToWkt(ShapeHelper.GetEsriPoints(this, this.Parts[0])));
        }

    }

    /// <summary>
    /// Changed but not tested. 93.03.21
    /// </summary>
    /// <returns></returns>
    public byte[] AsWkb()
    {
        List<byte> result = new List<byte>();

        if (this.Parts.Count() == 1)
        {
            result.AddRange(OgcWkbMapFunctions.ToWkbLineString(ShapeHelper.GetEsriPoints(this, 0)));
        }
        else
        {
            result.Add((byte)WkbByteOrder.WkbNdr);

            result.AddRange(BitConverter.GetBytes((uint)WkbGeometryType.MultiLineString));

            result.AddRange(BitConverter.GetBytes((uint)this.parts.Length));

            for (int i = 0; i < this.parts.Length; i++)
            {
                result.AddRange(OgcWkbMapFunctions.ToWkbLineString(ShapeHelper.GetEsriPoints(this, this.Parts[i])));
            }
        }

        return result.ToArray();
    }

    public IRI.Maptor.Ket.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        return AsPlacemark(this, projectToGeodeticFunc, color);
    }

    /// <summary>
    /// Returs Kml representation of the point. Note: Point must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    static IRI.Maptor.Ket.KmlFormat.Primitives.PlacemarkType AsPlacemark(EsriPolyline polyline, Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        IRI.Maptor.Ket.KmlFormat.Primitives.PlacemarkType placemark =
            new Ket.KmlFormat.Primitives.PlacemarkType();

        List<IRI.Maptor.Ket.KmlFormat.Primitives.LineStringType> linestrings =
            new List<Ket.KmlFormat.Primitives.LineStringType>();

        IRI.Maptor.Ket.KmlFormat.Primitives.MultiGeometryType multiGeometry =
            new Ket.KmlFormat.Primitives.MultiGeometryType();

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
            IRI.Maptor.Ket.KmlFormat.Primitives.LineStringType linestring = new Ket.KmlFormat.Primitives.LineStringType();

            linestring.coordinates = item;

            linestrings.Add(linestring);
        }

        multiGeometry.AbstractGeometry = linestrings.ToArray();

        //placemark.AbstractFeatureObjectExtensionGroup = new Ket.KmlFormat.Primitives.AbstractObjectType[] { multiGeometry };
        placemark.AbstractGeometry = multiGeometry;
        //IRI.Maptor.Ket.KmlFormat.Primitives.MultiGeometryType t = new Ket.KmlFormat.Primitives.MultiGeometryType();

        if (color == null)
        {
            return placemark;
        }

        IRI.Maptor.Ket.KmlFormat.Primitives.StyleType style =
            new Ket.KmlFormat.Primitives.StyleType();

        Ket.KmlFormat.Primitives.LineStyleType lineStyle = new Ket.KmlFormat.Primitives.LineStyleType();
        lineStyle.color = color;

        style.LineStyle = lineStyle;
        placemark.Styles = new Ket.KmlFormat.Primitives.AbstractStyleSelectorType[] { style };

        return placemark;
    }

    public string AsKml(Func<Point, Point> projectToGeodeticFunc = null)
    {
        return OgcKmlMapFunctions.AsKml(this.AsPlacemark(projectToGeodeticFunc));
    }

    public IEsriShape Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        return new EsriPolyline(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray(), this.Parts);
    }

    public Geometry<Point> AsGeometry()
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

    public bool IsNullOrEmpty()
    {
        return Points == null || Points.Length < 1;
    }

    public bool IsRingBase() => false;

}
