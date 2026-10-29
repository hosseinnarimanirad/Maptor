using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf.Models.Filters;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Wpf.Models.LayerSettings;

public enum FieldStatisticsKind
{
    Text,
    Numeric,
    Date,
}

/// <summary>One value of a field and how many features hold it (the most frequent values).</summary>
public class ValueFrequency
{
    /// <summary>The raw value; null stands for the empty (null or blank) features.</summary>
    public object? Value { get; init; }

    public int Count { get; init; }

    /// <summary>Share of all features, 0..100.</summary>
    public double Percent { get; init; }

    /// <summary>The field has a domain and this (non-empty) value is not one of its values.</summary>
    public bool IsOutOfDomain { get; init; }
}

/// <summary>
/// The statistics of one attribute over a set of features. Counts apply to every kind; the numeric
/// members are set for <see cref="FieldStatisticsKind.Numeric"/> and the date members for
/// <see cref="FieldStatisticsKind.Date"/> (null when the field has no usable value).
/// </summary>
public class FieldStatistics
{
    public const int TopValuesLimit = 10;

    public FieldStatisticsKind Kind { get; init; }

    public int FeatureCount { get; init; }

    /// <summary>Features whose value is null, DBNull or a blank string.</summary>
    public int EmptyCount { get; init; }

    /// <summary>Non-empty values that could not be read as the field's type (e.g. text in a numeric field).</summary>
    public int InvalidCount { get; init; }

    public int DistinctCount { get; init; }

    public double? Min { get; init; }

    public double? Max { get; init; }

    public double? Sum { get; init; }

    public double? Mean { get; init; }

    public double? Median { get; init; }

    /// <summary>Sample standard deviation; null below two values.</summary>
    public double? StandardDeviation { get; init; }

    /// <summary>Distribution of a numeric field; null for other kinds or no values.</summary>
    public Histogram? Histogram { get; init; }

    public DateTime? MinDate { get; init; }

    public DateTime? MaxDate { get; init; }

    public IReadOnlyList<ValueFrequency> TopValues { get; init; } = [];

    /// <summary>The field's domain (coded values or an allowed-values list); null for a free field.</summary>
    public FieldDomain? Domain { get; init; }

    /// <summary>With a domain: features whose non-empty value is not in it (data that skipped validation, e.g. an import).</summary>
    public int OutOfDomainCount { get; init; }

    /// <summary>With a domain: how many of its values occur at least once.</summary>
    public int UsedDomainValueCount { get; init; }

    public static FieldStatisticsKind GetKind(Field field)
    {
        var dataType = field.ResolveDataType();

        // a domain field is a set of categories: frequencies say more than a mean
        if (field.Domain is not null)
            return FieldStatisticsKind.Text;

        if (dataType == FieldDataType.DateTime)
            return FieldStatisticsKind.Date;

        if (dataType.IsNumeric())
            return FieldStatisticsKind.Numeric;

        return FieldStatisticsKind.Text;
    }

    /// <summary>
    /// <see cref="GetKind(Field)"/>, or, when the field's type is unknown (an older server, or a source that typed
    /// its fields from the first feature, whose value was empty), the kind of the values themselves: numeric
    /// when every non-empty value is a CLR number, date when every one is a date.
    /// </summary>
    public static FieldStatisticsKind ResolveKind(Field field, IReadOnlyList<Feature<Point>> features)
    {
        if (field.ResolveDataType() != FieldDataType.Unknown)
            return GetKind(field);

        if (field.Domain is not null)
            return FieldStatisticsKind.Text;

        bool allNumbers = true, allDates = true, any = false;

        foreach (var feature in features)
        {
            object? value = null;
            feature.Attributes?.TryGetValue(field.Name, out value);

            if (IsEmpty(value))
                continue;

            any = true;
            allNumbers &= value is not bool && value!.GetType().IsNumeric();
            allDates &= value is DateTime or DateTimeOffset;

            if (!allNumbers && !allDates)
                return FieldStatisticsKind.Text;
        }

        return !any ? FieldStatisticsKind.Text : allNumbers ? FieldStatisticsKind.Numeric : FieldStatisticsKind.Date;
    }

