using System;
using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.Validation;
using IRI.Maptor.Presentation.Core.Localization;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.LayerSettings;

/// <summary>
/// Localized texts for geometry problems (<see cref="GeometryValidator"/>), shared by Layer Settings › Geometry and
/// the edit prompt. Keys: <c>geometryIssue_kind_*</c> (one per <see cref="GeometryIssueKind"/>, camelCase),
/// <c>geometryIssue_outcome_*</c>, <c>geometryIssue_location_*</c>, <c>geometryIssue_featureLabel</c>.
/// </summary>
public static class GeometryIssueTexts
{
    private static string L(string key) => LocalizationManager.Instance[key];

    public static string KindTitle(GeometryIssueKind kind)
    {
        var name = kind.ToString();

        return L($"geometryIssue_kind_{char.ToLowerInvariant(name[0])}{name.Substring(1)}");
    }

    /// <summary>What the Fix button does with the feature (repaired, or nothing left / cannot be repaired: delete it).</summary>
    public static string OutcomeText(GeometryRepairOutcome outcome) => outcome switch
    {
        GeometryRepairOutcome.Repaired => L("geometryIssue_outcome_repaired"),
        GeometryRepairOutcome.Removed => L("geometryIssue_outcome_removed"),
        _ => L("geometryIssue_outcome_unrepairable"),
    };

    /// <summary>"Ring with zero area (part 2, ring 1)": the problem and, inside multi-part or polygon geometries, where it is.</summary>
    public static string IssueText(GeometryIssue<Point> issue)
    {
        var location = new List<string>();

        if (issue.PartIndex.HasValue)
            location.Add(string.Format(L("geometryIssue_location_part"), issue.PartIndex.Value + 1));

        if (issue.RingIndex.HasValue)
            location.Add(string.Format(L("geometryIssue_location_ring"), issue.RingIndex.Value + 1));

        var title = KindTitle(issue.Kind);

        return location.Count == 0 ? title : $"{title} ({string.Join(", ", location)})";
    }

    /// <summary>All problems of one feature, one per line.</summary>
    public static string IssuesText(IEnumerable<GeometryIssue<Point>> issues)
        => string.Join(Environment.NewLine, issues.Select(IssueText));

    /// <summary>The feature's label attribute when it has a value, otherwise "Feature 12" (its id).</summary>
    public static string FeatureLabel(Feature<Point> feature)
    {
        if (feature?.Attributes is not null &&
            !string.IsNullOrEmpty(feature.LabelAttribute) &&
            feature.Attributes.TryGetValue(feature.LabelAttribute, out var value) &&
            !string.IsNullOrWhiteSpace(value?.ToString()))
        {
            return value!.ToString()!;
        }

        return string.Format(L("geometryIssue_featureLabel"), feature?.Id);
    }
}
