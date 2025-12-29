using System;
using System.Linq;
using System.Xml.Linq;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Ogc.GML;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Ogc.Extensions;

internal enum GmlVersion
{
    Unknown,
    Gml2,
    Gml3
}

public static class Sta_GmlExtensions
{
    /// <summary>
    /// Converts a Geometry to GML 2 format string
    /// </summary>
    /// <param name="geometry">The geometry to convert</param>
    /// <param name="includeSrid">Whether to include SRID in srsName attribute</param>
    /// <returns>GML 2 formatted string</returns>
    public static string AsGml2(this IGeometry geometry, bool includeSrid = false)
    {
        return Gml2Writer.AsGml2(geometry, includeSrid);
    }

    /// <summary>
    /// Converts a Geometry to GML 3 format string
    /// </summary>
    /// <param name="geometry">The geometry to convert</param>
    /// <param name="includeSrid">Whether to include SRID in srsName attribute</param>
    /// <returns>GML 3 formatted string</returns>
    public static string AsGml3(this IGeometry geometry, bool includeSrid = false)
    {
        return Gml3Writer.AsGml3(geometry, includeSrid);
    }

    /// <summary>
    /// Parses a GML 2 string into a Geometry
    /// </summary>
    /// <param name="gmlString">GML 2 formatted string</param>
    /// <param name="srid">Spatial Reference System ID (will be extracted from srsName if not provided)</param>
    /// <returns>Parsed Geometry (Geometry&lt;Point&gt; or Geometry&lt;PointZ&gt; depending on Z values in GML)</returns>
    public static IGeometry FromGml2(string gmlString, int srid = 0)
    {
        return Gml2Reader.Parse(gmlString, srid);
    }

    /// <summary>
    /// Parses a GML 3 string into a Geometry
    /// </summary>
    /// <param name="gmlString">GML 3 formatted string</param>
    /// <param name="srid">Spatial Reference System ID (will be extracted from srsName if not provided)</param>
    /// <returns>Parsed Geometry (Geometry&lt;Point&gt; or Geometry&lt;PointZ&gt; depending on Z values in GML)</returns>
    public static IGeometry FromGml3(string gmlString, int srid = 0)
    {
        return Gml3Reader.Parse(gmlString, srid);
    }

