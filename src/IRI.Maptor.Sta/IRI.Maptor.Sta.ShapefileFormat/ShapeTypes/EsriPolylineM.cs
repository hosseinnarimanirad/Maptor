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
using IRI.Maptor.Sta.Spatial.Primitives.Esri;


namespace IRI.Maptor.Sta.ShapefileFormat.EsriType;

public class EsriPolylineM : EsriPointMCollection
{
    //public int Srid { get; set; }

    public override EsriShapeType EsriType => EsriShapeType.EsriPolyLineM;


    //private BoundingBox boundingBox;
    //public BoundingBox MinimumBoundingBox => boundingBox;


    //private EsriPoint[] points;
    ///// <summary>
    ///// Points for All Parts
    ///// </summary>
    //public EsriPoint[] Points => this.points;

    //public int NumberOfPoints => this.points?.Length ?? 0;


    //private int[] parts;
    ///// <summary>
    ///// Index to First Point in Part
    ///// </summary>
    //public int[] Parts => this.parts;

    //public int NumberOfParts => this.parts?.Length ?? 0;


    //private double minMeasure, maxMeasure;
    //private double[] measures;

    //public double MinMeasure => this.minMeasure;
    //public double MaxMeasure => this.maxMeasure;
    //public double[] Measures => this.measures;


    public override int ContentLength => 22 + 2 * NumberOfParts + 8 * NumberOfPoints + 8 + 4 * NumberOfPoints;

    public EsriPolylineM() : this(Array.Empty<EsriPoint>(), Array.Empty<int>(), Array.Empty<double>()) { }

    public EsriPolylineM(EsriPoint[] points, int[] parts, double[] measures)
    {
        if (points is null || points.Length != measures.Length)
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

        this.parts = parts;

        this.points = points;

        this.measures = measures;

        if (measures?.Count() > 0)
        {
            this.minMeasure = measures.Min();

            this.maxMeasure = measures.Max();
        }
        else
        {
            this.minMeasure = EsriConstants.NoDataValue;

            this.maxMeasure = EsriConstants.NoDataValue;
        }
    }

    internal EsriPolylineM(BoundingBox boundingBox, int[] parts, EsriPoint[] points, double minMeasure, double maxMeasure, double[] measures)
    {
        if (points is null || points.Length != measures.Length)
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

        this.minMeasure = minMeasure;

        this.maxMeasure = maxMeasure;

        this.measures = measures;
    }

    public override bool IsRingBase() => false;

    //public bool IsNullOrEmpty() => Points == null || Points.Length < 1;

    public override byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriPolyLineM), 0, ShapeConstants.IntegerSize);

        result.Write(Writer.ShpWriter.WriteBoundingBoxToByte(this), 0, 4 * ShapeConstants.DoubleSize);

        result.Write(System.BitConverter.GetBytes(this.NumberOfParts), 0, ShapeConstants.IntegerSize);

        result.Write(System.BitConverter.GetBytes(this.NumberOfPoints), 0, ShapeConstants.IntegerSize);

        foreach (int item in this.parts)
        {
            result.Write(System.BitConverter.GetBytes(item), 0, ShapeConstants.IntegerSize);
        }

        byte[] tempPoints = Writer.ShpWriter.WritePointsToByte(this.points);

        result.Write(tempPoints, 0, tempPoints.Length);

        byte[] tempMeasures = Writer.ShpWriter.WriteAdditionalData(this.MinMeasure, this.MaxMeasure, this.Measures);

        result.Write(tempMeasures, 0, tempMeasures.Length);

        return result.ToArray();
    }

    //public EsriPoint[] GetPart(int partNo) => ShapeHelper.GetEsriPoints(this, Parts[partNo]);

    //public string AsSqlServerWkt()
    //{
    //    StringBuilder result = new StringBuilder("MULTILINESTRING(");

    //    for (int i = 0; i < NumberOfParts; i++)
    //    {
    //        result.Append(string.Format("{0},",
    //            SqlServerWktHelper.PointMGroupElementToWkt(ShapeHelper.GetEsriPoints(this, i), ShapeHelper.GetMeasures(this, this.Parts[i]))));
    //    }

    //    return result.Remove(result.Length - 1, 1).Append(")").ToString();
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
    //        result.AddRange(OgcWkbMapFunctions.ToWkbLineStringM(ShapeHelper.GetEsriPoints(this, 0), ShapeHelper.GetMeasures(this, 0)));
    //    }
    //    else
    //    {
    //        result.Add((byte)WkbByteOrder.WkbNdr);

    //        result.AddRange(BitConverter.GetBytes((uint)WkbGeometryType.MultiLineStringM));

    //        result.AddRange(BitConverter.GetBytes((uint)this.parts.Length));

    //        for (int i = 0; i < this.parts.Length; i++)
    //        {
    //            result.AddRange(OgcWkbMapFunctions.ToWkbLineStringM(ShapeHelper.GetEsriPoints(this, i), ShapeHelper.GetMeasures(this, this.Parts[i])));
    //        }
    //    }

    //    return result.ToArray();
    //}

    public override IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        return AsPlacemark(this, projectToGeodeticFunc);
    }

    /// <summary>
    /// Returs Kml representation of the point. Note: M values are igonred
    /// </summary>
    /// <returns></returns>
    static IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(EsriPolylineM polyline, Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
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
            coordinates = polyline.Parts
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

        placemark.AbstractGeometryGroup = multiGeometry;

        return placemark;
    }

    //public string AsKml(Func<Point, Point> projectToGeodeticFunc = null)
    //{
    //    return OgcKmlMapFunctions.AsKml(this.AsPlacemark(projectToGeodeticFunc));
    //}

    public override EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        return new EsriPolylineM(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray(), this.Parts, this.Measures);
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
