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

namespace IRI.Maptor.Sta.Spatial.Primitives;

public class Geometry<T> : IGeometry where T : IPoint, new()
{
    static readonly T NullPoint = default;

    private static readonly Geometry<T> _empty = new Geometry<T>();

    public static Geometry<T> Empty { get { return _empty; } }

    public GeometryType Type { get; set; }

    private List<T> _points;

    public List<T> Points
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

    private List<Geometry<T>> _geometries;

    public List<Geometry<T>> Geometries
    {
        get { return _geometries; }
        set
        {
            if (this.Points != null && value != null)
            {
                throw new NotImplementedException();
            }

            this._geometries = value;
        }
    }

    public int NumberOfPoints { get { return Points?.Count ?? 0; } }

    public int NumberOfGeometries { get { return Geometries?.Count ?? 0; } }

    public int TotalNumberOfPoints
    {
        get
        {
            if (this.IsLeafGeometry())
            {
                return this.NumberOfPoints;
            }
            else
            {
                return Geometries?.Sum(g => g.TotalNumberOfPoints) ?? 0;
            }
        }
    }

    public int Srid { get; set; }

    public bool HasZ()
    {
        return typeof(IHasZ).IsAssignableFrom(typeof(T));
    }

    public bool HasM()
    {
        return typeof(IHasM).IsAssignableFrom(typeof(T));
    }
     

    #region Constructors

    private Geometry()
    {

    }

    //public Geometry(List<Point> points, GeometryType type, int srid) : this(points as List<T>, type, false, srid) { }

    public Geometry(List<T> points, GeometryType type, int srid) : this(points, type, false, srid) { }

