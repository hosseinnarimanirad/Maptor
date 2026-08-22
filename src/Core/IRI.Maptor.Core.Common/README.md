# IRI.Maptor.Core.Common

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.Common?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Core.Common/)
[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

The foundational package of the Maptor GIS stack. It provides the core geometric primitives, abstractions, data structures, mathematics, unit types, and helper utilities that every other `IRI.Maptor.*` package builds on.

## Installation

```bash
dotnet add package IRI.Maptor.Core.Common
```

## Features

- Geometric primitives: `Point`, `PointM`, `PointZ`, `PointZM`, `BoundingBox`, `LineSegment`, `PointCollection`
- Core abstractions, enums, attributes, and JSON converters shared across the stack
- Data structures: binary heaps (min/max), binary search tree, red-black tree, interval tree, order-statistic tree, B-tree, disjoint set, sort algorithms
- Mathematics: `Matrix`, `Vector`, and eigenvalue/eigenvector computation (`IRI.Maptor.Core.Common.Mathematics`), plus statistics models
- Linear and angular unit types with conversions: `Meter`, `Foot`, `Mile`, `Yard`, `Inch`, `Rod`, `Chain`, `Degree`, `Radian`, `Grade` (`IRI.Maptor.Core.Common.Metrics`)
- Encodings: Base64 URL encoding and Persian DOS code page conversion
- Helpers for I/O, JSON/XML, HTTP transport, hex strings, zip archives, randomness, and secure strings
- Extension methods for common BCL types (strings, numbers, dates, collections)
- Response/contract models for external map services (Google, Bing, Here, Mapzen)

## Usage

Working with bounding boxes:

```csharp
using IRI.Maptor.Core.Common.Primitives;

var bbox = new BoundingBox(xMin: 50.8, yMin: 35.5, xMax: 51.6, yMax: 35.9);

Console.WriteLine(bbox.Center);   // (51.2, 35.7)
Console.WriteLine(bbox.Width);    // 0.8

var merged = bbox.Add(new BoundingBox(50.0, 35.0, 51.0, 36.0));
var grown  = bbox.Expand(1.1);    // scale around the center
```

Converting between linear units:

```csharp
using IRI.Maptor.Core.Common.Metrics;

var distance = new Meter(1609.344);
var miles = (Mile)distance;       // explicit conversion between unit types
```

Basic linear algebra:

```csharp
using IRI.Maptor.Core.Common.Mathematics;

var m = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });
```

## See also

- [Algebra: Matrix and Vector](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Common/Mathematics/Algebra/README.md)

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Core.Common/) · [Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) · [Back to IRI.Maptor.Core](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/README.md)
