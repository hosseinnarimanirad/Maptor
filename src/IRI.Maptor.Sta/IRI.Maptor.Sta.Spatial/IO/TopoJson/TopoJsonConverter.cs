using System.Diagnostics;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.TopoJson;

/// <summary>
/// Converts between TopoJSON and Geometry objects
/// </summary>
public static class TopoJsonConverter
{

    #region Geometry<Point> to TopoJson

    /// <summary>
    /// Convert a single Geometry to TopoJSON topology
    /// </summary>
    public static TopoJsonTopology FromGeometry(Geometry<Point> geometry, string objectName = "geometry", bool quantize = true, int quantizationFactor = 10000)
    {
        var geometries = new Dictionary<string, Geometry<Point>> { { objectName, geometry } };
        return FromGeometries(geometries, quantize, quantizationFactor);
    }

    /// <summary>
    /// Convert multiple geometries to TopoJSON with shared topology
    /// </summary>
    public static TopoJsonTopology FromGeometries(Dictionary<string, Geometry<Point>> geometries, bool quantize = true, int quantizationFactor = 10000)
    {
        var topology = new TopoJsonTopology();
        var arcBuilder = new ArcBuilder();

        // Calculate bounding box
        var allPoints = geometries.Values
            .Where(g => g != null && !g.IsNullOrEmpty())
            .SelectMany(g => g.GetAllPoints())
            .ToList();

        if (allPoints.Count > 0)
        {
            var minX = allPoints.Min(p => p.X);
            var minY = allPoints.Min(p => p.Y);
            var maxX = allPoints.Max(p => p.X);
            var maxY = allPoints.Max(p => p.Y);

            topology.BBox = new[] { minX, minY, maxX, maxY };

            // Setup quantization if requested
            if (quantize && (maxX - minX) > 0 && (maxY - minY) > 0)
            {
                topology.Transform = new TopoJsonTransform
                {
                    Scale = new[]
                    {
                        (maxX - minX) / quantizationFactor,
                        (maxY - minY) / quantizationFactor
                    },
                    Translate = new[] { minX, minY }
                };
            }
        }

        // Convert each geometry
        foreach (var kvp in geometries)
        {
            if (kvp.Value == null || kvp.Value.IsNullOrEmpty())
                continue;

            topology.Objects[kvp.Key] = ConvertGeometry(kvp.Value, arcBuilder, topology.Transform);
        }

        // Build arcs
        topology.Arcs = arcBuilder.BuildArcs();

        return topology;
    }


    /// <summary>
    /// Converts a list of Feature&lt;Point&gt; objects to a TopoJSON topology,
    /// preserving each feature's ID and properties.
    /// </summary>
    /// <param name="features">List of features to convert.</param>
    /// <param name="quantize">Whether to apply quantization.</param>
    /// <param name="quantizationFactor">Quantization factor (e.g., 10000).</param>
    /// <returns>TopoJSON topology containing all features as separate objects.</returns>
    public static TopoJsonTopology FromFeatures(
        IReadOnlyList<Feature<Point>> features,
        bool quantize = true,
        int quantizationFactor = 10000)
    {
        if (features == null || features.Count == 0)
            throw new ArgumentException("The features list is null or empty.", nameof(features));

        var topology = new TopoJsonTopology();
        var arcBuilder = new ArcBuilder();

        // Collect all points from all geometries to compute bounding box and transform
        var allPoints = new List<Point>();
        foreach (var feature in features)
        {
            var geom = feature.TheGeometry;
            if (geom != null && !geom.IsNullOrEmpty())
                allPoints.AddRange(geom.GetAllPoints());
        }

        if (allPoints.Count > 0)
        {
            var minX = allPoints.Min(p => p.X);
            var minY = allPoints.Min(p => p.Y);
            var maxX = allPoints.Max(p => p.X);
            var maxY = allPoints.Max(p => p.Y);
            topology.BBox = new[] { minX, minY, maxX, maxY };

            if (quantize && (maxX - minX) > 0 && (maxY - minY) > 0)
            {
                topology.Transform = new TopoJsonTransform
                {
                    Scale = new[]
                    {
                    (maxX - minX) / quantizationFactor,
                    (maxY - minY) / quantizationFactor
                },
                    Translate = new[] { minX, minY }
                };
            }
        }

        // Convert each feature to a TopoJSON geometry object
        for (int i = 0; i < features.Count; i++)
        {
            var feature = features[i];
            var geometry = feature.TheGeometry;
            if (geometry == null || geometry.IsNullOrEmpty())
                continue;

            // Convert the geometry (reuse existing conversion logic)
            var topoGeometry = ConvertGeometry(geometry, arcBuilder, topology.Transform);
            if (topoGeometry == null)
                continue;

            // Attach feature properties and ID
            if (feature.Attributes != null && feature.Attributes.Count > 0)
                topoGeometry.Properties = new Dictionary<string, object>(feature.Attributes);

            if (feature.Id != 0)
                topoGeometry.Id = feature.Id.ToString();

            // Use feature ID if available, otherwise generate a name
            string objectName = feature.Id != 0 ? feature.Id.ToString() : $"feature_{i}";
            // Ensure uniqueness: if duplicate ID appears, append index

            if (topology.Objects.ContainsKey(objectName))
                objectName = $"{objectName}_{i}";
            topology.Objects[objectName] = topoGeometry;
        }

        // Build arcs from the collected arcs
        topology.Arcs = arcBuilder.BuildArcs();

        return topology;
    }

