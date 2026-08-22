using System.Linq;
using System.Windows.Media;

using IRI.Maptor.Presentation.Wpf.ViewModels.Symbology;
using IRI.Maptor.Core.Ogc.SLD;

namespace IRI.Maptor.Tests.OGC;

/// <summary>
/// Round-trips an SLD authored through the WPF editor view-models:
/// SldEditorViewModel -> StyledLayerDescriptor -> SldHelper.Serialize (XML)
/// -> SldHelper.Parse -> SldEditorViewModel, asserting that all five symbolizer
/// types, the color map, and the simple filter survive the trip.
/// </summary>
public class SldEditorRoundTripTest
{
    private static SldEditorViewModel BuildEditor()
    {
        var vm = new SldEditorViewModel
        {
            LayerName = "roads",
            StyleName = "default",
            StyleTitle = "Road Style"
        };

        var rule = new RuleViewModel
        {
            Name = "primary-roads",
            Title = "Primary roads",
            MinScale = 1000,
            MaxScale = 500000,
            HasFilter = true,
            FilterPropertyName = "type",
            FilterOperator = FilterComparisonOperator.Equal,
            FilterValue = "primary"
        };

        rule.Symbolizers.Add(new PointSymbolizerViewModel
        {
            WellKnownMarkType = WellKnownMark.circle,
            Size = 12,
            FillColor = Colors.Red,
            StrokeColor = Colors.Black,
            StrokeWidth = 2
        });

        rule.Symbolizers.Add(new LineSymbolizerViewModel
        {
            StrokeColor = Colors.Blue,
            StrokeWidth = 3,
            StrokeOpacity = 0.75,
            DashArray = "5 5",
            DashOffset = 5
        });

        rule.Symbolizers.Add(new PolygonSymbolizerViewModel
        {
            FillColor = Colors.Green,
            FillOpacity = 0.5,
            StrokeColor = Colors.Black,
            StrokeWidth = 1
        });

        rule.Symbolizers.Add(new TextSymbolizerViewModel
        {
            LabelPropertyName = "name",
            FontFamily = "Arial",
            FontSize = 14,
            FontColor = Colors.Black,
            EnableHalo = true,
            HaloRadius = 2,
            HaloColor = Colors.White,
            HaloOpacity = 0.5
        });

        var raster = new RasterSymbolizerViewModel { Opacity = 0.8 };
        raster.ColorMap.Add(new ColorMapEntryViewModel { Color = Colors.Blue, Quantity = 100, Label = "low", Opacity = 1.0 });
        raster.ColorMap.Add(new ColorMapEntryViewModel { Color = Colors.Red, Quantity = 200, Label = "high", Opacity = 0.5 });
        rule.Symbolizers.Add(raster);

        vm.Rules.Add(rule);
        return vm;
    }

    [Fact]
    public void SldEditor_SerializeParse_PreservesAllSymbolizersFilterAndColorMap()
    {
        // Arrange
        var editor = BuildEditor();

        // Act: VM -> model -> XML
        var sld = editor.ToStyledLayerDescriptor();
        var xml = SldHelper.Serialize(sld);

        Assert.False(string.IsNullOrWhiteSpace(xml));

        // XML -> model -> VM
        var parsed = SldHelper.Parse(xml);
        Assert.NotNull(parsed);

        var reloaded = new SldEditorViewModel();
        reloaded.FromStyledLayerDescriptor(parsed!);

        // Assert: document scaffolding
        Assert.Equal("roads", reloaded.LayerName);
        var rule = Assert.Single(reloaded.Rules);
        Assert.Equal("primary-roads", rule.Name);
        Assert.Equal(1000d, rule.MinScale);
        Assert.Equal(500000d, rule.MaxScale);

        // Assert: filter survived
        Assert.True(rule.HasFilter);
        Assert.Equal("type", rule.FilterPropertyName);
        Assert.Equal(FilterComparisonOperator.Equal, rule.FilterOperator);
        Assert.Equal("primary", rule.FilterValue);

        // Assert: all five symbolizer types survived, in order
        Assert.Equal(
            new[] { "Point", "Line", "Polygon", "Text", "Raster" },
            rule.Symbolizers.Select(s => s.SymbolizerType).ToArray());

        // Assert: line dash offset survived
        var line = rule.Symbolizers.OfType<LineSymbolizerViewModel>().Single();
        Assert.Equal(5d, line.DashOffset);

        // Assert: text halo opacity survived
        var text = rule.Symbolizers.OfType<TextSymbolizerViewModel>().Single();
        Assert.True(text.EnableHalo);
        Assert.Equal(0.5d, text.HaloOpacity);

        // Assert: raster opacity + color map survived
        var raster = rule.Symbolizers.OfType<RasterSymbolizerViewModel>().Single();
        Assert.Equal(0.8d, raster.Opacity);
        Assert.Equal(2, raster.ColorMap.Count);

        var first = raster.ColorMap[0];
        Assert.Equal(Colors.Blue, first.Color);
        Assert.Equal(100d, first.Quantity);
        Assert.Equal("low", first.Label);
        Assert.Equal(1.0d, first.Opacity);
    }
}
