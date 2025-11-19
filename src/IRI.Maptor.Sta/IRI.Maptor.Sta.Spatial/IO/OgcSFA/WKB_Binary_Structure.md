# WKB (Well-Known Binary) Internal Binary Structure

This document describes the internal binary structure of WKB format for Point, LineString (Polyline), Polygon, and their M, Z, and ZM variants.

## Overview

WKB uses little-endian byte order (WkbNdr = 1) by default. All numeric values are stored in little-endian format.

### Basic Data Types

- **Byte Order**: 1 byte (0 = Big Endian, 1 = Little Endian)
- **Geometry Type**: 4 bytes (uint32)
- **Count**: 4 bytes (uint32)
- **Double**: 8 bytes (IEEE 754 double precision)

## Point Geometries

### Point (2D)

**Total Size**: 21 bytes

| Offset | Size | Type   | Description                    |
|--------|------|--------|--------------------------------|
| 0      | 1    | byte   | Byte order (1 = Little Endian) |
| 1      | 4    | uint32 | Geometry type (1 = Point)      |
| 5      | 8    | double | X coordinate                   |
| 13     | 8    | double | Y coordinate                   |

**Example**: Point(10.5, 20.3)
```
[01] [01 00 00 00] [00 00 00 00 00 00 25 40] [33 33 33 33 33 33 34 40]
```

### PointM (2D + Measure)

**Total Size**: 29 bytes

| Offset | Size | Type   | Description                    |
|--------|------|--------|--------------------------------|
| 0      | 1    | byte   | Byte order (1 = Little Endian) |
| 1      | 4    | uint32 | Geometry type (2001 = PointM)  |
| 5      | 8    | double | X coordinate                   |
| 13     | 8    | double | Y coordinate                   |
| 21     | 8    | double | M (measure) value              |

**Example**: PointM(10.5, 20.3, 100.0)
```
[01] [D1 07 00 00] [X bytes] [Y bytes] [M bytes]
```

### PointZ (3D)

**Total Size**: 29 bytes

| Offset | Size | Type   | Description                    |
|--------|------|--------|--------------------------------|
| 0      | 1    | byte   | Byte order (1 = Little Endian) |
| 1      | 4    | uint32 | Geometry type (1001 = PointZ)  |
| 5      | 8    | double | X coordinate                   |
| 13     | 8    | double | Y coordinate                   |
| 21     | 8    | double | Z coordinate                   |

### PointZM (3D + Measure)

**Total Size**: 37 bytes

| Offset | Size | Type   | Description                    |
|--------|------|--------|--------------------------------|
| 0      | 1    | byte   | Byte order (1 = Little Endian) |
| 1      | 4    | uint32 | Geometry type (3001 = PointZM) |
| 5      | 8    | double | X coordinate                   |
| 13     | 8    | double | Y coordinate                   |
| 21     | 8    | double | Z coordinate                   |
| 29     | 8    | double | M (measure) value              |

**Note**: If measure equals `EsriConstants.NoDataValue`, it is converted to `double.NaN` in the binary representation.

## LineString (Polyline) Geometries

### LineString (2D)

**Total Size**: 9 + (n × 16) bytes, where n = number of points

| Offset | Size | Type   | Description                    |
|--------|------|--------|--------------------------------|
| 0      | 1    | byte   | Byte order (1 = Little Endian) |
| 1      | 4    | uint32 | Geometry type (2 = LineString) |
| 5      | 4    | uint32 | Number of points (n)           |
| 9      | 8    | double | Point 1 X coordinate           |
| 17     | 8    | double | Point 1 Y coordinate           |
| 25     | 8    | double | Point 2 X coordinate           |
| 33     | 8    | double | Point 2 Y coordinate           |
| ...    | ...  | ...    | ... (repeated for each point)  |

**Example**: LineString with 3 points
```
[01] [02 00 00 00] [03 00 00 00] [P1X] [P1Y] [P2X] [P2Y] [P3X] [P3Y]
```

### LineStringM (2D + Measure)

**Total Size**: 9 + (n × 24) bytes, where n = number of points

| Offset | Size | Type   | Description                    |
|--------|------|--------|--------------------------------|
| 0      | 1    | byte   | Byte order (1 = Little Endian) |
| 1      | 4    | uint32 | Geometry type (2002 = LineStringM) |
| 5      | 4    | uint32 | Number of points (n)           |
| 9      | 8    | double | Point 1 X coordinate           |
| 17     | 8    | double | Point 1 Y coordinate           |
| 25     | 8    | double | Point 1 M (measure) value      |
| 33     | 8    | double | Point 2 X coordinate           |
| 41     | 8    | double | Point 2 Y coordinate           |
| 49     | 8    | double | Point 2 M (measure) value      |
| ...    | ...  | ...    | ... (repeated for each point)  |

**Pattern**: For each point: [X] [Y] [M]

