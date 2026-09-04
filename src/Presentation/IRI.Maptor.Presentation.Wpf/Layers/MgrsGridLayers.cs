using System;
using System.Collections.Generic;
using System.Windows.Media;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;

namespace IRI.Maptor.Presentation.Wpf.Layers;

/// <summary>
/// The MGRS grid as a map layer: <strong>one</strong> layer, always visible, whose data source picks
/// the square size from the extent it is asked for.
/// </summary>
/// <remarks>
/// <para>
/// Built the way <c>IndexLayers</c> builds Iran's NCC sheet indexes — a vector data source, a
/// stroke-only symbolizer and labels — with one difference: the NCC indexes register a layer per
/// scale and gate each with a <see cref="ScaleInterval"/>, whereas this is a single layer whose data
/// source picks the level from the extent. So it is always visible and the grid simply gets finer as
/// the map zooms in.
/// </para>
/// <para>
/// The three things it draws — the squares, the name of each square, and the principal digits on the
/// lines — are one feature set told apart by a <c>Kind</c> attribute that each symbolizer filters on.
/// The renderer applies a symbolizer's filter once and uses the result for shapes and labels alike,
/// so this costs one pass each and gives the legend a single row.
/// </para>
/// <para>
/// <strong>Why not a group of three layers, which is what this was.</strong> A grouped overlay could
/// not be switched off: <c>LayerManager.Remove</c> tests its rule only against non-group layers — a
/// group is recursed into and never matched — so removing one by identity silently does nothing. The
/// grid stayed drawn while the ribbon toggle unchecked itself, and the next click put a second copy
/// on the map. The map-grid layers hit the same wall and are single layers for the same reason.
/// </para>
/// </remarks>
public static class MgrsGridLayers
{
    /// <summary>
    /// The grid: the squares, their names, and the numbers on their lines. Follows the zoom unless
    /// <paramref name="level"/> pins it.
    /// </summary>
    /// <remarks>
    /// The squares carry no text of their own. A caption is drawn only where a map would print one —
    /// the grid zone designator, or a 100 km square's two letters — while everything finer is read
    /// off the numbers on the lines, which are their own point features pinned to the edge of the
    /// view.
    /// </remarks>
    public static VectorLayer Create(string? title = null, MgrsGridLevel? level = null)
    {
        var symbolizers = new List<ISymbolizer>
        {
            CreateCells(),
            CreateSquareNames(),
            CreateAxisValues(),
        };

        return new VectorLayer(
            title ?? Localize("layer_mgrsGrid_title"),
            MgrsGridDataSource.Create(level),
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

    /// <summary>The squares themselves: a stroke, and nothing written inside them.</summary>
    private static SimpleSymbolizer CreateCells()
    {
        // Stroke only: a filled cell would hide the map underneath. The semi-transparent warm
        // stroke is the same choice the NCC sheet indexes make, and it reads on imagery and on a
        // light basemap alike.
        var symbolizer = SimpleSymbolizer.Create(hexFill: null, hexStroke: "#88EA4333", strokeThickness: 1, opacity: 0.9);

        symbolizer.IsFilterPassed = ByKind(MgrsGridDataSource.CellKind);

        return symbolizer;
    }

    /// <summary>
    /// The name of each visible square — <c>39S</c>, or <c>39S WV</c>. Larger and paler than the
    /// line values: it is the context they are read against, not a reading itself, and a paper sheet
    /// would carry it in the collar rather than on the map at all.
    /// </summary>
    private static LabelSymbolizer CreateSquareNames()
    {
        var symbolizer = LabelSymbolizer.Create(
            fontSize: 20,
            foreground: new SolidColorBrush(Color.FromArgb(120, 234, 67, 51)),
            fontFamily: new FontFamily("Consolas"),
            positionFunc: geometry => geometry.GetCentroidPlusPoint(),
            visibleRange: ScaleInterval.All,
            isRtl: false);

        symbolizer.IsFilterPassed = ByKind(MgrsGridDataSource.SquareIdKind);

        return symbolizer;
    }

    /// <summary>
    /// The principal digits on each line, against the bottom and left edges. The first line met
    /// inside each 100 km square spells its reference out in full.
    /// </summary>
    private static LabelSymbolizer CreateAxisValues()
    {
        var symbolizer = LabelSymbolizer.Create(
            fontSize: 12,
            foreground: Brushes.Red,
            fontFamily: new FontFamily("Consolas"),
            positionFunc: geometry => geometry.GetCentroidPlusPoint(),
            visibleRange: ScaleInterval.All,
            isRtl: false);

        symbolizer.IsFilterPassed = ByKind(MgrsGridDataSource.AxisValueKind);

        return symbolizer;
    }

    /// <summary>Matches the features of one kind, by the attribute the data source writes.</summary>
    private static Func<Feature<Point>, bool> ByKind(string expected)
        => feature => feature?.Attributes is not null
            && feature.Attributes.TryGetValue(MgrsGridDataSource.KindFieldName, out var value)
            && string.Equals(expected, value as string, StringComparison.Ordinal);

    private static string Localize(string key)
        => IRI.Maptor.Presentation.Core.Localization.LocalizationManager.Instance[key];
}
