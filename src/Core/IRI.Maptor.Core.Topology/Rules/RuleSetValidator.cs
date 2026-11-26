namespace IRI.Maptor.Core.Topology.Rules;

/// <summary>Finds mistakes in a rule set before it is used: unknown rule types or layers, repeated ids, bad parameters.</summary>
public static class RuleSetValidator
{
    /// <returns>One sentence per problem; empty when the rule set is fine.</returns>
    public static IReadOnlyList<string> Validate(TopologyRuleSet ruleSet, TopologyRuleCatalog catalog)
    {
        if (ruleSet is null)
            throw new ArgumentNullException(nameof(ruleSet));

        if (catalog is null)
            throw new ArgumentNullException(nameof(catalog));

        var problems = new List<string>();

        if (ruleSet.SamePlace < 0)
            problems.Add($"The same-place distance of rule set '{ruleSet.Key}' is negative.");

        foreach (var name in ruleSet.Layers.GroupBy(l => l.EntityName).Where(g => g.Count() > 1).Select(g => g.Key))
            problems.Add($"Layer '{name}' is declared more than once.");

        foreach (var id in ruleSet.Rules.GroupBy(r => r.Id).Where(g => g.Count() > 1).Select(g => g.Key))
            problems.Add($"Rule id '{id}' is used more than once.");

        foreach (var rule in ruleSet.Rules)
        {
            var ruleType = catalog.Find(rule.RuleTypeId);

            if (ruleType is null)
            {
                problems.Add($"Rule '{rule.Id}' uses rule type '{rule.RuleTypeId}', which is not in the catalog.");
                continue;
            }

            if (ruleSet.FindLayer(rule.LayerA) is null)
                problems.Add($"Rule '{rule.Id}' checks layer '{rule.LayerA}', which the rule set does not declare.");

            if (ruleType.NeedsLayerB && rule.LayerB is null)
                problems.Add($"Rule '{rule.Id}' needs a second layer.");

            if (!ruleType.NeedsLayerB && rule.LayerB is not null)
                problems.Add($"Rule '{rule.Id}' names a second layer, but its rule type uses only one.");

            if (rule.LayerB is not null && ruleSet.FindLayer(rule.LayerB) is null)
                problems.Add($"Rule '{rule.Id}' compares with layer '{rule.LayerB}', which the rule set does not declare.");

            foreach (var extra in ruleType.ExtraLayersOf(rule).Where(l => ruleSet.FindLayer(l) is null))
                problems.Add($"Rule '{rule.Id}' reads layer '{extra}', which the rule set does not declare.");

            foreach (var kind in rule.Severities.Keys.Concat(rule.HiddenKinds).Distinct().Where(k => !ruleType.Kinds.Contains(k)))
                problems.Add($"Rule '{rule.Id}' sets kind '{kind}', which rule type '{ruleType.Id}' does not report.");

            foreach (var problem in ruleType.Validate(rule))
                problems.Add($"Rule '{rule.Id}': {problem}");
        }

        return problems;
    }
}
