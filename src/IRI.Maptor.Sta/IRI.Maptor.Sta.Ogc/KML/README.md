# KML — Keyhole Markup Language

OGC KML 2.2: a tree of placemarks, folders and shared styles for the globe. All classes live in the `IRI.Maptor.Sta.KmlFormat` namespace (not `Sta.Ogc.KML`), and KML's convention is WGS84 (SRID 4326) throughout.

<p align="center">
  <img src="../images/kml-kmz.png" alt="KML document and KMZ package" width="800">
</p>

## Reading

`KmlReader` offers two levels: geometry-only, or full-fidelity `KmlFeature` objects that keep the name, description, attributes, style and region metadata.

```csharp
using IRI.Maptor.Sta.KmlFormat;

var geometries = KmlReader.ReadFromFile("map.kml", targetSrid: 4326);   // List<IGeometry>

var features = KmlReader.ReadFeaturesFromFile("map.kml");               // List<KmlFeature>
foreach (var f in features)
    Console.WriteLine($"{f.Name}: {f.Geometry.Type}, {f.Attributes.Count} attributes");
```

`KmlReader.Parse(kmlString)` / `ParseFeatures(kmlString)` do the same from a string in memory.

## Writing

`KmlWriter` turns geometries or features into KML; `ToKmlWithFolders` groups them into `<Folder>` elements.

```csharp
string kml = KmlWriter.ToKml(geometry, name: "London", description: "Capital of England");
// <kml><Document><Placemark>…

var folders = new Dictionary<string, List<Geometry<Point>>>
    { ["Cities"] = cities, ["Rivers"] = rivers };
string grouped = KmlWriter.ToKmlWithFolders(folders, "Geographic Data");

await KmlWriter.WriteToFileAsync(geometry, "london.kml", "London");
```

If the data isn't in WGS84, pass a `projectToGeodeticFunc` to reproject on the way out.

## Styling

`KmlStyleBuilder` assembles a `StyleType` fluently; give it an id and reference it from placemarks as a shared style. `KmlDecorator` applies styles and `<ExtendedData>` to existing placemarks.

```csharp
var pinStyle = new KmlStyleBuilder()
    .WithId("pin")
    .WithIconStyle(iconHref: "http://.../red-pushpin.png", scale: 1.2)
    .WithLabelStyle(red: 255, green: 0, blue: 0, scale: 1.1)
    .Build();
// colors are KML aabbggrr — KmlStyleBuilder.CreateKmlColorFromHex("#80FF0000") handles the flip
```

## Validation

`KmlValidator` checks a document against the KML 2.2 rules — with the OGC schemas embedded in the assembly, so schema validation works offline and reports line/column context.

```csharp
bool ok = KmlValidator.Validate(kmlContent, out var errors, out var warnings);

var options = new KmlValidator.KmlValidationOptions { ValidateSchema = true };
bool schemaOk = KmlValidator.Validate(kmlContent, out errors, out warnings, options);

string report = KmlValidator.GenerateValidationReport(kmlContent);
```

## Generated schema models

The strongly typed KML 2.2 models under `Generated/` (`IRI.Maptor.Sta.KmlFormat.Primitives`, `.Atom`, `.Xal`, `.Gx`) are produced with [XmlSchemaClassGenerator](https://github.com/mganss/XmlSchemaClassGenerator) from `tools\schema\kml22.local.xsd`; the validator consumes the compiled schemas embedded from `Schemas/`. Full API inventory and implementation notes: [KML_IMPLEMENTATION_SUMMARY.md](KML_IMPLEMENTATION_SUMMARY.md).

For KML packed in a zip with its icons and images, see [../KMZ/README.md](../KMZ/README.md).

---
[Back to IRI.Maptor.Sta.Ogc](../README.md)
