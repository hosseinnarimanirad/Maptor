# IRI.Maptor.Core.Topology

Checks spatial data against **topology rules** and reports what it finds as **issues**: points that should
be on lines, line ends that should meet, duplicates, polygons that overlap or leave slivers. An app declares
its rules once (which layers, which distances, which kinds of finding matter); the library does the checking.

The engine stores nothing and draws nothing. It reads features through `ITopologyFeatureSource`, runs the
rules, and returns a sorted list of issues with counts and timings. Showing, fixing and saving are the
caller's business.

```
rule set (in code)  ─┐
feature sources     ─┼─►  ValidationRunner  ─►  issues + summary
scope               ─┘         │
                      rule types from the TopologyRuleCatalog
```

## Concepts

| Type | What it is |
|---|---|
| `ITopologyRuleType` | One kind of check, written once (e.g. "point must be on line"). It only detects: features in, findings out. `TopologyRuleTypeBase` gives the usual defaults. |
| `TopologyRule` | A rule type applied to a layer, or to a pair of layers, with its parameters (distances in metres), a severity per kind, and the kinds that are hidden because they are normally correct. |
| `TopologyRuleSet` | An app's rules and layers, with one "same place" distance for all of them (default 1 cm). |
| `TopologyFinding` | One thing a rule found: rule, kind, features (by layer and id), location, value. |
| `TopologyIssue` | A finding with its identity and its effective severity. |
| `IssueIdentity` | Rule + kind + features + discriminator, hashed. Location and value are not part of it, so a tower that moves closer but not close enough is still the same issue. |
| `CheckComparer` | Compares two checks of the same scope: new, still there, gone. |
| `TopologyCheckScope` | Whole data, one layer, an extent, or chosen features. |

## Measuring

Every distance is in **metres**. Features must be in geodetic WGS84 (4326) or in an SRS whose units are
metres. **Web Mercator is refused**: its units are not ground metres (at 35° latitude one Web Mercator metre
is about 0.8 m on the ground). Rules measure with `LocalMetricFrame` from `IRI.Maptor.Core.Spatial`.

## Example

```csharp
using IRI.Maptor.Core.Topology.Checking;
using IRI.Maptor.Core.Topology.Rules;

var ruleSet = new TopologyRuleSet("network", version: 1,
    layers: new[]
    {
        new TopologyLayer("Pole", GeometryType.Point),
        new TopologyLayer("Line", GeometryType.LineString),
    },
    rules: new[]
    {
        new TopologyRule("network.pole-on-line", "pointOnLine", "Pole", "Line",
            new RuleParameters(new Dictionary<string, object> { ["distance"] = 0.5 })),
    });

var runner = new ValidationRunner(catalog);          // catalog: the rule types you use

var result = await runner.RunAsync(ruleSet, TopologyCheckScope.WholeData, new ITopologyFeatureSource[]
{
    new FeatureListSource("Pole", 4326, poles),
    new FeatureListSource("Line", 4326, lines),
});

foreach (var issue in result.Issues)
    Console.WriteLine($"{issue.Severity}: {issue.RuleId}/{issue.Kind} {string.Join(", ", issue.Finding.Features)}");
```

## Checking part of the data

An **extent** or **feature** scope also loads the neighbours within the rules' reach
(`ITopologyRuleType.ReachOf`), so that a finding at the edge is right. Findings located outside the extent,
or not about one of the chosen features, are then dropped.

## Status

The engine, the model and the comparer are in place. The rule types come next: the existing geometry
validator and points-on-lines builder first, then line ends, duplicates, lines along lines, and polygon
overlaps and gaps.
