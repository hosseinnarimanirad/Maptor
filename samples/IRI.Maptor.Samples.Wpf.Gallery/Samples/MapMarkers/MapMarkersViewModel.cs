using System.Collections.Generic;
using IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Core.Common.Primitives;
using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Samples.Wpf.Gallery.Samples.MapMarkers;

/// <summary>A map view model with one extra command — the usual way to extend MapViewModelBase.</summary>
public class MapMarkersViewModel : MapViewModelBase
{
    private RelayCommand? _addMarkersCommand;

    public RelayCommand AddMarkersCommand => _addMarkersCommand ??= new RelayCommand(_ => AddMarkers());

    public void AddMarkers()
    {
        // Locateable = a WGS 84 position + an anchor rule (which point of the element sits on the position)
        // + any WPF element. The built-in markers live in IRI.Maptor.Presentation.Wpf.Controls.MapMarkers.
        var markers = new List<Locateable>
        {
            new(new Point(-0.128, 51.507), AnchorFunctionHandlers.BottomCenter) { Element = new LocationMarker("L") },
            new(new Point(2.352, 48.857), AnchorFunctionHandlers.BottomCenter) { Element = new LocationMarker("P") },
            new(new Point(13.405, 52.520), AnchorFunctionHandlers.CenterCenter) { Element = new PointMarker("B") },
            new(new Point(12.496, 41.903), AnchorFunctionHandlers.CenterCenter) { Element = new RectangleMarker() },
            new(new Point(-3.704, 40.417), AnchorFunctionHandlers.BottomCenter) { Element = new LabelMarker("Madrid", true) },
        };

        var layer = new SpecialPointLayer("Cities", markers, opacity: 0.9, visibleRange: ScaleInterval.All, type: LayerType.Complex);

        AddLayer(layer);

        ZoomToExtent(layer.Extent, isExactExtent: false, isNewExtent: true);
    }
}
