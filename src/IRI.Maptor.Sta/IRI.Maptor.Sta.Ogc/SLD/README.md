# SLD (Styled Layer Descriptor) C# Classes

This library provides C# classes that implement the **OGC SLD 1.0.0** specification, enabling you to create, read, and manipulate Styled Layer Descriptors in .NET.

---

## ✅ Features
- Full support for core SLD 1.0.0 structures:
  - `StyledLayerDescriptor`, `NamedLayer`, `UserStyle`, `FeatureTypeStyle`, `Rule`
  - `PointSymbolizer`, `LineSymbolizer`, `PolygonSymbolizer`, and related styling elements
- XML Serialization and Deserialization using `System.Xml.Serialization`
- Handles namespaces for SLD, OGC filters, and XLink references

---

## ✅ How to Use

### 1. Deserialize an existing SLD file
```csharp
var serializer = new XmlSerializer(typeof(StyledLayerDescriptor));
StyledLayerDescriptor sld;

using (var stream = File.OpenRead("example.sld"))
{
    sld = (StyledLayerDescriptor)serializer.Deserialize(stream);
}
```


## ✅ References
- [OGC Styled Layer Descriptor Specification 1.0.0](https://portal.ogc.org/files/?artifact_id=1188) 
