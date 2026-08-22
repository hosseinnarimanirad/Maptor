using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace IRI.Maptor.Core.Common.Common.JsonConverters;

public class DictionaryStringObjectConverter : JsonConverter<Dictionary<string, object>>
{
    public override Dictionary<string, object> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return ConvertJsonElementToDictionary(doc.RootElement);
    }

    private Dictionary<string, object> ConvertJsonElementToDictionary(JsonElement element)
    {
        var dictionary = new Dictionary<string, object>();
        foreach (var property in element.EnumerateObject())
        {
            dictionary[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => GetNumberValue(property.Value),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => ConvertJsonElementToList(property.Value),
                JsonValueKind.Object => ConvertJsonElementToDictionary(property.Value),
                _ => throw new JsonException()
            };
        }
        return dictionary;
    }

    private object GetNumberValue(JsonElement element)
    {
        if (element.TryGetInt32(out int intValue)) return intValue;
        if (element.TryGetInt64(out long longValue)) return longValue;
        if (element.TryGetDouble(out double doubleValue)) return doubleValue;
        if (element.TryGetDecimal(out decimal decimalValue)) return decimalValue;
        throw new JsonException("Unsupported number format");
    }

    private List<object> ConvertJsonElementToList(JsonElement element)
    {
        var list = new List<object>();
        foreach (var item in element.EnumerateArray())
        {
            list.Add(item.ValueKind switch
            {
                JsonValueKind.String => item.GetString(),
                JsonValueKind.Number => GetNumberValue(item),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => ConvertJsonElementToList(item),
                JsonValueKind.Object => ConvertJsonElementToDictionary(item),
                _ => throw new JsonException()
            });
        }
        return list;
    }

    public override void Write(
        Utf8JsonWriter writer,
        Dictionary<string, object> value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}