### LineStringZ (3D)

**Total Size**: 9 + (n × 24) bytes, where n = number of points

| Offset | Size | Type   | Description                    |
|--------|------|--------|--------------------------------|
| 0      | 1    | byte   | Byte order (1 = Little Endian) |
| 1      | 4    | uint32 | Geometry type (1002 = LineStringZ) |
| 5      | 4    | uint32 | Number of points (n)           |
| 9      | 8    | double | Point 1 X coordinate           |
| 17     | 8    | double | Point 1 Y coordinate           |
| 25     | 8    | double | Point 1 Z coordinate           |
| 33     | 8    | double | Point 2 X coordinate           |
| 41     | 8    | double | Point 2 Y coordinate           |
| 49     | 8    | double | Point 2 Z coordinate           |
| ...    | ...  | ...    | ... (repeated for each point)  |

**Pattern**: For each point: [X] [Y] [Z]

### LineStringZM (3D + Measure)

**Total Size**: 9 + (n × 32) bytes, where n = number of points

| Offset | Size | Type   | Description                    |
|--------|------|--------|--------------------------------|
| 0      | 1    | byte   | Byte order (1 = Little Endian) |
| 1      | 4    | uint32 | Geometry type (3002 = LineStringZM) |
| 5      | 4    | uint32 | Number of points (n)           |
| 9      | 8    | double | Point 1 X coordinate           |
| 17     | 8    | double | Point 1 Y coordinate           |
| 25     | 8    | double | Point 1 Z coordinate           |
| 33     | 8    | double | Point 1 M (measure) value      |
| 41     | 8    | double | Point 2 X coordinate           |
| 49     | 8    | double | Point 2 Y coordinate           |
| 57     | 8    | double | Point 2 Z coordinate           |
| 65     | 8    | double | Point 2 M (measure) value      |
| ...    | ...  | ...    | ... (repeated for each point)  |

**Pattern**: For each point: [X] [Y] [Z] [M]

## Polygon Geometries

### Polygon (2D)

**Total Size**: 9 + (rings × (4 + points × 16)) bytes

| Offset | Size | Type   | Description                    |
|--------|------|--------|--------------------------------|
| 0      | 1    | byte   | Byte order (1 = Little Endian) |
| 1      | 4    | uint32 | Geometry type (3 = Polygon)    |
| 5      | 4    | uint32 | Number of rings (m)             |
| 9      | 4    | uint32 | Ring 1: Number of points (n1)   |
| 13     | 8    | double | Ring 1, Point 1 X coordinate   |
| 21     | 8    | double | Ring 1, Point 1 Y coordinate   |
| 29     | 8    | double | Ring 1, Point 2 X coordinate   |
| 37     | 8    | double | Ring 1, Point 2 Y coordinate   |
| ...    | ...  | ...    | ... (all points of ring 1)     |
| ...    | 4    | uint32 | Ring 2: Number of points (n2)   |
| ...    | ...  | ...    | ... (all points of ring 2)     |
| ...    | ...  | ...    | ... (repeated for each ring)    |

**Note**: 
- First ring is the exterior ring (boundary)
- Subsequent rings are interior rings (holes)
- Each ring must be closed (first point equals last point)

**Example**: Polygon with 1 exterior ring (4 points) and 1 interior ring (4 points)
```
[01] [03 00 00 00] [02 00 00 00] [04 00 00 00] [Ring1 points...] [04 00 00 00] [Ring2 points...]
```

### PolygonM (2D + Measure)

**Total Size**: 9 + (rings × (4 + points × 24)) bytes

| Offset | Size | Type   | Description                    |
|--------|------|--------|--------------------------------|
| 0      | 1    | byte   | Byte order (1 = Little Endian) |
| 1      | 4    | uint32 | Geometry type (2003 = PolygonM) |
| 5      | 4    | uint32 | Number of rings (m)             |
| 9      | 4    | uint32 | Ring 1: Number of points (n1)   |
| 13     | 8    | double | Ring 1, Point 1 X coordinate   |
| 21     | 8    | double | Ring 1, Point 1 Y coordinate   |
| 29     | 8    | double | Ring 1, Point 1 M (measure) value |
| 37     | 8    | double | Ring 1, Point 2 X coordinate   |
| 45     | 8    | double | Ring 1, Point 2 Y coordinate   |
| 53     | 8    | double | Ring 1, Point 2 M (measure) value |
| ...    | ...  | ...    | ... (repeated for each point in each ring) |

**Pattern**: For each ring: [Point Count] then for each point: [X] [Y] [M]

### PolygonZ (3D)

**Total Size**: 9 + (rings × (4 + points × 24)) bytes

