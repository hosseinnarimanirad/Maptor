using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Wpf.Models.LayerSettings;

/// <summary>One value of the group-by field and the totals of its features.</summary>
public class GroupStatisticsRow
{
    /// <summary>The group's value (as <see cref="FieldStatistics.TryGetKey"/> returns it); null groups the empty features.</summary>
    public object? Key { get; init; }

    public int Count { get; init; }

    /// <summary>Share of all features, 0..100.</summary>
    public double Percent { get; init; }

    /// <summary>Total length (m) or area (m²) of the group; null when the layer has no measure.</summary>
    public double? Measure { get; init; }

    /// <summary>Total of the sum field over the group; null without a sum field or when no feature has a value.</summary>
    public double? Sum { get; init; }

    /// <summary>The group-by field has a domain and this (non-empty) value is not one of its values.</summary>
    public bool IsOutOfDomain { get; init; }
}

/// <summary>
/// Features grouped by the values of one field: per value the feature count, the total geodesic length or
/// area, and optionally the total of a numeric field. Rows are ordered by count, largest first.
/// </summary>
public class GroupStatistics
{
    public int FeatureCount { get; init; }

    public GeometryMeasureKind MeasureKind { get; init; }

    public double? TotalMeasure { get; init; }

    public double? TotalSum { get; init; }

    /// <summary>Features whose value could not be read as the group-by field's type; they are in no group.</summary>
    public int InvalidCount { get; init; }

    /// <summary>With a domain on the group-by field: features whose non-empty value is not in it.</summary>
    public int OutOfDomainCount { get; init; }

    public IReadOnlyList<GroupStatisticsRow> Rows { get; init; } = [];

    /// <summary>All groups: more than <see cref="Rows"/> when the server sent only the largest ones.</summary>
    public int GroupCount => TotalGroupCount ?? Rows.Count;

    /// <summary>Set from the server's statistics, which cap the rows; null when every group is in <see cref="Rows"/>.</summary>
    public int? TotalGroupCount { get; init; }

    /// <summary>
    /// <paramref name="measures"/> holds the per-feature length or area, aligned with the features (see
    /// <see cref="GeometryMeasure.Calculate(IReadOnlyList{Feature{Point}}, GeometryMeasureKind, CancellationToken)"/>);
    /// <paramref name="sumField"/> is an optional numeric field to total per group. CPU-bound: call it off the UI thread.
    /// </summary>
    public static GroupStatistics Calculate(
        IReadOnlyList<Feature<Point>> features,
        Field groupField,
        GeometryMeasureKind measureKind,
        IReadOnlyList<double?>? measures,
        Field? sumField,
        CancellationToken cancellationToken = default)
    {
        if (measures is not null && measures.Count != features.Count)
            throw new ArgumentException("One measure per feature is expected.", nameof(measures));

        var groupKind = FieldStatistics.ResolveKind(groupField, features);

        var groups = new Dictionary<object, Accumulator>();
        var emptyGroup = new Accumulator();
        int invalidCount = 0;

        bool hasMeasure = measureKind != GeometryMeasureKind.None && measures is not null;

        for (int i = 0; i < features.Count; i++)
        {
            if (i % 10_000 == 0)
                cancellationToken.ThrowIfCancellationRequested();

            if (!FieldStatistics.TryGetKey(features[i], groupField, groupKind, out var key))
            {
                invalidCount++;
                continue;
            }

            Accumulator group;

            if (key is null)
            {
                group = emptyGroup;
            }
            else if (!groups.TryGetValue(key, out group!))
            {
                group = new Accumulator();
                groups.Add(key, group);
            }

            group.Count++;

            if (hasMeasure && measures![i] is double measure)
                group.Measure += measure;

            if (sumField is not null &&
                FieldStatistics.TryGetKey(features[i], sumField, FieldStatisticsKind.Numeric, out var number) &&
                number is double value)
            {
                group.Sum = (group.Sum ?? 0) + value;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        var total = features.Count;

        var domain = groupField.Domain;

        GroupStatisticsRow ToRow(object? key, Accumulator group) => new()
        {
            Key = key,
            Count = group.Count,
            Percent = total == 0 ? 0 : 100.0 * group.Count / total,
            Measure = hasMeasure ? group.Measure : null,
            Sum = group.Sum,
            IsOutOfDomain = domain is not null && key is not null && !domain.Contains(key),
        };

        var rows = groups
            .Select(g => ToRow(g.Key, g.Value))
            .Append(ToRow(null, emptyGroup))
            .Where(r => r.Count > 0)
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.Key is null)
            .ThenBy(r => r.Key?.ToString(), StringComparer.CurrentCulture)
            .ToList();

        var sums = rows.Where(r => r.Sum.HasValue).Select(r => r.Sum!.Value).ToList();

        return new GroupStatistics
        {
            FeatureCount = total,
            MeasureKind = hasMeasure ? measureKind : GeometryMeasureKind.None,
            TotalMeasure = hasMeasure ? rows.Sum(r => r.Measure ?? 0) : null,
            TotalSum = sums.Count > 0 ? sums.Sum() : null,
            InvalidCount = invalidCount,
            OutOfDomainCount = rows.Where(r => r.IsOutOfDomain).Sum(r => r.Count),
            Rows = rows,
        };
    }

    private class Accumulator
    {
        public int Count;

        public double Measure;

        public double? Sum;
    }
}
