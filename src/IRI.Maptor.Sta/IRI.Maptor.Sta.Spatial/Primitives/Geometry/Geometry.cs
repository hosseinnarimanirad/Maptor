using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Mathematics;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.Spatial.IO.SqlServerNativeBinary;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Spatial.Topology;
using System.Text;

namespace IRI.Maptor.Sta.Spatial.Primitives;

public class Geometry<T> : IGeometry where T : IPoint, new()
{
    static readonly T NullPoint = default;

    private static readonly Geometry<T> _empty = new Geometry<T>();

    public static Geometry<T> Empty { get { return _empty; } }

    public GeometryType Type { get; set; }

    public GeometryCategory Category => Type.GetCategory();

    //public bool IsMultiPartGeometry => Type == GeometryType.MultiPoint ||
    //                                Type == GeometryType.MultiLineString ||
    //                                Type == GeometryType.MultiPolygon;
    public bool IsMultiPartGeometry => Type.IsMultiPartGeometry();


    private List<T>? _points;
    public List<T>? Points
    {
        get { return _points; }
        set
        {
            if (this.Geometries != null && value != null)
            {
                throw new NotImplementedException();
            }

            this._points = value;
        }
    }

    private List<Geometry<T>>? _geometries;
    public List<Geometry<T>>? Geometries
    {
        get { return _geometries; }
        set
        {
            if (this.Points != null && value != null)
                throw new NotImplementedException();

            this._geometries = value;
        }
    }

    public int NumberOfPoints => Points?.Count ?? 0;

    public int NumberOfGeometries => Geometries?.Count ?? 0;

    public int TotalNumberOfPoints => IsLeafGeometry() ? NumberOfPoints : Geometries?.Sum(g => g.TotalNumberOfPoints) ?? 0;

    public int Srid { get; set; }

    public bool HasZ()
    {
        return typeof(IHasZ).IsAssignableFrom(typeof(T));
    }

    public bool HasM()
    {
        return typeof(IHasM).IsAssignableFrom(typeof(T));
    }

    public bool HasGeometry() => this.Geometries?.Count > 0;


    #region Constructors

    private Geometry()
    {
    }

    #endregion


    #region Simple Methods

    public CoordinateDimension GetDimension()
    {
        bool hasZ = this.HasZ();
        bool hasM = this.HasM();

        if (hasZ && hasM)
            return CoordinateDimension.ZM;

        if (hasZ)
            return CoordinateDimension.Z;

        if (hasM)
            return CoordinateDimension.M;

        return CoordinateDimension.TwoD;
    }

    //public List<Point> GetPoints()
    //{
    //    if (Points is null)
    //        return null;

    //    if (this.Points is List<Point> points)
    //    {
    //        return points;
    //    }
    //    else
    //        return this.Points.Select(p => new Point(p.X, p.Y)).ToList();
    //}

    public override string ToString()
    {
        return $"{Type} - #Points: {NumberOfPoints} - #Parts: {Geometries?.Count()}";
    }

    public bool IsRingBase() => this.Type.IsRingBase();
    //{
    //return this.Type == GeometryType.Polygon || this.Type == GeometryType.MultiPolygon || this.Type == GeometryType.CurvePolygon;
    //}

    public bool HasAnyPoint()
    {
        //1399.07.17
        //if (Points != null)
        if (this.IsLeafGeometry())
        {
            return Points?.Count > 0;
        }
        else /*if (Geometries != null)*/
        {
            return Geometries?.Any(g => g?.HasAnyPoint() == true) == true;
        }
        //else
        //{
        //    return false;
        //}
    }

    public bool IsEmpty()
    {
        return (this.Points.IsNullOrEmpty() && this.Geometries.IsNullOrEmpty()) ||
                 this.TotalNumberOfPoints == 0;
    }

    public bool IsValid()
    {
        switch (this.Type)
        {
            case GeometryType.Point:
                return this.Points?.Count == 1;

            case GeometryType.LineString:
                return this.Points?.Count > 1;

            case GeometryType.Polygon:
                return this.Geometries?.Count > 0 && this.Geometries?.All(g => g?.Points?.Count >= 3) == true;

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return this.Geometries?.All(g => g.IsValid()) == true;

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }
    }

    //public bool IsNotValidOrEmpty()
    //{
    //    return this.IsNullOrEmpty() || !IsValid();
    //}

    public bool IsNonEmptyLeafGeometry()
    {
        return IsLeafGeometry() && this.Points != null && this.Points.Count > 0;
    }

    public bool IsLeafGeometry()
    {
        switch (Type)
        {
            case GeometryType.Point:
            case GeometryType.LineString:
                return true;

            case GeometryType.Polygon:
            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return false;

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                return false;
        }
    }

    public BoundingBox GetBoundingBox() => BoundingBox.CalculateBoundingBox(GetAllPoints());

    public bool IsPointOrMultiPoint() => this.Type.GetCategory() == GeometryCategory.Point;
    //{
    //    return Type == GeometryType.Point || Type == GeometryType.MultiPoint;
    //}

    public bool IsLineStringOrMultiLineString() => this.Type.GetCategory() == GeometryCategory.Polyline;
    //{
    //    return Type == GeometryType.LineString || Type == GeometryType.MultiLineString;
    //}

    public bool IsPolygonOrMultiPolygon() => this.Type.GetCategory() == GeometryCategory.Polygon;
    //{
    //    return Type == GeometryType.Polygon || Type == GeometryType.MultiPolygon;
    //}

    public List<IGeometry>? GetGeometries() => Geometries?.Cast<IGeometry>().ToList();

    public int ToGlobalPointIndex(PointAddress pointAddress)
    {
        var partIndex = pointAddress.PartIndex;

        var polygonIndex = pointAddress.PolygonIndex;

        switch (Type)
        {
            case GeometryType.Point:
                return 0;

            case GeometryType.LineString:
                return pointAddress.LocalPointIndex;

            case GeometryType.MultiPoint:
                return pointAddress.PartIndex;

            case GeometryType.Polygon:
            case GeometryType.MultiLineString:
                var preceedingPartsPoints = Geometries?.Where((g, index) => index < partIndex).Select(g => g.TotalNumberOfPoints).DefaultIfEmpty(0).Sum() ?? 0;
                return pointAddress.LocalPointIndex + preceedingPartsPoints;

            case GeometryType.MultiPolygon:
                var preceedingPolygonPoints = Geometries?.Where((g, index) => index < polygonIndex!.Value).Select(g => g.TotalNumberOfPoints).DefaultIfEmpty(0).Sum() ?? 0;
                var preceedingRingsPoints = Geometries[polygonIndex!.Value].Geometries?.Where((g, index) => index < partIndex).Select(g => g.TotalNumberOfPoints).DefaultIfEmpty(0).Sum() ?? 0;
                return pointAddress.LocalPointIndex + preceedingRingsPoints + preceedingPolygonPoints;

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                return 0;
        }

    }

    public PointAddress FindPointAddress(int globalIndex)
    {
        switch (this.Type)
        {
            case GeometryType.Point:
            case GeometryType.LineString:
                return new PointAddress(null, 0, globalIndex);

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.Polygon:
                var tempCount = 0;

                for (int i = 0; i < NumberOfGeometries; i++)
                {
                    var partPointCount = Geometries?[i].NumberOfPoints ?? 0;

                    if (tempCount + partPointCount > globalIndex)
                        return new PointAddress(null, i, globalIndex - tempCount);

                    tempCount += partPointCount;
                }

                break;

            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                var tempCount2 = 0;

                for (int i = 0; i < NumberOfGeometries; i++)
                {
                    for (int j = 0; j < Geometries?[i].NumberOfGeometries; j++)
                    {
                        var partPointCount = Geometries?[i].Geometries?[j].NumberOfPoints ?? 0;

                        if (tempCount2 + partPointCount > globalIndex)
                            return new PointAddress(i, j, globalIndex - tempCount2);

                        tempCount2 += partPointCount;
                    }
                }

                break;

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                break;
        }

        return new PointAddress(null, -1, -1);
    }

    #endregion


    #region Find

    public bool TryFindPoint(T point, out int pointIndex, out int geometryIndex, out int subGeometryIndex)
    {
        pointIndex = -1;

        geometryIndex = -1;

        subGeometryIndex = -1;

        if (this.Type == GeometryType.GeometryCollection)
        {
            throw new NotImplementedException();
        }

        //LineString, Point cases
        if (this.Points != null)
        {
            return TryFind(this.Points, point, out pointIndex);
        }
        else if (this.Geometries != null)
        {
            for (int g = 0; g < this.Geometries.Count; g++)
            {
                //MultiPoint, MultiLineString, Polygon cases
                if (this.Geometries[g].Points != null)
                {
                    if (TryFind(this.Geometries[g].Points, point, out pointIndex))
                    {
                        geometryIndex = g;

                        return true;
                    }
                }
                //MultiPolygon case
                else if (this.Geometries[g].Geometries != null)
                {
                    for (int subG = 0; subG < this.Geometries[g].Geometries.Count; subG++)
                    {
                        if (TryFind(this.Geometries[g].Geometries[subG].Points, point, out pointIndex))
                        {
                            geometryIndex = g;

                            subGeometryIndex = subG;

                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private bool TryFind(List<T> points, T point, out int pointIndex)
    {
        pointIndex = -1;

        if (points == null)
            return false;

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i].Equals(point))
            {
                pointIndex = i;

                return true;
            }
        }

        return false; ;
    }

    private bool TryFind(List<T> points, double x, double y, out int pointIndex)
    {
        pointIndex = -1;

        if (points == null)
            return false;

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i].X == x && points[i].Y == y)
            {
                pointIndex = i;

                return true;
            }
        }

        return false; ;
    }

    #endregion


    #region Analysis

    public T? GetMeanPoint()
    {
        var allPoints = GetAllPoints();

        if (allPoints.IsNullOrEmpty())
            return default;

        return new T() { X = allPoints.Sum(i => i.X) / allPoints.Count, Y = allPoints.Sum(i => i.Y) / allPoints.Count };
    }

    public Geometry<T> GetCentroidPlus()
    {
        if (this.IsNullOrEmpty())
            return Geometry<T>.Empty;

        var centroidPoint = GetMeanPoint();

        if (this.IsPolygonOrMultiPolygon() && !Contains(centroidPoint))
        {
            centroidPoint = this.GetLastPoint();
        }

        return Geometry<T>.CreatePointOrLineString(new List<T>() { centroidPoint }, this.Srid);
    }

    public T GetCentroidPlusPoint() => GetCentroidPlus().AsPoint();

