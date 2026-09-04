using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;

using IRI.Maptor.Core.Spatial.Helpers.MapGrids;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Layers;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;
using IRI.Maptor.Tests.Common;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The grids drop-down: switching grids on and off, and keeping the menu honest about what is
/// actually on the map.
/// </summary>
/// <remarks>
/// Several grids may be drawn at once, so the two things worth pinning are that each one's numbers
/// get their own tier — otherwise two grids print over each other — and that a grid removed by the
/// legend unchecks itself in the menu.
/// </remarks>
[Collection(WpfCollection.Name)]
public class MapGridsViewModelTest
{
    /// <summary>
    /// A map presenter with nothing behind it but a layer list. `MapViewModelBase` is abstract only
    /// to stop it being used directly — it declares no abstract members — so standing one up costs
    /// wiring the two layer actions the real `MapViewer` supplies.
    /// </summary>
    private sealed class TestMap : MapViewModelBase
    {
        public TestMap()
        {
            // GeneralSettings is null until this runs — the setter is private, so there is no other
            // way to stand it up, and the grids' persistence reads through it.
            InitializeSettings(null, null, null, null);

            Layers = new ObservableCollection<ILayer>();

            RequestAddLayer = layer => Layers.Add(layer);

            RequestRemoveLayer = layer => Layers.Remove(layer);

            // What MapViewer wires: clears the drawn visuals and then drops the layer. The grids
            // use this rather than RequestRemoveLayer, because that one leaves the visuals behind.
            RequestClearLayer = (layer, remove, _, _) =>
            {
                ClearCount++;

                if (remove)
                    Layers.Remove(layer);
            };

            RequestRefresh = _ => RefreshCount++;
        }

        public int RefreshCount { get; private set; }

        public int ClearCount { get; private set; }
    }

    private static MapGridItemViewModel ItemFor(MapGridsViewModel grids, string key)
        => grids.Items.Single(item => item.Key == key);

    [Fact]
    public void Catalogue_OffersTheNamedGrids()
    {
        WpfTestHost.Run(() =>
        {
            var map = new TestMap();

            var keys = map.MapGridItems.Select(item => item.Key).ToList();

            Assert.Equal(
                new[] { "geodetic", "utm", "webMercator", "lccNioc", "lccFd58", "lccNahrwan" },
                keys);

            Assert.All(map.MapGridItems, item => Assert.False(item.IsChecked));
        });
    }

    /// <summary>
    /// One layer per grid, and emphatically not a group: <c>LayerManager.Remove</c> matches its rule
    /// only against non-group layers, so a grid drawn as a group could never be taken off the map
    /// again — it stayed drawn, the menu unchecked itself, and the next tick added a second copy.
    /// </summary>
    [Fact]
    public void Checking_PutsOnePlainLayerOnTheMap()
    {
        WpfTestHost.Run(() =>
        {
            var map = new TestMap();

            var item = ItemFor(map.MapGrids, "geodetic");

            item.IsChecked = true;

            Assert.True(item.IsChecked);
            Assert.Single(map.Layers);

            var layer = Assert.IsType<VectorLayer>(map.Layers[0]);

            Assert.False(layer.IsGroupLayer, "a grid must not be a group layer; groups cannot be removed by identity");
            Assert.True(layer.CanUserDelete);

            // Three line weights and the values, all on the one layer.
            Assert.Equal(4, layer.Symbolizers.Count);
        });
    }

    /// <summary>
    /// Switching a grid off must take it off the map — visuals included — and switching it on again
    /// must not leave a second copy behind. Both were broken while a grid was a group layer.
    /// </summary>
    [Fact]
    public void TickingAGridOnAndOffRepeatedlyLeavesExactlyOneOrNone()
    {
        WpfTestHost.Run(() =>
        {
            var map = new TestMap();

            var item = ItemFor(map.MapGrids, "utm");

            for (var round = 0; round < 3; round++)
            {
                item.IsChecked = true;

                Assert.Single(map.Layers);
                Assert.True(item.IsChecked);

                item.IsChecked = false;

                Assert.Empty(map.Layers);
                Assert.False(item.IsChecked);
            }

            // Removal went through the path that also clears what is already drawn.
            Assert.Equal(3, map.ClearCount);
        });
    }

