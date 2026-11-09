using System.Text;
using System.Text.Json;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Attributes;

namespace IRI.Maptor.Core.Persistence.Import;

/// <summary>
/// Works out which vector format a file (or pasted text) is in, so one "add data" command can send it to
/// the right reader instead of the user having to pick the format first. The extension decides, except for
/// JSON, where the start of the content tells GeoJSON, TopoJSON and Esri JSON apart, and for <c>.txt</c>,
/// where a tab in the first lines means TSV.
/// </summary>
public static class ImportFormatDetector
{
    /// <summary>How much of a JSON file is read to tell its kind; the deciding member is normally near the top.</summary>
    public const int JsonSniffBytes = 1 << 20;

    private const int TextSniffChars = 64 * 1024;

    /// <summary>The formats one "add data from file" command can open.</summary>
    public static IReadOnlyList<DataSourceKind> FileKinds { get; } = new[]
    {
        DataSourceKind.Shapefile,
        DataSourceKind.Kml,
        DataSourceKind.Kmz,
        DataSourceKind.Gpx,
        DataSourceKind.GeoPackage,
        DataSourceKind.Dxf,
        DataSourceKind.GeoJson,
        DataSourceKind.TopoJson,
        DataSourceKind.EsriJson,
        DataSourceKind.Csv,
        DataSourceKind.Tsv,
    };

    /// <summary>The formats that can be pasted as text.</summary>
    public static IReadOnlyList<DataSourceKind> PasteKinds { get; } = new[]
    {
        DataSourceKind.GeoJson,
        DataSourceKind.TopoJson,
        DataSourceKind.EsriJson,
        DataSourceKind.Kml,
        DataSourceKind.Csv,
        DataSourceKind.Tsv,
    };

