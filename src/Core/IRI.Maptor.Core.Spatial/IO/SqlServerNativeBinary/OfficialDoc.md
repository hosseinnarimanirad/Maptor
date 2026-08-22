2 Structures
2.1 GEOGRAPHY and GEOMETRY Structures
The GEOGRAPHY and GEOMETRY structures are serialized by using the binary format described later
in this section. Each structure contains several fixed fields (or header fields) and building
elements<2> that are repeated, as necessary, to describe the geography fully.
The GEOGRAPHY POINT and GEOMETRY POINT structures contain the coordinates for an individual
point and are repeated for as many points as are present in the GEOGRAPHY or GEOMETRY
structure. One shape structure appears for each OGC simple feature that is contained in the
GEOGRAPHY or GEOMETRY structure. A shape can consist of multiple figures, each of which is
defined by a single figure structure. The GEOGRAPHY and GEOMETRY structures contain flags and
counts that indicate how many of these building elements are contained in the GEOGRAPHY and
GEOMETRY structures.
The structures that are used to transfer geography and geometry data types are identical.
Therefore, in the remainder of this document, the term "GEOGRAPHY structure" refers to both the
GEOGRAPHY and GEOMETRY structures, except where it is necessary to distinguish between the
two structures. Likewise, "geography data type" refers to both the geography and geometry
protocol server data types.
Note The term "GEOGRAPHY POINT structure" does not also refer to the GEOMETRY POINT
structure in this document.


2.1.1 Basic GEOGRAPHY Structure (Version 1)
Version 1 of the GEOGRAPHY structure is formatted as shown in the following packet diagram.
All double fields contain double-precision floating-point numbers that are 64 bits (8 bytes) long. Integers and double-precision floating-point numbers are expressed in little-endian format.


SRID (4 bytes): (32 bit integer) The spatial reference identifier (SRID) for the geography. GEOGRAPHY structures MUST use SRID values in the range of 4120 through 4999, inclusive, with the exception of null geographies. A value of -1 indicates a null geography. When a null geography is indicated, all other fields are omitted. Default SRID for GEOGRAPHY instances is 4326. Default SRID for GEOMETRY instances is zero (0). For GEOMETRY instance, SRID can be any value: SRID is not constrained.
Version (1 byte): The version of the GEOGRAPHY structure.<3>
Serialization Properties (1 byte): A bit field that contains individual bit flags that indicate which optional content is present in the structure, as well as other attributes of the geography. The first 3 bits of the serialization properties are reserved for future use.


Where the bits are defined as: Value Description
Z
(0x01)
The structure has Z values.
M
(0x02)
The structure has M values.
V
(0x04)
Geography is valid.
For GEOGRAPHY structures, V in version 1 is always set.
P
(0x08)
Geography contains a single point. When P is set, Number of Points, Number of Figures, and Number of Shapes are implicitly assumed to be equal to 1 and are omitted from the structure. In addition, Figures is implicitly assumed to contain one figure representing a Stroke with a Point Offset of 0 (zero). Lastly, Shape is implicitly assumed to contain one shape of type Point, with a Figure Offset of 0 (zero) and without any parents (Parent Offset set to -1). This is an optimization for the common case of a single point.
L
(0x10)
Geography contains a single line segment. When L is set, Number of Points is implicitly assumed to be equal to 2 and does not explicitly appear in the serialized data. Number of Figures and Number of Shapes are implicitly assumed to be equal to 1 and do not explicitly appear in the serialized data. In addition, Figures is implicitly assumed to contain one stroke figure (0x01) with a Point Offset of 0 (zero). Lastly, Shape is implicitly assumed to contain one shape of type 0x02 (LineString), with a Figure Offset of 0 and without any parents (Parent Offset set to -1).
P and L are mutually exclusive properties.

