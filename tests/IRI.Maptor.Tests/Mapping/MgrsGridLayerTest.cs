using System.Linq;

using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Tests.Common;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// When the MGRS overlay writes on itself. The squares are drawn at every zoom; the text is not.
/// </summary>
/// <remarks>
/// The gate is a <c>ScaleInterval</c> on each label symbolizer, which the render strategies test
/// with the inverse map scale — the scale denominator. Reading that off a zoom level is the whole
/// subtlety, so the test goes through <see cref="WebMercatorUtility"/> exactly as the map does
/// rather than hard-coding a denominator.
/// </remarks>
[Collection(WpfCollection.Name)]
public class MgrsGridLayerTest
{
    /// <summary>What the render strategies pass to a symbolizer at a given Google zoom level.</summary>
    private static double InverseMapScaleAt(int zoomLevel) => 1.0 / WebMercatorUtility.GetGoogleMapScale(zoomLevel);

    /// <summary>Level 3 is out; 4 and everything closer in is in.</summary>
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, true)]
    [InlineData(8, true)]
    [InlineData(16, true)]
    public void LabelsAreWrittenOnlyFromZoomLevelFour(int zoomLevel, bool expected)
    {
        WpfTestHost.Run(() =>
        {
            var layer = MgrsGridLayers.Create();

            var labels = layer.Symbolizers.OfType<LabelSymbolizer>().ToList();

            // The square names and the values on the lines.
            Assert.Equal(2, labels.Count);

            foreach (var label in labels)
            {
                Assert.Equal(expected, label.Param!.IsInScaleRangeAndSelected(InverseMapScaleAt(zoomLevel)));
            }
        });
    }

    /// <summary>
    /// The squares themselves are not gated: the grid stays on the map at every zoom, it simply
    /// goes unnamed at the far end.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(16)]
    public void TheSquaresAreDrawnAtEveryZoomLevel(int zoomLevel)
    {
        WpfTestHost.Run(() =>
        {
            var layer = MgrsGridLayers.Create();

            var cells = layer.Symbolizers.OfType<SimpleSymbolizer>().Single();

            Assert.True(cells.Param!.IsInScaleRangeAndSelected(InverseMapScaleAt(zoomLevel)));
        });
    }
}
