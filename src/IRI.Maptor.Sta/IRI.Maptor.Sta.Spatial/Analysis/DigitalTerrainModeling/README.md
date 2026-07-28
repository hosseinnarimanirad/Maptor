# Digital terrain modeling

Two representations of a surface, convertible in both directions: `RegularDtm` (a grid DEM over a `Matrix`) and `IrregularDtm` (a TIN built on Delaunay triangulation).

> Namespace note: these classes live in `IRI.Maptor.Sta.Spatial.DigitalTerrainModeling` — no `.Analysis` segment.

## RegularDtm — grid DEM

A raster of elevations with a cell size and a lower-left anchor. It does grid arithmetic (difference of two DEMs of the same region, or against a finer-resolution one), derives **slope** and **aspect** matrices from finite differences, and exports Esri ASCII GRD.

```csharp
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.DigitalTerrainModeling;

var dem = new RegularDtm(elevations, cellSize: 30, lowerLeft: new Point(500000, 3950000));

var slope  = dem.GetSlopeMatrix();
var aspect = dem.GetAspectMatrix();
var change = dem.Difference(otherDem);          // elevation change of the same region

dem.SaveAsGRD("dem.grd", noDataValue: -9999);
```

Grid → TIN goes through **significant-point selection**: keep only the cells that carry the surface shape, then triangulate.

```csharp
var tinByCag = dem.ToIrregularDtmBasedOnCAG(numberOfPoints: 500); // Chen & Guevara 1987
var tinByLi  = dem.ToIrregularDtmBasedOnLi(threshold: 2.0);       // second-difference threshold
```

## IrregularDtm — TIN

Scattered `(east, north, value)` samples triangulated into a TIN. Elevation at any location is planar interpolation inside the containing triangle; each triangle also reports its own slope and aspect, and the whole surface integrates to a volume.

```csharp
var tin = new IrregularDtm(east, north, value);

double h = tin.Interpolate(new Point(51.39, 35.70));   // NaN outside the TIN
double v = tin.CalculateVolume(baseHeight: 1200);      // cut volume above a datum

var grid = tin.ToRegularDtm(cellSize: 10);             // TIN → raster
```

---

**NuGet**: [IRI.Maptor.Sta.Spatial](https://www.nuget.org/packages/IRI.Maptor.Sta.Spatial)

**Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)

[Back to Spatial analysis](../README.md)
