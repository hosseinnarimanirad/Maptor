using System.Linq;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Wpf.ViewModels.Symbology;
using IRI.Maptor.Sta.Ogc.SLD;

namespace IRI.Maptor.Tst.Main.OGC;

/// <summary>
/// Safety-net behaviors of the SLD pipeline: parse errors are surfaced instead of
/// swallowed, multi-style documents survive the editor round-trip instead of being
/// truncated to the first style, and unsupported (raster) symbolizers are skipped
/// by the runtime parser instead of throwing.
/// </summary>
public class SldSafetyTest
{
    [Fact]
    public void TryParse_MalformedXml_ReportsError()
    {
        var ok = SldHelper.TryParse("<StyledLayerDescriptor><unclosed>", out var sld, out var error);

        Assert.False(ok);
        Assert.Null(sld);
        Assert.False(string.IsNullOrWhiteSpace(error));

        // the old contract stays intact
        Assert.Null(SldHelper.Parse("<StyledLayerDescriptor><unclosed>"));
        Assert.False(SldHelper.TryParse("   ", out _, out var emptyError));
        Assert.False(string.IsNullOrWhiteSpace(emptyError));
    }

    [Fact]
    public void Editor_MultiStyleDocument_PreservesUneditedParts()
    {
        var sld = new StyledLayerDescriptor();

        var primaryLayer = new NamedLayer { Name = "primary" };
        var primaryStyle = new UserStyle { Name = "styleA", IsDefault = true };
        primaryStyle.FeatureTypeStyles.Add(new FeatureTypeStyle
        {
            Rules = { new Rule { Name = "r1", Title = "original title" } }
        });
        primaryStyle.FeatureTypeStyles.Add(new FeatureTypeStyle
        {
            Rules = { new Rule { Name = "keep-me" } }
        });
        primaryLayer.UserStyles.Add(primaryStyle);
        primaryLayer.UserStyles.Add(new UserStyle
        {
            Name = "styleB",
            FeatureTypeStyles = { new FeatureTypeStyle { Rules = { new Rule { Name = "styleB-rule" } } } }
        });
        sld.NamedLayers.Add(primaryLayer);

        sld.NamedLayers.Add(new NamedLayer { Name = "other" });

        var userLayer = new UserLayer { Name = "user-layer" };
        userLayer.UserStyles.Add(new UserStyle { Name = "user-style" });
        sld.UserLayers.Add(userLayer);

        var vm = new SldEditorViewModel();
        vm.FromStyledLayerDescriptor(sld);

        Assert.True(vm.HasPreservedContent);

        // BOTH FeatureTypeStyles of the primary style are surfaced as editable rules
        Assert.Equal(2, vm.Rules.Count);
        Assert.Equal("r1", vm.Rules[0].Name);
        Assert.Equal("keep-me", vm.Rules[1].Name);

        vm.Rules[0].Title = "edited title";

        var result = vm.ToStyledLayerDescriptor();

        // edited part landed, and each rule went back to its originating FeatureTypeStyle
        var resultPrimaryStyle = result.NamedLayers[0].UserStyles[0];
        Assert.Equal(2, resultPrimaryStyle.FeatureTypeStyles.Count);
        Assert.Equal("edited title", resultPrimaryStyle.FeatureTypeStyles[0].Rules.Single().Title);
        Assert.Equal("keep-me", resultPrimaryStyle.FeatureTypeStyles[1].Rules.Single().Name);

        // unedited parts survived the trip untouched
        Assert.Equal(2, result.NamedLayers.Count);
        Assert.Equal("other", result.NamedLayers[1].Name);
        Assert.Single(result.UserLayers);
        Assert.Equal("user-layer", result.UserLayers[0].Name);
        Assert.Equal(2, result.NamedLayers[0].UserStyles.Count);
        Assert.Equal("styleB-rule", result.NamedLayers[0].UserStyles[1].FeatureTypeStyles.Single().Rules.Single().Name);
    }