    public bool Contains(T point)
    {
        switch (this.Type)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
            case GeometryType.LineString:
            case GeometryType.MultiLineString:
            case GeometryType.Polygon:
            case GeometryType.MultiPolygon:
                return IntersectsPoint(point);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry > Contains");
        }
    }


    public T GetNearestPoint(IPoint point)
    {
        if (this.IsLeafGeometry())
        {
            var minDistance = double.MaxValue;

            T result = new T();

            for (int i = 0; i < this.Points.Count; i++)
            {
                var distance = SpatialUtility.GetEuclideanLength(this.Points[i], point);

                if (minDistance > distance)
                {
                    result = Points[i];

                    minDistance = distance;
                }
            }

            return result;
        }
        else
        {
            // Collect the nearest point from each sub‑geometry,
            // then pick the one closest to the input point.
            return this.Geometries
                .Select(g => g.GetNearestPoint(point))
                .OrderBy(candidate => SpatialUtility.GetEuclideanLength(candidate, point))
                .First();
        }
    }

    /// <summary>
    /// Does not filter point and multipoint features
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public Geometry<T> FilterPoints(Func<List<T>, List<T>> filter)
    {
        switch (this.Type)
        {
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
                return Geometry<T>.Empty;

            case GeometryType.GeometryCollection:
                return Geometry<T>.Create(this.Geometries.Select(i => i.FilterPoints(filter)).ToList(), GeometryType.GeometryCollection, this.Srid);

            case GeometryType.MultiPoint:
                return Geometry<T>.Create(this.Geometries, GeometryType.MultiPoint, this.Srid);

            case GeometryType.Point:
                return Geometry<T>.Create(this.Points, GeometryType.Point, this.Srid);

            case GeometryType.LineString:
                //todo: multiple cast consider changing it
                //return CreatePointOrLineString<T>(filter(this.Points.Select(i => (T)i).ToList()).Select(i => (IPoint)i).ToList(), this.Srid);
                return CreatePointOrLineString(filter(this.Points), this.Srid);

            case GeometryType.MultiLineString:
                return Geometry<T>.Create(this.Geometries.Select(i => i.FilterPoints(filter)).ToList(), GeometryType.MultiLineString, this.Srid);

            case GeometryType.Polygon:
                return Geometry<T>.Create(this.Geometries.Select(i => i.FilterPoints(filter)).ToList(), GeometryType.Polygon, this.Srid);

            case GeometryType.MultiPolygon:
                return Geometry<T>.Create(this.Geometries.Select(i => i.FilterPoints(filter)).ToList(), GeometryType.MultiPolygon, this.Srid);

            default:
                throw new NotImplementedException();
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="threshold"></param>
    /// <param name="type"></param>
    /// <param name="secondaryParameter">may be area threshold for `AdditiveByAreaAngle` or look ahead parameter for `Lang`</param>
    /// <returns></returns>
    public Geometry<T> Simplify(SimplificationType type, SimplificationParameters parameter)
    {
        Func<List<T>, List<T>> filter;

        switch (type)
        {
            case SimplificationType.NthPoint:
                filter = pList => Simplifications.SimplifyByNthPoint(pList, parameter);
                break;

            case SimplificationType.RandomPointSelection:
                filter = pList => Simplifications.SimplifyByRandomPointSelection(pList, parameter);
                break;

            case SimplificationType.EuclideanDistance:
                filter = pList => Simplifications.SimplifyByEuclideanDistance(pList, parameter);
                break;

            case SimplificationType.TriangleRoutine:
                filter = pList => Simplifications.SimplifyByTriangleRoutine(pList, parameter);
                break;

            case SimplificationType.CumulativeTriangleRoutine:
                filter = pList => Simplifications.SimplifyByCumulativeTriangleRoutine(pList, parameter);
                break;

            case SimplificationType.ModifiedTriangleRoutine:
                filter = pList => Simplifications.SimplifyByModifiedTriangleRoutine(pList, parameter);
                break;

            case SimplificationType.Angle:
                filter = pList => Simplifications.SimplifyByAngle(pList, parameter);
                break;

            case SimplificationType.CumulativeAngle:
                filter = pList => Simplifications.SimplifyByCumulativeAngle(pList, parameter);
                break;

            case SimplificationType.CumulativeEuclideanDistance:
                filter = pList => Simplifications.SimplifyByCumulativeEuclideanDistance(pList, parameter);
                break;

            case SimplificationType.VisvalingamWhyatt:
                filter = pList => Simplifications.SimplifyByVisvalingamWhyatt(pList, parameter, this.IsRingBase());
                break;

            case SimplificationType.RamerDouglasPeucker:
                filter = pList => Simplifications.SimplifyByRamerDouglasPeucker(pList, parameter);
                break;

            case SimplificationType.Lang:
                filter = pList => Simplifications.SimplifyByLang(pList, parameter);
                break;

            case SimplificationType.ReumannWitkam:
                filter = pList => Simplifications.SimplifyByReumannWitkam(pList, parameter);
                break;

            case SimplificationType.SleeveFitting:
                filter = pList => Simplifications.SimplifyBySleeveFitting(pList, parameter);
                break;

            case SimplificationType.PerpendicularDistance:
                filter = pList => Simplifications.SimplifyByPerpendicularDistance(pList, parameter);
                break;

            case SimplificationType.ModifiedPerpendicularDistance:
                filter = pList => Simplifications.SimplifyByModifiedPerpendicularDistance(pList, parameter);
                break;

            case SimplificationType.NormalOpeningWindow:
                filter = pList => Simplifications.SimplifyByNormalOpeningWindow(pList, parameter);
                break;

            case SimplificationType.BeforeOpeningWindow:
                filter = pList => Simplifications.SimplifyByBeforeOpeningWindow(pList, parameter);
                break;


            case SimplificationType.AdditiveAreaPlus:
                filter = pList => Simplifications.SimplifyByAdditiveAreaPlus(pList, parameter);
                break;

            case SimplificationType.CumulativeAreaAngle:
                filter = pList => Simplifications.SimplifyByCumulativeAngleArea(pList, parameter);
                break;

            case SimplificationType.APSC:
                filter = pList => Simplifications.SimplifyByAPSC(pList, parameter);
                break;

            default:
                throw new NotImplementedException();
        }

        return this.FilterPoints(filter);
    }

    public Geometry<T> Simplify(SimplificationType type, int zoomLevel, SimplificationParameters parameter)
    {
        var threshold = WebMercatorUtility.CalculateGroundResolution(zoomLevel, parameter.AverageLatitude ?? 0); //0 seconds!

        parameter.AreaThreshold = threshold * threshold;

        parameter.DistanceThreshold = threshold;

        return Simplify(type, parameter);
    }

    public Geometry<T> Transform(Func<T, T> transform, int newSrid = 0)
    {
        switch (this.Type)
        {
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            case GeometryType.GeometryCollection:
                System.Diagnostics.Debug.WriteLine($"****WARNNING: Geometry.cs -> Filter method invalid geometry type");
                return Geometry<T>.Empty;

            case GeometryType.MultiPoint:
                return Geometry<T>.Create(this.Geometries.Select(i => i.Transform(transform, newSrid)).ToList(), GeometryType.MultiPoint, newSrid);

            case GeometryType.Point:
                return Geometry<T>.Create(this.Points.Select(i => transform(i)).ToList(), GeometryType.Point, newSrid);

            case GeometryType.LineString:
                return Geometry<T>.Create(this.Points.Select(i => transform(i)).ToList(), GeometryType.LineString, newSrid);

            case GeometryType.MultiLineString:
                return Geometry<T>.Create(this.Geometries.Select(i => i.Transform(transform, newSrid)).ToList(), GeometryType.MultiLineString, newSrid);

            case GeometryType.MultiPolygon:
                return Geometry<T>.Create(this.Geometries.Select(i => i.Transform(transform, newSrid)).ToList(), GeometryType.MultiPolygon, newSrid);

            case GeometryType.Polygon:
                return Geometry<T>.Create(this.Geometries.Select(i => i.Transform(transform, newSrid)).ToList(), GeometryType.Polygon, newSrid);

            default:
                throw new NotImplementedException();
        }
    }

    public bool HasTheSameSignature(Geometry<T> other)
    {
        if (other == null)
            return false;

        if (other.Type != this.Type)
            return false;

        switch (this.Type)
        {
            case GeometryType.Point:
            case GeometryType.LineString:
                return true;

            case GeometryType.MultiPoint:
            case GeometryType.Polygon:
            case GeometryType.MultiLineString:
                return this.NumberOfGeometries == other.NumberOfGeometries;

            case GeometryType.MultiPolygon:
                if (this.NumberOfGeometries != other.NumberOfGeometries)
                    return false;
                else if (this.NumberOfGeometries == 0)
                    return true;
                else
                    return this.Geometries.Zip(other.Geometries, (g1, g2) => g1.HasTheSameSignature(g2)).All(f => f);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry > HasTheSameSignature");
        }
    }

    // 1401.11.15
    // Intersects
    public bool Intersects(Geometry<T> other)
    {
        if (this.IsNullOrEmpty() || other.IsNullOrEmpty())
            return false;

        if (this.IsNotValidOrEmpty() || other.IsNotValidOrEmpty())
            return false;

        if (this.Srid != other.Srid)
            return false;

        // expand by the epsilon tolerance so that near-touching geometries
        // (e.g. two points closer than EpsilonDistance) are not rejected by the gate
        var firstMbb = GetEpsilonExpanded(this.GetBoundingBox());

        var secondMbb = other.GetBoundingBox();

        if (!firstMbb.Intersects(secondMbb))
            return false;

        switch (other.Type)
        {
            case GeometryType.Point:
                return this.IntersectsPoint(other.Points[0]);

            case GeometryType.LineString:
                return this.IntersectsLineStringOrRing(other, isRing: false);

            case GeometryType.Polygon:
                return this.IntersectsPolygon(other);

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return other.Geometries.Any(Intersects);

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry > Intersects");
        }

    }

    /// <summary>
    /// SQL Server style alias for <see cref="Intersects(Geometry{T})"/> (OGC STIntersects):
    /// returns true when the two geometries share at least one point, including boundary touches.
    /// </summary>
    public bool STIntersects(Geometry<T> other) => Intersects(other);

    private static BoundingBox GetEpsilonExpanded(BoundingBox boundingBox)
    {
        return new BoundingBox(
            boundingBox.XMin - SpatialUtility.EpsilonDistance,
            boundingBox.YMin - SpatialUtility.EpsilonDistance,
            boundingBox.XMax + SpatialUtility.EpsilonDistance,
            boundingBox.YMax + SpatialUtility.EpsilonDistance);
    }

    private bool IntersectsPoint(T point)
    {
        if (point is null)
            return false;

        if (!GetEpsilonExpanded(this.GetBoundingBox()).Intersects(point))
            return false;

        switch (this.Type)
        {
            case GeometryType.Point:
                return SpatialUtility.GetEuclideanLength(this.AsPoint(), point) < SpatialUtility.EpsilonDistance;

            case GeometryType.LineString:
                return TopologyUtility.IsPointOnLineString(this, point);

            case GeometryType.Polygon:
                return TopologyUtility.IsPointInPolygonOrOnBoundary(this, point);

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return this.Geometries.Any(g => g.IntersectsPoint(point));

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry > IntersectsPoint");
        }
    }

    private bool IntersectsLineSegment(T startSegment, T endSegment)
    {
        if (startSegment is null || endSegment is null)
            return false;

        switch (this.Type)
        {
            case GeometryType.Point:
                return TopologyUtility.PointIntersectsLineSegment(this.AsPoint(), startSegment, endSegment);

            case GeometryType.LineString:
                return TopologyUtility.LineSegmentIntersectsLineStringOrRing(this, startSegment, endSegment, isRing: false);

            case GeometryType.Polygon:
                return this.Geometries.Any(g => TopologyUtility.LineSegmentIntersectsLineStringOrRing(g, startSegment, endSegment, isRing: true));

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return this.Geometries.Any(g => g.IntersectsLineSegment(startSegment, endSegment));

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry > IntersectsLineSegment");
        }
    }

    private bool IntersectsLineStringOrRing(Geometry<T> lineString, bool isRing)
    {
        if (lineString.IsNullOrEmpty()) return false;

        switch (this.Type)
        {
            case GeometryType.Point:
                return TopologyUtility.IsPointOnLineString(lineString, this.Points[0]);

            case GeometryType.LineString:
            case GeometryType.Polygon:

                if (this.Type == GeometryType.Polygon)
                {
                    if (TopologyUtility.IsPointInPolygon(this, lineString.Points[0]))
                        return true;
                }

                for (int i = 0; i < lineString.NumberOfPoints - 1; i++)
                {
                    if (IntersectsLineSegment(lineString.Points[i], lineString.Points[i + 1]))
                        return true;
                }

                if (isRing)
                {
                    if (IntersectsLineSegment(lineString.Points[0], lineString.Points[lineString.NumberOfPoints - 1]))
                        return true;
                }

                return false;

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return this.Geometries.Any(g => g.IntersectsLineStringOrRing(lineString, isRing));

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry > IntersectsLineStringOrRing");
        }

    }

    private bool IntersectsPolygon(Geometry<T> polygon)
    {
        if (polygon is null)
            return false;

        switch (this.Type)
        {
            case GeometryType.Point:
                return TopologyUtility.IsPointInPolygonOrOnBoundary(polygon, this.Points[0]);

            case GeometryType.LineString:
                //return polygon.Geometries.Any(g => g.IntersectsLineStringOrRing(this, isRing: false));
                return polygon.IntersectsLineStringOrRing(this, isRing: false);

            case GeometryType.Polygon:
                if (TopologyUtility.IsPointInPolygon(this, polygon.GetLastPoint()) ||
                    TopologyUtility.IsPointInPolygon(polygon, this.GetLastPoint()))
                    return true;

                //case GeometryType.Polygon:
                //    if (TopologyUtility.IsPointInPolygon(this, polygon.GetLastPoint()))
                //        return true;

                ////else if (TopologyUtility.IsPointInPolygon(polygon, this.GetLastPoint()))
                ////    return true;


                return this.Geometries.Any(g => polygon.IntersectsLineStringOrRing(g, isRing: true));

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return this.Geometries.Any(g => g.IntersectsPolygon(polygon));

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry > IntersectsPolygon");
        }
    }

    private bool IntersectsPolygon(BoundingBox boundingBox)
    {
        // check if polygon is inside boundingBox
        if (boundingBox.Covers(this.GetLastPoint()))
            return true;

        // check if bounding box is inside polygon
        if (TopologyUtility.IsPointInPolygon<T>(this, new T() { X = boundingBox.XMin, Y = boundingBox.YMin }))
            return true;

        var boundingBoxGeometry = boundingBox.AsGeometry<T>(this.Srid);

        return this.Geometries.Any(g => boundingBoxGeometry.IntersectsLineStringOrRing(g, isRing: true));
    }


    public bool Intersects(BoundingBox boundingBox)
    {
        if (this.IsNullOrEmpty())
            return false;

        switch (this.Type)
        {
            case GeometryType.Point:
                return boundingBox.Covers(this.Points[0]);

            case GeometryType.LineString:
                if (this.Points.Any(boundingBox.Covers))
                    return true;

                var currentBoundingBox = this.GetBoundingBox();

                if (!currentBoundingBox.Intersects(boundingBox))
                    return false;

                if (GetLineSegments().Any(s => boundingBox.IntersectsLineSegment(s.Start, s.End)))
                    return true;

                return false;

            case GeometryType.Polygon:
                return IntersectsPolygon(boundingBox);

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return this.Geometries.Any(g => g.Intersects(boundingBox));

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                // consider handling this
                //return false;
                throw new NotImplementedException("Geometry > Intersects");
        }
    }

    // 1402.01.17
    // may reside on the boundary of BoundingBox 
    public bool IsCoveredBy(BoundingBox boundingBox)
    {
        if (this.IsNullOrEmpty())
            return false;

        switch (this.Type)
        {
            case GeometryType.Point:
                return boundingBox.Covers(this.Points[0]);

            case GeometryType.LineString:
                return this.Points.All(boundingBox.Covers);

            case GeometryType.Polygon:

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                return this.Geometries.All(g => g.IsCoveredBy(boundingBox));

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry > Intersects");
        }
    }

    // 1402.01.17
    // cannot reside on the boundary of BoundingBox 
    public bool IsInside(BoundingBox boundingBox)
    {
        if (this.IsNullOrEmpty())
            return false;

        switch (this.Type)
        {
            case GeometryType.Point:
                return boundingBox.Contains(this.Points[0]);

            case GeometryType.LineString:
                return this.Points.All(boundingBox.Contains);

            case GeometryType.Polygon:

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                return this.Geometries.All(g => g.IsInside(boundingBox));

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry > Intersects");
        }
    }

    #endregion


    #region Union

    /// <summary>
    /// Computes the union of this geometry with another geometry.
    /// Equivalent to SQL Server's STUnion().
    /// </summary>
    /// <remarks>
    /// Overlapping polygons are merged (Greiner–Hormann); polygons that only touch stay separate
    /// MultiPolygon members. Polygon overlaps with degenerate boundaries (shared edges while the
    /// interiors overlap) throw <see cref="NotImplementedException"/> instead of silently
    /// returning a wrong result. Points and lines covered by a higher-dimensional operand are
    /// absorbed. Partially overlapping collinear line work is not dissolved — both lines are kept
    /// (coverage-correct, double-covered representation).
    /// </remarks>
    /// <param name="other">The geometry to union with</param>
    /// <returns>The union of the two geometries</returns>
    public Geometry<T> Union(Geometry<T> other)
    {
        if (this.IsNullOrEmpty())
            return other ?? Geometry<T>.Empty;

        if (other.IsNullOrEmpty())
            return this;

        if (this.Srid != other.Srid)
            throw new ArgumentException("SRIDs must match for Union operation");

        var points = new List<Geometry<T>>();
        var lineStrings = new List<Geometry<T>>();
        var polygons = new List<Geometry<T>>();

        FlattenIntoParts(this.Clone(), points, lineStrings, polygons);
        FlattenIntoParts(other.Clone(), points, lineStrings, polygons);

        return CombineUnionPieces(points, lineStrings, polygons, this.Srid);
    }

    /// <summary>
    /// SQL Server style alias for <see cref="Union(Geometry{T})"/> (OGC STUnion).
    /// </summary>
    public Geometry<T> STUnion(Geometry<T> other) => Union(other);

    private static Geometry<T> CombineUnionPieces(List<Geometry<T>> points, List<Geometry<T>> lineStrings, List<Geometry<T>> polygons, int srid)
    {
        // merge overlapping polygons into a valid polygon set
        var mergedPolygons = new List<Geometry<T>>();

        FlattenIntoParts(
            UnionPolygonPieces(polygons, srid, throwOnDegenerateOverlap: true),
            new List<Geometry<T>>(),
            new List<Geometry<T>>(),
            mergedPolygons);

        // drop duplicate lines and lines covered by a polygon
        var uniqueLineStrings = new List<Geometry<T>>();

        foreach (var lineString in lineStrings)
        {
            if (uniqueLineStrings.Any(l => AreLineStringsAlmostEqual(l, lineString)))
                continue;

            if (mergedPolygons.Any(p => IsLineStringCoveredByPolygon(lineString, p)))
                continue;

            uniqueLineStrings.Add(lineString);
        }

        // drop duplicate points and points covered by a line or polygon
        var uniquePoints = new List<Geometry<T>>();

        foreach (var pointGeometry in points)
        {
            var point = pointGeometry.Points[0];

            if (uniquePoints.Any(p => ArePointsAlmostEqual(p.Points[0], point)))
                continue;

            if (IsPointOnAnyLineString(uniqueLineStrings, point))
                continue;

            if (mergedPolygons.Any(p => TopologyUtility.IsPointInPolygonOrOnBoundary(p, point)))
                continue;

            uniquePoints.Add(pointGeometry);
        }

        return AssembleGeometryParts(uniquePoints, uniqueLineStrings, mergedPolygons, srid);
    }

    /// <summary>
    /// Pointwise epsilon equality of two line strings, in forward or reversed order.
    /// </summary>
    private static bool AreLineStringsAlmostEqual(Geometry<T> line1, Geometry<T> line2)
    {
        var points1 = line1.Points;
        var points2 = line2.Points;

        if (points1.Count != points2.Count)
            return false;

        int count = points1.Count;

        bool forward = true;
        bool backward = true;

        for (int i = 0; i < count && (forward || backward); i++)
        {
            forward = forward && ArePointsAlmostEqual(points1[i], points2[i]);
            backward = backward && ArePointsAlmostEqual(points1[i], points2[count - 1 - i]);
        }

        return forward || backward;
    }

    /// <summary>
    /// Sample-based coverage test: every vertex and segment midpoint of the line lies in or on the polygon.
    /// </summary>
    private static bool IsLineStringCoveredByPolygon(Geometry<T> lineString, Geometry<T> polygon)
    {
        var points = lineString.Points;

        for (int i = 0; i < points.Count; i++)
        {
            if (!TopologyUtility.IsPointInPolygonOrOnBoundary(polygon, points[i]))
                return false;

            if (i < points.Count - 1)
            {
                var midpoint = new T() { X = (points[i].X + points[i + 1].X) / 2.0, Y = (points[i].Y + points[i + 1].Y) / 2.0 };

                if (!TopologyUtility.IsPointInPolygonOrOnBoundary(polygon, midpoint))
                    return false;
            }
        }

        return true;
    }

    #endregion


    #region Intersection

    /// <summary>
    /// Computes the intersection of this geometry with another geometry.
    /// Equivalent to SQL Server's STIntersection(); the boolean counterpart is
    /// <see cref="Intersects(Geometry{T})"/> / <see cref="STIntersects(Geometry{T})"/>.
    /// </summary>
    /// <remarks>
    /// Curve types, polygon-polygon overlaps whose boundaries are degenerate (shared edges or
    /// vertices lying exactly on the other polygon's boundary while the interiors overlap) and
    /// holes overlapping the intersection area are not implemented and throw
    /// <see cref="NotImplementedException"/> instead of silently returning a wrong result.
    /// An empty result is returned as an empty GeometryCollection carrying this geometry's SRID.
    /// </remarks>
    /// <param name="other">The geometry to intersect with</param>
    /// <returns>The intersection of the two geometries</returns>
    public Geometry<T> Intersection(Geometry<T> other)
    {
        if (this.IsNullOrEmpty() || other.IsNullOrEmpty())
            return CreateEmptyIntersectionResult(this.Srid);

        if (this.Srid != other.Srid)
            throw new ArgumentException("SRIDs must match for Intersection operation");

        return IntersectionCore(other);
    }

    /// <summary>
    /// SQL Server style alias for <see cref="Intersection(Geometry{T})"/> (OGC STIntersection).
    /// </summary>
    public Geometry<T> STIntersection(Geometry<T> other) => Intersection(other);

    private Geometry<T> IntersectionCore(Geometry<T> other)
    {
        if (this.IsNullOrEmpty() || other.IsNullOrEmpty())
            return CreateEmptyIntersectionResult(this.Srid);

        if (other.Type == GeometryType.GeometryCollection)
            return CombineIntersectionPieces(other.Geometries.Select(g => this.IntersectionCore(g)).ToList(), this.Srid);

        switch (this.Type)
        {
            case GeometryType.Point:
                return IntersectionPoint(other);

            case GeometryType.LineString:
                return IntersectionLineString(other);

            case GeometryType.Polygon:
                return IntersectionPolygon(other);

            case GeometryType.MultiPoint:
                return IntersectionMultiPoint(other);

            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return CombineIntersectionPieces(this.Geometries.Select(g => g.IntersectionCore(other)).ToList(), this.Srid);

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException($"Intersection not implemented for {this.Type}");
        }
    }

    private Geometry<T> IntersectionPoint(Geometry<T> other)
    {
        var point = this.Points[0];

        bool intersects;

        switch (other.Type)
        {
            case GeometryType.Point:
                intersects = ArePointsAlmostEqual(point, other.Points[0]);
                break;

            case GeometryType.LineString:
                // guard the degenerate single-point line string (IsPointOnLineString throws on it)
                intersects = other.NumberOfPoints == 1
                                ? ArePointsAlmostEqual(point, other.Points[0])
                                : TopologyUtility.IsPointOnLineString(other, point);
                break;

            case GeometryType.Polygon:
                intersects = TopologyUtility.IsPointInPolygonOrOnBoundary(other, point);
                break;

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                intersects = other.Geometries.Any(g => !this.IntersectionPoint(g).IsNullOrEmpty());
                break;

            default:
                throw new NotImplementedException($"Intersection not implemented for {other.Type}");
        }

        return intersects ? this.Clone() : CreateEmptyIntersectionResult(this.Srid);
    }

    private Geometry<T> IntersectionLineString(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return other.IntersectionCore(this);

            case GeometryType.LineString:
                return IntersectionLineStrings(this, other);

            case GeometryType.Polygon:
                return IntersectionLineStringWithPolygon(this, other);

            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                return CombineIntersectionPieces(other.Geometries.Select(g => this.IntersectionLineString(g)).ToList(), this.Srid);

            default:
                throw new NotImplementedException($"Intersection not implemented for {other.Type}");
        }
    }

    private Geometry<T> IntersectionPolygon(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
            case GeometryType.LineString:
            case GeometryType.MultiLineString:
                return other.IntersectionCore(this);

            case GeometryType.Polygon:
                return IntersectionPolygons(this, other);

            case GeometryType.MultiPolygon:
                return CombineIntersectionPieces(other.Geometries.Select(g => this.IntersectionPolygon(g)).ToList(), this.Srid);

            default:
                throw new NotImplementedException($"Intersection not implemented for {other.Type}");
        }
    }

    private Geometry<T> IntersectionMultiPoint(Geometry<T> other)
    {
        var pieces = this.Geometries.Select(pointGeometry => pointGeometry.IntersectionPoint(other)).ToList();

        return CombineIntersectionPieces(pieces, this.Srid);
    }

    private static bool ArePointsAlmostEqual(T first, T second)
        => SpatialUtility.GetEuclideanLength(first, second) < SpatialUtility.EpsilonDistance;

    private static Geometry<T> CreateEmptyIntersectionResult(int srid)
        => Geometry<T>.CreateEmpty(GeometryType.GeometryCollection, srid);

    private static Geometry<T> CreatePointGeometry(T point, int srid)
        => Geometry<T>.Create(new List<T> { point }, GeometryType.Point, srid);

    private static Geometry<T> CreateLineStringGeometry(List<T> points, int srid)
        => Geometry<T>.Create(points, GeometryType.LineString, srid);

    /// <summary>
    /// Parameter of the orthogonal projection of <paramref name="point"/> onto the segment
    /// (0 at <paramref name="start"/>, 1 at <paramref name="end"/>).
    /// </summary>
    private static double GetParameterOnSegment(T start, T end, T point)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;

        double squaredLength = dx * dx + dy * dy;

        if (squaredLength == 0)
            return 0;

        return ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / squaredLength;
    }

    private static T GetPointOnSegment(T start, T end, double parameter)
        => new T() { X = start.X + parameter * (end.X - start.X), Y = start.Y + parameter * (end.Y - start.Y) };

    /// <summary>
    /// Recursively flattens a geometry into its Point/LineString/Polygon parts.
    /// </summary>
    private static void FlattenIntoParts(Geometry<T> geometry, List<Geometry<T>> points, List<Geometry<T>> lineStrings, List<Geometry<T>> polygons)
    {
        if (geometry.IsNullOrEmpty())
            return;

        switch (geometry.Type)
        {
            case GeometryType.Point:
                points.Add(geometry);
                break;

            case GeometryType.LineString:
                lineStrings.Add(geometry);
                break;

            case GeometryType.Polygon:
                polygons.Add(geometry);
                break;

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                foreach (var child in geometry.Geometries)
                    FlattenIntoParts(child, points, lineStrings, polygons);
                break;

            default:
                throw new NotImplementedException("Geometry > FlattenIntoParts");
        }
    }

    /// <summary>
    /// Groups parts into Point/MultiPoint, LineString/MultiLineString, Polygon/MultiPolygon or a
    /// GeometryCollection; an empty part set yields an empty GeometryCollection with the SRID.
    /// </summary>
    private static Geometry<T> AssembleGeometryParts(List<Geometry<T>> points, List<Geometry<T>> lineStrings, List<Geometry<T>> polygons, int srid)
    {
        var parts = new List<Geometry<T>>();

        if (points.Count == 1)
            parts.Add(points[0]);
        else if (points.Count > 1)
            parts.Add(Geometry<T>.Create(points, GeometryType.MultiPoint, srid));

        if (lineStrings.Count == 1)
            parts.Add(lineStrings[0]);
        else if (lineStrings.Count > 1)
            parts.Add(Geometry<T>.Create(lineStrings, GeometryType.MultiLineString, srid));

        if (polygons.Count == 1)
            parts.Add(polygons[0]);
        else if (polygons.Count > 1)
            parts.Add(Geometry<T>.Create(polygons, GeometryType.MultiPolygon, srid));

        if (parts.Count == 0)
            return CreateEmptyIntersectionResult(srid);

        if (parts.Count == 1)
            return parts[0];

        return Geometry<T>.Create(parts, GeometryType.GeometryCollection, srid);
    }

    /// <summary>
    /// Flattens intersection pieces into a single geometry: duplicate points and points already
    /// covered by a line/polygon piece are dropped, then the pieces are grouped into
    /// Point/MultiPoint, LineString/MultiLineString, Polygon/MultiPolygon or a GeometryCollection.
    /// </summary>
    private static Geometry<T> CombineIntersectionPieces(List<Geometry<T>> pieces, int srid)
    {
        var points = new List<Geometry<T>>();
        var lineStrings = new List<Geometry<T>>();
        var polygons = new List<Geometry<T>>();

        foreach (var piece in pieces)
            FlattenIntoParts(piece, points, lineStrings, polygons);

        var uniquePoints = new List<Geometry<T>>();

        foreach (var pointGeometry in points)
        {
            var point = pointGeometry.Points[0];

            if (uniquePoints.Any(p => ArePointsAlmostEqual(p.Points[0], point)))
                continue;

            if (IsPointOnAnyLineString(lineStrings, point))
                continue;

            if (polygons.Any(p => TopologyUtility.IsPointInPolygonOrOnBoundary(p, point)))
                continue;

            uniquePoints.Add(pointGeometry);
        }

        return AssembleGeometryParts(uniquePoints, lineStrings, polygons, srid);
    }

    private static bool IsPointOnAnyLineString(List<Geometry<T>> lineStrings, T point)
    {
        foreach (var lineString in lineStrings)
        {
            for (int i = 0; i < lineString.Points.Count - 1; i++)
            {
                if (TopologyUtility.GetPointToLineSegmentDistance(point, lineString.Points[i], lineString.Points[i + 1]) < SpatialUtility.EpsilonDistance)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Intersects two line strings: crossing points and collinear overlapping sub-segments.
    /// </summary>
    private static Geometry<T> IntersectionLineStrings(Geometry<T> line1, Geometry<T> line2)
    {
        var srid = line1.Srid;

        var pieces = new List<Geometry<T>>();

        for (int i = 0; i < line1.Points.Count - 1; i++)
        {
            var start1 = line1.Points[i];
            var end1 = line1.Points[i + 1];

            if (ArePointsAlmostEqual(start1, end1))
                continue;

            for (int j = 0; j < line2.Points.Count - 1; j++)
            {
                var start2 = line2.Points[j];
                var end2 = line2.Points[j + 1];

                if (ArePointsAlmostEqual(start2, end2))
                    continue;

                var piece = IntersectionLineSegments(start1, end1, start2, end2, srid);

                if (!piece.IsNullOrEmpty())
                    pieces.Add(piece);
            }
        }

        return CombineIntersectionPieces(pieces, srid);
    }

    /// <summary>
    /// Intersects two line segments: a crossing point, the collinear overlapping sub-segment, or empty.
    /// </summary>
    private static Geometry<T> IntersectionLineSegments(T start1, T end1, T start2, T end2, int srid)
    {
        var relation = TopologyUtility.LineSegmentsIntersects(start1, end1, start2, end2, out T intersection);

        if (relation == LineLineSegmentRelation.Intersect)
            return CreatePointGeometry(intersection, srid);

        if (relation != LineLineSegmentRelation.Coinciding)
            return CreateEmptyIntersectionResult(srid);

        // collinear segments: intersect their parameter intervals on the first segment
        var first = GetParameterOnSegment(start1, end1, start2);
        var second = GetParameterOnSegment(start1, end1, end2);

        var overlapStartParameter = Math.Max(0, Math.Min(first, second));
        var overlapEndParameter = Math.Min(1, Math.Max(first, second));

        if (overlapStartParameter > overlapEndParameter)
            return CreateEmptyIntersectionResult(srid);

        var overlapStart = GetPointOnSegment(start1, end1, overlapStartParameter);
        var overlapEnd = GetPointOnSegment(start1, end1, overlapEndParameter);

        if (ArePointsAlmostEqual(overlapStart, overlapEnd))
            return CreatePointGeometry(overlapStart, srid);

        return CreateLineStringGeometry(new List<T> { overlapStart, overlapEnd }, srid);
    }

    /// <summary>
    /// Ring segments including the implicit closing segment (rings are stored without the
    /// repeated closing vertex); zero-length segments are skipped.
    /// </summary>
    private static IEnumerable<(T Start, T End)> GetRingSegments(Geometry<T> ring)
    {
        var points = ring.Points;
        var count = points.Count;

        for (int i = 0; i < count; i++)
        {
            var start = points[i];
            var end = points[(i + 1) % count];

            if (!ArePointsAlmostEqual(start, end))
                yield return (start, end);
        }
    }

    /// <summary>
    /// Clips a line string to a polygon: the contained sub-segments plus isolated boundary touch points.
    /// </summary>
    private static Geometry<T> IntersectionLineStringWithPolygon(Geometry<T> lineString, Geometry<T> polygon)
        => ClipLineStringByPolygon(lineString, polygon, keepInside: true);

    /// <summary>
    /// Clips a line string by a polygon: the sub-segments inside (<paramref name="keepInside"/>)
    /// or outside the polygon; isolated boundary touch points are reported only when keeping the
    /// inside (they belong to the intersection, not to the difference).
    /// </summary>
    private static Geometry<T> ClipLineStringByPolygon(Geometry<T> lineString, Geometry<T> polygon, bool keepInside)
    {
        var srid = lineString.Srid;

        var pieces = new List<Geometry<T>>();
        var touchCandidates = new List<Geometry<T>>();

        var ringSegments = polygon.Geometries.SelectMany(GetRingSegments).ToList();

        var chain = new List<T>();

        void FlushChain()
        {
            if (chain.Count >= 2)
                pieces.Add(CreateLineStringGeometry(new List<T>(chain), srid));

            chain.Clear();
        }

        for (int i = 0; i < lineString.Points.Count - 1; i++)
        {
            var start = lineString.Points[i];
            var end = lineString.Points[i + 1];

            if (ArePointsAlmostEqual(start, end))
                continue;

            var parameterEpsilon = SpatialUtility.EpsilonDistance / SpatialUtility.GetEuclideanLength(start, end);

            // split the segment wherever it meets the polygon boundary
            var parameters = new List<double> { 0, 1 };

            foreach (var (ringStart, ringEnd) in ringSegments)
            {
                var relation = TopologyUtility.LineSegmentsIntersects(start, end, ringStart, ringEnd, out T intersection);

                if (relation == LineLineSegmentRelation.Intersect)
                {
                    parameters.Add(Math.Clamp(GetParameterOnSegment(start, end, intersection), 0, 1));
                }
                else if (relation == LineLineSegmentRelation.Coinciding)
                {
                    var first = GetParameterOnSegment(start, end, ringStart);
                    var second = GetParameterOnSegment(start, end, ringEnd);

                    if (Math.Max(first, second) < 0 || Math.Min(first, second) > 1)
                        continue;

                    parameters.Add(Math.Clamp(first, 0, 1));
                    parameters.Add(Math.Clamp(second, 0, 1));
                }
            }

            parameters.Sort();

            var splits = new List<double>();

            foreach (var parameter in parameters)
            {
                if (splits.Count == 0 || parameter - splits[splits.Count - 1] > parameterEpsilon)
                    splits.Add(parameter);
            }

            // keep the sub-segments whose midpoint lies in (or on) the polygon,
            // chaining consecutive kept pieces into a single line string
            for (int k = 0; k < splits.Count - 1; k++)
            {
                var midpoint = GetPointOnSegment(start, end, (splits[k] + splits[k + 1]) / 2.0);

                if (TopologyUtility.IsPointInPolygonOrOnBoundary(polygon, midpoint) == keepInside)
                {
                    var pieceStart = GetPointOnSegment(start, end, splits[k]);
                    var pieceEnd = GetPointOnSegment(start, end, splits[k + 1]);

                    if (chain.Count > 0 && !ArePointsAlmostEqual(chain[chain.Count - 1], pieceStart))
                        FlushChain();

                    if (chain.Count == 0)
                        chain.Add(pieceStart);

                    chain.Add(pieceEnd);
                }
                else
                {
                    FlushChain();
                }
            }

            if (!keepInside)
                continue;

            // isolated boundary touches; those covered by a kept sub-segment are pruned later
            foreach (var parameter in splits)
            {
                var splitPoint = GetPointOnSegment(start, end, parameter);

                if (TopologyUtility.IsPointInPolygonOrOnBoundary(polygon, splitPoint))
                    touchCandidates.Add(CreatePointGeometry(splitPoint, srid));
            }
        }

        FlushChain();

        pieces.AddRange(touchCandidates);

        return CombineIntersectionPieces(pieces, srid);
    }

    /// <summary>
    /// Intersects two polygons. Containment, disjoint, boundary-touch and crossing-boundary
    /// cases are supported; boundary-degenerate area overlaps (shared edges or vertices lying
    /// exactly on the other boundary) and holes overlapping the intersection area throw
    /// <see cref="NotImplementedException"/>.
    /// </summary>
    private static Geometry<T> IntersectionPolygons(Geometry<T> poly1, Geometry<T> poly2)
    {
        var srid = poly1.Srid;

        if (poly1.IsNullOrEmpty() || poly2.IsNullOrEmpty())
            return CreateEmptyIntersectionResult(srid);

        if (IsPolygonWithinPolygon(poly1, poly2))
            return poly1.Clone();

        if (IsPolygonWithinPolygon(poly2, poly1))
            return poly2.Clone();

        if (!poly1.Intersects(poly2))
            return CreateEmptyIntersectionResult(srid);

        var clippedRings = ClipRings(poly1.Geometries[0], poly2.Geometries[0], srid);

        if (clippedRings is null || clippedRings.Count == 0)
        {
            // no clean boundary crossings: either the polygons only touch on their boundaries,
            // or the overlap is boundary-degenerate
            if (!HaveInteriorOverlapAtSamplePoints(poly1, poly2))
                return IntersectionPolygonBoundaries(poly1, poly2, srid);

            throw new NotImplementedException(
                "Geometry > IntersectionPolygons: polygon-polygon intersection with boundary degeneracies " +
                "(shared edges or vertices lying exactly on the other polygon's boundary) is not implemented yet.");
        }

        var result = CreatePolygonOrMultiPolygon(clippedRings, srid);

        foreach (var hole in poly1.Geometries.Skip(1).Concat(poly2.Geometries.Skip(1)))
        {
            var holeAsPolygon = Geometry<T>.Create(new List<Geometry<T>> { hole.Clone() }, GeometryType.Polygon, srid);

            if (HaveInteriorOverlapAtSamplePoints(holeAsPolygon, result))
                throw new NotImplementedException(
                    "Geometry > IntersectionPolygons: holes overlapping the intersection area are not implemented yet.");
        }

        return result;
    }

    /// <summary>
    /// Ring vertices plus edge midpoints, used as containment/overlap probes.
    /// </summary>
    private static IEnumerable<T> GetPolygonSamplePoints(Geometry<T> polygonOrMultiPolygon)
    {
        if (polygonOrMultiPolygon.Type == GeometryType.MultiPolygon)
        {
            foreach (var polygon in polygonOrMultiPolygon.Geometries)
                foreach (var sample in GetPolygonSamplePoints(polygon))
                    yield return sample;

            yield break;
        }

        foreach (var ring in polygonOrMultiPolygon.Geometries)
        {
            foreach (var (start, end) in GetRingSegments(ring))
            {
                yield return start;
                yield return new T() { X = (start.X + end.X) / 2.0, Y = (start.Y + end.Y) / 2.0 };
            }
        }
    }

    /// <summary>
    /// Sample-based test for overlapping interiors (vertices and edge midpoints strictly inside the other polygon).
    /// </summary>
    private static bool HaveInteriorOverlapAtSamplePoints(Geometry<T> poly1, Geometry<T> poly2)
    {
        return GetPolygonSamplePoints(poly1).Any(p => TopologyUtility.IsPointInPolygon(poly2, p)) ||
               GetPolygonSamplePoints(poly2).Any(p => TopologyUtility.IsPointInPolygon(poly1, p));
    }

    /// <summary>
    /// Sample-based containment test: every vertex and edge midpoint of <paramref name="inner"/>
    /// lies in or on <paramref name="outer"/>, and no hole of <paramref name="outer"/> pokes into
    /// <paramref name="inner"/>.
    /// </summary>
    private static bool IsPolygonWithinPolygon(Geometry<T> inner, Geometry<T> outer)
    {
        foreach (var sample in GetPolygonSamplePoints(inner))
        {
            if (!TopologyUtility.IsPointInPolygonOrOnBoundary(outer, sample))
                return false;
        }

        foreach (var hole in outer.Geometries.Skip(1))
        {
            if (hole.Points.Any(v => TopologyUtility.IsPointInPolygon(inner, v)))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Boundary-only intersection of two polygons (shared edges and touch points).
    /// </summary>
    private static Geometry<T> IntersectionPolygonBoundaries(Geometry<T> poly1, Geometry<T> poly2, int srid)
    {
        var pieces = new List<Geometry<T>>();

        foreach (var ring1 in poly1.Geometries)
        {
            var closed1 = AsClosedLineString(ring1, srid);

            foreach (var ring2 in poly2.Geometries)
            {
                var piece = IntersectionLineStrings(closed1, AsClosedLineString(ring2, srid));

                if (!piece.IsNullOrEmpty())
                    pieces.Add(piece);
            }
        }

        return CombineIntersectionPieces(pieces, srid);
    }

    private static Geometry<T> AsClosedLineString(Geometry<T> ring, int srid)
    {
        var points = new List<T>(ring.Points);

        if (points.Count > 1 && !ArePointsAlmostEqual(points[0], points[points.Count - 1]))
            points.Add(new T() { X = points[0].X, Y = points[0].Y });

        return CreateLineStringGeometry(points, srid);
    }

    private sealed class ClipVertex
    {
        public T Point;
        public bool IsIntersection;
        public bool IsEntry;
        public bool Visited;
        public ClipVertex Twin;
        public ClipVertex Next;
        public ClipVertex Previous;
    }

    private static List<T> NormalizeRingPoints(Geometry<T> ring)
    {
        var result = new List<T>();

        foreach (var point in ring.Points)
        {
            if (result.Count == 0 || !ArePointsAlmostEqual(result[result.Count - 1], point))
                result.Add(point);
        }

        while (result.Count > 1 && ArePointsAlmostEqual(result[0], result[result.Count - 1]))
            result.RemoveAt(result.Count - 1);

        return result;
    }

    private enum RingClipOperation
    {
        Intersection,
        Union,
        Difference, // subject minus clip
    }

    /// <summary>
    /// Clips two simple rings against each other (Greiner–Hormann) and returns the rings of the
    /// intersection, union or difference (ring1 − ring2) area. Returns an empty list when the
    /// boundaries do not properly cross, and null when a boundary degeneracy prevents clipping
    /// (a vertex lying exactly on the other boundary, or partially coinciding edges).
    /// </summary>
    private static List<Geometry<T>>? ClipRings(Geometry<T> ring1, Geometry<T> ring2, int srid, RingClipOperation operation = RingClipOperation.Intersection)
    {
        var subjectPoints = NormalizeRingPoints(ring1);
        var clipPoints = NormalizeRingPoints(ring2);

        if (subjectPoints.Count < 3 || clipPoints.Count < 3)
            return null;

        // 1. find the proper pairwise edge crossings
        var crossings = new List<(int SubjectEdge, double SubjectParameter, int ClipEdge, double ClipParameter, T Point)>();

        for (int i = 0; i < subjectPoints.Count; i++)
        {
            var subjectStart = subjectPoints[i];
            var subjectEnd = subjectPoints[(i + 1) % subjectPoints.Count];

            var subjectParameterEpsilon = SpatialUtility.EpsilonDistance / SpatialUtility.GetEuclideanLength(subjectStart, subjectEnd);

            for (int j = 0; j < clipPoints.Count; j++)
            {
                var clipStart = clipPoints[j];
                var clipEnd = clipPoints[(j + 1) % clipPoints.Count];

                var relation = TopologyUtility.LineSegmentsIntersects(subjectStart, subjectEnd, clipStart, clipEnd, out T intersection);

                if (relation == LineLineSegmentRelation.Coinciding)
                {
                    // collinear edges: degenerate when the segments actually overlap
                    var first = GetParameterOnSegment(subjectStart, subjectEnd, clipStart);
                    var second = GetParameterOnSegment(subjectStart, subjectEnd, clipEnd);

                    if (Math.Min(1, Math.Max(first, second)) - Math.Max(0, Math.Min(first, second)) > subjectParameterEpsilon)
                        return null;

                    continue;
                }

                if (relation != LineLineSegmentRelation.Intersect)
                    continue;

                var subjectParameter = GetParameterOnSegment(subjectStart, subjectEnd, intersection);
                var clipParameter = GetParameterOnSegment(clipStart, clipEnd, intersection);

                var clipParameterEpsilon = SpatialUtility.EpsilonDistance / SpatialUtility.GetEuclideanLength(clipStart, clipEnd);

                // a crossing at (or too close to) a vertex is degenerate
                if (subjectParameter < subjectParameterEpsilon || subjectParameter > 1 - subjectParameterEpsilon ||
                    clipParameter < clipParameterEpsilon || clipParameter > 1 - clipParameterEpsilon)
                    return null;

                crossings.Add((i, subjectParameter, j, clipParameter, intersection));
            }
        }

        if (crossings.Count == 0)
            return new List<Geometry<T>>();

        if (crossings.Count % 2 != 0)
            return null;

        // 2. build the circular vertex lists with the crossings inserted in order
        var (subjectOrdered, subjectIntersectionNodes) = BuildRingNodes(subjectPoints, crossings, useSubjectEdge: true);
        var (clipOrdered, clipIntersectionNodes) = BuildRingNodes(clipPoints, crossings, useSubjectEdge: false);

        for (int k = 0; k < crossings.Count; k++)
        {
            subjectIntersectionNodes[k].Twin = clipIntersectionNodes[k];
            clipIntersectionNodes[k].Twin = subjectIntersectionNodes[k];
        }

        // 3. mark entry/exit by toggling the inside/outside state along each ring
        MarkEntryExit(subjectOrdered, CreateLineStringGeometry(clipPoints, srid));
        MarkEntryExit(clipOrdered, CreateLineStringGeometry(subjectPoints, srid));

        // 4. trace the intersection rings
        var resultRings = new List<Geometry<T>>();

        var maximumSteps = 4 * (subjectOrdered.Count + clipOrdered.Count + 2);

        foreach (var startNode in subjectIntersectionNodes)
        {
            if (startNode.Visited)
                continue;

            var ringPoints = new List<T>() { startNode.Point };

            var current = startNode;
            var onSubjectList = true;
            var remainingSteps = maximumSteps;

            while (true)
            {
                current.Visited = true;
                current.Twin.Visited = true;

                // intersection: forward at entries, backward at exits; union: the mirror rule;
                // difference (subject − clip): union rule on the subject ring, intersection rule
                // on the clip ring
                bool goForward = operation switch
                {
                    RingClipOperation.Intersection => current.IsEntry,
                    RingClipOperation.Union => !current.IsEntry,
                    _ => onSubjectList ? !current.IsEntry : current.IsEntry,
                };

                if (goForward)
                {
                    do
                    {
                        current = current.Next;
                        ringPoints.Add(current.Point);

                        if (--remainingSteps < 0)
                            return null;
                    } while (!current.IsIntersection);
                }
                else
                {
                    do
                    {
                        current = current.Previous;
                        ringPoints.Add(current.Point);

                        if (--remainingSteps < 0)
                            return null;
                    } while (!current.IsIntersection);
                }

                if (current == startNode || current == startNode.Twin)
                    break;

                current = current.Twin;
                onSubjectList = !onSubjectList;

                if (current == startNode || current == startNode.Twin)
                    break;

                if (--remainingSteps < 0)
                    return null;
            }

            while (ringPoints.Count > 1 && ArePointsAlmostEqual(ringPoints[0], ringPoints[ringPoints.Count - 1]))
                ringPoints.RemoveAt(ringPoints.Count - 1);

            if (ringPoints.Count >= 3)
                resultRings.Add(CreateLineStringGeometry(ringPoints, srid));
        }

        return resultRings;
    }

    private static (List<ClipVertex> OrderedNodes, ClipVertex[] IntersectionNodes) BuildRingNodes(
        List<T> ringPoints,
        List<(int SubjectEdge, double SubjectParameter, int ClipEdge, double ClipParameter, T Point)> crossings,
        bool useSubjectEdge)
    {
        var intersectionNodes = new ClipVertex[crossings.Count];

        var orderedNodes = new List<ClipVertex>();

        for (int i = 0; i < ringPoints.Count; i++)
        {
            orderedNodes.Add(new ClipVertex() { Point = ringPoints[i] });

            var edgeCrossings = Enumerable.Range(0, crossings.Count)
                                            .Where(c => (useSubjectEdge ? crossings[c].SubjectEdge : crossings[c].ClipEdge) == i)
                                            .OrderBy(c => useSubjectEdge ? crossings[c].SubjectParameter : crossings[c].ClipParameter);

            foreach (var crossingIndex in edgeCrossings)
            {
                var node = new ClipVertex() { Point = crossings[crossingIndex].Point, IsIntersection = true };

                intersectionNodes[crossingIndex] = node;

                orderedNodes.Add(node);
            }
        }

        for (int i = 0; i < orderedNodes.Count; i++)
        {
            orderedNodes[i].Next = orderedNodes[(i + 1) % orderedNodes.Count];
            orderedNodes[i].Previous = orderedNodes[(i - 1 + orderedNodes.Count) % orderedNodes.Count];
        }

        return (orderedNodes, intersectionNodes);
    }

    private static void MarkEntryExit(List<ClipVertex> orderedNodes, Geometry<T> otherRing)
    {
        var inside = TopologyUtility.IsPointInRing(otherRing, orderedNodes[0].Point);

        foreach (var node in orderedNodes)
        {
            if (!node.IsIntersection)
                continue;

            node.IsEntry = !inside;

            inside = !inside;
        }
    }

    #endregion


    #region Difference

    /// <summary>
    /// Computes the difference of this geometry with another geometry (this − other).
    /// Equivalent to SQL Server's STDifference().
    /// </summary>
    /// <remarks>
    /// Polygon differences with degenerate boundary overlaps (shared edges while the interiors
    /// overlap) or with holes interacting with the subtracted area throw
    /// <see cref="NotImplementedException"/> instead of silently returning a wrong result.
    /// Subtracting lower-dimensional geometries (points from lines, points/lines from polygons)
    /// leaves the geometry unchanged, matching OGC semantics.
    /// </remarks>
    /// <param name="other">The geometry to subtract</param>
    /// <returns>The difference of the two geometries</returns>
    public Geometry<T> Difference(Geometry<T> other)
    {
        if (this.IsNullOrEmpty())
            return CreateEmptyIntersectionResult(this.Srid);

        if (other.IsNullOrEmpty())
            return this;

        if (this.Srid != other.Srid)
            throw new ArgumentException("SRIDs must match for Difference operation");

        var thisPoints = new List<Geometry<T>>();
        var thisLineStrings = new List<Geometry<T>>();
        var thisPolygons = new List<Geometry<T>>();

        FlattenIntoParts(this.Clone(), thisPoints, thisLineStrings, thisPolygons);

        var otherPoints = new List<Geometry<T>>();
        var otherLineStrings = new List<Geometry<T>>();
        var otherPolygons = new List<Geometry<T>>();

        FlattenIntoParts(other, otherPoints, otherLineStrings, otherPolygons);

        // polygons: only polygons of the subtrahend reduce them
        var resultPolygons = new List<Geometry<T>>();

        foreach (var polygon in thisPolygons)
        {
            var pieces = new List<Geometry<T>> { polygon };

            foreach (var subtrahend in otherPolygons)
                pieces = pieces.SelectMany(p => ExtractPolygonParts(DifferencePolygons(p, subtrahend))).ToList();

            resultPolygons.AddRange(pieces);
        }

        // lines: clipped by polygon areas and by collinear overlaps with the subtrahend lines
        var resultLineStrings = new List<Geometry<T>>();

        foreach (var lineString in thisLineStrings)
        {
            var pieces = new List<Geometry<T>> { lineString };

            foreach (var polygon in otherPolygons)
                pieces = pieces.SelectMany(l => ExtractLineStringParts(ClipLineStringByPolygon(l, polygon, keepInside: false))).ToList();

            foreach (var otherLineString in otherLineStrings)
                pieces = pieces.SelectMany(l => ExtractLineStringParts(DifferenceLineStrings(l, otherLineString))).ToList();

            resultLineStrings.AddRange(pieces);
        }

        // points: removed when covered by anything in the subtrahend
        var resultPoints = thisPoints
            .Where(p => !IsPointCoveredByParts(p.Points[0], otherPoints, otherLineStrings, otherPolygons))
            .ToList();

        return AssembleGeometryParts(resultPoints, resultLineStrings, resultPolygons, this.Srid);
    }

    /// <summary>
    /// SQL Server style alias for <see cref="Difference(Geometry{T})"/> (OGC STDifference).
    /// </summary>
    public Geometry<T> STDifference(Geometry<T> other) => Difference(other);

    private static List<Geometry<T>> ExtractPolygonParts(Geometry<T> geometry)
    {
        var polygons = new List<Geometry<T>>();

        FlattenIntoParts(geometry, new List<Geometry<T>>(), new List<Geometry<T>>(), polygons);

        return polygons;
    }

    private static List<Geometry<T>> ExtractLineStringParts(Geometry<T> geometry)
    {
        var lineStrings = new List<Geometry<T>>();

        FlattenIntoParts(geometry, new List<Geometry<T>>(), lineStrings, new List<Geometry<T>>());

        return lineStrings;
    }

    private static bool IsPointCoveredByParts(T point, List<Geometry<T>> points, List<Geometry<T>> lineStrings, List<Geometry<T>> polygons)
    {
        return points.Any(p => ArePointsAlmostEqual(p.Points[0], point)) ||
               IsPointOnAnyLineString(lineStrings, point) ||
               polygons.Any(p => TopologyUtility.IsPointInPolygonOrOnBoundary(p, point));
    }

    /// <summary>
    /// Removes the collinear overlapping parts of <paramref name="lineString"/> that are covered
    /// by <paramref name="other"/> (crossing points have zero measure and leave the line unchanged).
    /// </summary>
    private static Geometry<T> DifferenceLineStrings(Geometry<T> lineString, Geometry<T> other)
    {
        var srid = lineString.Srid;

        var pieces = new List<Geometry<T>>();

        var chain = new List<T>();

        void FlushChain()
        {
            if (chain.Count >= 2)
                pieces.Add(CreateLineStringGeometry(new List<T>(chain), srid));

            chain.Clear();
        }

        for (int i = 0; i < lineString.Points.Count - 1; i++)
        {
            var start = lineString.Points[i];
            var end = lineString.Points[i + 1];

            if (ArePointsAlmostEqual(start, end))
                continue;

            var parameterEpsilon = SpatialUtility.EpsilonDistance / SpatialUtility.GetEuclideanLength(start, end);

            // split at collinear overlaps with the other line; crossings are split too so an
            // interval midpoint never coincides with a zero-measure crossing point
            var parameters = new List<double> { 0, 1 };

            for (int j = 0; j < other.Points.Count - 1; j++)
            {
                var otherStart = other.Points[j];
                var otherEnd = other.Points[j + 1];

                if (ArePointsAlmostEqual(otherStart, otherEnd))
                    continue;

                var relation = TopologyUtility.LineSegmentsIntersects(start, end, otherStart, otherEnd, out T crossing);

                if (relation == LineLineSegmentRelation.Intersect)
                {
                    parameters.Add(Math.Clamp(GetParameterOnSegment(start, end, crossing), 0, 1));
                    continue;
                }

                if (relation != LineLineSegmentRelation.Coinciding)
                    continue;

                var first = GetParameterOnSegment(start, end, otherStart);
                var second = GetParameterOnSegment(start, end, otherEnd);

                if (Math.Max(first, second) < 0 || Math.Min(first, second) > 1)
                    continue;

                parameters.Add(Math.Clamp(first, 0, 1));
                parameters.Add(Math.Clamp(second, 0, 1));
            }

            parameters.Sort();

            var splits = new List<double>();

            foreach (var parameter in parameters)
            {
                if (splits.Count == 0 || parameter - splits[splits.Count - 1] > parameterEpsilon)
                    splits.Add(parameter);
            }

            for (int k = 0; k < splits.Count - 1; k++)
            {
                var midpoint = GetPointOnSegment(start, end, (splits[k] + splits[k + 1]) / 2.0);

                if (!IsPointOnAnyLineString(new List<Geometry<T>> { other }, midpoint))
                {
                    var pieceStart = GetPointOnSegment(start, end, splits[k]);
                    var pieceEnd = GetPointOnSegment(start, end, splits[k + 1]);

                    if (chain.Count > 0 && !ArePointsAlmostEqual(chain[chain.Count - 1], pieceStart))
                        FlushChain();

                    if (chain.Count == 0)
                        chain.Add(pieceStart);

                    chain.Add(pieceEnd);
                }
                else
                {
                    FlushChain();
                }
            }
        }

        FlushChain();

        return CombineIntersectionPieces(pieces, srid);
    }

    /// <summary>
    /// Subtracts one polygon from another (minuend − subtrahend). Disjoint, touch-only,
    /// contained and crossing-boundary cases are supported; boundary-degenerate overlaps and
    /// holes interacting with the subtracted area throw <see cref="NotImplementedException"/>.
    /// </summary>
    private static Geometry<T> DifferencePolygons(Geometry<T> minuend, Geometry<T> subtrahend)
    {
        var srid = minuend.Srid;

        if (minuend.IsNullOrEmpty())
            return CreateEmptyIntersectionResult(srid);

        if (subtrahend.IsNullOrEmpty())
            return minuend;

        if (!minuend.Intersects(subtrahend))
            return minuend;

        // the minuend is fully covered: nothing remains
        if (IsPolygonWithinPolygon(minuend, subtrahend))
            return CreateEmptyIntersectionResult(srid);

        // holes of the minuend reaching the subtracted region are not supported yet
        foreach (var hole in minuend.Geometries.Skip(1))
        {
            var holeAsPolygon = Geometry<T>.Create(new List<Geometry<T>> { hole.Clone() }, GeometryType.Polygon, srid);

            if (holeAsPolygon.Intersects(subtrahend))
                throw new NotImplementedException(
                    "Geometry > DifferencePolygons: holes interacting with the subtracted area are not implemented yet.");
        }

        // subtrahend fully inside: punch it out as a hole (its own holes become islands)
        if (IsPolygonWithinPolygon(subtrahend, minuend))
        {
            var rings = minuend.Geometries.Select(r => r.Clone()).ToList();

            foreach (var ring in subtrahend.Geometries)
                rings.Add(ring.Clone());

            return CreatePolygonOrMultiPolygon(rings, srid);
        }

        // crossing boundaries: subtrahend holes reaching into the minuend are not supported yet
        foreach (var hole in subtrahend.Geometries.Skip(1))
        {
            var holeAsPolygon = Geometry<T>.Create(new List<Geometry<T>> { hole.Clone() }, GeometryType.Polygon, srid);

            if (holeAsPolygon.Intersects(minuend))
                throw new NotImplementedException(
                    "Geometry > DifferencePolygons: holes interacting with the subtracted area are not implemented yet.");
        }

        var differenceRings = ClipRings(minuend.Geometries[0], subtrahend.Geometries[0], srid, RingClipOperation.Difference);

        if (differenceRings is null)
        {
            // no clean boundary crossings: either the polygons only touch, or the overlap is
            // boundary-degenerate
            if (!HaveInteriorOverlapAtSamplePoints(minuend, subtrahend))
                return minuend;

            throw new NotImplementedException(
                "Geometry > DifferencePolygons: polygon-polygon difference with boundary degeneracies " +
                "(shared edges or vertices lying exactly on the other polygon's boundary) is not implemented yet.");
        }

        if (differenceRings.Count == 0)
            return minuend; // boundaries touch without interior overlap

        var resultRings = new List<Geometry<T>>(differenceRings);

        // minuend holes survive (guarded above: none of them touches the subtrahend)
        AppendSurvivingHoles(resultRings, minuend, subtrahend, srid);

        return CreatePolygonOrMultiPolygon(resultRings, srid);
    }

    #endregion


    #region Buffer

    /// <summary>
    /// Creates a buffer around this geometry at the specified distance (round joins/caps,
    /// comparable to SQL Server STBuffer). Geometries with SRID 4326 are buffered geodesically
    /// with the distance in meters.
    /// </summary>
    /// <remarks>
    /// The buffer is built from offset curves with circular arcs (64 segments per full circle),
    /// so areas differ from the exact buffer by well under 1%. Offset curves are not self-union
    /// cleaned: when the distance exceeds the local feature size of concave inputs, small
    /// self-intersecting slivers can remain in the boundary.
    /// </remarks>
    /// <param name="distance">The buffer distance (must be non-negative)</param>
    /// <returns>A buffered geometry</returns>
    public Geometry<T> Buffer(double distance)
    {
        if (this.IsNullOrEmpty())
            return Geometry<T>.Empty;

        if (distance < 0)
            throw new ArgumentException("Buffer distance cannot be negative", nameof(distance));

        if (Math.Abs(distance) < SpatialUtility.EpsilonDistance)
        {
            // For zero or very small distance, return original geometry for points/lines
            // For polygons, return as-is
            return this.Clone();
        }

        // Check if geometry is geodetic (lat/long on ellipsoid)
        if (this.Srid == SridHelper.GeodeticWGS84)
        {
            // Use geodesic buffer for geodetic coordinates
            return BufferGeodesic(distance);
        }

        switch (this.Type)
        {
            case GeometryType.Point:
                return BufferPoint(distance);

            case GeometryType.LineString:
                return BufferLineString(distance);

            case GeometryType.Polygon:
                return BufferPolygon(distance);

            case GeometryType.MultiPoint:
                return BufferMultiPoint(distance);

            case GeometryType.MultiLineString:
                return BufferMultiLineString(distance);

            case GeometryType.MultiPolygon:
                return BufferMultiPolygon(distance);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException($"Buffer not implemented for {this.Type}");
        }
    }

    /// <summary>
    /// Creates a geodesic buffer around this geometry at the specified distance (for geodetic coordinates)
    /// Uses Vincenty formulas for accurate ellipsoidal calculations
    /// </summary>
    /// <param name="distance">The buffer distance in meters</param>
    /// <returns>A buffered geometry</returns>
    public Geometry<T> BufferGeodesic(double distance)
    {
        if (this.IsNullOrEmpty())
            return Geometry<T>.Empty;

        if (distance < 0)
            throw new ArgumentException("Buffer distance cannot be negative", nameof(distance));

        if (Math.Abs(distance) < SpatialUtility.EpsilonDistance)
        {
            return this.Clone();
        }

        switch (this.Type)
        {
            case GeometryType.Point:
                return BufferPointGeodesic(distance);

            case GeometryType.LineString:
                return BufferLineStringGeodesic(distance);

            case GeometryType.Polygon:
                return BufferPolygonGeodesic(distance);

            case GeometryType.MultiPoint:
                return BufferMultiPointGeodesic(distance);

            case GeometryType.MultiLineString:
                return BufferMultiLineStringGeodesic(distance);

            case GeometryType.MultiPolygon:
                return BufferMultiPolygonGeodesic(distance);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException($"BufferGeodesic not implemented for {this.Type}");
        }
    }

    /// <summary>
    /// Creates a spherical buffer around this geometry at the specified distance (for geodetic coordinates)
    /// Uses Haversine formula for spherical calculations (faster but less accurate than geodesic)
    /// </summary>
    /// <param name="distance">The buffer distance in meters</param>
    /// <returns>A buffered geometry</returns>
    public Geometry<T> BufferSpherical(double distance)
    {
        if (this.IsNullOrEmpty())
            return Geometry<T>.Empty;

        if (distance < 0)
            throw new ArgumentException("Buffer distance cannot be negative", nameof(distance));

        if (Math.Abs(distance) < SpatialUtility.EpsilonDistance)
        {
            return this.Clone();
        }

        switch (this.Type)
        {
            case GeometryType.Point:
                return BufferPointSpherical(distance);

            case GeometryType.LineString:
                return BufferLineStringSpherical(distance);

            case GeometryType.Polygon:
                return BufferPolygonSpherical(distance);

            case GeometryType.MultiPoint:
                return BufferMultiPointSpherical(distance);

            case GeometryType.MultiLineString:
                return BufferMultiLineStringSpherical(distance);

            case GeometryType.MultiPolygon:
                return BufferMultiPolygonSpherical(distance);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException($"BufferSpherical not implemented for {this.Type}");
        }
    }

    private Geometry<T> BufferPoint(double distance)
    {
        var center = this.Points[0];
        return CreateCircle(center, distance, 64, this.Srid);
    }

    private Geometry<T> BufferLineString(double distance)
    {
        var points = RemoveConsecutiveDuplicatePoints(this.Points, isRing: false);

        if (points.Count == 0)
            return Geometry<T>.Empty;

        // a degenerate (zero-length) line string buffers like a point
        if (points.Count == 1)
            return CreateCircle(points[0], distance, 64, this.Srid);

        // a closed line string buffers like a ring (no end caps): outer offset + inner hole
        if (points.Count >= 4 && ArePointsAlmostEqual(points[0], points[points.Count - 1]))
        {
            var ringPoints = RemoveConsecutiveDuplicatePoints(points, isRing: true);

            if (ringPoints.Count >= 3)
                return BufferClosedPolyline(ringPoints, distance, offsetsLeft: true, OffsetLineSegment);
        }

        var leftOffset = OffsetLineSegment(points, distance, false);
        var rightOffset = OffsetLineSegment(points, -distance, false);

        if (leftOffset.Count < 2 || rightOffset.Count < 2)
            return Geometry<T>.Empty;

        // boundary: left side forward → end cap → right side backward → start cap
        var boundary = new List<T>(leftOffset);

        // end cap: sweep clockwise from the left offset through the forward direction to the right offset
        var endDirection = GetUnitDirection(points[points.Count - 2], points[points.Count - 1]);
        AddArcPoints(boundary, points[points.Count - 1], distance, Math.Atan2(endDirection.X, -endDirection.Y), -Math.PI);

        for (int i = rightOffset.Count - 1; i >= 0; i--)
            boundary.Add(rightOffset[i]);

        // start cap: sweep clockwise from the right offset through the backward direction to the left offset
        var startDirection = GetUnitDirection(points[0], points[1]);
        AddArcPoints(boundary, points[0], distance, Math.Atan2(startDirection.X, -startDirection.Y) - Math.PI, -Math.PI);

        boundary = RemoveConsecutiveDuplicatePoints(boundary, isRing: true);

        if (boundary.Count < 3)
            return Geometry<T>.Empty;

        return Geometry<T>.CreatePolygon(boundary, this.Srid);
    }

    private Geometry<T> BufferPolygon(double distance)
    {
        if (this.Geometries == null || this.Geometries.Count == 0)
            return Geometry<T>.Empty;

        var polygon = AsOgcOrientedPolygon();

        // exterior ring is CCW (OGC SFA), so with the left-offset convention a negative
        // distance moves it outward
        var bufferedExterior = OffsetLineSegment(polygon.Geometries[0].Points, -distance, true);

        if (bufferedExterior.Count < 3)
            return Geometry<T>.Empty;

        var bufferedRings = new List<Geometry<T>>
        {
            Geometry<T>.Create(bufferedExterior, GeometryType.LineString, this.Srid),
        };

        // holes are CW, so the same negative distance moves them into the hole (the hole
        // shrinks as the buffer grows)
        for (int i = 1; i < polygon.Geometries.Count; i++)
        {
            var hole = polygon.Geometries[i];

            if (ShouldEliminateHole(hole, distance))
                continue;

            var bufferedHole = OffsetLineSegment(hole.Points, -distance, true);

            if (bufferedHole.Count < 3)
                continue;

            // an offset hole whose orientation flipped (CW → CCW) has collapsed
            if (SpatialUtility.GetSignedEuclideanArea(bufferedHole) >= 0)
                continue;

            // the shrunk hole must still lie inside the buffered exterior
            if (TopologyUtility.IsPointInRing(bufferedRings[0], bufferedHole[0]))
                bufferedRings.Add(Geometry<T>.Create(bufferedHole, GeometryType.LineString, this.Srid));
        }

        return CreatePolygonOrMultiPolygon(bufferedRings, this.Srid);
    }

    private Geometry<T> BufferMultiPoint(double distance)
        => UnionBufferPieces(this.Geometries.Select(g => g.BufferPoint(distance)).ToList(), this.Srid);

    private Geometry<T> BufferMultiLineString(double distance)
        => UnionBufferPieces(this.Geometries.Select(g => g.BufferLineString(distance)).ToList(), this.Srid);

    private Geometry<T> BufferMultiPolygon(double distance)
        => UnionBufferPieces(this.Geometries.Select(g => g.BufferPolygon(distance)).ToList(), this.Srid);

    /// <summary>
    /// Buffers a closed polyline (a ring-shaped curve): the result is the band within the buffer
    /// distance of the ring path — outer offset ring plus, while the eroded interior hasn't
    /// collapsed, the inner offset ring as a hole.
    /// </summary>
    /// <param name="ringPoints">cleaned ring vertices without the repeated closing vertex</param>
    /// <param name="offsetsLeft">true when the offset function's positive distance is the LEFT side
    /// (planar convention); false for the compass-bearing convention (geodesic/spherical)</param>
    private Geometry<T> BufferClosedPolyline(List<T> ringPoints, double distance, bool offsetsLeft, Func<List<T>, double, bool, List<T>> offset)
    {
        double ringArea = SpatialUtility.GetSignedEuclideanArea(ringPoints);

        // the enclosed area lies on the left of a CCW ring; pick the sign that moves outward
        double outwardDistance = (ringArea > 0) == offsetsLeft ? -distance : distance;

        var outerRing = offset(ringPoints, outwardDistance, true);

        if (outerRing.Count < 3)
            return Geometry<T>.Empty;

        var rings = new List<Geometry<T>> { Geometry<T>.Create(outerRing, GeometryType.LineString, this.Srid) };

        var innerRing = offset(ringPoints, -outwardDistance, true);

        if (innerRing.Count >= 3 &&
            Math.Sign(SpatialUtility.GetSignedEuclideanArea(innerRing)) == Math.Sign(ringArea))
        {
            rings.Add(Geometry<T>.Create(innerRing, GeometryType.LineString, this.Srid));
        }

        return CreatePolygonOrMultiPolygon(rings, this.Srid);
    }

    /// <summary>
    /// Returns this polygon with the OGC SFA ring orientation enforced (exterior CCW, holes CW).
    /// Geometry sources such as the WKT reader keep the source ring order, so the invariant is
    /// re-established here before orientation-dependent offsetting.
    /// </summary>
    private Geometry<T> AsOgcOrientedPolygon()
    {
        var rings = new List<Geometry<T>>(this.Geometries.Count);

        for (int i = 0; i < this.Geometries.Count; i++)
        {
            var ring = this.Geometries[i];

            if (ring.IsNullOrEmpty() || ring.Points.Count < 3)
            {
                rings.Add(ring);
                continue;
            }

            bool isClockwise = SpatialUtility.IsClockwise(ring.Points);
            bool needsReverse = i == 0 ? isClockwise : !isClockwise;

            if (!needsReverse)
            {
                rings.Add(ring);
                continue;
            }

            var reversedPoints = new List<T>(ring.Points);
            reversedPoints.Reverse();

            rings.Add(Geometry<T>.Create(reversedPoints, GeometryType.LineString, this.Srid));
        }

        return Geometry<T>.Create(rings, GeometryType.Polygon, this.Srid);
    }

    /// <summary>
    /// Merges buffered pieces into a valid Polygon/MultiPolygon; pieces whose union cannot be
    /// computed cleanly are kept as separate members — buffering never throws for valid input.
    /// </summary>
    private static Geometry<T> UnionBufferPieces(List<Geometry<T>> pieces, int srid)
        => UnionPolygonPieces(pieces, srid, throwOnDegenerateOverlap: false);

    /// <summary>
    /// Merges polygon pieces into a valid Polygon/MultiPolygon: overlapping pieces are unioned
    /// (Greiner–Hormann). Pieces that only touch on their boundaries stay separate members.
    /// Pieces whose interiors overlap but whose boundaries are degenerate (shared edges) either
    /// throw <see cref="NotImplementedException"/> (<paramref name="throwOnDegenerateOverlap"/>)
    /// or are kept as separate, overlapping members.
    /// </summary>
    private static Geometry<T> UnionPolygonPieces(List<Geometry<T>> pieces, int srid, bool throwOnDegenerateOverlap)
    {
        var polygons = new List<Geometry<T>>();

        foreach (var piece in pieces)
        {
            if (piece.IsNullOrEmpty())
                continue;

            if (piece.Type == GeometryType.Polygon)
                polygons.Add(piece);
            else if (piece.Type == GeometryType.MultiPolygon)
                polygons.AddRange(piece.Geometries.Where(g => !g.IsNullOrEmpty()));
        }

        if (polygons.Count == 0)
            return Geometry<T>.Empty;

        var merged = new List<Geometry<T>>();
        var pending = new Queue<Geometry<T>>(polygons);

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();

            bool combined = false;

            for (int i = 0; i < merged.Count; i++)
            {
                if (!current.Intersects(merged[i]))
                    continue;

                var union = TryUnionPolygonPair(merged[i], current, srid);

                if (union is null)
                {
                    if (throwOnDegenerateOverlap && HaveInteriorOverlapAtSamplePoints(current, merged[i]))
                        throw new NotImplementedException(
                            "Geometry > UnionPolygonPieces: polygon-polygon union with boundary degeneracies " +
                            "(shared edges while the interiors overlap) is not implemented yet.");

                    continue;
                }

                merged.RemoveAt(i);

                // the union may now overlap other already-merged pieces; re-process it
                pending.Enqueue(union);

                combined = true;
                break;
            }

            if (!combined)
                merged.Add(current);
        }

        if (merged.Count == 1)
            return merged[0];

        return Geometry<T>.Create(merged, GeometryType.MultiPolygon, srid);
    }

    /// <summary>
    /// Greiner–Hormann union of two polygons via their outer rings. Holes are kept only when the
    /// other piece does not reach them. Returns null when the union cannot be computed cleanly
    /// (degenerate or touch-only boundaries) so the caller can keep the pieces separate.
    /// </summary>
    private static Geometry<T> TryUnionPolygonPair(Geometry<T> poly1, Geometry<T> poly2, int srid)
    {
        if (IsPolygonWithinPolygon(poly1, poly2))
            return RemoveHolesTouchedByPiece(poly2, poly1, srid);

        if (IsPolygonWithinPolygon(poly2, poly1))
            return RemoveHolesTouchedByPiece(poly1, poly2, srid);

        var unionRings = ClipRings(poly1.Geometries[0], poly2.Geometries[0], srid, RingClipOperation.Union);

        if (unionRings is null || unionRings.Count == 0)
            return null;

        var rings = new List<Geometry<T>>(unionRings);

        AppendSurvivingHoles(rings, poly1, poly2, srid);
        AppendSurvivingHoles(rings, poly2, poly1, srid);

        return CreatePolygonOrMultiPolygon(rings, srid);
    }

    /// <summary>
    /// Returns <paramref name="container"/> without the holes that the contained piece reaches.
    /// </summary>
    private static Geometry<T> RemoveHolesTouchedByPiece(Geometry<T> container, Geometry<T> piece, int srid)
    {
        var rings = new List<Geometry<T>> { container.Geometries[0].Clone() };

        AppendSurvivingHoles(rings, container, piece, srid);

        return CreatePolygonOrMultiPolygon(rings, srid);
    }

    private static void AppendSurvivingHoles(List<Geometry<T>> rings, Geometry<T> holeOwner, Geometry<T> other, int srid)
    {
        foreach (var hole in holeOwner.Geometries.Skip(1))
        {
            var holeAsPolygon = Geometry<T>.Create(new List<Geometry<T>> { hole.Clone() }, GeometryType.Polygon, srid);

            if (!other.Intersects(holeAsPolygon))
                rings.Add(hole.Clone());
        }
    }

    /// <summary>
    /// Creates a circle polygon around a center point
    /// </summary>
    private static Geometry<T> CreateCircle(T center, double radius, int segments, int srid)
    {
        var points = new List<T>();
        double angleStep = 2.0 * Math.PI / segments;

        for (int i = 0; i < segments; i++)
        {
            double angle = i * angleStep;
            double x = center.X + radius * Math.Cos(angle);
            double y = center.Y + radius * Math.Sin(angle);
            points.Add(new T() { X = x, Y = y });
        }

        return Geometry<T>.Create(points, GeometryType.Polygon, srid);
    }

    /// <summary>
    /// Appends the points of a circular arc around <paramref name="center"/> to
    /// <paramref name="points"/> (excluding the start angle, including the end angle).
    /// A negative sweep runs clockwise.
    /// </summary>
    private static void AddArcPoints(List<T> points, T center, double radius, double startAngle, double sweep)
    {
        int segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(sweep) / (Math.PI / 16.0)));

        for (int k = 1; k <= segments; k++)
        {
            double angle = startAngle + sweep * k / segments;

            points.Add(new T() { X = center.X + radius * Math.Cos(angle), Y = center.Y + radius * Math.Sin(angle) });
        }
    }

    private static List<T> RemoveConsecutiveDuplicatePoints(List<T> points, bool isRing)
    {
        var result = new List<T>(points?.Count ?? 0);

        if (points is null)
            return result;

        foreach (var point in points)
        {
            if (result.Count == 0 || !ArePointsAlmostEqual(result[result.Count - 1], point))
                result.Add(point);
        }

        while (isRing && result.Count > 1 && ArePointsAlmostEqual(result[0], result[result.Count - 1]))
            result.RemoveAt(result.Count - 1);

        return result;
    }

    private static (double X, double Y) GetUnitDirection(T from, T to)
    {
        double dx = to.X - from.X;
        double dy = to.Y - from.Y;

        double length = Math.Sqrt(dx * dx + dy * dy);

        return (dx / length, dy / length);
    }

    /// <summary>
    /// Offsets an open polyline or a closed ring to the LEFT of the traversal direction by
    /// <paramref name="distance"/> (a negative distance offsets to the right). Vertices that are
    /// convex on the offset side get round joins (circular arcs), SQL Server style.
    /// </summary>
    private static List<T> OffsetLineSegment(List<T> points, double distance, bool isClosed)
    {
        var cleaned = RemoveConsecutiveDuplicatePoints(points, isRing: isClosed);

        int count = cleaned.Count;

        if (count < 2 || (isClosed && count < 3))
            return new List<T>();

        int segmentCount = isClosed ? count : count - 1;

        var directions = new (double X, double Y)[segmentCount];

        for (int i = 0; i < segmentCount; i++)
            directions[i] = GetUnitDirection(cleaned[i], cleaned[(i + 1) % count]);

        var result = new List<T>();

        double radius = Math.Abs(distance);

        T OffsetBy((double X, double Y) direction, T point)
            => new T() { X = point.X - direction.Y * distance, Y = point.Y + direction.X * distance };

        if (!isClosed)
            result.Add(OffsetBy(directions[0], cleaned[0]));

        int firstJoinVertex = isClosed ? 0 : 1;
        int lastJoinVertexExclusive = isClosed ? count : count - 1;

        for (int i = firstJoinVertex; i < lastJoinVertexExclusive; i++)
        {
            var incoming = directions[(i - 1 + segmentCount) % segmentCount];
            var outgoing = directions[i];

            var vertex = cleaned[i];

            double cross = incoming.X * outgoing.Y - incoming.Y * outgoing.X;
            double dot = incoming.X * outgoing.X + incoming.Y * outgoing.Y;

            if (cross * distance < 0)
            {
                // vertex is convex on the offset side: insert a round join
                var incomingOffset = OffsetBy(incoming, vertex);

                result.Add(incomingOffset);

                double startAngle = Math.Atan2(incomingOffset.Y - vertex.Y, incomingOffset.X - vertex.X);
                double sweep = Math.Atan2(cross, dot);

                AddArcPoints(result, vertex, radius, startAngle, sweep);
            }
            else if (1 + dot >= 0.125) // miter length ≤ 4·|distance|
            {
                // reflex side (or straight-through): the exact offset is the miter point,
                // the intersection of the two adjacent offset lines
                double scale = distance / (1 + dot);

                result.Add(new T()
                {
                    X = vertex.X - (incoming.Y + outgoing.Y) * scale,
                    Y = vertex.Y + (incoming.X + outgoing.X) * scale,
                });
            }
            else
            {
                // near U-turn: fall back to a bevel to avoid a miter spike
                result.Add(OffsetBy(incoming, vertex));
                result.Add(OffsetBy(outgoing, vertex));
            }
        }

        if (!isClosed)
            result.Add(OffsetBy(directions[segmentCount - 1], cleaned[count - 1]));

        return RemoveConsecutiveDuplicatePoints(result, isRing: isClosed);
    }

    /// <summary>
    /// Cheap pre-check for hole elimination: the hole collapses when the buffer distance reaches
    /// its approximate half-width (minimum distance from the mean point to the ring edges).
    /// The definitive check is the orientation flip of the offset ring in <see cref="BufferPolygon"/>.
    /// </summary>
    private static bool ShouldEliminateHole(Geometry<T> hole, double bufferDistance)
    {
        if (hole.IsNullOrEmpty() || hole.Points == null || hole.Points.Count < 3)
            return true;

        var center = hole.GetMeanPoint();

        double minDistance = double.MaxValue;

        var points = hole.Points;
        int count = points.Count;

        for (int i = 0; i < count; i++)
        {
            double dist = TopologyUtility.GetPointToLineSegmentDistance(center, points[i], points[(i + 1) % count]);
            if (dist < minDistance)
                minDistance = dist;
        }

        return bufferDistance >= minDistance;
    }

    #region Geodesic Buffer Methods

    private Geometry<T> BufferPointGeodesic(double distance)
    {
        var center = this.Points[0];
        var circlePoints = SpatialUtility.CreateCircleGeodesic<T>(center, distance, 64);

        return Geometry<T>.Create(circlePoints, GeometryType.Polygon, this.Srid);
    }

    private Geometry<T> BufferLineStringGeodesic(double distance)
    {
        var points = RemoveConsecutiveDuplicatePoints(this.Points, isRing: false);

        if (points.Count == 0)
            return Geometry<T>.Empty;

        // a degenerate (zero-length) line string buffers like a point
        if (points.Count == 1)
            return Geometry<T>.Create(SpatialUtility.CreateCircleGeodesic<T>(points[0], distance, 64), GeometryType.Polygon, this.Srid);

        // a closed line string buffers like a ring (no end caps): outer offset + inner hole
        if (points.Count >= 4 && ArePointsAlmostEqual(points[0], points[points.Count - 1]))
        {
            var ringPoints = RemoveConsecutiveDuplicatePoints(points, isRing: true);

            if (ringPoints.Count >= 3)
                return BufferClosedPolyline(ringPoints, distance, offsetsLeft: false, OffsetLineSegmentGeodesic);
        }

        // bearings are compass style: bearing + π/2 is the RIGHT side of the travel direction,
        // so the left offset uses the negative distance
        var leftOffset = OffsetLineSegmentGeodesic(points, -distance, false);
        var rightOffset = OffsetLineSegmentGeodesic(points, distance, false);

        if (leftOffset.Count < 2 || rightOffset.Count < 2)
            return Geometry<T>.Empty;

        // boundary: left side forward → end cap → right side backward → start cap
        var boundary = new List<T>(leftOffset);

        // end cap: sweep from the left side through the forward bearing to the right side
        double endForward = SpatialUtility.GetBearingGeodesic(points[points.Count - 2], points[points.Count - 1]);
        AddBearingArcPoints(boundary, points[points.Count - 1], distance, endForward - Math.PI / 2.0, Math.PI, SpatialUtility.MovePointAlongGeodesic);

        for (int i = rightOffset.Count - 1; i >= 0; i--)
            boundary.Add(rightOffset[i]);

        // start cap: sweep from the right side through the backward bearing to the left side
        double startForward = SpatialUtility.GetBearingGeodesic(points[0], points[1]);
        AddBearingArcPoints(boundary, points[0], distance, startForward + Math.PI / 2.0, Math.PI, SpatialUtility.MovePointAlongGeodesic);

        boundary = RemoveConsecutiveDuplicatePoints(boundary, isRing: true);

        if (boundary.Count < 3)
            return Geometry<T>.Empty;

        return Geometry<T>.CreatePolygon(boundary, this.Srid);
    }

    private Geometry<T> BufferPolygonGeodesic(double distance)
    {
        if (this.Geometries == null || this.Geometries.Count == 0)
            return Geometry<T>.Empty;

        var polygon = AsOgcOrientedPolygon();

        // exterior ring is CCW (OGC SFA); with the compass-bearing convention (+π/2 = right side)
        // a positive distance moves it outward
        var bufferedExterior = OffsetLineSegmentGeodesic(polygon.Geometries[0].Points, distance, true);

        if (bufferedExterior.Count < 3)
            return Geometry<T>.Empty;

        var bufferedRings = new List<Geometry<T>>
        {
            Geometry<T>.Create(bufferedExterior, GeometryType.LineString, this.Srid),
        };

        // holes are CW, so the same positive distance moves them into the hole (the hole shrinks)
        for (int i = 1; i < polygon.Geometries.Count; i++)
        {
            var hole = polygon.Geometries[i];

            if (ShouldEliminateHoleGeodesic(hole, distance))
                continue;

            var bufferedHole = OffsetLineSegmentGeodesic(hole.Points, distance, true);

            if (bufferedHole.Count < 3)
                continue;

            // an offset hole whose orientation flipped (CW → CCW) has collapsed
            if (SpatialUtility.GetSignedEuclideanArea(bufferedHole) >= 0)
                continue;

            if (TopologyUtility.IsPointInRing(bufferedRings[0], bufferedHole[0]))
                bufferedRings.Add(Geometry<T>.Create(bufferedHole, GeometryType.LineString, this.Srid));
        }

        return CreatePolygonOrMultiPolygon(bufferedRings, this.Srid);
    }

    private Geometry<T> BufferMultiPointGeodesic(double distance)
        => UnionBufferPieces(this.Geometries.Select(g => g.BufferPointGeodesic(distance)).ToList(), this.Srid);

    private Geometry<T> BufferMultiLineStringGeodesic(double distance)
        => UnionBufferPieces(this.Geometries.Select(g => g.BufferLineStringGeodesic(distance)).ToList(), this.Srid);

    private Geometry<T> BufferMultiPolygonGeodesic(double distance)
        => UnionBufferPieces(this.Geometries.Select(g => g.BufferPolygonGeodesic(distance)).ToList(), this.Srid);

    /// <summary>
    /// Appends the points of a bearing-swept arc around <paramref name="center"/> (excluding the
    /// start bearing, including the end bearing), using the supplied geodesic/spherical mover.
    /// Bearings are compass style, so a positive sweep runs clockwise in map view.
    /// </summary>
    private static void AddBearingArcPoints(List<T> points, T center, double radius, double startBearing, double sweep, Func<T, double, double, T> movePoint)
    {
        int segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(sweep) / (Math.PI / 16.0)));

        for (int k = 1; k <= segments; k++)
        {
            double bearing = startBearing + sweep * k / segments;

            points.Add(movePoint(center, bearing, radius));
        }
    }

    private List<T> OffsetLineSegmentGeodesic(List<T> points, double distance, bool isClosed)
    {
        if (points == null || points.Count < 2)
            return new List<T>();

        var offsetPoints = new List<T>();
        int count = points.Count;

        for (int i = 0; i < count; i++)
        {
            T prev, curr, next;

            if (isClosed)
            {
                prev = points[(i - 1 + count) % count];
                curr = points[i];
                next = points[(i + 1) % count];
            }
            else
            {
                if (i == 0)
                {
                    curr = points[i];
                    next = points[i + 1];
                    double bearing = SpatialUtility.GetBearingGeodesic(curr, next);
                    double perpBearing = bearing + Math.PI / 2.0; // Perpendicular bearing (left side)
                    double absDistance = Math.Abs(distance);
                    double finalBearing = distance < 0 ? perpBearing + Math.PI : perpBearing; // Right side for negative distance
                    var offsetPoint = SpatialUtility.MovePointAlongGeodesic(curr, finalBearing, absDistance);
                    offsetPoints.Add(offsetPoint);
                    continue;
                }
                else if (i == count - 1)
                {
                    prev = points[i - 1];
                    curr = points[i];
                    double bearing = SpatialUtility.GetBearingGeodesic(prev, curr);
                    double perpBearing = bearing + Math.PI / 2.0;
                    double absDistance = Math.Abs(distance);
                    double finalBearing = distance < 0 ? perpBearing + Math.PI : perpBearing;
                    var offsetPoint = SpatialUtility.MovePointAlongGeodesic(curr, finalBearing, absDistance);
                    offsetPoints.Add(offsetPoint);
                    continue;
                }
                else
                {
                    prev = points[i - 1];
                    curr = points[i];
                    next = points[i + 1];
                }
            }

            var offsetPoint2 = OffsetPointGeodesic(curr, prev, next, distance);
            offsetPoints.Add(offsetPoint2);
        }

        return offsetPoints;
    }

    private static T OffsetPointGeodesic(T point, T prevPoint, T nextPoint, double distance)
    {
        double bearing1 = SpatialUtility.GetBearingGeodesic(prevPoint, point);
        double bearing2 = SpatialUtility.GetBearingGeodesic(point, nextPoint);

        double perpBearing1 = bearing1 + Math.PI / 2.0;
        double perpBearing2 = bearing2 + Math.PI / 2.0;

        double avgBearing = GetAverageBearing(perpBearing1, perpBearing2);

        double absDistance = Math.Abs(distance);
        double finalBearing = distance < 0 ? avgBearing + Math.PI : avgBearing; // Left side for negative distance

        return SpatialUtility.MovePointAlongGeodesic(point, finalBearing, absDistance);
    }

    /// <summary>
    /// Averages two compass bearings via their unit vectors, so bearings straddling north
    /// (0/2π) do not average to the opposite direction.
    /// </summary>
    private static double GetAverageBearing(double bearing1, double bearing2)
    {
        double east = Math.Sin(bearing1) + Math.Sin(bearing2);
        double north = Math.Cos(bearing1) + Math.Cos(bearing2);

        // opposite bearings (U-turn): fall back to the first one
        if (Math.Sqrt(east * east + north * north) < SpatialUtility.EpsilonDistance)
            return bearing1;

        return Math.Atan2(east, north);
    }

    private static bool ShouldEliminateHoleGeodesic(Geometry<T> hole, double bufferDistance)
    {
        if (hole.IsNullOrEmpty() || hole.Points == null || hole.Points.Count < 3)
            return true;

        var center = hole.GetMeanPoint();
        double minDistance = double.MaxValue;
        foreach (var point in hole.Points)
        {
            double dist = SpatialUtility.GetEllipsoidalLength(center, point);
            if (dist < minDistance)
                minDistance = dist;
        }

        return bufferDistance >= minDistance;
    }

    #endregion

    #region Spherical Buffer Methods

    private Geometry<T> BufferPointSpherical(double distance)
    {
        var center = this.Points[0];
        var circlePoints = SpatialUtility.CreateCircleSpherical<T>(center, distance, 64);
        return Geometry<T>.Create(circlePoints, GeometryType.Polygon, this.Srid);
    }

    private Geometry<T> BufferLineStringSpherical(double distance)
    {
        var points = RemoveConsecutiveDuplicatePoints(this.Points, isRing: false);

        if (points.Count == 0)
            return Geometry<T>.Empty;

        // a degenerate (zero-length) line string buffers like a point
        if (points.Count == 1)
            return Geometry<T>.Create(SpatialUtility.CreateCircleSpherical<T>(points[0], distance, 64), GeometryType.Polygon, this.Srid);

        // a closed line string buffers like a ring (no end caps): outer offset + inner hole
        if (points.Count >= 4 && ArePointsAlmostEqual(points[0], points[points.Count - 1]))
        {
            var ringPoints = RemoveConsecutiveDuplicatePoints(points, isRing: true);

            if (ringPoints.Count >= 3)
                return BufferClosedPolyline(ringPoints, distance, offsetsLeft: false, OffsetLineSegmentSpherical);
        }

        // bearings are compass style: bearing + π/2 is the RIGHT side of the travel direction,
        // so the left offset uses the negative distance
        var leftOffset = OffsetLineSegmentSpherical(points, -distance, false);
        var rightOffset = OffsetLineSegmentSpherical(points, distance, false);

        if (leftOffset.Count < 2 || rightOffset.Count < 2)
            return Geometry<T>.Empty;

        // boundary: left side forward → end cap → right side backward → start cap
        var boundary = new List<T>(leftOffset);

        // end cap: sweep from the left side through the forward bearing to the right side
        double endForward = SpatialUtility.GetBearingSpherical(points[points.Count - 2], points[points.Count - 1]);
        AddBearingArcPoints(boundary, points[points.Count - 1], distance, endForward - Math.PI / 2.0, Math.PI, SpatialUtility.MovePointAlongSpherical);

        for (int i = rightOffset.Count - 1; i >= 0; i--)
            boundary.Add(rightOffset[i]);

        // start cap: sweep from the right side through the backward bearing to the left side
        double startForward = SpatialUtility.GetBearingSpherical(points[0], points[1]);
        AddBearingArcPoints(boundary, points[0], distance, startForward + Math.PI / 2.0, Math.PI, SpatialUtility.MovePointAlongSpherical);

        boundary = RemoveConsecutiveDuplicatePoints(boundary, isRing: true);

        if (boundary.Count < 3)
            return Geometry<T>.Empty;

        return Geometry<T>.CreatePolygon(boundary, this.Srid);
    }

    private Geometry<T> BufferPolygonSpherical(double distance)
    {
        if (this.Geometries == null || this.Geometries.Count == 0)
            return Geometry<T>.Empty;

        var polygon = AsOgcOrientedPolygon();

        // exterior ring is CCW (OGC SFA); with the compass-bearing convention (+π/2 = right side)
        // a positive distance moves it outward
        var bufferedExterior = OffsetLineSegmentSpherical(polygon.Geometries[0].Points, distance, true);

        if (bufferedExterior.Count < 3)
            return Geometry<T>.Empty;

        var bufferedRings = new List<Geometry<T>>
        {
            Geometry<T>.Create(bufferedExterior, GeometryType.LineString, this.Srid),
        };

        // holes are CW, so the same positive distance moves them into the hole (the hole shrinks)
        for (int i = 1; i < polygon.Geometries.Count; i++)
        {
            var hole = polygon.Geometries[i];

            if (ShouldEliminateHoleSpherical(hole, distance))
                continue;

            var bufferedHole = OffsetLineSegmentSpherical(hole.Points, distance, true);

            if (bufferedHole.Count < 3)
                continue;

            // an offset hole whose orientation flipped (CW → CCW) has collapsed
            if (SpatialUtility.GetSignedEuclideanArea(bufferedHole) >= 0)
                continue;

            if (TopologyUtility.IsPointInRing(bufferedRings[0], bufferedHole[0]))
                bufferedRings.Add(Geometry<T>.Create(bufferedHole, GeometryType.LineString, this.Srid));
        }

        return CreatePolygonOrMultiPolygon(bufferedRings, this.Srid);
    }

    private Geometry<T> BufferMultiPointSpherical(double distance)
        => UnionBufferPieces(this.Geometries.Select(g => g.BufferPointSpherical(distance)).ToList(), this.Srid);

    private Geometry<T> BufferMultiLineStringSpherical(double distance)
        => UnionBufferPieces(this.Geometries.Select(g => g.BufferLineStringSpherical(distance)).ToList(), this.Srid);

    private Geometry<T> BufferMultiPolygonSpherical(double distance)
        => UnionBufferPieces(this.Geometries.Select(g => g.BufferPolygonSpherical(distance)).ToList(), this.Srid);

    private List<T> OffsetLineSegmentSpherical(List<T> points, double distance, bool isClosed)
    {
        if (points == null || points.Count < 2)
            return new List<T>();

        var offsetPoints = new List<T>();
        int count = points.Count;

        for (int i = 0; i < count; i++)
        {
            T prev, curr, next;

            if (isClosed)
            {
                prev = points[(i - 1 + count) % count];
                curr = points[i];
                next = points[(i + 1) % count];
            }
            else
            {
                if (i == 0)
                {
                    curr = points[i];
                    next = points[i + 1];
                    double bearing = SpatialUtility.GetBearingSpherical(curr, next);
                    double perpBearing = bearing + Math.PI / 2.0;
                    double absDistance = Math.Abs(distance);
                    double finalBearing = distance < 0 ? perpBearing + Math.PI : perpBearing;
                    var offsetPoint = SpatialUtility.MovePointAlongSpherical(curr, finalBearing, absDistance);
                    offsetPoints.Add(offsetPoint);
                    continue;
                }
                else if (i == count - 1)
                {
                    prev = points[i - 1];
                    curr = points[i];
                    double bearing = SpatialUtility.GetBearingSpherical(prev, curr);
                    double perpBearing = bearing + Math.PI / 2.0;
                    double absDistance = Math.Abs(distance);
                    double finalBearing = distance < 0 ? perpBearing + Math.PI : perpBearing;
                    var offsetPoint = SpatialUtility.MovePointAlongSpherical(curr, finalBearing, absDistance);
                    offsetPoints.Add(offsetPoint);
                    continue;
                }
                else
                {
                    prev = points[i - 1];
                    curr = points[i];
                    next = points[i + 1];
                }
            }

            var offsetPoint2 = OffsetPointSpherical(curr, prev, next, distance);
            offsetPoints.Add(offsetPoint2);
        }

        return offsetPoints;
    }

    private static T OffsetPointSpherical(T point, T prevPoint, T nextPoint, double distance)
    {
        double bearing1 = SpatialUtility.GetBearingSpherical(prevPoint, point);
        double bearing2 = SpatialUtility.GetBearingSpherical(point, nextPoint);

        double perpBearing1 = bearing1 + Math.PI / 2.0;
        double perpBearing2 = bearing2 + Math.PI / 2.0;

        double avgBearing = GetAverageBearing(perpBearing1, perpBearing2);

        double absDistance = Math.Abs(distance);
        double finalBearing = distance < 0 ? avgBearing + Math.PI : avgBearing;

        return SpatialUtility.MovePointAlongSpherical(point, finalBearing, absDistance);
    }

    private static bool ShouldEliminateHoleSpherical(Geometry<T> hole, double bufferDistance)
    {
        if (hole.IsNullOrEmpty() || hole.Points == null || hole.Points.Count < 3)
            return true;

        var center = hole.GetMeanPoint();
        double minDistance = double.MaxValue;
        foreach (var point in hole.Points)
        {
            double dist = SpatialUtility.GetSphericalLength(center, point);
            if (dist < minDistance)
                minDistance = dist;
        }

        return bufferDistance >= minDistance;
    }

    #endregion

    #endregion


    #region Simplification Measures
    // ref: McMaster, R. B. (1986). A statistical analysis of mathematical measures for linear simplification. The American Cartographer, 13(2), 103-116.


    // 1401.03.12
    public double CalculateTotalVectorDisplacement(Geometry<T> simplified)
    {
        return CalculateTotalVectorDisplacement(simplified, this.IsRingBase());
    }

    private double CalculateTotalVectorDisplacement(Geometry<T> simplified, bool isRingBase)
    {
        if (!this.HasTheSameSignature(simplified))
            throw new NotImplementedException("Geometry > CalculateTotalVectorDisplacement");
        // return double.PositiveInfinity;

        switch (this.Type)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                throw new NotImplementedException("Geometry > CalculateTotalVectorDisplacement");

            case GeometryType.LineString:
                return SpatialUtility.GetTotalVectorDisplacement(this.GetAllPoints(), simplified.GetAllPoints(), isRingBase);

            case GeometryType.Polygon:
                return this.Geometries.Zip(simplified.Geometries, (g1, g2) => g1.CalculateTotalVectorDisplacement(g2, isRingBase)).Sum();

            case GeometryType.MultiLineString:
                return this.Geometries.Zip(simplified.Geometries, (g1, g2) => g1.CalculateTotalVectorDisplacement(g2, isRingBase)).Sum();

            case GeometryType.MultiPolygon:
                return this.Geometries.Zip(simplified.Geometries, (g1, g2) => g1.CalculateTotalVectorDisplacement(g2, isRingBase)).Sum();

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry > CalculateTotalVectorDisplacement");
        }
    }

    // 1401.03.12
    public double CalculateTotalVectorDisplacementPerLength(Geometry<T> simplified)
    {
        var length = GetEuclideanLength();

        if (length != 0)
            return CalculateTotalVectorDisplacement(simplified) / length;

        return 0;
    }

    // 1401.03.12
    // PCLL
    public double PercentageChangeInLineLength(Geometry<T> simplified)
    {
        if (simplified.IsNullOrEmpty())
            return 1.0;

        var length = this.GetEuclideanLength();

        if (length == 0)
            return 1.0;

        return simplified.GetEuclideanLength() / length;
    }

    // 1401.03.12
    // PCC
    /// <summary>
    /// PCC (%): No of points in simplified geometry / No of points in original geometry
    /// </summary>
    /// <param name="simplified"></param>
    /// <returns>PCC in percent. between 0 and 1</returns>
    public double PercentageChangeInCoordinates(Geometry<T> simplified)
    {
        if (simplified.IsNullOrEmpty())
            return 1.0;

        var totalNumberOfPoints = (double)this.TotalNumberOfPoints;

        if (totalNumberOfPoints == 0)
            return 1.0;

        return simplified.TotalNumberOfPoints / totalNumberOfPoints;
    }

    public double Compression(Geometry<T> simplified) => 1 - PercentageChangeInCoordinates(simplified);

    // 1401.03.12
    // PDD
    public double PercentageChangeInPointDensity(Geometry<T> simplified)
    {
        if (simplified.IsNullOrEmpty())
            return 1.0;

        var density = this.CalculatePointDensity();

        if (density == 0)
            return 1.0;

        // 1402.10.03
        // it should be noted that the result value maybe negative
        // that means simplification may increase the density
        return density - simplified.CalculatePointDensity();
    }

    // 1401.03.12
    // PCANGLE
    public double PercentageChangeInAngularity(Geometry<T> simplified)
    {
        if (simplified.IsNullOrEmpty())
            return 1.0;

        var meanAngularChange = this.CalculateMeanAngularChange();

        if (meanAngularChange == 0)
            return 1.0;

        return simplified.CalculateMeanAngularChange() / meanAngularChange;
    }

    // 1401.03.12
    // PCCS
    public double PercentageChangeInCurvilinearSegments(Geometry<T> simplified)
    {
        if (simplified.IsNullOrEmpty())
            return 1.0;

        var numerOfCurvilinearityChange = this.GetNumerOfCurvilinearityChange();

        if (numerOfCurvilinearityChange == 0)
            return 1.0;

        return simplified.GetNumerOfCurvilinearityChange() / numerOfCurvilinearityChange;
    }

    // 1401.03.15
    /// <summary>
    /// SD for Simplificed Segment Lengths / SD for Original Segment Lengths
    /// </summary>
    /// <param name="simplified"></param>
    /// <returns></returns>
    public double PercentageChangeInSegmentLengthVariations(Geometry<T> simplified)
    {
        if (simplified.IsNullOrEmpty())
            return 1.0;

        var segmentLengthSD = this.CalculateSegmentLengthVariations();

        if (segmentLengthSD == 0)
            return 1.0;

        return simplified.CalculateSegmentLengthVariations() / segmentLengthSD;
    }

    #endregion


    #region Geometry Manipulation

    public List<T> GetAllPoints()
    {
        if (Points != null)
        {
            return Points;
        }
        else if (Geometries != null)
        {
            return Geometries.SelectMany(i => i.GetAllPoints()).ToList();
        }
        else
        {
            return new List<T>();
        }
    }

    public T GetLastPoint()
    {
        if (Points?.Any() == true)
        {
            return Points.Last();
        }
        else if (Geometries?.Count > 0)
        {
            return Geometries.Last().GetLastPoint();
        }
        else
        {
            return NullPoint;
        }
    }

    public T? GetMeanOrLastPoint()
    {
        switch (this.Type)
        {
            case GeometryType.Point:
                return this.AsPoint();

            case GeometryType.LineString:
            case GeometryType.MultiLineString:
                return this.GetLastPoint();

            case GeometryType.Polygon:
            case GeometryType.MultiPolygon:
                return this.GetMeanPoint();

            case GeometryType.MultiPoint:
            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }

    }

    public void StartNewGeometry(T startPoint, GeometryType type)
    {
        if (this.Geometries == null)
            throw new NotImplementedException();

        var geometries = this.Geometries.ToList();

        geometries.Add(Geometry<T>.Create([startPoint], type, this.Srid));

        this.Geometries = geometries.ToList();
    }

    public Geometry<T> Clone()
    {
        if (this.Points != null)
        {
            List<T> points = new List<T>(this.Points.Count);

            for (int i = 0; i < this.Points.Count; i++)
            {
                //points[i] = new T() { X = this.Points[i].X, Y = this.Points[i].Y };
                points.Add(new T() { X = this.Points[i].X, Y = this.Points[i].Y });
            }

            return Geometry<T>.Create(points, this.Type, this.Srid);
        }
        if (this.Geometries != null)
        {
            return Geometry<T>.Create(this.Geometries.Select(g => g.Clone()).ToList(), this.Type, this.Srid);
        }

        return Geometry<T>.CreateEmpty(this.Type, this.Srid);
    }

    //public Geometry<T> NeutralizeGenericPoint()
    //{
    //    if (this.Points != null)
    //    {
    //        List<T> points = new List<T>(this.Points.Count);

    //        for (int i = 0; i < this.Points.Count; i++)
    //        {
    //            points.Add(new T() { X = this.Points[i].X, Y = this.Points[i].Y });
    //        }

    //        return Geometry<T>.Create(points, this.Type, this.Srid);
    //    }
    //    if (this.Geometries != null)
    //    {
    //        return new Geometry<T>(this.Geometries.Select(g => g.NeutralizeGenericPoint()).ToList(), this.Type, this.Srid);
    //    }

    //    return new Geometry<T>(null, this.Type, false, this.Srid);
    //}

    //This method can better using Array.Resize()
    public void InsertLastPoint(T newPoint)
    {
        if (this.Points != null)
        {
            var points = this.Points.ToList();

            points.Add(newPoint);

            this.Points = points.ToList();
        }
        else
        {
            this.Geometries.Last().InsertLastPoint(newPoint);
        }
    }

    //public void InsertPoint(T newPoint, int index)
    //{
    //    var points = this.Points.ToList();

    //    points.Insert(index, newPoint);

    //    this.Points = points.ToList();
    //}
    public void InsertPoint(T newPoint, int index)
    {
        if (this.Points == null)
            throw new InvalidOperationException($"Cannot insert point into geometry of type {this.Type}");

        if (index < 0 || index > this.Points.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        // Insert directly
        this.Points.Insert(index, newPoint);
    }

    public void Remove(T point)
    {
        var list = this.Points.ToList();

        list.Remove(point);

        this.Points = list.ToList();
    }

    public void Remove(double x, double y)
    {
        var list = this.Points.ToList();

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].X == x && list[i].Y == y)
            {
                list.Remove(list[i]);

                break;
            }
        }

        this.Points = list.ToList();
    }

    public List<T> GetLastPart()
    {
        if (this.IsNullOrEmpty())
            return new List<T>();

        if (this.IsLeafGeometry())
        {
            return this.Points ?? new List<T>();
        }
        else
        {
            return this.Geometries.Last().GetLastPart();
        }

        //1399.07.17
        //if (this.Points == null && this.Geometries == null)
        //    return null;

        //if (this.Points == null)
        //{
        //    return this.Geometries.Last().GetLastPart();
        //}
        //else
        //{
        //    return this.Points;
        //}
    }

    //Returns last geometry ring or point
    public Geometry<T> GetLastGeometryPart()
    {
        if (this.Points != null)
        {
            return this;
        }
        else if (this.Geometries != null)
        {
            if (this.Geometries.Count > 0)
            {
                return this.Geometries.Last().GetLastGeometryPart();
            }
        }

        return null;
    }

    public void ClearEmptyGeometries()
    {
        if (Geometries == null)
        {
            return;
        }

        for (int i = Geometries.Count - 1; i >= 0; i--)
        {
            if (Geometries[i].HasAnyPoint())
            {
                Geometries[i].ClearEmptyGeometries();
            }
            else
            {
                this.Geometries.RemoveAt(i);
            }
        }
    }

    public Geometry<T> GetRingOrLineStringPassingPoint(double x, double y)
    {
        int index;

        switch (this.Type)
        {
            case GeometryType.LineString:
                if (TryFind(this.Points, x, y, out index))
                {
                    return this;
                }

                return null;

            case GeometryType.Polygon:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                for (int i = 0; i < this.Geometries.Count; i++)
                {
                    var geo = this.Geometries[i].GetRingOrLineStringPassingPoint(x, y);

                    if (geo != null)
                    {
                        return geo;
                    }
                }

                return null;

            case GeometryType.Point:
            case GeometryType.MultiPoint:
            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }
    }

    public bool TryAddNewPart()
    {
        var currentGeometry = this.Clone();

        //e.g. having a LineString with just one point we cannot convert to MultilineString
        if (!currentGeometry.IsValid()) return false;

        switch (this.Type)
        {
            case GeometryType.Point:
                this.Points = null;
                this.Geometries = [currentGeometry, CreateEmpty(GeometryType.Point, this.Srid)];
                this.Type = GeometryType.MultiPoint;
                break;

            case GeometryType.LineString:
                this.Points = null;
                this.Geometries = [currentGeometry, CreateEmpty(GeometryType.LineString, this.Srid)];
                this.Type = GeometryType.MultiLineString;
                break;

            case GeometryType.Polygon:
                this.Geometries =
                [
                    currentGeometry,
                    CreateNew(GeometryType.Polygon, this.Srid),
                ];
                this.Type = GeometryType.MultiPolygon;
                break;

            case GeometryType.MultiPoint:
                this.Geometries.Add(CreateEmpty(GeometryType.Point, this.Srid));
                break;
            case GeometryType.MultiLineString:
                this.Geometries.Add(CreateEmpty(GeometryType.LineString, this.Srid));
                break;

            case GeometryType.MultiPolygon:
                this.Geometries.Add(CreateNew(GeometryType.Polygon, this.Srid));
                break;

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }

        return true;
    }

    public bool TryAddNewRing()
    {
        if (this.Type != GeometryType.Polygon)
            return false;

        this.Geometries.Add(Geometry<T>.CreateNew(GeometryType.LineString, this.Srid));

        return true;
    }

    public bool TryRemovePart(Geometry<T> geometry)
    {
        if (this.Geometries?.Count > 0)
        {
            for (int i = 0; i < this.Geometries.Count; i++)
            {
                if (this.Geometries[i] == geometry)
                {
                    var temp = this.Geometries.ToList();

                    temp.Remove(geometry);

                    this.Geometries = temp.ToList();

                    return true;
                }
            }
        }

        return false;
    }

    public bool TryRemoveEntireRingOrLineString(double x, double y)
    {
        var part = this.GetRingOrLineStringPassingPoint(x, y);

        if (this.Geometries?.Count > 0)
        {
            for (int i = 0; i < this.Geometries.Count; i++)
            {
                switch (Geometries[i].Type)
                {
                    case GeometryType.LineString:
                        if (this.TryRemovePart(part))
                            return true;
                        break;

                    case GeometryType.Polygon:
                        for (int g = Geometries[i].Geometries.Count - 1; g >= 0; g--)
                        {
                            if (Geometries[i].TryRemovePart(part))
                            {
                                return true;
                            }
                        }
                        break;

                    case GeometryType.MultiLineString:
                    case GeometryType.MultiPoint:
                    case GeometryType.MultiPolygon:
                    case GeometryType.Point:
                    case GeometryType.GeometryCollection:
                    case GeometryType.CircularString:
                    case GeometryType.CompoundCurve:
                    case GeometryType.CurvePolygon:
                    default:
                        throw new NotImplementedException();
                }
            }

            return false;
        }

        return false;
    }

    public List<LineSegment<T>> GetLineSegments()
    {
        switch (this.Type)
        {
            case GeometryType.LineString:
                return GetLineSegments(false);

            case GeometryType.Polygon:
                return Geometries.SelectMany(i => i.GetLineSegments(true)).ToList();

            case GeometryType.MultiLineString:
                return Geometries.SelectMany(i => i.GetLineSegments(false)).ToList();

            case GeometryType.MultiPolygon:
                return Geometries.SelectMany(i => i.GetLineSegments()).ToList();

            case GeometryType.GeometryCollection:
                return Geometries.SelectMany(i => i.GetLineSegments()).ToList();

            case GeometryType.Point:
            case GeometryType.MultiPoint:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }
    }

    private List<LineSegment<T>> GetLineSegments(bool isClosed)
    {
        List<LineSegment<T>> result = new List<LineSegment<T>>();

        if (Points != null)
        {
            for (int i = 0; i < this.Points.Count - 1; i++)
            {
                result.Add(new LineSegment<T>(Points[i], Points[i + 1]));
            }

            if (isClosed)
            {
                //prevent returning segment when polygon has one point
                if (Points.Count > 1)
                {
                    result.Add(new LineSegment<T>(Points[Points.Count - 1], Points[0]));
                }
            }
        }

        return result;
    }

    public void CloseLineString()
    {
        if (this.Type != GeometryType.LineString)
            return;

        this.Points.Add(this.Points[0]);
    }

    private void Reverse()
    {
        switch (this.Type)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                break;

            case GeometryType.LineString:
                this.Points.Reverse();
                break;

            case GeometryType.Polygon:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                for (int i = 0; i < this.Geometries?.Count; i++)
                {
                    this.Geometries[i].Reverse();
                }
                break;

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry.cs > Reverse");
        }
    }

    /// <summary>
    /// Split geometry into list of points or lineStrings
    /// </summary>
    /// <param name="clone"></param>
    /// <returns></returns>
    public List<Geometry<T>> Split(bool clone)
    {
        switch (Type)
        {
            case GeometryType.Point:
            case GeometryType.LineString:
                return new List<Geometry<T>> { clone ? this.Clone() : this };

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
                return Geometries.Select(g => clone ? g.Clone() : g).ToList();

            case GeometryType.Polygon:
                return SplitPolygon(this, clone);

            case GeometryType.MultiPolygon:
                return Geometries.SelectMany(g => g.Split(clone)).ToList();

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry > Split");
        }
    }

    private static List<Geometry<T>> SplitPolygon(Geometry<T> polygon, bool clone)
    {
        if (polygon.Type != GeometryType.Polygon)
            throw new NotImplementedException("Geometry > SplitPolygon");

        if (polygon.NumberOfGeometries == 1)
            return new List<Geometry<T>>() { clone ? polygon.Clone() : polygon };

        return polygon.Geometries.Select(g => Geometry<T>.CreatePolygonOrMultiPolygon(new List<Geometry<T>>() { clone ? g.Clone() : g }, g.Srid)).ToList();
    }

    public T AsPoint()
    {
        if (this.Type != GeometryType.Point)
        {
            throw new NotImplementedException("Geometry > AsPoint");
        }

        return new T() { X = this.Points[0].X, Y = this.Points[0].Y };
    }

    public Geometry<T>? GetExteriorRing()
    {
        switch (this.Type)
        {
            case GeometryType.Point:
            case GeometryType.LineString:
            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
                return null;

            case GeometryType.Polygon:
                return this.Geometries[0];

            case GeometryType.MultiPolygon:
                return CreatePolygonOrMultiPolygon(this.Geometries.Select(g => g.GetExteriorRing())
                                                                    .Where(g => g is not null)
                                                                    .Select(g => g!)
                                                                    .ToList(), this.Srid);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry > GetExteriorRing");
        }
    }

    public Geometry<T>? GetEnvelope()
    {
        return this.GetBoundingBox().AsGeometry<T>(Srid);
    }

    public Geometry<T>? GetConvexHull()
    {
        var points = this.GetAllPoints().Select(p => new Point(p.X, p.Y)).ToList();

        var hull = ComputationalGeometry.CreateConvexHull(points);

        if (hull.Count == 0)
            return CreateEmpty(GeometryType.Polygon, this.Srid);

        return Create(hull.Select(r => new T() { X = r.X, Y = r.Y }).ToList(), GeometryType.Polygon, this.Srid);
    }

    public Geometry<T> GetBoundary()
    {
        switch (this.Type)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return Empty;

            case GeometryType.LineString:
                if (this.NumberOfPoints < 2)
                    throw new NotImplementedException("Geometry > GetBoundary > linestring at least has two points");

                if (this.Points[0].Equals(this.Points[NumberOfPoints - 1]))
                    return Empty;

                return Create([this.Points[0], this.Points[NumberOfPoints - 1]], GeometryType.MultiPoint, this.Srid);

            case GeometryType.Polygon:
                var points = this.GetExteriorRing().GetAllPoints();
                return Create(points, GeometryType.LineString, this.Srid);

            case GeometryType.MultiLineString:
                return Create(this.Geometries.SelectMany<Geometry<T>, T>(g => [g.Points[0], g.Points[NumberOfPoints - 1]]).ToList(), GeometryType.MultiPoint, this.Srid);

            case GeometryType.MultiPolygon:
                return Geometry<T>.Create(this.Geometries.Select(g => g.GetBoundary()).ToList(), GeometryType.MultiPolygon, this.Srid);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry > GetBoundary > not supported type!");
        }
    }

    public void FixPolygonRingOrientations()
    {
        if (this.IsEmpty())
            return;

        if (this.Type == GeometryType.Polygon)
        {
            if (this.Geometries[0] is null)
                return;

            for (int i = 0; i < this.Geometries.Count; i++)
            {
                // Only the first outter ring is CCW
                var shouldBeClockwise = i != 0;

                var points = this.Geometries[i]?.Points;

                if (points is null)
                    continue;

                if (SpatialUtility.IsClockwise(points) != shouldBeClockwise)
                {
                    this.Geometries[i].Reverse();
                }
            }
        }
        else if (this.Type == GeometryType.MultiPolygon)
        {
            foreach (var item in this.Geometries)
            {
                item.FixPolygonRingOrientations();
            }
        }
    }

    #endregion


    #region Project & Srs

    public SrsBase GetSrs()
    {
        return SridHelper.AsSrsBase(Srid);
    }

    public Geometry<T> Project(SrsBase targetSrs)
    {
        var sourceSrs = GetSrs();

        return Project(sourceSrs, targetSrs);
    }

    public Geometry<T> Project(SrsBase sourceSrs, SrsBase targetSrs)
    {
        if (sourceSrs.Srid == targetSrs.Srid && sourceSrs.Srid != 0)
        {
            return this;
        }
        else if (sourceSrs.Ellipsoid.AreTheSame(targetSrs.Ellipsoid))
        {
            var c1 = this.Transform(p => sourceSrs.ToGeodetic(p), SridHelper.GeodeticWGS84);

            return c1.Transform(p => targetSrs.FromGeodetic(p), targetSrs.Srid);
        }
        else
        {
            var c1 = this.Transform(p => sourceSrs.ToGeodetic(p), SridHelper.GeodeticWGS84);

            return c1.Transform(p => targetSrs.FromGeodetic(p, sourceSrs.Ellipsoid), targetSrs.Srid);
        }
    }


    public Geometry<T> GeodeticToMercator()
    {
        return this.Transform(point => MapProjects.GeodeticToMercator(point, Ellipsoids.WGS84), SridHelper.Mercator);
    }

    public Geometry<T> GeodeticWgs84ToWebMercator()
    {
        return this.Transform(point => MapProjects.GeodeticWgs84ToWebMercator(point), SridHelper.WebMercator);
    }

    public Geometry<T> WebMercatorToGeodeticWgs84()
    {
        return this.Transform(p => MapProjects.WebMercatorToGeodeticWgs84(p), SridHelper.GeodeticWGS84);
    }

    public Geometry<T> GeodeticToCylindricalEqualArea()
    {
        return this.Transform(point => MapProjects.GeodeticToCylindricalEqualArea<T>(point, Ellipsoids.WGS84), SridHelper.CylindricalEqualArea);
    }


    #endregion


    #region Ogc Wkb & Wkt

    public static Geometry<T> FromWkt(string wktString, int srid)
    {
        var result = WktReader.Parse(wktString, srid) as Geometry<T>;

        return result ?? Geometry<T>.Empty;
    }

    public static Geometry<T> FromWkb(byte[] bytes, int srid)
    {
        var result = WkbReader.Parse(bytes, srid) as Geometry<T>;

        return result ?? Geometry<T>.Empty;
    }

    public string AsWkt() => WktWriter.AsWkt(this);

    public string AsWkt(int? coordinateDecimalPlaces) => WktWriter.AsWkt(this, coordinateDecimalPlaces);

    public byte[]? AsWkb() => WkbWriter.AsWkb(this);

    public string AsWkbHexString()
    {
        return IRI.Maptor.Sta.Common.Helpers.HexStringHelper.ToHexStringUsingBitFiddle(AsWkb(), append0x: true);
    }


    public byte[]? AsSqlServerNativeBinary() => SqlServerSpatialNativeBinary.Serialize(this);

    public string AsSqlServerWkt() => SqlServerWktWriter.AsWkt(this);

    public string AsSqlServerWkt(int? coordinateDecimalPlaces) => SqlServerWktWriter.AsWkt(this, coordinateDecimalPlaces);

    #endregion


    #region CSV/TSV

    public string AsDelimited(char delimiter, int precision, bool useThousandSeparator = false)
    {
        if (this.IsNullOrEmpty())
        {
            return string.Empty;
        }

        StringBuilder result = new StringBuilder();

        switch (this.Type)
        {
            case GeometryType.Point:
            case GeometryType.LineString:
                AsDelimited(this.Points, delimiter, precision, useThousandSeparator, result);
                break;

            case GeometryType.Polygon:
            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                AsDelimited(this.Geometries, delimiter, precision, useThousandSeparator, result);
                break;

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            case GeometryType.None:
            default:
                throw new NotImplementedException("Geometry > AsDelimited");
        }

        return result.ToString();
    }

    private void AsDelimited(List<T>? points, char delimiter, int precision, bool useThousandSeparator, StringBuilder sb)
    {
        if (points.IsNullOrEmpty())
            return;

        foreach (var point in points)
        {
            sb.AppendLine(point.AsDelimited(delimiter, precision, useThousandSeparator));
        }
    }

    private void AsDelimited(List<Geometry<T>>? geometries, char delimiter, int precision, bool useThousandSeparator, StringBuilder sb)
    {
        if (geometries.IsNullOrEmpty())
            return;

        foreach (var geometry in geometries)
        {
            sb.AppendLine(geometry.AsDelimited(delimiter, precision, useThousandSeparator));
            sb.AppendLine();
        }
    }

    #endregion


    #region Ogc GML

    // Note: GML conversion methods are implemented as extension methods in IRI.Maptor.Sta.Ogc.Extensions.Sta_GmlExtensions
    // Use geometry.AsGml2() or geometry.AsGml3() extension methods
    // Use Sta_GmlExtensions.FromGml2() or Sta_GmlExtensions.FromGml3() static methods

    #endregion


    #region Sql Server Native Binary

    public byte[]? AsSqlServerByte() => SqlServerSpatialNativeBinary.Serialize(this);



    #endregion


    #region Conversions

    public Feature<T> AsFeature()
    {
        var area = this.EuclideanArea;

        var length = this.GetEuclideanLength();

        return new Feature<T>(this, new Dictionary<string, object>() { { "Area", area }, { "Length", length } });
    }

    public FeatureSet<T> AsFeatureSet()
    {
        return FeatureSet<T>.Create(string.Empty, [this.AsFeature()]);
    }

    public GeoJsonFeatureSet AsGeoJsonFeatureSet()
    {
        return new GeoJsonFeatureSet()
        {
            TotalFeatures = 1,
            Features = new List<GeoJsonFeature>()
            {
                this.AsFeature().AsGeoJsonFeature()
            }
        };
    }

    #endregion


    #region Static Create

    public static Geometry<T> CreateNew(GeometryType type, int srid)
    {
        switch (type)
        {
            case GeometryType.Point:
            case GeometryType.LineString:
                return Geometry<T>.Create(new List<T>(), type, srid);

            case GeometryType.Polygon:
                return Geometry<T>.Create([CreateEmpty(GeometryType.LineString, srid)], GeometryType.Polygon, srid);

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return Geometry<T>.Create(new List<Geometry<T>>(), type, srid);

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }
    }

    public static Geometry<T> CreateEmpty(GeometryType type, int srid)
    {
        switch (type)
        {
            case GeometryType.Polygon:
            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return Geometry<T>.Create(new List<Geometry<T>>(), type, srid);

            case GeometryType.Point:
            case GeometryType.LineString:
                return Geometry<T>.Create(new List<T>(), type, srid);

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }
    }


    public static Geometry<T> Create(double x, double y, int srid = 0)
    {
        return Geometry<T>.Create(new List<T> { new T() { X = x, Y = y } }, GeometryType.Point, srid);
    }

    private static Geometry<T> CreatePoint(List<T> points, int srid)
    {
        return new Geometry<T>()
        {
            Type = GeometryType.Point,
            Srid = srid,
            Points = points,
        };
    }

    private static Geometry<T> CreateLineString(List<T> points, int srid)
    {
        return new Geometry<T>
        {
            Type = GeometryType.LineString,
            Srid = srid,
            Points = points
        };
    }

    private static Geometry<T> CreateMultiPoint(List<T> points, int srid)
    {
        return new Geometry<T>()
        {
            Type = GeometryType.MultiPoint,
            Srid = srid,
            Geometries = points.Select(p => CreatePoint([p], srid)).ToList()
        };
    }

    public static Geometry<T> CreatePolygonRing(List<T> points, int srid)
    {
        var result = new Geometry<T>
        {
            Type = GeometryType.LineString,
            Srid = srid
        };

        if (points?.Count > 1)
        {
            var lastPoint = points[points.Count - 1];

            var firstPoint = points[0];

            // in some cases (e.g. reading KML files) the last point is repeated
            //if (lastPoint.X == firstPoint.X && lastPoint.Y == firstPoint.Y)
            if (firstPoint.HaveTheSameXY(lastPoint))
            {
                result.Points = points.Take(points.Count - 1).ToList();

                return result;
            }
        }

        result.Points = points;

        return result;
    }

    public static Geometry<T> CreatePolygon(List<T> points, int srid)
    {
        var ring = CreatePolygonRing(points, srid);

        // outter ring must be CCW
        if (ring.Points != null && SpatialUtility.IsClockwise(ring.Points))
        {
            ring.Points.Reverse();
        }

        return new Geometry<T>()
        {
            Srid = srid,
            Type = GeometryType.Polygon,
            Geometries = [ring]
        };
    }


    /// <summary>
    /// For Polygons do not repeat first point in the last point
    /// </summary>
    /// <param name="points"></param>
    /// <param name="type"></param>
    /// <param name="srid"></param>
    /// <returns></returns>
    public static Geometry<T> Create(List<T> points, GeometryType type, int srid = 0)
    {
        // do not check empty list, what if we are making a new geometry
        if (points is null)
            return CreateEmpty(type, srid);

        switch (type)
        {
            case GeometryType.Point:
                return CreatePoint(points!, srid);

            case GeometryType.LineString:
                return CreateLineString(points, srid);
            //return new Geometry<T>(points!, GeometryType.LineString, srid);

            case GeometryType.Polygon:
                return CreatePolygon(points, srid);
            //return new Geometry<T>(Geometry<T>.Create(points!, GeometryType.LineString, srid), GeometryType.Polygon, srid);

            case GeometryType.MultiPoint:
                return CreateMultiPoint(points!, srid);

            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }
    }

    public static Geometry<T> CreatePointOrLineString(List<T> points, int srid)
    {
        if (points.Count == 1)
            return CreatePoint(points, srid);

        else
            //return Geometry<T>.Create(points, GeometryType.LineString, srid);
            return CreateLineString(points, srid);
    }

    //public static Geometry<T> CreatePointOrLineString(int srid, params T[] points)
    //{
    //    return CreatePointOrLineString(points.ToList(), srid);
    //}

    public static Geometry<T> CreatePointOrLineStringOrPolygon(List<T> points, int srid)
    {
        if (points.Count > 2 && points[0].HaveTheSameXY(points[points.Count - 1]))

            //return Geometry<T>.Create(points.Take(points.Count - 1).ToList(), GeometryType.Polygon, srid);
            return CreatePolygon(points, srid);

        else
            return CreatePointOrLineString(points, srid);
    }

    public static Geometry<T> CreateLineStringFromPoints(List<Geometry<T>> geometries)
    {
        if (geometries.IsNullOrEmpty())
            return Geometry<T>.Empty;

        var points = geometries.Select(g => g.AsPoint()).ToList();

        return CreatePointOrLineString(points, geometries?.FirstOrDefault()?.Srid ?? 0);
    }

    public static Geometry<T> CreatePolygonOrMultiPolygon(List<Geometry<T>> rings, int srid, bool fixOrientation = true)
    {
        // OGC SFA for polygon:
        // The exterior boundary LinearRing defines the “top” of the surface which is the side of the surface from which the
        // exterior boundary appears to traverse the boundary in a counter clockwise direction. The interior LinearRings will
        // have the opposite orientation, and appear as clockwise when viewed from the “top”,
        if (rings.IsNullOrEmpty())
            return Geometry<T>.CreateEmpty(GeometryType.Polygon, srid);

        if (rings.Count == 1)
            return Create(rings, GeometryType.Polygon, srid);

        var orderedRings = rings.Select(p => (area: p.EuclideanArea, geo: p)).OrderByDescending(i => i.area).ToList();

        var ringBboxes = orderedRings.Select(r => BoundingBox.CalculateBoundingBox(r.geo.GetAllPoints())).ToList();

        var masterPolygons = new List<Geometry<T>>();
        var masterOuterBboxes = new List<BoundingBox>();
        var masterHoleBboxes = new List<List<BoundingBox>>();

        for (int i = 0; i < orderedRings.Count; i++)
        {
            var currentRing = orderedRings[i].geo;

            if (currentRing.NumberOfPoints < 3)
                continue;

            bool isMasterRing = true;

            var testPoint = currentRing.Points.First();

            for (int p = 0; p < masterPolygons.Count; p++)
            {
                if (!masterOuterBboxes[p].Covers(testPoint))
                    continue;

                if (!TopologyUtility.IsPointInRing(masterPolygons[p].Geometries[0], testPoint, masterOuterBboxes[p]))
                    continue;

                bool inAnyHole = false;

                for (int h = 1; h < masterPolygons[p].Geometries.Count; h++)
                {
                    var holeBbox = masterHoleBboxes[p][h - 1];
                    if (!holeBbox.Covers(testPoint))
                        continue;

                    if (TopologyUtility.IsPointInRing(masterPolygons[p].Geometries[h], testPoint, holeBbox))
                    {
                        inAnyHole = true;
                        break;
                    }
                }

                if (inAnyHole)
                    continue;

                isMasterRing = false;

                // inner rings must be CW
                if (fixOrientation && !SpatialUtility.IsClockwise(currentRing.Points))
                {
                    currentRing.Reverse();
                }

                masterPolygons[p].Geometries.Add(currentRing);
                masterHoleBboxes[p].Add(ringBboxes[i]);
                break;
            }

            if (isMasterRing)
            {
                // outter ring must be CCW
                if (fixOrientation && SpatialUtility.IsClockwise(currentRing.Points))
                {
                    currentRing.Reverse();
                }

                masterPolygons.Add(Geometry<T>.Create([currentRing], GeometryType.Polygon, srid));
                masterOuterBboxes.Add(ringBboxes[i]);
                masterHoleBboxes.Add(new List<BoundingBox>());
            }
        }

        return masterPolygons.Count == 1 ?
            masterPolygons.First() :
            Create(masterPolygons, GeometryType.MultiPolygon, srid);
        //new Geometry<T>(masterPolygons, GeometryType.MultiPolygon, srid);
    }

    public static Geometry<T> Create(List<Geometry<T>> geometries, GeometryType type, int srid)
    {
        if (type != GeometryType.MultiLineString &&
            type != GeometryType.MultiPoint &&
            type != GeometryType.MultiPolygon &&
            type != GeometryType.Polygon &&
            type != GeometryType.GeometryCollection)
            throw new NotImplementedException();

        var result = new Geometry<T>()
        {
            Geometries = geometries,
            Type = type,
            Srid = srid
        };

        if (geometries.Count > 0)
        {
            var tempSubType = geometries.First().Type;

            if (geometries.Any(i => i.Type != tempSubType))
            {
                result.Type = GeometryType.GeometryCollection;
            }
        }

        return result;
    }

    #endregion


    #region Static Methods


    // To do: provide sample input and expected output for this method  
    public static Geometry<T> ParseToGeodeticGeometry(double[][] geoCoordinates, GeometryType geometryType, bool isLongitudeFirst = true)
    {
        return Geometry<T>.Create(geoCoordinates.Select(p => ParseLineStringToGeodeticGeometry(p.ToList(), isLongitudeFirst)).ToList(), geometryType, SridHelper.GeodeticWGS84);
    }

    // To do: provide sample input and expected output for this method
    private static Geometry<T> ParseLineStringToGeodeticGeometry(List<double> values, bool isLongitudeFirst)
    {
        if (values == null || values.Count() % 2 != 0)
        {
            throw new NotImplementedException();
        }

        List<T> result = new List<T>(values.Count / 2);

        if (isLongitudeFirst)
        {
            for (int i = 0; i < values.Count - 1; i += 2)
            {
                result.Add(new T() { X = values[i], Y = values[i + 1] });
            }
        }
        else
        {
            for (int i = 0; i < values.Count - 1; i += 2)
            {
                result.Add(new T() { X = values[i + 1], Y = values[i] });
            }
        }

        return Geometry<T>.Create(result, GeometryType.LineString, SridHelper.GeodeticWGS84);

    }

    public static Geometry<T> ParsePointToGeometry(double[] xy, bool isLongitudeFirst, int srid = SridHelper.GeodeticWGS84)
    {
        return Geometry<T>.Create(new List<T>() { Point.Parse<T>(xy, isLongitudeFirst) }, GeometryType.Point, srid);
    }

    public static Geometry<T> ParseLineStringToGeometry(
        double[][]? geoCoordinates,
        GeometryType geometryType,
        bool isRing,
        bool isLongitudeFirst = true,
        int srid = SridHelper.GeodeticWGS84)
    {
        if (geoCoordinates.IsNullOrEmpty())
            return Geometry<T>.CreateEmpty(geometryType, srid);

        if (isRing)
        {
            var numberOfPoints = geoCoordinates.Length;

            // skip last point
            return Geometry<T>.Create(geoCoordinates.Take(numberOfPoints - 1).Select(p => Point.Parse<T>(p, isLongitudeFirst)).ToList(), geometryType, srid);
        }
        else
        {
            return Geometry<T>.Create(geoCoordinates.Select(p => Point.Parse<T>(p, isLongitudeFirst)).ToList(), geometryType, srid);
        }
    }

    public static Geometry<T> ParsePolygonToGeometry(
        double[][][] rings,
        GeometryType geometryType,
        bool isLongitudeFirst,
        int srid = SridHelper.GeodeticWGS84)
    {
        return Geometry<T>.Create(rings.Select(p => ParseLineStringToGeometry(p, GeometryType.LineString, true, isLongitudeFirst, srid)).ToList(), geometryType, srid);
    }


    #endregion


    #region Area

    public double EuclideanArea => GetUnsignedEuclideanArea();

    // https://www.mathopenref.com/coordpolygonarea.html
    private double GetUnsignedEuclideanArea()
    {
        if (this.IsNullOrEmpty())
            return 0;

        switch (this.Type)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
            case GeometryType.LineString:
            case GeometryType.MultiLineString:
                return 0;

            case GeometryType.Polygon:
                return this.GetUnsignedEuclideanAreaForPolygon();

            case GeometryType.MultiPolygon:
                return Geometries.Sum(g => g.GetUnsignedEuclideanArea());

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }
    }

    //1399.06.10
    //یک چندضلعی از یک رینگ بزرگ که ممکنه حفره داشته باشه
    //تشکیل شده. بنابر این بزرگ‌ترین مساحت متعلق به رینگ بزرگ 
    //و باقی همه حفره‌ها هستن
    //این الگوریتم اگه چندضلعی معتبر نباشه درست جواب نمی‌ده
    private double GetUnsignedEuclideanAreaForPolygon()
    {
        if (this.Geometries is null || this.Geometries.Count == 0)
            return 0;

        double outerArea = 0;
        double holesArea = 0;

        for (int i = 0; i < this.Geometries.Count; i++)
        {
            var area = SpatialUtility.GetUnsignedEuclideanArea(this.Geometries[i].GetAllPoints());

            // If this ring is bigger than current outer, swap
            if (area > outerArea)
            {
                holesArea += outerArea; // previous outer becomes hole
                outerArea = area;
            }
            else
            {
                holesArea += area; // this is a hole
            }
        }

        return Math.Max(0, outerArea - holesArea);
    }

    #endregion


    #region Length

    public double GetEuclideanLength() => GetLength(SpatialUtility.GetEuclideanLength);

    public double GetSphericalLength()
    {
        if (this.Srid != SridHelper.GeodeticWGS84)
            throw new NotImplementedException("Geometry > GetSphericalLength > should be geodetic wgs84");

        return GetLength(SpatialUtility.GetSphericalLength);
    }

    public double GetEllipsoidalLength()
    {
        if (this.Srid != SridHelper.GeodeticWGS84)
            throw new NotImplementedException("Geometry > GetEllipsoidalLength > should be geodetic wgs84");

        return GetLength(SpatialUtility.GetEllipsoidalLength);
    }


    //public double GetEuclideanLength()
    //{
    //    if (this.IsNullOrEmpty())
    //        return 0;

    //    //1399.07.17
    //    //if (this.Points == null && this.Geometries == null)
    //    //    return 0;

    //    switch (this.Type)
    //    {
    //        case GeometryType.Point:
    //        case GeometryType.MultiPoint:
    //            return 0;

    //        case GeometryType.LineString:
    //            return GetEuclideanLengthForLineStringOrRing(false);

    //        case GeometryType.Polygon:
    //            return Geometries.Sum(g => g.GetEuclideanLengthForLineStringOrRing(true));

    //        case GeometryType.MultiLineString:
    //        case GeometryType.MultiPolygon:
    //            return Geometries.Sum(g => g.GetEuclideanLength());

    //        case GeometryType.GeometryCollection:
    //        case GeometryType.CircularString:
    //        case GeometryType.CompoundCurve:
    //        case GeometryType.CurvePolygon:
    //        default:
    //            throw new NotImplementedException("Geometry.cs > CalculateEuclideanLength");
    //    }
    //}

    //private double GetEuclideanLengthForLineStringOrRing(bool isRing)
    //{
    //    if (this.Points == null || this.Points.Count < 2)
    //        return 0;

    //    double result = 0;

    //    for (int i = 0; i < this.Points.Count - 1; i++)
    //    {
    //        result += SpatialUtility.GetEuclideanLength(this.Points[i], this.Points[i + 1]);
    //    }

    //    if (isRing)
    //    {
    //        result += SpatialUtility.GetEuclideanLength(this.Points[this.Points.Count - 1], this.Points[0]);
    //    }

    //    return result;
    //}

    //public double GetSphericalLength(Func<T, T> toWgs84Geodetic)
    //{
    //    // return geometry.AsSqlGeometry().Project(toWgs84Geodetic, SridHelper.GeodeticWGS84).MakeValid().STLength().Value;

    //    if (this.IsNotValidOrEmpty())
    //        return 0;

    //    switch (this.Type)
    //    {
    //        case GeometryType.Point:
    //        case GeometryType.MultiPoint:
    //            return 0;

    //        case GeometryType.LineString:
    //            return GetSphericalLengthForLineStringOrRing(false, toWgs84Geodetic);

    //        case GeometryType.Polygon:
    //            return Geometries.Sum(g => g.GetSphericalLengthForLineStringOrRing(true, toWgs84Geodetic));

    //        case GeometryType.MultiLineString:
    //        case GeometryType.MultiPolygon:
    //            return Geometries.Sum(g => g.GetSphericalLength(toWgs84Geodetic));

    //        case GeometryType.GeometryCollection:
    //        case GeometryType.CircularString:
    //        case GeometryType.CompoundCurve:
    //        case GeometryType.CurvePolygon:
    //        default:
    //            throw new NotImplementedException("Geometry.cs > CalculateEuclideanLength");
    //    }
    //}

    //private double GetSphericalLengthForLineStringOrRing(bool isRing, Func<T, T> toWgs84Geodetic)
    //{
    //    if (Points is null || Points.Count < 2)
    //        return 0;

    //    double result = 0;

    //    var wgs84Pionts = this.Points.Select(toWgs84Geodetic).ToList();

    //    for (int i = 0; i < wgs84Pionts.Count - 1; i++)
    //    {
    //        result += SpatialUtility.GetSphericalLength(wgs84Pionts[i], wgs84Pionts[i + 1]);
    //    }

    //    if (isRing)
    //    {
    //        result += SpatialUtility.GetSphericalLength(wgs84Pionts[this.Points.Count - 1], wgs84Pionts[0]);
    //    }

    //    return result;
    //}

    //public double GetEllipsoidalLength(Func<T, T> toWgs84Geodetic)
    //{
    //    // return geometry.AsSqlGeometry().Project(toWgs84Geodetic, SridHelper.GeodeticWGS84).MakeValid().STLength().Value;

    //    if (this.IsNotValidOrEmpty())
    //        return 0;

    //    switch (this.Type)
    //    {
    //        case GeometryType.Point:
    //        case GeometryType.MultiPoint:
    //            return 0;

    //        case GeometryType.LineString:
    //            return GetEllipsoidalLengthForLineStringOrRing(false, toWgs84Geodetic);

    //        case GeometryType.Polygon:
    //            return Geometries.Sum(g => g.GetEllipsoidalLengthForLineStringOrRing(true, toWgs84Geodetic));

    //        case GeometryType.MultiLineString:
    //        case GeometryType.MultiPolygon:
    //            return Geometries.Sum(g => g.GetEllipsoidalLength(toWgs84Geodetic));

    //        case GeometryType.GeometryCollection:
    //        case GeometryType.CircularString:
    //        case GeometryType.CompoundCurve:
    //        case GeometryType.CurvePolygon:
    //        default:
    //            throw new NotImplementedException("Geometry.cs > CalculateEuclideanLength");
    //    }
    //}

    //private double GetEllipsoidalLengthForLineStringOrRing(bool isRing, Func<T, T> toWgs84Geodetic)
    //{
    //    if (Points is null || Points.Count < 2)
    //        return 0;

    //    double result = 0;

    //    var wgs84Pionts = this.Points.Select(toWgs84Geodetic).ToList();

    //    for (int i = 0; i < wgs84Pionts.Count - 1; i++)
    //    {
    //        result += SpatialUtility.GetEllipsoidalLength(wgs84Pionts[i], wgs84Pionts[i + 1]);
    //    }

    //    if (isRing)
    //    {
    //        result += SpatialUtility.GetEllipsoidalLength(wgs84Pionts[this.Points.Count - 1], wgs84Pionts[0]);
    //    }

    //    return result;
    //}


    private double GetLength(Func<T, T, double> distanceFunc)
    {
        if (this.IsNullOrEmpty())
            return 0;

        //1399.07.17
        //if (this.Points == null && this.Geometries == null)
        //    return 0;

        switch (this.Type)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return 0;

            case GeometryType.LineString:
                return GetLengthForLineStringOrRing(distanceFunc, false);

            case GeometryType.Polygon:
                return Geometries.Sum(g => g.GetLengthForLineStringOrRing(distanceFunc, true));

            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                return Geometries.Sum(g => g.GetLength(distanceFunc));

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry.cs > CalculateEuclideanLength");
        }
    }

    private double GetLengthForLineStringOrRing(Func<T, T, double> distanceFunc, bool isRing)
    {
        if (this.Points == null || this.Points.Count < 2)
            return 0;

        double result = 0;

        for (int i = 0; i < this.Points.Count - 1; i++)
        {
            result += distanceFunc(this.Points[i], this.Points[i + 1]);
        }

        if (isRing)
        {
            result += distanceFunc(this.Points[this.Points.Count - 1], this.Points[0]);
        }

        return result;
    }


    #endregion


    #region Density

    // 1401.02.31; 1401.03.12
    public double CalculatePointDensity()
    {
        var length = this.GetEuclideanLength();

        if (length == 0)
            return double.PositiveInfinity;

        return this.TotalNumberOfPoints / length;
    }

    /// <summary>
    /// Standard Deviation for segment lengths
    /// </summary>
    /// <returns></returns>
    public double CalculateSegmentLengthVariations()
    {
        var segments = GetSegmentLengths();

        return Statistics.CalculateStandardDeviation(segments, VarianceCalculationMode.Population);
    }

    public List<double> GetSegmentLengths()
    {
        if (this.IsNullOrEmpty())
            return new List<double>();

        switch (this.Type)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return new List<double>();

            case GeometryType.LineString:
                return GetSegmentLengthsForLineStringOrRing(false);

            case GeometryType.Polygon:
                return this.Geometries.SelectMany(g => g.GetSegmentLengthsForLineStringOrRing(true)).ToList();

            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                return this.Geometries.SelectMany(g => g.GetSegmentLengths()).ToList();

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }
    }

    private List<double> GetSegmentLengthsForLineStringOrRing(bool isRing)
    {
        List<double> result = new List<double>();

        if (this.Points == null || this.Points.Count < 2)
            return result;

        for (int i = 0; i < this.Points.Count - 1; i++)
        {
            result.Add(SpatialUtility.GetEuclideanLength(this.Points[i], this.Points[i + 1]));
        }

        if (isRing)
        {
            result.Add(SpatialUtility.GetEuclideanLength(this.Points[this.Points.Count - 1], this.Points[0]));
        }

        return result;
    }

    #endregion


    #region Angularity

    /// <summary>
    /// returns weighted average of angles between triads of points in radian
    /// </summary>
    /// <returns></returns>
    public double CalculateMeanAngularChange()
    {
        if (this.IsNullOrEmpty())
            return 0;

        switch (this.Type)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return 0;

            case GeometryType.LineString:
                return CalculateMeanAngularChangeForLineStringOrRing(false);

            case GeometryType.Polygon:
                return Geometries.Sum(g => g.TotalNumberOfPoints * g.CalculateMeanAngularChangeForLineStringOrRing(true))
                        /
                        Geometries.Sum(g => g.TotalNumberOfPoints);

            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                return Geometries.Sum(g => g.TotalNumberOfPoints * g.CalculateMeanAngularChange())
                        /
                        Geometries.Sum(g => g.TotalNumberOfPoints);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry.cs > CalculateSumOfAngles");
        }
    }

    /// <summary>
    /// Mean angular changes for lineString or ring in radian
    /// </summary>
    /// <param name="isRing"></param>
    /// <returns></returns>
    private double CalculateMeanAngularChangeForLineStringOrRing(bool isRing)
    {
        if (this.Points == null || this.Points.Count < 3)
            return 0;

        double result = 0;

        for (int i = 0; i < this.Points.Count - 2; i++)
        {
            result += SpatialUtility.GetOuterAngle(Points[i], Points[i + 1], Points[i + 2]);
        }

        if (isRing)
        {
            result += SpatialUtility.GetOuterAngle(this.Points[this.Points.Count - 2], this.Points[this.Points.Count - 1], this.Points[0]);
            result += SpatialUtility.GetOuterAngle(this.Points[this.Points.Count - 1], this.Points[0], this.Points[1]);
        }

        result = isRing ? result / Points.Count : result / (Points.Count - 2);

        return result;
    }

    #endregion


    #region Curvilinearity

    /// <summary>
    /// part which are CW or CCW
    /// </summary>
    public double GetNumerOfCurvilinearityChange()
    {
        if (this.IsNullOrEmpty())
            return 0;

        switch (Type)
        {
            case GeometryType.LineString:
                return GetNumerOfCurvilinearityChangeForLineStringOrRing(false);

            case GeometryType.Polygon:
                return this.Geometries.Sum(p => p.GetNumerOfCurvilinearityChangeForLineStringOrRing(true));

            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                return this.Geometries.Sum(p => p.GetNumerOfCurvilinearityChange());

            case GeometryType.MultiPoint:
            case GeometryType.Point:
            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                return 0;
        }
    }

    private double GetNumerOfCurvilinearityChangeForLineStringOrRing(bool isRing)
    {
        // to prevent divide by zero, default value set to one
        if (this.Points == null || this.Points.Count < 3)
            return 1;

        double result = 1;

        bool isClockWise = SpatialUtility.IsClockwise(new List<T>() { Points[0], Points[1], Points[2] });
        bool temporaryIsClockWise = isClockWise;

        for (int i = 0; i < this.Points.Count - 2; i++)
        {
            isClockWise = SpatialUtility.IsClockwise(new List<T>() { Points[i], Points[i + 1], Points[i + 2] });

            if (temporaryIsClockWise != isClockWise)
            {
                result++;
                temporaryIsClockWise = isClockWise;
            }
        }

        if (isRing)
        {
            isClockWise = SpatialUtility.IsClockwise(new List<T>() { this.Points[this.Points.Count - 2], this.Points[this.Points.Count - 1], this.Points[0] });

            if (temporaryIsClockWise != isClockWise)
            {
                result++;
                temporaryIsClockWise = isClockWise;
            }

            isClockWise = SpatialUtility.IsClockwise(new List<T>() { this.Points[this.Points.Count - 1], this.Points[0], this.Points[1] });

            if (temporaryIsClockWise != isClockWise)
            {
                result++;
                //temporaryIsClockWise = isClockWise;
            }
        }

        return result;
    }

    #endregion


    #region Cleansing

    public bool HasDuplicatePoints()
    {
        if (this.IsNullOrEmpty())
        {
            return false;
        }

        if (this.Points?.Count > 0)
        {
            return this.Points.GroupBy(g => new { x = g.X, y = g.Y }).Any(g => g.Count() > 1);
        }
        else
        {
            return this.Geometries?.Any(g => g.HasDuplicatePoints()) == true;
        }
    }

    public void RemoveConsecutiveDuplicatePoints()
    {
        if (this.IsNullOrEmpty())
        {
            return;
        }

        if (this.IsLeafGeometry())
        {
            for (int i = this.Points.Count - 1; i >= 1; i--)
            {
                if (this.Points[i].Equals(this.Points[i - 1]))
                {
                    this.Points.RemoveAt(i);
                }
            }
        }
        else
        {
            for (int i = this.Geometries.Count - 1; i >= 0; i--)
            {
                if (this.Geometries[i] == null || this.Geometries[i].IsNullOrEmpty())
                {
                    this.Geometries.RemoveAt(i);
                }
            }

            for (int i = this.Geometries.Count - 1; i >= 0; i--)
            {
                this.Geometries[i].RemoveConsecutiveDuplicatePoints();
            }
        }

        this.ReevaluateGeometryType();
    }


    //private void ReevaluateGeometryType()
    //{
    //    if (this == null || this.IsNullOrEmpty())
    //    {
    //        return;
    //    }

    //    var numberOfPoints = this.Points?.Count;

    //    var numberOfGeometries = this.Geometries?.Count;

    //    //حالت نقطه یا خط
    //    if (numberOfPoints > 0)
    //    {
    //        if (numberOfPoints == 1)
    //        {
    //            this.Type = GeometryType.Point;
    //        }
    //        else
    //        {
    //            this.Type = GeometryType.LineString;
    //        }
    //    }
    //    //سایر حالت‌ها
    //    else if (numberOfGeometries > 0)
    //    {
    //        var types = this.Geometries.Select(g => g.Type).Distinct();

    //        //حالت ترکیبی
    //        if (types.Count() > 1)
    //        {
    //            this.Type = GeometryType.GeometryCollection;
    //        }
    //        else
    //        {
    //            var subGeometryType = this.Geometries.First().Type;

    //            switch (subGeometryType)
    //            {
    //                //حالت چند نقطه‌ای
    //                case GeometryType.Point:
    //                    this.Type = GeometryType.MultiPoint;
    //                    break;

    //                //حالت چند خطی یا چند ضلعی
    //                case GeometryType.LineString:
    //                    if (this.Type == GeometryType.LineString)
    //                        this.Type = GeometryType.MultiLineString;
    //                    else if (this.Type == GeometryType.Polygon)
    //                        // 1400.06.28
    //                        //this.Type = GeometryType.MultiPolygon;
    //                        this.Type = GeometryType.Polygon;

    //                    break;

    //                //حالت چندضلعی‌های چند تکه‌ای
    //                case GeometryType.Polygon:
    //                    this.Type = GeometryType.MultiPolygon;
    //                    break;

    //                case GeometryType.MultiPoint:
    //                case GeometryType.MultiLineString:
    //                case GeometryType.MultiPolygon:
    //                    this.Type = GeometryType.GeometryCollection;
    //                    break;

    //                case GeometryType.GeometryCollection:
    //                case GeometryType.CircularString:
    //                case GeometryType.CompoundCurve:
    //                case GeometryType.CurvePolygon:
    //                default:
    //                    throw new NotImplementedException("Geometry.cs > ReevaluateGeometryType");
    //            }
    //        }
    //    }

    //    return;
    //}

    private void ReevaluateGeometryType()
    {
        if (this == null || this.IsNullOrEmpty())
            return;

        // If Points is non-null, we have a leaf geometry
        if (this.Points != null)
        {
            if (this.Points.Count == 1)
            {
                this.Type = GeometryType.Point;
            }
            else if (this.Points.Count > 1)
            {
                this.Type = GeometryType.LineString;
            }
            // No need to restructure – leaf geometries already have correct data layout.
            return;
        }

        // If Geometries is null or empty, nothing to do
        if (this.Geometries == null || this.Geometries.Count == 0)
            return;

        var types = this.Geometries.Select(g => g.Type).Distinct().ToList();
        bool isHomogeneous = types.Count == 1;

        if (!isHomogeneous)
        {
            // Mixed types -> GeometryCollection, data already correct
            this.Type = GeometryType.GeometryCollection;
            return;
        }

        var subType = types[0];
        int geomCount = this.Geometries.Count;

        switch (subType)
        {
            case GeometryType.Point when geomCount == 1:
                // MultiPoint with one point -> convert to a single Point
                var singlePoint = this.Geometries[0].Points[0];
                this.Points = new List<T> { singlePoint };
                this.Geometries = null;
                this.Type = GeometryType.Point;
                break;

            case GeometryType.Point:
                // Keep as MultiPoint
                this.Type = GeometryType.MultiPoint;
                // Data is already Geometries list of Points – correct.
                break;

            case GeometryType.LineString when geomCount == 1:
                // MultiLineString with one line -> convert to a single LineString
                var singleLine = this.Geometries[0];
                this.Points = new List<T>(singleLine.Points);
                this.Geometries = null;
                this.Type = GeometryType.LineString;
                break;

            case GeometryType.LineString:
                // Keep as MultiLineString
                this.Type = GeometryType.MultiLineString;
                break;

            case GeometryType.Polygon when geomCount == 1:
                // MultiPolygon with one polygon -> convert to a single Polygon
                var singlePolygon = this.Geometries[0];
                this.Geometries = new List<Geometry<T>>(singlePolygon.Geometries); // copy rings
                this.Type = GeometryType.Polygon;
                break;

            case GeometryType.Polygon:
                // Keep as MultiPolygon
                this.Type = GeometryType.MultiPolygon;
                break;

            default:
                // For any other case, default to GeometryCollection to be safe
                this.Type = GeometryType.GeometryCollection;
                break;
        }
    }

    #endregion

}
