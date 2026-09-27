# Logistic regression: status and next steps (2026-09-17)

Start here when resuming. This file summarizes where the work stands, what was found, and what is left. The full evidence (line anchors, tables, probe output) is in [`logistic-regression-bugs.md`](logistic-regression-bugs.md).

## State of the work

| Part | What | Git state on 2026-09-17 |
|---|---|---|
| Phase A | Fixes #3–#6: `Predict`/`Fit` no longer mutate inputs; stored std uses the training mode; constructor options default. 6 tests | committed in `04b7953ba` |
| Phase B | Fixes #1 (gradient scaled by rows) and #2 (L1 sign): `Fit` rewritten as a proximal Newton solver. Replication test unskipped, 2 L1 tests | **staged, not committed** |
| Options | `LogisticRegressionOptions.RegularizationStrength` / `MaxIterations` / `Tolerance`, validation in `Fit`. 4 tests | **unstaged** |
| Phase C | Re-fit of the research models and re-run of the published comparison; harness in `phase-c/`; doc sections | doc changes **unstaged**, `phase-c/` **untracked** |

- Files: `src/Core/IRI.Maptor.Core.MachineLearning/LogisticRegression/{LogisticRegression.cs, LogisticRegressionOptions.cs}`, `.../Common/Normalization.cs` (Phase A), `tests/IRI.Maptor.Tests/MachineLearning/LogisticRegressionTest.cs`, `docs/future-improvements/LR/*`, `docs/future-improvements/README.md`.
- Tests: `LogisticRegressionTest`, 18 passed, 0 skipped.
- Pre-fix behavior can be reproduced at commit `66d5d080a`.

## What the library does now

`LogisticRegression.Fit` minimizes `mean negative log-likelihood + (λ/m)·penalty`, where the penalty is L1 = Σ|β_k| or L2 = Σβ_k²/2, the intercept is not penalized, λ = `RegularizationStrength` (default 1, = scikit-learn `1/C`) and m is the number of rows.

- **Solver:** proximal Newton. Each step builds the Hessian (O(m·p²)), runs coordinate descent on the quadratic model (soft-threshold for L1), then an Armijo line search.
- **Stopping:** max (sub)gradient component ≤ `Tolerance` (1e−10), or `MaxIterations` steps (100), or no further decrease.
- **Start:** coefficients start at 0.
- **Speed:** converges in 6–10 steps, 4–35 ms on the 4,182-row training sets.
- **Why not tuned gradient descent (measured):** proximal gradient descent with the safe step took 23k iterations for LR4, did not converge in 200k for LR7 (10 s), and never for ALL13.
- **Test expectations:** all original scikit-learn-referenced values are unchanged.
- **Behavior change:** on perfectly separable, unregularized data the coefficients now grow until the tolerance stops them, instead of stopping at the old 5,000-iteration cap.

## Findings from Phase C

Everything under `E:\University.Ph.D` was only read.

### The research models

