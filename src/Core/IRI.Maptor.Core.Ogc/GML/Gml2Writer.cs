using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Ogc.GML.v212;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.Ogc.GML;

public static class Gml2Writer
{
    private const string GmlNamespace = "http://www.opengis.net/gml";

    public static string AsGml2(IGeometry geometry, bool includeSrid = false)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        var geometryType = geometry.GetType();
        
        // Verify it's a Geometry<T>
        if (!geometryType.IsGenericType || 
            geometryType.GetGenericTypeDefinition() != typeof(Geometry<>))
        {
            throw new ArgumentException(
                $"Geometry must be of type Geometry<T> where T : IPoint, new(). Actual type: {geometryType.FullName}", 
                nameof(geometry));
        }

        // Extract generic type parameter T
        var pointType = geometryType.GetGenericArguments()[0];
        
        // Verify T implements IPoint
        if (!typeof(IPoint).IsAssignableFrom(pointType))
        {
            throw new ArgumentException(
                $"Point type {pointType.FullName} must implement IPoint", 
                nameof(geometry));
        }

        // Get the internal generic method
        var method = typeof(Gml2Writer).GetMethod(
            nameof(AsGml2Internal), 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        if (method == null)
        {
            throw new InvalidOperationException($"Method {nameof(AsGml2Internal)} not found on {nameof(Gml2Writer)}");
        }

        // Make it generic with the point type
        var genericMethod = method.MakeGenericMethod(pointType);
        
        // Cast geometry to Geometry<T> and invoke
        // Since we've verified it's a Geometry<T>, we can safely cast
        var typedGeometry = (dynamic)geometry;
        var result = genericMethod.Invoke(null, new object[] { typedGeometry, includeSrid });
        
        return result as string ?? string.Empty;
    }

    private static string AsGml2Internal<T>(Geometry<T> geometry, bool includeSrid = false) where T : IPoint, new()
    {
        if (geometry.IsNullOrEmpty())
            return string.Empty;

        var gmlGeometry = ConvertToGml2(geometry);
        if (gmlGeometry == null)
            return string.Empty;

        var serializer = new XmlSerializer(gmlGeometry.GetType(), GmlNamespace);
        
        // Serialize to a StringWriter first, then parse into XElement
        // This avoids the ConformanceLevel.Fragment issue with XElement.CreateWriter()
        using var stringWriter = new StringWriter();
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("", GmlNamespace);
        
        serializer.Serialize(stringWriter, gmlGeometry, namespaces);
        var serializedXml = stringWriter.ToString();
        
        // Parse the serialized XML and use the root element
        var doc = XDocument.Parse(serializedXml);
        var root = doc.Root;
        
        if (root == null)
            return string.Empty;

        // Add srsName attribute if requested
        if (includeSrid && geometry.Srid > 0)
        {
            root.SetAttributeValue("srsName", $"http://www.opengis.net/gml/srs/epsg.xml#{geometry.Srid}");
        }

        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private static AbstractGeometryType ConvertToGml2<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        return geometry.Type switch
        {
            GeometryType.Point => ConvertPoint(geometry),
            GeometryType.LineString => ConvertLineString(geometry),
            GeometryType.Polygon => ConvertPolygon(geometry),
            GeometryType.MultiPoint => ConvertMultiPoint(geometry),
            GeometryType.MultiLineString => ConvertMultiLineString(geometry),
            GeometryType.MultiPolygon => ConvertMultiPolygon(geometry),
            GeometryType.GeometryCollection => ConvertGeometryCollection(geometry),
            _ => throw new NotImplementedException($"GML 2 conversion not supported for geometry type {geometry.Type}")
        };
    }