    private static readonly HashSet<string> GeoJsonTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "FeatureCollection", "Feature", "Point", "MultiPoint", "LineString", "MultiLineString",
        "Polygon", "MultiPolygon", "GeometryCollection",
    };

    // Top-level members only an Esri JSON feature set has.
    private static readonly HashSet<string> EsriJsonMembers = new(StringComparer.Ordinal)
    {
        "geometryType", "spatialReference", "fields", "fieldAliases", "displayFieldName",
        "globalIdFieldName", "objectIdFieldName", "hasZ", "hasM",
    };

    /// <summary>
    /// The format of <paramref name="filePath"/>, or null when it is not one of <see cref="FileKinds"/> (unknown
    /// extension, or JSON whose content matches no supported kind). Only JSON and <c>.txt</c> files are read.
    /// </summary>
    /// <exception cref="IOException">The file could not be read.</exception>
    public static DataSourceKind? DetectFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        switch (Path.GetExtension(filePath).ToLowerInvariant())
        {
            case ".shp": return DataSourceKind.Shapefile;
            case ".kml": return DataSourceKind.Kml;
            case ".kmz": return DataSourceKind.Kmz;
            case ".gpx": return DataSourceKind.Gpx;
            case ".gpkg": return DataSourceKind.GeoPackage;
            case ".dxf": return DataSourceKind.Dxf;
            case ".csv": return DataSourceKind.Csv;
            case ".tsv": return DataSourceKind.Tsv;

            case ".txt":
                return HasTabInFirstLines(ReadStartAsText(filePath)) ? DataSourceKind.Tsv : DataSourceKind.Csv;

            // The content wins over the extension: a TopoJSON saved as .geojson is still TopoJSON.
            case ".geojson": return DetectJson(ReadStart(filePath, JsonSniffBytes)) ?? DataSourceKind.GeoJson;
            case ".topojson": return DetectJson(ReadStart(filePath, JsonSniffBytes)) ?? DataSourceKind.TopoJson;
            case ".json": return DetectJson(ReadStart(filePath, JsonSniffBytes));

            default: return null;
        }
    }

    /// <summary>
    /// The format of pasted text: JSON by its content, KML for XML with a <c>kml</c> element, otherwise TSV when a
    /// line has a tab and CSV for any other text (the CSV reader also takes two numbers separated by spaces). Null
    /// for empty text, other XML and JSON that matches no supported kind.
    /// </summary>
    public static DataSourceKind? DetectFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = text!.TrimStart('﻿', ' ', '\t', '\r', '\n');

        if (trimmed.Length == 0)
            return null;

        if (trimmed[0] == '{' || trimmed[0] == '[')
        {
            var head = trimmed.Length > JsonSniffBytes ? trimmed.Substring(0, JsonSniffBytes) : trimmed;

            return DetectJson(Encoding.UTF8.GetBytes(head));
        }

        if (trimmed[0] == '<')
        {
            // the root element, after any XML declaration or comment, is <kml> (with or without a prefix)
            var head = trimmed.Length > TextSniffChars ? trimmed.Substring(0, TextSniffChars) : trimmed;

            return System.Text.RegularExpressions.Regex.IsMatch(head, @"<(\w+:)?kml[\s>/]", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                ? DataSourceKind.Kml
                : null;
        }

        return HasTabInFirstLines(trimmed) ? DataSourceKind.Tsv : DataSourceKind.Csv;
    }

    /// <summary>
    /// GeoJSON, TopoJSON or Esri JSON from the start of a UTF-8 JSON document (it may be cut off), or null.
    /// The document must be an object; the first deciding member wins.
    /// </summary>
    public static DataSourceKind? DetectJson(ReadOnlySpan<byte> utf8)
    {
        if (utf8.Length >= 3 && utf8[0] == 0xEF && utf8[1] == 0xBB && utf8[2] == 0xBF)
            utf8 = utf8.Slice(3);

        var reader = new Utf8JsonReader(utf8, isFinalBlock: false, new JsonReaderState(new JsonReaderOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        }));

        string? topMember = null;

        try
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                return null;

            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                if (reader.CurrentDepth == 1)
                {
                    topMember = reader.GetString();

                    if (topMember is null)
                        continue;

                    if (EsriJsonMembers.Contains(topMember))
                        return DataSourceKind.EsriJson;

                    if (topMember == "arcs")
                        return DataSourceKind.TopoJson;

                    if (topMember == "type")
                    {
                        if (!reader.Read())
                            return null;

                        if (reader.TokenType != JsonTokenType.String)
                            continue;

                        var type = reader.GetString();

                        if (string.Equals(type, "Topology", StringComparison.OrdinalIgnoreCase))
                            return DataSourceKind.TopoJson;

                        if (type is not null && GeoJsonTypes.Contains(type))
                            return DataSourceKind.GeoJson;
                    }
                }
                // Members of the first features: Esri features carry "attributes", GeoJSON ones "properties"/"type".
                else if (reader.CurrentDepth == 3 && topMember == "features")
                {
                    var member = reader.GetString();

                    if (member == "attributes")
                        return DataSourceKind.EsriJson;

                    if (member == "properties" || member == "type")
                        return DataSourceKind.GeoJson;
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON, or broken before a deciding member: nothing to go on.
        }

        return null;
    }

    /// <summary>
    /// True when the file holds everything needed to add it (KML, KMZ, GPX, GeoPackage, and a shapefile whose
    /// .prj can be read); false when the import options dialog has to ask for something, mainly the CRS.
    /// </summary>
    public static bool CanAddWithoutOptions(DataSourceKind kind, string? filePath)
    {
        switch (kind)
        {
            case DataSourceKind.Kml:
            case DataSourceKind.Kmz:
            case DataSourceKind.Gpx:
            case DataSourceKind.GeoPackage:
                return true;

            case DataSourceKind.Shapefile:
                return !string.IsNullOrWhiteSpace(filePath) && ShapefileFormat.Shapefile.TryGetSrs(filePath!) is not null;

            default:
                return false;
        }
    }

    /// <summary>
    /// The open-file dialog filter for <see cref="FileKinds"/>: "All supported files" first (selected by default),
    /// then each format's own filter from <see cref="DataSourceKindInfoAttribute"/>, without repeats.
    /// </summary>
    /// <param name="allSupportedLabel">The localized name of the first entry, e.g. "All supported files".</param>
    public static string GetFileDialogFilter(string allSupportedLabel)
    {
        var entries = new List<(string Name, string Patterns)>();

        foreach (var kind in FileKinds)
        {
            var parts = IRI.Maptor.Extensions.DataSourceKindExtensions.GetFileFilter(kind).Split('|');

            for (int i = 0; i + 1 < parts.Length; i += 2)
            {
                var entry = (parts[i].Trim(), parts[i + 1].Trim());

                if (entry.Item1.Length > 0 && entry.Item2.Length > 0 && !entries.Contains(entry))
                    entries.Add(entry);
            }
        }

        var allPatterns = entries
            .SelectMany(e => e.Patterns.Split(';'))
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var label = string.IsNullOrWhiteSpace(allSupportedLabel) ? "All supported files" : allSupportedLabel.Replace("|", " ");

        var all = string.Join(";", allPatterns);

        return string.Join("|", new[] { $"{label}|{all}" }.Concat(entries.Select(e => $"{e.Name}|{e.Patterns}")));
    }

    private static byte[] ReadStart(string filePath, int maxBytes)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var buffer = new byte[(int)Math.Min(maxBytes, stream.Length)];

        int read = 0;

        while (read < buffer.Length)
        {
            int n = stream.Read(buffer, read, buffer.Length - read);

            if (n == 0)
                break;

            read += n;
        }

        return read == buffer.Length ? buffer : buffer.AsSpan(0, read).ToArray();
    }

    private static string ReadStartAsText(string filePath) => Encoding.UTF8.GetString(ReadStart(filePath, TextSniffChars));

    private static bool HasTabInFirstLines(string text)
    {
        int lines = 0;

        using var reader = new StringReader(text);

        string? line;

        while ((line = reader.ReadLine()) is not null && lines < 20)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.IndexOf('\t') >= 0)
                return true;

            lines++;
        }

        return false;
    }
}