    [Fact]
    public void Editor_OneRulePerFeatureTypeStyle_AllRulesEditableAndMappedBack()
    {
        // the GeoServer cookbook pattern: one UserStyle holding one FeatureTypeStyle per rule
        var sld = new StyledLayerDescriptor();
        var layer = new NamedLayer { Name = "Attribute-based line" };
        var style = new UserStyle { Title = "SLD Cook Book: Attribute-based line" };

        foreach (var name in new[] { "400", "230", "63" })
        {
            style.FeatureTypeStyles.Add(new FeatureTypeStyle
            {
                Rules = { new Rule { Name = name, Symbolizers = { new LineSymbolizer() } } }
            });
        }

        layer.UserStyles.Add(style);
        sld.NamedLayers.Add(layer);

        var vm = new SldEditorViewModel();
        vm.FromStyledLayerDescriptor(sld);

        // all three rules are editable; this layout is not "preserved content"
        Assert.Equal(new[] { "400", "230", "63" }, vm.Rules.Select(r => r.Name).ToArray());
        Assert.False(vm.HasPreservedContent);

        // round trip keeps the one-rule-per-FeatureTypeStyle structure
        var roundTripped = vm.ToStyledLayerDescriptor();
        var resultStyle = roundTripped.NamedLayers[0].UserStyles[0];
        Assert.Equal(3, resultStyle.FeatureTypeStyles.Count);
        Assert.Equal(new[] { "400", "230", "63" }, resultStyle.FeatureTypeStyles.Select(f => f.Rules.Single().Name).ToArray());

        // deleting the middle rule prunes its now-empty FeatureTypeStyle
        vm.Rules.RemoveAt(1);
        var afterDelete = vm.ToStyledLayerDescriptor();
        Assert.Equal(2, afterDelete.NamedLayers[0].UserStyles[0].FeatureTypeStyles.Count);
        Assert.Equal(new[] { "400", "63" },
            afterDelete.NamedLayers[0].UserStyles[0].FeatureTypeStyles.Select(f => f.Rules.Single().Name).ToArray());

        // a rule created in the editor lands in the last FeatureTypeStyle (drawn topmost)
        var newRule = new RuleViewModel { Name = "new-rule" };
        vm.Rules.Add(newRule);
        var afterAdd = vm.ToStyledLayerDescriptor();
        var lastFts = afterAdd.NamedLayers[0].UserStyles[0].FeatureTypeStyles.Last();
        Assert.Contains(lastFts.Rules, r => r.Name == "new-rule");
    }

    [Fact]
    public void Editor_SingleStyleDocument_HasNoPreservedContent()
    {
        var sld = new StyledLayerDescriptor();
        var layer = new NamedLayer { Name = "roads" };
        var style = new UserStyle { Name = "default" };
        style.FeatureTypeStyles.Add(new FeatureTypeStyle { Rules = { new Rule { Name = "r1" } } });
        layer.UserStyles.Add(style);
        sld.NamedLayers.Add(layer);

        var vm = new SldEditorViewModel();
        vm.FromStyledLayerDescriptor(sld);

        Assert.False(vm.HasPreservedContent);
    }

    [Fact]
    public void ParseToSymbolizers_SkipsRasterSymbolizer()
    {
        var sld = new StyledLayerDescriptor();
        var layer = new NamedLayer { Name = "mixed" };
        var style = new UserStyle { Name = "default" };
        style.FeatureTypeStyles.Add(new FeatureTypeStyle
        {
            Rules =
            {
                new Rule
                {
                    Name = "r1",
                    Symbolizers = { new RasterSymbolizer(), new PolygonSymbolizer() }
                }
            }
        });
        layer.UserStyles.Add(style);
        sld.NamedLayers.Add(layer);

        var symbolizers = sld.ParseToSymbolizers(); // must not throw

        Assert.Single(symbolizers); // only the polygon symbolizer is renderable
    }
}
