// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Linq;

using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Sta.Common.Abstrations;

namespace IRI.Maptor.Sta.ShapefileFormat.EsriType;

public struct EsriMultiPoint : IEsriSimplePoints
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

    public int NumberOfParts
    {
        get { return this.Parts.Length; }
    }

    public EsriPoint[] Points
    {
        get { return this.points; }
    }

    public int[] Parts
    {
        get { return new int[] { 0 }; }
    }

    public EsriMultiPoint(EsriPoint[] points)
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

    }

    internal EsriMultiPoint(BoundingBox boundingBox, EsriPoint[] points)
    {
        if (points == null || points.Length < 1)
        {
            throw new NotImplementedException();
        }

        this.boundingBox = boundingBox;

        this.points = points;

        this.Srid = points.First().Srid;
    }

    public byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriMultiPoint), 0, ShapeConstants.IntegerSize);

        result.Write(Writer.ShpWriter.WriteBoundingBoxToByte(this), 0, 4 * ShapeConstants.DoubleSize);

        result.Write(System.BitConverter.GetBytes(this.NumberOfPoints), 0, ShapeConstants.IntegerSize);

        byte[] tempPoints = Writer.ShpWriter.WritePointsToByte(this.points);

        result.Write(tempPoints, 0, tempPoints.Length);

        return result.ToArray();
    }

    public int ContentLength
    {
        get { return 20 + 8 * NumberOfPoints; }
    }

    public EsriShapeType Type
    {
        get { return EsriShapeType.EsriMultiPoint; }
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
        return string.Format(
            "MULTIPOINT({0})",
            string.Join(",", this.points.Select(i => string.Format("({0})", SqlServerWktHelper.SinglePointElementToWkt(i))).ToArray()));
    }

    public byte[] AsWkb()
    {
        return OgcWkbMapFunctions.ToWkbMultiPoint(this.points); 
    }

    /// <summary>
    /// Returns Kml representation of the point. Note: Point must be in Lat/Long System
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

    public IEsriShape Transform(Func<IPoint, IPoint> transform, int newSrid) /*where TPoint : IPoint, new()*/
    {
        return new EsriMultiPoint(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray());
    }

    public Geometry<Point> AsGeometry()
    {
        return new Geometry<Point>(points.Select(p => p.AsGeometry()).ToList(), GeometryType.MultiPoint, Srid);
    }

    public bool IsNullOrEmpty()
    {
        return Points == null || Points.Length < 1;
    }

    public bool IsRingBase() => false;

}
