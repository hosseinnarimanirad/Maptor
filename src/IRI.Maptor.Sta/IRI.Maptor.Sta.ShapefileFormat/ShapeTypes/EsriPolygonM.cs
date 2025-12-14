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

public class EsriPolygonM : EsriPointMCollection
{
    //public int Srid { get; set; }

    public override EsriShapeType EsriType => EsriShapeType.EsriPolygonM;

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

    public EsriPolygonM() : this(Array.Empty<EsriPoint>(), Array.Empty<int>(), Array.Empty<double>()) { }

    public EsriPolygonM(EsriPoint[] points, int[] parts, double[] measures)
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

        this.boundingBox = BoundingBox.CalculateBoundingBox(points);

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

    internal EsriPolygonM(BoundingBox boundingBox, int[] parts, EsriPoint[] points, double minMeasure, double maxMeasure, double[] measures)
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

    public override bool IsRingBase() => true;

    //public bool IsNullOrEmpty() => Points == null || Points.Length < 1;

    public override byte[] WriteContentsToByte()
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes((int)EsriShapeType.EsriPolygonM), 0, ShapeConstants.IntegerSize);

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
    //    StringBuilder result = new StringBuilder("POLYGON(");

    //    for (int i = 0; i < NumberOfParts; i++)
    //    {
    //        result.Append(
    //            string.Format("{0},",
    //            SqlServerWktHelper.PointMGroupElementToWkt(
    //                ShapeHelper.GetEsriPoints(this, this.Parts[i]),
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

    //    result.AddRange(BitConverter.GetBytes((uint)WkbGeometryType.PolygonM));

    //    result.AddRange(BitConverter.GetBytes((uint)this.parts.Length));

    //    for (int i = 0; i < this.parts.Length; i++)
    //    {
    //        result.AddRange(OgcWkbMapFunctions.ToWkbLinearRingM(ShapeHelper.GetEsriPoints(this, this.Parts[i]), ShapeHelper.GetMeasures(this, this.Parts[i])));
    //    }

    //    return result.ToArray();
    //}

    /// <summary>
    /// Returs Kml representation of the polygon. Note: M values are ignored. Polygon must be in Lat/Long System
    /// </summary>
    /// <returns></returns>
    public override IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType AsPlacemark(Func<Point, Point> projectFunc = null, byte[] color = null)
    {
        IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType placemark = new IRI.Maptor.Sta.KmlFormat.Primitives.PlacemarkType();

        if (this.NumberOfParts == 0 || this.NumberOfPoints == 0)
        {
            return placemark;
        }

        // For single part polygon, create a simple PolygonType
        if (this.NumberOfParts == 1)
        {
            IRI.Maptor.Sta.KmlFormat.Primitives.PolygonType polygon = new IRI.Maptor.Sta.KmlFormat.Primitives.PolygonType();
            
            IRI.Maptor.Sta.KmlFormat.Primitives.BoundaryType outerBoundary = new IRI.Maptor.Sta.KmlFormat.Primitives.BoundaryType();
            IRI.Maptor.Sta.KmlFormat.Primitives.LinearRingType outerRing = new IRI.Maptor.Sta.KmlFormat.Primitives.LinearRingType();
            
            var points = ShapeHelper.GetPoints(this, this.Parts[0]);
            string coordinates;
            
            if (projectFunc != null)
            {
                coordinates = string.Join(" ", points.Select(p =>
                {
                    var projected = projectFunc(p);
                    return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17},{1:G17}", projected.X, projected.Y);
                }));
            }
            else
            {
                coordinates = string.Join(" ", points.Select(p =>
                    string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17},{1:G17}", p.X, p.Y)));
            }
            
            // Close the ring by adding the first point at the end
            var firstPoint = points[0];
            Point firstPointProjected = projectFunc != null ? projectFunc(firstPoint) : firstPoint;
            coordinates += " " + string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17},{1:G17}", firstPointProjected.X, firstPointProjected.Y);
            
            outerRing.Coordinates.Add(coordinates);
            outerBoundary.LinearRing = outerRing;
            polygon.OuterBoundaryIs = outerBoundary;
            
            placemark.AbstractGeometryGroup = polygon;
        }
        else
        {
            // Multiple parts: first part is outer boundary, rest are inner boundaries
            IRI.Maptor.Sta.KmlFormat.Primitives.PolygonType polygon = new IRI.Maptor.Sta.KmlFormat.Primitives.PolygonType();
            
            // Outer boundary
            IRI.Maptor.Sta.KmlFormat.Primitives.BoundaryType outerBoundary = new IRI.Maptor.Sta.KmlFormat.Primitives.BoundaryType();
            IRI.Maptor.Sta.KmlFormat.Primitives.LinearRingType outerRing = new IRI.Maptor.Sta.KmlFormat.Primitives.LinearRingType();
            
            var outerPoints = ShapeHelper.GetPoints(this, this.Parts[0]);
            string outerCoordinates;
            
            if (projectFunc != null)
            {
                outerCoordinates = string.Join(" ", outerPoints.Select(p =>
                {
                    var projected = projectFunc(p);
                    return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17},{1:G17}", projected.X, projected.Y);
                }));
            }
            else
            {
                outerCoordinates = string.Join(" ", outerPoints.Select(p =>
                    string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17},{1:G17}", p.X, p.Y)));
            }
            
            // Close the ring
            var firstOuterPoint = outerPoints[0];
            Point firstOuterProjected = projectFunc != null ? projectFunc(firstOuterPoint) : firstOuterPoint;
            outerCoordinates += " " + string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17},{1:G17}", firstOuterProjected.X, firstOuterProjected.Y);
            
            outerRing.Coordinates.Add(outerCoordinates);
            outerBoundary.LinearRing = outerRing;
            polygon.OuterBoundaryIs = outerBoundary;
            
            // Inner boundaries (holes)
            for (int i = 1; i < this.NumberOfParts; i++)
            {
                IRI.Maptor.Sta.KmlFormat.Primitives.BoundaryType innerBoundary = new IRI.Maptor.Sta.KmlFormat.Primitives.BoundaryType();
                IRI.Maptor.Sta.KmlFormat.Primitives.LinearRingType innerRing = new IRI.Maptor.Sta.KmlFormat.Primitives.LinearRingType();
                
                var innerPoints = ShapeHelper.GetPoints(this, this.Parts[i]);
                string innerCoordinates;
                
                if (projectFunc != null)
                {
                    innerCoordinates = string.Join(" ", innerPoints.Select(p =>
                    {
                        var projected = projectFunc(p);
                        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17},{1:G17}", projected.X, projected.Y);
                    }));
                }
                else
                {
                    innerCoordinates = string.Join(" ", innerPoints.Select(p =>
                        string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17},{1:G17}", p.X, p.Y)));
                }
                
                // Close the ring
                var firstInnerPoint = innerPoints[0];
                Point firstInnerProjected = projectFunc != null ? projectFunc(firstInnerPoint) : firstInnerPoint;
                innerCoordinates += " " + string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17},{1:G17}", firstInnerProjected.X, firstInnerProjected.Y);
                
                innerRing.Coordinates.Add(innerCoordinates);
                innerBoundary.LinearRing = innerRing;
                polygon.InnerBoundaryIs.Add(innerBoundary);
            }
            
            placemark.AbstractGeometryGroup = polygon;
        }

        if (color != null)
        {
            IRI.Maptor.Sta.KmlFormat.Primitives.StyleType style = new IRI.Maptor.Sta.KmlFormat.Primitives.StyleType();
            IRI.Maptor.Sta.KmlFormat.Primitives.PolyStyleType polyStyle = new IRI.Maptor.Sta.KmlFormat.Primitives.PolyStyleType();
            polyStyle.Color = color;
            style.PolyStyle = polyStyle;
            placemark.AbstractStyleSelectorGroup.Add(style);
        }

        return placemark;
    }

    //public override string AsKml(Func<Point, Point> projectToGeodeticFunc = null)
    //{
    //    return OgcKmlMapFunctions.AsKml(this.AsPlacemark(projectToGeodeticFunc));
    //}

    public override EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid)
    {
        return new EsriPolygonM(this.Points.Select(i => i.Transform(transform, newSrid)).Cast<EsriPoint>().ToArray(), this.Parts, this.Measures);
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
            return new Geometry<Point>(new List<Geometry<Point>> { new Geometry<Point>(ShapeHelper.GetPoints(this, Parts[0]), GeometryType.LineString, Srid) }, GeometryType.Polygon, Srid);
        }
        else
        {
            return Geometry<Point>.CreateEmpty(GeometryType.Polygon, Srid);
        }
    }
}
