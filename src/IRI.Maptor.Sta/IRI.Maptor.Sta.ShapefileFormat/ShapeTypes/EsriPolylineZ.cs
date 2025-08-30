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
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Spatial.Primitives.Esri;

namespace IRI.Maptor.Sta.ShapefileFormat.EsriType;

public struct EsriPolylineZ : IEsriPointsWithZ
{ 
    /// <summary>
    /// MinX, MinY, MaxX, MaxY
    /// </summary>
    private BoundingBox boundingBox;

    public int Srid { get; set; }

    private EsriPoint[] points;

    public int NumberOfPoints
    {
        get { return this.points.Length; }
    }

    /// <summary>
    /// Points for All Parts
    /// </summary>
    public EsriPoint[] Points
    {
        get { return this.points; }
    }

    public int[] Parts
    {
        get { return this.parts; }
    }

    /// <summary>
    /// Index to First Point in Part
    /// </summary>
    private int[] parts;

    public int NumberOfParts
    {
        get { return this.parts.Length; }
    }


    private double minMeasure, maxMeasure;

    private double[] measures;

    public double MinMeasure
    {
        get { return this.minMeasure; }
    }

    public double MaxMeasure
    {
        get { return this.maxMeasure; }
    }

    public double[] Measures
    {
        get { return this.measures; }
    }


    private double minZ, maxZ;

    private double[] zValues;

    public double MinZ
    {
        get { return this.minZ; }
    }

    public double MaxZ
    {
        get { return this.maxZ; }
    }

    public double[] ZValues
    {
        get { return this.zValues; }
    }

    public EsriPolylineZ(EsriPoint[] points, int[] parts, double[] zValues, double[] measures)
    {
        if (points == null || points.Length != zValues?.Length || points.Length != measures?.Length)
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

        this.parts = parts;

        this.points = points;

        this.measures = measures;

        this.zValues = zValues;

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

        if (zValues?.Count() > 0)
        {
            this.minZ = zValues.Min();

            this.maxZ = zValues.Max();
        }
        else
        {
            this.minZ = EsriConstants.NoDataValue;

            this.maxZ = EsriConstants.NoDataValue;
        }

    }

    public EsriPolylineZ(BoundingBox boundingBox,
                        int[] parts,
                        EsriPoint[] points,
                        double minZ,
                        double maxZ,
                        double[] zValues,
                        double minMeasure,
                        double maxMeasure,
                        double[] measures)
    {
        if (points == null || points.Length != zValues?.Length || points.Length != measures?.Length)
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

        this.minZ = minZ;

        this.maxZ = maxZ;

        this.zValues = zValues;

        this.minMeasure = minMeasure;

        this.maxMeasure = maxMeasure;

        this.measures = measures;
    }

    //private Point[] GetPoints(int partNumber)
    //{
    //    int startIndex = parts[partNumber];

    //    int lastIndex = (partNumber == NumberOfParts - 1) ? NumberOfPoints - 1 : parts[partNumber + 1] - 1;

    //    Point[] result = new Point[lastIndex - startIndex + 1];

    //    Array.ConstrainedCopy(this.Points, startIndex, result, 0, result.Length);

    //    return result;
    //}

    //private double[] GetMeasures(int partNumber)
    //{
    //    int startIndex = parts[partNumber];

    //    int lastIndex = (partNumber == NumberOfParts - 1) ? NumberOfPoints - 1 : parts[partNumber + 1] - 1;

    //    double[] result = new double[lastIndex - startIndex + 1];

    //    Array.ConstrainedCopy(this.Measures, startIndex, result, 0, result.Length);

    //    return result;
    //}

    //private double[] GetZValues(int partNumber)
    //{
    //    int startIndex = parts[partNumber];

    //    int lastIndex = (partNumber == NumberOfParts - 1) ? NumberOfPoints - 1 : parts[partNumber + 1] - 1;

    //    double[] result = new double[lastIndex - startIndex + 1];

    //    Array.ConstrainedCopy(this.ZValues, startIndex, result, 0, result.Length);

