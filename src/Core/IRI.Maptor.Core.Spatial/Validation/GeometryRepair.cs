using System;
using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Spatial.Validation;

/// <summary>
/// Turns a <see cref="GeometryValidationReport{T}"/> into the features to add, for each choice the user can make.
/// Wrong ring orientation is always fixed. Features without problems are returned as they are; a changed feature is
/// a new <see cref="Feature{T}"/> (same Id, Key, label and attribute dictionary) and the input is never modified.
/// The original order is kept.
/// </summary>
public static class GeometryRepair
{
    /// <summary>
    /// "Fix and add": repairs what can be repaired. Features that cannot be repaired, or that have nothing left
    /// after repair, are left out.
    /// </summary>
    public static List<Feature<T>> Fix<T>(GeometryValidationReport<T> report) where T : IPoint, new()
    {
        return Select(report, problem => problem.Outcome == GeometryRepairOutcome.Repaired ? WithRepairedGeometry(problem) : null);
    }

    /// <summary>
    /// "Leave out invalid features": keeps only features without problems. Features whose only problem is fixed
    /// automatically are kept, fixed.
    /// </summary>
    public static List<Feature<T>> LeaveOutInvalid<T>(GeometryValidationReport<T> report) where T : IPoint, new()
    {
        return Select(report, problem => problem.HasAutomaticFixesOnly ? WithRepairedGeometry(problem) : null);
    }

    /// <summary>
    /// "Add as is": keeps every feature unchanged, except that features whose only problem is fixed automatically
    /// are fixed. A feature with other problems keeps its geometry exactly as it was read.
    /// </summary>
    public static List<Feature<T>> KeepAsIs<T>(GeometryValidationReport<T> report) where T : IPoint, new()
    {
        return Select(report, problem => problem.HasAutomaticFixesOnly ? WithRepairedGeometry(problem) : problem.Feature);
    }

    private static List<Feature<T>> Select<T>(GeometryValidationReport<T> report, Func<FeatureValidationResult<T>, Feature<T>?> forProblem)
        where T : IPoint, new()
    {
        if (report is null)
            throw new ArgumentNullException(nameof(report));

        var problems = report.ProblemFeatures.ToDictionary(p => p.FeatureIndex);

        var result = new List<Feature<T>>(report.Features.Count);

        for (int i = 0; i < report.Features.Count; i++)
        {
            var feature = report.Features[i];

            if (feature is null)
                continue;

            if (!problems.TryGetValue(i, out var problem))
            {
                result.Add(feature);
                continue;
            }

            var selected = forProblem(problem);

            if (selected is not null)
                result.Add(selected);
        }

        return result;
    }

    private static Feature<T> WithRepairedGeometry<T>(FeatureValidationResult<T> problem) where T : IPoint, new()
    {
        var feature = problem.Feature;

        return new Feature<T>(problem.RepairedGeometry!, feature.Attributes)
        {
            Id = feature.Id,
            Key = feature.Key,
            LabelAttribute = feature.LabelAttribute,
        };
    }
}
