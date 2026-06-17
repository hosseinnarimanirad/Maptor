# IRI.Maptor.Ket.GdiPlus

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.GdiPlus.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.GdiPlus)
[![.NET](https://img.shields.io/badge/.NET-8.0--windows-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)

A **.NET 8 (Windows)** library that bridges GDI+/System.Drawing with the Maptor spatial stack — providing georeferenced raster I/O, digital image processing, and satellite imagery helpers.

---

## Features

### Raster / Geo-tagged Image I/O
- `GeoRasterFileDataSource` — load georeferenced raster files (GeoTIFF, Worldfile) as a spatial data source
- `ClusteredGeoTaggedImageSource` — data source for clustered sets of geo-tagged images

### Digital Image Processing
- `ImageHelper` / `DoubleBitmap` — GDI+ bitmap utilities with high-precision pixel access
- Pixel value types: `ArgbValues`, `ByteArgbValues`, `RgbValues`, `ByteRgbValues`
- `ImageMatrix` — matrix representation of raster data for numerical processing
- `Conversion` — helpers for converting between image formats and numeric arrays
- `SpatialDomainEnhancement` — spatial filtering (e.g. sharpening, smoothing)
- `ImageMatching` — image similarity / template matching
- `RemoteSensing` — satellite and aerial imagery analysis utilities

---

## Installation

```bash
dotnet add package IRI.Maptor.Ket.GdiPlus
```

> Requires Windows (uses `System.Drawing.Common` / GDI+).

---

## Project Structure

```
Ket.GdiPlus/
├── IO/
│   ├── GeoRasterFileDataSource.cs
│   └── ClusteredGeoTaggedImageSource.cs
├── DigitalImageProcessing/
│   ├── ImageHelper.cs
│   ├── DoubleBitmap.cs
│   ├── ImageMatrix.cs
│   ├── ImageMatching/
│   ├── RemoteSensing/
│   └── SpatialDomainEnhancement/
├── Model/
│   ├── ArgbValues.cs
│   ├── ByteArgbValues.cs
│   ├── RgbValues.cs
│   ├── ByteRgbValues.cs
│   └── Conversion.cs
├── Persistence/
└── Extensions/
```

---

📦 **NuGet**: [IRI.Maptor.Ket.GdiPlus](https://www.nuget.org/packages/IRI.Maptor.Ket.GdiPlus)

🐞 **Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)
