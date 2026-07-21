# IRI.Maptor.Sta.Ogc

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Ogc.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.Ogc)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

OGC standards implementation for the Maptor library. Provides parsers, serializers, and client helpers for the major Open Geospatial Consortium specifications — all targeting **.NET Standard 2.1**.

---

## Standards Covered

### SFA — Simple Features Access (WKT / WKB)
- Read and write **Well-Known Text (WKT)** and **Well-Known Binary (WKB)** according to OGC SFA and ISO 19125.
- Supports all standard geometry types: Point, LineString, Polygon, Multi*, GeometryCollection.

### GML — Geography Markup Language
- Parser and serializer for **GML 2** and **GML 3** XML representations.
- Converts between GML elements and native `Geometry<T>` objects.

### KML / KMZ — Keyhole Markup Language
- Read and write **KML** (OGC KML 2.2) files.
- **KMZ** support (zipped KML with embedded assets).
- Preserves placemark names, descriptions, and style references.

### WMS — Web Map Service
- Client helpers for building **WMS 1.1 / 1.3** `GetMap`, `GetCapabilities`, and `GetFeatureInfo` request URLs.

### WFS — Web Feature Service
- Client helpers for constructing **WFS 1.0 / 1.1 / 2.0** `GetFeature` and `DescribeFeatureType` requests.
- GML feature response parsing.

### SLD — Styled Layer Descriptor
- Read and write **SLD 1.0 / 1.1** XML documents.
- Maps SLD rules to the library's `VisualParameters` / symbolizer model for rendering.

### Filter Encoding
- OGC Filter Encoding 1.1 expression model used by WFS/SLD.

---

## Installation

```bash
dotnet add package IRI.Maptor.Sta.Ogc
```

---

## Quick Start

```csharp
// Parse GML into the native geometry model
using IRI.Maptor.Sta.Ogc.GML;

var geometry = Gml3Reader.Parse(gmlXml, srid: 4326);
Console.WriteLine($"Type: {geometry.Type}");

// Parse a WMS BBOX with the right axis order for the version/CRS pair
using IRI.Maptor.Sta.Ogc.WMS;

var bbx = WmsHelper.ParseCrs(WmsConstants.version130, WmsConstants.Epsg4326,
                             "35.5,50.8,35.9,51.6");
// WMS 1.3.0 + EPSG:4326 is lat,lon — ParseCrs swaps it back to x/y
```

WKT/WKB parsing (`WktReader`, `WkbReader`) lives in the companion package `IRI.Maptor.Sta.Spatial` (`IRI.Maptor.Sta.Spatial.IO.OgcSFA` namespace); this package carries the OGC geometry object model (`IRI.Maptor.Sta.Ogc.SFA`).

---

## Project Structure

```
Sta.Ogc/
├── Common/           # Shared OGC types and helpers
├── Extensions/       # Extension methods
├── FilterEncoding/   # OGC Filter Encoding 1.1 model
├── GML/              # GML 2 / GML 3 parser & serializer
├── KML/              # KML 2.2 reader / writer
├── KMZ/              # KMZ (zipped KML) support
├── SFA/              # WKT / WKB (Simple Features Access)
├── SLD/              # Styled Layer Descriptor reader / writer
├── WFS/              # WFS request builder & response parser
└── WMS/              # WMS request URL builder
```

---

📦 **NuGet**: [IRI.Maptor.Sta.Ogc](https://www.nuget.org/packages/IRI.Maptor.Sta.Ogc)

🐞 **Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)
