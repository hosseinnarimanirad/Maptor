using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Spatial.Validation;

/// <summary>
/// What <see cref="GeometryRepair.Fix{T}"/> would do with a feature that has problems.
/// </summary>
public enum GeometryRepairOutcome
{
    /// <summary>The geometry can be repaired and the feature kept.</summary>
    Repaired,

    /// <summary>Repairing leaves nothing (every part had to be dropped), so the feature is removed.</summary>
    Removed,

    /// <summary>The geometry has a problem that cannot be repaired (NaN or out-of-range coordinates, no geometry).</summary>
    Unrepairable,
}

/// <summary>
/// The problems of one feature and what repairing it would do.
/// </summary>
public class FeatureValidationResult<T> where T : IPoint, new()
{
    internal FeatureValidationResult(Feature<T> feature, int featureIndex, List<GeometryIssue<T>> issues, GeometryRepairOutcome outcome, Geometry<T>? repairedGeometry)
    {
        Feature = feature;
        FeatureIndex = featureIndex;
        Issues = issues;
        Outcome = outcome;
        RepairedGeometry = repairedGeometry;
    }

    public Feature<T> Feature { get; }

    public int FeatureIndex { get; }

    public IReadOnlyList<GeometryIssue<T>> Issues { get; }

    public GeometryRepairOutcome Outcome { get; }

    /// <summary>True when every problem of this feature is fixed automatically (wrong ring orientation only).</summary>
    public bool HasAutomaticFixesOnly => Issues.All(i => i.IsFixedAutomatically);

    /// <summary>
    /// The repaired geometry when <see cref="Outcome"/> is <see cref="GeometryRepairOutcome.Repaired"/>; otherwise null.
    /// A new geometry: the feature's own is not changed (apply it e.g. as an edit of the feature).
    /// </summary>
    public Geometry<T>? RepairedGeometry { get; }
}

/// <summary>
/// The result of <see cref="GeometryValidator"/>: the validated features and the problems found.
/// The features themselves are not changed; use <see cref="GeometryRepair"/> to get the features to add.
/// </summary>
public class GeometryValidationReport<T> where T : IPoint, new()
{
    internal GeometryValidationReport(IReadOnlyList<Feature<T>> features, List<FeatureValidationResult<T>> problems)
    {
        Features = features;
        ProblemFeatures = problems;
        Issues = problems.SelectMany(p => p.Issues).ToList();
    }

    /// <summary>The validated features, unchanged and in their original order.</summary>
    public IReadOnlyList<Feature<T>> Features { get; }

    /// <summary>The features that have at least one problem (including automatic fixes), in their original order.</summary>
    public IReadOnlyList<FeatureValidationResult<T>> ProblemFeatures { get; }

    /// <summary>Every problem found, feature by feature.</summary>
    public IReadOnlyList<GeometryIssue<T>> Issues { get; }

    /// <summary>True when nothing at all was found.</summary>
    public bool IsClean => Issues.Count == 0;

    /// <summary>True when there are problems the user has to decide about (anything but automatic fixes).</summary>
    public bool NeedsUserDecision => Issues.Any(i => !i.IsFixedAutomatically);

    /// <summary>True when the only problems are ones fixed automatically (a notification is enough).</summary>
    public bool HasAutomaticFixesOnly => !IsClean && !NeedsUserDecision;

    /// <summary>The number of problems of each kind.</summary>
    public IReadOnlyDictionary<GeometryIssueKind, int> CountByKind =>
        Issues.GroupBy(i => i.Kind).ToDictionary(g => g.Key, g => g.Count());

    /// <summary>The number of features with problems the user has to decide about.</summary>
    public int InvalidFeatureCount => ProblemFeatures.Count(p => !p.HasAutomaticFixesOnly);

    /// <summary>The number of features whose only problem is fixed automatically.</summary>
    public int AutomaticallyFixedFeatureCount => ProblemFeatures.Count(p => p.HasAutomaticFixesOnly);
}
