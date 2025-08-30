// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Sta.Spatial.Primitives.Esri;

namespace IRI.Maptor.Sta.ShapefileFormat.EsriType;

public struct EsriMultiPointZ : IEsriPointsWithZ
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

    public EsriPoint[] Points
    {
        get { return this.points; }
    }

    public int[] Parts
    {
        get { return new int[] { 0 }; }
    }

    public int NumberOfParts
    {
        get { return this.Parts.Length; }
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

    public EsriMultiPointZ(EsriPoint[] points, double[] zValues, double[] measures)
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

    internal EsriMultiPointZ(BoundingBox boundingBox,
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
         
        this.points = points;

        this.minZ = minZ;

        this.maxZ = maxZ;

        this.zValues = zValues;

        this.minMeasure = minMeasure;

        this.maxMeasure = maxMeasure;

        this.measures = measures;
    }

    public EsriMultiPointZ(EsriPointZ[] points)
    {
        if (points == null || points.Length < 1)
        {
            throw new NotImplementedException();
        }

        this.boundingBox = BoundingBox.CalculateBoundingBox(points/*.Cast<IPoint>()*/);

        this.Srid = points.First().Srid;

        this.points = new EsriPoint[points.Length];

        this.measures = new double[points.Length];

        this.zValues = new double[points.Length];

        this.minMeasure = points[0].Measure;

        this.maxMeasure = points[0].Measure;

        this.minZ = points[0].Z;

        this.maxZ = points[0].Z;

        for (int i = 0; i < points.Length; i++)
        {
            this.points[i] = new EsriPoint(points[i].X, points[i].Y, points[i].Srid);

            this.measures[i] = points[i].Measure;

            this.zValues[i] = points[i].Z;

            if (this.minMeasure > points[i].Measure)
            {
                this.minMeasure = points[i].Measure;
            }

            if (this.maxMeasure < points[i].Measure)
            {
                this.maxMeasure = points[i].Measure;
            }

            if (this.minZ > points[i].Z)
            {
                this.minZ = points[i].Z;
            }

            if (this.minZ < points[i].Z)
            {
                this.minZ = points[i].Z;
            }
        }
    }

    #region IShape Members

    public byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriMultiPointZM), 0, ShapeConstants.IntegerSize);

        result.Write(Writer.ShpWriter.WriteBoundingBoxToByte(this), 0, 4 * ShapeConstants.DoubleSize);

        result.Write(System.BitConverter.GetBytes(this.NumberOfPoints), 0, ShapeConstants.IntegerSize);

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
        get { return 20 + 8 * NumberOfPoints + 2 * (8 + 4 * NumberOfPoints); }
    }

    public EsriShapeType Type
    {
        get { return EsriShapeType.EsriMultiPointZM; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="partNo">this parameter will be ignored</param>
    /// <returns></returns>
    public EsriPoint[] GetPart(int partNo)
    {
        return this.Points;
    }


    public BoundingBox MinimumBoundingBox
    {
        get { return boundingBox; }
    }

    public string AsSqlServerWkt()
    {
        //StringBuilder result = new StringBuilder("MULTIPOINT(");

        //for (int i = 0; i < NumberOfPoints - 1; i++)
        //{
        //    result.Append(string.Format(" {0} {1} {2} {3},", this.Points[i].X, this.Points[i].Y, this.ZValues[i], this.Measures[i] == ShapeConstants.NoDataValue ? "NULL" : this.Measures[i].ToString()));
        //}

        //result.Append(string.Format(" {0} {1} {2} {3})",
        //                                this.Points[NumberOfPoints - 1].X,
        //                                this.Points[NumberOfPoints - 1].Y,
        //                                this.ZValues[NumberOfPoints - 1],
        //                                this.Measures[NumberOfPoints - 1] == ShapeConstants.NoDataValue ? "NULL" : this.Measures[NumberOfPoints - 1].ToString()));

        //return result.ToString();
        return string.Format("MULTIPOINT{0}", SqlServerWktHelper.PointZGroupElementToWkt(this.Points, this.ZValues, this.Measures));
    }

    public byte[] AsWkb()
    {
        //byte[] result = new byte[1 + 4 + 4 + 16 * this.NumberOfPoints];
        List<byte> result = new List<byte>
        {
            (byte)WkbByteOrder.WkbNdr
        };

        result.AddRange(BitConverter.GetBytes((int)WkbGeometryType.MultiPoint));

        result.AddRange(BitConverter.GetBytes((UInt32)this.NumberOfPoints));

        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            result.AddRange(OgcWkbMapFunctions.ToWkbPoint(this.points[i]));
        }

        return result.ToArray();
    }

    /// <summary>
    /// Returs Kml representation of the point. Note: Point must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    public IRI.Maptor.Ket.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectFunc = null, byte[] color = null)
    {
        throw new NotImplementedException();
    }

    public string AsKml(Func<Point, Point> projectToGeodeticFunc = null)
    {
        return OgcKmlMapFunctions.AsKml(this.AsPlacemark(projectToGeodeticFunc));
    }

    public IEsriShape Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        return new EsriMultiPointZ(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray(), this.ZValues, this.Measures);
    }

    public Geometry<Point> AsGeometry()
    {
        return new Geometry<Point>(points.Select(p => new Point(p.X, p.Y)).ToList(), GeometryType.MultiPoint, Srid);
    }

    public bool IsNullOrEmpty()
    {
        return Points == null || Points.Length < 1;
    }

    public bool IsRingBase() => false;

    #endregion
}
