# SQL Server Native Binary Format

Read and write SQL Server's native spatial binary format (MS-SSCLRT) — the on-wire/on-disk format used by `geometry` and `geography` columns.

## Classes

| Class | Direction | Description |
|---|---|---|
| `SqlServerSpatialNativeBinaryDeserializer` | Read | Parse a raw byte array (MS-SSCLRT) into a Maptor `Geometry<Point>` |
| `SqlServerSpatialNativeBinarySerializer` | Write | Serialize a Maptor `Geometry<Point>` to a raw MS-SSCLRT byte array |
| `SqlServerSpatialNativeBinaryTypes` | — | Enum and type constants matching the MS-SSCLRT specification |
| `SerializationProp` / `SerializationPropHelper` | — | Internal serialization property helpers |

## Format Reference

The binary format is documented in **[\[MS-SSCLRT\]](./OfficialDoc.md)** — the Microsoft SQL Server Client-Side Spatial Library Reference.

## Usage

```csharp
using IRI.Maptor.Sta.Spatial.IO.SqlServerNativeBinary;

// Read: byte[] → Geometry<Point>
byte[] rawBytes = /* from SQL Server spatial column */;
var geometry = SqlServerSpatialNativeBinaryDeserializer.Deserialize(rawBytes);

// Write: Geometry<Point> → byte[]
var bytes = SqlServerSpatialNativeBinarySerializer.Serialize(geometry);
```
