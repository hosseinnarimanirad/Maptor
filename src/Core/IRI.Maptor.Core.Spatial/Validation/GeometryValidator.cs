using System;
using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.Spatial.Validation;

/// <summary>
/// Checks feature geometries for problems that break later processing (area, editing, export) and works out how
/// each can be repaired. Self-intersections are not checked.
/// </summary>
/// <remarks>
/// Polygon ring 0 is the outer ring; it must be counterclockwise and holes clockwise, the same convention as
/// <see cref="Geometry{T}.FixPolygonRingOrientations"/>. Points are compared by exact X/Y.
/// </remarks>
public static class GeometryValidator
{
    /// <summary>
    /// Checks every feature. The features are not changed; pass the report to <see cref="GeometryRepair"/> to get
    /// the features to add. Null entries are ignored.
    /// </summary>
    public static GeometryValidationReport<T> Validate<T>(IReadOnlyList<Feature<T>> features) where T : IPoint, new()
    {
        if (features is null)
            throw new ArgumentNullException(nameof(features));

        var problems = new List<FeatureValidationResult<T>>();

        for (int i = 0; i < features.Count; i++)
        {
            if (features[i] is null)
                continue;

            var result = Validate(features[i], i);

            if (result is not null)
                problems.Add(result);
        }

        return new GeometryValidationReport<T>(features, problems);
    }

    /// <summary>
    /// Checks one feature; returns null when nothing was found.
    /// </summary>
    public static FeatureValidationResult<T>? Validate<T>(Feature<T> feature, int featureIndex = 0) where T : IPoint, new()
    {
        if (feature is null)
            throw new ArgumentNullException(nameof(feature));

        var checker = new Checker<T>(feature, featureIndex);

        Geometry<T>? repaired = null;

        if (feature.TheGeometry is null || feature.TheGeometry.IsEmpty())
            checker.Add(GeometryIssueKind.NoGeometry, null, null);
        else
            repaired = checker.Check(feature.TheGeometry, null);

        if (checker.Issues.Count == 0)
            return null;

        var outcome = checker.Issues.Any(i => !i.CanFix) ? GeometryRepairOutcome.Unrepairable
                    : repaired is null ? GeometryRepairOutcome.Removed
                    : GeometryRepairOutcome.Repaired;

        return new FeatureValidationResult<T>(
            feature,
            featureIndex,
            checker.Issues,
            outcome,
            outcome == GeometryRepairOutcome.Repaired ? repaired : null);
    }

    /// <summary>
    /// Walks one geometry, records its problems and builds the repaired geometry: the same instance when nothing
    /// changes, a new one otherwise, or null when nothing is left. The input geometry is never modified.
    /// </summary>
    private sealed class Checker<T> where T : IPoint, new()
    {
        private readonly Feature<T> _feature;

        private readonly int _featureIndex;

        private readonly bool _isGeodetic;

        public Checker(Feature<T> feature, int featureIndex)
        {
            _feature = feature;
            _featureIndex = featureIndex;
            _isGeodetic = feature.TheGeometry?.Srid == SridHelper.GeodeticWGS84;
        }

        public List<GeometryIssue<T>> Issues { get; } = new List<GeometryIssue<T>>();

        public void Add(GeometryIssueKind kind, int? partIndex, int? ringIndex)
        {
            Issues.Add(new GeometryIssue<T>(_feature, _featureIndex, kind, partIndex, ringIndex));
        }

        public Geometry<T>? Check(Geometry<T> geometry, int? partIndex)
        {
            switch (geometry.Type)
            {
                case GeometryType.Point:
                case GeometryType.LineString:
                    {
                        var minCount = geometry.Type == GeometryType.Point ? 1 : 2;

                        var points = CheckPath(geometry.Points, minCount, isRing: false, partIndex, null, out _);

                        if (points is null)
                            return null;

                        return ReferenceEquals(points, geometry.Points) ? geometry : Geometry<T>.Create(points, geometry.Type, geometry.Srid);
                    }

                case GeometryType.Polygon:
                    return CheckPolygon(geometry, partIndex);

                case GeometryType.MultiPoint:
                case GeometryType.MultiLineString:
                case GeometryType.MultiPolygon:
                case GeometryType.GeometryCollection:
                    return CheckParts(geometry, partIndex);

                default:
                    // curves are not checked
                    return geometry;
            }
        }

