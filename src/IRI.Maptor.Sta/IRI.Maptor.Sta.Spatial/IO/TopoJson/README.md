# 🗺️ TopoJSON Support in Maptor

![TopoJSON](https://img.shields.io/badge/TopoJSON-Specification_compliant-blue)
![.NET](https://img.shields.io/badge/.NET-Standard_2.1-green)

A .NET Standard implementation of TopoJSON for compact representation of geographic data with shared topology, supporting read/write operations and conversion to/from Geometry types.

## ✨ Features

- **Full TopoJSON Support**  
  ✅ TopoJSON Specification compliant  
  ✅ Geometry types: `Point`, `MultiPoint`, `LineString`, `MultiLineString`, `Polygon`, `MultiPolygon`  
  ✅ `GeometryCollection` is fully supported (parsed and written) 
  ✅ Arc-based topology for eliminating redundancy
  ✅ Quantization support for size reduction
  ✅ **Power BI Shape Map ready** – writing a list of features produces a single `GeometryCollection
  
- **Conversion Tools**  
  🔄 TopoJSON ↔ Geometry<Point>  
  🔄 Automatic topology extraction  
  🔄 Shared arc deduplication  
  🔄 Properties dictionary automatically converted to proper .NET types (`int`, `double`, `string`, `bool`, etc.) 

## 📦 What is TopoJSON?

TopoJSON is an extension of GeoJSON that encodes topology. Instead of representing geometries discretely, geometries in TopoJSON files are stitched together from shared line segments called **arcs**. This results in:

- **Smaller file sizes** (often 80% smaller than GeoJSON)
- **Topology preservation** (shared boundaries stay consistent)
- **Efficient storage** (no coordinate duplication)

## ⚙️ Installation

```bash
dotnet add package IRI.Maptor.Sta.Spatial
```

## 🚀 Getting Started

### Reading TopoJSON

```csharp
using IRI.Maptor.Sta.Spatial.IO.TopoJson;

// Read from file
var topology = await TopoJson.ReadFromFileAsync("map.topojson");

// Parse from string
string topoJsonString = File.ReadAllText("map.topojson");
var topology = TopoJson.Parse(topoJsonString);

// Convert to Features (with strongly typed properties)
var features = TopoJson.ToFeature(topology, srid: 4326);
foreach (var kvp in features)
{
    Console.WriteLine($"Feature '{kvp.Key}': {kvp.Value.TheGeometry.Type}");
    if (kvp.Value.Attributes.ContainsKey("BranchId"))
    {
        int branchId = (int)kvp.Value.Attributes["BranchId"];
        Console.WriteLine($"  BranchId: {branchId}");
    }
}
```

### Writing TopoJSON (Power BI compatible)

When writing a list of features, the library automatically groups them into a single GeometryCollection – exactly what Power BI Shape Map expects.

```csharp
using IRI.Maptor.Sta.Spatial.IO.TopoJson;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

// Create features with attributes
var features = new List<Feature<Point>>();
foreach (var region in regionsData)
{
    var geometry = Geometry<Point>.Create(region.Points, GeometryType.Polygon, 4326);
    var attributes = new Dictionary<string, object>
    {
        ["Name"] = region.Name,
        ["Population"] = region.Population,
        ["IsCapital"] = region.IsCapital
    };
    features.Add(new Feature<Point>(geometry, attributes));
}

// Write to TopoJSON file (the output will contain a single "data" GeometryCollection)
await TopoJson.WriteToFileAsync(features, "output.topojson", quantize: true, quantizationFactor: 10000, collectionName: "regions");
```

### Converting Multiple Geometries with Shared Topology

If you need to write raw geometries without attributes, use FromGeometries (which also groups them into a GeometryCollection):

```csharp
var geometries = new Dictionary<string, Geometry<Point>>
{
    ["boundary"] = boundaryGeometry,
    ["roads"] = roadsGeometry,
    ["buildings"] = buildingsGeometry
};

// TopoJSON will automatically detect and share common arcs
var topology = TopoJson.FromGeometries(geometries, quantize: true);

TopoJson.WriteToFile(topology, "map.topojson");
```

## 📐 Quantization

TopoJSON supports quantization to reduce file size further:

```csharp
// High precision (larger file)
var topology1 = TopoJsonConverter.FromFeatures(features, quantize: true, quantizationFactor: 1000000);

// Lower precision (smaller file)
var topology2 = TopoJsonConverter.FromFeatures(features, quantize: true, quantizationFactor: 10000);

// No quantization (exact coordinates)
var topology3 = TopoJsonConverter.FromFeatures(features, quantize: false);
```

## 🔧 Advanced Usage

### Working with Arcs

```csharp
var topology = await TopoJson.ReadFromFileAsync("map.topojson");

Console.WriteLine($"Number of arcs: {topology.Arcs.Count}");
Console.WriteLine($"Number of objects: {topology.Objects.Count}");

// Inspect transform
if (topology.Transform != null)
{
    Console.WriteLine($"Scale: [{topology.Transform.Scale[0]}, {topology.Transform.Scale[1]}]");
    Console.WriteLine($"Translate: [{topology.Transform.Translate[0]}, {topology.Transform.Translate[1]}]");
}

// Inspect bounding box
if (topology.BBox != null)
{
    Console.WriteLine($"BBox: [{string.Join(", ", topology.BBox)}]");
}
```

### Serialization Options

```csharp
// Compact JSON (no whitespace)
var compactJson = TopoJson.Serialize(topology, indented: false);

// Pretty-printed JSON
var prettyJson = TopoJson.Serialize(topology, indented: true);
```

## 📊 File Size Comparison

Example comparison for a typical geographic dataset:

| Format | Size | Reduction |
|--------|------|-----------|
| GeoJSON (original) | 1.2 MB | - |
| TopoJSON (no quantization) | 650 KB | 46% |
| TopoJSON (quantized 10k) | 250 KB | 79% |
| TopoJSON (quantized 1k) | 180 KB | 85% |

## 🎯 Use Cases

- **Power BI Shape Maps** – Write features directly to a format ready for the Shape Map visual (single `GeometryCollection`)
- **Web mapping** - Reduce bandwidth for vector tiles
- **Data archival** - Store geographic data efficiently
- **Topology analysis** - Maintain shared boundaries
- **Network analysis** - Road/river networks with shared segments

## 🔗 Resources

- [TopoJSON Specification](https://github.com/topojson/topojson-specification)
- [TopoJSON Wiki](https://github.com/topojson/topojson/wiki)

## 📝 Notes

- TopoJSON uses delta encoding for arcs (each coordinate is relative to the previous)
- Negative arc indices indicate reversed direction
- Points and MultiPoints don't use arcs (stored as absolute coordinates)
- Quantization is lossy but often acceptable for visualization

## 🐞 Known Limitations

- Very large datasets (>100k arcs) may have slower conversion times
- Topology simplification is not yet implemented (use pre-simplified geometries)
- Only 2D coordinates are currently supported (Z and M are ignored)