    private static TopoJsonGeometry ConvertGeometry(Geometry<Point> geometry, ArcBuilder arcBuilder, TopoJsonTransform? transform)
    {
        // Skip GeometryCollection when converting from Geometry to TopoJSON
        if (geometry.Type == GeometryType.GeometryCollection)
        {
            Debug.WriteLine($"[TopoJSON] Skipping GeometryCollection with {geometry.Geometries?.Count ?? 0} sub-geometries during TopoJSON conversion");
            throw new NotSupportedException("GeometryCollection is not supported for TopoJSON conversion. Please convert individual geometries separately.");
        }

        return geometry.Type switch
        {
            GeometryType.Point => ConvertPoint(geometry, transform),
            GeometryType.MultiPoint => ConvertMultiPoint(geometry, transform),
            GeometryType.LineString => ConvertLineString(geometry, arcBuilder, transform),
            GeometryType.MultiLineString => ConvertMultiLineString(geometry, arcBuilder, transform),
            GeometryType.Polygon => ConvertPolygon(geometry, arcBuilder, transform),
            GeometryType.MultiPolygon => ConvertMultiPolygon(geometry, arcBuilder, transform),
            _ => throw new NotSupportedException($"Geometry type {geometry.Type} not supported in TopoJSON conversion")
        };
    }

    private static TopoJsonPoint ConvertPoint(Geometry<Point> geometry, TopoJsonTransform? transform)
    {
        var point = geometry.GetAllPoints().First();
        var coords = transform != null
            ? transform.Inverse(point.X, point.Y).Select(i => (double)i).ToArray()
            : new[] { point.X, point.Y };

        return new TopoJsonPoint { Coordinates = coords };
    }

    private static TopoJsonMultiPoint ConvertMultiPoint(Geometry<Point> geometry, TopoJsonTransform? transform)
    {
        var points = geometry.GetAllPoints();
        var coordinates = new List<double[]>();

        foreach (var point in points)
        {
            var coords = transform != null
                ? transform.Inverse(point.X, point.Y).Select(i => (double)i).ToArray()
                : new[] { point.X, point.Y };
            coordinates.Add(coords);
        }

        return new TopoJsonMultiPoint { Coordinates = coordinates };
    }

    private static TopoJsonLineString ConvertLineString(Geometry<Point> geometry, ArcBuilder arcBuilder, TopoJsonTransform? transform)
    {
        var points = geometry.GetAllPoints();
        var arcIndex = arcBuilder.AddArc(points, transform);

        return new TopoJsonLineString { Arcs = new List<int> { arcIndex } };
    }

    private static TopoJsonMultiLineString ConvertMultiLineString(Geometry<Point> geometry, ArcBuilder arcBuilder, TopoJsonTransform? transform)
    {
        var arcs = new List<List<int>>();

        foreach (var lineString in geometry.Geometries)
        {
            var points = lineString.GetAllPoints();
            var arcIndex = arcBuilder.AddArc(points, transform);
            arcs.Add(new List<int> { arcIndex });
        }

        return new TopoJsonMultiLineString { Arcs = arcs };
    }

    private static TopoJsonPolygon ConvertPolygon(Geometry<Point> geometry, ArcBuilder arcBuilder, TopoJsonTransform? transform)
    {
        var arcs = new List<List<int>>();

        foreach (var ring in geometry.Geometries)
        {
            var points = ring.GetAllPoints();
            // Close the ring if not already closed
            if (!points.First().Equals(points.Last()))
            {
                points = new List<Point>(points) { points.First() };
            }
            var arcIndex = arcBuilder.AddArc(points, transform);
            arcs.Add(new List<int> { arcIndex });
        }

        return new TopoJsonPolygon { Arcs = arcs };
    }

