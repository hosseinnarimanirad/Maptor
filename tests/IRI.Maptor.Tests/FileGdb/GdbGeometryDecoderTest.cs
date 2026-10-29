using System.Collections.Generic;
using System.IO;
using System.Linq;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.FileGdb.IO;
using IRI.Maptor.Core.FileGdb.Shapes;
using IRI.Maptor.Core.FileGdb.Table;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Tests.FileGdb;

public class GdbGeometryDecoderTest
{
    // coordinates are stored as integers: real = stored / 1000 + origin
    private static readonly GdbGeometryFieldInfo Field = new() { XOrigin = -100, YOrigin = -200, XYScale = 1000 };

    private const int Srid = 4326;

    #region Blob writer

    private sealed class ShapeBlob
    {
        private readonly List<byte> _bytes = [];

        private long _dx, _dy;

        public ShapeBlob VarUInt(ulong value)
        {
            while (value >= 0x80)
            {
                _bytes.Add((byte)(value | 0x80));
                value >>= 7;
            }

            _bytes.Add((byte)value);
            return this;
        }

        public ShapeBlob VarInt(long value)
        {
            ulong magnitude = (ulong)Math.Abs(value);
            byte first = (byte)((magnitude & 0x3F) | (value < 0 ? 0x40u : 0));
            magnitude >>= 6;

            if (magnitude == 0)
            {
                _bytes.Add(first);
                return this;
            }

            _bytes.Add((byte)(first | 0x80));
            return VarUInt(magnitude);
        }

        public ShapeBlob Raw(params byte[] bytes)
        {
            _bytes.AddRange(bytes);
            return this;
        }

        public ShapeBlob BoundingBox() => VarUInt(0).VarUInt(0).VarUInt(0).VarUInt(0);

        /// <summary>Writes real coordinates as running deltas, as the format stores them.</summary>
        public ShapeBlob Points(params double[] xy)
        {
            for (int i = 0; i < xy.Length; i += 2)
            {
                long x = (long)Math.Round((xy[i] - Field.XOrigin) * Field.XYScale);
                long y = (long)Math.Round((xy[i + 1] - Field.YOrigin) * Field.XYScale);

                VarInt(x - _dx);
                VarInt(y - _dy);
                _dx = x;
                _dy = y;
            }

            return this;
        }

        public byte[] ToArray() => _bytes.ToArray();
    }

    private static GdbDecodedGeometry Decode(ShapeBlob blob) => GdbGeometryDecoder.Decode(blob.ToArray(), Field, Srid)!;

    private static double[] Xy(Geometry<Point> lineOrRing) => lineOrRing.Points!.SelectMany(p => new[] { p.X, p.Y }).ToArray();

    #endregion

    [Theory]
    [InlineData(new byte[] { 0x05 }, 5L)]
    [InlineData(new byte[] { 0x45 }, -5L)]
    [InlineData(new byte[] { 0x80, 0x01 }, 64L)]
    [InlineData(new byte[] { 0xC0, 0x01 }, -64L)]
    public void VarInt_Decodes(byte[] bytes, long expected)
    {
        Assert.Equal(expected, new GdbByteReader(bytes).ReadVarInt64());
    }

    [Fact]
    public void Point()
    {
        // points store (value + 1), so 0 can mean "empty"
        var blob = new ShapeBlob().VarUInt(1)
                                  .VarUInt((ulong)Math.Round((51.4 - Field.XOrigin) * Field.XYScale) + 1)
                                  .VarUInt((ulong)Math.Round((35.7 - Field.YOrigin) * Field.XYScale) + 1);

        var result = Decode(blob);

        Assert.Equal(GeometryType.Point, result.Geometry.Type);
        Assert.Equal(Srid, result.Geometry.Srid);
        Assert.Equal(51.4, result.Geometry.Points![0].X, 9);
        Assert.Equal(35.7, result.Geometry.Points![0].Y, 9);
        Assert.False(result.HasCurves);
    }

    [Fact]
    public void EmptyPoint_AndNullShape()
    {
        var empty = Decode(new ShapeBlob().VarUInt(1).VarUInt(0).VarUInt(0));

        Assert.Equal(GeometryType.Point, empty.Geometry.Type);
        Assert.True(empty.Geometry.IsEmpty());

        Assert.Null(GdbGeometryDecoder.Decode(new ShapeBlob().VarUInt(0).ToArray(), Field, Srid));
    }

    [Fact]
    public void MultiPoint()
    {
        var result = Decode(new ShapeBlob().VarUInt(8).VarUInt(2).BoundingBox().Points(1, 2, 2, 1.5));

        Assert.Equal(GeometryType.MultiPoint, result.Geometry.Type);
        Assert.Equal(new[] { 1.0, 2.0, 2.0, 1.5 }, result.Geometry.Geometries!.SelectMany(Xy));
    }

    [Fact]
    public void Polyline_SinglePart_IsLineString()
    {
        var result = Decode(new ShapeBlob().VarUInt(3).VarUInt(3).VarUInt(1).BoundingBox().Points(0, 0, 1, 1, 2, 0));

        Assert.Equal(GeometryType.LineString, result.Geometry.Type);
        Assert.Equal(new[] { 0.0, 0, 1, 1, 2, 0 }, Xy(result.Geometry));
    }

