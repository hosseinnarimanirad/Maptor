using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Satellite.Propagation;
using IRI.Maptor.Core.Spatial.Primitives;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;
using IRI.Maptor.Presentation.Wpf.Controls;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;

namespace IRI.Maptor.Presentation.Wpf.Layers;

/// <summary>
/// Satellites as <strong>one</strong> map layer: the point beneath each one, their names, and for the
/// selected one its ground track, the area that sees it and its imaging swath.
/// </summary>
/// <remarks>
/// <para>
/// Everything comes from a <see cref="SatellitesDataSource"/>, told apart by its <c>Kind</c> field.
/// To move the satellites, set <see cref="SatellitesDataSource.Time"/> and redraw the layer in place,
/// or let a <see cref="MapClockBinding"/> do both on every tick of a <see cref="MapClock"/>.
/// </para>
/// <para>
/// Fills keep their translucency in the brush colour, as in <see cref="DayNightLayers"/>: the renderer
/// draws each feature separately, so the swath pieces and the footprint stay see-through over the map.
/// </para>
/// </remarks>
public static class SatelliteLayers
{
    /// <param name="source">The data source; read on every render.</param>
    /// <param name="layerName">Name shown in the legend.</param>
    /// <param name="style">Colours and sizes; <see cref="SatelliteLayerStyle.Default"/> when null.</param>
    public static VectorLayer Create(SatellitesDataSource source, string layerName = "Satellites", SatelliteLayerStyle? style = null)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        style ??= SatelliteLayerStyle.Default;

        // Drawing order: swath, footprint, region, past and future track, the pass, the others, the selected one, the names
        var swath = new SimpleSymbolizer(Kind(SatellitesDataSource.SwathKind),
            new VisualParameters(Brush(style.SwathFill), Brush(style.SwathStroke), 1, 1));

        var footprint = new SimpleSymbolizer(Kind(SatellitesDataSource.FootprintKind),
            new VisualParameters(Brush(style.FootprintFill), Brush(style.FootprintStroke), 1.5, 1));

        var regionParameters = new VisualParameters(Brush(style.RegionFill), Brush(style.RegionStroke), 2, 1);
        regionParameters.PointSymbol.SymbolWidth = regionParameters.PointSymbol.SymbolHeight = style.RegionPointSize;

        var region = new SimpleSymbolizer(Kind(SatellitesDataSource.RegionKind), regionParameters);

        var pass = new SimpleSymbolizer(Kind(SatellitesDataSource.PassTrackKind),
            new VisualParameters(null, Brush(style.PassTrack), style.PassTrackThickness, 1));

        var past = new SimpleSymbolizer(Kind(SatellitesDataSource.PastTrackKind),
            new VisualParameters(null, Brush(style.PastTrack), style.PastTrackThickness, 1));

        var future = new SimpleSymbolizer(Kind(SatellitesDataSource.FutureTrackKind),
            new VisualParameters(null, Brush(style.FutureTrack), style.FutureTrackThickness, 1));

        var others = new VisualParameters(style.OtherFill, style.OtherStroke, 1.5, 1);
        others.PointSymbol.SymbolWidth = others.PointSymbol.SymbolHeight = style.OtherSize;

        var selected = new VisualParameters(style.SelectedFill, style.SelectedStroke, 2, 1);
        selected.PointSymbol.SymbolWidth = selected.PointSymbol.SymbolHeight = style.SelectedSize;

        // The renderer centres a label on the point it is given, and gives it the features in screen
        // pixels: the name goes a fixed number of pixels above the marker, not across it
        var labels = LabelSymbolizer.Create(style.LabelSize, Brush(style.LabelColor), style.LabelFont,
            geometry => Above(geometry.GetCentroidPlusPoint(), style.LabelOffset), ScaleInterval.All, isRtl: false);

        // the source names only the satellites its LabelMode asks for
        labels.IsFilterPassed = feature => feature?.LabelAttribute == SatellitesDataSource.NameFieldName;

