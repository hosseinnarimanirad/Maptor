# IRI.Maptor.Infrastructure.GdiPlus

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Infrastructure.GdiPlus?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Infrastructure.GdiPlus/)
[![Target](https://img.shields.io/badge/net8.0--windows-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

Bridges GDI+/System.Drawing with the Maptor spatial stack — georeferenced raster data sources
(worldfile-based), EXIF geotag reading/writing, and digital image processing utilities used by the
Maptor desktop applications.

## Installation

```bash
dotnet add package IRI.Maptor.Infrastructure.GdiPlus
```

Requires Windows (uses `System.Drawing` / GDI+).

## Features

- `GeoRasterFileDataSource` — raster data source for worldfile-georeferenced images
- `ClusteredGeoTaggedImageSource` — data source for clustered sets of geo-tagged images
- `WorldfileManager` / `WorldfilePyramid` — read/write ESRI worldfiles and build worldfile-based tile pyramids
- `ImageHelper` — EXIF geotag read/write (`GetLatitude`, `GetLongitude`, `GetAltitude`, `SaveGeoTagInfo`), bitmap-matrix conversion, image differencing and confusion-matrix comparison, bitmap overlay
- `DoubleBitmap` — high-precision pixel access over GDI+ bitmaps
- Pixel value types (`ArgbValues`, `ByteArgbValues`, `RgbValues`, `ByteRgbValues`) and `Conversion` helpers between images and numeric arrays
- `ImageMatrix` — matrix representation of raster data for numerical processing
- Spatial-domain enhancement: `GaussianConvolution`, `RadiometricEnhancement`, `GeometricEnhancement`
- SIFT image matching (`ScaleInvariantFeatureTransform`, `SiftImageMatching`)
- Remote sensing: principal component transformation

## Usage

```csharp
using IRI.Maptor.Infrastructure.GdiPlus.Helpers;
using IRI.Maptor.Infrastructure.GdiPlus.WorldfileFormat;

// load a worldfile-georeferenced image
var geoImage = await WorldfileManager.ReadWorldfileAsync(@"c:\data\map.png", srid: 4326);

// read the EXIF geotag of a photo
using var bitmap = new System.Drawing.Bitmap(@"c:\photos\p1.jpg");
double? latitude = ImageHelper.GetLatitude(bitmap);
double? longitude = ImageHelper.GetLongitude(bitmap);
```

## Limitations

- Windows only (`System.Drawing.Common` / GDI+).
- Georeferencing is worldfile-based; embedded GeoTIFF tags are not read.

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Infrastructure.GdiPlus/) ·
[Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) ·
[Back to IRI.Maptor.Infrastructure](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Infrastructure/README.md)
