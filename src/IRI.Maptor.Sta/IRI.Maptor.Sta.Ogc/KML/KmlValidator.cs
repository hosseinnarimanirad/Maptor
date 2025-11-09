using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Schema;

namespace IRI.Maptor.Ket.KmlFormat;

/// <summary>
/// KML Validator for validating KML content against KML 2.2 specification
/// </summary>
public static class KmlValidator
{
    private const string KmlNamespace = "http://www.opengis.net/kml/2.2";
    private const double MinLatitude = -90.0;
    private const double MaxLatitude = 90.0;
    private const double MinLongitude = -180.0;
    private const double MaxLongitude = 180.0;
    private static readonly Lazy<XmlSchemaSet> SchemaSet = new(LoadSchemaSet);

    public sealed class KmlValidationOptions
    {
        public static KmlValidationOptions Default => new KmlValidationOptions();

        /// <summary>
        /// Enables XML Schema validation against the embedded OGC KML schemas.
        /// </summary>
        public bool ValidateSchema { get; set; } = true;

        /// <summary>
        /// Continues with structural validation even when schema validation reports errors.
        /// </summary>
        public bool BestEffort { get; set; } = true;
    }

    #region Public Validation Methods

    /// <summary>
    /// Validates a KML file
    /// </summary>
    /// <param name="filePath">Path to the KML file</param>
    /// <param name="errors">List of validation errors</param>
    /// <param name="warnings">List of validation warnings</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateFile(string filePath, out List<string> errors, out List<string> warnings, KmlValidationOptions? options = null)
    {
        errors = new List<string>();
        warnings = new List<string>();

        if (!File.Exists(filePath))
        {
            errors.Add($"File not found: {filePath}");
            return false;
        }

        try
        {
            var kmlContent = File.ReadAllText(filePath);
            return Validate(kmlContent, out errors, out warnings, options);
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to read file: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Validates a KML string
    /// </summary>
    /// <param name="kmlString">KML content as string</param>
    /// <param name="errors">List of validation errors</param>
    /// <param name="warnings">List of validation warnings</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool Validate(string kmlString, out List<string> errors, out List<string> warnings, KmlValidationOptions? options = null)
    {
        errors = new List<string>();
        warnings = new List<string>();
        var validationOptions = NormalizeOptions(options);

        if (string.IsNullOrWhiteSpace(kmlString))
        {
            errors.Add("KML content is null or empty");
            return false;
        }

        // Validate XML structure
        if (!ValidateXmlStructure(kmlString, errors))
        {
            return false;
        }

        if (validationOptions.ValidateSchema)
        {
            ValidateAgainstSchema(kmlString, errors, warnings);

            if (!validationOptions.BestEffort && errors.Count > 0)
            {
                return false;
            }
        }

        // Parse and validate KML content
        try
        {
            var document = XDocument.Parse(kmlString);
            ValidateKmlContent(document, errors, warnings);
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse KML: {ex.Message}");
            return false;
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Quick validation check - returns true if valid
    /// </summary>
    /// <param name="kmlString">KML content as string</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(string kmlString, KmlValidationOptions? options = null)
    {
        return Validate(kmlString, out _, out _, options);
    }

    /// <summary>
    /// Validates coordinates range
    /// </summary>
    /// <param name="longitude">Longitude value</param>
    /// <param name="latitude">Latitude value</param>
    /// <returns>True if coordinates are in valid range</returns>
    public static bool ValidateCoordinates(double longitude, double latitude)
    {
        return longitude >= MinLongitude && longitude <= MaxLongitude &&
               latitude >= MinLatitude && latitude <= MaxLatitude;
    }

    /// <summary>
    /// Validates a coordinate string in KML format (longitude,latitude[,altitude])
    /// </summary>
    /// <param name="coordinateString">Coordinate string</param>
    /// <param name="error">Error message if invalid</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateCoordinateString(string coordinateString, out string error)
    {
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(coordinateString))
        {
            error = "Coordinate string is empty";
            return false;
        }

        var parts = coordinateString.Trim().Split(',');

        if (parts.Length < 2)
        {
            error = "Coordinate must have at least longitude and latitude";
            return false;
        }

        if (!double.TryParse(parts[0], out double longitude))
        {
            error = $"Invalid longitude value: {parts[0]}";
            return false;
        }

        if (!double.TryParse(parts[1], out double latitude))
        {
            error = $"Invalid latitude value: {parts[1]}";
            return false;
        }

        if (!ValidateCoordinates(longitude, latitude))
        {
            error = $"Coordinates out of range: longitude={longitude}, latitude={latitude}";
            return false;
        }

        // Validate altitude if present
        if (parts.Length > 2)
        {
            if (!double.TryParse(parts[2], out _))
            {
                error = $"Invalid altitude value: {parts[2]}";
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Private Validation Methods

    private static KmlValidationOptions NormalizeOptions(KmlValidationOptions? options) =>
        options ?? KmlValidationOptions.Default;

    private static void ValidateAgainstSchema(string kmlString, List<string> errors, List<string> warnings)
    {
        try
        {
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = SchemaSet.Value,
                ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings,
                DtdProcessing = DtdProcessing.Ignore
            };

            settings.ValidationEventHandler += (_, args) =>
            {
                var message = FormatSchemaMessage(args);
                if (args.Severity == XmlSeverityType.Warning)
                {
                    warnings.Add(message);
                }
                else
                {
                    errors.Add(message);
                }
            };

            using var reader = XmlReader.Create(new StringReader(kmlString), settings);
            while (reader.Read())
            {
                // Intentionally empty: reading the document triggers validation callbacks.
            }
        }
        catch (XmlException ex)
        {
            errors.Add($"Schema validation failed: {ex.Message}");
        }
    }

    private static bool ValidateXmlStructure(string xmlString, List<string> errors)
    {
        try
        {
            var doc = XDocument.Parse(xmlString);

            // Check for KML namespace
            if (doc.Root?.Name.NamespaceName != KmlNamespace)
            {
                errors.Add($"Invalid KML namespace. Expected: {KmlNamespace}, Found: {doc.Root?.Name.NamespaceName}");
                return false;
            }

            // Check root element is 'kml'
            if (doc.Root?.Name.LocalName != "kml")
            {
                errors.Add($"Root element must be 'kml', found: {doc.Root?.Name.LocalName}");
                return false;
            }

            return true;
        }
        catch (XmlException ex)
        {
            errors.Add($"Invalid XML structure: {ex.Message}");
            return false;
        }
    }

    private static void ValidateKmlContent(XDocument document, List<string> errors, List<string> warnings)
    {
        if (document?.Root == null)
        {
            errors.Add("KML document root is null");
            return;
        }

        XNamespace kml = KmlNamespace;

        // Find all Placemarks
        var placemarks = document.Descendants(kml + "Placemark");

        if (!placemarks.Any())
        {
            warnings.Add("KML document contains no placemarks");
        }

        foreach (var placemark in placemarks)
        {
            ValidatePlacemark(placemark, kml, errors, warnings);
        }
    }

    private static void ValidatePlacemark(XElement placemark, XNamespace kml, List<string> errors, List<string> warnings)
    {
        var name = placemark.Element(kml + "name")?.Value;

        // Warning for placemark without name
        if (string.IsNullOrWhiteSpace(name))
        {
            warnings.Add("Placemark has no name");
        }

        // Check if placemark has geometry
        var hasGeometry = placemark.Element(kml + "Point") != null ||
                         placemark.Element(kml + "LineString") != null ||
                         placemark.Element(kml + "Polygon") != null ||
                         placemark.Element(kml + "MultiGeometry") != null;

        if (!hasGeometry)
        {
            errors.Add($"Placemark '{name ?? "unnamed"}' has no geometry");
            return;
        }

        // Validate specific geometry types
        var point = placemark.Element(kml + "Point");
        if (point != null)
        {
            ValidatePointElement(point, kml, name ?? "unnamed", errors, warnings);
        }

        var lineString = placemark.Element(kml + "LineString");
        if (lineString != null)
        {
            ValidateLineStringElement(lineString, kml, name ?? "unnamed", errors, warnings);
        }

        var polygon = placemark.Element(kml + "Polygon");
        if (polygon != null)
        {
            ValidatePolygonElement(polygon, kml, name ?? "unnamed", errors, warnings);
        }

        var multiGeometry = placemark.Element(kml + "MultiGeometry");
        if (multiGeometry != null)
        {
            ValidateMultiGeometryElement(multiGeometry, kml, name ?? "unnamed", errors, warnings);
        }
    }

    private static void ValidatePointElement(XElement point, XNamespace kml, string featureName, List<string> errors, List<string> warnings)
    {
        var coordinates = point.Element(kml + "coordinates")?.Value;

        if (string.IsNullOrWhiteSpace(coordinates))
        {
            errors.Add($"Point in feature '{featureName}' has no coordinates");
            return;
        }

        if (!ValidateCoordinateString(coordinates, out string error))
        {
            errors.Add($"Invalid point coordinates in feature '{featureName}': {error}");
        }
    }

    private static void ValidateLineStringElement(XElement lineString, XNamespace kml, string featureName, List<string> errors, List<string> warnings)
    {
        var coordinates = lineString.Element(kml + "coordinates")?.Value;

        if (string.IsNullOrWhiteSpace(coordinates))
        {
            errors.Add($"LineString in feature '{featureName}' has no coordinates");
            return;
        }

        var coordinateSets = coordinates.Trim()
            .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        if (coordinateSets.Length < 2)
        {
            errors.Add($"LineString in feature '{featureName}' must have at least 2 points, found {coordinateSets.Length}");
        }

        foreach (var coordSet in coordinateSets)
        {
            if (!ValidateCoordinateString(coordSet, out string error))
            {
                errors.Add($"Invalid LineString coordinate in feature '{featureName}': {error}");
            }
        }
    }

    private static void ValidateLinearRingElement(XElement linearRing, XNamespace kml, string featureName, List<string> errors, List<string> warnings)
    {
        var coordinates = linearRing.Element(kml + "coordinates")?.Value;

        if (string.IsNullOrWhiteSpace(coordinates))
        {
            errors.Add($"LinearRing in feature '{featureName}' has no coordinates");
            return;
        }

        var coordinateSets = coordinates.Trim()
            .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        if (coordinateSets.Length < 4)
        {
            errors.Add($"LinearRing in feature '{featureName}' must have at least 4 points, found {coordinateSets.Length}");
        }

        // Validate ring is closed (first point = last point)
        if (coordinateSets.Length >= 2)
        {
            var firstCoord = coordinateSets[0];
            var lastCoord = coordinateSets[coordinateSets.Length - 1];

            if (firstCoord != lastCoord)
            {
                warnings.Add($"LinearRing in feature '{featureName}' is not closed (first point ≠ last point)");
            }
        }

        foreach (var coordSet in coordinateSets)
        {
            if (!ValidateCoordinateString(coordSet, out string error))
            {
                errors.Add($"Invalid LinearRing coordinate in feature '{featureName}': {error}");
            }
        }
    }

    private static void ValidatePolygonElement(XElement polygon, XNamespace kml, string featureName, List<string> errors, List<string> warnings)
    {
        var outerBoundary = polygon.Element(kml + "outerBoundaryIs");
        if (outerBoundary == null)
        {
            errors.Add($"Polygon in feature '{featureName}' has no outer boundary");
            return;
        }

        var outerRing = outerBoundary.Element(kml + "LinearRing");
        if (outerRing == null)
        {
            errors.Add($"Polygon in feature '{featureName}' outer boundary has no LinearRing");
            return;
        }

        ValidateLinearRingElement(outerRing, kml, $"{featureName} (outer)", errors, warnings);

        // Validate inner boundaries
        var innerBoundaries = polygon.Elements(kml + "innerBoundaryIs");
        int innerIndex = 1;
        foreach (var innerBoundary in innerBoundaries)
        {
            var innerRing = innerBoundary.Element(kml + "LinearRing");
            if (innerRing != null)
            {
                ValidateLinearRingElement(innerRing, kml, $"{featureName} (inner {innerIndex})", errors, warnings);
            }
            innerIndex++;
        }
    }

    private static void ValidateMultiGeometryElement(XElement multiGeometry, XNamespace kml, string featureName, List<string> errors, List<string> warnings)
    {
        var children = multiGeometry.Elements().Where(e => e.Name.Namespace == kml).ToList();

        if (children.Count == 0)
        {
            errors.Add($"MultiGeometry in feature '{featureName}' has no geometries");
            return;
        }

        int index = 1;
        foreach (var child in children)
        {
            if (child.Name.LocalName == "Point")
            {
                ValidatePointElement(child, kml, $"{featureName} (geometry {index})", errors, warnings);
            }
            else if (child.Name.LocalName == "LineString")
            {
                ValidateLineStringElement(child, kml, $"{featureName} (geometry {index})", errors, warnings);
            }
            else if (child.Name.LocalName == "Polygon")
            {
                ValidatePolygonElement(child, kml, $"{featureName} (geometry {index})", errors, warnings);
            }

            index++;
        }
    }

    private static XmlSchemaSet LoadSchemaSet()
    {
        var assembly = typeof(KmlValidator).Assembly;
        var resolver = new EmbeddedResourceResolver(assembly);
        var schemaSet = new XmlSchemaSet
        {
            XmlResolver = resolver
        };

        var readerSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = resolver
        };

        foreach (var resourceName in resolver.ResourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded KML schema resource '{resourceName}' could not be found.");
            using var reader = XmlReader.Create(stream, readerSettings, resourceName);
            schemaSet.Add(null, reader);
        }

        schemaSet.Compile();
        return schemaSet;
    }

    private static string FormatSchemaMessage(ValidationEventArgs args)
    {
        var location = args.Exception is { LineNumber: > 0, LinePosition: > 0 }
            ? $" (line {args.Exception.LineNumber}, position {args.Exception.LinePosition})"
            : string.Empty;
        return $"{args.Severity}: {args.Message}{location}";
    }

    private sealed class EmbeddedResourceResolver : XmlUrlResolver
    {
        private readonly Assembly _assembly;
        private readonly Dictionary<string, string> _resourceMap;

        public EmbeddedResourceResolver(Assembly assembly)
        {
            _assembly = assembly;
            _resourceMap = assembly.GetManifestResourceNames()
                .Where(name => name.EndsWith(".xsd", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(GetFileKey, name => name, StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> ResourceNames => _resourceMap.Values;

        public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        {
            var key = absoluteUri.IsAbsoluteUri
                ? Path.GetFileName(absoluteUri.LocalPath)
                : absoluteUri.OriginalString;

            if (string.IsNullOrWhiteSpace(key))
            {
                key = absoluteUri.ToString();
            }

            if (_resourceMap.TryGetValue(key, out var resourceName))
            {
                var stream = _assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    return stream;
                }
            }

            return base.GetEntity(absoluteUri, role, ofObjectToReturn);
        }

        private static string GetFileKey(string resourceName)
        {
            var parts = resourceName.Split('.');
            return parts.Length >= 2 ? $"{parts[^2]}.{parts[^1]}" : resourceName;
        }
    }

    #endregion

    #region Validation Report

    /// <summary>
    /// Generates a detailed validation report
    /// </summary>
    /// <param name="kmlString">KML content to validate</param>
    /// <returns>Validation report as string</returns>
    public static string GenerateValidationReport(string kmlString, KmlValidationOptions? options = null)
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== KML Validation Report ===");
        report.AppendLine();

        var isValid = Validate(kmlString, out var errors, out var warnings, options);

        report.AppendLine($"Status: {(isValid ? "VALID" : "INVALID")}");
        report.AppendLine($"Errors: {errors.Count}");
        report.AppendLine($"Warnings: {warnings.Count}");
        report.AppendLine();

        if (errors.Count > 0)
        {
            report.AppendLine("Errors:");
            for (int i = 0; i < errors.Count; i++)
            {
                report.AppendLine($"  {i + 1}. {errors[i]}");
            }
            report.AppendLine();
        }

        if (warnings.Count > 0)
        {
            report.AppendLine("Warnings:");
            for (int i = 0; i < warnings.Count; i++)
            {
                report.AppendLine($"  {i + 1}. {warnings[i]}");
            }
            report.AppendLine();
        }

        if (isValid)
        {
            report.AppendLine("✓ KML is valid and ready to use");
        }
        else
        {
            report.AppendLine("✗ KML has validation errors that must be fixed");
        }

        return report.ToString();
    }

    #endregion
}

