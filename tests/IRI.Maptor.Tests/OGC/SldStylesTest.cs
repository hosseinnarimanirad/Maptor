using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.ViewModels.Symbology;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Tests.OGC;

/// <summary>
/// Several named styles (UserStyles) in one layer SLD: the layer is drawn with exactly one of them,
/// can switch between them, and editing or resetting one leaves the others intact.
/// </summary>
public class SldStylesTest
{
    private class StubLayer : SymbolizableLayer
    {
        public override LayerType Type => LayerType.VectorLayer;

        public override Task<FeatureSet<Point>> GetFeatureSet(BoundingBox mapExtent, double mapScale)
            => Task.FromResult<FeatureSet<Point>>(null!);
    }

    private static UserStyle BuildStyle(string? name, int ruleCount, bool? isDefault = null)
    {
        var style = new UserStyle { Name = name!, Title = name is null ? null! : $"{name} title", IsDefault = isDefault };
        var featureTypeStyle = new FeatureTypeStyle();

        for (var i = 0; i < ruleCount; i++)
            featureTypeStyle.Rules.Add(new Rule { Name = $"{name}-r{i}", Symbolizers = { new LineSymbolizer() } });

        style.FeatureTypeStyles.Add(featureTypeStyle);
        return style;
    }

    // "size": 1 rule; "material": 3 rules — the symbolizer count tells which style is drawn
    private static StyledLayerDescriptor BuildTwoStyleSld(bool materialIsDefault = false)
    {
        var sld = new StyledLayerDescriptor();
        var layer = new NamedLayer { Name = "pipes" };
        layer.UserStyles.Add(BuildStyle("size", 1));
        layer.UserStyles.Add(BuildStyle("material", 3, materialIsDefault ? true : null));
        sld.NamedLayers.Add(layer);
        return sld;
    }

    private static StubLayer CreateLayer(StyledLayerDescriptor sld)
    {
        var layer = new StubLayer();
        layer.ReplaceSymbolizers(sld.ParseToSymbolizers(), sld);
        layer.CaptureDefaultSymbology();
        return layer;
    }

    [Fact]
    public void GetStyles_ListsStylesAndDefault()
    {
        var styles = BuildTwoStyleSld(materialIsDefault: true).GetStyles();

        Assert.Equal(new[] { "size", "material" }, styles.Select(s => s.Name).ToArray());
        Assert.Equal(new[] { false, true }, styles.Select(s => s.IsDefault).ToArray());
        Assert.Equal("material title", styles[1].Title);
    }

    [Fact]
    public void ParseToSymbolizers_DrawsOneStyleOnly()
    {
        var sld = BuildTwoStyleSld();

        // no IsDefault: the first style, never both stacked (1 + 3)
        Assert.Single(sld.ParseToSymbolizers());
        Assert.Equal(3, sld.ParseToSymbolizers("material").Count);
        Assert.Equal(3, BuildTwoStyleSld(materialIsDefault: true).ParseToSymbolizers().Count);

        // an unknown name falls back to the default
        Assert.Single(sld.ParseToSymbolizers("no-such-style"));
    }

    [Fact]
    public void Layer_StartsOnDefaultStyle_AndSwitches()
    {
        var layer = CreateLayer(BuildTwoStyleSld());

        Assert.Equal("size", layer.ActiveStyleName);
        Assert.True(layer.HasMultipleStyles);
        Assert.Single(layer.Symbolizers);
        Assert.Single(layer.SymbologyLegend.Groups.SelectMany(g => g.Rows));

        Assert.True(layer.SetActiveStyle("material"));

        Assert.Equal("material", layer.ActiveStyleName);
        Assert.Equal(3, layer.Symbolizers.Count);
        Assert.Equal(3, layer.SymbologyLegend.Groups.SelectMany(g => g.Rows).Count());

        // switching is not a user modification, and the SLD keeps both styles
        Assert.False(layer.IsSymbologyUserModified);
        Assert.Equal(2, layer.SourceSld!.NamedLayers[0].UserStyles.Count);

        Assert.False(layer.SetActiveStyle("no-such-style"));
        Assert.Equal("material", layer.ActiveStyleName);
    }

