using System.Globalization;
using System.Text;
using System.Text.Json;

namespace IRI.Maptor.Ket.VersioningPersistence;

/// <summary>
/// Attribute dictionaries ⇄ canonical JSON: ordinally sorted keys, invariant culture,
/// ISO-8601 dates. Canonical form keeps stored proposal state diffable and byte-stable
/// for identical input.
/// </summary>
public static class CanonicalAttributeSerializer
{
    public static string Serialize(IReadOnlyDictionary<string, object?> attributes)
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            foreach (var pair in attributes.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                writer.WritePropertyName(pair.Key);
                WriteValue(writer, pair.Value);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static Dictionary<string, object?> Deserialize(string json)
    {
        var result = new Dictionary<string, object?>();

        using var document = JsonDocument.Parse(json);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            result[property.Name] = ReadValue(property.Value);
        }

        return result;
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case byte or sbyte or short or ushort or int or uint or long:
                writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                break;
            case ulong ul:
                writer.WriteNumberValue(ul);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case decimal m:
                writer.WriteNumberValue(m);
                break;
            // Unspecified kinds are written as-is: converting them would silently shift
            // values whose zone the source system never declared.
            case DateTime dt:
                writer.WriteStringValue(dt.Kind == DateTimeKind.Unspecified
                    ? dt.ToString("O", CultureInfo.InvariantCulture)
                    : dt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
                break;
            case DateTimeOffset dto:
                writer.WriteStringValue(dto.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
                break;
            case Guid g:
                writer.WriteStringValue(g);
                break;
            case byte[] bytes:
                writer.WriteBase64StringValue(bytes);
                break;
            case JsonElement element:
                element.WriteTo(writer);
                break;
            default:
                JsonSerializer.Serialize(writer, value);
                break;
        }
    }

    private static object? ReadValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var integral) ? integral : (object)element.GetDouble(),
        _ => element.Clone(),
    };
}
