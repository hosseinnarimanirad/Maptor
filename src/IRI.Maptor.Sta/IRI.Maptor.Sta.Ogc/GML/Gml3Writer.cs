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
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Ogc.GML.v313;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Sta.Ogc.GML;

public static class Gml3Writer
{
    private const string GmlNamespace = "http://www.opengis.net/gml";

    public static string AsGml3(IGeometry geometry, bool includeSrid = false)
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
        var method = typeof(Gml3Writer).GetMethod(
            nameof(AsGml3Internal), 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        if (method == null)
        {
            throw new InvalidOperationException($"Method {nameof(AsGml3Internal)} not found on {nameof(Gml3Writer)}");
        }

        // Make it generic with the point type
        var genericMethod = method.MakeGenericMethod(pointType);
        
        // Cast geometry to Geometry<T> and invoke
        // Since we've verified it's a Geometry<T>, we can safely cast
        var typedGeometry = (dynamic)geometry;
        var result = genericMethod.Invoke(null, new object[] { typedGeometry, includeSrid });
        
        return result as string ?? string.Empty;
    }

    private static string AsGml3Internal<T>(Geometry<T> geometry, bool includeSrid = false) where T : IPoint, new()
    {
        if (geometry.IsNullOrEmpty())
            return string.Empty;

        var gmlGeometry = ConvertToGml3(geometry);
        if (gmlGeometry == null)
            return string.Empty;

        var serializer = new XmlSerializer(gmlGeometry.GetType(), GmlNamespace);
        
        // Serialize to a StringWriter first, then parse into XElement
        // This avoids the ConformanceLevel.Fragment issue with XElement.CreateWriter()
        using var stringWriter = new StringWriter();
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("gml", GmlNamespace);
        namespaces.Add("", GmlNamespace);
        
        serializer.Serialize(stringWriter, gmlGeometry, namespaces);
        var serializedXml = stringWriter.ToString();
        
        // Parse the serialized XML and use the root element
        var doc = XDocument.Parse(serializedXml);
        var root = doc.Root;
        
        if (root == null)
            return string.Empty;

        // Fix XML formatting issues
        bool hasZ = geometry.HasZ();
        FixPosElements(root);
        RemoveEmptyDescriptionElements(root);
        FixSrsDimension(root, hasZ);

        // Add srsName attribute if requested
        if (includeSrid && geometry.Srid > 0)
        {
            root.SetAttributeValue("srsName", $"http://www.opengis.net/gml/srs/epsg.xml#{geometry.Srid}");
        }

        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private static AbstractGeometryType ConvertToGml3<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        return geometry.Type switch
        {
            GeometryType.Point => ConvertPoint(geometry),
            GeometryType.LineString => ConvertLineString(geometry),
            GeometryType.Polygon => ConvertPolygon(geometry),
            GeometryType.MultiPoint => ConvertMultiPoint(geometry),
            GeometryType.MultiLineString => ConvertMultiLineString(geometry),
            GeometryType.MultiPolygon => ConvertMultiPolygon(geometry),
            _ => throw new NotImplementedException($"GML 3 conversion not supported for geometry type {geometry.Type}")
        };
    }

