using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Abstractions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Ogc.GML.v212;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Sta.Ogc.GML;

public static class Gml2Reader
{
    private const string GmlNamespace = "http://www.opengis.net/gml";

    public static IGeometry Parse(string gmlString, int srid = 0)
    {
        if (string.IsNullOrWhiteSpace(gmlString))
            return Geometry<Point>.Empty;

        try
        {
            var doc = XDocument.Parse(gmlString);
            var root = doc.Root;

            if (root == null)
                return Geometry<Point>.Empty;

            // Extract SRID from srsName attribute if present
            var srsName = root.Attribute("srsName")?.Value;
            if (!string.IsNullOrEmpty(srsName) && srid == 0)
            {
                srid = ExtractSridFromSrsName(srsName);
            }

            // Use polymorphic deserialization - AbstractGeometryType has XmlIncludeAttribute for all derived types
            // Use StringReader to read from the XML string directly for proper polymorphic deserialization
            var serializer = new XmlSerializer(typeof(AbstractGeometryType), GmlNamespace);
            using var reader = new StringReader(gmlString);
            using var xmlReader = XmlReader.Create(reader);
            var geometry = serializer.Deserialize(xmlReader) as AbstractGeometryType;

            if (geometry == null)
                return Geometry<Point>.Empty;

            return ConvertFromGml2(geometry, srid);
        }
        catch (Exception ex)
        {
            throw new FormatException($"Failed to parse GML 2 string: {ex.Message}", ex);
        }
    }