    [Fact]
    public void Unchecking_TakesItOffAgain()
    {
        WpfTestHost.Run(() =>
        {
            var map = new TestMap();

            var item = ItemFor(map.MapGrids, "utm");

            item.IsChecked = true;
            item.IsChecked = false;

            Assert.False(item.IsChecked);
            Assert.Empty(map.Layers);
        });
    }

    [Fact]
    public void TwoGridsCanBeOnAtOnce()
    {
        WpfTestHost.Run(() =>
        {
            var map = new TestMap();

            ItemFor(map.MapGrids, "geodetic").IsChecked = true;
            ItemFor(map.MapGrids, "utm").IsChecked = true;

            Assert.Equal(2, map.Layers.Count);
            Assert.Equal(2, map.MapGrids.ActiveCount);
        });
    }

    /// <summary>
    /// Every grid writes its values against the edges of the view, so the second one on has to sit
    /// a row further in or the two sets of numbers land on top of each other.
    /// </summary>
    [Fact]
    public void EachGridGetsItsOwnLabelTier()
    {
        WpfTestHost.Run(() =>
        {
            var map = new TestMap();

            var first = ItemFor(map.MapGrids, "geodetic");
            var second = ItemFor(map.MapGrids, "utm");

            first.IsChecked = true;
            second.IsChecked = true;

            Assert.Equal(0, first.Definition.LabelTier);
            Assert.Equal(1, second.Definition.LabelTier);
        });
    }

    /// <summary>And the gap closes when one leaves, so the remaining row is not left floating.</summary>
    [Fact]
    public void RemovingAGridRepacksTheTiersAndRedraws()
    {
        WpfTestHost.Run(() =>
        {
            var map = new TestMap();

            var first = ItemFor(map.MapGrids, "geodetic");
            var second = ItemFor(map.MapGrids, "utm");

            first.IsChecked = true;
            second.IsChecked = true;

            var before = map.RefreshCount;

            first.IsChecked = false;

            Assert.Equal(0, second.Definition.LabelTier);
            Assert.True(map.RefreshCount > before, "the map must be asked to redraw once a tier moves");
        });
    }

    /// <summary>
    /// The group is user-deletable, so it can leave by the legend rather than by the menu. The menu
    /// has to notice, or it would claim the grid was on and the next click would remove nothing.
    /// </summary>
    [Fact]
    public void DeletingTheGroupFromTheLegendUnchecksTheMenuItem()
    {
        WpfTestHost.Run(() =>
        {
            var map = new TestMap();

            var item = ItemFor(map.MapGrids, "geodetic");

            item.IsChecked = true;

            var raised = false;
            item.PropertyChanged += (_, e) => raised |= e.PropertyName == nameof(item.IsChecked);

            // What the legend's delete command ends up doing.
            map.Layers.RemoveAt(0);

            Assert.False(item.IsChecked);
            Assert.True(raised, "the menu item must raise PropertyChanged so the ribbon updates");
            Assert.Equal(0, map.MapGrids.ActiveCount);

            // And it can be switched straight back on.
            item.IsChecked = true;

            Assert.True(item.IsChecked);
            Assert.Single(map.Layers);
        });
    }

    /// <summary>
    /// The same bug the MGRS toggle had, fixed in the same pass: its group is user-deletable too,
    /// and the ribbon kept reporting the grid as visible after the legend removed it — so the next
    /// click removed an already-gone layer instead of putting the grid back.
    /// </summary>
    [Fact]
    public void DeletingTheMgrsGridFromTheLegendUnchecksItsToggle()
    {
        WpfTestHost.Run(() =>
        {
            var map = new TestMap();

            map.ToggleMgrsGridCommand.Execute(null);

            Assert.True(map.IsMgrsGridVisible);
            Assert.Single(map.Layers);

            map.Layers.RemoveAt(0);

            Assert.False(map.IsMgrsGridVisible);

            // The toggle now puts it back rather than trying to remove it again.
            map.ToggleMgrsGridCommand.Execute(null);

            Assert.True(map.IsMgrsGridVisible);
            Assert.Single(map.Layers);
        });
    }

