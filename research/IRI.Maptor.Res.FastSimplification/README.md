# IRI.Maptor.Res.FastSimplification

The visual experiment of the ISPRS 2025 paper on the Cumulative Triangle Routine (CTR): *A linear-time
online simplification algorithm for polylines and polygons using a sliding-window area threshold*
(Narimani Rad and Pahlavani, ISPRS Archives 2025). A WPF harness that runs RDP, VW, NOPW and CTR from
IRI.Maptor.Core.Spatial over the OSM shapefiles and writes one PNG per feature, render size and method
with the original (red) and the simplified (green) geometry overlaid.

## What it does

`Analysis/SimplificationHelper.cs`:

- `GeneralTest` iterates over the shapefiles of the data folder and writes into a dated output folder;
  layers that already have an output folder there are skipped, so an interrupted run can be resumed.
- `ProcessVisualQuality` takes every single-part feature with 40–500 points, expands its bounding box by
  10 % and, for render sizes 64, 128 and 256 px, finds the zoom level at which the box fits the screen.
  The threshold is 4 pixels at that level (squared for the area-based VW and CTR). File names record
  layer, feature index, zoom level, render size, method, scale denominator, compression and TLVD per
  length: `Building_WM-1-15-64-CTR-18,000-65%-.12.png`.

CTR is `SimplificationType.CumulativeTriangleRoutine`, which dispatches to
`Simplifications.SimplifyByCumulativeTriangleRoutine`: the middle point is retained when the accumulated
triangle area exceeds the threshold, and the window restarts at that middle point. This is the variant
that produced every published figure (checked 2026-09-26 against the published run: all sampled
compression values match). `SimplifyByCumulativeTriangleRoutine2`, which retains the middle and the
last point and restarts at the last point, is what the paper's pseudo-code literally describes; it was
never dispatched. `tests/IRI.Maptor.Tests/Spatial/CumulativeTriangleRoutineTest.cs` pins both down.

Not in this project: the execution-time measurements of the paper (Figures 9 and 10); their data were not kept.

## Thesis re-run (2026-10-26)

For the thesis the author chose to describe CTR as the pseudo-code does and to re-run its results with that variant:
`SimplificationType.CumulativeTriangleRoutine2` (description `CTR2`) dispatches to `SimplifyByCumulativeTriangleRoutine2`,
which `CumulativeTriangleRoutineTest` checks against a line-by-line transcription of the pseudo-code. The published
experiment is unchanged. `Analysis/ThesisExperiments.cs`, run headless from a Release build:

```powershell
dotnet build research\IRI.Maptor.Res.FastSimplification -c Release -o <folder>
<folder>\IRI.Maptor.Res.FastSimplification.exe --run ThesisVisual   # same features, sizes and threshold; RDP, VW, NOPW, CTR2
<folder>\IRI.Maptor.Res.FastSimplification.exe --run ThesisTiming   # time against points, 1:2M and 1:1M, nested random samples
```

`ThesisVisual` writes one row per feature, render size and method (`All-summary.txt`; `Collapsed` = 1 when the result is empty
or a point, 2 when a ring collapsed inside a polygon so that TLVD is undefined) and, for the features of the paper's
Figures 5–8 (Building 11, Road 9938, AdminArea 662, 455, 438, 53, 443), GeoJSON and PNG files. `ThesisTiming` takes the single
parts of all seven layers in a fixed random order (seed 2026), cuts nested samples at 10k, 100k, 1M, 3.5M and 7M points, and
logs a warm-up plus five repetitions per method (RDP, VW, NOPW, BOPW, CTR2) and scale; run it on an otherwise idle machine.
Without `--run` the window of the published experiment opens as before.

## How to run

The project is a WPF executable (`net8.0-windows`). The input shapefile folder and the output root are
hard-coded in `GeneralTest`; point them at your own data first, then:

```powershell
dotnet run --project research\IRI.Maptor.Res.FastSimplification
```

and click the button in the main window to start the batch run.

---
[Back to the research index](../README.md)
