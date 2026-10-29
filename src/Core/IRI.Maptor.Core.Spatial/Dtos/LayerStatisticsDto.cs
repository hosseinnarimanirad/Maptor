using System;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Spatial.Dtos;

// Statistics of a whole layer computed by the database (GET /LayerQuery/{layerId}/Statistics/...), for a Live layer
// whose features stay on the server. They mirror the client's in-memory calculators (the layer settings dialog's
// Statistics tab), which turn them into the same results. Unsaved edits on the client are not included.

/// <summary>The measure of a layer's geometries: <see cref="Length"/> (metres) or <see cref="Area"/> (square metres), geodesic.</summary>
public static class GeometryMeasureKinds
{
    public const string Length = "Length";

    public const string Area = "Area";

    /// <summary><see cref="Length"/> or <see cref="Area"/> (any case); null for anything else.</summary>
    public static string? Normalize(string? kind)
        => string.Equals(kind, Length, StringComparison.OrdinalIgnoreCase) ? Length
         : string.Equals(kind, Area, StringComparison.OrdinalIgnoreCase) ? Area
         : null;
}

/// <summary>Equal-width bins (see <see cref="Analysis.HistogramLayout"/>).</summary>
public class HistogramDto
{
    public double Start { get; set; }

    public double BinWidth { get; set; }

    public int[] Counts { get; set; } = Array.Empty<int>();
}

/// <summary>Min, max, sum, mean, median (exact), sample standard deviation and histogram of a set of numbers.</summary>
public class NumericSummaryDto
{
    public int Count { get; set; }

    public double Min { get; set; }

    public double Max { get; set; }

    public double Sum { get; set; }

    public double Mean { get; set; }

    public double Median { get; set; }

    /// <summary>Null below two values.</summary>
    public double? StandardDeviation { get; set; }

    public HistogramDto? Histogram { get; set; }
}

/// <summary>The statistics of one attribute over the whole layer.</summary>
public class FieldStatisticsDto
{
    public int FeatureCount { get; set; }

    /// <summary>Features whose value is null or, for text, blank.</summary>
    public int EmptyCount { get; set; }

    /// <summary>Distinct non-empty values (the database's comparison: SQL Server's default collation ignores case).</summary>
    public int DistinctCount { get; set; }

    /// <summary>Set for a numeric column with at least one value.</summary>
    public NumericSummaryDto? Numbers { get; set; }

    /// <summary>Set for a date column with at least one value.</summary>
    public DateTime? MinDate { get; set; }

    public DateTime? MaxDate { get; set; }

    /// <summary>The non-empty values, most frequent first, aligned with <see cref="Counts"/>; capped (see <see cref="Truncated"/>).</summary>
    [JsonConverter(typeof(IRI.Maptor.Core.Common.Common.JsonConverters.ObjectArrayConverter))]
    public object?[] Values { get; set; } = Array.Empty<object?>();

    public int[] Counts { get; set; } = Array.Empty<int>();

    /// <summary>More distinct values exist than <see cref="Values"/> holds.</summary>
    public bool Truncated { get; set; }
}

/// <summary>Features grouped by the values of one field: per group the count, the total measure and the total of a numeric field.</summary>
public class GroupStatisticsDto
{
    public int FeatureCount { get; set; }

    /// <summary><see cref="GeometryMeasureKinds"/>; null when no measure was asked for.</summary>
    public string? MeasureKind { get; set; }

    /// <summary>The non-empty group values, largest group first, aligned with the arrays below; capped (see <see cref="Truncated"/>).</summary>
    [JsonConverter(typeof(IRI.Maptor.Core.Common.Common.JsonConverters.ObjectArrayConverter))]
    public object?[] Keys { get; set; } = Array.Empty<object?>();

    public int[] Counts { get; set; } = Array.Empty<int>();

    /// <summary>Total length (m) or area (m²) per group; null without a measure or when nothing was measured.</summary>
    public double?[] Measures { get; set; } = Array.Empty<double?>();

    /// <summary>Total of the sum field per group; null without a sum field or when no feature has a value.</summary>
    public double?[] Sums { get; set; } = Array.Empty<double?>();

    /// <summary>The group of the features with an empty value.</summary>
    public int EmptyCount { get; set; }

    public double? EmptyMeasure { get; set; }

    public double? EmptySum { get; set; }

    /// <summary>Over all features, also those of the groups left out by the cap.</summary>
    public double? TotalMeasure { get; set; }

    public double? TotalSum { get; set; }

    /// <summary>All groups, the empty one included.</summary>
    public int GroupCount { get; set; }

    public bool Truncated { get; set; }
}

/// <summary>The geometries of the layer: how many are empty, and the per-feature length or area summarised.</summary>
public class GeometryStatisticsDto
{
    public int FeatureCount { get; set; }

    public int EmptyGeometryCount { get; set; }

    /// <summary><see cref="GeometryMeasureKinds"/>; null when no measure was asked for.</summary>
    public string? MeasureKind { get; set; }

    /// <summary>Non-empty geometries of another type than the measure's.</summary>
    public int UnmeasuredCount { get; set; }

    public NumericSummaryDto? Measures { get; set; }
}
