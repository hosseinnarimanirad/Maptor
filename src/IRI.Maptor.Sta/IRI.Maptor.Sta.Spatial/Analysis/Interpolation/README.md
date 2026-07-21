# Spatial Interpolation

Estimate a value between samples: every neighbour votes, nearer neighbours vote harder.

## Inverse Distance Weighting (IDW)

`Idw.Calculate` weighs each sample by **1 / distance²** and returns the weighted average of the `Z` values. Samples farther than `maxDistance` are ignored; if none qualify, the result is `null` — IDW never invents data beyond its neighbours.

![IDW interpolation](../../images/idw-interpolation.png)

```csharp
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Analysis;

var samples = new List<PointZM>
{
    new PointZM(51.32, 35.70, 1204),   // Z carries the measured value
    new PointZM(51.45, 35.68, 1310),
    new PointZM(51.38, 35.79, 1187),
};

double? value = Idw.Calculate(samples, new Point(51.39, 35.72), maxDistance: 0.2);
// weighted average of the samples within 0.2° — null if none are in range
```

Interpolating from a TIN instead (planar, per-triangle) lives in [DigitalTerrainModeling](../DigitalTerrainModeling/README.md) — `IrregularDtm.Interpolate`.

---

📦 **NuGet**: [IRI.Maptor.Sta.Spatial](https://www.nuget.org/packages/IRI.Maptor.Sta.Spatial)

🐞 **Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)