| Offset | Size | Type   | Description                    |
|--------|------|--------|--------------------------------|
| 0      | 1    | byte   | Byte order (1 = Little Endian) |
| 1      | 4    | uint32 | Geometry type (1003 = PolygonZ) |
| 5      | 4    | uint32 | Number of rings (m)             |
| 9      | 4    | uint32 | Ring 1: Number of points (n1)   |
| 13     | 8    | double | Ring 1, Point 1 X coordinate   |
| 21     | 8    | double | Ring 1, Point 1 Y coordinate   |
| 29     | 8    | double | Ring 1, Point 1 Z coordinate   |
| 37     | 8    | double | Ring 1, Point 2 X coordinate   |
| 45     | 8    | double | Ring 1, Point 2 Y coordinate   |
| 53     | 8    | double | Ring 1, Point 2 Z coordinate   |
| ...    | ...  | ...    | ... (repeated for each point in each ring) |

**Pattern**: For each ring: [Point Count] then for each point: [X] [Y] [Z]

### PolygonZM (3D + Measure)

**Total Size**: 9 + (rings × (4 + points × 32)) bytes

| Offset | Size | Type   | Description                    |
|--------|------|--------|--------------------------------|
| 0      | 1    | byte   | Byte order (1 = Little Endian) |
| 1      | 4    | uint32 | Geometry type (3003 = PolygonZM) |
| 5      | 4    | uint32 | Number of rings (m)             |
| 9      | 4    | uint32 | Ring 1: Number of points (n1)   |
| 13     | 8    | double | Ring 1, Point 1 X coordinate   |
| 21     | 8    | double | Ring 1, Point 1 Y coordinate   |
| 29     | 8    | double | Ring 1, Point 1 Z coordinate   |
| 37     | 8    | double | Ring 1, Point 1 M (measure) value |
| 45     | 8    | double | Ring 1, Point 2 X coordinate   |
| 53     | 8    | double | Ring 1, Point 2 Y coordinate   |
| 61     | 8    | double | Ring 1, Point 2 Z coordinate   |
| 69     | 8    | double | Ring 1, Point 2 M (measure) value |
| ...    | ...  | ...    | ... (repeated for each point in each ring) |

**Pattern**: For each ring: [Point Count] then for each point: [X] [Y] [Z] [M]

## MultiPoint Geometries

### MultiPoint (2D)

**Total Size**: 9 + (n × 21) bytes, where n = number of points

| Offset | Size | Type   | Description                    |
|--------|------|--------|--------------------------------|
| 0      | 1    | byte   | Byte order (1 = Little Endian) |
| 1      | 4    | uint32 | Geometry type (4 = MultiPoint) |
| 5      | 4    | uint32 | Number of points (n)           |
| 9      | 21   | bytes  | Point 1 (complete Point structure) |
| 30     | 21   | bytes  | Point 2 (complete Point structure) |
| ...    | ...  | ...    | ... (repeated for each point)  |

**Note**: Each point in MultiPoint is stored as a complete Point structure (with its own byte order and geometry type).

### MultiPointM, MultiPointZ, MultiPointZM

Follow the same pattern as MultiPoint, but each embedded point uses the corresponding PointM, PointZ, or PointZM structure.

## Geometry Type Codes

| Code | Geometry Type |
|------|---------------|
| 1 | Point |
| 2 | LineString |
| 3 | Polygon |
| 4 | MultiPoint |
| 5 | MultiLineString |
| 6 | MultiPolygon |
| 1001 | PointZ |
| 1002 | LineStringZ |
| 1003 | PolygonZ |
| 2001 | PointM |
| 2002 | LineStringM |
| 2003 | PolygonM |
| 3001 | PointZM |
| 3002 | LineStringZM |
| 3003 | PolygonZM |

## LinearRing Structure

LinearRing is used internally within Polygon geometries. It does **not** include byte order or geometry type headers.

### LinearRing (2D)

**Total Size**: 4 + (n × 16) bytes

| Offset | Size | Type   | Description                    |
|--------|------|--------|--------------------------------|
| 0      | 4    | uint32 | Number of points (n)           |
| 4      | 8    | double | Point 1 X coordinate           |
| 12     | 8    | double | Point 1 Y coordinate           |
| 20     | 8    | double | Point 2 X coordinate           |
| 28     | 8    | double | Point 2 Y coordinate           |
| ...    | ...  | ...    | ... (repeated for each point)  |

**Note**: LinearRingM and LinearRingZM follow the same pattern but include M and Z values respectively.

## Byte Order

- **0** (WkbXdr): Big Endian
- **1** (WkbNdr): Little Endian (default)

All implementations in this codebase use Little Endian (WkbNdr = 1).

## Special Values

- **NoDataValue**: When a measure value equals `EsriConstants.NoDataValue`, it is converted to `double.NaN` (0x7FF8000000000000) in the binary representation for PointZM, LineStringZM, and PolygonZM geometries.

## References

- OGC Simple Feature Access Specification
- ISO/IEC 13249-3:2016 (SQL/MM Spatial)
- This implementation: `OgcWkbMapFunctions.cs`

