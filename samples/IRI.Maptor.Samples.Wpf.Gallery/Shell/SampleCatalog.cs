using System.Collections.Generic;
using MahApps.Metro.IconPacks;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.AttributeTable;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.BasicMap;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.DayNight;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.DelaunayVoronoi;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.DrawingLegend;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.GoTo;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.Identify;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.Localization;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.MapLegend;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.MapMarkers;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.Measurement;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.SatellitePosition;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.ThemeAndControls;

namespace IRI.Maptor.Samples.Wpf.Gallery.Shell;

/// <summary>
/// The list of samples, in display order. To add a sample: create a folder under <c>Samples/</c>
/// with a UserControl and a README.md, then add one line here.
/// </summary>
public static class SampleCatalog
{
    public static IReadOnlyList<SampleInfo> All { get; } =
    [
        new("Getting started", "Basic map",
            "A MapViewer with a base-map picker, navigation commands, a scale bar and a coordinate panel — the minimum every map app has.",
            "BasicMap", () => new BasicMapSample(), PackIconMaterialKind.MapOutline),

        new("Navigation", "Go to",
            "Move the map to a typed position with the Go To dialog, or host the same GoToView inline in your own layout.",
            "GoTo", () => new GoToSample(), PackIconMaterialKind.CrosshairsGps),

        new("Layers and legend", "Map legend",
            "MapLegendView as the table of contents: layer visibility, order, symbology and per-layer commands. Add an in-memory layer, a shapefile or a GeoJSON file.",
            "MapLegend", () => new MapLegendSample(), PackIconMaterialKind.LayersOutline),

        new("Layers and legend", "Attribute table",
            "FeatureTabControl shows one tab per selected layer with its features; zoom to a row, edit attributes, remove the tab.",
            "AttributeTable", () => new AttributeTableSample(), PackIconMaterialKind.TableLarge),

        new("Layers and legend", "Map markers",
            "SpecialPointLayer places WPF elements (location pins, labels, shapes, your own controls) at geographic positions.",
            "MapMarkers", () => new MapMarkersSample(), PackIconMaterialKind.MapMarkerMultipleOutline),

        new("Tools", "Drawing and drawing legend",
            "Draw points, polylines, polygons and text on the map; MapDrawingLegendView lists, styles, reorders and removes them.",
            "DrawingLegend", () => new DrawingLegendSample(), PackIconMaterialKind.VectorPolyline),

        new("Tools", "Measurement",
            "Measure lengths and areas interactively; SketchBarView offers finish/undo/cancel while a sketch is in progress.",
            "Measurement", () => new MeasurementSample(), PackIconMaterialKind.RulerSquare),

        new("Tools", "Identify",
            "Click a feature to inspect its attributes in the Identify results window.",
            "Identify", () => new IdentifySample(), PackIconMaterialKind.InformationOutline),

        new("Analysis", "Delaunay and Voronoi",
            "Scatter random points over Europe, then draw the Delaunay triangulation and the Voronoi diagram Maptor derives from them. Show either, or both.",
            "DelaunayVoronoi", () => new DelaunayVoronoiSample(), PackIconMaterialKind.VectorTriangle),

        new("Astronomy", "Day and night",
            "Shade the night side of the Earth, with civil, nautical and astronomical twilight, for any date and time. Follow the clock, or play time forward and watch the terminator sweep west.",
            "DayNight", () => new DayNightSample(), PackIconMaterialKind.WeatherNight),

        new("Astronomy", "Satellite positions",
            "Read TLEs or OMM JSON, propagate them with SGP4 and see where each satellite is right now, updated every few seconds. Choose one for its position, height, speed and how old its elements are.",
            "SatellitePosition", () => new SatellitePositionSample(), PackIconMaterialKind.SatelliteVariant),

        new("UI", "Theme and controls",
            "Every Maptor style token on one page, over a live accent + light/dark switch. Flip Dark and watch the status palette, pills and banners move; the over-map chrome deliberately does not.",
            "ThemeAndControls", () => new ThemeAndControlsSample(), PackIconMaterialKind.PaletteOutline),

        new("UI", "Localization and right-to-left",
            "Switch the UI language at run time; Maptor controls and your own localized bindings follow, including right-to-left flow.",
            "Localization", () => new LocalizationSample(), PackIconMaterialKind.Translate),
    ];
}