    [Fact]
    public void Polyline_MultiPart_DeltasContinueAcrossParts()
    {
        // 2 parts: first has 2 points (stored), second gets the remaining 3
        var result = Decode(new ShapeBlob().VarUInt(3).VarUInt(5).VarUInt(2).BoundingBox().VarUInt(2)
                                           .Points(10, 10, 11, 11, 20, 20, 21, 21, 22, 20));

        Assert.Equal(GeometryType.MultiLineString, result.Geometry.Type);
        Assert.Equal(new[] { 10.0, 10, 11, 11 }, Xy(result.Geometry.Geometries![0]));
        Assert.Equal(new[] { 20.0, 20, 21, 21, 22, 20 }, Xy(result.Geometry.Geometries![1]));
    }

    [Fact]
    public void Polygon_ClosedClockwiseRing_IsOpenedAndMadeCounterClockwise()
    {
        var result = Decode(new ShapeBlob().VarUInt(5).VarUInt(5).VarUInt(1).BoundingBox()
                                           .Points(0, 0, 0, 10, 10, 10, 10, 0, 0, 0));

        Assert.Equal(GeometryType.Polygon, result.Geometry.Type);

        var ring = result.Geometry.Geometries![0];
        Assert.Equal(4, ring.NumberOfPoints);
        Assert.False(SpatialUtility.IsClockwise(ring.Points));
    }

    [Fact]
    public void Polygon_WithHole()
    {
        var result = Decode(new ShapeBlob().VarUInt(5).VarUInt(10).VarUInt(2).BoundingBox().VarUInt(5)
                                           .Points(0, 0, 0, 10, 10, 10, 10, 0, 0, 0)      // outer, CW
                                           .Points(4, 4, 6, 4, 6, 6, 4, 6, 4, 4));       // hole, CCW

        Assert.Equal(GeometryType.Polygon, result.Geometry.Type);
        Assert.Equal(2, result.Geometry.Geometries!.Count);
        Assert.False(SpatialUtility.IsClockwise(result.Geometry.Geometries[0].Points));
        Assert.True(SpatialUtility.IsClockwise(result.Geometry.Geometries[1].Points));
    }

    [Fact]
    public void Polygon_TwoRings_IsMultiPolygon()
    {
        var result = Decode(new ShapeBlob().VarUInt(5).VarUInt(8).VarUInt(2).BoundingBox().VarUInt(4)
                                           .Points(0, 0, 0, 1, 1, 0, 0, 0)
                                           .Points(5, 5, 5, 6, 6, 5, 5, 5));

        Assert.Equal(GeometryType.MultiPolygon, result.Geometry.Type);
        Assert.Equal(2, result.Geometry.Geometries!.Count);
    }

    [Fact]
    public void Polygon_WithZAndM_IgnoresTrailingArrays()
    {
        // PolygonZM: the Z array follows XY, then M (here "no M": a single 0x42 byte)
        var result = Decode(new ShapeBlob().VarUInt(15).VarUInt(4).VarUInt(1).BoundingBox()
                                           .Points(0, 0, 0, 1, 1, 0, 0, 0)
                                           .VarInt(5).VarInt(0).VarInt(0).VarInt(0)
                                           .Raw(0x42));

        Assert.Equal(GeometryType.Polygon, result.Geometry.Type);
        Assert.Equal(3, result.Geometry.Geometries![0].NumberOfPoints);
    }

    [Fact]
    public void GeneralPolyline_WithCurves_KeepsVerticesAndFlagsCurves()
    {
        // general polyline + curve flag: a curve count follows the part count; the curve
        // descriptions come after the coordinates and are not read
        var result = Decode(new ShapeBlob().VarUInt(50 | 0x20000000).VarUInt(3).VarUInt(1).VarUInt(1).BoundingBox()
                                           .Points(0, 0, 5, 5, 10, 0)
                                           .Raw(0, 0, 0, 0, 1, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8));

        Assert.True(result.HasCurves);
        Assert.Equal(GeometryType.LineString, result.Geometry.Type);
        Assert.Equal(new[] { 0.0, 0, 5, 5, 10, 0 }, Xy(result.Geometry));
    }

    [Fact]
    public void GeneralPolygon_WithoutCurveFlag_HasNoCurveCount()
    {
        var result = Decode(new ShapeBlob().VarUInt(51).VarUInt(4).VarUInt(1).BoundingBox().Points(0, 0, 0, 1, 1, 0, 0, 0));

        Assert.False(result.HasCurves);
        Assert.Equal(GeometryType.Polygon, result.Geometry.Type);
    }

    [Fact]
    public void EmptyMultiPartShapes()
    {
        Assert.True(Decode(new ShapeBlob().VarUInt(3).VarUInt(0)).Geometry.IsEmpty());
        Assert.Equal(GeometryType.Polygon, Decode(new ShapeBlob().VarUInt(5).VarUInt(0)).Geometry.Type);
        Assert.Equal(GeometryType.MultiPoint, Decode(new ShapeBlob().VarUInt(8).VarUInt(0)).Geometry.Type);
    }

    [Fact]
    public void Unsupported_AndCorruptShapes_Throw()
    {
        Assert.Throws<NotSupportedException>(() => Decode(new ShapeBlob().VarUInt(32).VarUInt(3)));
        Assert.Throws<NotSupportedException>(() => Decode(new ShapeBlob().VarUInt(54).VarUInt(3)));
        Assert.Throws<InvalidDataException>(() => Decode(new ShapeBlob().VarUInt(99)));

        // 3 points promised, 2 present
        Assert.Throws<InvalidDataException>(() => Decode(new ShapeBlob().VarUInt(3).VarUInt(3).VarUInt(1).BoundingBox().Points(0, 0, 1, 1)));

        // part sizes larger than the point total
        Assert.Throws<InvalidDataException>(() => Decode(new ShapeBlob().VarUInt(3).VarUInt(3).VarUInt(2).BoundingBox().VarUInt(9).Points(0, 0, 1, 1, 2, 2)));
    }
}
