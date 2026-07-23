# WMS — Web Map Service

A WMS server renders the map for you: `GetMap` returns finished pixels (PNG/JPEG), styled server-side. The subtlety is versioning — WMS 1.1.1 and 1.3.0 disagree about axis order for some coordinate reference systems, and this folder (namespace `IRI.Maptor.Sta.Ogc.WMS`) handles exactly that plus the typed capabilities model.

<p align="center">
  <img src="../images/wms-wfs.png" alt="WMS vs WFS" width="800">
</p>

## The capabilities model

`WMSCapabilities` is the typed shape of a `GetCapabilities` response: `Service` metadata, and a `Capability` holding the request formats and the nested `Layer` tree (each layer with its `Name`, `Title`, `Style`s and `Boundingbox`es).

```csharp
using IRI.Maptor.Sta.Ogc.WMS;

var root = capabilities.Capability.Layer;         // the layer tree
foreach (var layer in root.Layers)
    Console.WriteLine($"{layer.Name} — {layer.Title}");

var formats = capabilities.Capability.Request.GetMap.Format;   // e.g. image/png, image/jpeg
```

## Versions and axis order

In WMS 1.3.0, `EPSG:4326` means **lat,lon** — the BBOX is flipped relative to 1.1.1 (and to `CRS:84`). `WmsHelper.ParseCrs` reads a BBOX string under the right convention for the version/CRS pair and returns a normalized `BoundingBox`; `WmsConstants` carries the version and CRS identifiers.

```csharp
var bbx = WmsHelper.ParseCrs(WmsConstants.version130, WmsConstants.Epsg4326,
                             "35.5,50.8,35.9,51.6");
// 1.3.0 + EPSG:4326 is lat,lon — ParseCrs swaps it back to x/y for you
```

---
[Back to IRI.Maptor.Sta.Ogc](../README.md)
