# IRI.Maptor.Res.LRSimplification

The tool and the experiments of the LRS paper: *An online simplification algorithm for on-the-fly
spatial visualization using logistic regression* (Narimani Rad and Pahlavani, under revision). LRS
itself lives in the library: `IRI.Maptor.Core.MachineLearning`, class `LogisticSimplification<T>`
(traversal), `LogisticRegression` (solver), `LRSimplificationParameters` (the triad features),
`SyntheticDataFactory` (the training data). This WPF application is the labelling and model-building
tool of the paper (its Figure 6) and the driver of the comparisons.

## The tool

- **Training data tab** (`ViewModel/TrainingModel/`): shows a three-point line given as WKT in screen
  pixels next to its two-point version, lets the user mark the middle point as retained or removed,
  collects the samples, saves and loads them as `*.sdjson`, and builds a logistic-regression model from
  the checked samples with the selected features. The "Create initial syn. data" button (hidden) writes
  the rule-based synthetic set of `SyntheticDataFactory` and the images of its label grids
  (`Common/SyntheticDataHelper.cs`).
- **Model tab** (`ViewModel/ApplicationPresenter.cs`): loads a training-data JSON (the model is re-fitted
  from it), saves it as JSON or CSV, and offers per-layer commands on drawn geometries: simplify with the
  loaded model, with LRSv4 and LRSv7, and "ALL" (LRSv4, LRSv4-LF, LRSv7, BOPW, NOPW, RW and RDP at the
  current zoom level, with an image comparison written to disk).
- **Analyze** button: `LRHelper.GeneralTest()`, the published comparison below.

## The experiments (`Common/`)

All experiments read the OSM layers in Web Mercator through `ExperimentData.LoadFeatures` (split
multi-part geometries, filter by point count, seeded random sample, remove duplicate points) and use the
models of `LRHelper.GetLRAModels()`: LRSv4 = `LR4v23.json` and LRSv7 = `LR7v23.json`, re-fitted from their
training data at load time. `LRHelper.GeneralTest(LrsExperiment)` runs one experiment over all layers into
a new dated folder under the output root; layers whose summary already exists there are skipped.

