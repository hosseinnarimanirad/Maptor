# IRI.Maptor.Samples.Core

A cookbook for the UI-free Maptor libraries (`IRI.Maptor.Core.*`). Every sample is one file with
one `[Sample]`-attributed static method; `Program.cs` discovers them by reflection, lists them, and
runs the one you name. There are no input files and no absolute paths — samples generate what
they need and write any output to the temp folder.

## How to run

```bash
dotnet run --project samples/IRI.Maptor.Samples.Core                   # list the samples
dotnet run --project samples/IRI.Maptor.Samples.Core -- geodesy/precision
dotnet run --project samples/IRI.Maptor.Samples.Core -- all
```

## Samples

| Id | File | Shows |
|---|---|---|
| `geodesy/precision` | [Geodesy/CoordinatePrecision.cs](Geodesy/CoordinatePrecision.cs) | Ground distance of one decimal place of latitude/longitude ([README](Geodesy/README.md)) |
| `geodesy/web-mercator-resolution` | [Geodesy/WebMercatorResolution.cs](Geodesy/WebMercatorResolution.cs) | Metres per pixel and scale per tile zoom level ([README](Geodesy/README.md)) |
| `graph/algorithms` | [Graph/GraphAlgorithms.cs](Graph/GraphAlgorithms.cs) | BFS, DFS, topological sort, SCC, MST ([README](Graph/README.md)) |
| `formats/geojson-vs-shapefile` | [SpatialFormats/GeoJsonVsShapefile.cs](SpatialFormats/GeoJsonVsShapefile.cs) | File size of the same polygons as GeoJSON and as Shapefile ([README](SpatialFormats/README.md)) |

## Adding a sample

```csharp
using IRI.Maptor.Samples.Core.Runner;

public static class MySample
{
    [Sample("area/my-sample", "One-line title")]
    public static void Run()
    {
        // ...
    }
}
```

Put the file in a topic folder, add the row above and a README in the topic folder.

---
[Back to the samples index](../README.md)
