using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.AdvancedStructures;

using Xunit;

namespace IRI.Maptor.Tests.Spatial;

public class BoxGridTest
{
    [Fact]
    public void ItemSpanningSeveralCells_IsReturnedOnce()
    {
        var grid = new BoxGrid<string>(10);

        grid.Add("long", 0, 0, 95, 5);

        Assert.Equal(new[] { "long" }, grid.Query(0, 0, 100, 10).ToArray());
    }

    [Fact]
    public void Query_FindsOnlyItemsInTouchedCells()
    {
        var grid = new BoxGrid<string>(10);

        grid.Add("near", 2, 2, 4, 4);
        grid.Add("far", 52, 52, 54, 54);

        Assert.Equal(new[] { "near" }, grid.Query(0, 0, 5, 5).ToArray());
        Assert.Empty(grid.Query(200, 200, 210, 210));
    }

    [Fact]
    public void NegativeCoordinates_AreIndexed()
    {
        var grid = new BoxGrid<int>(1);

        grid.Add(7, -3.5, -2.5, -3.1, -2.1);

        Assert.Equal(new[] { 7 }, grid.Query(-4, -3, -3, -2).ToArray());
        Assert.Empty(grid.Query(0, 0, 1, 1));
    }

    [Fact]
    public void BoundingBoxOverloads_MatchTheCoordinateOnes()
    {
        var grid = new BoxGrid<string>(10);

        grid.Add("a", new BoundingBox(1, 1, 2, 2));

        Assert.Equal(new[] { "a" }, grid.Query(new BoundingBox(0, 0, 3, 3)).ToArray());
    }

    [Fact]
    public void NonPositiveCellSize_FallsBackToOne()
    {
        Assert.Equal(1, new BoxGrid<int>(0).CellSize);
        Assert.Equal(1, new BoxGrid<int>(-5).CellSize);
    }

    [Fact]
    public void ItemsOfACell_ComeBackInTheOrderAdded()
    {
        var grid = new BoxGrid<int>(100);

        foreach (var i in new[] { 5, 3, 9, 1 })
            grid.Add(i, 1, 1, 2, 2);

        Assert.Equal(new[] { 5, 3, 9, 1 }, grid.Query(0, 0, 10, 10).ToArray());
    }

    [Fact]
    public void Comparer_DecidesWhatIsTheSameItem()
    {
        var grid = new BoxGrid<string>(10, System.StringComparer.OrdinalIgnoreCase);

        grid.Add("Tower", 1, 1, 2, 2);
        grid.Add("tower", 31, 1, 32, 2);

        Assert.Single(grid.Query(0, 0, 40, 10));
    }

    [Fact]
    public void WorksThroughTheInterface()
    {
        ISpatialIndex<string> index = new BoxGrid<string>(10);

        index.Add("x", new BoundingBox(5, 5, 6, 6));

        Assert.Contains("x", index.Query(new BoundingBox(0, 0, 10, 10)));
    }
}
