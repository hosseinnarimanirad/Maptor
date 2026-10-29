using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;
using IRI.Maptor.Presentation.Wpf.Models.LayerSettingsAdmin;
using IRI.Maptor.Presentation.Wpf.ViewModels.LayerSettings;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Core.Spatial.Dtos;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Tests.OGC;

/// <summary>
/// Managing a layer's named styles: the admin's style-list actions on the SLD and on the admin row,
/// symbolizers kept across styles, and the users' Symbology tab switching between styles.
/// </summary>
public class SldStyleListTest
{
    private class StubLayer : SymbolizableLayer
    {
        public override LayerType Type => LayerType.VectorLayer;

        public override Task<FeatureSet<Point>> GetFeatureSet(BoundingBox mapExtent, double mapScale)
            => Task.FromResult<FeatureSet<Point>>(null!);
    }

    private static UserStyle BuildStyle(string? name, int ruleCount, string? title = null)
    {
        var style = new UserStyle { Name = name!, Title = title! };
        var featureTypeStyle = new FeatureTypeStyle();

        for (var i = 0; i < ruleCount; i++)
            featureTypeStyle.Rules.Add(new Rule { Name = $"r{i}", Symbolizers = { new LineSymbolizer() } });

        style.FeatureTypeStyles.Add(featureTypeStyle);
        return style;
    }

    private static StyledLayerDescriptor BuildSld(params UserStyle[] styles)
    {
        var sld = new StyledLayerDescriptor();
        var layer = new NamedLayer { Name = "pipes" };
        layer.UserStyles.AddRange(styles);
        sld.NamedLayers.Add(layer);
        return sld;
    }

    private static ServerLayerSettingItem CreateRow(string? sld) => new(new LayerSettingAdminDto
    {
        Id = 7,
        Title = "Pipes",
        IsActive = true,
        HexStroke = "#FF3366CC",
        StrokeThickness = 1,
        Opacity = 1,
        LayerSld = sld,
    }, 100);

    [Fact]
    public void EnsureStyleNames_NamesLegacyStylesAndKeepsOneDefault()
    {
        var sld = BuildSld(BuildStyle(null, 1), BuildStyle("dup", 1), BuildStyle("dup", 1));
        sld.NamedLayers[0].UserStyles[1].IsDefault = true;
        sld.NamedLayers[0].UserStyles[2].IsDefault = true;

        Assert.True(sld.EnsureStyleNames());

        var styles = sld.GetStyles();
        Assert.Equal(3, styles.Select(s => s.Name).Distinct().Count());
        Assert.All(styles, s => Assert.False(string.IsNullOrWhiteSpace(s.Name)));
        Assert.Single(sld.NamedLayers[0].UserStyles, s => s.IsDefault == true);
        Assert.Equal("dup", sld.NamedLayers[0].UserStyles[1].Name);

        Assert.False(sld.EnsureStyleNames());
    }

    [Fact]
    public void AddCopyMoveRemove_KeepNamesAndDefault()
    {
        var sld = new StyledLayerDescriptor();

        var first = sld.AddStyle(BuildStyle(null, 1, "By size"));
        Assert.Equal("style-1", first);
        Assert.True(sld.FindStyle(first)!.IsDefault);

        var copy = sld.CopyStyle(first, "By material")!;
        Assert.Equal("style-2", copy);
        Assert.Null(sld.FindStyle(copy)!.IsDefault);
        Assert.Equal(new[] { "By size", "By material" }, sld.GetStyles().Select(s => s.Title).ToArray());

        // the copy is a deep copy
        sld.FindStyle(copy)!.FeatureTypeStyles[0].Rules.Clear();
        Assert.Single(sld.FindStyle(first)!.FeatureTypeStyles[0].Rules);

        Assert.True(sld.MoveStyle(copy, -1));
        Assert.Equal(new[] { copy, first }, sld.GetStyles().Select(s => s.Name).ToArray());
        Assert.False(sld.MoveStyle(copy, -1));

        Assert.True(sld.SetDefaultStyle(copy));
        Assert.Equal(copy, sld.GetDefaultStyleName());

        // removing the default hands the default to the first remaining style
        Assert.True(sld.RemoveStyle(copy));
        Assert.Equal(first, sld.GetDefaultStyleName());

        // renaming the title keeps the identity
        Assert.True(sld.SetStyleTitle(first, "Pipes by size"));
        Assert.Equal(first, sld.GetStyles().Single().Name);
    }