| model (`E:\University.Ph.D\4. Paper2\Model\`) | features | training accuracy old → new | decisions changed |
|---|---|---|---|
| LR4v23 | Area, BaseLength, DistanceToNext, DistanceToPrevious | 91.85 % → 94.69 % | 243 / 4,182 |
| LR7v23 | Area, BaseLength, CosineOfAngle, DistanceToNext, DistanceToPrevious, SquareCosineOfAngle, VerticalDistance | 92.11 % → 94.88 % | 340 |
| LR4v23-LF | BaseLength, CosineOfAngle, SquareCosineOfAngle, VerticalDistance | 83.79 % → 86.99 % | 662 |

- **Training data:** the three JSON files are exactly `SyntheticDataFactory.CreateAll()` as it is today (same records, order, labels, feature values).
- **Probe mismatch:** the probe's "LR4" (DistanceToNext, DistanceToPrevious, Area, SquareCosineOfAngle) was a guess and is **not** LR4v23. The probe's LR7 does equal LR7v23.
- **Load-time re-fit:** the research tools re-fit these models from JSON when they load, so they already use the corrected fit.
- **Where each model is used:** LR4v23-LF is used by the research app (`ApplicationPresenter`) but not by `LRHelper.GetLRAModels`, so it is not in the published run.

### The published comparison, re-run

- **Run:** `E:\University.Ph.D\4. Paper2\AlgorithmRun\1403-07-17-single feature multi threshold 128`, from 2024-10-08, with LR4v23 and LR7v23 against RDP, RW, NOPW and BOPW at 0.5×, 1× and 2×. It was produced by `LRHelper.CompareSingleFeatureVaryingThreshold`, the method active in `GeneralTest`.
- **Same features as the paper:** the harness re-used the 348 features that run saved (`*-F{i}-*-original.json`). It matched them back to the exact shapefile geometry and ran old and new models side by side.
- **Geometry metrics** (points, compression, TLVD) reproduce the published values for 315 of 348 features; the line layers (Railway, Road, Waterways) match 100 %. The 33 differing features are all polygons and differ for the baselines too, so the geometry code changed since 2024, not the LR models.
- **Confusion-matrix metrics** (TP/FP/FN, accuracy, F1) reproduce **nothing**: true positives roughly double, so the rasterization changed. F1 is comparable old vs new inside the re-run, not with the paper.

Means over the 315 exactly reproduced features:

| method | compression | TLVD / length | F1 (re-run raster) | points kept |
|---|---|---|---|---|
| LR4v23 old → new | 0.427 → 0.442 | 0.0597 → 0.0733 | 0.943 → 0.937 | 37.3 → 34.9 |
| LR7v23 old → new | 0.453 → 0.445 | 0.0809 → 0.0728 | 0.930 → 0.936 | 33.7 → 34.8 |
| LR4v23-LF old → new | 0.494 → 0.438 | 0.1011 → 0.0747 | 0.898 → 0.935 | 30.7 → 35.1 |
| RDP ×0.5 / ×1 / ×2 | 0.454 / 0.554 / 0.650 | 0.0416 / 0.0889 / 0.1816 | 0.953 / 0.894 / 0.768 | 35.4 / 26.6 / 19.3 |
| RW ×0.5 / ×1 / ×2 | 0.371 / 0.469 / 0.569 | 0.0510 / 0.1003 / 0.2087 | 0.959 / 0.906 / 0.793 | 44.0 / 34.0 / 25.5 |
| NOPW ×0.5 / ×1 / ×2 | 0.338 / 0.446 / 0.560 | 0.0329 / 0.0759 / 0.1669 | 0.966 / 0.920 / 0.814 | 45.8 / 34.8 / 25.3 |
| BOPW ×0.5 / ×1 / ×2 | 0.467 / 0.565 / 0.653 | 0.0609 / 0.1370 / 0.2938 | 0.934 / 0.845 / 0.655 | 34.2 / 25.7 / 19.0 |

What it means:

- The simplification output changes for 264 (LR4v23), 256 (LR7v23) and 310 (LR4v23-LF) of the 348 features.
- **The three corrected models nearly coincide** (compression ≈ 0.44, TLVD ≈ 0.073). The differences between LR4, LR7 and LF in the published results came mostly from where each non-converged fit stopped, not from the feature sets.
- **LR7v23 and LR4v23-LF improve** (lower TLVD, higher F1). **LR4v23 trades the other way** (more compression, higher TLVD).
- Share of the 348 features where LR has lower TLVD than the baseline at 1×:
  - LR7v23: RDP 71.6 → 81.6 %, RW 42.8 → 55.2 %, NOPW 33.6 → 42.8 %, BOPW 86.8 → 90.2 %.
  - LR4v23: RDP 89.1 → 80.2 %, RW 71.3 → 57.5 %, NOPW 63.2 → 45.7 %, BOPW 90.2 → 89.7 %.
  - LR4v23-LF: RDP 39.1 → 79.6 %, RW 25.0 → 54.0 %, NOPW 14.4 → 44.3 %, BOPW 74.7 → 90.2 %.
- **Independent of the fix:** at about the same compression, RDP ×0.5 and BOPW ×0.5 have lower TLVD than every LR model, old or new. NOPW ×1 is the closest match to the corrected models.

## Problems found and NOT fixed

1. **Geometry regression (the important one).** `Geometry.RemoveConsecutiveDuplicatePoints()` (`src/Core/IRI.Maptor.Core.Spatial/Primitives/Geometry/Geometry.cs`) calls `ReevaluateGeometryType()`, which was rewritten in `a4029ca83` (2026-05-22).
   - **One-ring Polygon:** it throws `NotImplementedException`, because it assigns `Points` while `Geometries` is still set.
   - **Multi-ring Polygon:** it silently turns the Polygon into a `MultiLineString`.
   - **Impact:** every `LRHelper` comparison (and the TrajectoryCompression and FastSimplification research tools) calls it per feature, so they crash on polygon layers today.
   - **Old behavior:** the replaced version is still in the file, commented out: kept Polygon as Polygon.
   - **Workaround:** the `phase-c` harness removes duplicates without changing the type.
   - Fix as a separate change with tests.
2. **Rasterization changed since 2024.** Confusion-matrix metrics no longer reproduce the published run. The cause has not been investigated: `VectorLayer.AsGdiBitmapAsync`, `GdiBitmapRenderStrategy` and `ImageHelper.CalculateConfusionMatrixBitmaps` are the candidates.
3. **Polygon geometry metrics differ for 33 published features**, with equal input point counts. Probably the same geometry refactor (ring handling); not investigated.
4. **`LogisticSimplification.SimplifyByLogisticRegression_Fast_O_n_CumulativeArea` stops at the first retained vertex.** After a retain, `middleIndex == firstIndex` hits `break`; `continue` was probably meant. It is unused: the extension method calls `SimplifyByLogisticRegression`.
5. **`SyntheticDataFactory.IsRetainedFunc0_5` has no `dy = 0` case.** All 18 base-0.5 records with dy = 0 are labelled retained. Bases 1/2/4 keep 7 of 19/21/25, bases 8/16/32 keep 17 of 33/49/81. Every commented-out version has the same gap; it may be deliberate.
6. **Design note, not a bug:** training records always pair the middle vertex with the next original vertex. `SimplifyByLogisticRegression` also tests middles 2, 4, 8… vertices back and keeps the last point of a retained triple untested. The synthetic training lines are all 3 points, so the model never sees the wider triples.
7. **Research quirks** (frozen code, affect interpretation only):
   - `CompareSingleFeatureVaryingThreshold` renders all sampled features into the "original" bitmap, so neighbors count as false negatives for every method.
   - Its random sample (`OrderBy(Guid.NewGuid())`) makes runs non-reproducible.
   - In `All-summary.txt` the LR rows carry the thresholds of the previous coefficient loop.

## Decisions needed (Hossein)

1. Which published runs the paper's figures and tables come from. Then decide whether to update the paper with the corrected models or document the change.
2. Whether the `IsRetainedFunc0_5` gap is deliberate. If not, the synthetic data and all models change again.
3. Fix the `ReevaluateGeometryType` regression (and check whether the rendering change was intended).
4. Whether `docs/future-improvements/LR/` should go into the `$DenyPatterns` of `build/publish-public.ps1`. These docs name the research drive paths and are currently mirrored publicly.
5. Commit Phase B (staged), the options change, the Phase C docs and `phase-c/`.

## Next steps, in order

1. Commit what is ready (item 5 above).
2. Decide on the paper (item 1).
3. If the paper is updated, re-run the other published runs the same way. The harness hard-codes the run folder and the body of `CompareSingleFeatureVaryingThreshold`; adapt `PublishedRun` and the loop body per run:
   - `…multi threshold 256`: same method with `EstimateZoomLevel(…, 256, 256)`.
   - `…single feature multi scale(-2)`: `CompareSingleFeatureMultiScale`.
   - `…layerbased-large data 512/1024`: `CompareLayersVaryingThreshold`.
   - `…single feature admin area`.
4. Fix the geometry regression (problem 1), then check whether polygon features reproduce again.
5. If published F1 values must be reproduced, find the rendering change (problem 2).
6. Resolve the 0.5 px label gap (problem 5). Retrain and re-run if it changes.
7. Optional cleanups: `_Fast_O_n_CumulativeArea` break (problem 4); dead Fisher-information methods in `LogisticRegression`; the duplicate LR rows / stale thresholds in research logs.

## How to resume

```
# tests (18)
dotnet test tests/IRI.Maptor.Tests/IRI.Maptor.Tests.csproj --filter "FullyQualifiedName~LogisticRegressionTest"

# probe (labels written for the pre-fix code; see logistic-regression-bugs.md)
cd docs/future-improvements/LR/probe
dotnet run -c Release -- basic
dotnet run -c Release -- usecase

# Phase C harness (needs E:; ~3 minutes for compare; writes only to the folder you pass)
cd docs/future-improvements/LR/phase-c
dotnet run -c Release -- fit
dotnet run -c Release -- compare <empty output folder>
python analyze.py <that folder>
```

Delete `bin/` and `obj/` under `probe/` and `phase-c/` after use.

## Rules to keep

- **Never write to `E:\University.Ph.D`.** `LRHelper.GeneralTest` writes into `4. Paper2\AlgorithmRun\…` and skips files that already have output there, so do not run it as-is. Use the harness with a scratch output folder.
- `research/IRI.Maptor.Res.*` is frozen (paths cited in articles): do not move or rename. Behavior changes there are a separate decision.
- Claude does not commit, push or stage; Hossein does.
