using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf.Converters;
using IRI.Maptor.Presentation.Wpf.Models.Identify;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

/// <summary>
/// Turns a feature's attribute dictionary into display rows for the identify results view,
/// and picks the human-readable title of a feature node.
/// Formatting rules intentionally match the attribute table (<c>DataGridDictionaryBehavior</c>).
/// </summary>
public static class IdentifyAttributeHelper
{
    /// <summary>Shown for null / DBNull values (en dash).</summary>
    public const string NullDisplayText = "–";

    /// <summary>
    /// Grouped thousands, up to four decimals. The attribute table uses <c>"0,0.####"</c>,
    /// whose two forced digit placeholders render 1 as "01"; the leading <c>#</c> here keeps the
    /// grouping without that artefact.
    /// </summary>
    public const string DefaultNumericFormat = "#,0.####";

    /// <summary>
    /// Title fallback chain: <see cref="Feature{T}.Label"/> → first non-empty string attribute
    /// (schema order first, then any extra attribute) → <c>#Id</c>.
    /// </summary>
    public static string ResolveTitle(Feature<Point> feature, IReadOnlyList<Field>? fields)
    {
        if (feature is null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(feature.Label))
            return feature.Label.Trim();

        var attributes = feature.Attributes;

        if (attributes is not null && attributes.Count > 0)
        {
            foreach (var key in OrderedKeys(attributes, fields))
            {
                if (attributes[key] is not string text || string.IsNullOrWhiteSpace(text))
                    continue;

                var field = FindField(fields, key);

                // an attribute outside the schema has no visibility rule, so it is allowed
                if (field is null || FeatureTableHelper.IsDisplayableField(field))
                    return text.Trim();
            }
        }

        return $"#{feature.Id}";
    }

    /// <summary>
    /// Rows in schema order; fields hidden by <see cref="FeatureTableHelper.IsDisplayableField"/>
    /// are dropped, and attributes the feature carries but the schema does not know are appended
    /// at the end so nothing is silently lost.
    /// </summary>
    public static IReadOnlyList<IdentifyAttributeRow> BuildRows(Feature<Point> feature, IReadOnlyList<Field>? fields)
    {
        var rows = new List<IdentifyAttributeRow>();

        var attributes = feature?.Attributes ?? new Dictionary<string, object>();

        var consumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (fields is not null)
        {
            foreach (var field in fields)
            {
                if (field is null || string.IsNullOrWhiteSpace(field.Name))
                    continue;

                // a hidden schema field must not resurface as a "schemaless" extra below
                consumed.Add(field.Name);

                if (!FeatureTableHelper.IsDisplayableField(field))
                    continue;

                rows.Add(CreateRow(field.Name, field, TryGetValue(attributes, field.Name)));
            }
        }

        foreach (var pair in attributes)
        {
            if (consumed.Contains(pair.Key))
                continue;

            rows.Add(CreateRow(pair.Key, null, pair.Value));
        }

        return rows;
    }

    public static string FormatValue(object? value, Field? field)
    {
        if (value is null || value is DBNull)
            return NullDisplayText;

        switch (value)
        {
            case string text:
                return text;

            case DateTime or DateTimeOffset:
                var converter = new LocalizedDateTimeConverter { Format = field?.DisplayFormat };
                return converter.Convert(value, typeof(string), null!, CultureInfo.CurrentCulture)?.ToString() ?? string.Empty;

            case bool flag:
                return flag.ToString();

            case byte[] bytes:
                return $"[{bytes.Length} bytes]";
        }

        if (value.GetType().IsNumeric() && value is IFormattable formattable)
            return formattable.ToString(field?.DisplayFormat ?? DefaultNumericFormat, CultureInfo.CurrentCulture);

        return value.ToString() ?? string.Empty;
    }

    private static IdentifyAttributeRow CreateRow(string name, Field? field, object? value)
    {
        Type? type = null;

        if (field is not null && !string.IsNullOrWhiteSpace(field.TypeFullName))
            type = Type.GetType(field.TypeFullName);

        type ??= value?.GetType();

        var isNumeric = type?.IsNumeric() == true;

        var isDateTime = type?.IsDateTime() == true || value is DateTime || value is DateTimeOffset;

        var displayName = string.IsNullOrWhiteSpace(field?.Alias) ? name : field!.Alias!;

        return new IdentifyAttributeRow(name, displayName, value, FormatValue(value, field), field, isNumeric, isDateTime);
    }

    private static IEnumerable<string> OrderedKeys(Dictionary<string, object> attributes, IReadOnlyList<Field>? fields)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (fields is not null)
        {
            foreach (var field in fields)
            {
                if (field?.Name is null)
                    continue;

                var key = attributes.Keys.FirstOrDefault(k => string.Equals(k, field.Name, StringComparison.OrdinalIgnoreCase));

                if (key is not null && seen.Add(key))
                    yield return key;
            }
        }

        foreach (var key in attributes.Keys)
        {
            if (seen.Add(key))
                yield return key;
        }
    }

    private static Field? FindField(IReadOnlyList<Field>? fields, string name)
    {
        return fields?.FirstOrDefault(f => string.Equals(f?.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static object? TryGetValue(Dictionary<string, object> attributes, string name)
    {
        if (attributes.TryGetValue(name, out var exact))
            return exact;

        var key = attributes.Keys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));

        return key is null ? null : attributes[key];
    }
}
