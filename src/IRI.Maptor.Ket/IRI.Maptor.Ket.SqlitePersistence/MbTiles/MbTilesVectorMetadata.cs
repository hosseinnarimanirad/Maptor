using System;
using System.Collections.Generic;
using System.Text.Json;

using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Ket.SqlitePersistence.MbTiles;

/// <summary>
/// Parses the <c>vector_layers</c> array of the MBTiles <c>json</c> metadata field
/// (https://github.com/mapbox/mbtiles-spec) into <see cref="MvtVectorLayerInfo"/>.
/// </summary>
public static class MbTilesVectorMetadata
{
    public static List<MvtVectorLayerInfo> Parse(string? json)
    {
        var result = new List<MvtVectorLayerInfo>();

        if (string.IsNullOrWhiteSpace(json))
            return result;

        try
        {
            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("vector_layers", out var layers) ||
                layers.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var layer in layers.EnumerateArray())
            {
                if (layer.ValueKind != JsonValueKind.Object)
                    continue;

                var info = new MvtVectorLayerInfo
                {
                    Id = GetString(layer, "id") ?? string.Empty,
                    MinZoom = GetInt(layer, "minzoom"),
                    MaxZoom = GetInt(layer, "maxzoom"),
                    Fields = ParseFields(layer),
                };

                if (!string.IsNullOrEmpty(info.Id))
                    result.Add(info);
            }
        }
        catch (JsonException)
        {
            // Malformed metadata: fall back to sample-tile based enumeration upstream.
        }

        return result;
    }

    private static List<Field> ParseFields(JsonElement layer)
    {
        var fields = new List<Field>();

        if (!layer.TryGetProperty("fields", out var fieldsElement) ||
            fieldsElement.ValueKind != JsonValueKind.Object)
            return fields;

        foreach (var property in fieldsElement.EnumerateObject())
        {
            fields.Add(new Field
            {
                Name = property.Name,
                TypeFullName = MapFieldType(property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null),
            });
        }

        return fields;
    }

    // vector_layers field types are "String", "Number" or "Boolean".
    private static string MapFieldType(string? mvtType) => mvtType switch
    {
        "Number" => typeof(double).FullName!,
        "Boolean" => typeof(bool).FullName!,
        _ => typeof(string).FullName!,
    };

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? GetInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result)
            ? result
            : (int?)null;
}
