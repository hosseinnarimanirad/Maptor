# Logistic regression: correctness review and pending fixes

- **Status:** analysis done 2026-09-05; **all six defects fixed 2026-09-17** (see *Applied 2026-09-17*); options added; **research models re-fitted and the published comparison re-run 2026-09-17** (see *Phase C*). Open: Hossein's decisions listed at the end of *Phase C*
- **Area:** `src/Core/IRI.Maptor.Core.MachineLearning` — `LogisticRegression` and the `LogisticSimplification` use case
- **Risk:** fixes #3–#6 changed no fitted coefficient (the only output change is #3 on LR7, 2 of 300 test polylines). Fixes #1 and #2 **changed every trained simplification model**; research models re-fit at load time, so their results change the next time they run
- **Effort:** #3–#6 about one hour including tests; #1+#2 about half a day including retraining and re-evaluating the simplification models
- **Companion:** `probe/` next to this file is a console project that reproduces every number below (`dotnet run -c Release -- all`)

## Where this lives in the repo

Line numbers were correct on 2026-09-05; treat them as anchors.

| File | What matters |
|---|---|
| `src/Core/IRI.Maptor.Core.MachineLearning/LogisticRegression/LogisticRegression.cs` | `Fit` 41–159, `Predict` 161–186. Line 55 `beta = 1.0` init; 65 stats via `GetStatisticsByColumns`; 85 z-score normalization with the options mode; 94 `InsertColumn` bias column; 98 dummy stat inserted at index 0; 101 loop `_maxIteration = 5000`; 137 L1 term; 141 L2 term; 150 gradient scaling `1.0 / beta.Length`; 152 update with `_learningRate = 0.1`; 167 `xValues.Insert(0, 1)`; 181 in-place normalization in `Predict`. Two private `ComputeTheFisherInformationMatrix*` methods (188–234) are unused |
| `.../LogisticRegression/LogisticRegressionHelper.cs` | logit / sigmoid wrappers over `DotProduct`; `CalculateNegLogLikelihood` has no clamping (log 0) but is not used by `Fit` |
| `.../LogisticRegression/LogisticRegressionOptions.cs` | defaults: `RegularizationMethod = None`, `VarianceCalculationMode = Population`, `NormalizeFeatures = true` |
| `.../Common/Normalization.cs` | `NormalizeColumnsUsingZScore(Matrix, mode)`, std == 0 → 1 fallback; duplicate `using IRI.Maptor.Extensions;` (warning CS0105) |
| `src/Core/IRI.Maptor.Core.Common/Mathematics/Algebra/Matrix.cs` | column-major jagged storage `Element[column][row]`; `GetColumn` (≈521) returns the internal array; `SetColumn` (≈554) stores the reference; `InsertColumn` (≈657) rebuilds **in place** through `Reconstruct` (≈525); `GetStatisticsByColumns` (≈1934) builds `BasicStatisticsInfo` per column |
| `src/Core/IRI.Maptor.Core.Common/Mathematics/Statistics/Model/BasicStatisticsInfo.cs` | ctor calls `Statistics.CalculateStandardDeviation(values)` with its default mode, which is **Sample** (`Statistics.cs` ≈316) |
| `.../LogisticRegression/UseCases/SpatialSimplification/LogisticSimplification.cs` | `Create` 24–87 builds the Matrix from `FeatureValues`, fits with **L1**; `IsRetained` 103–136 thresholds `Predict` at 0.5; `SimplifyByLogisticRegression` and the two `_Fast_O_n*` variants walk the polyline with first/middle/last triples |
| `.../SpatialSimplification/LRSimplificationParameters.cs` | one training record; `FeatureValues` (53–66) is cached in `_featureValues` and rebuilt when its count no longer equals `Features.Count` — this is what masks bug #5 |
| `.../SpatialSimplification/LRSimplificationTrainingData.cs` | `Create` 77–155 emits **one record per middle vertex** of the original line; `LoadFromJson` |
| `.../SpatialSimplification/SyntheticDataFactory.cs` | `CreateAll()` → 4,182 three-point lines (base lengths 0.5…32 px, middle point on a 0.5 px grid, label from the `IsRetainedFunc*` rules) |
| `tests/IRI.Maptor.Tests/MachineLearning/LogisticRegressionTest.cs` | 6 original tests: 4 on the 40-row gmat/gpa/work-experience dataset (reference coefficients from scikit-learn `penalty='l2', C=1` and `solver='saga', penalty='l1', C=1`), 2 on a 100-row exam-score dataset (`TestNormalize`, `..._UsingSampleVariance`); `..._WithRegularization` is empty. Plus 6 tests added 2026-09-17 for #1 and #3–#6 |

