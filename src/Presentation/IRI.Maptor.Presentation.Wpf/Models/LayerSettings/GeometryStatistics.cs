using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Wpf.Models.LayerSettings;

/// <summary>
/// The geometries of a layer: how many are empty, and for line/polygon layers the per-feature geodesic length
/// or area summarised (total, min, max, mean, median, standard deviation, histogram).
/// </summary>
public class GeometryStatistics
{
    public int FeatureCount { get; init; }

    /// <summary>Features without a geometry, or with an empty one.</summary>
    public int EmptyGeometryCount { get; init; }

    public GeometryMeasureKind MeasureKind { get; init; }

    /// <summary>Non-empty geometries that got no measure (another geometry type, or an SRID that cannot be brought to WGS84).</summary>
    public int UnmeasuredCount { get; init; }

    /// <summary>Length (m) or area (m²) per feature; null without a measure kind or when nothing was measured.</summary>
    public NumericSummary? Measures { get; init; }

    /// <param name="measures">Per-feature measure aligned with <paramref name="features"/>
    /// (<see cref="GeometryMeasure.Calculate(IReadOnlyList{Feature{Point}}, GeometryMeasureKind, System.Threading.CancellationToken)"/>);
    /// null when <paramref name="measureKind"/> is <see cref="GeometryMeasureKind.None"/>.</param>
    public static GeometryStatistics Calculate(IReadOnlyList<Feature<Point>> features, GeometryMeasureKind measureKind, IReadOnlyList<double?>? measures)
    {
        if (measures is not null && measures.Count != features.Count)
            throw new ArgumentException("One measure per feature is expected.", nameof(measures));

        int emptyCount = 0, unmeasuredCount = 0;
        var values = new List<double>(features.Count);

        bool hasMeasure = measureKind != GeometryMeasureKind.None && measures is not null;

        for (int i = 0; i < features.Count; i++)
        {
            var geometry = features[i].TheGeometry;

            if (geometry is null || geometry.IsNullOrEmpty())
            {
                emptyCount++;
                continue;
            }

            if (!hasMeasure)
                continue;

            if (measures![i] is double measure)
                values.Add(measure);
            else
                unmeasuredCount++;
        }

        return new GeometryStatistics
        {
            FeatureCount = features.Count,
            EmptyGeometryCount = emptyCount,
            MeasureKind = hasMeasure ? measureKind : GeometryMeasureKind.None,
            UnmeasuredCount = unmeasuredCount,
            Measures = hasMeasure ? NumericSummary.Calculate(values) : null,
        };
    }
}
