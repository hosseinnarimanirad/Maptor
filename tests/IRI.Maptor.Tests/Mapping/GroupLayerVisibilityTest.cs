using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Core.Layers;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// Show/hide in a group that holds another group (a file geodatabase's feature datasets, Saba's and Gas's server
/// layer groups). A group's own <see cref="ILayer.IsVisible"/> flag does not follow its children, so a parent has to
/// judge a sub-group by <see cref="ILayer.AllChildrenVisible"/>; it once read the flag, which starts hidden, and hiding
/// the last visible plain layer of the parent hid every layer of the sub-groups too.
/// </summary>
[Collection(WpfCollection.Name)]
public class GroupLayerVisibilityTest
{
    private static VectorLayer Leaf(string name)
        => new(name,
               new List<Geometry<Point>> { Geometry<Point>.CreatePointOrLineString(new List<Point> { new(5_000_000, 4_000_000) }, SridHelper.WebMercator) },
               VisualParameters.CreateNew(1),
               LayerType.VectorLayer,
               RenderMode.Default,
               RasterizationMethod.GdiPlus);

    /// <summary>root: [ inner: [ a ], b ]</summary>
    private static (GroupLayer Root, GroupLayer Inner, VectorLayer A, VectorLayer B) Tree()
    {
        var a = Leaf("a");
        var b = Leaf("b");

        var inner = new GroupLayer("inner");
        inner.AddSubLayer(a);

        var root = new GroupLayer("root");
        root.AddSubLayer(b);
        root.AddSubLayer(inner);

        return (root, inner, a, b);
    }

    [Fact]
    public void HidingARootLayer_LeavesTheSubGroupsLayersAlone()
    {
        WpfTestHost.Run(() =>
        {
            var (root, inner, a, b) = Tree();

            Assert.Equal(true, root.AllChildrenVisible);

            b.IsVisible = false;

            Assert.True(a.IsVisible);
            Assert.Equal(true, inner.AllChildrenVisible);
            Assert.Null(root.AllChildrenVisible);               // some visible
        });
    }

    [Fact]
    public void ShowingARootLayer_DoesNotBringBackAHiddenSubGroupLayer()
    {
        WpfTestHost.Run(() =>
        {
            var (root, inner, a, b) = Tree();

            a.IsVisible = false;
            Assert.Equal(false, inner.AllChildrenVisible);
            Assert.Null(root.AllChildrenVisible);

            b.IsVisible = false;
            Assert.Equal(false, root.AllChildrenVisible);       // every leaf hidden

            b.IsVisible = true;

            Assert.False(a.IsVisible);
            Assert.Null(root.AllChildrenVisible);
        });
    }

    [Fact]
    public void TheGroupsTick_StillShowsAndHidesEverything()
    {
        WpfTestHost.Run(() =>
        {
            var (root, inner, a, b) = Tree();

            root.AllChildrenVisible = false;                    // the legend's group check box
            Assert.False(a.IsVisible);
            Assert.False(b.IsVisible);
            Assert.Equal(false, inner.AllChildrenVisible);

            root.AllChildrenVisible = true;
            Assert.True(a.IsVisible);
            Assert.True(b.IsVisible);
            Assert.Equal(true, inner.AllChildrenVisible);
        });
    }

    [Fact]
    public void ASubGroupGoingMixed_ReachesItsParent()
    {
        WpfTestHost.Run(() =>
        {
            var a2 = Leaf("a2");
            var (root, inner, a, _) = Tree();
            inner.AddSubLayer(a2);

            a.IsVisible = false;

            Assert.Null(inner.AllChildrenVisible);
            Assert.Null(root.AllChildrenVisible);
        });
    }

    [Fact]
    public void AFlatGroup_BehavesAsBefore()
    {
        WpfTestHost.Run(() =>
        {
            var x = Leaf("x");
            var y = Leaf("y");
            var group = new GroupLayer("flat");
            group.AddSubLayer(x);
            group.AddSubLayer(y);

            x.IsVisible = false;
            Assert.Null(group.AllChildrenVisible);
            Assert.True(y.IsVisible);

            y.IsVisible = false;
            Assert.Equal(false, group.AllChildrenVisible);

            x.IsVisible = true;
            Assert.Null(group.AllChildrenVisible);
            Assert.False(y.IsVisible);
        });
    }
}
