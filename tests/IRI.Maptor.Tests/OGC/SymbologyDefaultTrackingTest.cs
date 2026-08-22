using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Tests.OGC;

/// <summary>
/// Default-symbology tracking on <see cref="SymbolizableLayer"/>: capturing the
/// creation-time style, flagging user edits, and resetting back to the pristine
/// snapshot (the basis for save-only-real-overrides in project files).
/// </summary>
public class SymbologyDefaultTrackingTest
{
    private class StubLayer : SymbolizableLayer
    {
        public override LayerType Type => LayerType.VectorLayer;

        public override Task<FeatureSet<Point>> GetFeatureSet(BoundingBox mapExtent, double mapScale)
            => Task.FromResult<FeatureSet<Point>>(null!);
    }

    private static StyledLayerDescriptor BuildSld(string styleName)
    {
        var sld = new StyledLayerDescriptor();
        var layer = new NamedLayer { Name = "layer" };
        var style = new UserStyle { Name = styleName };
        style.FeatureTypeStyles.Add(new FeatureTypeStyle
        {
            Rules = { new Rule { Name = "r1", Symbolizers = { new PolygonSymbolizer() } } }
        });
        layer.UserStyles.Add(style);
        sld.NamedLayers.Add(layer);
        return sld;
    }

    [Fact]
    public void CaptureAndReset_RestoresDefaultSymbology()
    {
        var serverSld = BuildSld("server-style");

        var layer = new StubLayer();
        layer.ReplaceSymbolizers(serverSld.ParseToSymbolizers(), serverSld);
        layer.CaptureDefaultSymbology();

        Assert.True(layer.CanResetSymbology);
        Assert.False(layer.IsSymbologyUserModified);

        // simulate a user edit through the SLD editor apply path
        var editedSld = BuildSld("edited-style");
        layer.ReplaceSymbolizers(editedSld.ParseToSymbolizers(), editedSld);
        layer.IsSymbologyUserModified = true;

        Assert.Equal("edited-style", layer.SourceSld!.NamedLayers[0].UserStyles[0].Name);

        Assert.True(layer.ResetSymbologyToDefault());

        Assert.False(layer.IsSymbologyUserModified);
        Assert.Equal("server-style", layer.SourceSld!.NamedLayers[0].UserStyles[0].Name);
        Assert.Single(layer.Symbolizers);

        // the layer got a clone; the pristine snapshot stays isolated from later edits
        Assert.NotSame(layer.DefaultSld, layer.SourceSld);
    }

    [Fact]
    public void WithoutCapturedDefault_ResetIsUnavailable()
    {
        var layer = new StubLayer();

        Assert.False(layer.CanResetSymbology);
        Assert.False(layer.ResetSymbologyToDefault());
    }

    [Fact]
    public void Reset_SurvivesRepeatedEdits()
    {
        var serverSld = BuildSld("server-style");

        var layer = new StubLayer();
        layer.ReplaceSymbolizers(serverSld.ParseToSymbolizers(), serverSld);
        layer.CaptureDefaultSymbology();

        for (var i = 0; i < 3; i++)
        {
            var edited = BuildSld($"edit-{i}");
            layer.ReplaceSymbolizers(edited.ParseToSymbolizers(), edited);
            layer.IsSymbologyUserModified = true;

            Assert.True(layer.ResetSymbologyToDefault());
            Assert.Equal("server-style", layer.SourceSld!.NamedLayers[0].UserStyles[0].Name);
        }
    }
}
