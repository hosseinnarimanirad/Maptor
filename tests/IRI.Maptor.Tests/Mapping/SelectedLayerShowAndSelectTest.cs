using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Tests.Common;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// SelectedLayer.ShowAndSelect, behind the identify window's "show in attribute table": rows the table
/// already lists are reused, missing ones appended, the table moves to the row's page and highlights it.
/// </summary>
[Collection(WpfCollection.Name)]
public class SelectedLayerShowAndSelectTest
{
    private static Feature<Point> CreateFeature(int id)
    {
        var feature = Geometry<Point>.Create(id, id, SridHelper.WebMercator).AsFeature();

        feature.Id = id;

        return feature;
    }

    // any VectorLayer serves; a drawing layer is the simplest one to build
    private static SelectedLayer CreateTable(IEnumerable<Feature<Point>> rows)
    {
        var layer = DrawingItemLayer.Create("table", CreateFeature(-1), [])!;

        return new SelectedLayer(null!, layer, new List<Field>())
        {
            Features = new ObservableCollection<Feature<Point>>(rows),
        };
    }

    [Fact]
    public void ExistingRow_IsReused_ByIdNotByReference()
    {
        var existing = CreateFeature(5);
        var table = CreateTable([CreateFeature(4), existing]);

        var rows = table.ShowAndSelect([CreateFeature(5)]);

        Assert.Same(existing, Assert.Single(rows));
        Assert.Equal(2, table.Features.Count);
        Assert.Same(existing, Assert.Single(table.HighlightedFeatures));
    }

    [Fact]
    public void MissingRow_IsAppended_AndOthersKept()
    {
        var table = CreateTable([CreateFeature(1), CreateFeature(2)]);

        var added = CreateFeature(9);

        table.ShowAndSelect([added]);

        Assert.Equal([1, 2, 9], table.Features.Select(f => f.Id));
        Assert.Same(added, Assert.Single(table.HighlightedFeatures));
    }

    [Fact]
    public void RowOnLaterPage_MovesToThatPage()
    {
        var table = CreateTable(Enumerable.Range(1, 250).Select(CreateFeature));

        table.ShowAndSelect([CreateFeature(230)]);

        Assert.Equal(2, table.CurrentPageIndex);
        Assert.Equal(230, Assert.Single(table.HighlightedFeatures).Id);
    }

    [Fact]
    public void SeveralFeatures_AllListed()
    {
        var table = CreateTable([CreateFeature(1)]);

        var rows = table.ShowAndSelect([CreateFeature(1), CreateFeature(2), CreateFeature(3)]);

        Assert.Equal([1, 2, 3], rows.Select(f => f.Id));
        Assert.Equal(3, table.HighlightedFeatures.Count);
    }

    [Fact]
    public void WithoutBoundView_ScrollIsKeptUntilTheViewAsks()
    {
        var table = CreateTable([CreateFeature(1), CreateFeature(2)]);

        table.ShowAndSelect([CreateFeature(2)]);

        // a grid binding to the tab clears the highlight; the view then re-applies it
        table.UpdateHighlightedFeatures([]);

        var target = table.TakePendingShow();

        Assert.Equal(2, target!.Id);
        Assert.Equal(2, Assert.Single(table.HighlightedFeatures).Id);
        Assert.Null(table.TakePendingShow());
    }

    [Fact]
    public void WithBoundView_ScrollIsRequestedAtOnce()
    {
        var table = CreateTable([CreateFeature(1), CreateFeature(2)]);

        Feature<Point>? scrolled = null;

        table.RequestScrollIntoView = f => scrolled = f;

        table.ShowAndSelect([CreateFeature(2)]);

        Assert.Equal(2, scrolled!.Id);
        Assert.Null(table.TakePendingShow());
    }
}