    /// <summary>CPU-bound over all the features: call it off the UI thread.</summary>
    public static FieldStatistics Calculate(IReadOnlyList<Feature<Point>> features, Field field, CancellationToken cancellationToken = default)
    {
        var kind = ResolveKind(field, features);

        var frequencies = new Dictionary<object, int>();
        var numbers = new List<double>();
        DateTime? minDate = null, maxDate = null;
        int emptyCount = 0, invalidCount = 0;

        for (int i = 0; i < features.Count; i++)
        {
            if (i % 10_000 == 0)
                cancellationToken.ThrowIfCancellationRequested();

            if (!TryGetKey(features[i], field, kind, out var value))
            {
                invalidCount++;
                continue;
            }

            if (value is null)
            {
                emptyCount++;
                continue;
            }

            if (value is double number)
            {
                numbers.Add(number);
            }
            else if (value is DateTime date)
            {
                minDate = minDate is null || date < minDate ? date : minDate;
                maxDate = maxDate is null || date > maxDate ? date : maxDate;
            }

            frequencies[value] = frequencies.TryGetValue(value, out var count) ? count + 1 : 1;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var total = features.Count;

        var domain = field.Domain;

        bool IsOutOfDomain(object? value) => domain is not null && value is not null && !domain.Contains(value);

        var topValues = frequencies
            .Select(f => (Value: (object?)f.Key, Count: f.Value))
            .Append((Value: null, Count: emptyCount))
            .Where(f => f.Count > 0)
            .OrderByDescending(f => f.Count)
            .ThenBy(f => f.Value?.ToString(), StringComparer.CurrentCulture)
            .Take(TopValuesLimit)
            .Select(f => new ValueFrequency { Value = f.Value, Count = f.Count, Percent = total == 0 ? 0 : 100.0 * f.Count / total, IsOutOfDomain = IsOutOfDomain(f.Value) })
            .ToList();

        var summary = NumericSummary.Calculate(numbers);

        return new FieldStatistics
        {
            Kind = kind,
            FeatureCount = total,
            EmptyCount = emptyCount,
            InvalidCount = invalidCount,
            DistinctCount = frequencies.Count,
            MinDate = minDate,
            MaxDate = maxDate,
            TopValues = topValues,
            Domain = domain,
            OutOfDomainCount = domain is null ? 0 : frequencies.Where(f => !domain.Contains(f.Key)).Sum(f => f.Value),
            UsedDomainValueCount = domain is null ? 0 : frequencies.Keys.Count(domain.Contains),
            Min = summary?.Min,
            Max = summary?.Max,
            Sum = summary?.Sum,
            Mean = summary?.Mean,
            Median = summary?.Median,
            StandardDeviation = summary?.StandardDeviation,
            Histogram = summary?.Histogram,
        };
    }

    /// <summary>
    /// The feature's value of <paramref name="field"/> as a comparable key: a double for a numeric field (int 5
    /// and long 5 are one value), a DateTime for a date field, the raw value otherwise; null when empty.
    /// False when the value cannot be read as the field's type.
    /// </summary>
    internal static bool TryGetKey(Feature<Point> feature, Field field, FieldStatisticsKind kind, out object? key)
    {
        object? value = null;
        feature.Attributes?.TryGetValue(field.Name, out value);

        return TryNormalize(value, kind, out key);
    }

    /// <summary>
    /// <paramref name="value"/> as the key <see cref="TryGetKey"/> gives (a value from the server's statistics goes through
    /// the same normalisation: a date arrives as text, an integer as int or long).
    /// </summary>
    internal static bool TryNormalize(object? value, FieldStatisticsKind kind, out object? key)
    {
        key = null;

        if (IsEmpty(value))
            return true;

        switch (kind)
        {
            case FieldStatisticsKind.Numeric:
                if (!NumericColumnFilter.TryToDouble(value, out var number) || double.IsNaN(number))
                    return false;

                key = number;
                return true;

            case FieldStatisticsKind.Date:
                if (!DateColumnFilter.TryGetDateTime(value, out var date))
                    return false;

                key = date;
                return true;

            default:
                key = value;
                return true;
        }
    }

    private static bool IsEmpty(object? value) => value is null || value is DBNull || (value is string text && string.IsNullOrWhiteSpace(text));
}