    [Fact]
    public void AdminRow_StyleActionsRewriteLayerSld_AndRevertRestores()
    {
        var original = SldHelper.Serialize(BuildSld(BuildStyle("size", 1, "By size")));
        var row = CreateRow(original);

        Assert.Single(row.Styles);
        Assert.False(row.IsDirty);

        row.CopyStyle(row.Styles[0], "By material");

        Assert.True(row.IsDirty);
        Assert.Equal(new[] { "By size", "By material" }, row.Styles.Select(s => s.Title).ToArray());
        Assert.Equal("By material", row.SelectedStyle!.Title);

        row.SetDefaultStyle(row.SelectedStyle);
        Assert.True(row.Styles[1].IsDefault);

        // editing a title in the list renames the style, not its identity
        var name = row.Styles[1].Name;
        row.Styles[1].Title = "Pipes by material";
        Assert.Equal("Pipes by material", row.Styles.Single(s => s.Name == name).Title);

        row.Revert();
        Assert.Single(row.Styles);
        Assert.False(row.IsDirty);
    }

    [Theory]
    [InlineData(SpatialModelMode.Polyline)]
    [InlineData(SpatialModelMode.Polygon)]
    [InlineData(SpatialModelMode.Point)]
    [InlineData(null)]
    public void AdminFlow_AddCopyEdit_ThenEveryStyleDraws(SpatialModelMode? geometry)
    {
        // the admin flow: Add (from the row's simple colours), Copy, edit the copy in the SLD editor
        var row = CreateRow(null);

        var template = new ISymbolizer[] { SimpleSymbolizer.Create(null, "#FFE0338F", 2, 1) }.ParseToSld(geometry).FindStyleAt(0)!;
        row.AddStyle(template, "Style 1");
        row.CopyStyle(row.Styles[0], "Style 2");

        var sld = SldHelper.Parse(row.LayerSld!)!;
        var editor = IRI.Maptor.Presentation.Wpf.ViewModels.Symbology.SldEditorViewModel.Create("pipes", sld, styleName: row.Styles[1].Name);
        row.ReplaceSld(editor.ToStyledLayerDescriptor(), editor.StyleName);

        var stored = SldHelper.Parse(row.LayerSld!)!;
        var layer = new StubLayer();
        layer.ApplySld(stored);

        foreach (var style in layer.Styles)
            Assert.True(layer.SetActiveStyle(style.Name!), style.Name);
    }

