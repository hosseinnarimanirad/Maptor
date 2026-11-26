using System;
using System.Globalization;
using System.Linq;

using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;
using IRI.Maptor.Presentation.Core.Localization;

namespace IRI.Maptor.Presentation.Wpf.Topology;

/// <summary>
/// The words of the topology panels. A rule's own texts come from the app (its <see cref="TopologyRule.MessageKeyPrefix"/>:
/// <c>{prefix}_title</c>, <c>{prefix}_{kind}</c>), so Barg speaks of towers and routes; otherwise the generic library texts
/// (<c>topology_rule_{ruleType}</c>, <c>topology_kind_{ruleType}_{kind}</c>); otherwise the ids themselves. A kind's text
/// may hold <c>{0}</c> for the measured value.
/// </summary>
public static class TopologyMessages
{
    public static string RuleTitle(TopologyRule rule)
        => Text(rule.MessageKeyPrefix is null ? null : rule.MessageKeyPrefix + "_title")
           ?? Text($"topology_rule_{rule.RuleTypeId}")
           ?? rule.Id;

    public static string KindText(TopologyRule? rule, string ruleTypeId, string kind)
        => Text(rule?.MessageKeyPrefix is null ? null : rule.MessageKeyPrefix + "_" + kind)
           ?? Text($"topology_kind_{ruleTypeId}_{kind}")
           ?? kind;

    /// <summary>The issue in one sentence, with its value in place of <c>{0}</c>.</summary>
    public static string Sentence(TopologyIssue issue, TopologyRule? rule)
    {
        var template = KindText(rule, rule?.RuleTypeId ?? string.Empty, issue.Kind);

        if (!template.Contains("{0}"))
            return template;

        try
        {
            return string.Format(CultureInfo.CurrentCulture, template, ValueText(issue.Finding));
        }
        catch (FormatException)
        {
            return template;
        }
    }

    /// <summary>The measured value with its unit, in the current language's digits; empty when there is none.</summary>
    public static string ValueText(TopologyFinding finding)
        => finding.Value is double value ? QuantityText(value, finding.ValueUnit) : string.Empty;

    /// <summary>A number with its unit, in the current language's digits, e.g. "0.8 m".</summary>
    public static string QuantityText(double value, ValueUnit valueUnit)
    {
        var number = valueUnit switch
        {
            ValueUnit.Count => Math.Round(value).ToString("0", CultureInfo.InvariantCulture),
            _ => value.ToString(Math.Abs(value) >= 100 ? "0" : Math.Abs(value) >= 1 ? "0.0#" : "0.###", CultureInfo.InvariantCulture),
        };

        var localized = LocalizationManager.GetLocalizedNumberString(number);

        var unit = valueUnit switch
        {
            ValueUnit.Metres => Text("topology_unit_metres") ?? "m",
            ValueUnit.SquareMetres => Text("topology_unit_squareMetres") ?? "m²",
            ValueUnit.Degrees => "°",
            _ => string.Empty,
        };

        return unit.Length == 0 ? localized : $"{localized} {unit}";
    }

    public static string SeverityText(IssueSeverity severity) => severity switch
    {
        IssueSeverity.Error => Text("topology_severity_error") ?? "Error",
        IssueSeverity.Warning => Text("topology_severity_warning") ?? "Warning",
        _ => Text("topology_severity_info") ?? "Notice",
    };

    /// <summary>The features as <c>[id]</c> runs, e.g. <c>Tower [1234] · Route [77]</c>.</summary>
    public static string FeaturesText(TopologyFinding finding, TopologyRuleSet ruleSet)
        => string.Join(" · ", finding.Features
            .GroupBy(f => f.EntityName)
            .Select(g => FeatureText(LayerName(ruleSet, g.Key), string.Join(" ", g.Select(f => $"[{f.FeatureId}]")))));

    /// <summary>
    /// A layer name and its ids. A Latin name inside right-to-left text is held together with left-to-right marks,
    /// or the brackets around the ids land on the wrong side.
    /// </summary>
    public static string FeatureText(string layerName, string ids)
        => layerName.Any(c => c >= '֐' && c <= 'ࣿ')
            ? $"{layerName} {ids}"
            : $"‎{layerName} {ids}‎";

    public static string LayerName(TopologyRuleSet ruleSet, string entityName)
        => Text(ruleSet.FindLayer(entityName)?.DisplayNameKey) ?? entityName;

    /// <summary>The text of a resource key, or null when the key is missing (the store answers <c>#key</c>).</summary>
    public static string? Text(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var value = LocalizationManager.Instance[key!];

        return value is null || value == "#" + key ? null : value;
    }

    /// <summary><see cref="Text"/> with arguments, or the key itself when it is missing.</summary>
    public static string Format(string key, params object[] arguments)
    {
        var template = Text(key) ?? key;

        try
        {
            return string.Format(CultureInfo.CurrentCulture, template, arguments.Select(a => a is int or long or double ? LocalizationManager.GetLocalizedNumberString(a) : a).ToArray());
        }
        catch (FormatException)
        {
            return template;
        }
    }
}