    private static int ExtractSridFromSrsName(string srsName)
    {
        // Try to extract SRID from formats like:
        // http://www.opengis.net/gml/srs/epsg.xml#4326
        // EPSG:4326
        // urn:ogc:def:crs:EPSG::4326

        if (string.IsNullOrEmpty(srsName))
            return 0;

        var parts = srsName.Split('#');
        if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out var srid))
            return srid;

        parts = srsName.Split(':');
        if (parts.Length > 0 && int.TryParse(parts[parts.Length - 1], out srid))
            return srid;

        return 0;
    }

    private static IGeometry ConvertFromGml2(AbstractGeometryType gmlGeometry, int srid)
    {
        return gmlGeometry switch
        {
            PointType point => ConvertPoint(point, srid),
            LineStringType lineString => ConvertLineString(lineString, srid),
            LinearRingType linearRing => ConvertLinearRing(linearRing, srid),
            PolygonType polygon => ConvertPolygon(polygon, srid),
            MultiPointType multiPoint => ConvertMultiPoint(multiPoint, srid),
            MultiLineStringType multiLineString => ConvertMultiLineString(multiLineString, srid),
            MultiPolygonType multiPolygon => ConvertMultiPolygon(multiPolygon, srid),
            GeometryCollectionType geometryCollection => ConvertGeometryCollection(geometryCollection, srid),
            _ => throw new NotImplementedException($"GML 2 geometry type {gmlGeometry.GetType().Name} is not supported")
        };
    }

    private static IGeometry ConvertPoint(PointType point, int srid)
    {
        var coords = ExtractCoordinates(point.Item);
        if (coords.Count == 0)
            return Geometry<Point>.Empty;

        var p = coords[0];
        bool hasZ = HasZValues(coords);

        if (hasZ && p.Z.HasValue)
        {
            return Geometry<PointZ>.Create(new List<PointZ> { new PointZ { X = p.X, Y = p.Y, Z = p.Z.Value } }, GeometryType.Point, srid);
        }
        else
        {
            return Geometry<Point>.Create(new List<Point> { new Point(p.X, p.Y) }, GeometryType.Point, srid);
        }
    }

    private static IGeometry ConvertLineString(LineStringType lineString, int srid)
    {
        var coords = ExtractCoordinates(lineString.Items);
        if (coords.Count < 2)
            return Geometry<Point>.Empty;

        bool hasZ = HasZValues(coords);

        if (hasZ && coords.Any(c => c.Z.HasValue))
        {
            var points = coords.Select(c => new PointZ { X = c.X, Y = c.Y, Z = c.Z ?? 0 }).ToList();
            return Geometry<PointZ>.Create(points, GeometryType.LineString, srid);
        }
        else
        {
            var points = coords.Select(c => new Point(c.X, c.Y)).ToList();
            return Geometry<Point>.Create(points, GeometryType.LineString, srid);
        }
    }

    private static IGeometry ConvertLinearRing(LinearRingType linearRing, int srid)
    {
        var coords = ExtractCoordinates(linearRing.Items);
        if (coords.Count < 3)
            return Geometry<Point>.Empty;

        bool hasZ = HasZValues(coords);

        if (hasZ && coords.Any(c => c.Z.HasValue))
        {
            var points = coords.Select(c => new PointZ { X = c.X, Y = c.Y, Z = c.Z ?? 0 }).ToList();
            // Ensure ring is closed (first point equals last point)
            if (points.Count > 0)
            {
                var firstPoint = points[0];
                var lastPoint = points[points.Count - 1];
                if (firstPoint.X != lastPoint.X || firstPoint.Y != lastPoint.Y || firstPoint.Z != lastPoint.Z)
                {
                    points.Add(new PointZ { X = firstPoint.X, Y = firstPoint.Y, Z = firstPoint.Z });
                }
            }
            return Geometry<PointZ>.Create(points, GeometryType.LineString, srid);
        }
        else
        {
            var points = coords.Select(c => new Point(c.X, c.Y)).ToList();
            // Ensure ring is closed (first point equals last point)
            if (points.Count > 0 && !points[0].AreExactlyTheSame(points[points.Count - 1]))
            {
                points.Add(new Point(points[0].X, points[0].Y));
            }
            return Geometry<Point>.Create(points, GeometryType.LineString, srid);
        }
    }

    private static IGeometry ConvertPolygon(PolygonType polygon, int srid)
    {
        var rings = new List<IGeometry>();
        bool hasZ = false;

        // Outer boundary
        if (polygon.outerBoundaryIs?._Geometry is LinearRingType outerRing)
        {
            var outerCoords = ExtractCoordinates(outerRing.Items);
            if (outerCoords.Count >= 3)
            {
                hasZ = HasZValues(outerCoords);
                if (hasZ && outerCoords.Any(c => c.Z.HasValue))
                {
                    var outerPoints = outerCoords.Select(c => new PointZ { X = c.X, Y = c.Y, Z = c.Z ?? 0 }).ToList();
                    // Ensure ring is closed
                    if (outerPoints.Count > 0)
                    {
                        var firstPoint = outerPoints[0];
                        var lastPoint = outerPoints[outerPoints.Count - 1];
                        if (firstPoint.X != lastPoint.X || firstPoint.Y != lastPoint.Y || firstPoint.Z != lastPoint.Z)
                        {
                            outerPoints.Add(new PointZ { X = firstPoint.X, Y = firstPoint.Y, Z = firstPoint.Z });
                        }
                    }
                    rings.Add(Geometry<PointZ>.Create(outerPoints, GeometryType.LineString, srid));
                }
                else
                {
                    var outerPoints = outerCoords.Select(c => new Point(c.X, c.Y)).ToList();
                    // Ensure ring is closed
                    if (outerPoints.Count > 0 && !outerPoints[0].AreExactlyTheSame(outerPoints[outerPoints.Count - 1]))
                    {
                        outerPoints.Add(new Point(outerPoints[0].X, outerPoints[0].Y));
                    }
                    rings.Add(Geometry<Point>.Create(outerPoints, GeometryType.LineString, srid));
                }
            }
        }

        // Inner boundaries (holes)
        if (polygon.innerBoundaryIs != null)
        {
            foreach (var innerBoundary in polygon.innerBoundaryIs)
            {
                if (innerBoundary._Geometry is LinearRingType innerRing)
                {
                    var innerCoords = ExtractCoordinates(innerRing.Items);
                    if (innerCoords.Count >= 3)
                    {
                        // Use same dimension as outer ring
                        if (hasZ && innerCoords.Any(c => c.Z.HasValue))
                        {
                            var innerPoints = innerCoords.Select(c => new PointZ { X = c.X, Y = c.Y, Z = c.Z ?? 0 }).ToList();
                            // Ensure ring is closed
                            if (innerPoints.Count > 0)
                            {
                                var firstPoint = innerPoints[0];
                                var lastPoint = innerPoints[innerPoints.Count - 1];
                                if (firstPoint.X != lastPoint.X || firstPoint.Y != lastPoint.Y || firstPoint.Z != lastPoint.Z)
                                {
                                    innerPoints.Add(new PointZ { X = firstPoint.X, Y = firstPoint.Y, Z = firstPoint.Z });
                                }
                            }
                            rings.Add(Geometry<PointZ>.Create(innerPoints, GeometryType.LineString, srid));
                        }
                        else
                        {
                            var innerPoints = innerCoords.Select(c => new Point(c.X, c.Y)).ToList();
                            // Ensure ring is closed
                            if (innerPoints.Count > 0 && !innerPoints[0].AreExactlyTheSame(innerPoints[innerPoints.Count - 1]))
                            {
                                innerPoints.Add(new Point(innerPoints[0].X, innerPoints[0].Y));
                            }
                            rings.Add(Geometry<Point>.Create(innerPoints, GeometryType.LineString, srid));
                        }
                    }
                }
            }
        }

        if (rings.Count == 0)
            return Geometry<Point>.Empty;

        // Convert rings to same type for CreatePolygonOrMultiPolygon
        if (hasZ && rings[0] is Geometry<PointZ>)
        {
            var pointZRings = rings.Cast<Geometry<PointZ>>().ToList();
            return Geometry<PointZ>.CreatePolygonOrMultiPolygon(pointZRings, srid);
        }
        else
        {
            var pointRings = rings.Cast<Geometry<Point>>().ToList();
            return Geometry<Point>.CreatePolygonOrMultiPolygon(pointRings, srid);
        }
    }

    private static IGeometry ConvertMultiPoint(MultiPointType multiPoint, int srid)
    {
        if (multiPoint.geometryMember == null || multiPoint.geometryMember.Count == 0)
            return Geometry<Point>.Empty;

        var allCoords = new List<(double X, double Y, double? Z)>();
        foreach (var member in multiPoint.geometryMember)
        {
            if (member._Geometry is PointType point)
            {
                var coords = ExtractCoordinates(point.Item);
                if (coords.Count > 0)
                {
                    allCoords.Add(coords[0]);
                }
            }
        }

        if (allCoords.Count == 0)
            return Geometry<Point>.Empty;

        bool hasZ = HasZValues(allCoords);

        if (hasZ && allCoords.Any(c => c.Z.HasValue))
        {
            var points = allCoords.Select(c => new PointZ { X = c.X, Y = c.Y, Z = c.Z ?? 0 }).ToList();
            return Geometry<PointZ>.Create(points, GeometryType.MultiPoint, srid);
        }
        else
        {
            var points = allCoords.Select(c => new Point(c.X, c.Y)).ToList();
            return Geometry<Point>.Create(points, GeometryType.MultiPoint, srid);
        }
    }

    private static IGeometry ConvertMultiLineString(MultiLineStringType multiLineString, int srid)
    {
        if (multiLineString.geometryMember == null || multiLineString.geometryMember.Count == 0)
            return Geometry<Point>.Empty;

        var allCoords = new List<(double X, double Y, double? Z)>();
        foreach (var member in multiLineString.geometryMember)
        {
            if (member._Geometry is LineStringType lineString)
            {
                var coords = ExtractCoordinates(lineString.Items);
                allCoords.AddRange(coords);
            }
        }

        if (allCoords.Count < 2)
            return Geometry<Point>.Empty;

        bool hasZ = HasZValues(allCoords);

        if (hasZ && allCoords.Any(c => c.Z.HasValue))
        {
            var lineStrings = new List<Geometry<PointZ>>();
            foreach (var member in multiLineString.geometryMember)
            {
                if (member._Geometry is LineStringType lineString)
                {
                    var coords = ExtractCoordinates(lineString.Items);
                    if (coords.Count >= 2)
                    {
                        var points = coords.Select(c => new PointZ { X = c.X, Y = c.Y, Z = c.Z ?? 0 }).ToList();
                        lineStrings.Add(Geometry<PointZ>.Create(points, GeometryType.LineString, srid));
                    }
                }
            }
            if (lineStrings.Count == 0)
                return Geometry<PointZ>.Empty;
            return Geometry<PointZ>.Create(lineStrings, GeometryType.MultiLineString, srid);
        }
        else
        {
            var lineStrings = new List<Geometry<Point>>();
            foreach (var member in multiLineString.geometryMember)
            {
                if (member._Geometry is LineStringType lineString)
                {
                    var coords = ExtractCoordinates(lineString.Items);
                    if (coords.Count >= 2)
                    {
                        var points = coords.Select(c => new Point(c.X, c.Y)).ToList();
                        lineStrings.Add(Geometry<Point>.Create(points, GeometryType.LineString, srid));
                    }
                }
            }
            if (lineStrings.Count == 0)
                return Geometry<Point>.Empty;
            return Geometry<Point>.Create(lineStrings, GeometryType.MultiLineString, srid);
        }
    }

    private static IGeometry ConvertMultiPolygon(MultiPolygonType multiPolygon, int srid)
    {
        if (multiPolygon.geometryMember == null || multiPolygon.geometryMember.Count == 0)
            return Geometry<Point>.Empty;

        var polygons = new List<IGeometry>();
        bool hasZ = false;
        foreach (var member in multiPolygon.geometryMember)
        {
            if (member._Geometry is PolygonType polygon)
            {
                var converted = ConvertPolygon(polygon, srid);
                if (converted != null && !converted.IsEmpty())
                {
                    if (converted is Geometry<PointZ>)
                        hasZ = true;
                    polygons.Add(converted);
                }
            }
        }

        if (polygons.Count == 0)
            return Geometry<Point>.Empty;

        // Convert to same type
        if (hasZ && polygons.All(p => p is Geometry<PointZ>))
        {
            var pointZPolygons = polygons.Cast<Geometry<PointZ>>().ToList();
            return Geometry<PointZ>.Create(pointZPolygons, GeometryType.MultiPolygon, srid);
        }
        else
        {
            var pointPolygons = polygons.Cast<Geometry<Point>>().ToList();
            return Geometry<Point>.Create(pointPolygons, GeometryType.MultiPolygon, srid);
        }
    }

    private static IGeometry ConvertGeometryCollection(GeometryCollectionType geometryCollection, int srid)
    {
        if (geometryCollection.geometryMember == null || geometryCollection.geometryMember.Count == 0)
            return Geometry<Point>.Empty;

        var geometries = new List<IGeometry>();
        foreach (var member in geometryCollection.geometryMember)
        {
            var converted = ConvertFromGml2(member._Geometry, srid);
            if (converted != null && !converted.IsEmpty())
            {
                geometries.Add(converted);
            }
        }

        if (geometries.Count == 0)
            return Geometry<Point>.Empty;

        // GeometryCollection can contain mixed types, so we keep them as IGeometry
        // But we need to create a Geometry<Point> or Geometry<PointZ> wrapper
        // For now, convert all to Point if any is Point, otherwise use PointZ
        bool hasZ = geometries.Any(g => g is Geometry<PointZ>);

        if (hasZ)
        {
            // Convert all to PointZ for consistency
            var pointZGeometries = geometries.Select(g =>
            {
                if (g is Geometry<PointZ> pz) return pz;
                if (g is Geometry<Point> p)
                {
                    // Convert Point to PointZ with Z=0
                    return ConvertToPointZ(p);
                }
                return null;
            }).Where(g => g != null).Cast<Geometry<PointZ>>().ToList();

            if (pointZGeometries.Count == 0)
                return Geometry<Point>.Empty;
            return Geometry<PointZ>.Create(pointZGeometries, GeometryType.GeometryCollection, srid);
        }
        else
        {
            var pointGeometries = geometries.Cast<Geometry<Point>>().ToList();
            return Geometry<Point>.Create(pointGeometries, GeometryType.GeometryCollection, srid);
        }
    }

    /// <summary>
    /// Converts a Geometry<Point> to Geometry<PointZ> with Z=0
    /// </summary>
    private static Geometry<PointZ> ConvertToPointZ(Geometry<Point> geometry)
    {
        if (geometry.IsNullOrEmpty())
            return Geometry<PointZ>.Empty;

        if (geometry.Points != null)
        {
            var pointZList = geometry.Points.Select(p => new PointZ { X = p.X, Y = p.Y, Z = 0 }).ToList();
            return Geometry<PointZ>.Create(pointZList, geometry.Type, geometry.Srid);
        }
        else if (geometry.Geometries != null)
        {
            var convertedGeometries = geometry.Geometries.Select(ConvertToPointZ).ToList();
            return Geometry<PointZ>.Create(convertedGeometries, geometry.Type, geometry.Srid);
        }

        return Geometry<PointZ>.Empty;
    }

    private static List<(double X, double Y, double? Z)> ExtractCoordinates(object item)
    {
        var result = new List<(double X, double Y, double? Z)>();

        if (item == null)
            return result;

        switch (item)
        {
            case CoordType coord:
                // Check if Z value is present (not default/zero, but we'll check if it's explicitly set)
                // In GML 2, Z is optional, so we check if it's non-zero or if we should check srsDimension
                double? z = null;
                if (coord.Z != 0) // Simple check - in practice, we might need to check srsDimension
                {
                    z = (double)coord.Z;
                }
                result.Add(((double)coord.X, (double)coord.Y, z));
                break;

            case CoordinatesType coordinates:
                result.AddRange(ParseCoordinatesString(coordinates.Value, coordinates.cs ?? ",", coordinates.ts ?? " "));
                break;

            case List<object> items:
                foreach (var subItem in items)
                {
                    result.AddRange(ExtractCoordinates(subItem));
                }
                break;
        }

        return result;
    }

    private static List<(double X, double Y, double? Z)> ParseCoordinatesString(string value, string coordinateSeparator, string tupleSeparator)
    {
        var result = new List<(double X, double Y, double? Z)>();

        if (string.IsNullOrWhiteSpace(value))
            return result;

        var tuples = value.Split(new[] { tupleSeparator }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var tuple in tuples)
        {
            var coords = tuple.Split(new[] { coordinateSeparator }, StringSplitOptions.RemoveEmptyEntries);
            if (coords.Length >= 2 &&
                double.TryParse(coords[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                double.TryParse(coords[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            {
                double? z = null;
                // Check if third coordinate (Z) is present
                if (coords.Length >= 3 &&
                    double.TryParse(coords[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var zValue))
                {
                    z = zValue;
                }
                result.Add((x, y, z));
            }
        }

        return result;
    }

    /// <summary>
    /// Determines if coordinates contain Z values
    /// </summary>
    private static bool HasZValues(List<(double X, double Y, double? Z)> coords)
    {
        return coords.Any(c => c.Z.HasValue);
    }
}

