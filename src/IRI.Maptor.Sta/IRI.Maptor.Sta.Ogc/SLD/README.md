# SLD (Styled Layer Descriptor) C# Classes

This library provides C# classes that implement the **OGC SLD 1.0.0** specification, enabling you to create, read, and manipulate Styled Layer Descriptors in .NET.

---

## ✅ Features
- Full support for core SLD 1.0.0 structures:
  - `StyledLayerDescriptor`, `NamedLayer`, `UserStyle`, `FeatureTypeStyle`, `Rule`
  - `PointSymbolizer`, `LineSymbolizer`, `PolygonSymbolizer`, `TextSymbolizer`, `RasterSymbolizer`, and related styling elements
- Reuses the project's Filter Encoding classes (`OgcFilter`) for the `<ogc:Filter>` inside a `Rule`
- XML serialization and deserialization using `System.Xml.Serialization`
- Handles namespaces for SLD, OGC filters, and XLink references

---

## ✅ How to Use

`SldHelper` (namespace `IRI.Maptor.Sta.Ogc.SLD`) provides the read/write entry points.

### 1. Read an SLD document
```csharp
string xml = File.ReadAllText("example.sld");
StyledLayerDescriptor? sld = SldHelper.Parse(xml); // null if the XML is not a valid SLD
```

### 2. Write an SLD document
```csharp
// To a string (e.g. for a preview):
string? xml = SldHelper.Serialize(sld, indented: true);

// Directly to a file:
SldHelper.Save("example.sld", sld);
```

The namespace prefixes (`sld`, `ogc`, `xlink`, `xsi`) are emitted automatically from the
declarations `StyledLayerDescriptor` carries, so no `XmlSerializerNamespaces` is required.


## ✅ References
- [OGC Styled Layer Descriptor Specification 1.0.0](https://portal.ogc.org/files/?artifact_id=1188) 