    private static PointType ConvertPoint<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.Points == null || geometry.Points.Count == 0)
            return null;

        var point = geometry.Points[0];
        var coordValues = new List<double> { point.X, point.Y };
        
        // Add Z value if available
        if (point is IHasZ hasZ)
        {
            coordValues.Add(hasZ.Z);
        }

        var pos = new DirectPositionType
        {
            Text = coordValues
        };

        // Set srsDimension based on coordinate dimension
        if (geometry.HasZ())
        {
            pos.srsDimension = "3";
        }
        else
        {
            pos.srsDimension = "2";
        }

        return new PointType 
        { 
            Item = pos
        };
    }

    private static LineStringType ConvertLineString<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.Points == null || geometry.Points.Count < 2)
            return null;

        var coordValues = new List<double>();
        bool hasZ = geometry.HasZ();
        
        foreach (var point in geometry.Points)
        {
            coordValues.Add(point.X);
            coordValues.Add(point.Y);
            if (hasZ && point is IHasZ hasZPoint)
            {
                coordValues.Add(hasZPoint.Z);
            }
        }

        var posList = new DirectPositionListType
        {
            Text = coordValues,
            count = geometry.Points.Count.ToString()
        };

        // Set srsDimension based on coordinate dimension
        if (hasZ)
        {
            posList.srsDimension = "3";
        }
        else
        {
            posList.srsDimension = "2";
        }

        return new LineStringType 
        { 
            Items = new object[] { posList },
            ItemsElementName = new[] { ItemsChoiceType1.posList }
        };
    }

    private static PolygonType ConvertPolygon<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return null;

        var polygon = new PolygonType();
        bool hasZ = geometry.HasZ();

        // Outer boundary (first ring)
        if (geometry.Geometries.Count > 0)
        {
            var outerRing = geometry.Geometries[0];
            var ringPoints = EnsureRingClosed(outerRing.Points);
            
            var coordValues = new List<double>();
            foreach (var point in ringPoints)
            {
                coordValues.Add(point.X);
                coordValues.Add(point.Y);
                if (hasZ && point is IHasZ hasZPoint)
                {
                    coordValues.Add(hasZPoint.Z);
                }
            }

            var posList = new DirectPositionListType
            {
                Text = coordValues,
                count = ringPoints.Count.ToString()
            };

            // Set srsDimension based on coordinate dimension
            if (hasZ)
            {
                posList.srsDimension = "3";
            }
            else
            {
                posList.srsDimension = "2";
            }

            var linearRing = new LinearRingType
            {
                Items = new object[] { posList },
                ItemsElementName = new[] { ItemsChoiceType7.posList }
            };

            polygon.exterior = new AbstractRingPropertyType { _Ring = linearRing };
        }

        // Inner boundaries (holes)
        if (geometry.Geometries.Count > 1)
        {
            polygon.interior = new List<AbstractRingPropertyType>();
            for (int i = 1; i < geometry.Geometries.Count; i++)
            {
                var innerRing = geometry.Geometries[i];
                var ringPoints = EnsureRingClosed(innerRing.Points);
                
                var coordValues = new List<double>();
                foreach (var point in ringPoints)
                {
                    coordValues.Add(point.X);
                    coordValues.Add(point.Y);
                    if (hasZ && point is IHasZ hasZPoint)
                    {
                        coordValues.Add(hasZPoint.Z);
                    }
                }

                var posList = new DirectPositionListType
                {
                    Text = coordValues,
                    count = ringPoints.Count.ToString()
                };

                // Set srsDimension based on coordinate dimension
                if (hasZ)
                {
                    posList.srsDimension = "3";
                }
                else
                {
                    posList.srsDimension = "2";
                }

                var linearRing = new LinearRingType
                {
                    Items = new object[] { posList },
                    ItemsElementName = new[] { ItemsChoiceType7.posList }
                };

                polygon.interior.Add(new AbstractRingPropertyType { _Ring = linearRing });
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
            pointMembers = new List<PointType>()
        };

        foreach (var pointGeo in geometry.Geometries)
        {
            if (pointGeo.Points != null && pointGeo.Points.Count > 0)
            {
                var point = ConvertPoint(pointGeo);
                if (point != null)
                {
                    multiPoint.pointMembers.Add(point);
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
            lineStringMember = new List<LineStringPropertyType>()
        };

        foreach (var lineGeo in geometry.Geometries)
        {
            var lineString = ConvertLineString(lineGeo);
            if (lineString != null)
            {
                multiLineString.lineStringMember.Add(new LineStringPropertyType { LineString = lineString });
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
            polygonMember = new List<PolygonPropertyType>()
        };

        foreach (var polyGeo in geometry.Geometries)
        {
            var polygon = ConvertPolygon(polyGeo);
            if (polygon != null)
            {
                multiPolygon.polygonMember.Add(new PolygonPropertyType { Polygon = polygon });
            }
        }

        return multiPolygon;
    }

    /// <summary>
    /// Fixes pos and posList elements by converting Text attribute to element content
    /// </summary>
    private static void FixPosElements(XElement root)
    {
        var gmlNs = XNamespace.Get(GmlNamespace);
        
        // Fix pos elements
        var posElements = root.Descendants(gmlNs + "pos").ToList();
        foreach (var posElement in posElements)
        {
            // Check if pos element has Text attribute (incorrect serialization)
            var textAttr = posElement.Attribute("Text");
            if (textAttr != null)
            {
                // Extract coordinate values from the attribute
                // The attribute value should be a space-separated list of doubles
                var coordString = textAttr.Value;
                
                // Create new pos element with coordinates as text content
                var newPos = new XElement(gmlNs + "pos");
                
                // Copy all attributes except Text
                foreach (var attr in posElement.Attributes())
                {
                    if (attr.Name != "Text")
                    {
                        newPos.SetAttributeValue(attr.Name, attr.Value);
                    }
                }
                
                // Set coordinates as element content
                newPos.Value = coordString;
                
                // Replace the old element
                posElement.ReplaceWith(newPos);
            }
        }

        // Fix posList elements
        var posListElements = root.Descendants(gmlNs + "posList").ToList();
        foreach (var posListElement in posListElements)
        {
            // Check if posList element has Text attribute (incorrect serialization)
            var textAttr = posListElement.Attribute("Text");
            if (textAttr != null)
            {
                // Extract coordinate values from the attribute
                var coordString = textAttr.Value;
                
                // Create new posList element with coordinates as text content
                var newPosList = new XElement(gmlNs + "posList");
                
                // Copy all attributes except Text
                foreach (var attr in posListElement.Attributes())
                {
                    if (attr.Name != "Text")
                    {
                        newPosList.SetAttributeValue(attr.Name, attr.Value);
                    }
                }
                
                // Set coordinates as element content
                newPosList.Value = coordString;
                
                // Replace the old element
                posListElement.ReplaceWith(newPosList);
            }
        }
    }

    /// <summary>
    /// Removes empty description elements
    /// </summary>
    private static void RemoveEmptyDescriptionElements(XElement root)
    {
        var gmlNs = XNamespace.Get(GmlNamespace);
        var descriptionElements = root.Descendants(gmlNs + "description").ToList();

        foreach (var descElement in descriptionElements)
        {
            // Remove if empty or contains only whitespace
            if (string.IsNullOrWhiteSpace(descElement.Value))
            {
                descElement.Remove();
            }
        }
    }

    /// <summary>
    /// Fixes srsDimension attribute - removes it for 2D geometries, ensures it's present for 3D
    /// </summary>
    private static void FixSrsDimension(XElement root, bool hasZ)
    {
        var gmlNs = XNamespace.Get(GmlNamespace);
        
        // Find all pos and posList elements
        var posElements = root.Descendants(gmlNs + "pos").ToList();
        var posListElements = root.Descendants(gmlNs + "posList").ToList();

        if (!hasZ)
        {
            // For 2D geometries, remove srsDimension attribute
            foreach (var posElement in posElements)
            {
                posElement.Attribute("srsDimension")?.Remove();
            }
            foreach (var posListElement in posListElements)
            {
                posListElement.Attribute("srsDimension")?.Remove();
            }
        }
        else
        {
            // For 3D geometries, ensure srsDimension="3" is present
            foreach (var posElement in posElements)
            {
                if (posElement.Attribute("srsDimension") == null)
                {
                    posElement.SetAttributeValue("srsDimension", "3");
                }
            }
            foreach (var posListElement in posListElements)
            {
                if (posListElement.Attribute("srsDimension") == null)
                {
                    posListElement.SetAttributeValue("srsDimension", "3");
                }
            }
        }
    }
}