Number of Points (optional, unsigned) (4 bytes): The number of points in the GEOGRAPHY structure. This MUST be a positive number or 0 (zero). If either the P or L bit is set in the Serialization Properties bit field, this field is omitted from the structure.
Points (optional, variable) (16 * Number of Points bytes) (variable): A sequence of point structures. The point coordinates are contained in GEOGRAPHY POINT structures in GEOGRAPHY structures. Likewise, coordinates are contained in GEOMETRY POINT structures in GEOMETRY structures. Both structures contain a pair of doubles.
If neither the P nor L bit is set in the Serialization Properties bit field, there will be Number of Points points in the sequence. If the P bit is set, there will be one point. If the L bit is set, there will be two points.
Z Values (optional, 8 * Number of Points bytes) (variable): A sequence of double values for the Z value of each point. If the Z bit is set, there will be Number of Points doubles in the array. If a Z value for an individual point is NULL, it is represented by QNaN [IEEE754].
M Values (optional, 8 * Number of Points bytes) (variable): A sequence of double values for the M value of each point. If the M bit is set, there will be Number of Points doubles in the array. If an M value for an individual point is NULL, it is represented as QNaN.
Number of Figures (optional, unsigned) (4 bytes): The number of figures in the structure. This MUST be a positive number or 0 (zero).
Figures (optional, 5 * Number of Figure bytes) (variable): A sequence of figure structures.
Number of Shapes (optional, unsigned) (4 bytes): The number of shapes in the structure. This MUST be a positive number.
Shapes (optional, 9 * Number of Shapes bytes) (variable): A sequence of shape structures.


2.1.3 FIGURE Structure
The FIGURE structure defines the partitions in the Points, Z Values, and M Values sequences for each constituent of the simple feature represented by the geography. A simple feature can have more than one part, whereas the collection of simple feature types can contain more than one simple feature.

Figures Attribute (byte) (1 byte): Determines the role of this figure within the GEOMETRY structure.
In version 1 of the serialization format, valid values are as follows:
▪ 0 (0x00): Figure is an interior ring in a polygon. Interior rings represent holes in exterior rings.
▪ 1 (0x01): Figure is a stroke. A stroke is a point or a line.
▪ 2 (0x02): Figure is an exterior ring in a polygon. An exterior ring represents the outer boundary of a polygon.
In version 2 of the serialization format, valid values are as follows:
▪ 0 (0x00): Figure is a point.
▪ 1 (0x01): Figure is a line.
▪ 2 (0x02): Figure is an arc.
▪ 3 (0x03): Figure is a composite curve, that is, it contains both line and arc segments.
The order of the coordinates in each ring of a geography polygon (but not a geometry polygon) is important. The outer rings for polygons are constructed by using the "left-hand" rule to determine the interior region of a polygon shape. Thus, outer polygon rings have their GEOGRAPHY POINT coordinate pairs ordered in a counter-clockwise direction. Polygon holes are constructed by using the "right-hand" rule. Thus, the GEOGRAPHY POINT coordinate pairs of a polygon holes are ordered in a clockwise direction.
Point Offset (32-bit integer) (4 bytes): The offset to the FIGURE structure’s first point in the Points, Z Values, and M Values sequences.


2.1.4 SHAPE Structure
The SHAPE structure identifies each simple feature contained in the GEOGRAPHY structure. It links together the simple feature type, the figure that represents it, and the parent simple feature that contains the present simple feature (if there is one).

Parent Offset (32-bit integer) (4 bytes): The offset to the SHAPE structure’s parent (containing) shape in the Shapes sequence if the shape has a parent, such as an outer ring if a hole, or a multipart simple feature.
Figure Offset (32-bit integer) (4 bytes): The offset to the SHAPE structure’s Figure in the Figures sequence.
OpenGIS Type (1 byte) (1 byte): The type of simple feature represented by the SHAPE structure.
In version 1 of the serialization format, valid values are as follows:
▪ 1 (0x01): Point
▪ 2 (0x02): LineString
▪ 3 (0x03): Polygon
▪ 4 (0x04): MultiPoint
▪ 5 (0x05): MultiLineString
▪ 6 (0x06): MultiPolygon
▪ 7 (0x07): GeometryCollection
Version 2 of the serialization format adds the following valid values:
▪ 8 (0x08): CircularString
▪ 9 (0x09): CompoundCurve
▪ 10 (0x0A): CurvePolygon
▪ 11 (0x0B): FullGlobe


2.1.6 GEOMETRY POINT Structure
The GEOMETRY POINT structure contains x-coordinates and y-coordinates as double values representing a point located on a plane.
X Coordinate (double) (8 bytes): The GEOMETRY POINT structure's x-coordinate.
Y Coordinate (double) (8 bytes): The GEOMETRY POINT structure's y-coordinate.
The following rules apply to the structure's x and y coordinates:
▪ X Coordinate and Y Coordinate values MUST NOT contain Infinity or NaN.
▪ The example structure that is provided in this section uses the Well-Known Text (WKT) protocol that is described in [OGCSFS].




link:
https://sqlprotocoldoc.z19.web.core.windows.net/MS-SSCLRT/%5bMS-SSCLRT%5d-221101-diff.pdf
