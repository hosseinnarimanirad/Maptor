using System.Collections.Generic;
using System.Text.Json;

using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Presentation.Wpf.Layers;

/// <summary>One drawing read from a file: either a drawing with its own style, or a geometry to draw with the default style.</summary>
/// <param name="Name">The drawing's name: the file name, numbered when the file holds several features.</param>
/// <param name="Styled">A drawing saved by Maptor, with its style; null for a plain geometry.</param>
/// <param name="Geometry">A plain geometry (Web Mercator), drawn with the default style; null for a styled drawing.</param>
public sealed record DrawingItemFileEntry(string Name, DrawingItemLayer? Styled, Geometry<Point>? Geometry);

/// <summary>
/// Reads the drawings of a file opened from the drawings list. It takes what Maptor writes and any other GeoJSON:
/// <list type="bullet">
/// <item>a drawing saved by Maptor (<c>.mtd</c>, or <c>.geojson</c> with the same content): one GeoJSON feature with
/// its style embedded in its properties; also a text drawing;</item>
/// <item>a FeatureCollection, as the drawings list's "Export as GeoJSON" writes (one feature, no style) and as other
/// programs write: each feature becomes a drawing, with its embedded style if it has one;</item>
/// <item>a single feature without a style, and a bare geometry.</item>
/// </list>
/// Coordinates are longitude, latitude (WGS 84), as GeoJSON has them. A file it cannot read gives no entries.
/// </summary>
public static class DrawingItemFileReader
{
    public static IReadOnlyList<DrawingItemFileEntry> Read(string name, string json)
    {
        var result = new List<DrawingItemFileEntry>();

        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
            return result;

        switch (typeElement.GetString())
        {
            case GeoJson.FeatureCollection:
                if (!root.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
                    return result;

                var count = features.GetArrayLength();

                var index = 0;

                foreach (var feature in features.EnumerateArray())
                {
                    index++;

                    AddFeature(result, count == 1 ? name : $"{name} ({index})", feature.GetRawText());
                }

                break;

            case GeoJson.Feature:
                AddFeature(result, name, root.GetRawText());
                break;

            // a bare geometry: read as a feature without properties
            default:
                AddFeature(result, name, $"{{\"type\":\"Feature\",\"properties\":{{}},\"geometry\":{root.GetRawText()}}}");
                break;
        }

        return result;
    }

    private static void AddFeature(List<DrawingItemFileEntry> result, string name, string featureJson)
    {
        // a drawing saved by Maptor: a feature with its style embedded
        if (DrawingItemLayer.Deserialize(name, featureJson) is { } styled)
        {
            result.Add(new DrawingItemFileEntry(name, styled, null));
            return;
        }

        // any other feature: its geometry, drawn with the default style
        var feature = JsonHelper.Deserialize<GeoJsonFeature>(featureJson);

        if (feature?.Geometry is null)
            return;

        var geometry = feature.AsFeature(isLongitudeFirst: true, SrsBases.WebMercator).TheGeometry;

        if (geometry.IsNullOrEmpty())
            return;

        result.Add(new DrawingItemFileEntry(name, null, geometry));
    }
}
