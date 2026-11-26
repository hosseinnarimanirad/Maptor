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

## Standard rule types

`StandardRuleTypes.CreateCatalog()` registers them all. Distances are in metres.

| Id | Layers | Finds | Kinds (default severity) | Parameters |
|---|---|---|---|---|
| `geometryValid` | A | What `GeometryValidator` reports | The `GeometryIssueKind` names: `WrongRingOrientation` information, the rest errors | — |
| `distinctPoints` | A points | Points within `distance` of each other, one finding per group (single linkage, `PositionClusterer`) | `samePlace` warning, `close` warning, `chain` (more than twice the distance across) error | `distance` = the rule set's same-place distance |
| `pointOnLine` | A points, B lines | A point farther than `distance` from every line | `nearLine` (a line within `nearDistance`; names it, with the distance), `farFromLines` — errors | `distance` 0.5, `nearDistance` 50 |
| `lineEndCovered` | A lines, B points | A line end with no point on it; ends where two parts of one line meet are skipped | `start`, `end` — warnings | `distance` 0.5, `interiorPartEnds` 0 |
| `lineCarriesPoints` | A lines, B points | A line with too few points on it | `tooFewPoints` — warning | `distance` 0.5, `minimumPoints` 2 |
| `pointOnOneLine` | A points, B lines | A point on two or more lines (a junction) | `onSeveralLines` — information | `distance` 0.5 |
| `lineEndsConnect` | A lines (+ `connectTo` lines, `endCoverLayers` points) | A line end that meets nothing | `unsnappedEnd` warning (another end within `distance`, one finding per group), `overshoot` warning (ends past a crossing), `undershoot` warning (stops short of a line), `freeEnd` information, `chain` error | `distance` 0.5, `searchDistance` 5, `maxOvershoot` 5, `connectTo`, `endCoverLayers` |
| `partsJoin` | A lines | Parts of a multi-part line that do not make one path | `gap` warning, `detachedPart` error, `branch` warning, `outOfOrder` information, `reversedPart` information | `distance` 0.5, `maxGap` 50 |
| `distinctLines` | A lines | Duplicate lines | `wholeLine` error (same vertices within `distance`, either direction), `sharedStretch` warning (only with `sharedStretch` 1) | `distance` = same place, `minimumLength` 10 |
| `touchAndReturn` | A lines (or A against `otherLayer`) | Two crossings within `distance` of each other along both lines | `touchAndReturn` information | `distance` 0.5, `otherLayer` |
| `polygonsNoOverlap` | A polygons | Polygons sharing area | `duplicate`, `inside`, `overlap` errors, `thinOverlap` warning (narrower than `sliverWidth`) | `minimumArea` 0.01 m², `minimumWidth` = same place, `sliverWidth` 0.5 |
| `polygonsNoGap` | A polygons | Narrow empty space between neighbours, or space they enclose | `sliver` warning, `enclosedGap` information (only with `enclosedGaps` 1) | `distance` 0.5, `minimumLength` 1, `maximumGapArea` |
| `sequenceAlongLine` | A points, B lines | Numbers that do not rise along the line | `missing` warning, `duplicate` error, `outOfOrder` error, `gap` information, `directionUndecided` warning | `sequenceField` (required), `distance` 0.5 |

The last five read one shared `PointLineTopologyBuilder` pass per pair of layers and distance; when the rule
set has a `sequenceAlongLine` rule for the pair, its numbers are read in the same pass. A line with fewer than
two points reports only `tooFewPoints`, not its open ends.

## Status

Rules other than `geometryValid` skip a feature whose geometry cannot be repaired (it is reported by
`geometryValid`) and check a repairable one with its repaired geometry.

Overlaps are measured with `PolygonOverlap` (Core.Spatial), not `Geometry.Intersection`, which does not handle
boundaries that share edges or vertices. Enclosed gaps come from `EnclosedGaps`, which chains the free pieces of the
polygons' boundaries into loops.

The engine and all version 1 rule types are in place.
