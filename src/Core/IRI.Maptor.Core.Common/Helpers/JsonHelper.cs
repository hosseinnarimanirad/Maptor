using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Common.Helpers;

public static class JsonHelper
{

    // Single, reusable options instance with all necessary settings
    public static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
        WriteIndented = false,
        // default; can be overridden per call
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Servers configured with JsonStringEnumConverter send enums as names (e.g. "Local");
        // this reads both names and numbers, so responses parse either way.
        Converters = { new JsonStringEnumConverter() }
    };


    static readonly JsonSerializerOptions _ignoreNullValue;

    static JsonHelper()
    {
        _ignoreNullValue = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public static JsonSerializerOptions IgnoreNullValue { get => _ignoreNullValue; }

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value);
    }

    public static string Serialize<T>(T value, bool indented)
    {
        return JsonSerializer.Serialize(
                value,
                new JsonSerializerOptions
                {
                    WriteIndented = indented, // true for pretty-printed, false for compact
                });
    }


    public static string Serialize<T>(T value, JsonSerializerOptions options)
    {
        return JsonSerializer.Serialize(value, options);
    }

    public static string SerializeWithIgnoreNullOption<T>(T value)
    {
        return JsonSerializer.Serialize(value, _ignoreNullValue);
    }

    public static T? Deserialize<T>(string jsonString)
    {
        return JsonSerializer.Deserialize<T>(jsonString, DefaultOptions);
    }

    public static T? Deserialize<T>(string jsonString, JsonSerializerOptions options) => JsonSerializer.Deserialize<T>(jsonString, options);

    public static T? Deserialize<T>(string jsonString, JsonConverter converter)
    {
        return JsonSerializer.Deserialize<T>(jsonString, new JsonSerializerOptions()
        {
            Converters = { converter },
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
        });
    }

    /// <summary>
    /// Deserializes a JsonNode to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="jsonNode">The JsonNode to deserialize.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    public static T? Deserialize<T>(JsonNode? jsonNode)
    {
        if (jsonNode == null)
            return default;

        var options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        return JsonSerializer.Deserialize<T>(jsonNode, options);
    }

    /// <summary>
    /// Deserializes a JsonNode to the specified type with a custom converter.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="jsonNode">The JsonNode to deserialize.</param>
    /// <param name="converter">The custom JsonConverter to use.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    public static T? Deserialize<T>(JsonNode? jsonNode, JsonConverter converter)
    {
        if (jsonNode == null)
            return default;

        var options = new JsonSerializerOptions()
        {
            Converters = { converter },
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        return JsonSerializer.Deserialize<T>(jsonNode, options);
    }

}
