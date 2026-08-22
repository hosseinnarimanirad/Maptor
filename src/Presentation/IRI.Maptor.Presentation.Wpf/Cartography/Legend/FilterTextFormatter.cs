using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using IRI.Maptor.Core.Ogc;

namespace IRI.Maptor.Presentation.Wpf.Cartography.Legend;

/// <summary>
/// Turns an <see cref="OgcFilter"/> into the display label(s) of the field(s) it references
/// (alias when resolvable, raw name otherwise) and a rule's scale range into a short caption
/// (e.g. "1:1k–1:500k"). The rule's value itself is not repeated — the rule title carries it.
/// </summary>
internal static class FilterTextFormatter
{
    /// <summary>
    /// Display label(s) of the field(s) a filter references — "name (alias)" when the alias is
    /// resolvable, the raw field name otherwise — distinct, joined with " · "; null when no
    /// field is referenced.
    /// </summary>
    /// <param name="aliasResolver">Optional field-name → display-alias map; null (or a null result) keeps the raw name.</param>
    public static string? FormatFieldNames(OgcFilter? filter, Func<string, string?>? aliasResolver = null)
    {
        if (filter?.Predicate is null)
            return null;

        var names = new List<string>();
        CollectPropertyNames(filter.Predicate, names);

        var labels = names
            .Select(n => DisplayName(n, aliasResolver))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();

        return labels.Count == 0 ? null : string.Join(" · ", labels);
    }

    /// <summary>Readable rendering of a scale range, or null when unbounded.</summary>
    public static string? FormatScale(double? minScaleDenominator, double? maxScaleDenominator)
    {
        bool hasMin = minScaleDenominator.HasValue && minScaleDenominator.Value > 0;
        bool hasMax = maxScaleDenominator.HasValue && maxScaleDenominator.Value > 0 && maxScaleDenominator.Value < double.MaxValue;

        if (hasMin && hasMax)
            return $"1:{Abbreviate(minScaleDenominator!.Value)}–1:{Abbreviate(maxScaleDenominator!.Value)}";

        if (hasMin)
            return $"≥ 1:{Abbreviate(minScaleDenominator!.Value)}";

        if (hasMax)
            return $"≤ 1:{Abbreviate(maxScaleDenominator!.Value)}";

        return null;
    }

    private static void CollectPropertyNames(object? predicate, List<string> names)
    {
        switch (predicate)
        {
            case OgcComparisonOperator comparison:
                var name = comparison.GetPropertyName();
                if (!string.IsNullOrWhiteSpace(name))
                    names.Add(name!);
                break;

            case OgcAnd andFilter when andFilter.Predicates is not null:
                foreach (var child in andFilter.Predicates)
                    CollectPropertyNames(child, names);
                break;

            case OgcOr orFilter when orFilter.Predicates is not null:
                foreach (var child in orFilter.Predicates)
                    CollectPropertyNames(child, names);
                break;

            case OgcNot not:
                CollectPropertyNames(not.Predicate, names);
                break;
        }
    }

    /// <summary>"name (alias)" when an alias is resolvable, otherwise the raw field name.</summary>
    private static string? DisplayName(string? propertyName, Func<string, string?>? aliasResolver)
    {
        if (propertyName is null || aliasResolver is null)
            return propertyName;

        var alias = aliasResolver(propertyName);

        return string.IsNullOrWhiteSpace(alias) ? propertyName : $"{propertyName} ({alias})";
    }

    private static string Abbreviate(double value)
    {
        if (value >= 1_000_000)
            return Trim(value / 1_000_000) + "M";

        if (value >= 1_000)
            return Trim(value / 1_000) + "k";

        return Trim(value);
    }

    private static string Trim(double value)
        => value.ToString("0.##", CultureInfo.InvariantCulture);
}
