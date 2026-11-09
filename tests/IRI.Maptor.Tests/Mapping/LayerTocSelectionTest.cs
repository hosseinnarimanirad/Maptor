using IRI.Maptor.Core.Spatial.Helpers.MapGrids;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;
using IRI.Maptor.Tests.Common;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The legend shows a layer's options while the layer is selected in the TOC, and
/// <see cref="LayerManager"/> keeps that to at most one layer.
/// </summary>
[Collection(WpfCollection.Name)]
public class LayerTocSelectionTest
{
    private static VectorLayer CreateLayer() => MapGridLayers.Create(MapGridDefinition.Geodetic());

    [Fact]
    public void SelectingALayerDeselectsTheOthers()
    {
        WpfTestHost.Run(() =>
        {
            var manager = new LayerManager();
            var legend = new LegendViewModel();

            var a = CreateLayer();
            var b = CreateLayer();

            manager.Add(legend, a, 1.0);
            manager.Add(legend, b, 1.0);

            a.IsSelectedInToc = true;
            b.IsSelectedInToc = true;

            Assert.False(a.IsSelectedInToc);
            Assert.True(b.IsSelectedInToc);
        });
    }

    /// <summary>Setting it again still toggles it off, so a second click closes the options.</summary>
    [Fact]
    public void SelectingTheSelectedLayerAgainDeselectsIt()
    {
        WpfTestHost.Run(() =>
        {
            var manager = new LayerManager();

            var a = CreateLayer();

            manager.Add(new LegendViewModel(), a, 1.0);

            a.IsSelectedInToc = true;
            a.IsSelectedInToc = true;

            Assert.False(a.IsSelectedInToc);
        });
    }

    /// <summary>Sub-layers of a group count like any other layer.</summary>
    [Fact]
    public void ALayerInsideAGroupTakesPart()
    {
        WpfTestHost.Run(() =>
        {
            var manager = new LayerManager();
            var legend = new LegendViewModel();

            var root = CreateLayer();
            var child = CreateLayer();

            var group = new GroupLayer("group");
            group.AddSubLayer(child);

            manager.Add(legend, root, 1.0);
            manager.Add(legend, group, 1.0);

            root.IsSelectedInToc = true;
            child.IsSelectedInToc = true;

            Assert.False(root.IsSelectedInToc);
            Assert.True(child.IsSelectedInToc);

            root.IsSelectedInToc = true;

            Assert.False(child.IsSelectedInToc);
            Assert.True(root.IsSelectedInToc);
        });
    }

    /// <summary>
    /// For a group, being selected means being expanded, and a group has no options to show, so
    /// opening a group leaves the open layer alone.
    /// </summary>
    [Fact]
    public void SelectingAGroupKeepsTheSelectedLayer()
    {
        WpfTestHost.Run(() =>
        {
            var manager = new LayerManager();
            var legend = new LegendViewModel();

            var layer = CreateLayer();

            var group = new GroupLayer("group");
            group.AddSubLayer(CreateLayer());

            manager.Add(legend, layer, 1.0);
            manager.Add(legend, group, 1.0);

            layer.IsSelectedInToc = true;
            group.IsSelectedInToc = true;

            Assert.True(layer.IsSelectedInToc);
        });
    }

    /// <summary>A layer taken off the map no longer deselects the layers still on it.</summary>
    [Fact]
    public void ARemovedLayerNoLongerTakesPart()
    {
        WpfTestHost.Run(() =>
        {
            var manager = new LayerManager();
            var legend = new LegendViewModel();

            var kept = CreateLayer();
            var removed = CreateLayer();

            manager.Add(legend, kept, 1.0);
            manager.Add(legend, removed, 1.0);

            manager.Remove(removed, forceRemove: true, keepEmptyParentGroup: false);

            kept.IsSelectedInToc = true;
            removed.IsSelectedInToc = true;

            Assert.True(kept.IsSelectedInToc);
        });
    }
}
