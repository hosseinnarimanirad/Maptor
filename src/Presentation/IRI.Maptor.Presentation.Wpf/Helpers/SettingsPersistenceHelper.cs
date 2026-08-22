using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

/// <summary>
/// Serializes/deserializes only a whitelisted set of property names so shared settings types
/// can be persisted per application without separate DTO types.
/// </summary>
public static class SettingsPersistenceHelper
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    /// <summary>
    /// Serializes <paramref name="value"/> using its runtime type, emitting only properties in <paramref name="propertyNames"/>.
    /// </summary>
    public static string SerializeFiltered<T>(T value, IReadOnlyCollection<string> propertyNames)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(propertyNames);

        var node = JsonSerializer.SerializeToNode(value, value.GetType()) as JsonObject
                   ?? new JsonObject();

        var filtered = new JsonObject();
        foreach (var name in propertyNames)
        {
            if (node.TryGetPropertyValue(name, out var prop) && prop is not null)
                filtered[name] = prop.DeepClone();
        }

        return filtered.ToJsonString();
    }

    /// <summary>
    /// Deserializes JSON into a temporary instance, then copies only whitelisted properties onto <paramref name="target"/>.
    /// Non-listed properties on <paramref name="target"/> are unchanged.
    /// </summary>
    public static void DeserializeAndMerge<T>(string json, T target, IReadOnlyCollection<string> propertyNames)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(propertyNames);

        if (string.IsNullOrWhiteSpace(json))
            return;

        var partial = JsonSerializer.Deserialize<T>(json, DeserializeOptions);
        if (partial is null)
            return;

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        foreach (var name in propertyNames)
        {
            var prop = typeof(T).GetProperty(name, flags);
            if (prop?.CanRead != true || !prop.CanWrite)
                continue;

            prop.SetValue(target, prop.GetValue(partial));
        }
    }
}