    public Geometry(List<T> points, GeometryType type, bool isClosed, int srid)
    {
        if (type == GeometryType.LineString || type == GeometryType.Point)
        {
            if (type == GeometryType.LineString && isClosed && points?.Count > 0)
            {
                var lastPoint = points[points.Count - 1];

                var firstPoint = points[0];

                if (lastPoint.X != firstPoint.X && lastPoint.Y != firstPoint.Y)
                {
                    throw new ArgumentException("the last point and first point must be the same");
                }
            }

            this.Points = points;

            this.Type = type;

            this.Srid = srid;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public Geometry(List<Geometry<T>> geometries, GeometryType type, int srid)
    {
        if (type != GeometryType.MultiLineString && type != GeometryType.MultiPoint && type != GeometryType.MultiPolygon && type != GeometryType.Polygon && type != GeometryType.GeometryCollection)
        {
            throw new NotImplementedException();
        }

        this.Geometries = geometries;

        this.Type = type;

        this.Srid = srid;

        if (geometries.Count > 0)
        {
            var tempSubType = geometries.First().Type;

            if (geometries.Any(i => i.Type != tempSubType))
            {
                this.Type = GeometryType.GeometryCollection;
            }
        }
    }

    public Geometry(Geometry<T> geometry, GeometryType type, int srid) : this(new List<Geometry<T>>() { geometry }, type, srid) { }

    #endregion


    #region Simple Methods

    public override string ToString()
    {
        return $"{Type} - #Points: {NumberOfPoints} - #Parts: {Geometries?.Count()}";
    }

    public bool IsRingBase()
    {
        return this.Type == GeometryType.Polygon || this.Type == GeometryType.MultiPolygon || this.Type == GeometryType.CurvePolygon;
    }

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


    public bool IsValid()
    {
        switch (this.Type)
        {
            case GeometryType.Point:
                return this.Points?.Count == 1;

            case GeometryType.LineString:
                return this.Points.Count > 1;

            case GeometryType.Polygon:
                return this.Geometries?.Count > 0 && this.Geometries?.All(g => g.Points?.Count >= 3) == true;

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

    public BoundingBox GetBoundingBox()
    {
        return BoundingBox.CalculateBoundingBox(GetAllPoints());
    }

    public bool IsPointOrMultiPoint()
    {
        return Type == GeometryType.Point || Type == GeometryType.MultiPoint;
    }

    public bool IsLineStringOrMultiLineString()
    {
        return Type == GeometryType.LineString || Type == GeometryType.MultiLineString;
    }

    public bool IsPolygonOrMultiPolygon()
    {
        return Type == GeometryType.Polygon || Type == GeometryType.MultiPolygon;
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

    public T GetMeanPoint()
    {
        var allPoints = GetAllPoints();

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

    //must be reviewed: 
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
            var nearestPoints = this.Geometries.Select(g => g.GetNearestPoint(point)).ToList();

            //must be reviewed
            return nearestPoints.First();
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
                return new Geometry<T>(this.Geometries.Select(i => i.FilterPoints(filter)).ToList(), GeometryType.GeometryCollection, this.Srid);

            case GeometryType.MultiPoint:
                return new Geometry<T>(this.Geometries, GeometryType.MultiPoint, this.Srid);

            case GeometryType.Point:
                return new Geometry<T>(this.Points, GeometryType.Point, this.Srid);

            case GeometryType.LineString:
                //todo: multiple cast consider changing it
                //return CreatePointOrLineString<T>(filter(this.Points.Select(i => (T)i).ToList()).Select(i => (IPoint)i).ToList(), this.Srid);
                return CreatePointOrLineString(filter(this.Points), this.Srid);

            case GeometryType.MultiLineString:
                return new Geometry<T>(this.Geometries.Select(i => i.FilterPoints(filter)).ToList(), GeometryType.MultiLineString, this.Srid);

            case GeometryType.Polygon:
                return new Geometry<T>(this.Geometries.Select(i => i.FilterPoints(filter)).ToList(), GeometryType.Polygon, this.Srid);

            case GeometryType.MultiPolygon:
                return new Geometry<T>(this.Geometries.Select(i => i.FilterPoints(filter)).ToList(), GeometryType.MultiPolygon, this.Srid);

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
    public Geometry<T> Simplify(SimplificationType type, SimplificationParamters paramters)
    {
        Func<List<T>, List<T>> filter;

        switch (type)
        {
            case SimplificationType.NthPoint:
                filter = pList => Simplifications.SimplifyByNthPoint(pList, paramters);
                break;

            case SimplificationType.RandomPointSelection:
                filter = pList => Simplifications.SimplifyByRandomPointSelection(pList, paramters);
                break;

            case SimplificationType.EuclideanDistance:
                filter = pList => Simplifications.SimplifyByEuclideanDistance(pList, paramters);
                break;

            case SimplificationType.TriangleRoutine:
                filter = pList => Simplifications.SimplifyByTriangleRoutine(pList, paramters);
                break;

            case SimplificationType.CumulativeTriangleRoutine:
                filter = pList => Simplifications.SimplifyByCumulativeTriangleRoutine(pList, paramters);
                break;

            case SimplificationType.ModifiedTriangleRoutine:
                filter = pList => Simplifications.SimplifyByModifiedTriangleRoutine(pList, paramters);
                break;

            case SimplificationType.Angle:
                filter = pList => Simplifications.SimplifyByAngle(pList, paramters);
                break;

            case SimplificationType.CumulativeAngle:
                filter = pList => Simplifications.SimplifyByCumulativeAngle(pList, paramters);
                break;

            case SimplificationType.CumulativeEuclideanDistance:
                filter = pList => Simplifications.SimplifyByCumulativeEuclideanDistance(pList, paramters);
                break;

            case SimplificationType.VisvalingamWhyatt:
                filter = pList => Simplifications.SimplifyByVisvalingamWhyatt(pList, paramters, this.IsRingBase());
                break;

            case SimplificationType.RamerDouglasPeucker:
                filter = pList => Simplifications.SimplifyByRamerDouglasPeucker(pList, paramters);
                break;

            case SimplificationType.Lang:
                filter = pList => Simplifications.SimplifyByLang(pList, paramters);
                break;

            case SimplificationType.ReumannWitkam:
                filter = pList => Simplifications.SimplifyByReumannWitkam(pList, paramters);
                break;

            case SimplificationType.SleeveFitting:
                filter = pList => Simplifications.SimplifyBySleeveFitting(pList, paramters);
                break;

            case SimplificationType.PerpendicularDistance:
                filter = pList => Simplifications.SimplifyByPerpendicularDistance(pList, paramters);
                break;

            case SimplificationType.ModifiedPerpendicularDistance:
                filter = pList => Simplifications.SimplifyByModifiedPerpendicularDistance(pList, paramters);
                break;

            case SimplificationType.NormalOpeningWindow:
                filter = pList => Simplifications.SimplifyByNormalOpeningWindow(pList, paramters);
                break;

            case SimplificationType.BeforeOpeningWindow:
                filter = pList => Simplifications.SimplifyByBeforeOpeningWindow(pList, paramters);
                break;


            case SimplificationType.AdditiveAreaPlus:
                filter = pList => Simplifications.SimplifyByAdditiveAreaPlus(pList, paramters);
                break;

            case SimplificationType.CumulativeAreaAngle:
                filter = pList => Simplifications.SimplifyByCumulativeAngleArea(pList, paramters);
                break;

            case SimplificationType.APSC:
                filter = pList => Simplifications.SimplifyByAPSC(pList, paramters);
                break;

            default:
                throw new NotImplementedException();
        }

        return this.FilterPoints(filter);
    }

    public Geometry<T> Simplify(SimplificationType type, int zoomLevel, SimplificationParamters paramters)
    {
        var threshold = WebMercatorUtility.CalculateGroundResolution(zoomLevel, paramters.AverageLatitude ?? 0); //0 seconds!

        paramters.AreaThreshold = threshold * threshold;

        paramters.DistanceThreshold = threshold;

        return Simplify(type, paramters);
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
                return new Geometry<T>(this.Geometries.Select(i => i.Transform(transform)).ToList(), GeometryType.MultiPoint, newSrid);

            case GeometryType.Point:
                return new Geometry<T>(this.Points.Select(i => transform(i)).ToList(), GeometryType.Point, newSrid);

            case GeometryType.LineString:
                return new Geometry<T>(this.Points.Select(i => transform(i)).ToList(), GeometryType.LineString, newSrid);

            case GeometryType.MultiLineString:
                return new Geometry<T>(this.Geometries.Select(i => i.Transform(transform)).ToList(), GeometryType.MultiLineString, newSrid);

            case GeometryType.MultiPolygon:
                return new Geometry<T>(this.Geometries.Select(i => i.Transform(transform)).ToList(), GeometryType.MultiPolygon, newSrid);

            case GeometryType.Polygon:
                return new Geometry<T>(this.Geometries.Select(i => i.Transform(transform)).ToList(), GeometryType.Polygon, newSrid);

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
                    return this.Geometries.Zip(other.Geometries, (g1, g2) => g1.HasTheSameSignature(g2)).Any(f => f);

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

        var firstMbb = this.GetBoundingBox();

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
                return other.Geometries.Any(Intersects);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry > Intersects");
        }

    }

    private bool IntersectsPoint(T point)
    {
        if (point is null)
            return false;

        if (!this.GetBoundingBox().Intersects(point))
            return false;

        switch (this.Type)
        {
            case GeometryType.Point:
                return SpatialUtility.GetEuclideanLength(this.AsPoint(), point) < SpatialUtility.EpsilonDistance;

            case GeometryType.LineString:
                return TopologyUtility.IsPointOnLineString(this, point);

            case GeometryType.Polygon:
                return TopologyUtility.IsPointInPolygon(this, point);

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                return this.Geometries.Any(g => g.IntersectsPoint(point));

            case GeometryType.GeometryCollection:
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
                return this.Geometries.Any(g => g.IntersectsLineSegment(startSegment, endSegment));

            case GeometryType.GeometryCollection:
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
                return this.Geometries.Any(g => g.IntersectsLineStringOrRing(lineString, isRing));

            case GeometryType.GeometryCollection:
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
                return TopologyUtility.IsPointInPolygon(polygon, this.Points[0]);

            case GeometryType.LineString:
                //return polygon.Geometries.Any(g => g.IntersectsLineStringOrRing(this, isRing: false));
                return polygon.IntersectsLineStringOrRing(this, isRing: false);

            case GeometryType.Polygon:
                if (TopologyUtility.IsPointInPolygon(this, polygon.GetLastPoint()))
                    return true;

                //else if (TopologyUtility.IsPointInPolygon(polygon, this.GetLastPoint()))
                //    return true;


                return this.Geometries.Any(g => polygon.IntersectsLineStringOrRing(g, isRing: true));

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                return this.Geometries.Any(g => g.IntersectsPolygon(polygon));

            case GeometryType.GeometryCollection:
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
    /// Computes the union of this geometry with another geometry
    /// </summary>
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

        switch (this.Type)
        {
            case GeometryType.Point:
                return UnionPoint(other);

            case GeometryType.LineString:
                return UnionLineString(other);

            case GeometryType.Polygon:
                return UnionPolygon(other);

            case GeometryType.MultiPoint:
                return UnionMultiPoint(other);

            case GeometryType.MultiLineString:
                return UnionMultiLineString(other);

            case GeometryType.MultiPolygon:
                return UnionMultiPolygon(other);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException($"Union not implemented for {this.Type}");
        }
    }

    private Geometry<T> UnionPoint(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.Point:
                if (this.Points[0].AreExactlyTheSame(other.Points[0]))
                    return this;
                return new Geometry<T>(new List<Geometry<T>> { this, other }, GeometryType.MultiPoint, this.Srid);

            case GeometryType.MultiPoint:
                var points = new List<Geometry<T>> { this };
                points.AddRange(other.Geometries);
                return new Geometry<T>(points, GeometryType.MultiPoint, this.Srid);

            default:
                return new Geometry<T>(new List<Geometry<T>> { this, other }, GeometryType.GeometryCollection, this.Srid);
        }
    }

    private Geometry<T> UnionLineString(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.LineString:
                // Check if lines are connected - simplified: just combine into MultiLineString
                return new Geometry<T>(new List<Geometry<T>> { this, other }, GeometryType.MultiLineString, this.Srid);

            case GeometryType.MultiLineString:
                var lines = new List<Geometry<T>> { this };
                lines.AddRange(other.Geometries);
                return new Geometry<T>(lines, GeometryType.MultiLineString, this.Srid);

            default:
                return new Geometry<T>(new List<Geometry<T>> { this, other }, GeometryType.GeometryCollection, this.Srid);
        }
    }

    private Geometry<T> UnionPolygon(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.Polygon:
                return UnionPolygons(this, other);

            case GeometryType.MultiPolygon:
                var result = this;
                foreach (var poly in other.Geometries)
                {
                    result = result.UnionPolygon(poly);
                }
                return result;

            default:
                return new Geometry<T>(new List<Geometry<T>> { this, other }, GeometryType.GeometryCollection, this.Srid);
        }
    }

    private Geometry<T> UnionMultiPoint(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.Point:
                var points = new List<Geometry<T>>(this.Geometries) { other };
                return new Geometry<T>(points, GeometryType.MultiPoint, this.Srid);

            case GeometryType.MultiPoint:
                var allPoints = new List<Geometry<T>>(this.Geometries);
                allPoints.AddRange(other.Geometries);
                // Remove duplicate points
                var uniquePoints = new List<Geometry<T>>();
                foreach (var point in allPoints)
                {
                    if (!uniquePoints.Any(p => p.Points[0].AreExactlyTheSame(point.Points[0])))
                        uniquePoints.Add(point);
                }
                return new Geometry<T>(uniquePoints, GeometryType.MultiPoint, this.Srid);

            default:
                return new Geometry<T>(new List<Geometry<T>> { this, other }, GeometryType.GeometryCollection, this.Srid);
        }
    }

    private Geometry<T> UnionMultiLineString(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.LineString:
                var lines = new List<Geometry<T>>(this.Geometries) { other };
                return new Geometry<T>(lines, GeometryType.MultiLineString, this.Srid);

            case GeometryType.MultiLineString:
                var allLines = new List<Geometry<T>>(this.Geometries);
                allLines.AddRange(other.Geometries);
                return new Geometry<T>(allLines, GeometryType.MultiLineString, this.Srid);

            default:
                return new Geometry<T>(new List<Geometry<T>> { this, other }, GeometryType.GeometryCollection, this.Srid);
        }
    }

    private Geometry<T> UnionMultiPolygon(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.Polygon:
                var result = other;
                foreach (var poly in this.Geometries)
                {
                    result = result.UnionPolygon(poly);
                }
                return result;

            case GeometryType.MultiPolygon:
                result = this.Geometries[0];
                for (int i = 1; i < this.Geometries.Count; i++)
                {
                    result = result.UnionPolygon(this.Geometries[i]);
                }
                foreach (var poly in other.Geometries)
                {
                    result = result.UnionPolygon(poly);
                }
                return result;

            default:
                return new Geometry<T>(new List<Geometry<T>> { this, other }, GeometryType.GeometryCollection, this.Srid);
        }
    }

