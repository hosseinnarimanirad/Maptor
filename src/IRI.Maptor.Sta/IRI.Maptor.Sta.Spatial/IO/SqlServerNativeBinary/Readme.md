# SQL Server native binary

Read and write SQL Server's native spatial binary format (MS-SSCLRT) — the on-wire/on-disk format used by `geometry` and `geography` columns.

## Supported capabilities

| Capability | Supported |
|---|---|
| Read | Yes — `SqlServerSpatialNativeBinary.Deserialize` |
| Write | Yes — `SqlServerSpatialNativeBinary.Serialize` |
| `geography` payloads | Yes — via the `isGeography` flag on both methods |

## Classes

| Type | Description |
|---|---|
| `SqlServerSpatialNativeBinary` | Static class (split across `SqlServerSpatialNativeBinarySerializer.cs` / `SqlServerSpatialNativeBinaryDeserializer.cs`) with `Deserialize(byte[], bool isGeography = false)` → `IGeometry` and `Serialize<T>(Geometry<T>, bool isGeography = false)` → `byte[]?`; also `DeserializeGeometryPoint` for single points |
| `SqlServerSpatialNativeBinaryTypes` | Enum matching the MS-SSCLRT shape type constants |
| `SerializationProp` / `SerializationPropHelper` | Serialization-properties flags (Z, M, validity, single point, ...) and their parser |

## Format reference

The binary format is documented in **[\[MS-SSCLRT\]](./OfficialDoc.md)** — the Microsoft SQL Server Client-Side Spatial Library Reference.

## Usage

```csharp
using IRI.Maptor.Sta.Spatial.IO.SqlServerNativeBinary;
using IRI.Maptor.Sta.Spatial.Primitives;

// Read: byte[] → IGeometry
byte[] rawBytes = /* value of a SQL Server spatial column */;
IGeometry geometry = SqlServerSpatialNativeBinary.Deserialize(rawBytes);

// Write: Geometry<T> → byte[]
var point = Geometry<Point>.Create(51.4, 35.7, srid: 4326);
byte[]? bytes = SqlServerSpatialNativeBinary.Serialize(point);
```

---
[Back to IRI.Maptor.Sta.Spatial](../../README.md)
