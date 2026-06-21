using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Common.Common.JsonConverters;

/// <summary>
/// Deserializes a JSON array into an <see cref="object"/>[] whose elements are real CLR
/// primitives (string, int, long, double, decimal, bool, null) instead of <see cref="JsonElement"/>.
/// Mirrors <see cref="DictionaryStringObjectConverter"/> so that values surviving an API round-trip
/// (e.g. <c>Field.AllowedValues</c>) compare equal to the equally-typed cell values they are matched
/// against in the UI (e.g. a combobox SelectedItem bound to a string attribute).
/// </summary>
public class ObjectArrayConverter : JsonConverter<object[]>
{
    public override object[] Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);

        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
            return [];

        var list = new List<object?>();

        foreach (var item in root.EnumerateArray())
        {
            list.Add(ConvertElement(item));
        }

        return list.ToArray()!;
    }

    private static object? ConvertElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => GetNumberValue(element),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    private static object GetNumberValue(JsonElement element)
    {
        if (element.TryGetInt32(out int intValue)) return intValue;
        if (element.TryGetInt64(out long longValue)) return longValue;
        if (element.TryGetDouble(out double doubleValue)) return doubleValue;
        if (element.TryGetDecimal(out decimal decimalValue)) return decimalValue;
        throw new JsonException("Unsupported number format");
    }

    public override void Write(
        Utf8JsonWriter writer,
        object[] value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
