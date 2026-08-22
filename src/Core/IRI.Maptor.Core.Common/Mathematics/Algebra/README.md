# Linear algebra

Dense linear algebra primitives for the Maptor suite. The central type is `Matrix`
(real, dense, `double`-based), used across the library for coordinate
transformations, geometry predicates (Delaunay in-circle tests), SIFT keypoint
localization, and PCA.

## Contents

| File | Purpose |
|---|---|
| `Matrix.cs` | Dense real matrix: arithmetic, determinant/inverse, submatrix editing, Jacobi eigen solver, rotation-matrix factories, convolution helpers |
| `Vector.cs` | Simple dense vector companion |
| `EigenvaluesEigenvectors.cs` | Result container for `Matrix.GetEigenvaluesEigenvectors()` |
| `Exceptions.cs` | `NonSquareMatrixException`, `UnequalMatrixSizeException`, `ImproperMatrixSizeForMultiplicationException`, `OutOfBoundIndexException`, `NumberOfElementsException`, `IllegalInputException` |

## Matrix storage layout — read this first

Storage is **column-major jagged**: `Element[column][row]`. This has one
important consequence for the two array constructors:

```csharp
// double[,]  — first index is the ROW (what you'd write on paper):
var a = new Matrix(new double[,] { { 1, 2 },
                                   { 3, 4 } });   // a[0,1] == 2

// double[][] — each inner array is a COLUMN:
var b = new Matrix(new double[][] { new double[] { 1, 3 },     // column 0
                                    new double[] { 2, 4 } });  // column 1
// b equals a: b[0,1] == 2
```

Writing "rows" into the `double[][]` constructor silently produces the
transpose — a historical source of bugs (see `RotateAboutX`'s history).

## API overview

- **Construction / factories** — `Matrix(int row, int column)` (zeros),
  `Matrix(double[,])`, `Matrix(double[][])`, `Zeros`, `Ones`, `Identity`,
  `DiagonalMatrix`, `ScalarMatrix`, `RotateAboutX/Y/Z`, `Rotate(ω, φ, κ)`
  (photogrammetric convention).
- **Arithmetic** — `Multiply` / `*`, `Add` / `+`, `Subtract` / `-`,
  `DotMultiply` (Hadamard), scalar `Multiply` / `scalar *`, unary `-`
  (non-mutating), `Negate()` (**in place**).
- **Determinant & inverse** — `Determinant`, `Inverse()`, `Adjoint()`,
  `CofactorOf`, `MinorOf`, `CofactorMatrixOf`, `LeftInverse`, `RightInverse`,
  `LU` (no pivoting — prefer `Determinant`/`Inverse`, which pivot internally).
- **Structure editing** — `GetRow/SetRow`, `GetColumn/SetColumn`,
  `InsertRow/InsertColumn`, `RemoveRow/RemoveColumn`, `SwapRows/SwapColumns`,
  `SubMatrix`, `Transpose`, `Clone`.
- **Eigen (symmetric only)** — `GetEigenvaluesEigenvectors()`,
  `CalculateEigenvector()`, `CalculateEigenvalues()`.
- **Predicates** — `IsSquare`, `IsSymmetric`, `IsDiagonal`, `IsOrthogonal`,
  `IsSingular`, `IsRowMatrix`, `IsColumnMatrix`.
- **Image processing** — static `CrossCorrelate`, `Convolve`.

## Algorithms and precision

- **Determinant**: closed-form expansion for n ≤ 3; LU factorization with
  partial pivoting for n ≥ 4 (O(n³)). The n ≤ 3 forms reproduce classic
  first-row cofactor expansion **bit-for-bit** — sign-sensitive geometry
  predicates (`TopologyUtility.GetPointCircleRelation`) depend on the exact
  3×3 result. A singular matrix yields exactly `0.0`.
- **Inverse**: closed adjugate forms for n ≤ 3; LU solve of `A·xⱼ = eⱼ` per
  column for n ≥ 4.
- **Eigen**: cyclic Jacobi rotations, applied as O(n) in-place Givens updates;
  converges when the absolute off-diagonal sum drops below 1e-10, capped at
  100 sweeps. Input must be symmetric (`NotImplementedException` otherwise).
- **Equality**: `operator ==` compares element-wise after rounding both sides
  to **10 decimal places** (an absolute tolerance — near-equal values of very
  large magnitude may still compare unequal).

## Behavior notes

- `Negate()` mutates the matrix **in place** and returns `this`; the unary `-`
  operator and `Negative()` return a new matrix.
- `Inverse()` of a **singular** matrix does not throw — it returns an
  Infinity/NaN-filled matrix (`1/0 · adjugate`), matching the definition.
  Check `Determinant == 0` first if you need to reject singular input.
- `GetColumn` returns the **live internal column array** (no copy): mutating it
  mutates the matrix. `GetRow` returns a fresh copy.
- `Equals`/`GetHashCode` compare via the culture-dependent `ToString()` and are
  therefore **not** consistent with `operator ==`'s rounding tolerance; prefer
  `==` for numeric comparison.
- `ToString()` format: elements separated by `,`, rows terminated by `;` —
  `{{1,2},{3,4}}` → `"1,2;3,4;"` (current-culture formatting).

## Performance

The 2026-07 optimization pass (raw-array kernels, LU, in-place Jacobi) measured
these speedups against the previous implementation (Release, .NET 8):

| Operation | Speedup | Allocations |
|---|---|---|
| 3×3 determinant + inverse (SIFT keypoint pattern) | ~22× | 34× fewer |
| 5×5 determinant / inverse | ~30× / ~79× | 158× / 490× fewer |
| 200×200 multiply | ~4× | bit-identical results |
| 3×3 / 6×6 symmetric eigen | ~2× / ~6× | 11× / 82× fewer |

## Tests

Characterization tests (43) live in
`tests/IRI.Maptor.Tests/Common/MatrixTest.cs` and lock in exact 3×3
determinant values, the in-circle sign contract, inverse/adjoint values,
eigenvalue–eigenvector pairing, tolerance boundaries, and exception types:

```bash
dotnet test tests/IRI.Maptor.Tests/IRI.Maptor.Tests.csproj --filter "FullyQualifiedName~MatrixTest"
```

---
[Back to IRI.Maptor.Core.Common](../../README.md)
