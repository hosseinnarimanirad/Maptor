# Research projects

WPF harnesses that produced the experiments of Hossein Narimani Rad's PhD thesis (University of Tehran)
on online simplification of spatial lines and polygons for visualization. The algorithms, the machine
learning model and the measures are in the library (`src/Core/IRI.Maptor.Core.Spatial`,
`src/Core/IRI.Maptor.Core.MachineLearning`, rendering and the pixel confusion matrix in
`src/Presentation/IRI.Maptor.Presentation.Wpf/Helpers/SimplificationHelper.cs` and
`src/Infrastructure/IRI.Maptor.Infrastructure.GdiPlus/IO/ImageHelper.cs`); these projects only drive them
over the OSM datasets. Folder names are cited in the papers and must not change.

| Project | Paper | Contribution |
|---|---|---|
| [`IRI.Maptor.Res.LRSimplification`](IRI.Maptor.Res.LRSimplification/README.md) | LRS paper (logistic-regression simplification, under revision) | labelling and model-building tool; the LRS-versus-RDP/RW/NOPW/BOPW comparisons with the pixel F1 metric |
| [`IRI.Maptor.Res.TrajectoryCompression`](IRI.Maptor.Res.TrajectoryCompression/README.md) | Journal of Spatial Science assessment paper | ten algorithms × seven OSM layers × eleven thresholds × six McMaster measures |
| [`IRI.Maptor.Res.FastSimplification`](IRI.Maptor.Res.FastSimplification/README.md) | ISPRS 2025 conference paper (CTR) | visual comparison of CTR with RDP, VW and NOPW |
| `IRI.Maptor.Res.Sfc` | — | space-filling curve experiments, unrelated to the simplification work |

Tests that pin the published algorithms down: `tests/IRI.Maptor.Tests/MachineLearning/LogisticSimplificationTest.cs`
(LRS traversal and training data), `tests/IRI.Maptor.Tests/Spatial/CumulativeTriangleRoutineTest.cs` (CTR),
`tests/IRI.Maptor.Tests/Spatial/VisualSimplificationTest.cs` (RDP, PD, APSC),
`tests/IRI.Maptor.Tests/MachineLearning/LogisticRegressionTest.cs` (the solver).

---
[Back to the solution README](../README.md)
