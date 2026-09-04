using System;
using System.Collections.Generic;
using System.Windows.Media;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Spatial.Helpers.MapGrids;
using IRI.Maptor.Core.Spatial.Primitives;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;

namespace IRI.Maptor.Presentation.Wpf.Layers;

/// <summary>
/// A map grid as a map layer: <strong>one</strong> layer per grid.
/// </summary>
/// <remarks>
/// <para>
/// Everything a grid draws — the principal lines, their subdivisions, the UTM zone seam and the
/// values written against the edges — comes from one data source and one layer, told apart by the
/// <c>Kind</c> attribute that each symbolizer filters on. The renderer computes its filtered feature
/// list once per symbolizer and uses it for shapes and labels alike, so this costs one pass per
/// weight and gives the legend a single row for the grid.
/// </para>
/// <para>
/// <strong>Why not a group of layers.</strong> Two earlier shapes were tried and both failed. A
/// layer per weight was rejected outright — a grid should be one thing in the legend. A
/// <see cref="GroupLayer"/> holding a lines layer and a values layer looked right but could not be
/// taken off the map again: <c>LayerManager.Remove</c> tests its rule only against non-group layers
/// (a group is recursed into and never matched), so removing a group by identity does nothing. The
/// grid stayed drawn, the menu item unchecked itself, and the next tick added a second copy. One
/// plain <see cref="VectorLayer"/> has none of that.
/// </para>
/// </remarks>
public static class MapGridLayers
{
    /// <summary>
    /// The grid, as one layer: its lines in three weights, and the values written on them at the
    /// edges of the view.
    /// </summary>
    /// <param name="definition">
    /// The grid. Held by reference by the data source, so later edits to its interval, label sides
    /// or tier take effect on the next render.
    /// </param>
    /// <param name="style">Colours and weights; the default is chosen from the grid's kind.</param>
    /// <param name="options">The engine's knobs; the shared defaults when omitted.</param>
    public static VectorLayer Create(MapGridDefinition definition, MapGridStyle? style = null, MapGridOptions? options = null)
    {
        if (definition is null)
            throw new ArgumentNullException(nameof(definition));

        var theStyle = style ?? MapGridStyle.For(definition);

        var major = CreateStroke(theStyle.Hex, theStyle.MajorThickness, theStyle.MajorOpacity, MapGridLineKind.Major);

        var minor = CreateStroke(theStyle.Hex, theStyle.MinorThickness, theStyle.MinorOpacity, MapGridLineKind.Minor);

        var seam = CreateStroke(theStyle.Hex, theStyle.SeamThickness, theStyle.SeamOpacity, MapGridLineKind.ZoneSeam);

        // The principal weight goes first, because that is the one the symbology dialog edits:
        // SymbolizableLayer.GetMainOrDefaultSymbology() returns the first symbolizer, and seeding
        // the dialog from the faintest subdivision made a colour change look like it had done
        // nothing. The others then follow its colour, so one edit recolours the whole grid.
        MapGridSymbologyLink.Attach(major.Param, minor.Param, seam.Param);

        // Stacking follows the same order: subdivisions and the seam draw over the principal lines,
        // and the values over everything. Subdivisions never coincide with a principal line — only
        // cross one — and at 50% opacity a crossing is a faint dot, while the seam and the numbers
        // genuinely belong on top.
        var symbolizers = new List<ISymbolizer> { major, minor, seam, CreateLabels(theStyle) };

        return new VectorLayer(
            definition.Title,
            MapGridDataSource.Create(definition, options),
            symbolizers,
            LayerType.VectorLayer,
            RenderMode.Default,
            RasterizationMethod.DrawingVisual,
            ScaleInterval.All,
            LegendViewModel.DefaultTocGroup)
        {
            CanUserDelete = true,
        };
    }

    /// <summary>One line weight: a stroke, matched to the features that carry that weight.</summary>
    private static SimpleSymbolizer CreateStroke(string hex, double thickness, double opacity, MapGridLineKind kind)
    {
        // Stroke only, never a fill: these are lines, and a fill on an unclosed polyline would be
        // drawn as though it were closed.
        var symbolizer = SimpleSymbolizer.Create(hexFill: null, hexStroke: hex, strokeThickness: thickness, opacity: opacity);

        symbolizer.IsFilterPassed = ByKind(kind.ToString());

        return symbolizer;
    }

    /// <summary>
    /// The values on the lines. Filtered to the point features that carry text, so the line
    /// symbolizers never see them and this never sees a line.
    /// </summary>
    private static LabelSymbolizer CreateLabels(MapGridStyle style)
    {
        var labels = LabelSymbolizer.Create(
            fontSize: style.FontSize,
            foreground: BrushHelper.CreateBrush(style.Hex) ?? Brushes.Black,
            // A grid value is a number read digit by digit; a monospaced face keeps the column of
            // them aligned down the margin, and carries the superscripts the full form uses.
            fontFamily: new FontFamily("Consolas"),
            positionFunc: geometry => geometry.GetCentroidPlusPoint(),
            visibleRange: ScaleInterval.All,
            isRtl: false);

        labels.IsFilterPassed = ByKind(MapGridDataSource.LabelKind);

        return labels;
    }

    /// <summary>Matches the features of one kind, by the attribute the data source writes.</summary>
    private static Func<Feature<Point>, bool> ByKind(string expected)
        => feature => feature?.Attributes is not null
            && feature.Attributes.TryGetValue(MapGridDataSource.KindFieldName, out var value)
            && string.Equals(expected, value as string, StringComparison.Ordinal);
}