    /// <summary>
    /// Automatically detects and parses a GML string (GML 2 or GML 3) into a Geometry.
    /// Detection is performed by checking XML headers (schemaLocation and namespace URIs) first,
    /// then falling back to element-based detection.
    /// </summary>
    /// <param name="gmlString">GML formatted string (GML 2 or GML 3)</param>
    /// <param name="srid">Spatial Reference System ID (will be extracted from srsName if not provided)</param>
    /// <returns>Parsed Geometry (Geometry&lt;Point&gt; or Geometry&lt;PointZ&gt; depending on Z values in GML)</returns>
    public static IGeometry FromGml(string gmlString, int srid = 0)
    {
        if (string.IsNullOrWhiteSpace(gmlString))
            return Geometry<Point>.Empty;

        try
        {
            var doc = XDocument.Parse(gmlString);
            var root = doc.Root;

            if (root == null)
                return Geometry<Point>.Empty;

            // Detect GML version
            var detectedVersion = DetectGmlVersion(root);

            // Try parsing with detected version
            if (detectedVersion == GmlVersion.Gml3)
            {
                try
                {
                    return FromGml3(gmlString, srid);
                }
                catch (Exception ex)
                {
                    // Fallback to GML 2 if GML 3 parsing fails
                    try
                    {
                        return FromGml2(gmlString, srid);
                    }
                    catch
                    {
                        throw new FormatException($"Failed to parse GML string. Detected as GML 3 but parsing failed. GML 3 error: {ex.Message}", ex);
                    }
                }
            }
            else if (detectedVersion == GmlVersion.Gml2)
            {
                try
                {
                    return FromGml2(gmlString, srid);
                }
                catch (Exception ex)
                {
                    // Fallback to GML 3 if GML 2 parsing fails
                    try
                    {
                        return FromGml3(gmlString, srid);
                    }
                    catch
                    {
                        throw new FormatException($"Failed to parse GML string. Detected as GML 2 but parsing failed. GML 2 error: {ex.Message}", ex);
                    }
                }
            }
            else
            {
                // Unknown version - try GML 3 first (more modern), then GML 2
                try
                {
                    return FromGml3(gmlString, srid);
                }
                catch
                {
                    try
                    {
                        return FromGml2(gmlString, srid);
                    }
                    catch (Exception ex)
                    {
                        throw new FormatException($"Failed to parse GML string. Could not determine GML version and both parsers failed. Last error: {ex.Message}", ex);
                    }
                }
            }
        }
        catch (FormatException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FormatException($"Failed to parse GML string: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Detects the GML version by checking XML headers and element structure
    /// </summary>
    private static GmlVersion DetectGmlVersion(XElement root)
    {
        // Priority 1: Check xsi:schemaLocation attribute
        var schemaLocation = DetectFromSchemaLocation(root);
        if (schemaLocation != GmlVersion.Unknown)
            return schemaLocation;

        // Priority 2: Check namespace URIs
        var namespaceVersion = DetectFromNamespace(root);
        if (namespaceVersion != GmlVersion.Unknown)
            return namespaceVersion;

        // Priority 3: Element-based detection
        return DetectFromElements(root);
    }

    /// <summary>
    /// Detects GML version from xsi:schemaLocation attribute
    /// </summary>
    private static GmlVersion DetectFromSchemaLocation(XElement root)
    {
        var xsiNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
        var schemaLocationAttr = root.Attribute(xsiNamespace + "schemaLocation") ?? root.Attribute("schemaLocation");

        if (schemaLocationAttr == null || string.IsNullOrWhiteSpace(schemaLocationAttr.Value))
            return GmlVersion.Unknown;

        var schemaLocation = schemaLocationAttr.Value;

        // Check for GML 3 patterns
        if (schemaLocation.Contains("/gml/3") || 
            schemaLocation.Contains("/gml3") || 
            schemaLocation.Contains("gml32") ||
            schemaLocation.Contains("gml/3.1.1") ||
            schemaLocation.Contains("gml/3.2"))
        {
            return GmlVersion.Gml3;
        }

        // Check for GML 2 patterns
        if (schemaLocation.Contains("/gml/2") || 
            schemaLocation.Contains("/gml2") ||
            schemaLocation.Contains("gml/2.1") ||
            schemaLocation.Contains("gml/2.1.2"))
        {
            return GmlVersion.Gml2;
        }

        return GmlVersion.Unknown;
    }

    /// <summary>
    /// Detects GML version from namespace URIs
    /// </summary>
    private static GmlVersion DetectFromNamespace(XElement root)
    {
        // Check all namespace declarations
        foreach (var attr in root.Attributes())
        {
            if (attr.IsNamespaceDeclaration)
            {
                var namespaceUri = attr.Value;
                
                // Check for GML 3 version-specific namespace paths
                if (namespaceUri.Contains("/gml/3") || 
                    namespaceUri.Contains("/gml3") ||
                    namespaceUri == "http://www.opengis.net/gml/3.2" ||
                    namespaceUri == "http://www.opengis.net/gml/3.1.1")
                {
                    return GmlVersion.Gml3;
                }
            }
            else if (attr.Name == "xmlns" || attr.Name.LocalName == "gml")
            {
                var namespaceUri = attr.Value;
                if (namespaceUri.Contains("/gml/3") || namespaceUri.Contains("/gml3"))
                {
                    return GmlVersion.Gml3;
                }
            }
        }

        return GmlVersion.Unknown;
    }

    /// <summary>
    /// Detects GML version from element structure (fallback method)
    /// </summary>
    private static GmlVersion DetectFromElements(XElement root)
    {
        var gmlNamespace = XNamespace.Get("http://www.opengis.net/gml");

        // Check for GML 3 specific elements
        var hasPos = root.Descendants(gmlNamespace + "pos").Any();
        var hasPosList = root.Descendants(gmlNamespace + "posList").Any();
        var hasExterior = root.Descendants(gmlNamespace + "exterior").Any();
        var hasInterior = root.Descendants(gmlNamespace + "interior").Any();
        var hasPointMember = root.Descendants(gmlNamespace + "pointMember").Any();
        var hasLineStringMember = root.Descendants(gmlNamespace + "lineStringMember").Any();
        var hasPolygonMember = root.Descendants(gmlNamespace + "polygonMember").Any();

        if (hasPos || hasPosList || hasExterior || hasInterior || 
            hasPointMember || hasLineStringMember || hasPolygonMember)
        {
            return GmlVersion.Gml3;
        }

        // Check for GML 2 specific elements
        var hasOuterBoundaryIs = root.Descendants(gmlNamespace + "outerBoundaryIs").Any();
        var hasInnerBoundaryIs = root.Descendants(gmlNamespace + "innerBoundaryIs").Any();

        if (hasOuterBoundaryIs || hasInnerBoundaryIs)
        {
            return GmlVersion.Gml2;
        }

        return GmlVersion.Unknown;
    }
}