    /// <summary>
    /// Unions two polygons by combining all rings and using CreatePolygonOrMultiPolygon logic
    /// </summary>
    private static Geometry<T> UnionPolygons(Geometry<T> poly1, Geometry<T> poly2)
    {
        if (poly1.IsNullOrEmpty())
            return poly2;
        if (poly2.IsNullOrEmpty())
            return poly1;

        // Collect all rings from both polygons
        var allRings = new List<Geometry<T>>();

        // Add rings from first polygon
        foreach (var ring in poly1.Geometries)
        {
            allRings.Add(ring.Clone());
        }

        // Add rings from second polygon
        foreach (var ring in poly2.Geometries)
        {
            allRings.Add(ring.Clone());
        }

        // Use CreatePolygonOrMultiPolygon to organize rings correctly
        return CreatePolygonOrMultiPolygon(allRings, poly1.Srid);
    }

    /// <summary>
    /// Checks if two polygons overlap
    /// </summary>
    private static bool ArePolygonsOverlapping(Geometry<T> poly1, Geometry<T> poly2)
    {
        if (poly1.IsNullOrEmpty() || poly2.IsNullOrEmpty())
            return false;

        // Check bounding box intersection first
        var bbox1 = poly1.GetBoundingBox();
        var bbox2 = poly2.GetBoundingBox();
        if (!bbox1.Intersects(bbox2))
            return false;

        // Check if any point from poly1 is inside poly2 or vice versa
        var testPoint1 = poly1.Geometries[0].Points[0];
        var testPoint2 = poly2.Geometries[0].Points[0];

        if (TopologyUtility.IsPointInPolygon(poly2, testPoint1) ||
            TopologyUtility.IsPointInPolygon(poly1, testPoint2))
            return true;

        // Check if boundaries intersect
        return poly1.Intersects(poly2);
    }

    /// <summary>
    /// Merges a list of potentially overlapping polygons
    /// </summary>
    private static Geometry<T> MergeOverlappingPolygons(List<Geometry<T>> polygons)
    {
        if (polygons.IsNullOrEmpty())
            return Geometry<T>.Empty;

        if (polygons.Count == 1)
            return polygons[0];

        var result = polygons[0];
        for (int i = 1; i < polygons.Count; i++)
        {
            result = result.UnionPolygon(polygons[i]);
        }
        return result;
    }

    #endregion


    #region Intersection

