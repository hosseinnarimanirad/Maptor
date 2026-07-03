using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using IRI.Maptor.Sta.Ogc;

namespace IRI.Maptor.Jab.Common.Cartography.Legend;

/// <summary>
/// Turns an <see cref="OgcFilter"/> and a rule's scale range into short, human-readable captions
/// for a legend (e.g. "type = primary", "pop ≥ 1000", "1:1k–1:500k"). Best-effort: attribute
/// comparisons and And/Or/Not are rendered fully; spatial / temporal operators fall back to their
/// operator name.
/// </summary>
internal static class FilterTextFormatter
{
    /// <summary>Readable rendering of a filter, or null when there is no (renderable) filter.</summary>
    public static string? Format(OgcFilter? filter)
    {
        if (filter?.Predicate is null)
            return null;

        var text = Format(filter.Predicate);

        return string.IsNullOrWhiteSpace(text) ? null : text;
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

    private static string? Format(object? predicate)
    {
        switch (predicate)
        {
            case OgcPropertyIsEqualTo eq:
                return $"{eq.GetPropertyName()} = {eq.GetLiteral()}";

            case OgcPropertyIsNotEqualTo neq:
                return $"{neq.GetPropertyName()} ≠ {neq.GetLiteral()}";

            case OgcPropertyIsLessThan lt:
                return $"{lt.GetPropertyName()} < {FormatNumber(lt.GetLiteral())}";

            case OgcPropertyIsGreaterThan gt:
                return $"{gt.GetPropertyName()} > {FormatNumber(gt.GetLiteral())}";

            case OgcPropertyIsLessThanOrEqualTo le:
                return $"{le.GetPropertyName()} ≤ {FormatNumber(le.GetLiteral())}";

            case OgcPropertyIsGreaterThanOrEqualTo ge:
                return $"{ge.GetPropertyName()} ≥ {FormatNumber(ge.GetLiteral())}";

            case OgcPropertyIsLike like:
                return $"{like.GetPropertyName()} like {FirstLiteral(like.Expressions)}";

            case OgcPropertyIsBetween between:
                return $"{between.GetPropertyName()} in [{LiteralOf(between.LowerBoundary?.Expression)}, {LiteralOf(between.UpperBoundary?.Expression)}]";

            case OgcPropertyIsNull isNull:
                return $"{isNull.GetPropertyName()} is null";

            case OgcPropertyIsNil isNil:
                return $"{isNil.GetPropertyName()} is nil";

            case OgcAnd and:
                return JoinChildren(and.Predicates, " AND ");

            case OgcOr or:
                return JoinChildren(or.Predicates, " OR ");

            case OgcNot not:
                var inner = Format(not.Predicate);
                return string.IsNullOrWhiteSpace(inner) ? "NOT" : $"NOT ({inner})";

            // Spatial / temporal operators: no literal rendering yet — show the operator name.
            case OgcFilterBase other:
                return OperatorName(other);

            default:
                return null;
        }
    }

    private static string JoinChildren(IEnumerable<OgcFilterBase> children, string separator)
    {
        var parts = children?
            .Select(c => Format(c))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (parts is null || parts.Count == 0)
            return string.Empty;

        return string.Join(separator, parts.Select(p => parts.Count > 1 ? $"({p})" : p));
    }

    private static string? FirstLiteral(List<OgcExpression>? expressions)
        => (expressions?.FirstOrDefault(e => e is OgcLiteral) as OgcLiteral)?.Value;

    private static string? LiteralOf(OgcExpression? expression)
        => (expression as OgcLiteral)?.Value;

    private static string FormatNumber(double? value)
        => value.HasValue ? value.Value.ToString("0.###", CultureInfo.InvariantCulture) : string.Empty;

    private static string OperatorName(OgcFilterBase filter)
    {
        // Strip the "Ogc" prefix from the runtime type name (OgcIntersects -> Intersects).
        var name = filter.GetType().Name;
        return name.StartsWith("Ogc") ? name.Substring(3) : name;
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
