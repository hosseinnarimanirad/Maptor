# IRI.Maptor.Res.FastSimplification

A WPF research harness for evaluating fast line-simplification. It runs the simplification
algorithms implemented in IRI.Maptor.Sta.Spatial (Ramer–Douglas–Peucker, Visvalingam–Whyatt,
normal opening window, cumulative triangle routine) over OSM shapefile datasets, renders the
original versus the simplified geometry at several zoom levels and render sizes, and writes
side-by-side comparison PNGs whose file names record the compression ratio and the total
vector displacement per length. The output was produced for a conference-paper experiment on
visual quality of simplification.

## How to run

The project is a WPF executable (`net8.0-windows`). The input shapefile folder and output
folder are hard-coded in `Analysis/SimplificationHelper.cs` (`GeneralTest`) — point them at
your own data first, then:

```powershell
dotnet run --project research\IRI.Maptor.Res.FastSimplification
```

and click the button in the main window to start the batch run.

---
[Back to the solution README](../../README.md)