    //    return result;
    //}        

    public byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriPolyLineZM), 0, ShapeConstants.IntegerSize);

        result.Write(Writer.ShpWriter.WriteBoundingBoxToByte(this), 0, 4 * ShapeConstants.DoubleSize);

        result.Write(System.BitConverter.GetBytes(this.NumberOfParts), 0, ShapeConstants.IntegerSize);

        result.Write(System.BitConverter.GetBytes(this.NumberOfPoints), 0, ShapeConstants.IntegerSize);

        foreach (int item in this.parts)
        {
            result.Write(System.BitConverter.GetBytes(item), 0, ShapeConstants.IntegerSize);
        }


        byte[] tempPoints = Writer.ShpWriter.WritePointsToByte(this.points);

        result.Write(tempPoints, 0, tempPoints.Length);


        byte[] tempZ = Writer.ShpWriter.WriteAdditionalData(this.MinZ, this.MaxZ, this.ZValues);

        result.Write(tempZ, 0, tempZ.Length);


        byte[] tempMeasures = Writer.ShpWriter.WriteAdditionalData(this.MinMeasure, this.MaxMeasure, this.Measures);

        result.Write(tempMeasures, 0, tempMeasures.Length);

        return result.ToArray();
    }

    public int ContentLength
    {
        get { return 22 + 2 * NumberOfParts + 8 * NumberOfPoints + 2 * (8 + 4 * NumberOfPoints); }
    }

    public EsriShapeType Type
    {
        get { return EsriShapeType.EsriPolyLineZM; }
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
        StringBuilder result = new StringBuilder("MULTILINESTRING(");

        for (int i = 0; i < NumberOfParts; i++)
        {
            result.Append(
                string.Format("{0},",
                SqlServerWktHelper.PointZGroupElementToWkt(
                    ShapeHelper.GetEsriPoints(this, i), ShapeHelper.GetZValues(this, i), ShapeHelper.GetMeasures(this, this.Parts[i]))));
        }

        return result.Remove(result.Length - 1, 1).Append(")").ToString();
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
            result.AddRange(OgcWkbMapFunctions.ToWkbLineStringZM(
                ShapeHelper.GetEsriPoints(this, 0), ShapeHelper.GetZValues(this, 0), ShapeHelper.GetMeasures(this, 0)));
        }
        else
        {
            result.Add((byte)WkbByteOrder.WkbNdr);

            result.AddRange(BitConverter.GetBytes((uint)WkbGeometryType.MultiLineStringZM));

            result.AddRange(BitConverter.GetBytes((uint)this.parts.Length));

            for (int i = 0; i < this.parts.Length; i++)
            {
                result.AddRange(
                    OgcWkbMapFunctions.ToWkbLineStringZM(
                        ShapeHelper.GetEsriPoints(this, this.Parts[i]), ShapeHelper.GetZValues(this, this.Parts[i]), ShapeHelper.GetMeasures(this, this.Parts[i])));
            }
        }

        return result.ToArray();
    }


    public IRI.Maptor.Ket.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
    {
        return AsPlacemark(this, projectToGeodeticFunc);
    }

    /// <summary>
    /// Returs Kml representation of the point. Note: Z,M values are igonred
    /// </summary>
    /// <returns></returns>
    static IRI.Maptor.Ket.KmlFormat.Primitives.PlacemarkType AsPlacemark(EsriPolylineZ polyline, Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null)
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
            coordinates = polyline.Parts
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

        multiGeometry.AbstractGeometryObjectExtensionGroup = linestrings.ToArray();

        placemark.AbstractFeatureObjectExtensionGroup = new Ket.KmlFormat.Primitives.AbstractObjectType[] { multiGeometry };

        return placemark;
    }

    public string AsKml(Func<Point, Point> projectToGeodeticFunc = null)
    {
        return OgcKmlMapFunctions.AsKml(this.AsPlacemark(projectToGeodeticFunc));
    }

    public IEsriShape Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        return new EsriPolylineZ(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray(), this.Parts, this.ZValues, this.Measures);
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
