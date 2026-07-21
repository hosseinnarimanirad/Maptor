using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Tst.Main.Common;

public class PointEqualityTests
{
    [Fact]
    public void Point_EqualValues_AreEqualWithEqualHashes()
    {
        var a = new Point(1.5, -2.5);
        var b = new Point(1.5, -2.5);

        Assert.True(a.Equals(b));
        Assert.True(b.Equals(a));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Point_DifferentValues_AreNotEqual()
    {
        Assert.False(new Point(1, 2).Equals(new Point(1, 3)));
        Assert.False(new Point(1, 2).Equals(null));
    }

    [Fact]
    public void Point_IsNotEqualToPointZWithSameXY()
    {
        // exact-type checks keep the equality relation symmetric
        var p = new Point(1, 2);
        var pz = new PointZ { X = 1, Y = 2, Z = 0 };

        Assert.False(p.Equals(pz));
        Assert.False(pz.Equals(p));
    }

    [Fact]
    public void PointZ_EqualValues_AreEqualWithEqualHashes()
    {
        var a = new PointZ { X = 1, Y = 2, Z = 3 };
        var b = new PointZ { X = 1, Y = 2, Z = 3 };

        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.False(a.Equals(new PointZ { X = 1, Y = 2, Z = 4 }));
    }

    [Fact]
    public void PointM_EqualValues_AreEqualWithEqualHashes()
    {
        var a = new PointM { X = 1, Y = 2, M = 3 };
        var b = new PointM { X = 1, Y = 2, M = 3 };

        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.False(a.Equals(new PointM { X = 1, Y = 2, M = 4 }));
    }

    [Fact]
    public void PointZM_EqualValues_AreEqualWithEqualHashes()
    {
        var a = new PointZM { X = 1, Y = 2, Z = 3, M = 4 };
        var b = new PointZM { X = 1, Y = 2, Z = 3, M = 4 };

        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.False(a.Equals(new PointZM { X = 1, Y = 2, Z = 3, M = 5 }));
    }

    [Fact]
    public void HashSetOfPoint_DeduplicatesByValue()
    {
        var set = new HashSet<Point>
        {
            new Point(0, 0),
            new Point(0, 0),
            new Point(1, 1),
        };

        Assert.Equal(2, set.Count);
        Assert.Contains(new Point(1, 1), set);
    }
}
