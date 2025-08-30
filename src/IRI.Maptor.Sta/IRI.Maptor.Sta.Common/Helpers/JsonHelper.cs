using System.Text.Json;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Common.Helpers;

public static class JsonHelper
{
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

    public static string SerializeWithIgnoreNullOption<T>(T value)
    {
        return JsonSerializer.Serialize(value, _ignoreNullValue);
    }

    public static T? Deserialize<T>(string jsonString)
    {
        return JsonSerializer.Deserialize<T>(jsonString, new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
        });
    }

    public static T? Deserialize<T>(string jsonString, JsonConverter converter)
    {
        return JsonSerializer.Deserialize<T>(jsonString, new JsonSerializerOptions()
        {
            Converters = { converter },
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
        });
    }

}
