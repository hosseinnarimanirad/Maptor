using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Dtos;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Presentation.Wpf.Models.LayerSettings;

/// <summary>
/// The server's statistics of a Live layer (<see cref="FieldStatisticsDto"/>, <see cref="GroupStatisticsDto"/>,
/// <see cref="GeometryStatisticsDto"/>) as the results the in-memory calculators give, so the Statistics tab shows
/// them the same way: values normalised as feature values are, the empty value as a row, the same order and the
/// same domain checks.
/// </summary>
public static class ServerStatistics
{
    public static FieldStatistics ToFieldStatistics(FieldStatisticsDto dto, Field field)
    {
        var kind = GetKind(field, dto);

        var frequencies = Normalize(dto.Values, dto.Counts, kind, out var invalidCount);

        var total = dto.FeatureCount;

        var domain = field.Domain;

        bool IsOutOfDomain(object? value) => domain is not null && value is not null && !domain.Contains(value);

        var topValues = frequencies
            .Select(f => (Value: (object?)f.Key, Count: f.Value))
            .Append((Value: null, Count: dto.EmptyCount))
            .Where(f => f.Count > 0)
            .OrderByDescending(f => f.Count)
            .ThenBy(f => f.Value?.ToString(), StringComparer.CurrentCulture)
            .Take(FieldStatistics.TopValuesLimit)
            .Select(f => new ValueFrequency { Value = f.Value, Count = f.Count, Percent = total == 0 ? 0 : 100.0 * f.Count / total, IsOutOfDomain = IsOutOfDomain(f.Value) })
            .ToList();

        // a numeric column read as categories (a domain field) keeps only the counts, as the calculator does
        var numbers = kind == FieldStatisticsKind.Numeric ? dto.Numbers : null;

        return new FieldStatistics
        {
            Kind = kind,
            FeatureCount = total,
            EmptyCount = dto.EmptyCount,
            InvalidCount = invalidCount,
            DistinctCount = dto.DistinctCount,
            MinDate = kind == FieldStatisticsKind.Date ? dto.MinDate : null,
            MaxDate = kind == FieldStatisticsKind.Date ? dto.MaxDate : null,
            TopValues = topValues,
            Domain = domain,
            // only the values the server sent: with more distinct values than its cap the rarest are not checked
            OutOfDomainCount = domain is null ? 0 : frequencies.Where(f => !domain.Contains(f.Key)).Sum(f => f.Value),
            UsedDomainValueCount = domain is null ? 0 : frequencies.Keys.Count(domain.Contains),
            Min = numbers?.Min,
            Max = numbers?.Max,
            Sum = numbers?.Sum,
            Mean = numbers?.Mean,
            Median = numbers?.Median,
            StandardDeviation = numbers?.StandardDeviation,
            Histogram = ToHistogram(numbers?.Histogram),
        };
    }

    public static GroupStatistics ToGroupStatistics(GroupStatisticsDto dto, Field groupField)
    {
        var kind = FieldStatistics.GetKind(groupField);

        var measureKind = ToMeasureKind(dto.MeasureKind);
        var hasMeasure = measureKind != GeometryMeasureKind.None;

        var total = dto.FeatureCount;

        var domain = groupField.Domain;

        GroupStatisticsRow ToRow(object? key, int count, double? measure, double? sum) => new()
        {
            Key = key,
            Count = count,
            Percent = total == 0 ? 0 : 100.0 * count / total,
            // the calculator starts a measured group at 0
            Measure = hasMeasure ? measure ?? 0 : null,
            Sum = sum,
            IsOutOfDomain = domain is not null && key is not null && !domain.Contains(key),
        };

        var rows = new List<GroupStatisticsRow>();
        var invalidCount = 0;

        for (int i = 0; i < dto.Keys.Length; i++)
        {
            if (!FieldStatistics.TryNormalize(ReadServerValue(dto.Keys[i], kind), kind, out var key) || key is null)
            {
                invalidCount += dto.Counts[i];
                continue;
            }

            rows.Add(ToRow(key, dto.Counts[i], At(dto.Measures, i), At(dto.Sums, i)));
        }

        if (dto.EmptyCount > 0)
            rows.Add(ToRow(null, dto.EmptyCount, dto.EmptyMeasure, dto.EmptySum));

        rows = rows
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.Key is null)
            .ThenBy(r => r.Key?.ToString(), StringComparer.CurrentCulture)
            .ToList();

