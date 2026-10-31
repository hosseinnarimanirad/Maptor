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
| `FeatureCounts` | `CountFeatures` | one row per layer: shapefile records, single parts, parts with 15 < points < 500, features kept after duplicate removal, their points, and how many fall below / within / above the natural zoom band 10–20 |

The ten algorithms: ED, RDP, RW, PD, VW, SF, NOPW, BOPW, TR and APSC (`SimplificationType`). The TLVD of
APSC is not computed because APSC moves and adds vertices.

Feature protocol, checked against the published run `1403-03-10` (2026-10-31): polygons are split into one
polygon per ring, then the filter 15 < points < 500 (rings counted without the closing vertex), then
consecutive duplicates are removed and features that still repeat a point are dropped. This gives 1,583 /
29,890 / 42,197 / 1,195 / 153,167 / 6,465 / 17,903 features (admin area, building, landuse, railway, road,
water, waterways; 252,400 in all), exactly the published run. The paper's Tables 3 and 4 report only the
242,319 features whose natural zoom level lies between 10 and 20 (the run also holds levels 7–9 and 21–22);
analysis scripts have to apply the same band.

Ring orientation: since 2026-10-13 the shapefile reader turns outer rings counter-clockwise; `LoadFeatures`
restores the clockwise orientation of the published run (`RestorePublishedRingOrientation`), because the
order-dependent algorithms (RW, NOPW, BOPW, SF, APSC) otherwise walk polygons the other way.

## How to run

The project is a WPF executable (`net8.0-windows`). Data and output folders are hard-coded in
`SimplificationHelper` (`DataFolder`, `OutputRoot`); point them at your own copies first, then either

```powershell
dotnet run -c Release --project research\IRI.Maptor.Res.TrajectoryCompression -- --run SingleFeatures
```

(headless; any `Experiment` name works, the output goes to a dated folder `yyyy-MM-dd-<Experiment>` under
`OutputRoot`; use `-c Release` whenever the elapsed-ms column matters) or start it without arguments and
click "Run assessment" in the main window. The published run took about 55 minutes and wrote 5 GB.

---
[Back to the research index](../README.md)
