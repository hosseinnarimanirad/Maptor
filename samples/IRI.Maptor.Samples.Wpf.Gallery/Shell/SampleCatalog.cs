using System.Collections.Generic;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.AttributeTable;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.BasicMap;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.DelaunayVoronoi;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.DrawingLegend;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.GoTo;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.Identify;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.Localization;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.MapLegend;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.MapMarkers;
using IRI.Maptor.Samples.Wpf.Gallery.Samples.Measurement;
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
            "BasicMap", () => new BasicMapSample()),

        new("Navigation", "Go to",
            "Move the map to a typed position with the Go To dialog, or host the same GoToView inline in your own layout.",
            "GoTo", () => new GoToSample()),

        new("Layers and legend", "Map legend",
            "MapLegendView as the table of contents: layer visibility, order, symbology and per-layer commands. Add an in-memory layer, a shapefile or a GeoJSON file.",
            "MapLegend", () => new MapLegendSample()),

        new("Layers and legend", "Attribute table",
            "FeatureTabControl shows one tab per selected layer with its features; zoom to a row, edit attributes, remove the tab.",
            "AttributeTable", () => new AttributeTableSample()),

        new("Layers and legend", "Map markers",
            "SpecialPointLayer places WPF elements (location pins, labels, shapes, your own controls) at geographic positions.",
            "MapMarkers", () => new MapMarkersSample()),

        new("Tools", "Drawing and drawing legend",
            "Draw points, polylines, polygons and text on the map; MapDrawingLegendView lists, styles, reorders and removes them.",
            "DrawingLegend", () => new DrawingLegendSample()),

        new("Tools", "Measurement",
            "Measure lengths and areas interactively; SketchBarView offers finish/undo/cancel while a sketch is in progress.",
            "Measurement", () => new MeasurementSample()),

        new("Tools", "Identify",
            "Click a feature to inspect its attributes in the Identify results window.",
            "Identify", () => new IdentifySample()),

        new("Analysis", "Delaunay and Voronoi",
            "Scatter random points over Europe, then draw the Delaunay triangulation and the Voronoi diagram Maptor derives from them. Show either, or both.",
            "DelaunayVoronoi", () => new DelaunayVoronoiSample()),

        new("UI", "Theme and controls",
            "Every Maptor style token on one page, over a live accent + light/dark switch. Flip Dark and watch the status palette, pills and banners move; the over-map chrome deliberately does not.",
            "ThemeAndControls", () => new ThemeAndControlsSample()),

        new("UI", "Localization and right-to-left",
            "Switch the UI language at run time; Maptor controls and your own localized bindings follow, including right-to-left flow.",
            "Localization", () => new LocalizationSample()),
    ];
}
