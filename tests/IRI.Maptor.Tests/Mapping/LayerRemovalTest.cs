using System.Linq;

using IRI.Maptor.Core.Spatial.Helpers.MapGrids;
using IRI.Maptor.Presentation.Core.Layers;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;
using IRI.Maptor.Tests.Common;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// What <see cref="LayerManager"/> can and cannot take off the map, and why both the map grids and
/// the MGRS overlay are each a single layer rather than a group.
/// </summary>
/// <remarks>
/// This exists because the constraint is invisible at the call site: removing a group layer looks
/// exactly like removing any other layer, succeeds silently, and leaves the layer on the map. It
/// cost two user-reported defects — a grid that could not be switched off, and one that duplicated
/// itself on every tick — before it was found.
/// </remarks>
[Collection(WpfCollection.Name)]
public class LayerRemovalTest
{
    private static (LayerManager manager, LegendViewModel legend) CreateManager()
        => (new LayerManager(), new LegendViewModel());

    /// <summary>
    /// The constraint itself. <c>LayerManager.Remove</c> matches its rule only against non-group
    /// layers — a group is recursed into and never tested — so removing one by identity does
    /// nothing, and its sub-layers keep it from ever emptying.
    /// </summary>
    [Fact]
    public void AGroupLayerCannotBeRemovedByIdentity()
    {
        WpfTestHost.Run(() =>
        {
            var (manager, legend) = CreateManager();

            var group = new GroupLayer("group") { CanUserDelete = true };

            // A real, non-group child. An empty *group* as the child would not reproduce this: the
            // empty-group branch would delete the child, which would then empty the parent and let
            // the same branch delete that too — passing for a reason unrelated to the rule.
            group.AddSubLayer(MapGridLayers.Create(MapGridDefinition.Geodetic()));

            manager.Add(legend, group, 1.0);

            Assert.Contains(group, manager.CurrentLayers);

            manager.Remove(group, forceRemove: true, keepEmptyParentGroup: false);

            Assert.Contains(group, manager.CurrentLayers);
        });
    }

    /// <summary>And a plain layer is removed exactly as one would expect.</summary>
    [Fact]
    public void APlainLayerIsRemovedByIdentity()
    {
        WpfTestHost.Run(() =>
        {
            var (manager, legend) = CreateManager();

            var layer = MapGridLayers.Create(MapGridDefinition.Geodetic());

            manager.Add(legend, layer, 1.0);

            Assert.Contains(layer, manager.CurrentLayers);

            manager.Remove(layer, forceRemove: true, keepEmptyParentGroup: false);

            Assert.DoesNotContain(layer, manager.CurrentLayers);
        });
    }

    /// <summary>So a map grid is one layer, and the map can take it off again.</summary>
    [Fact]
    public void AMapGridIsOneRemovableLayer()
    {
        WpfTestHost.Run(() =>
        {
            var layer = MapGridLayers.Create(MapGridDefinition.Utm());

            Assert.False(layer.IsGroupLayer);
            Assert.True(layer.CanUserDelete);
        });
    }

    /// <summary>
    /// And so is the MGRS overlay. It was a group of three sub-layers, which meant its ribbon toggle
    /// unchecked itself while the grid stayed drawn — the same defect the map grids hit, in the
    /// feature they were modelled on.
    /// </summary>
    [Fact]
    public void TheMgrsGridIsOneRemovableLayer()
    {
        WpfTestHost.Run(() =>
        {
            var (manager, legend) = CreateManager();

            var layer = MgrsGridLayers.Create();

            Assert.False(layer.IsGroupLayer, "the MGRS grid must not be a group; groups cannot be removed by identity");

            manager.Add(legend, layer, 1.0);

            Assert.Contains(layer, manager.CurrentLayers.Cast<ILayer>());

            manager.Remove(layer, forceRemove: true, keepEmptyParentGroup: false);

            Assert.DoesNotContain(layer, manager.CurrentLayers.Cast<ILayer>());
        });
    }
}
