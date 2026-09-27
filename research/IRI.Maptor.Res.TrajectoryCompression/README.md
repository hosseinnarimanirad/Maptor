# IRI.Maptor.Res.TrajectoryCompression

The assessment of the JSS paper: *An in-depth assessment of simplification algorithm performance across
diverse feature types and scales, employing a range of threshold values* (Narimani Rad and Pahlavani,
Journal of Spatial Science). This is the code the paper's data-availability statement points to. A WPF
harness; the algorithms and the McMaster measures live in `IRI.Maptor.Core.Spatial`
(`Analysis/Simplification/Simplifications.cs`, `Geometry.cs`).

## The experiments (`Analysis/SimplificationHelper.cs`)

`GeneralTest(Experiment)` runs one experiment over every shapefile of the data folder into a dated output
folder (layers whose summary already exists there are skipped). Shared protocol: single-part features
with 15 < points < 500, consecutive duplicate points removed; each feature gets the zoom level at which
its bounding box fits a 256² screen (its "natural scale"); the base threshold is 10 px at that level
(area thresholds are its square) and the eleven coefficients 6, 5, 4, 3, 2, 1, 1/2, 1/3, 1/4, 1/5, 1/6
are applied to it.

| `Experiment` | Method | Output |
|---|---|---|
| `SingleFeatures` (default) | `TestSingleFeatures` | one TSV row per feature × coefficient × algorithm (`Model/SingleSimplifiedGeometryLog.cs`): point counts, PCC, PDD, PCLL, PCANGLE, PCCS, TLVD and TLVD per length, elapsed ms. The paper's ">26 million records" |
| `PerFeatureRanks` | `StatisticsEnhancedTest` | one row per feature × coefficient with the six measures of all ten algorithms and their ranks as columns (`Model/NineAlgoSixMeasureLog.cs`) |
| `VisualOutput` | `TestWithOutput` | for the hand-picked features of `IsSpecialCase` (the RDP-versus-SF cases of the paper's Figure 9): one PNG per coefficient with RDP and SF overlaid on the original, plus the GeoJSON of every result |

The ten algorithms: ED, RDP, RW, PD, VW, SF, NOPW, BOPW, TR and APSC (`SimplificationType`). The TLVD of
APSC is not computed because APSC moves and adds vertices.

Note: the feature filter (15 < points < 500) applies per single part; the paper's Table 3 counts all
features of the datasets.

## How to run

The project is a WPF executable (`net8.0-windows`). Data and output folders are hard-coded in
`GeneralTest`; point them at your own copies first, then:

```powershell
dotnet run --project research\IRI.Maptor.Res.TrajectoryCompression
```

and click "Run assessment" in the main window.

---
[Back to the research index](../README.md)