    #region Persistence

    /// <summary>
    /// The host saves off the settings' <c>PropertyChanged</c>, so writing the keys is the whole of
    /// the save path — there is nothing else to call.
    /// </summary>
    [Fact]
    public void SwitchingGridsOnAndOffRecordsTheirKeys()
    {
        WpfTestHost.Run(() =>
        {
            var map = new TestMap();

            Assert.Equal(string.Empty, map.GeneralSettings.MapGrids_SelectedKeys);

            ItemFor(map.MapGrids, "geodetic").IsChecked = true;

            Assert.Equal("geodetic", map.GeneralSettings.MapGrids_SelectedKeys);

            ItemFor(map.MapGrids, "utm").IsChecked = true;

            Assert.Equal("geodetic,utm", map.GeneralSettings.MapGrids_SelectedKeys);

            ItemFor(map.MapGrids, "geodetic").IsChecked = false;

            Assert.Equal("utm", map.GeneralSettings.MapGrids_SelectedKeys);
        });
    }

    /// <summary>Deleting a grid from the legend is still the user switching it off.</summary>
    [Fact]
    public void DeletingAGridFromTheLegendAlsoForgetsIt()
    {
        WpfTestHost.Run(() =>
        {
            var map = new TestMap();

            ItemFor(map.MapGrids, "utm").IsChecked = true;

            map.Layers.RemoveAt(0);

            Assert.Equal(string.Empty, map.GeneralSettings.MapGrids_SelectedKeys);
        });
    }

    [Fact]
    public void RestoreBringsBackTheGridsThatWereOn()
    {
        WpfTestHost.Run(() =>
        {
            var map = new TestMap();

            map.GeneralSettings.MapGrids_SelectedKeys = "utm,geodetic";

            map.MapGrids.RestoreFromSettings();

            Assert.True(ItemFor(map.MapGrids, "geodetic").IsChecked);
            Assert.True(ItemFor(map.MapGrids, "utm").IsChecked);
            Assert.Equal(2, map.Layers.Count);

            // Restored in catalogue order, so the tiers come back the same way round however the
            // keys happened to be written.
            Assert.Equal(0, ItemFor(map.MapGrids, "geodetic").Definition.LabelTier);
            Assert.Equal(1, ItemFor(map.MapGrids, "utm").Definition.LabelTier);
        });
    }

    /// <summary>Restoring must not rewrite what it just read, nor double up if it runs twice.</summary>
    [Fact]
    public void RestoreIsIdempotentAndDoesNotRewriteTheSetting()
    {
        WpfTestHost.Run(() =>
        {
            var map = new TestMap();

            map.GeneralSettings.MapGrids_SelectedKeys = "geodetic";

            map.MapGrids.RestoreFromSettings();
            map.MapGrids.RestoreFromSettings();

            Assert.Single(map.Layers);
            Assert.Equal(1, map.MapGrids.ActiveCount);
            Assert.Equal("geodetic", map.GeneralSettings.MapGrids_SelectedKeys);
        });
    }

    /// <summary>
    /// A key written by a build whose catalogue has since changed is dropped rather than throwing
    /// or restoring the wrong grid — which is why keys are stored rather than indices.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nosuchgrid")]
    [InlineData("nosuchgrid,utm")]
    public void RestoreIgnoresKeysItDoesNotRecognise(string stored)
    {
        WpfTestHost.Run(() =>
        {
            var map = new TestMap();

            map.GeneralSettings.MapGrids_SelectedKeys = stored;

            map.MapGrids.RestoreFromSettings();

            Assert.Equal(stored.Contains("utm") ? 1 : 0, map.MapGrids.ActiveCount);
        });
    }

    #endregion

    #region Recolouring

    private static VectorLayer LineLayerOf(TestMap map, string key)
    {
        ItemFor(map.MapGrids, key).IsChecked = true;

        return map.Layers.OfType<VectorLayer>().Last();
    }