    /// <summary>
    /// Computes the intersection of this geometry with another geometry
    /// </summary>
    /// <param name="other">The geometry to intersect with</param>
    /// <returns>The intersection of the two geometries</returns>
    public Geometry<T> Intersection(Geometry<T> other)
    {
        if (this.IsNullOrEmpty() || other.IsNullOrEmpty())
            return Geometry<T>.Empty;

        if (this.Srid != other.Srid)
            throw new ArgumentException("SRIDs must match for Intersection operation");

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
                return IntersectionMultiLineString(other);

            case GeometryType.MultiPolygon:
                return IntersectionMultiPolygon(other);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException($"Intersection not implemented for {this.Type}");
        }
    }

    private Geometry<T> IntersectionPoint(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.Point:
                if (this.Points[0].AreExactlyTheSame(other.Points[0]))
                    return this;
                return Geometry<T>.Empty;

            case GeometryType.LineString:
                if (TopologyUtility.IsPointOnLineString(other, this.Points[0]))
                    return this;
                return Geometry<T>.Empty;

            case GeometryType.Polygon:
                if (TopologyUtility.IsPointInPolygon(other, this.Points[0]))
                    return this;
                return Geometry<T>.Empty;

            case GeometryType.MultiPoint:
                foreach (var pointGeo in other.Geometries)
                {
                    if (this.Points[0].AreExactlyTheSame(pointGeo.Points[0]))
                        return this;
                }
                return Geometry<T>.Empty;

            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                foreach (var geo in other.Geometries)
                {
                    var intersection = this.IntersectionPoint(geo);
                    if (!intersection.IsNullOrEmpty())
                        return intersection;
                }
                return Geometry<T>.Empty;

            default:
                return Geometry<T>.Empty;
        }
    }

    private Geometry<T> IntersectionLineString(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.Point:
                return other.IntersectionPoint(this);

            case GeometryType.LineString:
                // Simplified: check if lines intersect, return intersection points or overlapping segments
                if (this.Intersects(other))
                {
                    // For simplicity, return empty - full implementation would require finding intersection points/segments
                    // This is a placeholder that indicates intersection exists
                    return Geometry<T>.Empty; // TODO: Implement proper line-line intersection
                }
                return Geometry<T>.Empty;

            case GeometryType.Polygon:
                // Return parts of line that are inside polygon
                if (this.Intersects(other))
                {
                    // Simplified: return empty - full implementation would clip line to polygon
                    return Geometry<T>.Empty; // TODO: Implement line-polygon clipping
                }
                return Geometry<T>.Empty;

            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                var intersections = new List<Geometry<T>>();
                foreach (var geo in other.Geometries)
                {
                    var intersection = this.IntersectionLineString(geo);
                    if (!intersection.IsNullOrEmpty())
                        intersections.Add(intersection);
                }
                if (intersections.Count == 0)
                    return Geometry<T>.Empty;
                // Combine intersections
                return intersections[0]; // Simplified

            default:
                return Geometry<T>.Empty;
        }
    }

    private Geometry<T> IntersectionPolygon(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.Point:
                return other.IntersectionPoint(this);

            case GeometryType.LineString:
                return other.IntersectionLineString(this);

            case GeometryType.Polygon:
                return IntersectionPolygons(this, other);

            case GeometryType.MultiPolygon:
                var intersections = new List<Geometry<T>>();
                foreach (var poly in other.Geometries)
                {
                    var intersection = this.IntersectionPolygon(poly);
                    if (!intersection.IsNullOrEmpty())
                        intersections.Add(intersection);
                }
                if (intersections.Count == 0)
                    return Geometry<T>.Empty;
                // Union all intersections
                var result = intersections[0];
                for (int i = 1; i < intersections.Count; i++)
                {
                    result = result.UnionPolygon(intersections[i]);
                }
                return result;

            default:
                return Geometry<T>.Empty;
        }
    }

    private Geometry<T> IntersectionMultiPoint(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.Point:
                return other.IntersectionPoint(this);

            case GeometryType.MultiPoint:
                var commonPoints = new List<Geometry<T>>();
                foreach (var point1 in this.Geometries)
                {
                    foreach (var point2 in other.Geometries)
                    {
                        if (point1.Points[0].AreExactlyTheSame(point2.Points[0]))
                        {
                            commonPoints.Add(point1);
                            break;
                        }
                    }
                }
                if (commonPoints.Count == 0)
                    return Geometry<T>.Empty;
                if (commonPoints.Count == 1)
                    return commonPoints[0];
                return new Geometry<T>(commonPoints, GeometryType.MultiPoint, this.Srid);

            default:
                return Geometry<T>.Empty;
        }
    }

    private Geometry<T> IntersectionMultiLineString(Geometry<T> other)
    {
        var intersections = new List<Geometry<T>>();
        foreach (var line in this.Geometries)
        {
            var intersection = line.IntersectionLineString(other);
            if (!intersection.IsNullOrEmpty())
                intersections.Add(intersection);
        }
        if (intersections.Count == 0)
            return Geometry<T>.Empty;
        // Simplified: return first intersection
        return intersections[0];
    }

    private Geometry<T> IntersectionMultiPolygon(Geometry<T> other)
    {
        var intersections = new List<Geometry<T>>();
        foreach (var poly in this.Geometries)
        {
            var intersection = poly.IntersectionPolygon(other);
            if (!intersection.IsNullOrEmpty())
                intersections.Add(intersection);
        }
        if (intersections.Count == 0)
            return Geometry<T>.Empty;
        // Union all intersections
        var result = intersections[0];
        for (int i = 1; i < intersections.Count; i++)
        {
            result = result.UnionPolygon(intersections[i]);
        }
        return result;
    }

    /// <summary>
    /// Intersects two polygons - simplified implementation
    /// </summary>
    private static Geometry<T> IntersectionPolygons(Geometry<T> poly1, Geometry<T> poly2)
    {
        if (poly1.IsNullOrEmpty() || poly2.IsNullOrEmpty())
            return Geometry<T>.Empty;

        // Check if polygons overlap
        if (!ArePolygonsOverlapping(poly1, poly2))
            return Geometry<T>.Empty;

        // Simplified: For proper intersection, we would need polygon clipping algorithm
        // This is a placeholder that uses bounding box intersection
        // Full implementation would require more complex geometric operations

        // For now, return empty - indicating intersection exists but not computing exact result
        // TODO: Implement proper polygon-polygon intersection using clipping algorithm
        return Geometry<T>.Empty;
    }

    #endregion


    #region Difference

    /// <summary>
    /// Computes the difference of this geometry with another geometry (this - other)
    /// </summary>
    /// <param name="other">The geometry to subtract</param>
    /// <returns>The difference of the two geometries</returns>
    public Geometry<T> Difference(Geometry<T> other)
    {
        if (this.IsNullOrEmpty())
            return Geometry<T>.Empty;

        if (other.IsNullOrEmpty())
            return this;

        if (this.Srid != other.Srid)
            throw new ArgumentException("SRIDs must match for Difference operation");

        switch (this.Type)
        {
            case GeometryType.Point:
                return DifferencePoint(other);

            case GeometryType.LineString:
                return DifferenceLineString(other);

            case GeometryType.Polygon:
                return DifferencePolygon(other);

            case GeometryType.MultiPoint:
                return DifferenceMultiPoint(other);

            case GeometryType.MultiLineString:
                return DifferenceMultiLineString(other);

            case GeometryType.MultiPolygon:
                return DifferenceMultiPolygon(other);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException($"Difference not implemented for {this.Type}");
        }
    }

    private Geometry<T> DifferencePoint(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.Point:
                if (this.Points[0].AreExactlyTheSame(other.Points[0]))
                    return Geometry<T>.Empty;
                return this;

            case GeometryType.LineString:
            case GeometryType.Polygon:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                if (other.IntersectsPoint(this.Points[0]))
                    return Geometry<T>.Empty;
                return this;

            case GeometryType.MultiPoint:
                foreach (var pointGeo in other.Geometries)
                {
                    if (this.Points[0].AreExactlyTheSame(pointGeo.Points[0]))
                        return Geometry<T>.Empty;
                }
                return this;

            default:
                return this;
        }
    }

    private Geometry<T> DifferenceLineString(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.Point:
                if (TopologyUtility.IsPointOnLineString(this, other.Points[0]))
                {
                    // Remove point from line - simplified: return line as-is
                    return this; // TODO: Implement point removal from line
                }
                return this;

            case GeometryType.LineString:
            case GeometryType.Polygon:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                if (!this.Intersects(other))
                    return this;
                // Simplified: return empty if intersects - full implementation would clip
                return Geometry<T>.Empty; // TODO: Implement proper line clipping

            case GeometryType.MultiPoint:
                return this; // Points don't affect line geometry

            default:
                return this;
        }
    }

    private Geometry<T> DifferencePolygon(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.Point:
                return this; // Point doesn't affect polygon

            case GeometryType.LineString:
                return this; // Line doesn't affect polygon (simplified)

            case GeometryType.Polygon:
                return DifferencePolygons(this, other);

            case GeometryType.MultiPolygon:
                var result = this;
                foreach (var poly in other.Geometries)
                {
                    result = result.DifferencePolygon(poly);
                    if (result.IsNullOrEmpty())
                        break;
                }
                return result;

            default:
                return this;
        }
    }

    private Geometry<T> DifferenceMultiPoint(Geometry<T> other)
    {
        switch (other.Type)
        {
            case GeometryType.Point:
                var remainingPoints = new List<Geometry<T>>();
                foreach (var pointGeo in this.Geometries)
                {
                    if (!pointGeo.Points[0].AreExactlyTheSame(other.Points[0]))
                        remainingPoints.Add(pointGeo);
                }
                if (remainingPoints.Count == 0)
                    return Geometry<T>.Empty;
                if (remainingPoints.Count == 1)
                    return remainingPoints[0];
                return new Geometry<T>(remainingPoints, GeometryType.MultiPoint, this.Srid);

            case GeometryType.MultiPoint:
                var points = new List<Geometry<T>>();
                foreach (var point1 in this.Geometries)
                {
                    bool found = false;
                    foreach (var point2 in other.Geometries)
                    {
                        if (point1.Points[0].AreExactlyTheSame(point2.Points[0]))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        points.Add(point1);
                }
                if (points.Count == 0)
                    return Geometry<T>.Empty;
                if (points.Count == 1)
                    return points[0];
                return new Geometry<T>(points, GeometryType.MultiPoint, this.Srid);

            default:
                return this;
        }
    }

    private Geometry<T> DifferenceMultiLineString(Geometry<T> other)
    {
        var remainingLines = new List<Geometry<T>>();
        foreach (var line in this.Geometries)
        {
            var diff = line.DifferenceLineString(other);
            if (!diff.IsNullOrEmpty())
                remainingLines.Add(diff);
        }
        if (remainingLines.Count == 0)
            return Geometry<T>.Empty;
        if (remainingLines.Count == 1)
            return remainingLines[0];
        return new Geometry<T>(remainingLines, GeometryType.MultiLineString, this.Srid);
    }

    private Geometry<T> DifferenceMultiPolygon(Geometry<T> other)
    {
        var remainingPolygons = new List<Geometry<T>>();
        foreach (var poly in this.Geometries)
        {
            var diff = poly.DifferencePolygon(other);
            if (!diff.IsNullOrEmpty())
                remainingPolygons.Add(diff);
        }
        if (remainingPolygons.Count == 0)
            return Geometry<T>.Empty;
        if (remainingPolygons.Count == 1)
            return remainingPolygons[0];
        return new Geometry<T>(remainingPolygons, GeometryType.MultiPolygon, this.Srid);
    }

    /// <summary>
    /// Subtracts polygon2 from polygon1 - simplified implementation
    /// </summary>
    private static Geometry<T> DifferencePolygons(Geometry<T> poly1, Geometry<T> poly2)
    {
        if (poly1.IsNullOrEmpty())
            return Geometry<T>.Empty;

        if (poly2.IsNullOrEmpty())
            return poly1;

        // Check if polygons overlap
        if (!ArePolygonsOverlapping(poly1, poly2))
            return poly1;

        // Simplified: For proper difference, we would need polygon clipping algorithm
        // This is a placeholder
        // Full implementation would require subtracting poly2 from poly1 using boolean operations

        // For now, return empty if poly1 is completely inside poly2
        var testPoint = poly1.Geometries[0].Points[0];
        if (TopologyUtility.IsPointInPolygon(poly2, testPoint))
        {
            // Check if poly1 is completely contained
            // Simplified: return empty
            return Geometry<T>.Empty; // TODO: Implement proper polygon difference
        }

        return poly1; // Simplified: return original if not completely contained
    }

    #endregion


    #region Buffer

    /// <summary>
    /// Creates a buffer around this geometry at the specified distance
    /// </summary>
    /// <param name="distance">The buffer distance</param>
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

    private Geometry<T> BufferPoint(double distance)
    {
        var center = this.Points[0];
        return CreateCircle(center, distance, 64, this.Srid);
    }

    private Geometry<T> BufferLineString(double distance)
    {
        if (this.Points == null || this.Points.Count < 2)
            return Geometry<T>.Empty;

        var offsetPoints = OffsetLineSegment(this.Points, distance, false);
        if (offsetPoints.Count < 2)
            return Geometry<T>.Empty;

        // Add end caps (semi-circles)
        var startCap = CreateSemiCircle(this.Points[0], this.Points[1], distance, true, this.Srid);
        var endCap = CreateSemiCircle(this.Points[this.Points.Count - 1], this.Points[this.Points.Count - 2], distance, false, this.Srid);

        // Combine: start cap + offset line + end cap (reversed)
        var allPoints = new List<T>();

        // Add start cap points (if not degenerate)
        if (startCap.Type == GeometryType.LineString && startCap.Points != null)
        {
            allPoints.AddRange(startCap.Points);
        }
        else if (startCap.Type == GeometryType.Point && startCap.Points != null && startCap.Points.Count > 0)
        {
            allPoints.Add(startCap.Points[0]);
        }

        // Add offset line points
        allPoints.AddRange(offsetPoints);

        // Add end cap points (reversed, if not degenerate)
        if (endCap.Type == GeometryType.LineString && endCap.Points != null)
        {
            var reversedEndCap = new List<T>(endCap.Points);
            reversedEndCap.Reverse();
            allPoints.AddRange(reversedEndCap);
        }
        else if (endCap.Type == GeometryType.Point && endCap.Points != null && endCap.Points.Count > 0)
        {
            allPoints.Add(endCap.Points[0]);
        }

        // Close the polygon if not already closed
        if (allPoints.Count > 0)
        {
            var firstPoint = allPoints[0];
            var lastPoint = allPoints[allPoints.Count - 1];
            if (!firstPoint.AreExactlyTheSame(lastPoint))
            {
                allPoints.Add(new T() { X = firstPoint.X, Y = firstPoint.Y });
            }
        }

        if (allPoints.Count < 3)
            return Geometry<T>.Empty;

        return new Geometry<T>(allPoints, GeometryType.Polygon, this.Srid);
    }

    private Geometry<T> BufferPolygon(double distance)
    {
        if (this.Geometries == null || this.Geometries.Count == 0)
            return Geometry<T>.Empty;

        var bufferedRings = new List<Geometry<T>>();

        // Buffer exterior ring outward
        var exteriorRing = this.Geometries[0];
        var bufferedExterior = OffsetLineSegment(exteriorRing.Points, distance, true);
        if (bufferedExterior.Count >= 3)
        {
            bufferedRings.Add(new Geometry<T>(bufferedExterior, GeometryType.LineString, this.Srid));
        }

        // Buffer interior rings (holes) inward (negative distance)
        for (int i = 1; i < this.Geometries.Count; i++)
        {
            var hole = this.Geometries[i];
            if (!ShouldEliminateHole(hole, distance))
            {
                var bufferedHole = OffsetLineSegment(hole.Points, -distance, true);
                if (bufferedHole.Count >= 3)
                {
                    // Check if buffered hole is still inside the buffered exterior
                    var testPoint = bufferedHole[0];
                    if (TopologyUtility.IsPointInRing(bufferedRings[0], testPoint))
                    {
                        bufferedRings.Add(new Geometry<T>(bufferedHole, GeometryType.LineString, this.Srid));
                    }
                }
            }
        }

        if (bufferedRings.Count == 0)
            return Geometry<T>.Empty;

        return CreatePolygonOrMultiPolygon(bufferedRings, this.Srid);
    }

    private Geometry<T> BufferMultiPoint(double distance)
    {
        var bufferedPolygons = new List<Geometry<T>>();
        foreach (var pointGeo in this.Geometries)
        {
            var buffered = pointGeo.BufferPoint(distance);
            bufferedPolygons.Add(buffered);
        }

        // Union all buffered circles
        if (bufferedPolygons.Count == 0)
            return Geometry<T>.Empty;

        var result = bufferedPolygons[0];
        for (int i = 1; i < bufferedPolygons.Count; i++)
        {
            result = result.UnionPolygon(bufferedPolygons[i]);
        }
        return result;
    }

    private Geometry<T> BufferMultiLineString(double distance)
    {
        var bufferedPolygons = new List<Geometry<T>>();
        foreach (var lineGeo in this.Geometries)
        {
            var buffered = lineGeo.BufferLineString(distance);
            bufferedPolygons.Add(buffered);
        }

        // Union all buffered polygons
        if (bufferedPolygons.Count == 0)
            return Geometry<T>.Empty;

        var result = bufferedPolygons[0];
        for (int i = 1; i < bufferedPolygons.Count; i++)
        {
            result = result.UnionPolygon(bufferedPolygons[i]);
        }
        return result;
    }

    private Geometry<T> BufferMultiPolygon(double distance)
    {
        var bufferedPolygons = new List<Geometry<T>>();
        foreach (var polyGeo in this.Geometries)
        {
            var buffered = polyGeo.BufferPolygon(distance);
            if (!buffered.IsNullOrEmpty())
            {
                bufferedPolygons.Add(buffered);
            }
        }

        // Union all buffered polygons
        if (bufferedPolygons.Count == 0)
            return Geometry<T>.Empty;

        var result = bufferedPolygons[0];
        for (int i = 1; i < bufferedPolygons.Count; i++)
        {
            result = result.UnionPolygon(bufferedPolygons[i]);
        }
        return result;
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

        // Close the polygon
        points.Add(new T() { X = points[0].X, Y = points[0].Y });

        return new Geometry<T>(points, GeometryType.Polygon, srid);
    }

    /// <summary>
    /// Creates a semi-circle cap for line endpoints
    /// </summary>
    private static Geometry<T> CreateSemiCircle(T center, T directionPoint, double radius, bool isStart, int srid)
    {
        var points = new List<T>();
        int segments = 16;
        double angleStep = Math.PI / segments;

        // Calculate direction vector
        double dx = directionPoint.X - center.X;
        double dy = directionPoint.Y - center.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length < SpatialUtility.EpsilonDistance)
            return new Geometry<T>(new List<T> { center }, GeometryType.Point, srid);

        // Perpendicular direction (rotate 90 degrees)
        double perpX = -dy / length;
        double perpY = dx / length;

        // Start angle
        double startAngle = Math.Atan2(perpY, perpX);
        if (!isStart)
            startAngle += Math.PI;

        for (int i = 0; i <= segments; i++)
        {
            double angle = startAngle + i * angleStep;
            double x = center.X + radius * Math.Cos(angle);
            double y = center.Y + radius * Math.Sin(angle);
            points.Add(new T() { X = x, Y = y });
        }

        return new Geometry<T>(points, GeometryType.LineString, srid);
    }

    /// <summary>
    /// Offsets a line segment or ring by a specified distance
    /// </summary>
    private List<T> OffsetLineSegment(List<T> points, double distance, bool isClosed)
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
                    // Start point: use next segment direction
                    curr = points[i];
                    next = points[i + 1];
                    double dx = next.X - curr.X;
                    double dy = next.Y - curr.Y;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len < SpatialUtility.EpsilonDistance)
                    {
                        offsetPoints.Add(new T() { X = curr.X, Y = curr.Y });
                        continue;
                    }
                    double perpX = -dy / len * distance;
                    double perpY = dx / len * distance;
                    offsetPoints.Add(new T() { X = curr.X + perpX, Y = curr.Y + perpY });
                    continue;
                }
                else if (i == count - 1)
                {
                    // End point: use previous segment direction
                    prev = points[i - 1];
                    curr = points[i];
                    double dx = curr.X - prev.X;
                    double dy = curr.Y - prev.Y;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len < SpatialUtility.EpsilonDistance)
                    {
                        offsetPoints.Add(new T() { X = curr.X, Y = curr.Y });
                        continue;
                    }
                    double perpX = -dy / len * distance;
                    double perpY = dx / len * distance;
                    offsetPoints.Add(new T() { X = curr.X + perpX, Y = curr.Y + perpY });
                    continue;
                }
                else
                {
                    prev = points[i - 1];
                    curr = points[i];
                    next = points[i + 1];
                }
            }

            // Calculate offset at vertex
            var offsetPoint = OffsetPoint(curr, prev, next, distance);
            offsetPoints.Add(offsetPoint);
        }

        return offsetPoints;
    }

    /// <summary>
    /// Calculates the offset point at a vertex
    /// </summary>
    private static T OffsetPoint(T point, T prevPoint, T nextPoint, double distance)
    {
        // Calculate direction vectors
        double dx1 = point.X - prevPoint.X;
        double dy1 = point.Y - prevPoint.Y;
        double len1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);

        double dx2 = nextPoint.X - point.X;
        double dy2 = nextPoint.Y - point.Y;
        double len2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);

        if (len1 < SpatialUtility.EpsilonDistance || len2 < SpatialUtility.EpsilonDistance)
        {
            // Degenerate case: use perpendicular to available segment
            if (len1 >= SpatialUtility.EpsilonDistance)
            {
                double perpX = -dy1 / len1 * distance;
                double perpY = dx1 / len1 * distance;
                return new T() { X = point.X + perpX, Y = point.Y + perpY };
            }
            else if (len2 >= SpatialUtility.EpsilonDistance)
            {
                double perpX = -dy2 / len2 * distance;
                double perpY = dx2 / len2 * distance;
                return new T() { X = point.X + perpX, Y = point.Y + perpY };
            }
            return new T() { X = point.X, Y = point.Y };
        }

        // Normalize direction vectors
        dx1 /= len1;
        dy1 /= len1;
        dx2 /= len2;
        dy2 /= len2;

        // Calculate perpendicular vectors
        double perp1X = -dy1;
        double perp1Y = dx1;
        double perp2X = -dy2;
        double perp2Y = dx2;

        // Calculate bisector direction
        double bisectorX = perp1X + perp2X;
        double bisectorY = perp1Y + perp2Y;
        double bisectorLen = Math.Sqrt(bisectorX * bisectorX + bisectorY * bisectorY);

        if (bisectorLen < SpatialUtility.EpsilonDistance)
        {
            // Parallel segments: use perpendicular direction
            return new T() { X = point.X + perp1X * distance, Y = point.Y + perp1Y * distance };
        }

        // Normalize bisector
        bisectorX /= bisectorLen;
        bisectorY /= bisectorLen;

        // Calculate offset distance along bisector
        // Use miter limit to prevent excessive offsets at sharp angles
        double angle = Math.Acos(Math.Max(-1, Math.Min(1, dx1 * dx2 + dy1 * dy2)));
        double offsetDist = distance / Math.Sin(angle / 2.0);

        // Apply miter limit (prevent excessive offsets)
        double miterLimit = 5.0; // Maximum miter length as multiple of distance
        if (offsetDist > distance * miterLimit)
        {
            // Use simple perpendicular offset instead
            return new T() { X = point.X + perp1X * distance, Y = point.Y + perp1Y * distance };
        }

        return new T() { X = point.X + bisectorX * offsetDist, Y = point.Y + bisectorY * offsetDist };
    }

    /// <summary>
    /// Checks if a hole should be eliminated due to buffer distance
    /// </summary>
    private static bool ShouldEliminateHole(Geometry<T> hole, double bufferDistance)
    {
        if (hole.IsNullOrEmpty() || hole.Points == null || hole.Points.Count < 3)
            return true;

        // Calculate approximate hole size (minimum distance from center to edge)
        var center = hole.GetMeanPoint();
        double minDistance = double.MaxValue;
        foreach (var point in hole.Points)
        {
            double dist = SpatialUtility.GetEuclideanLength(center, point);
            if (dist < minDistance)
                minDistance = dist;
        }

        // If buffer distance exceeds hole size, hole will be eliminated
        return bufferDistance >= minDistance;
    }

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
            return null;
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
            case GeometryType.LineString:
            case GeometryType.MultiLineString:
                return this.GetLastPoint();

            case GeometryType.Polygon:
            case GeometryType.MultiPolygon:
                return this.GetMeanPoint();

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

    public void StartNewGeometry(T startPoint, GeometryType type)
    {
        if (this.Geometries == null)
            throw new NotImplementedException();

        var geometries = this.Geometries.ToList();

        geometries.Add(new Geometry<T>(new List<T>() { startPoint }, type, this.Srid));

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
            return new Geometry<T>(this.Geometries.Select(g => g.Clone()).ToList(), this.Type, this.Srid);
        }

        return new Geometry<T>(null, this.Type, false, this.Srid);
    }

    public Geometry<T> NeutralizeGenericPoint()
    {
        if (this.Points != null)
        {
            List<T> points = new List<T>(this.Points.Count);

            for (int i = 0; i < this.Points.Count; i++)
            {
                points.Add(new T() { X = this.Points[i].X, Y = this.Points[i].Y });
            }

            return Geometry<T>.Create(points, this.Type, this.Srid);
        }
        if (this.Geometries != null)
        {
            return new Geometry<T>(this.Geometries.Select(g => g.NeutralizeGenericPoint()).ToList(), this.Type, this.Srid);
        }

        return new Geometry<T>(null, this.Type, false, this.Srid);
    }

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

    public void InsertPoint(T newPoint, int index)
    {
        var points = this.Points.ToList();

        points.Insert(index, newPoint);

        this.Points = points.ToList();
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
            return null;

        if (this.IsLeafGeometry())
        {
            return this.Points;
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
        if (!currentGeometry.IsValid())
        {
            return false;
        }

        switch (this.Type)
        {
            case GeometryType.Point:
                this.Geometries = new List<Geometry<T>>();
                this.Geometries.Add(currentGeometry);
                this.Geometries.Add(new Geometry<T>(new List<T>(), GeometryType.Point, this.Srid));
                this.Points = null;
                this.Type = GeometryType.MultiPoint;
                break;
            //this.Geometries = new Geometry[2];
            //this.Geometries[0] = currentGeometry;
            //this.Geometries[1] = new Geometry(new IPoint[0], GeometryType.Point, this.Srid);
            //this.Points = null;
            //this.Type = GeometryType.MultiPoint;
            //break;

            case GeometryType.LineString:
                this.Points = null;
                this.Geometries = new List<Geometry<T>>();
                this.Geometries.Add(currentGeometry);
                this.Geometries.Add(new Geometry<T>(new List<T>(), GeometryType.LineString, this.Srid));
                this.Type = GeometryType.MultiLineString;
                break;

            //this.Points = null;
            //this.Geometries = new Geometry[2];
            //this.Geometries[0] = currentGeometry;
            //this.Geometries[1] = new Geometry(new IPoint[0], GeometryType.LineString, this.Srid);

            //this.Type = GeometryType.MultiLineString;
            //break;

            case GeometryType.Polygon:
                this.Geometries = new List<Geometry<T>>();
                this.Geometries.Add(currentGeometry);
                this.Geometries.Add(new Geometry<T>(new List<Geometry<T>>() { new Geometry<T>(new List<T>(), GeometryType.LineString, this.Srid) }, GeometryType.Polygon, this.Srid));
                this.Type = GeometryType.MultiPolygon;
                break;

            //this.Geometries = new Geometry[2];
            //this.Geometries[0] = currentGeometry;
            //this.Geometries[1] = new Geometry(new Geometry[] { new Geometry(new IPoint[0], GeometryType.LineString, this.Srid) }, GeometryType.Polygon, this.Srid);
            //this.Type = GeometryType.MultiPolygon;
            //break;

            case GeometryType.MultiPoint:
                this.Geometries.Add(new Geometry<T>(new List<T>(), GeometryType.Point, this.Srid));
                break;
            //var points = this.Geometries.ToList();
            //points.Add(new Geometry(new IPoint[0], GeometryType.Point, this.Srid));
            //this.Geometries = points.ToList();
            //break;

            case GeometryType.MultiLineString:
                this.Geometries.Add(new Geometry<T>(new List<T>(), GeometryType.LineString, this.Srid));
                break;

            //var lines = this.Geometries.ToList();
            //lines.Add(new Geometry(new IPoint[0], GeometryType.LineString, this.Srid));
            //this.Geometries = lines.ToList();
            //break;

            case GeometryType.MultiPolygon:
                this.Geometries.Add(new Geometry<T>(new List<Geometry<T>> { new Geometry<T>(new List<T>(), GeometryType.LineString, this.Srid) }, GeometryType.Polygon, this.Srid));
                break;

            //var polygons = this.Geometries.ToList();
            //polygons.Add(new Geometry(new Geometry[] { new Geometry(new IPoint[0], GeometryType.LineString, this.Srid) }, GeometryType.Polygon, this.Srid));
            //this.Geometries = polygons.ToList();
            //break;


            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }

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
        var points = this.GetAllPoints();
        var xList = points.Select(p => p.X).ToList();
        var yList = points.Select(p => p.Y).ToList();
        var result = ComputationalGeometry.CreateConvexHull(new PointCollection(xList, yList));

        return Create(result.Select(r => new T() { X = r.X, Y = r.Y }).ToList(), GeometryType.Polygon, this.Srid);
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
                return new Geometry<T>(this.Geometries.Select(g => g.GetBoundary()).ToList(), GeometryType.MultiPolygon, this.Srid);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("Geometry > GetBoundary > not supported type!");
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

    public string AsWkt()
    {
        return WktWriter.AsWkt(this);
    }

    public byte[]? AsWkb()
    {
        return WkbWriter.AsWkb(this);
    }

    public string AsWkbHexString()
    {
        return IRI.Maptor.Sta.Common.Helpers.HexStringHelper.ToHexStringUsingBitFiddle(AsWkb(), append0x: true);
    }

    #endregion


    #region Sql Server Native Binary

    public byte[] AsSqlServerByte()
    {
        //using (var ms = new MemoryStream())
        //using (var bw = new BinaryWriter(ms))
        //{
        //    bw.Write(Srid);          // SRID (little-endian)
        //    bw.Write((byte)0x01);    // Version marker
        //    bw.Write((byte)0x0C);    // Version marker

        //    // Write the actual geography bytes
        //    var wkb = AsWkb(); 
        //    bw.Write(wkb);

        //    return ms.ToArray();
        //}

        // ************************************************************
        //string hex = "0xE6100000010C10FA420AD60941406069CF8839A54840";

        //if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        //    hex = hex.Substring(2);

        //var result = Enumerable.Range(0, hex.Length / 2)
        //.Select(x => Convert.ToByte(hex.Substring(x * 2, 2), 16))
        //.ToArray();

        //var hex2 = "0x" + BitConverter.ToString(result).Replace("-", "");
        // ************************************************************


        return null;
    }



    #endregion


    #region Conversions

    public Feature<T> AsFeature()
    {
        return new Feature<T>(this, new Dictionary<string, object>() { { "_", " " } });
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

    //public static readonly Geometry EmptyPoint = new Geometry(new IPoint[0], GeometryType.Point);
    //public static readonly Geometry EmptyLineString = new Geometry(new IPoint[0], GeometryType.LineString);
    //public static readonly Geometry EmptyPolygon = new Geometry(new IPoint[0], GeometryType.Polygon);
    //public static readonly Geometry EmptyMultiPoint = new Geometry(new Geometry[0], GeometryType.MultiPoint);
    //public static readonly Geometry EmptyMultiLineString = new Geometry(new Geometry[0], GeometryType.MultiLineString);
    //public static readonly Geometry EmptyMultiPolygon = new Geometry(new Geometry[0], GeometryType.MultiPolygon);

    public static Geometry<T> CreateEmpty(GeometryType type, int srid)
    {
        switch (type)
        {
            case GeometryType.GeometryCollection:
                return new Geometry<T>(new List<Geometry<T>>(), GeometryType.GeometryCollection, srid);

            case GeometryType.LineString:
                return new Geometry<T>(new List<T>(), GeometryType.LineString, srid);

            case GeometryType.MultiLineString:
                return new Geometry<T>(new List<Geometry<T>>(), GeometryType.MultiLineString, srid);

            case GeometryType.MultiPoint:
                return new Geometry<T>(new List<Geometry<T>>(), GeometryType.MultiPoint, srid);

            case GeometryType.MultiPolygon:
                return new Geometry<T>(new List<Geometry<T>>(), GeometryType.MultiPolygon, srid);

            case GeometryType.Point:
                return new Geometry<T>(new List<T>(), GeometryType.Point, srid);

            case GeometryType.Polygon:
                return new Geometry<T>(new List<Geometry<T>>(), GeometryType.Polygon, srid);

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }
    }

    public static Geometry<T> Create(double x, double y, int srid = 0)
    {
        return new Geometry<T>(new List<T> { new T() { X = x, Y = y } }, GeometryType.Point, srid);
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
        if (points == null)
            CreateEmpty(type, srid);

        switch (type)
        {
            case GeometryType.Point:
                return new Geometry<T>(points!, GeometryType.Point, srid);
            //return CreatePointOrLineString(points, srid); //do not use this method: what if making a new Geometry LineString with first point

            case GeometryType.LineString:
                return new Geometry<T>(points!, GeometryType.LineString, srid);
            //return CreatePointOrLineString(points, srid);

            case GeometryType.Polygon:
                return new Geometry<T>(new Geometry<T>(points!, GeometryType.LineString, srid), GeometryType.Polygon, srid);

            case GeometryType.MultiPoint:
                return new Geometry<T>(points.Select(p => Geometry<T>.Create(p.X, p.Y, srid)).ToList(), GeometryType.MultiPoint, srid);

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
            return new Geometry<T>(points, GeometryType.Point, srid);

        else
            return new Geometry<T>(points, GeometryType.LineString, srid);
    }

    public static Geometry<T> CreatePointOrLineStringOrRing(List<T> points, int srid)
    {
        if (points.Count > 2 && points[0].AreExactlyTheSame(points[points.Count - 1]))

            return Geometry<T>.Create(points.Take(points.Count - 1).ToList(), GeometryType.Polygon, srid);

        else
            return CreatePointOrLineString(points, srid);
    }

    public static Geometry<T> CreatePointOrLineString(int srid, params T[] points)
    {
        return CreatePointOrLineString(points.ToList(), srid);
    }

    public static Geometry<T> CreateLineStringFromPoints(List<Geometry<T>> geometries)
    {
        if (geometries.IsNullOrEmpty())
            return Geometry<T>.Empty;

        var points = geometries.Select(g => g.AsPoint()).ToList();

        return Geometry<T>.CreatePointOrLineString(points, geometries?.FirstOrDefault()?.Srid ?? 0);
    }



    public static Geometry<T> CreatePolygonOrMultiPolygon(List<Geometry<T>> rings, int srid)
    {
        // OGC SFA for polygon:
        // The exterior boundary LinearRing defines the “top” of the surface which is the side of the surface from which the
        // exterior boundary appears to traverse the boundary in a counter clockwise direction. The interior LinearRings will
        // have the opposite orientation, and appear as clockwise when viewed from the “top”,
        if (rings.IsNullOrEmpty())
        {
            return Geometry<T>.CreateEmpty(GeometryType.Polygon, srid);
        }

        if (rings.Count == 1)
        {
            return new Geometry<T>(rings, GeometryType.Polygon, srid);
        }

        var orderedRings = rings.Select(p => (area: p.EuclideanArea, geo: p)).OrderByDescending(i => i.area).ToList();

        var masterPolygons = new List<Geometry<T>>();

        for (int i = 0; i < orderedRings.Count; i++)
        {
            var currentRing = orderedRings[i].geo;

            bool isMasterRing = true;

            for (int p = 0; p < masterPolygons.Count; p++)
            {
                if (TopologyUtility.IsPointInRing(masterPolygons[p].Geometries[0], currentRing.Points.First()))
                {
                    //not in any of polygon holes
                    if (masterPolygons[p].Geometries.Skip(1).Any(g => TopologyUtility.IsPointInRing(g, currentRing.Points.First())))
                        continue;

                    isMasterRing = false;

                    // inner rings must be CW
                    if (!SpatialUtility.IsClockwise(currentRing.Points))
                    {
                        currentRing.Reverse();
                    }

                    masterPolygons[p].Geometries.Add(currentRing);
                }
            }
            if (isMasterRing)
            {
                // outter ring must be CCW
                if (SpatialUtility.IsClockwise(currentRing.Points))
                {
                    currentRing.Reverse();
                }

                masterPolygons.Add(new Geometry<T>(currentRing, GeometryType.Polygon, srid));
            }
        }

        return masterPolygons.Count == 1 ?
            masterPolygons.First() :
            new Geometry<T>(masterPolygons, GeometryType.MultiPolygon, srid);
    }

    #endregion


    #region Static Methods


    // To do: provide sample input and expected output for this method  
    public static Geometry<T> ParseToGeodeticGeometry(double[][] geoCoordinates, GeometryType geometryType, bool isLongitudeFirst = true)
    {
        return new Geometry<T>(geoCoordinates.Select(p => ParseLineStringToGeodeticGeometry(p.ToList(), isLongitudeFirst)).ToList(), geometryType, SridHelper.GeodeticWGS84);
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

        return new Geometry<T>(result, GeometryType.LineString, SridHelper.GeodeticWGS84);

    }


    //public static Point ParsePointToGeometry(double[] values, bool isLongitudeFirst)
    //{
    //    if (values == null || values.Count() != 2)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    if (isLongitudeFirst)
    //    {
    //        return new Point(values[0], values[1]);
    //    }
    //    else
    //    {
    //        return new Point(values[1], values[0]);
    //    }
    //}

    public static Geometry<T> ParsePointToGeometry(double[] xy, bool isLongitudeFirst, int srid = SridHelper.GeodeticWGS84)
    {
        return new Geometry<T>(new List<T>() { Point.Parse<T>(xy, isLongitudeFirst) }, GeometryType.Point, srid);
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
            return new Geometry<T>(geoCoordinates.Take(numberOfPoints - 1).Select(p => Point.Parse<T>(p, isLongitudeFirst)).ToList(), geometryType, srid);
        }
        else
        {
            return new Geometry<T>(geoCoordinates.Select(p => Point.Parse<T>(p, isLongitudeFirst)).ToList(), geometryType, srid);
        }
    }

    public static Geometry<T> ParsePolygonToGeometry(double[][][] rings, GeometryType geometryType, bool isLongFirst, int srid = SridHelper.GeodeticWGS84)
    {
        return new Geometry<T>(rings.Select(p => ParseLineStringToGeometry(p, GeometryType.LineString, true, isLongFirst, srid)).ToList(), geometryType, srid);
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


    private void ReevaluateGeometryType()
    {
        if (this == null || this.IsNullOrEmpty())
        {
            return;
        }

        var numberOfPoints = this.Points?.Count;

        var numberOfGeometries = this.Geometries?.Count;

        //حالت نقطه یا خط
        if (numberOfPoints > 0)
        {
            if (numberOfPoints == 1)
            {
                this.Type = GeometryType.Point;
            }
            else
            {
                this.Type = GeometryType.LineString;
            }
        }
        //سایر حالت‌ها
        else if (numberOfGeometries > 0)
        {
            var types = this.Geometries.Select(g => g.Type).Distinct();

            //حالت ترکیبی
            if (types.Count() > 1)
            {
                this.Type = GeometryType.GeometryCollection;
            }
            else
            {
                var subGeometryType = this.Geometries.First().Type;

                switch (subGeometryType)
                {
                    //حالت چند نقطه‌ای
                    case GeometryType.Point:
                        this.Type = GeometryType.MultiPoint;
                        break;

                    //حالت چند خطی یا چند ضلعی
                    case GeometryType.LineString:
                        if (this.Type == GeometryType.LineString)
                            this.Type = GeometryType.MultiLineString;
                        else if (this.Type == GeometryType.Polygon)
                            // 1400.06.28
                            //this.Type = GeometryType.MultiPolygon;
                            this.Type = GeometryType.Polygon;

                        break;

                    //حالت چندضلعی‌های چند تکه‌ای
                    case GeometryType.Polygon:
                        this.Type = GeometryType.MultiPolygon;
                        break;

                    case GeometryType.MultiPoint:
                    case GeometryType.MultiLineString:
                    case GeometryType.MultiPolygon:
                        this.Type = GeometryType.GeometryCollection;
                        break;

                    case GeometryType.GeometryCollection:
                    case GeometryType.CircularString:
                    case GeometryType.CompoundCurve:
                    case GeometryType.CurvePolygon:
                    default:
                        throw new NotImplementedException("Geometry.cs > ReevaluateGeometryType");
                }
            }
        }

        return;
    }

    #endregion


}
