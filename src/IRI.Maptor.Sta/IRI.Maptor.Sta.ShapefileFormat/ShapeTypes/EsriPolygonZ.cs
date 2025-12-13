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

public class EsriPolygonZ : IEsriPointsWithZ
{
    //public int Srid { get; set; }

    public override EsriShapeType EsriType => EsriShapeType.EsriPolygonZM;

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

    //private double minZ, maxZ;
    //private double[] zValues;

    //public double MinZ => this.minZ;

    //public double MaxZ => this.maxZ;

    //public double[] ZValues => this.zValues;

    public override int ContentLength => 22 + 2 * NumberOfParts + 8 * NumberOfPoints + 2 * (8 + 4 * NumberOfPoints);

    public EsriPolygonZ() : this(Array.Empty<EsriPoint>(), Array.Empty<int>(), Array.Empty<double>(), Array.Empty<double>()) { }

    public EsriPolygonZ(EsriPoint[] points, int[] parts, double[] zValues, double[] measures)
    {
        if (points is null || points.Length != zValues?.Length || points.Length != measures?.Length)
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

    internal EsriPolygonZ(BoundingBox boundingBox,
                        int[] parts,
                        EsriPoint[] points,
                        double minZ,
                        double maxZ,
                        double[] zValues,
                        double minMeasure,
                        double maxMeasure,
                        double[] measures)
    {
        if (points is null || points.Length != zValues?.Length || points.Length != measures?.Length)
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

        this.minZ = minZ;

        this.maxZ = maxZ;

        this.zValues = zValues;

        this.minMeasure = minMeasure;

        this.maxMeasure = maxMeasure;

        this.measures = measures;
    }

    public override bool IsRingBase() => true;

    //public bool IsNullOrEmpty() => Points == null || Points.Length < 1;

    public override byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriPolygonZM), 0, ShapeConstants.IntegerSize);

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

    //public EsriPoint[] GetPart(int partNo) => ShapeHelper.GetEsriPoints(this, Parts[partNo]);

    //public string AsSqlServerWkt()
    //{
    //    StringBuilder result = new StringBuilder("POLYGON(");

    //    for (int i = 0; i < NumberOfParts; i++)
    //    {
    //        result.Append(string.Format("{0},",
    //            SqlServerWktHelper.PointZGroupElementToWkt(
    //                ShapeHelper.GetEsriPoints(this, this.Parts[i]),
    //                ShapeHelper.GetZValues(this, this.Parts[i]),
    //                ShapeHelper.GetMeasures(this, this.Parts[i]))));
    //    }

    //    return result.Remove(result.Length - 1, 1).Append(")").ToString();
    //}

    ////Error Prone: not checking for multipolygon cases
    //public byte[] AsWkb()
    //{
    //    List<byte> result = new List<byte>
    //    {
    //        (byte)WkbByteOrder.WkbNdr
    //    };

    //    result.AddRange(BitConverter.GetBytes((uint)WkbGeometryType.PolygonZM));

    //    result.AddRange(BitConverter.GetBytes((uint)this.parts.Length));

    //    for (int i = 0; i < this.parts.Length; i++)
    //    {
    //        result.AddRange(OgcWkbMapFunctions.ToWkbLinearRingZM(
    //                ShapeHelper.GetEsriPoints(this, this.Parts[i]),
    //                ShapeHelper.GetZValues(this, this.Parts[i]),
    //                ShapeHelper.GetMeasures(this, this.Parts[i])));
    //    }

    //    return result.ToArray();
    //}

    /// <summary>
    /// Returs Kml representation of the point. Note: Point must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    public override IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectFunc = null, byte[] color = null)
    {
        throw new NotImplementedException();
    }

    //public string AsKml(Func<Point, Point> projectToGeodeticFunc = null)
    //{
    //    return OgcKmlMapFunctions.AsKml(this.AsPlacemark(projectToGeodeticFunc));
    //}

    public override IEsriShape Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        return new EsriPolygonZ(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray(), this.Parts, this.ZValues, this.Measures);
    }

    //always returns polygon not multi polygon
    public override Geometry<Point> AsGeometry()
    {
        if (this.NumberOfParts > 1)
        {
            List<Geometry<Point>> parts = new List<Geometry<Point>>(this.NumberOfParts);

            for (int i = 0; i < NumberOfParts; i++)
            {
                parts.Add(new Geometry<Point>(ShapeHelper.GetPoints(this, Parts[i]), GeometryType.LineString, Srid));
            }

            return Geometry<Point>.CreatePolygonOrMultiPolygon(parts, Srid);
        }
        else if (this.NumberOfParts == 1)
        {
            return new Geometry<Point>(new List<Geometry<Point>>() { new Geometry<Point>(ShapeHelper.GetPoints(this, Parts[0]), GeometryType.LineString, Srid) }, GeometryType.Polygon, Srid);
        }
        else
        {
            return Geometry<Point>.CreateEmpty(GeometryType.Polygon, Srid);
        }
    }
}