    [Fact]
    public void Editor_EditsTargetStyle_OthersUnchanged()
    {
        var sld = BuildTwoStyleSld();
        var vm = SldEditorViewModel.Create("pipes", sld, styleName: "material");

        Assert.Equal(3, vm.Rules.Count);
        Assert.Equal("material", vm.StyleName);

        vm.Rules.RemoveAt(0);
        vm.StyleName = "material-renamed";

        var edited = vm.ToStyledLayerDescriptor();
        var styles = edited.NamedLayers[0].UserStyles;

        Assert.Equal(2, styles.Count);
        Assert.Equal("size", styles[0].Name);
        Assert.Equal(SldHelper.Serialize(sld.ExtractStyle("size")), SldHelper.Serialize(edited.ExtractStyle("size")));
        Assert.Equal("material-renamed", styles[1].Name);
        Assert.Equal(2, styles[1].FeatureTypeStyles.Single().Rules.Count);

        // the layer keeps drawing the edited style under its new name
        var layer = CreateLayer(sld);
        layer.SetActiveStyle("material");
        layer.ApplySld(edited, vm.StyleName);

        Assert.Equal("material-renamed", layer.ActiveStyleName);
        Assert.Equal(2, layer.Symbolizers.Count);
    }

    [Fact]
    public void Reset_RestoresAllStyles_AndKeepsActiveStyle()
    {
        var layer = CreateLayer(BuildTwoStyleSld());
        layer.SetActiveStyle("material");

        // a personal tweak: the material style loses a rule
        var vm = SldEditorViewModel.Create("pipes", layer.SourceSld, styleName: layer.ActiveStyleName);
        vm.Rules.RemoveAt(0);
        layer.ApplySld(vm.ToStyledLayerDescriptor(), vm.StyleName);
        layer.IsSymbologyUserModified = true;

        Assert.Equal(2, layer.Symbolizers.Count);

        Assert.True(layer.ResetSymbologyToDefault());

        Assert.False(layer.IsSymbologyUserModified);
        Assert.Equal("material", layer.ActiveStyleName);
        Assert.Equal(3, layer.Symbolizers.Count);
        Assert.Equal(2, layer.SourceSld!.NamedLayers[0].UserStyles.Count);
    }

    [Fact]
    public void Reset_FallsBackToDefault_WhenActiveStyleIsGone()
    {
        var layer = CreateLayer(BuildTwoStyleSld());

        // the user's copy now has a style the admin's default does not
        var extra = BuildTwoStyleSld();
        extra.NamedLayers[0].UserStyles.Add(BuildStyle("local", 2));
        layer.ApplySld(extra, "local");

        Assert.Equal("local", layer.ActiveStyleName);

        Assert.True(layer.ResetSymbologyToDefault());

        Assert.Equal("size", layer.ActiveStyleName);
        Assert.Single(layer.Symbolizers);
    }

    [Fact]
    public void SingleUnnamedStyle_BehavesAsBefore()
    {
        var sld = new StyledLayerDescriptor();
        var named = new NamedLayer { Name = "legacy" };
        named.UserStyles.Add(BuildStyle(null, 2));
        sld.NamedLayers.Add(named);

        var layer = CreateLayer(sld);

        Assert.Null(layer.ActiveStyleName);
        Assert.False(layer.HasMultipleStyles);
        Assert.Single(layer.Styles);
        Assert.Equal(2, layer.Symbolizers.Count);
        Assert.Equal(2, layer.SymbologyLegend.Groups.SelectMany(g => g.Rows).Count());

        var vm = SldEditorViewModel.Create("legacy", layer.SourceSld, styleName: layer.ActiveStyleName);
        Assert.Equal(2, vm.Rules.Count);
        Assert.Single(vm.ToStyledLayerDescriptor().NamedLayers[0].UserStyles);
    }
}