Callers of the model:

- `research/IRI.Maptor.Res.LRSimplification/Common/LRHelper.cs` `GetLRAModels()` loads `LR4v23.json` / `LR7v23.json` from an external drive (absolute paths, not present on this machine) and **re-fits at load time** through `LogisticSimplification<Point>.Create`. So any change to `Fit` changes those models the next time they load.
- `research/IRI.Maptor.Res.LRSimplification/ViewModel/TrainingModel/SyntheticDataTrainingViewModel.cs` builds training data from `SyntheticDataFactory` items; **all 13 features are selected by default** (ctor, ≈28–50). The feature lists are not in the repo but are stored in the model JSON files (see *Phase C*). The "LR4" used by the probe below (DistanceToNext, DistanceToPrevious, Area, SquareCosineOfAngle) was a guess and is **not** the real LR4v23 (Area, BaseLength, DistanceToNext, DistanceToPrevious); the probe's LR7 does match LR7v23.
- `research/IRI.Maptor.Res.TrajectoryCompression`, `src/Presentation/IRI.Maptor.Presentation.Wpf/Helpers/SimplificationHelper.cs`, `.../MachineLearning/Extensions/GeometryExtensions.cs` consume already-built models.
- `research/` is frozen (paths cited in articles). Do not move anything there; behavior changes through the library are a separate decision.

## How `Fit` actually works (verified, the probe replicates it bit for bit)

1. `beta` = all ones, length p+1.
2. If `NormalizeFeatures`: `xStatistics = xValues.GetStatisticsByColumns()` (**Sample** std) and `xValues` is replaced by a z-scored **copy** using the options mode (**Population** by default). Otherwise `xStatistics = null` and the caller's matrix is used directly.
3. A column of ones is inserted at index 0 (`InsertColumn`, in place). A dummy `BasicStatisticsInfo { Mean = 1, Std = 0 }` is inserted at index 0 of `xStatistics`.
4. 5,000 iterations of full-batch gradient descent, learning rate 0.1, no convergence check:
   `grad_k = (1/L) · Σ_i (σ(x_i·β) − y_i) · x_ik + reg_k`, where **L = number of parameters** (not rows), λ = 1, intercept excluded from regularization, `reg_k = λ/L` for L1 (no sign), `λ/L · β_k` for L2.
5. `Predict(list)`: inserts 1 at index 0 **into the caller's list**, z-scores indices 1..p in place with the stored (Sample) statistics, returns the sigmoid.

Why the sklearn reference values in the tests come out right: sklearn with C=1 minimizes `0.5·‖w‖² + Σ loss` (L2) or `‖w‖₁ + Σ loss` (L1). The code's gradient is `(1/L)·(Σ loss-gradient + λ·reg)`, i.e. the same objective scaled by a constant, so the **optimum is identical** whenever the iteration converges. Keep this equivalence when fixing #1 (scale λ by the same factor).

## Findings