    private static TopoJsonMultiPolygon ConvertMultiPolygon(Geometry<Point> geometry, ArcBuilder arcBuilder, TopoJsonTransform? transform)
    {
        var arcs = new List<List<List<int>>>();

        foreach (var polygon in geometry.Geometries)
        {
            var polygonArcs = new List<List<int>>();

            foreach (var ring in polygon.Geometries)
            {
                var points = ring.GetAllPoints();
                // Close the ring if not already closed
                if (!points.First().Equals(points.Last()))
                {
                    points = new List<Point>(points) { points.First() };
                }
                var arcIndex = arcBuilder.AddArc(points, transform);
                polygonArcs.Add(new List<int> { arcIndex });
            }

            arcs.Add(polygonArcs);
        }

        return new TopoJsonMultiPolygon { Arcs = arcs };
    }

    /// <summary>
    /// Helper class for building arcs from geometries
    /// </summary>
    private class ArcBuilder
    {
        private readonly List<List<int[]>> _arcs = new();
        private readonly Dictionary<string, int> _arcMap = new();

        public int AddArc(List<Point> points, TopoJsonTransform? transform)
        {
            // Create arc key for deduplication
            var key = CreateArcKey(points);

            // Check if this arc already exists
            if (_arcMap.TryGetValue(key, out var existingIndex))
            {
                return existingIndex;
            }

            // Convert points to quantized delta-encoded coordinates
            var arc = new List<int[]>();
            var prevX = 0;
            var prevY = 0;

            foreach (var point in points)
            {
                int x, y;

                if (transform != null)
                {
                    var quantized = transform.Inverse(point.X, point.Y);
                    x = quantized[0];
                    y = quantized[1];
                }
                else
                {
                    x = (int)Math.Round(point.X);
                    y = (int)Math.Round(point.Y);
                }

                // Delta encoding
                arc.Add(new[] { x - prevX, y - prevY });
                prevX = x;
                prevY = y;
            }

            var index = _arcs.Count;
            _arcs.Add(arc);
            _arcMap[key] = index;

            return index;
        }

        public List<List<int[]>> BuildArcs()
        {
            return _arcs;
        }

        private static string CreateArcKey(List<Point> points)
        {
            // Simple key based on start and end points
            // In a production system, you might want a more sophisticated key
            if (points.Count == 0)
                return string.Empty;

            var first = points.First();
            var last = points.Last();
            return $"{first.X:F6},{first.Y:F6}|{last.X:F6},{last.Y:F6}|{points.Count}";
        }
    }

    #endregion


    #region TopoJson to Geometry<Point>

    /// <summary>
    /// Convert TopoJSON topology to geometries
    /// </summary>
    public static Dictionary<string, Geometry<Point>> ToGeometry(TopoJsonTopology topology, int srid = 4326)
    {
        var result = new Dictionary<string, Geometry<Point>>();

        if (topology?.Objects == null)
            return result;

        foreach (var kvp in topology.Objects)
        {
            var geometry = ConvertToGeometry(kvp.Value, topology.Arcs, topology.Transform, srid);

            if (geometry != null && !geometry.IsNullOrEmpty())
            {
                result[kvp.Key] = geometry;
            }
        }

        return result;
    }


    private static Geometry<Point>? ConvertToGeometry(TopoJsonGeometry topoGeometry, List<List<int[]>> arcs, TopoJsonTransform? transform, int srid)
    {
        return topoGeometry switch
        {
            TopoJsonPoint point => ConvertToPoint(point, transform, srid),
            TopoJsonMultiPoint multiPoint => ConvertToMultiPoint(multiPoint, transform, srid),
            TopoJsonLineString lineString => ConvertToLineString(lineString, arcs, transform, srid),
            TopoJsonMultiLineString multiLineString => ConvertToMultiLineString(multiLineString, arcs, transform, srid),
            TopoJsonPolygon polygon => ConvertToPolygon(polygon, arcs, transform, srid),
            TopoJsonMultiPolygon multiPolygon => ConvertToMultiPolygon(multiPolygon, arcs, transform, srid),
            TopoJsonGeometryCollection geometryCollection => SkipGeometryCollection(geometryCollection),
            _ => null
        };
    }

    private static Geometry<Point>? SkipGeometryCollection(TopoJsonGeometryCollection geometryCollection)
    {
        var count = geometryCollection.Geometries?.Count ?? 0;
        var id = string.IsNullOrEmpty(geometryCollection.Id) ? "N/A" : geometryCollection.Id;
        Debug.WriteLine($"[TopoJSON] Skipping GeometryCollection with {count} nested geometries (ID: {id})");
        return null;
    }

    private static Geometry<Point> ConvertToPoint(TopoJsonPoint point, TopoJsonTransform? transform, int srid)
    {
        var coords = point.Coordinates;
        var x = coords[0];
        var y = coords[1];

        if (transform != null)
        {
            var transformed = transform.Apply(new[] { (int)x, (int)y });
            x = transformed[0];
            y = transformed[1];
        }

        return Geometry<Point>.Create(x, y, srid);
    }