| `LrsExperiment` | Class | What it does | Run folder it reproduces |
|---|---|---|---|
| `SingleFeatureVaryingThreshold128` (default) | `SingleFeatureVaryingThresholdExperiment` | 50 random features per layer (30–2000 points), each at the zoom level where it fits a 128² screen; LRS models versus RDP, RW, NOPW, BOPW at 2, 1 and 0.5 px; pixel confusion matrix, F1, compression, TLVD per length | `1403-07-17-single feature multi threshold 128` (the paper's Figure 8 and Appendix A) |
| `SingleFeatureVaryingThreshold256` | same, 256² screen | | `... multi threshold 256` |
| `SingleFeatureMultiScale` | `SingleFeatureMultiScaleExperiment` | 100 random features (15–500 points) at the 512² level and two levels below; baselines at every level, LRS at the base level | `... single feature multi scale`, `... multi scale-2` |
| `LayersVaryingThreshold512` / `1024` | `LayerExperiments.RunVaryingThresholdAsync` | the whole layer rendered at one scale; layer-level confusion matrix | `... layerbased-large data 512` / `1024` |
| `LayersByZoomLevel` | `LayerExperiments.RunByZoomLevelAsync` | the whole layer at fixed zoom levels 10–15 with RDP, VW, NOPW, BOPW, APSC; difference images | `... single feature admin area` (by output naming) |
| `SpecialCases` | `SpecialCaseExperiment` | features on which LRS has the lowest TLVD of all methods, rendered with every method | material of the paper's Figures 9 and 10 |
| `RevisionPublishedFeatures128` | `PublishedFeaturesRevisionExperiment` + `SingleFeatureVaryingThresholdExperiment` | the 348 features of the published 128² run (`{layer}_WM-F{i}.json` in its folder), under their published indices, with the revision models of `LRHelper.GetRevisionModels()` (LR4v23, LR7v23, `Tree3v23`, `Tree4v23` = decision trees on VerticalDistance/Overhang/BaseLength, `Rule` = remove if ≤ 0.5 px off the base line and ≤ 2.5 px overhang) and the baselines; all measures of `RasterComparison` and the simplification time; then `ExperimentStatistics` writes `Stats-by-layer.txt`, `Stats-wilcoxon.txt`, `Stats-matched.txt` | `2026-09-28-RevisionPublishedFeatures128` (Transactions in GIS revision) |
| `RevisionOracle128` | same driver | the same 348 features and baselines with the oracle-trained models of `LRHelper.GetOracleModels()`: `OracleT3-k{0,1,2}` / `OracleT4-k{0,1,2}` (depth-3/4 trees on VerticalDistance/Overhang/BaseLength trained on the oracle-labelled grid with tolerance k differing pixels), plus LR7v23 and Tree3v23 for reference | `<date>-RevisionOracle128` |
| `ModelSelection` | `ModelSelectionExperiment` | no map data: training set per base length, label conflicts, feature ranking (standardized L1 coefficients; entry into the L1 path), λ by 5-fold CV with the one-standard-error rule, feature subsets, stratified 80/20 test metrics and the coefficients of the deployed models, for label rules V23 and V24 (`ModelSelection-V23.txt`, `-V24.txt`); also writes the label grids | `2026-10-02-ModelSelection` (Sections 3.2/4.1, Tables 3–4) |
| `ReproducePublishedMetric` | `PublishedMetricReproduction` | recomputes the published run's baselines (348 features × RDP/RW/NOPW/BOPW × 2, 1, 0.5 px) with the pinned metric rendering and compares points and TP/FP/FN row by row with its log | `2026-10-02-ReproducePublishedMetric` (4,169 of 4,176 rows equal) |
| `E1RdpLabels` | `RdpLabelExperiment` | training labels copied from RDP: 50 sampled OSM features per layer (seed 1404, disjoint from the published ones) simplified with RDP at 1 px, every vertex a triad labelled kept/removed; label conflicts and CV of the logistic regression on these labels; LR7-RDP and LR4-RDP run with LR7v23/LR4v23 and the baselines on the 348 published features | `2026-10-02-E1RdpLabels` (`E1-summary.md`) |
| `E2OracleRealTriads` | `ResubmissionExperiments.OracleOnRealTriadsAsync` | the rasterization oracle on 37,243 consecutive triads of the E1 sample (real OSM lines at their 128² zoom level, true sub-pixel positions): differing pixels when the middle vertex is removed, and the agreement of LR7v23/LR4v23/hand rule with the oracle at k = 0, 1, 2 | `2026-10-02-E2OracleRealTriads` (`E2-oracle-real-triads.txt`) |
| `E3BoundarySensitivity` | `ResubmissionExperiments.BoundarySensitivityAsync` | LR7/LR4 retrained on the V23 labels with the retain/remove boundary moved one grid cell (0.5 px) outwards ("wide") and inwards ("narrow", `SyntheticDataFactory.ShiftBoundary`), on the 348 published features against RDP at eight thresholds | `2026-10-02-E3BoundarySensitivity` |
| `E5TraversalAblation` | `ResubmissionExperiments.TraversalAblationAsync` | the same fitted LR7v23 under four traversals (`LrsTraversal`: PowerOfTwo = published, FullScan, PreviousOnly, Midpoint) against RDP | `2026-10-02-E5TraversalAblation` |
| `E7MainComparison` | `ResubmissionExperiments.MainComparisonAsync` | LR7v23, LR4v23 and the hand rule against RDP, RW, NOPW, BOPW, SF, Lang (look-ahead 8), CTR at eight thresholds and every n-th point (n = 2–12); no images; `Stats-matched.txt` is the comparison at matched compression. SF duplicates BOPW (same algorithm in this library) | `2026-10-02-E7MainComparison` |
| `E8ScreenSizes` | `ResubmissionExperiments.ScreenSizesAsync` | LR7v23/LR4v23 and the four standard baselines on 128², 256² and 512² screens (`Screen128/256/512` sub-folders) | `2026-10-02-E8ScreenSizes` |
| `E6Timing` | `ResubmissionExperiments.Timing` | single-threaded simplification time (median of 5 runs, no rendering) of LR7 (power-of-two and full scan), RW, NOPW, BOPW, RDP on real line parts (≥ 100 points, stratified by log n) and on a jittered straight line of 1,000–200,000 points; log–log slopes in `Timing-summary.txt` | `2026-10-04-E6Timing` |
| `E9DisplacementLabels` | `ResubmissionExperiments.DisplacementLabelsAsync` | LR7, LR4 and depth-5 trees trained on oracle v3 labels by displacement (`hd-1`, `hd-1.5`, `hd-2`: remove when no drawn pixel moves farther than d) and by pixel count (`any-k2`), against the baselines on the 348 published features | `2026-10-05-E9DisplacementLabels` (`E9-summary.md`) |
| `E10CoarseScreens` | `ResubmissionExperiments.CoarseScreensAsync` | the 348 features at 64², 32², 16² screens (1–3 zoom levels coarser than natural), LR4v23/LR7v23/hand rule against RDP, RW, NOPW, BOPW, Lang 8, CTR | `2026-10-05-E10CoarseScreens` (`E10-summary.md`) |
| `E11VisualLimit` | `ResubmissionExperiments.VisualLimitAsync` → `SingleFeatureVaryingThresholdExperiment.RunVisualLimitAsync` | compression at the visual-equivalence limit: each baseline swept per feature over 0.125 · 2^(k/3) px (0.125–8 px, stop after 3 visibly different results) at 128/64/32/16 px; models once | `2026-10-05-E11VisualLimit` (`E11-summary.md`) |
| `E12ThinParts` | `ResubmissionExperiments.ThinPartsAsync` | 124 polygons with thin parts selected from the full layers (`4. Paper2\DataWm\ThinParts-2026-10-05`) at 128/64/32/16 px; LR4v23, LR4 on hd-1 labels, hand rule; every baseline at all 16 thresholds 0.125–4 px, plus `RDPs`/`BOPWs` = RDP and BOPW with distance to the chord segment; omission/commission and topology columns | `2026-10-05-E12ThinParts` (`E12-summary.md`) |

The visual-metric rows carry, since 2026-10-05, `ExcessDisplacement` (mean (d − √2)² over the pixels of both rasters
farther than the 8-neighbourhood from the other raster), `Displacement95`, `FractionBeyond1/2/3`, `OmissionBeyond2`,
`CommissionBeyond2` (`RasterComparison`) and `CollapsedRings`, `SelfIntersections` (`Common\TopologyCounts.cs`, logged
only: topology is out of the scope of the visual comparison). RDP, NOPW and BOPW in `Simplifications.cs` measure the
distance to the infinite line through the chord (the definition of Ramer 1972 and Douglas & Peucker 1973; `SpatialUtility.GetPointToLineDistance`,
named `GetPointToLineSegmentDistance` until 2026-10-06); `SimplifyByRamerDouglasPeuckerSegment` and `SimplifyByBeforeOpeningWindowSegment` are the segment-distance
variants, used only in E12. Code changes of 2026-10-05: `docs\future-improvements\LR\phase3-2026-10-05.md`.

**Metric rendering** (since 2026-10-02): every raster of the visual metric is drawn by
`MetricRasterizer` (`IRI.Maptor.Infrastructure.GdiPlus`) with `SimplificationHelper.RenderOptions`, by default
`MetricRenderOptions.Published` = GDI+ 1-px pen, aliased, drawn = neither transparent nor white: the rendering of the
published run. The map renderer (anti-aliased since 2025) is no longer used for the metric; that change had doubled
the drawn pixels and was why the published F1 values did not reproduce. Each run folder holds `Rendering.txt`.

**Oracle labelling** (`Common/OracleLabeller.cs`, `--label-oracle`): the synthetic grid of `SyntheticDataFactory` (same base
lengths, 0.5-px step, ±4-px margins) is drawn three-point and two-point with the metric rendering at 5 base orientations
(0°–90°) and 4 sub-pixel offsets, and the number of differing pixels is stored per triad (`OracleLabelledTriad`). Labels
are derived at load time for any tolerance k (`IsRetained(k)` = more than k pixels differ), so one file serves a family
of models. Output: `4. Paper2\Model\Oracle\oracle-triads-v2.json` (published rendering), its `-agreement.txt` (agreement
with the published rule table per tolerance and base length) and `-grids\`. `oracle-triads-v1.json` (2026-09-28,
anti-aliased) is kept and never overwritten. The published `LR*v23.json` training data is untouched.

**Label grids** (`--export-labels [folder]`): `SyntheticDataFactory.CreateLabelGrid` for the rule sets V23 (published) and
V24 (V23 plus the collinear row of the 0.5-px base) as `label-grid-V23.json` / `-V24.json` in `4. Paper2\Model\`; Figure 3
of the paper is drawn from them.

**Model evaluation** (library, `IRI.Maptor.Core.MachineLearning.ModelEvaluation`): seeded stratified train/test split and
k-fold cross-validation, λ selection with the one-standard-error rule, label conflicts. The coefficients printed in the
paper's Tables 4–5 up to manuscript v12 were fitted with scikit-learn on an earlier 1,328-triad export, and the models of
the published run by the 2024 gradient-descent trainer (kept in the tests as `PublishedGradientDescentLogisticRegression`);
see `docs/future-improvements/LR/phase1-2026-10-02.md`.

`LRHelper.ProcessGeoJson` renders hand-picked GeoJSON samples with every method (figure material).

Measures per row (`VisualSimplificationLog`): compression, TLVD/length, the exact pixel confusion matrix
(TP/FP/FN, precision, recall, F1; TN not counted) and, since 2026-09-28, `RasterComparison`: tolerant
precision/recall/F1 (a pixel counts when the other raster has a pixel within 1 px·√2, i.e. its 8-neighbourhood;
Wiedemann's correctness/completeness), mean displacement of the simplified pixels to the original and of the
original pixels to the simplified raster (in px, from an exact Euclidean distance transform), and the Hausdorff
distance. `Stats-wilcoxon.txt` holds paired Wilcoxon signed-rank tests (model vs. baseline × coefficient, per
layer and pooled) on compression, F1, tolerant F1, displacement, Hausdorff and TLVD; `Stats-matched.txt` the
same at matched compression (per feature, the baseline threshold whose compression is closest to the model's).
`ExperimentStatistics.WriteAll` works from `All-summary.txt`, so it can be re-run on any run folder.

Changes made on 2026-09-26 to `SingleFeatureVaryingThresholdExperiment` that alter its numbers relative
to the published run: the reference raster contains only the feature under test (it used to contain all
sampled features of the layer, so neighbours counted as false negatives); the random sample is seeded
(`ExperimentData.DefaultSeed`); LRS rows are written once per feature; elapsed milliseconds measure the
simplification alone. The features of the published run are stored next to its outputs as `*-F{i}.json`.

Log formats: `Model/VisualSimplificationLog.cs` (one TSV row per feature × method, with the confusion
matrix) and `SimplificationAccuracy` from `IRI.Maptor.Presentation.Wpf.Helpers.SimplificationHelper`,
which also holds the rendering and the pixel confusion matrix (`CompareBySingleFeature`, `CompareByLayer`).

## How to run

The project is a WPF executable (`net8.0-windows`). Data, model and output folders are constants at the
top of `Common/LRHelper.cs`; point them at your own copies first, then:

```powershell
dotnet run --project research\IRI.Maptor.Res.LRSimplification
```

Headless: `--run <LrsExperiment>` runs one experiment without the window and exits (errors go to
`Error-*.txt` in the output root):

```powershell
dotnet run --project research\IRI.Maptor.Res.LRSimplification -- --run RevisionPublishedFeatures128
dotnet run --project research\IRI.Maptor.Res.LRSimplification -c Release -- --run E6Timing   # timing: always Release
dotnet run --project research\IRI.Maptor.Res.LRSimplification -- --run ModelSelection
dotnet run --project research\IRI.Maptor.Res.LRSimplification -- --run ReproducePublishedMetric
dotnet run --project research\IRI.Maptor.Res.LRSimplification -- --label-oracle
dotnet run --project research\IRI.Maptor.Res.LRSimplification -- --export-labels
```

---
[Back to the research index](../README.md)
