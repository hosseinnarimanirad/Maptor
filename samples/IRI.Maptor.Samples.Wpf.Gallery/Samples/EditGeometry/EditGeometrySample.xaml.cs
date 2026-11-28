using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Data;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Core.TileServices;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Samples.Wpf.Gallery.Shell;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Samples.Wpf.Gallery.Samples.EditGeometry;

/// <summary>
/// Drawing and editing with everything that helps: snapping to layers and to other drawings, guides, the cursor box
/// with typed lengths and angles, the length labels, and the measure panel of the sketch bar. The layers are made
/// here (a block of parcels and a road), so the sample needs no data file.
/// </summary>
public partial class EditGeometrySample : UserControl
{
    // a small area by the Seine, west of Paris (WGS 84)
    private const double West = 2.20;
    private const double South = 48.84;

    private bool _initialized;

    public EditGeometrySample()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
            return; // Loaded fires again each time the gallery re-attaches this view

        _initialized = true;

        var presenter = new GalleryMapViewModel();

        presenter.InitializeSettings(
            ProxySettings.Default,
            BaseMapSettings.Default,
            new MapSettings { InitialExtent = BoundingBoxes.WebMercator_Europe },
            GeneralSettings.Default);

        // Connects the MapViewer to the view model and wires the default dialogs to the owner window.
        await MapInitializationHelper.InitializeMapAsync(map, Window.GetWindow(this)!, presenter);

        DataContext = presenter;

        presenter.SelectedMapProvider = TileMapProviderFactory.GoogleRoadMap;

        var parcels = ParcelsLayer();

        presenter.AddLayer(parcels);
        presenter.AddLayer(RoadLayer());

        // the block and the road, with a margin
        var southWest = ToWebMercator(West - 0.0008, South - 0.0010);
        var northEast = ToWebMercator(West + 0.0056, South + 0.0026);

        presenter.ZoomToExtent(new BoundingBox(southWest.X, southWest.Y, northEast.X, northEast.Y), isExactExtent: false, isNewExtent: true);
    }

    private static Point ToWebMercator(double longitude, double latitude)
        => MapProjects.GeodeticWgs84ToWebMercator(new Point(longitude, latitude));

    /// <summary>A block of 4 × 3 parcels of about 70 × 55 m with a street between the rows; rings are not closed.</summary>
    private static VectorLayer ParcelsLayer()
    {
        var features = new List<Feature<Point>>();

        const double width = 0.001;    // about 73 m of longitude here
        const double height = 0.0005;  // about 56 m of latitude

        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                var x = West + column * width;
                var y = South + row * (height + 0.0002);

                // a slanted side on every other parcel, so there are edges at more than one angle to snap and guide on
                var slant = column % 2 == 0 ? 0 : width * 0.15;

                var ring = new List<Point>
                {
                    ToWebMercator(x, y),
                    ToWebMercator(x + width, y),
                    ToWebMercator(x + width - slant, y + height),
                    ToWebMercator(x, y + height),
                };

                var polygon = Geometry<Point>.CreatePolygon(ring, SridHelper.WebMercator);

                features.Add(new Feature<Point>(polygon, new Dictionary<string, object> { ["Parcel"] = $"{row + 1}-{column + 1}" }));
            }
        }

        return new VectorLayer(
            layerName: "Parcels",
            dataSource: new MemoryDataSource(features),
            parameters: new VisualParameters(fill: Colors.Khaki, stroke: Colors.SaddleBrown, strokeThickness: 1, opacity: 0.6),
            type: LayerType.VectorLayer,
            renderMode: RenderMode.Default,
            rasterizationMethod: RasterizationMethod.DrawingVisual,
            visibleRange: ScaleInterval.All);
    }

    /// <summary>A road along the south side of the block that bends north past its east side: ends, vertices and edges to snap to.</summary>
    private static VectorLayer RoadLayer()
    {
        var points = new[] { (2.1994, 48.8396), (2.2016, 48.8394), (2.2041, 48.8395), (2.2046, 48.8404), (2.2047, 48.8423) }
            .Select(p => ToWebMercator(p.Item1, p.Item2))
            .ToList();

        var road = Geometry<Point>.Create(points, GeometryType.LineString, SridHelper.WebMercator);

        return new VectorLayer(
            layerName: "Road",
            dataSource: new MemoryDataSource(new List<Feature<Point>> { new(road, new Dictionary<string, object> { ["Name"] = "Road" }) }),
            parameters: new VisualParameters(fill: null, stroke: Brushes.DimGray, strokeThickness: 3, opacity: 0.9),
            type: LayerType.VectorLayer,
            renderMode: RenderMode.Default,
            rasterizationMethod: RasterizationMethod.DrawingVisual,
            visibleRange: ScaleInterval.All);
    }

    /// <summary>
    /// A drawing of 2,500 vertices to edit as a big geometry: a line that winds over about 4 km around the block,
    /// about 27 km long, with its vertices at equal distances of about 11 m: 14 px apart at zoom 17, more beyond. More than the 2,000
    /// vertices in view that get handles, so zoomed out the map asks to zoom in.
    /// </summary>
    private void OnAddLongLine(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MapViewModelBase presenter)
            return;

        const int count = 2_500;

        // west to east over about 4.4 km, swinging about 1.1 km north and south six times; the curve is traced finely
        // first and the vertices then put at equal distances along it, so none bunch up where the curve turns
        Point Curve(double t) => ToWebMercator(West - 0.028 + t * 0.06, South + 0.001 + 0.01 * Math.Sin(t * Math.PI * 12));

        const int fine = 100_000;

        var trace = new Point[fine + 1];
        var along = new double[fine + 1];

        for (int i = 0; i <= fine; i++)
        {
            trace[i] = Curve(i / (double)fine);

            if (i > 0)
                along[i] = along[i - 1] + Math.Sqrt(Math.Pow(trace[i].X - trace[i - 1].X, 2) + Math.Pow(trace[i].Y - trace[i - 1].Y, 2));
        }

        var points = new List<Point>(count);

        for (int i = 0, j = 0; i < count; i++)
        {
            var wanted = along[fine] * i / (count - 1);

            while (j < fine && along[j + 1] < wanted)
                j++;

            var span = j < fine ? along[j + 1] - along[j] : 0;
            var f = span > 0 ? (wanted - along[j]) / span : 0;
            var next = trace[Math.Min(j + 1, fine)];

            points.Add(new Point(trace[j].X + f * (next.X - trace[j].X), trace[j].Y + f * (next.Y - trace[j].Y)));
        }

        presenter.AddDrawingItem(Geometry<Point>.Create(points, GeometryType.LineString, SridHelper.WebMercator), "Long line (2,500 vertices)");
    }
}