| # | Defect | Where | Effect on the simplification use case | Fix |
|---|---|---|---|---|
| 1 | Gradient scaled by 1/(number of parameters) instead of 1/(number of rows); effective step grows with the training set and the fit **does not converge** above a few hundred rows | `LogisticRegression.cs:150` | **Changes the model.** See *Measured impact* | divide by `numberOfRow`; scale λ by the same 1/m; re-tune learning rate / add a convergence check; re-baseline tests |
| 2 | L1 term is a constant `λ/L` with no `Math.Sign(beta[i])`; correct subgradient only when β > 0. Negative coefficients are pushed away from zero; a zero-variance feature drifts by `−lr·λ/L` per iteration (−34.7 after 5,000 iterations) | `LogisticRegression.cs:137` | negligible on a converged fit (5–20 of 4,182 decisions, λ = 1 vs 4,182 rows) **except** with constant features, where the drifted coefficient later multiplies raw pixels at prediction time | `regularizationComponent = lambda / L * Math.Sign(beta[i])` |
| 3 | Training normalizes with Population std, stored `xStatistics` use Sample std, so `Predict` standardizes with a different σ than `Fit`; a single-row matrix throws (Sample variance needs 2 values) | `LogisticRegression.cs:65` vs `:85`; `BasicStatisticsInfo` ctor | none measurable (0 of 4,182 decisions; 300/300 polylines identical for LR4, 298/300 for LR7) | build `xStatistics` with the options mode (compute mean/std once in `Normalization` and return both) |
| 4 | With `NormalizeFeatures = false`, `InsertColumn` mutates the caller's `Matrix` in place; a second `Fit` on the same matrix gets a 5-length beta and `Predict` throws `NotImplementedException("ArrayExtensions > Multiply")` | `LogisticRegression.cs:94` | not reachable (normalization is on) | clone before inserting the bias column |
| 5 | `Predict` mutates the input list (bias insert + in-place z-score); a second call with the same list throws `ArgumentOutOfRangeException` | `LogisticRegression.cs:167,181` | masked by the `FeatureValues` re-init guard | copy into a local array |
| 6 | `LogisticRegression(double[] beta, List<BasicStatisticsInfo>)` leaves `_options` null → `Fit` throws NRE; the stored statistics must carry the dummy entry at index 0 (undocumented, off-by-one otherwise) | `LogisticRegression.cs:34` | not reachable | default the options; document or handle the dummy entry |

Minor, not bugs: learning rate, iteration count and λ are hard-coded; `GetRow` allocates per row per iteration (5,000 × rows); dead Fisher-information methods; `CalculateNegLogLikelihood` yields NaN/∞ for predictions of exactly 0 or 1.

### Evidence on the unit-test dataset (40 rows, probe mode `basic`)

Same 40 rows replicated k times has the same optimum; the current code only reaches it for small k:

| rows | fitted beta | mean-gradient norm at the end |
|---|---|---|
| 40 | −0.713, 0.953, 1.530, 1.721 | 1.7e−16 |
| 200 | identical | 2.5e−17 |
| 1,000 | −0.047, 1.517, 2.051, 2.109 | 6.3e−2 |
| 4,000 | 17.9, 13.8, 19.5, 26.6 | 8.7e−2 |
| 10,000 | 1.80, 11.7, 6.04, 4.22 | 8.8e−2 |

Negating the gmat column should flip only that coefficient: L2 gives 0.827 → −0.827 exactly; L1 gives 0.718 → −2.257 and moves the other two (1.123 → 0.526, 1.315 → 1.419). The existing L1 test passes only because all three reference coefficients are positive.

Stored std for gmat is 61.43 (Sample) while training used 60.65 (Population); row-0 probability 0.9553 as fitted vs 0.9532 from `Predict`. With 3 rows the stored std is 1.53 vs 1.25.

## Measured impact on the simplification use case (probe mode `usecase`)

The numbers in this section and in *Evidence on the unit-test dataset* describe the code **before** the 2026-09-17 fixes ("current" = as shipped then). To reproduce them, build the probe against commit `66d5d080a`. Against the fixed code the probe prints the results listed under *Applied 2026-09-17*.

Training set = `SyntheticDataFactory.CreateAll()` pushed through `LRSimplificationTrainingData.Create` exactly as the research tool does: 4,182 records, 3,452 retained (base rate 82.5 %), no NaN features. "Current" = `LogisticSimplification.Create` as shipped. "Converged" = the same objective solved to gradient norm 1e−9 (momentum GD in the probe). Test set for simplification = 300 random screen-space polylines (random walks with 1–20 px steps and sine waves with 0.3–30 px amplitude), run through `SimplifyByLogisticRegression`, models compared by injecting betas via reflection into `_regression`.

