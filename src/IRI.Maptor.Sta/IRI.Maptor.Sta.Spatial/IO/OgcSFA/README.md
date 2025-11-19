# Differences Between OGC WKT and SQL Server WKT

This document explains the key differences between **OGC WKT** (Well-Known Text) and **SQL Server WKT** formats, and how this library handles both formats.

## Overview

The library provides separate readers and writers for OGC WKT and SQL Server WKT formats:
- **OGC WKT**: `WktReader` and `WktWriter` - Compliant with OGC Simple Feature Access (SFA) specification
- **SQL Server WKT**: `SqlServerWktReader` and `SqlServerWktWriter` - Compatible with Microsoft SQL Server's geometry format

## Key Differences

### 1. Dimension Suffixes

**OGC WKT** uses explicit dimension suffixes after the geometry type name:
- `POINT Z (1 2 3)` - 3D point with Z coordinate
- `POINT M (1 2 100)` - Point with M (measure) coordinate
- `POINT ZM (1 2 3 100)` - 4D point with both Z and M coordinates
- `LINESTRING Z (0 0 0, 1 1 1, 2 2 2)` - 3D linestring

**SQL Server WKT** does not use dimension suffixes. Dimension is inferred from the coordinate count:
- `POINT (1 2)` - 2D point (2 coordinates)
- `POINT (1 2 3)` - 3D point (3 coordinates, assumed Z)
- `POINT (1 2 100)` - Point with M coordinate (3 coordinates, assumed M)
- `POINT (1 2 3 100)` - 4D point (4 coordinates, assumed ZM)
- `LINESTRING (0 0 0, 1 1 1, 2 2 2)` - 3D linestring

**Example Comparison:**

| OGC WKT                       | SQL Server WKT              |
|-------------------------------|-----------------------------|
| `POINT Z (1 2 3)`             | `POINT (1 2 3)`             |
| `POINT M (1 2 100)`           | `POINT (1 2 100)`           |
| `POINT ZM (1 2 3 100)`        | `POINT (1 2 3 100)`         |
| `LINESTRING Z (0 0 0, 1 1 1)` | `LINESTRING (0 0 0, 1 1 1)` |

### 2. Dimension Detection

**OGC WKT Reader:**
- Detects dimension from the type suffix (`Z`, `M`, `ZM`)
- Removes the suffix to get the base geometry type
- Validates that coordinates match the declared dimension

**SQL Server WKT Reader:**
- Detects dimension by examining the first coordinate set
- 2 coordinates = 2D
- 3 coordinates = 3D (assumed Z)
- 4 coordinates = 4D (assumed ZM)
- No suffix removal needed

### 3. MULTIPOINT Format

**OGC WKT** requires nested parentheses for MULTIPOINT:
```
MULTIPOINT (((1 2)), ((3 4)), ((5 6)))
```

**SQL Server WKT** allows a simpler format:
```
MULTIPOINT ((1 2), (3 4), (5 6))
```

The SQL Server reader supports both formats for compatibility.
 

## Usage Examples

### Reading OGC WKT

```csharp
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;

// OGC format with explicit dimension suffix
string ogcWkt = "POINT Z (1 2 3)";
var geometry = WktReader.Parse(ogcWkt);

// Convert to OGC WKT string
string output = geometry.AsWkt(); // Returns: "POINT Z (1 2 3)"
```

### Reading SQL Server WKT

```csharp
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;

// SQL Server format without dimension suffix
string sqlWkt = "POINT (1 2 3)";
var geometry = SqlServerWktReader.Parse(sqlWkt);

// Convert to SQL Server WKT string
string output = geometry.AsSqlServerWkt(); // Returns: "POINT (1 2 3)"
```

### Converting Between Formats

```csharp
// Read SQL Server format
var geometry = SqlServerWktReader.Parse("POINT (1 2 3)");

// Write as OGC format
string ogcFormat = geometry.AsWkt(); // Returns: "POINT Z (1 2 3)"

// Write as SQL Server format
string sqlFormat = geometry.AsSqlServerWkt(); // Returns: "POINT (1 2 3)"
```

## Complete Examples

### Point Examples

**OGC WKT:**
```
POINT (1 2)                    // 2D
POINT Z (1 2 3)                // 3D with Z
POINT M (1 2 100)              // 2D with M
POINT ZM (1 2 3 100)           // 4D with Z and M
```

**SQL Server WKT:**
```
POINT (1 2)                    // 2D
POINT (1 2 3)                  // 3D (assumed Z)
POINT (1 2 100)                // 3D (assumed M)
POINT (1 2 3 100)              // 4D (assumed ZM)
```

### LineString Examples

**OGC WKT:**
```
LINESTRING (1 1, 2 2, 3 3)
LINESTRING Z (0 0 0, 1 1 1, 2 2 2)
LINESTRING ZM (0 0 0 0, 1 1 1 1, 2 2 2 2)
```

**SQL Server WKT:**
```
LINESTRING (1 1, 2 2, 3 3)
LINESTRING (0 0 0, 1 1 1, 2 2 2)
LINESTRING (0 0 0 0, 1 1 1 1, 2 2 2 2)
```

### Polygon Examples

**OGC WKT:**
```
POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))
POLYGON Z ((0 0 0, 10 0 0, 10 10 0, 0 10 0, 0 0 0))
```

**SQL Server WKT:**
```
POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))
POLYGON ((0 0 0, 10 0 0, 10 10 0, 0 10 0, 0 0 0))
```

### MultiPoint Examples

**OGC WKT:**
```
MULTIPOINT (((1 2)), ((3 4)), ((5 6)))
MULTIPOINT Z (((0 0 0), (1 1 1), (2 2 2)))
```

**SQL Server WKT:**
```
MULTIPOINT ((1 2), (3 4), (5 6))
MULTIPOINT ((0 0 0), (1 1 1), (2 2 2))
```

## When to Use Which Format

### Use OGC WKT When:
- Interoperating with other GIS systems (PostGIS, GeoServer, etc.)
- Following OGC standards strictly
- You need explicit dimension declaration
- Working with external APIs that expect OGC format

### Use SQL Server WKT When:
- Interacting with Microsoft SQL Server spatial data
- Using SQL Server's `STGeomFromText()` or `STGeomFromWKT()` functions
- Reading geometry data directly from SQL Server
- You prefer a more compact format without dimension suffixes

## Implementation Details

## Related Classes

- `WktReader` - Parses OGC-compliant WKT strings
- `WktWriter` - Writes OGC-compliant WKT strings (via `Geometry<T>.AsWkt()`)
- `SqlServerWktReader` - Parses SQL Server WKT strings
- `SqlServerWktWriter` - Writes SQL Server WKT strings (via `Geometry<T>.AsSqlServerWkt()`)
- `WktHelpers` - Shared helper methods for both formats

## References

- [OGC Simple Feature Access Specification](https://www.ogc.org/standards/sfa)
- [SQL Server Spatial Data Types](https://docs.microsoft.com/en-us/sql/relational-databases/spatial/spatial-data-types-overview)
- [SQL Server Geometry Data Type](https://docs.microsoft.com/en-us/sql/t-sql/spatial-geometry/spatial-types-geometry-transact-sql)


