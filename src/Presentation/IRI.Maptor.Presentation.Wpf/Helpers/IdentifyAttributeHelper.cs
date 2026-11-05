using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf.Converters;
using IRI.Maptor.Presentation.Wpf.Models.Identify;
using IRI.Maptor.Core.Common.Enums;
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
    /// The feature's label, the first non-empty one of:
    /// <list type="number">
    /// <item>the layer's label column (<paramref name="labelColumn"/>: what the administrator set in the layer settings),</item>
    /// <item>the field the data model marks <see cref="Field.IsDisplayField"/>,</item>
    /// <item><see cref="Feature{T}.Label"/> (sources that name their own label attribute: shapefile, KML, ...),</item>
    /// <item>the first non-empty string attribute that <see cref="BuildRows"/> would show (schema order; any attribute when there is no schema).</item>
    /// </list>
    /// Empty when none has a value; the view puts the feature's id in front of it, so a row is never blank.
    /// </summary>
    public static string ResolveTitle(Feature<Point> feature, IReadOnlyList<Field>? fields, string? labelColumn = null)
    {
        if (feature is null)
            return string.Empty;

        var attributes = feature.Attributes;

        if (TryGetText(attributes, labelColumn, fields, out var labelText))
            return labelText;

        if (TryGetText(attributes, fields?.FirstOrDefault(f => f?.IsDisplayField == true)?.Name, fields, out var displayText))
            return displayText;

        if (!string.IsNullOrWhiteSpace(feature.Label))
            return feature.Label.Trim();

        if (attributes is not null && attributes.Count > 0)
        {
            var hasSchema = HasSchema(fields);

            foreach (var key in OrderedKeys(attributes, fields))
            {
                if (attributes[key] is not string text || string.IsNullOrWhiteSpace(text))
                    continue;

                var field = FindField(fields, key);

                // same rule as BuildRows: with a schema only its displayable fields, otherwise anything
                if (field is null ? !hasSchema : FeatureTableHelper.IsDisplayableField(field))
                    return text.Trim();
            }
        }

        return string.Empty;
    }

    /// <summary>The named attribute as display text (numbers and dates formatted as in the grid); false when it is absent or blank.</summary>
    private static bool TryGetText(Dictionary<string, object>? attributes, string? name, IReadOnlyList<Field>? fields, out string text)
    {
        text = string.Empty;

        if (attributes is null || string.IsNullOrWhiteSpace(name))
            return false;

        var value = TryGetValue(attributes, name);

        if (value is null || value is DBNull)
            return false;

        text = FormatValue(value, FindField(fields, name)).Trim();

        return text.Length > 0;
    }

    /// <summary>
    /// With a schema: its displayable fields (<see cref="FeatureTableHelper.IsDisplayableField"/>)
    /// in schema order, and nothing else, which is the attribute table's rule. Attributes outside the
    /// schema are not shown: <see cref="Field.GetFields{T}"/> leaves <c>[Field(CanRead = false)]</c>
    /// properties (RowVersion, CreatedById, ...) out of the schema entirely, so they would otherwise
    /// come back here. Without a schema every attribute is listed.
    /// </summary>
    public static IReadOnlyList<IdentifyAttributeRow> BuildRows(Feature<Point> feature, IReadOnlyList<Field>? fields)
    {
        var rows = new List<IdentifyAttributeRow>();

        var attributes = feature?.Attributes ?? new Dictionary<string, object>();

        if (HasSchema(fields))
        {
            foreach (var field in fields!)
            {
                if (!FeatureTableHelper.IsDisplayableField(field))
                    continue;

                rows.Add(CreateRow(field.Name, field, TryGetValue(attributes, field.Name)));
            }

            return rows;
        }

        foreach (var pair in attributes)
            rows.Add(CreateRow(pair.Key, null, pair.Value));

        return rows;
    }

    private static bool HasSchema(IReadOnlyList<Field>? fields) =>
        fields is not null && fields.Any(f => !string.IsNullOrWhiteSpace(f?.Name));

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
        var dataType = field?.ResolveDataType() ?? FieldDataType.Unknown;

        // a field that does not say: the value does
        if (dataType == FieldDataType.Unknown)
            dataType = FieldDataTypes.FromClrType(value?.GetType());

        var isNumeric = dataType.IsNumeric();

        var isDateTime = dataType == FieldDataType.DateTime || value is DateTime || value is DateTimeOffset;

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
