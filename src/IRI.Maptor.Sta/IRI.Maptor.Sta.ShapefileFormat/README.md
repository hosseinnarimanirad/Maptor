[![NuGet Version](https://img.shields.io/nuget/v/IRI.Maptor.Sta.ShapefileFormat?color=blue&logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Sta.ShapefileFormat/)
[![License](https://img.shields.io/github/license/hosseinnarimanirad/Maptor)](LICENSE)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

A comprehensive .NET Standard 2.1 library for reading, writing, and converting ESRI Shapefile formats with advanced geometry support, international character encoding, and coordinate system transformations.

## ✨ Features

### 📁 Supported File Formats
- ✔️ **SHP** - Geometry storage with full ESRI specification compliance
- ✔️ **DBF** - Attribute data with advanced encoding support
- ✔️ **SHX** - Shape index for fast spatial queries
- ✔️ **PRJ** - Projection and coordinate system information
- ✔️ **CPG** - Code page detection for character encoding

### 🔷 Geometry Types
| Point Types | Polyline Types | Polygon Types | Multipoint Types |
|------------|---------------|--------------|-----------------|
| Point      | Polyline      | Polygon      | Multipoint      |
| PointM     | PolylineM     | PolygonM     | MultipointM     |
| PointZ     | PolylineZ     | PolygonZ     | MultipointZ     |

### 🌍 Advanced Capabilities
- **Format Conversion**: Seamless conversion between ESRI types and standard formats:
  - **WKT** (Well-Known Text)
  - **WKB** (Well-Known Binary)
  - **GeoJSON** support
- **Internationalization**: Advanced DBF file encoding support for global character sets
- **Performance**: Memory-efficient streaming API for large files
- **Type Safety**: Strongly-typed attribute data handling with custom mapping
- **Coordinate Systems**: Full projection and coordinate transformation support
- **Spatial Indexing**: Built-in spatial indexing for fast spatial queries

## ⚙️ Installation

### Package Manager
```bash
Install-Package IRI.Maptor.Sta.ShapefileFormat
```

### .NET CLI
```bash
dotnet add package IRI.Maptor.Sta.ShapefileFormat
```

### PackageReference
```xml
<PackageReference Include="IRI.Maptor.Sta.ShapefileFormat" Version="2.8.6" />
```

## 💻 Usage
### Reading a Shapefile

```csharp
using IRI.Maptor.Sta.ShapefileFormat;

var esriShapes = await Shapefile.ReadShapesAsync("path/to/file.shp");

foreach (var shape in esriShapes)
{
    Console.WriteLine($"Geometry: {shape.AsSqlServerWkt()}");            
}
```