    private static Geometry<Point> ConvertToMultiPoint(TopoJsonMultiPoint multiPoint, TopoJsonTransform? transform, int srid)
    {
        var points = new List<Geometry<Point>>();

        foreach (var coords in multiPoint.Coordinates)
        {
            var x = coords[0];
            var y = coords[1];

            if (transform != null)
            {
                var transformed = transform.Apply(new[] { (int)x, (int)y });
                x = transformed[0];
                y = transformed[1];
            }

            points.Add(Geometry<Point>.Create(x, y, srid));
        }

        return Geometry<Point>.Create(points, GeometryType.MultiPoint, srid);
    }

    private static Geometry<Point> ConvertToLineString(TopoJsonLineString lineString, List<List<int[]>> arcs, TopoJsonTransform? transform, int srid)
    {
        var points = ResolveArcs(lineString.Arcs, arcs, transform);
        return Geometry<Point>.Create(points, GeometryType.LineString, srid);
    }

    private static Geometry<Point> ConvertToMultiLineString(TopoJsonMultiLineString multiLineString, List<List<int[]>> arcs, TopoJsonTransform? transform, int srid)
    {
        var lineStrings = new List<Geometry<Point>>();

        foreach (var arcSequence in multiLineString.Arcs)
        {
            var points = ResolveArcs(arcSequence, arcs, transform);
            lineStrings.Add(Geometry<Point>.Create(points, GeometryType.LineString, srid));
        }

        return Geometry<Point>.Create(lineStrings, GeometryType.MultiLineString, srid);
    }

    private static Geometry<Point> ConvertToPolygon(TopoJsonPolygon polygon, List<List<int[]>> arcs, TopoJsonTransform? transform, int srid)
    {
        var rings = new List<Geometry<Point>>();

        foreach (var arcSequence in polygon.Arcs)
        {
            var points = ResolveArcs(arcSequence, arcs, transform);
            // Remove last point if it's the same as first (TopoJSON includes it, but Geometry doesn't need it for rings)
            if (points.Count > 1 && points.First().Equals(points.Last()))
            {
                points = points.Take(points.Count - 1).ToList();
            }
            rings.Add(Geometry<Point>.Create(points, GeometryType.LineString, srid));
        }

        return Geometry<Point>.Create(rings, GeometryType.Polygon, srid);
    }

    private static Geometry<Point> ConvertToMultiPolygon(TopoJsonMultiPolygon multiPolygon, List<List<int[]>> arcs, TopoJsonTransform? transform, int srid)
    {
        var polygons = new List<Geometry<Point>>();

        foreach (var polygonArcs in multiPolygon.Arcs)
        {
            var rings = new List<Geometry<Point>>();

            foreach (var arcSequence in polygonArcs)
            {
                var points = ResolveArcs(arcSequence, arcs, transform);
                // Remove last point if it's the same as first
                if (points.Count > 1 && points.First().Equals(points.Last()))
                {
                    points = points.Take(points.Count - 1).ToList();
                }
                rings.Add(Geometry<Point>.Create(points, GeometryType.LineString, srid));
            }

            polygons.Add(Geometry<Point>.Create(rings, GeometryType.Polygon, srid));
        }

        return Geometry<Point>.Create(polygons, GeometryType.MultiPolygon, srid);
    }

    /// <summary>
    /// Resolve arc indices to actual coordinates
    /// </summary>
    private static List<Point> ResolveArcs(List<int> arcIndices, List<List<int[]>> arcs, TopoJsonTransform? transform)
    {
        var points = new List<Point>();
        var currentX = 0.0;
        var currentY = 0.0;
        var isFirst = true;

        foreach (var arcIndex in arcIndices)
        {
            var isReversed = arcIndex < 0;
            var actualIndex = isReversed ? ~arcIndex : arcIndex;

            if (actualIndex >= arcs.Count)
                continue;

            var arc = arcs[actualIndex];
            var arcPoints = isReversed ? Enumerable.Reverse(arc).ToList() : arc;

            foreach (var point in arcPoints)
            {
                // Delta decoding
                currentX += point[0];
                currentY += point[1];

                // Apply transform if present
                var x = currentX;
                var y = currentY;

                if (transform != null)
                {
                    var transformed = transform.Apply(new[] { (int)currentX, (int)currentY });
                    x = transformed[0];
                    y = transformed[1];
                }

                // Skip first point of subsequent arcs (already added from previous arc)
                if (!isFirst || points.Count == 0)
                {
                    points.Add(new Point(x, y));
                }

                isFirst = false;
            }
        }

        return points;
    }

    #endregion

}

