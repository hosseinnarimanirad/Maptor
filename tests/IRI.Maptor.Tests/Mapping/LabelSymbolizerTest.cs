using System.Windows.Media;

using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;
using IRI.Maptor.Tests.Common;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// Guards the gate every label in the application has to pass.
/// </summary>
/// <remarks>
/// Both render strategies draw a label only when
/// <c>Param.IsInScaleRangeAndSelected(1 / mapScale)</c> is true, and that is
/// <c>VisibleRange.IsInRange(...) &amp;&amp; IsSelected</c>. <c>VisualParameters.CreateLabel</c>
/// built its result through an object initializer instead of the constructor the other factories
/// use with <c>isOn: true</c>, so <c>IsSelected</c> stayed at its default of false and nothing
/// built by <see cref="LabelSymbolizer.Create"/> ever drew — not the MGRS grid, not the NCC sheet
/// indexes, not anything. Fixed 2026-08-29; this is here so it cannot come back silently.
/// </remarks>
[Collection(WpfCollection.Name)]
public class LabelSymbolizerTest
{
    private static LabelSymbolizer Create(ScaleInterval visibleRange)
    {
        LabelSymbolizer? symbolizer = null;

        WpfTestHost.Run(() =>
        {
            symbolizer = LabelSymbolizer.Create(
                fontSize: 12,
                foreground: Brushes.Red,
                fontFamily: new FontFamily("Consolas"),
                positionFunc: geometry => geometry.GetCentroidPlusPoint(),
                visibleRange: visibleRange,
                isRtl: false);
        });

        Assert.NotNull(symbolizer);

        return symbolizer!;
    }

    [Fact]
    public void Create_MarksTheLabelAsOn()
    {
        Assert.True(Create(ScaleInterval.All).Param.IsSelected);
    }

    /// <summary>A label with an unbounded scale range draws at every zoom, which is the point of it.</summary>
    [Theory]
    [InlineData(1.0 / 1000.0)]
    [InlineData(1.0 / 1000000.0)]
    [InlineData(1.0 / 10.0)]
    public void Create_WithAllScales_PassesTheRenderGate(double inverseMapScale)
    {
        Assert.True(Create(ScaleInterval.All).Param.IsInScaleRangeAndSelected(inverseMapScale));
    }

    /// <summary>The scale range still applies — the fix turns the label on, it does not ignore the range.</summary>
    [Fact]
    public void Create_OutsideItsScaleRange_StillDoesNotDraw()
    {
        var symbolizer = Create(ScaleInterval.Create(10, 12));

        Assert.True(symbolizer.Param.IsSelected);

        Assert.False(symbolizer.Param.IsInScaleRangeAndSelected(1.0 / 100000000.0));
    }
}