    private static PointType ConvertPoint<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.Points == null || geometry.Points.Count == 0)
            return null;

        var point = geometry.Points[0];
        var coord = new CoordType
        {
            X = (decimal)point.X,
            Y = (decimal)point.Y
        };

        // Add Z value if available (GML 2 supports Z in CoordType)
        if (point is IHasZ hasZ)
        {
            coord.Z = (decimal)hasZ.Z;
        }

        return new PointType { Item = coord };
    }

    private static LineStringType ConvertLineString<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.Points == null || geometry.Points.Count < 2)
            return null;

        var coordinates = new CoordinatesType
        {
            Value = FormatCoordinates(geometry.Points),
            cs = ",",
            ts = " ",
            @decimal = "."
        };

        return new LineStringType { Items = new List<object> { coordinates } };
    }

    private static PolygonType ConvertPolygon<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return null;

        var polygon = new PolygonType();

        // Outer boundary (first ring)
        if (geometry.Geometries.Count > 0)
        {
            var outerRing = geometry.Geometries[0];
            var ringPoints = EnsureRingClosed(outerRing.Points);
            
            var linearRing = new LinearRingType
            {
                Items = new List<object>
                {
                    new CoordinatesType
                    {
                        Value = FormatCoordinates(ringPoints),
                        cs = ",",
                        ts = " ",
                        @decimal = "."
                    }
                }
            };

            polygon.outerBoundaryIs = new LinearRingMemberType { _Geometry = linearRing };
        }

        // Inner boundaries (holes)
        if (geometry.Geometries.Count > 1)
        {
            polygon.innerBoundaryIs = new List<LinearRingMemberType>();
            for (int i = 1; i < geometry.Geometries.Count; i++)
            {
                var innerRing = geometry.Geometries[i];
                var ringPoints = EnsureRingClosed(innerRing.Points);
                
                var linearRing = new LinearRingType
                {
                    Items = new List<object>
                    {
                        new CoordinatesType
                        {
                            Value = FormatCoordinates(ringPoints),
                            cs = ",",
                            ts = " ",
                            @decimal = "."
                        }
                    }
                };

                polygon.innerBoundaryIs.Add(new LinearRingMemberType { _Geometry = linearRing });
            }
        }

        return polygon;
    }

    /// <summary>
    /// Ensures a ring is closed by adding the first point as the last point if needed
    /// </summary>
    private static List<T> EnsureRingClosed<T>(List<T> points) where T : IPoint, new()
    {
        if (points == null || points.Count == 0)
            return points;

        // Check if ring is already closed
        var firstPoint = points[0];
        var lastPoint = points[points.Count - 1];
        
        bool isClosed = firstPoint.X == lastPoint.X && firstPoint.Y == lastPoint.Y;
        
        // For Z coordinates, also check Z if available
        if (isClosed && firstPoint is IHasZ firstHasZ && lastPoint is IHasZ lastHasZ)
        {
            isClosed = Math.Abs(firstHasZ.Z - lastHasZ.Z) < 1e-10; // Use epsilon for floating point comparison
        }

        if (!isClosed)
        {
            // Create a new point with the same coordinates as the first point
            var closingPoint = CreatePointCopy<T>(firstPoint);
            var closedPoints = new List<T>(points) { closingPoint };
            return closedPoints;
        }

        return points;
    }

    /// <summary>
    /// Creates a copy of a point, preserving Z value if available
    /// </summary>
    private static T CreatePointCopy<T>(IPoint source) where T : IPoint, new()
    {
        var copy = new T() { X = source.X, Y = source.Y };
        
        // Copy Z value if both source and target support it
        // Note: IHasZ only has getter, but concrete types like PointZ have setter
        if (source is IHasZ sourceHasZ)
        {
            // Try to set Z using type checking first (more efficient)
            if (copy is PointZ pointZCopy)
            {
                pointZCopy.Z = sourceHasZ.Z;
            }
            else
            {
                // Use reflection as fallback for other types that might have Z
                var zProperty = copy.GetType().GetProperty("Z");
                if (zProperty != null && zProperty.CanWrite && zProperty.PropertyType == typeof(double))
                {
                    zProperty.SetValue(copy, sourceHasZ.Z);
                }
            }
        }
        
        return copy;
    }

    private static MultiPointType ConvertMultiPoint<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return null;

        var multiPoint = new MultiPointType
        {
            geometryMember = new List<GeometryAssociationType>()
        };

        foreach (var pointGeo in geometry.Geometries)
        {
            if (pointGeo.Points != null && pointGeo.Points.Count > 0)
            {
                var point = ConvertPoint(pointGeo);
                if (point != null)
                {
                    multiPoint.geometryMember.Add(new PointMemberType { _Geometry = point });
                }
            }
        }

        return multiPoint;
    }

    private static MultiLineStringType ConvertMultiLineString<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return null;

        var multiLineString = new MultiLineStringType
        {
            geometryMember = new List<GeometryAssociationType>()
        };

        foreach (var lineGeo in geometry.Geometries)
        {
            var lineString = ConvertLineString(lineGeo);
            if (lineString != null)
            {
                multiLineString.geometryMember.Add(new LineStringMemberType { _Geometry = lineString });
            }
        }

        return multiLineString;
    }

    private static MultiPolygonType ConvertMultiPolygon<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return null;

        var multiPolygon = new MultiPolygonType
        {
            geometryMember = new List<GeometryAssociationType>()
        };

        foreach (var polyGeo in geometry.Geometries)
        {
            var polygon = ConvertPolygon(polyGeo);
            if (polygon != null)
            {
                multiPolygon.geometryMember.Add(new PolygonMemberType { _Geometry = polygon });
            }
        }

        return multiPolygon;
    }

    private static GeometryCollectionType ConvertGeometryCollection<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return null;

        var collection = new GeometryCollectionType
        {
            geometryMember = new List<GeometryAssociationType>()
        };

        foreach (var geo in geometry.Geometries)
        {
            var gmlGeo = ConvertToGml2(geo);
            if (gmlGeo != null)
            {
                collection.geometryMember.Add(new GeometryAssociationType { _Geometry = gmlGeo });
            }
        }

        return collection;
    }

    private static string FormatCoordinates<T>(List<T> points) where T : IPoint
    {
        if (points == null || points.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        bool hasZ = points.Count > 0 && points[0] is IHasZ;
        
        for (int i = 0; i < points.Count; i++)
        {
            if (i > 0)
                sb.Append(" ");

            if (hasZ && points[i] is IHasZ hasZPoint)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2}", points[i].X, points[i].Y, hasZPoint.Z);
            }
            else
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0},{1}", points[i].X, points[i].Y);
            }
        }

        return sb.ToString();
    }
}