Current fit is an oscillation snapshot, not an optimum:

| feature set | mean-gradient norm at the current beta | beta change in the last iteration (4999→5000) |
|---|---|---|
| LR4 | 4.2e−1 | up to 10.4 |
| LR7 | 1.2e−1 | up to 3.7 |
| ALL13 | 1.7e−1 | up to 2.2 |

Training accuracy (decision = probability > 0.5):

| feature set | current | converged (fix #1) | converged + sign (fix #1+#2) |
|---|---|---|---|
| LR4 | 72.26 % | 84.98 % | 85.01 % |
| LR7 | 92.11 % | 95.03 % | 94.88 % |
| ALL13 | 92.25 % | 95.07 % | 94.88 % |

Decisions and simplification output that change relative to the current model:

| fix | training decisions changed (of 4,182) | polylines with identical output (of 300) | points kept, current → fixed (of 15,001) |
|---|---|---|---|
| #3 only | LR4 0, LR7 0, ALL13 0 | LR4 300, LR7 298, ALL13 300 | LR4 9,793 → 9,793; LR7 9,557 → 9,555 |
| #1 only | LR4 1,628, LR7 338, ALL13 338 | LR4 14, LR7 95, ALL13 12 | LR4 9,793 → 13,549; LR7 9,557 → 9,635; ALL13 3,234 → 1,196 |
| #1 + #2 | LR4 1,633, LR7 340, ALL13 342 | LR4 14, LR7 96, ALL13 1 | LR4 → 13,576; LR7 → 9,640; ALL13 → 11,293 |
| #2 given #1 | LR4 5, LR7 14, ALL13 20 | — | — |

Current betas for reference (bias first, features in the order listed):

- LR4 (DistanceToNext, DistanceToPrevious, Area, SquareCosineOfAngle): `34.629, −7.107, −6.924, 70.369, 3.070`; converged: `5.905, −1.180, −1.145, 8.588, 0.680`
- LR7 (LR4 + VerticalDistance, CosineOfAngle, BaseLength): `28.245, 44.517, 44.688, 33.153, −1.986, 2.330, 4.796, −62.142`; converged: `10.106, 10.090, 10.149, 12.406, −0.523, 0.146, 0.349, −15.305`

ALL13 side observation: in the synthetic set `dY13` is always 0 (start and end share y), `dX13 == BaseLength`, and `VerticalDistance == dY12 == dY23`. The constant feature gets std 0 → 1, so bug #2 drifts its coefficient to −34.7 (current) or −477 (fix #1 without #2); at prediction time that multiplies raw pixel differences, which is why ALL13 keeps only 3,234 of 15,001 points. Any model trained with all features selected is dominated by this artifact. Fix #2 (sign) removes the drift (coefficient 0.000), and the collinear columns make the unregularized problem flat, which is why the ALL13 reference fits only reached gradient 2e−4 in 400k iterations.

## Decision (made 2026-09-17: apply #1 and #2)

Fix #1 replaces the current models with the actual optimum of the objective the code encodes. By the objective's own measure it is better (LR4 goes from worse-than-"always retain" to 85 %), but **every retrained simplification model and every comparison made with the current models changes**. Since the research models are re-fitted at load time, a library fix changes the research results the next time they run.

Recommended plan:

1. ~~Apply #3, #4, #5, #6 now, plus a regression test each.~~ Done 2026-09-17.
2. ~~Add a unit test that replicates the 40-row dataset 100× and asserts the same coefficients as the 40-row fit.~~ Done 2026-09-17.
3. ~~Implement #1 (+#2), re-tune, re-baseline the sklearn-referenced tests.~~ Done 2026-09-17; no expected value had to change. **Still open:** re-fit the real LR4v23 / LR7v23 / LR4v23-LF models from their JSON files and re-run the simplification comparisons in `research/IRI.Maptor.Res.LRSimplification`.
4. ~~Make iteration cap, λ and tolerance part of `LogisticRegressionOptions`.~~ Done 2026-09-17: `RegularizationStrength` (λ, default 1, = scikit-learn `1/C`), `MaxIterations` (default 100) and `Tolerance` (default 1e−10). Negative or NaN values throw `ArgumentOutOfRangeException` from `Fit`. There is no learning rate any more; the coordinate-descent sweep cap stays internal (10,000). Tests: `TestZeroRegularizationStrengthEqualsNoRegularization`, `TestRegularizationStrengthIsRelativeToTheNumberOfRows` (10 copies of the rows with λ = 10 give the same fit as the rows once with λ = 1, for L1 and L2), `TestMaxIterationsLimitsTheFit`, `TestInvalidOptionsThrow`; 18 passed.

## Applied 2026-09-17 (#3–#6)

Code:

- `Normalization.NormalizeColumnsUsingZScore(Matrix, mode, out statistics)` (new overload) returns the mean and std each column was normalized with; the old overload delegates to it. `NormalizeUsingZScore(double[], mean, std)` is new and the mode-based array overload delegates to it (same arithmetic, bit-identical results). Duplicate `using` removed.
- `LogisticRegression.Fit` stores those statistics (#3) and clones the matrix when `NormalizeFeatures = false` (#4). `Predict` works on a copy of the input list (#5). Both constructors now leave valid options (`null` options fall back to the defaults), and the `(beta, xStatistics)` constructor documents the index-0 placeholder (#6).

Tests added in `LogisticRegressionTest.cs`: `TestPredictDoesNotChangeTheInput` (#5), `TestFitWithoutNormalizationDoesNotChangeTheInput` (#4), `TestFitStoresTheStatisticsUsedForNormalization` and `TestFitWithSingleRow` (#3), `TestFitAfterCreatingFromCoefficients` (#6), and `TestFitOnReplicatedRowsGivesTheSameCoefficients` (#1). The last one is `Skip`ped; run without the skip it fails with intercept 17.871 against −0.713, matching the table above. Result: 11 passed, 1 skipped.

Verified with the probe against the fixed library:

- `basic`: A–C unchanged (they show #1 and #2); D now stores the population std (60.6548) and the fitted and predicted probabilities agree (0.955313); E, F and G no longer mutate or throw.
- `usecase`: replica max|diff| is still 0 and the LR4 / LR7 / ALL13 betas are identical to the ones listed above, so no coefficient moved. The library model is now the "fix #3" model: LR7 keeps 9,555 points instead of 9,557, LR4 and ALL13 unchanged, training accuracy unchanged.

Probe labels written before the fix are now stale: section D still says "stored stats use Sample", E–G still say "bug", and the `usecase` "fix #3 only" rows compare two identical models (300/300 is expected).

## Applied 2026-09-17 (#1, #2)

`LogisticRegression.Fit` no longer uses fixed-step gradient descent. It minimizes

`mean negative log-likelihood + (λ/m) · penalty(β₁…β_p)`, with L1 = Σ|β_k|, L2 = Σβ_k²/2, intercept not penalized, λ = 1, m = number of rows

with proximal Newton steps. Each step builds the gradient and Hessian of the smooth part (O(m·p²)) and minimizes the quadratic model plus the exact penalty by cyclic coordinate descent, using soft-thresholding for L1. A backtracking (Armijo) line search follows. It stops when the largest (sub)gradient component is below 1e−10, or after 100 steps, or when no step decreases the objective. The objective is the old one divided by m, so the scikit-learn C = 1 equivalence holds. Coefficients now start at 0 instead of 1.

- **#1:** everything is scaled by 1/m, so nothing depends on the number of rows.
- **#2:** soft-thresholding is the exact L1 proximal step. The sign is handled, and coefficients reach exactly 0.

Why not gradient descent with a tuned step: on the synthetic set, proximal gradient descent with the safe fixed step 4/(1+p) needed 23,494 iterations (1.0 s) for LR4. LR7 was still at optimality 1.6e−9 after 200,000 iterations (10.2 s), and ALL13 at 5e−5 after 200,000 (15.2 s). The Newton fit converges in 6–10 steps: 21 ms (LR4), 16 ms (LR7), 35 ms (ALL13).

Tests: `TestFitOnReplicatedRowsGivesTheSameCoefficients` is no longer skipped. Two #2 tests were added: `TestL1RegularizationIgnoresTheSignOfCoefficients` (negating a feature only flips its coefficient) and `TestL1RegularizationKeepsAConstantFeatureAtZero`. All sklearn-referenced expected values are unchanged; the 100-row fit gives 1.718449, 4.012903, 3.743903, the same as before to 6 decimals. Result: 14 passed.

Probe against the fixed library:

- `basic`: B gives −0.7132, 0.9533, 1.5299, 1.7206 for every m from 40 to 10,000. C: L1 on −gmat gives −0.4134, **−0.7176**, 1.1232, 1.3150, so only the sign flips. With `NormalizeFeatures = false` the fit now reaches the same optimum (E: 0.0079, like F).
- `usecase`, LR4: beta `5.845, −1.154, −1.118, 8.471, 0.667` = the converged reference C2. Training accuracy 85.01 %. 0 decisions and 300/300 polylines differ from C2 (13,576 points kept).
- `usecase`, LR7: beta `9.754, 8.707, 8.765, 12.012, −0.420, 0.276, 0.105, −13.254` = C2. Training accuracy 94.88 %. 0 decisions and 300/300 polylines differ from C2 (9,640 points kept).
- The printed "mean-gradient norm" is now √p/m (4.78e−4 for LR4, 6.33e−4 for LR7). That is expected at an L1 optimum: the probe's norm leaves out the penalty, whose subgradient is exactly λ/m per nonzero coefficient.
- `usecase`, ALL13: converged, optimality 2e−12. The probe's C2 reference never converged, so it is not a valid comparison: 0 training decisions differ, but only 82/300 polylines are identical (11,863 vs 11,293 points). In the synthetic set the duplicated columns (`BaseLength` = `dX13`, `VerticalDistance` = `dY12` = `dY23`) make the L1 optimum non-unique. The fit puts VerticalDistance's weight on `dY12` and splits BaseLength's −13.254 between `BaseLength` (−1.886) and `dX13` (−11.368); every other coefficient equals LR7's. On real polylines those columns differ, so the ALL13 model is not determined by this training set. Do not select all 13 features.
- Stale probe output: section A is labelled "current code" but is now the fixed fit, "replica" (a copy of the old loop) no longer matches the library, and the "current vs fixed" simplification rows compare the fixed library with the references.

Behavior changes worth knowing:

- Perfectly separable data without regularization has no finite optimum. The coefficients now grow until the gradient falls below the tolerance, instead of stopping at the 5,000-iteration cap. Probe D's 3-row example now predicts 1.0000 instead of 0.9974.
- The research models (LR4v23, LR7v23, LR4v23-LF) re-fit from their JSON at load time, so every research run from now on uses the corrected models.

## How to resume

Run the existing tests:

```
dotnet test tests/IRI.Maptor.Tests/IRI.Maptor.Tests.csproj --filter "FullyQualifiedName~LogisticRegressionTest"
```

Run the probe (builds the MachineLearning project by relative reference; about one minute for `usecase` because of the 400k-iteration ALL13 reference fits). Against the current code it prints the *Applied 2026-09-17* numbers; for the pre-fix numbers use commit `66d5d080a`:

```
cd docs/future-improvements/LR/probe
dotnet run -c Release -- basic     # unit-test dataset: replication, L1 sign, std mismatch, mutation, NRE
dotnet run -c Release -- usecase   # synthetic simplification training set, LR4 / LR7 / ALL13
```

Delete `bin/` and `obj/` under `probe/` afterwards; they are not meant to be committed.

Library "done" criteria, all met 2026-09-17: the replication test passes; `Predict` and `Fit` are side-effect free; the library fit equals the converged reference for LR4 / LR7 (300/300 identical output).

Research side: done 2026-09-17, see *Phase C*. To re-run it (needs the `E:` drive; about 3 minutes; writes only to the output folder you pass):

```
cd docs/future-improvements/LR/phase-c
dotnet run -c Release -- fit                     # old vs new fit of LR4v23 / LR7v23 / LR4v23-LF
dotnet run -c Release -- compare <empty folder>  # re-run of the published comparison, ~63 MB of images
python analyze.py <that folder>
```

Delete `bin/` and `obj/` under `phase-c/` afterwards.

## Phase C: research models and the published comparison (2026-09-17)

Everything on `E:\University.Ph.D` was only read. Outputs went to a scratch folder. The harness is `phase-c/`: `Program.cs`, `analyze.py`, and `OldLogisticRegression.cs`, which is the pre-fix class verbatim from `66d5d080a`.

### The v23 models

| model | features (from the JSON) | training accuracy old → new | training decisions changed |
|---|---|---|---|
| LR4v23 | Area, BaseLength, DistanceToNext, DistanceToPrevious | 91.85 % → 94.69 % | 243 of 4,182 |
| LR7v23 | Area, BaseLength, CosineOfAngle, DistanceToNext, DistanceToPrevious, SquareCosineOfAngle, VerticalDistance | 92.11 % → 94.88 % | 340 |
| LR4v23-LF | BaseLength, CosineOfAngle, SquareCosineOfAngle, VerticalDistance | 83.79 % → 86.99 % | 662 |

Always predicting "retain" scores 82.54 %. The new fits reach a max L1 subgradient ≤ 1.6e−12 in 4–21 ms. The old fits took 317–460 ms.

- All three JSON files hold exactly `SyntheticDataFactory.CreateAll()` as it is today: same 4,182 records in the same order, identical labels and feature values. So the training data can be regenerated from the repository.
- `IsRetainedFunc0_5` gap: the 18 base-0.5 records with dy = 0 are all labelled *retained* in the v23 data. For comparison, bases 1/2/4 keep 7 of 19/21/25 and bases 8/16/32 keep 17 of 33/49/81. The old LR7v23 predicts 4 of the 18 as retained, the new one 8.
- LR4v23-LF is used by the research app (`ApplicationPresenter`) but is commented out in `LRHelper.GetLRAModels`, so it is not in the published run.

### Re-run of `1403-07-17-single feature multi threshold 128`

This is `LRHelper.CompareSingleFeatureVaryingThreshold`, the comparison active in `GeneralTest`, run on 2024-10-08 with LR4v23 and LR7v23 against RDP, RW, NOPW and BOPW at 0.5×, 1× and 2× the pixel threshold.

The research method draws a new random sample (`OrderBy(Guid.NewGuid()).Take(50)`) on every run. The harness instead reuses the 348 features the published run saved as `*-F{i}-*-original.json`. It re-reads their exact Web Mercator geometry from the shapefiles by bounding box (4 features had duplicate parts with the same box) and runs the method body with old and new models side by side.

Fidelity against the published `All-summary.txt`, pre-fix models:

- **Geometry metrics** (points kept, compression, TLVD per length) are identical for 315 of 348 features, for every method. All 2,664 rows of the line layers (Railway, Road, Waterways) match. The 33 differing features are polygons (Water 315 of 900 rows, AdminArea 33, Building 27, Landuse 17) and differ for the baselines too, with equal input point counts. That points to polygon handling in the geometry code having changed since 2024, not the LR models.
- **Confusion-matrix metrics** (TP/FP/FN, accuracy, F1) match in 0 rows. True-positive counts roughly double, e.g. 462 → 1,090 for AdminArea F1, so the rasterization has changed since the published run. F1 below is comparable old vs new within this re-run, but not with the published values.
- **Geometry regression found on the way:** `Geometry.RemoveConsecutiveDuplicatePoints()` calls `ReevaluateGeometryType()`, which was rewritten in `a4029ca83` (2026-05-22). For a one-ring Polygon it throws `NotImplementedException`: it assigns `Points` while `Geometries` is still set. For a multi-ring Polygon it would return `MultiLineString`. Every `LRHelper` comparison calls it on each feature, so **the research tools currently crash on polygon layers**. The harness uses the pre-refactor behavior (remove duplicates, keep the type). Not fixed; only research code calls it.

Means over the 315 exactly reproduced features (all 348 give the same picture):

| method | compression | TLVD / length | F1 (re-run raster) | points kept |
|---|---|---|---|---|
| LR4v23 old → new | 0.427 → 0.442 | 0.0597 → 0.0733 | 0.943 → 0.937 | 37.3 → 34.9 |
| LR7v23 old → new | 0.453 → 0.445 | 0.0809 → 0.0728 | 0.930 → 0.936 | 33.7 → 34.8 |
| LR4v23-LF old → new | 0.494 → 0.438 | 0.1011 → 0.0747 | 0.898 → 0.935 | 30.7 → 35.1 |
| RDP ×0.5 / ×1 / ×2 | 0.454 / 0.554 / 0.650 | 0.0416 / 0.0889 / 0.1816 | 0.953 / 0.894 / 0.768 | 35.4 / 26.6 / 19.3 |
| RW ×0.5 / ×1 / ×2 | 0.371 / 0.469 / 0.569 | 0.0510 / 0.1003 / 0.2087 | 0.959 / 0.906 / 0.793 | 44.0 / 34.0 / 25.5 |
| NOPW ×0.5 / ×1 / ×2 | 0.338 / 0.446 / 0.560 | 0.0329 / 0.0759 / 0.1669 | 0.966 / 0.920 / 0.814 | 45.8 / 34.8 / 25.3 |
| BOPW ×0.5 / ×1 / ×2 | 0.467 / 0.565 / 0.653 | 0.0609 / 0.1370 / 0.2938 | 0.934 / 0.845 / 0.655 | 34.2 / 25.7 / 19.0 |

What moves:

- The simplification changes for 264 (LR4v23), 256 (LR7v23) and 310 (LR4v23-LF) of the 348 features.
- **The three corrected models nearly coincide:** compression 0.44, TLVD 0.073, ~35 points. The old models differed from each other mostly because of where each non-converged fit stopped. Differences between LR4/LR7/LF in the published results should not be read as feature-set effects.
- LR7v23 and LR4v23-LF get strictly better (lower TLVD, higher F1, slightly less compression). LR4v23 trades the other way: more compression, higher TLVD.
- Share of features where LR has the lower TLVD than the baseline at 1× (all 348):
  - LR7v23: RDP 71.6 → 81.6 %, RW 42.8 → 55.2 %, NOPW 33.6 → 42.8 %, BOPW 86.8 → 90.2 %.
  - LR4v23: RDP 89.1 → 80.2 %, RW 71.3 → 57.5 %, NOPW 63.2 → 45.7 %, BOPW 90.2 → 89.7 %.
  - Full tables (TLVD, F1, compression against every baseline and threshold) are in `analyze.py` output section 4.
- Old and new alike, at about the same compression, RDP ×0.5 (0.454, TLVD 0.042) and BOPW ×0.5 (0.467, TLVD 0.061) have lower TLVD than every LR model. NOPW ×1 (0.446, 0.076) is the closest match to the corrected LR models.

### Decisions for Hossein

1. Whether the paper's figures and tables come from this run (and the `256` / `multi scale` / `layerbased` runs, which can be re-run the same way), and whether to update them with the corrected models or document the change.
2. Whether the `IsRetainedFunc0_5` gap is deliberate. If not, the synthetic data and all models change again.
3. Fixing the `ReevaluateGeometryType` regression in Core.Spatial (separate change, with tests), plus deciding whether the rendering change that moved the confusion-matrix metrics was intended.
4. Whether `docs/future-improvements/LR/` (doc, probe, phase-c, which name the research drive paths) should be added to the `publish-public.ps1` deny list.
