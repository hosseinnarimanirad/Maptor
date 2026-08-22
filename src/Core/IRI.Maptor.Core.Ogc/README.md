# IRI.Maptor.Core.Ogc

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.Ogc?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Core.Ogc/)
[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

Open Geospatial Consortium (OGC) standards support for the Maptor stack: parsers, writers, and object models for KML/KMZ, GML, WMS, WFS, SLD, Filter Encoding, and the Simple Features Access geometry model.

## Installation

```bash
dotnet add package IRI.Maptor.Core.Ogc
```

## Features

- KML 2.2 read and write (`KmlReader`, `KmlWriter`) plus KMZ archives (`KmzReader`, `KmzWriter`), with style building (`KmlStyleBuilder`) and document validation (`KmlValidator`)
- GML 2.1.2 and 3.1.1 readers and writers (`Gml2Reader`/`Gml2Writer`, `Gml3Reader`/`Gml3Writer`) converting between GML XML and the native `IGeometry` model
- WMS 1.1.1/1.3.0 `GetCapabilities` document model (`WmsGetCapabilities`) and BBOX/CRS parsing with correct axis order per version (`WmsHelper.ParseCrs`)
- WFS 1.1.0 schema object model (`WFSv110`)
- SLD 1.0.0 object model with parse/serialize/save helpers (`SldHelper`)
- OGC Filter Encoding expression model
- Simple Features Access (SFA) geometry object model (`IOgcGeometry`, `OgcLineString`, `OgcLinearRing`, …)

Note: WKT/WKB parsing (`WktReader`, `WkbReader`) lives in the companion package `IRI.Maptor.Core.Spatial` (`IRI.Maptor.Core.Spatial.IO.OgcSFA` namespace); this package carries the OGC geometry object model.

## Usage

Read KML/KMZ — note the namespace is `IRI.Maptor.Core.Ogc.Kml`, not `IRI.Maptor.Core.Ogc.KML`:

```csharp
using IRI.Maptor.Core.Ogc.Kml;

var geometries = KmlReader.ReadFromFile("places.kml", targetSrid: 4326);
var fromKmz = KmzReader.ReadFromFile("archive.kmz");
```

Parse GML into the native geometry model:

```csharp
using IRI.Maptor.Core.Ogc.GML;

var geometry = Gml3Reader.Parse(gmlXml, srid: 4326);
```

Parse a WMS BBOX with the right axis order for the version/CRS pair:

```csharp
using IRI.Maptor.Core.Ogc.WMS;

var bbx = WmsHelper.ParseCrs(WmsConstants.version130, WmsConstants.Epsg4326,
                             "35.5,50.8,35.9,51.6");
// WMS 1.3.0 + EPSG:4326 is lat,lon — ParseCrs swaps it back to x/y
```

## See also

- [KML](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Ogc/KML/README.md)
- [KMZ](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Ogc/KMZ/README.md)
- [GML](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Ogc/GML/README.md)
- [WMS](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Ogc/WMS/README.md)
- [WFS](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Ogc/WFS/README.md)
- [SLD](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Ogc/SLD/README.md)

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Core.Ogc/) · [Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) · [Back to IRI.Maptor.Core](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/README.md)
