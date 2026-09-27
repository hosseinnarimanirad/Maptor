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

`LRHelper.ProcessGeoJson` renders hand-picked GeoJSON samples with every method (figure material).

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

---
[Back to the research index](../README.md)