    [Fact]
    public void SwitchingStyle_AnnouncesOnlyTheFinalStyle()
    {
        // a picker bound two-way to SelectedStyleName switches again on every announced value: announcing
        // the default style on the way (as the SourceSld setter does) made it loop until the stack overflowed
        var sld = BuildSld(BuildStyle("size", 1, "By size"), BuildStyle("material", 3, "By material"));
        var layer = new StubLayer();
        layer.ApplySld(sld);

        var announced = new System.Collections.Generic.List<string?>();
        var stylesAnnounced = 0;
        layer.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SymbolizableLayer.SelectedStyleName))
                announced.Add(layer.SelectedStyleName);
            if (e.PropertyName == nameof(SymbolizableLayer.Styles))
                stylesAnnounced++;
        };

        var styles = layer.Styles;

        layer.SelectedStyleName = "material";

        Assert.Equal(new string?[] { "material" }, announced);
        Assert.Equal(0, stylesAnnounced);
        Assert.Same(styles, layer.Styles);

        // a picker answering the notification with another switch is ignored while the first one runs
        layer.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SymbolizableLayer.SelectedStyleName))
                layer.SelectedStyleName = "size";
        };

        layer.SelectedStyleName = "size";
        Assert.Equal("size", layer.ActiveStyleName);
    }

    [Fact]
    public void AdminRow_ModeFollowsTheSld_AndThePreviewFollowsTheSelectedStyle()
    {
        var row = CreateRow(null);
        Assert.True(row.IsSimpleMode);
        Assert.Empty(row.SelectedStyleLegendRows);

        row.AddStyle(BuildStyle(null, 1, "By size"), "By size");
        row.CopyStyle(row.Styles[0], "By material");
        Assert.False(row.IsSimpleMode);

        // the copy is selected; give it more rules through the editor path and check the preview follows
        var sld = SldHelper.Parse(row.LayerSld!)!;
        sld.FindStyleAt(1)!.FeatureTypeStyles[0].Rules.Add(new Rule { Name = "r1", Symbolizers = { new LineSymbolizer() } });
        row.ReplaceSld(sld, row.Styles[1].Name);

        Assert.Equal("By material", row.SelectedStyle!.Title);
        Assert.Equal(2, row.SelectedStyleLegendRows.Count);

        row.SelectedStyle = row.Styles[0];
        Assert.Single(row.SelectedStyleLegendRows);

        row.LayerSld = null;
        Assert.True(row.IsSimpleMode);
        Assert.Empty(row.SelectedStyleLegendRows);
    }

    [Fact]
    public void AdminRow_MoveKeepsTheMovedStyleSelected()
    {
        var row = CreateRow(SldHelper.Serialize(BuildSld(BuildStyle("a", 1, "A"), BuildStyle("b", 1, "B"), BuildStyle("c", 1, "C"))));

        row.SelectedStyle = row.Styles[2];
        row.MoveStyle(row.SelectedStyle, -1);

        Assert.Equal(new[] { "A", "C", "B" }, row.Styles.Select(s => s.Title).ToArray());
        Assert.Equal("C", row.SelectedStyle!.Title);

        row.MoveStyle(row.SelectedStyle, -1);
        Assert.Equal(new[] { "C", "A", "B" }, row.Styles.Select(s => s.Title).ToArray());
        Assert.Equal("C", row.SelectedStyle!.Title);

        row.RemoveStyle(row.SelectedStyle);
        Assert.Equal(new[] { "A", "B" }, row.Styles.Select(s => s.Title).ToArray());
        Assert.NotNull(row.SelectedStyle);
    }

    [Fact]
    public void AdminRow_DuplicateOrMissingTitles_AreErrors()
    {
        var row = CreateRow(SldHelper.Serialize(BuildSld(BuildStyle("a", 1, "Same"), BuildStyle("b", 1, "same"))));

        Assert.Contains(LayerSettingError.DuplicateStyleTitle, row.Errors);

        row.Styles[1].Title = "";
        Assert.Contains(LayerSettingError.StyleTitleRequired, row.Errors);

        row.Styles[1].Title = "Other";
        Assert.DoesNotContain(LayerSettingError.StyleTitleRequired, row.Errors);
        Assert.DoesNotContain(LayerSettingError.DuplicateStyleTitle, row.Errors);

        // a single legacy style without a title is fine
        Assert.DoesNotContain(LayerSettingError.StyleTitleRequired, CreateRow(SldHelper.Serialize(BuildSld(BuildStyle(null, 1)))).Errors);
    }

    [Fact]
    public void AdminRow_RemovingLastStyle_RemovesTheSld()
    {
        var row = CreateRow(null);

        row.AddStyle(BuildStyle(null, 1), "Style 1");
        Assert.True(row.HasSld);
        Assert.Single(row.Styles);

        row.RemoveStyle(row.Styles[0]);
        Assert.False(row.HasSld);
        Assert.Empty(row.Styles);
        Assert.False(row.IsDirty);
    }

    [Fact]
    public void KeepAcrossStyles_SurvivesSwitchAndReset_AndStaysOutOfGetSld()
    {
        var sld = BuildSld(BuildStyle("size", 1, "By size"), BuildStyle("material", 3, "By material"));
        var layer = new StubLayer();
        layer.ReplaceSymbolizers(sld.ParseToSymbolizers(), sld);

        var label = new LabelSymbolizer(VisualParameters.CreateLabel(ScaleInterval.All, 10, System.Windows.Media.Brushes.Black, new System.Windows.Media.FontFamily("Arial"), null!, isRtl: false), "name");
        layer.KeepAcrossStyles(label);
        layer.CaptureDefaultSymbology();

        Assert.Contains(label, layer.Symbolizers);

        layer.SetActiveStyle("material");
        Assert.Contains(label, layer.Symbolizers);
        Assert.Equal(4, layer.Symbolizers.Count);

        Assert.True(layer.ResetSymbologyToDefault());
        Assert.Contains(label, layer.Symbolizers);

        var withoutSourceSld = new StubLayer();
        withoutSourceSld.ReplaceSymbolizers([SimpleSymbolizer.Create("#FF0000FF", "#FF000000", 1, 1)]);
        withoutSourceSld.KeepAcrossStyles(label);
        Assert.DoesNotContain(withoutSourceSld.GetSld().NamedLayers[0].UserStyles[0].FeatureTypeStyles[0].Rules,
            r => r.Symbolizers.Any(s => s is TextSymbolizer));
    }

    [Fact]
    public void SymbologyTab_SwitchesStyle_AndFollowsTheLayer()
    {
        var sld = BuildSld(BuildStyle("size", 1, "By size"), BuildStyle("material", 3, "By material"));
        var layer = new StubLayer();
        layer.ReplaceSymbolizers(sld.ParseToSymbolizers(), sld);
        layer.CaptureDefaultSymbology();

        var repaints = 0;
        layer.RequestRepaint = _ => repaints++;

        var tab = new LayerSettings_SymbologyViewModel(layer);

        Assert.True(tab.HasMultipleStyles);
        Assert.Equal("By size", tab.SelectedStyle!.Title);
        Assert.Single(tab.LegendRows);

        tab.SelectedStyle = tab.Styles[1];

        Assert.Equal("material", layer.ActiveStyleName);
        Assert.Equal(1, repaints);
        Assert.Equal(3, tab.LegendRows.Count);

        // a switch from elsewhere (the legend) shows up in the tab
        layer.SelectedStyleName = "size";
        Assert.Equal("By size", tab.SelectedStyle!.Title);
        Assert.Single(tab.LegendRows);
        Assert.False(tab.IsUserModified);
    }

    [Fact]
    public void SymbologyTab_MarksTheStylesTheUserChanged_AndNamesTheLegend()
    {
        var sld = BuildSld(BuildStyle("size", 1, "By size"), BuildStyle("material", 3, "By material"));
        var layer = new StubLayer();
        layer.ReplaceSymbolizers(sld.ParseToSymbolizers(), sld);
        layer.CaptureDefaultSymbology();

        var tab = new LayerSettings_SymbologyViewModel(layer);

        Assert.False(tab.IsSimple);
        Assert.Contains("By size", tab.LegendTitle);
        Assert.True(tab.Styles[0].IsDefault);
        Assert.All(tab.Styles, s => Assert.False(s.IsUserChanged));

        // the user edits "By material" in the SLD editor (one rule fewer)
        var edited = BuildSld(BuildStyle("size", 1, "By size"), BuildStyle("material", 2, "By material"));
        layer.ApplySld(edited, "material");
        layer.IsSymbologyUserModified = true;

        Assert.Contains("By material", tab.LegendTitle);
        Assert.False(tab.Styles[0].IsUserChanged);
        Assert.True(tab.Styles[1].IsUserChanged);
        Assert.Contains("By material", tab.ChangesText);
        Assert.DoesNotContain("By size", tab.ChangesText);

        Assert.True(layer.ResetSymbologyToDefault());
        Assert.All(tab.Styles, s => Assert.False(s.IsUserChanged));
        Assert.Equal(3, tab.LegendRows.Count);

        // a layer without an SLD: no style list, the simple-colours hint
        var simple = new StubLayer();
        simple.ReplaceSymbolizers([SimpleSymbolizer.Create("#FF0000FF", "#FF000000", 1, 1)]);
        simple.CaptureDefaultSymbology();
        var simpleTab = new LayerSettings_SymbologyViewModel(simple);

        Assert.True(simpleTab.IsSimple);
        Assert.False(simpleTab.HasMultipleStyles);
        Assert.True(simpleTab.CanQuickEdit);
    }

    [Fact]
    public void SimpleLayer_StaysSimpleThroughRestoreAndProjectReplay()
    {
        var layer = new StubLayer { LayerName = "Valves" };
        layer.ReplaceSymbolizers([SimpleSymbolizer.Create("#FF0000FF", "#FF000000", 1, 1)]);
        layer.CaptureDefaultSymbology();

        // its one unnamed rule stands for the layer: the legend row shows the layer name
        Assert.Equal("Valves", Assert.Single(layer.SymbologyLegend.Groups.SelectMany(g => g.Rows)).Title);

        // ...and follows a rename
        layer.LayerName = "Main valves";
        Assert.Equal("Main valves", Assert.Single(layer.SymbologyLegend.Groups.SelectMany(g => g.Rows)).Title);

        // the quick dialog changes the colours in place: the legend is rebuilt
        var legend = layer.SymbologyLegend;
        layer.GetMainOrDefaultSymbology().StrokeThickness = 4;
        layer.RaiseSymbologyChanged();
        Assert.NotSame(legend, layer.SymbologyLegend);

        // the project saves the quick colours as an SLD; replaying them keeps the layer simple
        var saved = layer.GetSld();
        var reopened = new StubLayer();
        reopened.ReplaceSymbolizers([SimpleSymbolizer.Create("#FF0000FF", "#FF000000", 1, 1)]);
        reopened.ApplySavedSld(saved, null);
        Assert.Null(reopened.SourceSld);
        Assert.Equal(4, reopened.GetMainOrDefaultSymbology().StrokeThickness);

        // a rule-based SLD (saved from the SLD editor) becomes the layer's SLD
        var ruled = new StubLayer();
        ruled.ReplaceSymbolizers([SimpleSymbolizer.Create("#FF0000FF", "#FF000000", 1, 1)]);
        ruled.ApplySavedSld(BuildSld(BuildStyle("size", 2, "By size")), "size");
        Assert.NotNull(ruled.SourceSld);

        // the SLD editor on a simple layer, then Restore: back to simple colours, not an SLD of them
        layer.ApplySld(BuildSld(BuildStyle("size", 2, "By size")));
        Assert.NotNull(layer.SourceSld);
        Assert.True(layer.ResetSymbologyToDefault());
        Assert.Null(layer.SourceSld);
        Assert.Equal(1, layer.GetMainOrDefaultSymbology().StrokeThickness);
    }
}
