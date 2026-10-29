using System.Globalization;
using System.Xml.Linq;

namespace IRI.Maptor.Core.FileGdb.Catalog.Xml;

/// <summary>
/// Helpers for the item definition XML in GDB_Items. The same document can come with or without
/// namespace prefixes (<c>&lt;typens:GPRangeDomain2&gt;</c> or <c>&lt;GPCodedValueDomain2&gt;</c>), on
/// one line or pretty-printed, and with any Esri schema version (10.0 to 10.8 seen), so elements
/// are always matched by local name only.
/// </summary>
internal static class GdbXml
{
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    public static XElement Parse(string xml) => XDocument.Parse(xml).Root!;

    public static XElement? Child(this XElement element, string localName)
        => element.Elements().FirstOrDefault(e => e.Name.LocalName == localName);

    public static IEnumerable<XElement> Children(this XElement? element, string localName)
        => element?.Elements().Where(e => e.Name.LocalName == localName) ?? Enumerable.Empty<XElement>();

    /// <summary>Text of a child element; <c>null</c> when it is missing or empty.</summary>
    public static string? Text(this XElement element, string localName)
    {
        var value = element.Child(localName)?.Value;

        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static bool Bool(this XElement element, string localName)
        => string.Equals(element.Text(localName), "true", StringComparison.OrdinalIgnoreCase);

    public static int? Int(this XElement element, string localName)
        => int.TryParse(element.Text(localName), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    /// <summary>
    /// A value element typed by its <c>xsi:type</c> attribute (e.g. <c>&lt;Code xsi:type='xs:short'&gt;1&lt;/Code&gt;</c>).
    /// Untyped or unknown types are returned as text.
    /// </summary>
    public static object? TypedValue(this XElement? element)
    {
        if (element is null || element.IsEmpty)
            return null;

        var text = element.Value;
        var type = element.Attribute(Xsi + "type")?.Value;
        var localType = type?.Substring(type.IndexOf(':') + 1);

        try
        {
            switch (localType)
            {
                case "short": return short.Parse(text, CultureInfo.InvariantCulture);
                case "int": return int.Parse(text, CultureInfo.InvariantCulture);
                case "long": return long.Parse(text, CultureInfo.InvariantCulture);
                case "float": return float.Parse(text, CultureInfo.InvariantCulture);
                case "double": return double.Parse(text, CultureInfo.InvariantCulture);
                case "dateTime": return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                default: return text;
            }
        }
        catch (FormatException)
        {
            return text;
        }
        catch (OverflowException)
        {
            return text;
        }
    }
}