    /// <summary>
    /// The dialog seeds itself from the layer's <em>first</em> symbolizer, so that has to be the
    /// weight the user can actually see. It used to be the faintest subdivision, which is why
    /// recolouring a grid looked like it had done nothing.
    /// </summary>
    [Fact]
    public void TheSymbologyDialogEditsThePrincipalWeight()
    {
        WpfTestHost.Run(() =>
        {
            var lines = LineLayerOf(new TestMap(), "geodetic");

            var edited = lines.GetMainOrDefaultSymbology();

            Assert.Equal(MapGridStyle.Geodetic.MajorThickness, edited.StrokeThickness);
            Assert.Equal(MapGridStyle.Geodetic.MajorOpacity, edited.Opacity);
        });
    }

    /// <summary>
    /// And changing its colour changes the whole grid — the reported defect. Every weight is one
    /// layer, so nothing but this link could carry the change across.
    /// </summary>
    [Fact]
    public void RecolouringAGridRecoloursEveryWeight()
    {
        WpfTestHost.Run(() =>
        {
            var lines = LineLayerOf(new TestMap(), "geodetic");

            var green = new SolidColorBrush(Colors.Green);

            // Exactly what DefaultActions.GetDefaultShowSymbologyView does on Apply.
            lines.GetMainOrDefaultSymbology().Stroke = green;

            var strokes = lines.Symbolizers.OfType<SimpleSymbolizer>().Select(s => s.Param!.Stroke).ToList();

            Assert.Equal(3, strokes.Count);
            Assert.All(strokes, stroke => Assert.Same(green, stroke));
        });
    }

    /// <summary>
    /// Thickness follows proportionally rather than verbatim, so a grid drawn heavier stays a grid
    /// instead of collapsing into three lines of equal weight.
    /// </summary>
    [Fact]
    public void ThickeningAGridKeepsTheWeightsApart()
    {
        WpfTestHost.Run(() =>
        {
            var lines = LineLayerOf(new TestMap(), "utm");

            var style = MapGridStyle.Utm;

            var main = lines.GetMainOrDefaultSymbology();

            main.StrokeThickness = style.MajorThickness * 2;

            var thicknesses = lines.Symbolizers.OfType<SimpleSymbolizer>().Select(s => s.Param!.StrokeThickness).ToList();

            Assert.Equal(style.MajorThickness * 2, thicknesses[0], 6);
            Assert.Equal(style.MinorThickness * 2, thicknesses[1], 6);
            Assert.Equal(style.SeamThickness * 2, thicknesses[2], 6);

            // The subdivisions stay lighter than the principal lines, which is the whole point.
            Assert.True(thicknesses[1] < thicknesses[0]);
        });
    }

    /// <summary>Opacity is what separates the weights, and the dialog does not set it — so it is left alone.</summary>
    [Fact]
    public void RecolouringLeavesTheWeightsOpacityAlone()
    {
        WpfTestHost.Run(() =>
        {
            var lines = LineLayerOf(new TestMap(), "geodetic");

            lines.GetMainOrDefaultSymbology().Stroke = new SolidColorBrush(Colors.Red);

            var opacities = lines.Symbolizers.OfType<SimpleSymbolizer>().Select(s => s.Param!.Opacity).ToList();

            Assert.Equal(MapGridStyle.Geodetic.MajorOpacity, opacities[0]);
            Assert.Equal(MapGridStyle.Geodetic.MinorOpacity, opacities[1]);
            Assert.Equal(MapGridStyle.Geodetic.SeamOpacity, opacities[2]);
        });
    }

    #endregion

    /// <summary>
    /// The definition instance is shared with the layers built from it, which is what lets a later
    /// change to the interval reach the map with nothing to rewire.
    /// </summary>
    [Fact]
    public void TheDefinitionIsTheSameObjectTheLayersRead()
    {
        WpfTestHost.Run(() =>
        {
            var map = new TestMap();

            var item = ItemFor(map.MapGrids, "utm");

            item.IsChecked = true;

            var layer = (VectorLayer)map.Layers[0];

            var source = Assert.IsType<IRI.Maptor.Core.Persistence.DataSources.MapGridDataSource>(layer.DataSource);

            // Lines and values come from the one source reading the one definition, which is what
            // keeps the numbers on the lines they name when the user changes the interval.
            Assert.Same(item.Definition, source.Definition);
            Assert.Equal(MapGridKind.Utm, source.Definition.Kind);
        });
    }
}