        private Geometry<T>? CheckParts(Geometry<T> geometry, int? partIndex)
        {
            if (geometry.Geometries is null || geometry.Geometries.Count == 0)
            {
                Add(GeometryIssueKind.TooFewPoints, partIndex, null);
                return null;
            }

            var kept = new List<Geometry<T>>(geometry.Geometries.Count);

            var changed = false;

            for (int i = 0; i < geometry.Geometries.Count; i++)
            {
                var part = geometry.Geometries[i];

                // a part of a nested collection is reported under its top-level part
                var repaired = part is null ? null : Check(part, partIndex ?? i);

                if (repaired is null)
                {
                    changed = true;
                    continue;
                }

                if (!ReferenceEquals(repaired, part))
                    changed = true;

                kept.Add(repaired);
            }

            if (kept.Count == 0)
                return null;

            return changed ? Geometry<T>.Create(kept, geometry.Type, geometry.Srid) : geometry;
        }

        private Geometry<T>? CheckPolygon(Geometry<T> polygon, int? partIndex)
        {
            var rings = polygon.Geometries;

            if (rings is null || rings.Count == 0)
            {
                Add(GeometryIssueKind.TooFewPoints, partIndex, null);
                return null;
            }

            var kept = new List<Geometry<T>>(rings.Count);

            var changed = false;

            for (int i = 0; i < rings.Count; i++)
            {
                var ring = rings[i];

                var points = CheckPath(ring?.Points, 3, isRing: true, partIndex, i, out var hasInvalidCoordinates);

                if (points is null)
                {
                    // without its outer ring the polygon (and its holes) is gone
                    if (i == 0)
                        return null;

                    changed = true;
                    continue;
                }

                if (!hasInvalidCoordinates)
                {
                    var shouldBeClockwise = i != 0;

                    if (SpatialUtility.IsClockwise(points) != shouldBeClockwise)
                    {
                        Add(GeometryIssueKind.WrongRingOrientation, partIndex, i);

                        points = Enumerable.Reverse(points).ToList();
                    }
                }

                if (ReferenceEquals(points, ring!.Points))
                {
                    kept.Add(ring);
                }
                else
                {
                    kept.Add(Geometry<T>.CreatePolygonRing(points, ring.Srid));
                    changed = true;
                }
            }

            return changed ? Geometry<T>.Create(kept, GeometryType.Polygon, polygon.Srid) : polygon;
        }

        /// <summary>
        /// Checks a point list; returns the same list when nothing changes, a cleaned copy, or null when too few
        /// points are left. With invalid coordinates the list is returned unchanged and not checked further.
        /// </summary>
        private List<T>? CheckPath(List<T>? points, int minCount, bool isRing, int? partIndex, int? ringIndex, out bool hasInvalidCoordinates)
        {
            hasInvalidCoordinates = false;

            if (points is null || points.Count == 0)
            {
                Add(GeometryIssueKind.TooFewPoints, partIndex, ringIndex);
                return null;
            }

            if (points.Any(p => p is null || !IsFinite(p.X) || !IsFinite(p.Y)))
            {
                Add(GeometryIssueKind.InvalidCoordinate, partIndex, ringIndex);
                hasInvalidCoordinates = true;
                return points;
            }

            if (_isGeodetic && points.Any(p => Math.Abs(p.X) > 180 || Math.Abs(p.Y) > 90))
                Add(GeometryIssueKind.CoordinateOutOfRange, partIndex, ringIndex);

            var result = points;

            if (isRing && result.Count > 1)
            {
                var end = result.Count;

                while (end > 1 && HaveTheSameXY(result[0], result[end - 1]))
                    end--;

                if (end < result.Count)
                {
                    Add(GeometryIssueKind.ClosedRing, partIndex, ringIndex);
                    result = result.Take(end).ToList();
                }
            }

            List<T>? deduplicated = null;

            for (int i = 1; i < result.Count; i++)
            {
                if (HaveTheSameXY(result[i], result[i - 1]))
                    deduplicated ??= result.Take(i).ToList();
                else
                    deduplicated?.Add(result[i]);
            }

            if (deduplicated is not null)
            {
                Add(GeometryIssueKind.RepeatedPoint, partIndex, ringIndex);
                result = deduplicated;
            }

            if (result.Count < minCount)
            {
                Add(GeometryIssueKind.TooFewPoints, partIndex, ringIndex);
                return null;
            }

            if (isRing && SpatialUtility.GetSignedEuclideanArea(result) == 0)
            {
                Add(GeometryIssueKind.ZeroAreaRing, partIndex, ringIndex);
                return null;
            }

            return result;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        // exact X/Y, whatever the point types (PointZ.HaveTheSameXY only matches another PointZ)
        private static bool HaveTheSameXY(T first, T second) => first.X == second.X && first.Y == second.Y;
    }
}
