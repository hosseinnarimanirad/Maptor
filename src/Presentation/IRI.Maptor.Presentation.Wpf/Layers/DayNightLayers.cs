using System;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Spatial.Primitives;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;

namespace IRI.Maptor.Presentation.Wpf.Layers;

/// <summary>
/// The day/night overlay as <strong>one</strong> map layer: the night side and its twilight bands,
/// and optionally a marker where the Sun is at the zenith.
/// </summary>
/// <remarks>
/// <para>
/// The bands are nested caps drawn one over another with the <em>same</em> translucent fill, so each
/// cap a place lies in darkens it once more: daylight is untouched, twilight slightly shaded, deep
/// night darkest. That only works because the renderer draws every feature separately; the
/// translucency therefore has to be in the <strong>brush colour</strong>. A layer opacity would be
/// applied to the finished picture, after the overlaps, and flatten all bands into one shade.
/// </para>
/// <para>
/// To move the overlay, set <see cref="DayNightDataSource.Time"/> and redraw the layer:
/// <c>presenter.ClearLayer(layer, remove: false)</c> then <c>presenter.AddLayer(layer)</c>, which keeps
/// its row and settings in the legend.
/// </para>
/// </remarks>
public static class DayNightLayers
{
    /// <summary>Deep navy, the colour of the shading.</summary>
    public const string DefaultHexFill = "#0B1A3A";

    /// <summary>Alpha of one band; four stacked give about 0.75 over full night.</summary>
    public const double DefaultBandOpacity = 0.3;

    /// <param name="source">The data source; its <c>Time</c>, <c>Bands</c> and <c>IncludeSubSolarPoint</c> are read on every render.</param>
    /// <param name="layerName">Name shown in the legend.</param>
    /// <param name="hexFill">Colour of the shading.</param>
    /// <param name="bandOpacity">Alpha of each band's fill, in (0, 1].</param>
    public static VectorLayer Create(DayNightDataSource source, string layerName = "Day and night", string hexFill = DefaultHexFill, double bandOpacity = DefaultBandOpacity)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (!(bandOpacity > 0 && bandOpacity <= 1))
            throw new ArgumentOutOfRangeException(nameof(bandOpacity), "The band opacity must be in (0, 1].");

        // Fill only: an outline would draw the antimeridian seam of a split cap and the clip edge at ±85°.
        // The bands come first, because the symbology dialog edits the first symbolizer.
        var bands = SimpleSymbolizer.Create(BrushHelper.CreateBrush(hexFill, bandOpacity), stroke: null, strokeThickness: 0, opacity: 1);

        bands.IsFilterPassed = feature => !IsKind(feature, DayNightDataSource.SubSolarPointKind);

        var sunParameters = new VisualParameters(BrushHelper.CreateBrush("#FFC107"), BrushHelper.CreateBrush("#7A4E00"), strokeThickness: 1.5, opacity: 1);

        sunParameters.PointSymbol.SymbolWidth = 14;

        sunParameters.PointSymbol.SymbolHeight = 14;

        var sun = new SimpleSymbolizer(feature => IsKind(feature, DayNightDataSource.SubSolarPointKind), sunParameters);

        return new VectorLayer(
            layerName,
            source,
            new List<ISymbolizer> { bands, sun },
            LayerType.VectorLayer,
            RenderMode.Default,
            RasterizationMethod.DrawingVisual,
            ScaleInterval.All,
            LegendViewModel.DefaultTocGroup)
        {
            CanUserDelete = true,
        };
    }

    private static bool IsKind(Feature<Point>? feature, string expected)
        => feature?.Attributes is not null
            && feature.Attributes.TryGetValue(DayNightDataSource.KindFieldName, out var value)
            && string.Equals(expected, value as string, StringComparison.Ordinal);
}