        return new VectorLayer(
            layerName,
            source,
            new List<ISymbolizer>
            {
                swath,
                footprint,
                region,
                past,
                future,
                pass,
                new SimpleSymbolizer(Kind(SatellitesDataSource.OtherKind), others),
                new SimpleSymbolizer(Kind(SatellitesDataSource.SelectedKind), selected),
                labels,
            },
            LayerType.VectorLayer,
            RenderMode.Default,
            RasterizationMethod.DrawingVisual,
            ScaleInterval.All,
            LegendViewModel.DefaultTocGroup)
        {
            CanUserDelete = true,
        };
    }

    /// <summary>The satellite under the mouse on <paramref name="map"/>, within <paramref name="tolerance"/> pixels; null if none.</summary>
    public static Sgp4Propagator? HitTest(MapViewer map, SatellitesDataSource source, MouseEventArgs e, double tolerance = SatellitesDataSource.DefaultHitTolerance)
    {
        if (map is null)
            throw new ArgumentNullException(nameof(map));

        if (e is null)
            throw new ArgumentNullException(nameof(e));

        // the map's own screen frame: the canvas its view transform maps to
        return HitTest(map, source, e.GetPosition(map.mapView), tolerance);
    }

    /// <summary>The satellite drawn nearest to <paramref name="screenPoint"/>, in the frame of <see cref="MapViewer.GeodeticToScreen"/>; null if none.</summary>
    public static Sgp4Propagator? HitTest(MapViewer map, SatellitesDataSource source, System.Windows.Point screenPoint, double tolerance = SatellitesDataSource.DefaultHitTolerance)
    {
        if (map is null)
            throw new ArgumentNullException(nameof(map));

        if (source is null)
            throw new ArgumentNullException(nameof(source));

        return source.HitTest(new Point(screenPoint.X, screenPoint.Y), geographic =>
        {
            var screen = map.GeodeticToScreen(new System.Windows.Point(geographic.X, geographic.Y));

            return new Point(screen.X, screen.Y);
        }, tolerance);
    }

    /// <summary>The point <paramref name="pixels"/> above <paramref name="point"/> on screen (y grows downwards).</summary>
    private static Point Above(Point point, double pixels) => new(point.X, point.Y - pixels);

    private static Func<Feature<Point>, bool> Kind(string kind) => feature =>
        feature?.Attributes is not null
        && feature.Attributes.TryGetValue(SatellitesDataSource.KindFieldName, out var value)
        && string.Equals(kind, value as string, StringComparison.Ordinal);

    private static SolidColorBrush Brush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}

/// <summary>Colours and sizes of <see cref="SatelliteLayers.Create"/>. The defaults read well on satellite imagery.</summary>
public sealed class SatelliteLayerStyle
{
    public static SatelliteLayerStyle Default => new();

    /// <summary>The imager's strip; translucent amber.</summary>
    public Color SwathFill { get; set; } = Color.FromArgb(0x40, 0xFF, 0xB0, 0x20);

    public Color SwathStroke { get; set; } = Color.FromArgb(0xB0, 0xFF, 0xB0, 0x20);

    /// <summary>The area that sees the satellite; translucent sky blue.</summary>
    public Color FootprintFill { get; set; } = Color.FromArgb(0x24, 0x38, 0xBD, 0xF8);

    public Color FootprintStroke { get; set; } = Color.FromArgb(0xD0, 0x38, 0xBD, 0xF8);

    /// <summary>Where the satellite has been; faint white.</summary>
    public Color PastTrack { get; set; } = Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF);

    public double PastTrackThickness { get; set; } = 1.5;

    /// <summary>Where it is going; bright amber.</summary>
    public Color FutureTrack { get; set; } = Color.FromRgb(0xFF, 0xD5, 0x4F);

    public double FutureTrackThickness { get; set; } = 2;

    /// <summary>The region passes are found for; a light veil with a magenta edge.</summary>
    public Color RegionFill { get; set; } = Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF);

    public Color RegionStroke { get; set; } = Color.FromRgb(0xE0, 0x40, 0xFB);

    public double RegionPointSize { get; set; } = 12;

    /// <summary>The track flown during the highlighted pass; bold green.</summary>
    public Color PassTrack { get; set; } = Color.FromRgb(0x69, 0xF0, 0xAE);

    public double PassTrackThickness { get; set; } = 4;

    public Color OtherFill { get; set; } = Color.FromRgb(0x38, 0xBD, 0xF8);

    public Color OtherStroke { get; set; } = Colors.White;

    public double OtherSize { get; set; } = 10;

    public Color SelectedFill { get; set; } = Color.FromRgb(0xFF, 0xC1, 0x07);

    public Color SelectedStroke { get; set; } = Color.FromRgb(0x7A, 0x4E, 0x00);

    public double SelectedSize { get; set; } = 16;

    public int LabelSize { get; set; } = 12;

    /// <summary>The renderer backs every label with a translucent white plate, so the text is dark slate.</summary>
    public Color LabelColor { get; set; } = Color.FromRgb(0x1F, 0x29, 0x37);

    /// <summary>How far above the marker's centre the name sits, in pixels; clear of the selected marker.</summary>
    public double LabelOffset { get; set; } = 26;

    public FontFamily LabelFont { get; set; } = new("Segoe UI Semibold");
}