        return new GroupStatistics
        {
            FeatureCount = total,
            MeasureKind = measureKind,
            TotalMeasure = hasMeasure ? dto.TotalMeasure ?? 0 : null,
            TotalSum = dto.TotalSum,
            InvalidCount = invalidCount,
            OutOfDomainCount = rows.Where(r => r.IsOutOfDomain).Sum(r => r.Count),
            Rows = rows,
            TotalGroupCount = dto.Truncated ? dto.GroupCount : null,
        };
    }

    public static GeometryStatistics ToGeometryStatistics(GeometryStatisticsDto dto)
    {
        var measureKind = ToMeasureKind(dto.MeasureKind);

        return new GeometryStatistics
        {
            FeatureCount = dto.FeatureCount,
            EmptyGeometryCount = dto.EmptyGeometryCount,
            MeasureKind = measureKind,
            UnmeasuredCount = dto.UnmeasuredCount,
            Measures = measureKind == GeometryMeasureKind.None ? null : ToNumericSummary(dto.Measures),
        };
    }

    /// <summary>The measure the server is asked for: <see cref="GeometryMeasureKinds"/>, null for none.</summary>
    public static string? ToServerMeasureKind(GeometryMeasureKind kind) => kind switch
    {
        GeometryMeasureKind.Length => GeometryMeasureKinds.Length,
        GeometryMeasureKind.Area => GeometryMeasureKinds.Area,
        _ => null,
    };

    private static GeometryMeasureKind ToMeasureKind(string? kind) => GeometryMeasureKinds.Normalize(kind) switch
    {
        GeometryMeasureKinds.Length => GeometryMeasureKind.Length,
        GeometryMeasureKinds.Area => GeometryMeasureKind.Area,
        _ => GeometryMeasureKind.None,
    };

    // the field's own kind; for a field of unknown type (an older server) the kind of what the server computed
    private static FieldStatisticsKind GetKind(Field field, FieldStatisticsDto dto)
    {
        if (field.ResolveDataType() != FieldDataType.Unknown || field.Domain is not null)
            return FieldStatistics.GetKind(field);

        return dto.Numbers is not null ? FieldStatisticsKind.Numeric
            : dto.MinDate is not null ? FieldStatisticsKind.Date
            : FieldStatisticsKind.Text;
    }

    // values as the calculator keys them; values that become equal (5 and 5.0) are merged
    private static Dictionary<object, int> Normalize(object?[] values, int[] counts, FieldStatisticsKind kind, out int invalidCount)
    {
        var result = new Dictionary<object, int>();
        invalidCount = 0;

        for (int i = 0; i < values.Length; i++)
        {
            if (!FieldStatistics.TryNormalize(ReadServerValue(values[i], kind), kind, out var key))
            {
                invalidCount += counts[i];
                continue;
            }

            if (key is null)
                continue;

            result[key] = result.TryGetValue(key, out var count) ? count + counts[i] : counts[i];
        }

        return result;
    }

    // a date travels as ISO text: read it with the invariant culture (the current one may be Persian, another calendar)
    private static object? ReadServerValue(object? value, FieldStatisticsKind kind)
    {
        if (kind == FieldStatisticsKind.Date && value is string text &&
            DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
        {
            return date;
        }

        return value;
    }

    private static double? At(double?[] values, int index) => index < values.Length ? values[index] : null;

    private static NumericSummary? ToNumericSummary(NumericSummaryDto? dto) => dto is null ? null : new NumericSummary
    {
        Count = dto.Count,
        Min = dto.Min,
        Max = dto.Max,
        Sum = dto.Sum,
        Mean = dto.Mean,
        Median = dto.Median,
        StandardDeviation = dto.StandardDeviation,
        Histogram = ToHistogram(dto.Histogram),
    };

    private static Histogram? ToHistogram(HistogramDto? dto)
        => dto is null || dto.Counts.Length == 0 ? null : new Histogram { Start = dto.Start, BinWidth = dto.BinWidth, Counts = dto.Counts };
}
