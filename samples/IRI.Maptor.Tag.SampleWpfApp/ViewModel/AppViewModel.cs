using System.Collections.Generic;

using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.ViewModels;
using IRI.Maptor.Jab.Common.Views.MapMarkers;
using IRI.Maptor.Jab.Common.Assets.Commands;

namespace IRI.Maptor.Tag.SampleWpfApp.ViewModel;

public class AppViewModel : MapViewModel
{
    public void ShowSpecialPointLayerSample()
    {
        var markers = new List<Locateable>()
        {
            new Locateable(new Point(48, 30), AncherFunctionHandlers.BottomCenter) { Element = new LocationMarker("A") },
            new Locateable(new Point(49, 31), AncherFunctionHandlers.BottomCenter) { Element = new LocationMarker("B") },
            new Locateable(new Point(50, 32), AncherFunctionHandlers.CenterCenter) { Element = new PointMarker("C") },
            new Locateable(new Point(51, 33), AncherFunctionHandlers.CenterCenter) { Element = new RectangeMarker() },
            new Locateable(new Point(52, 34), AncherFunctionHandlers.BottomCenter) { Element = new LabelMarker("E", true) }
        };

        var pointLayer =
            new SpecialPointLayer(
                name: "poi",
                items: markers,
                opacity: 0.8,
                visibleRange: ScaleInterval.All,
                type: LayerType.Complex);

        this.AddLayer(pointLayer);

        this.ZoomToExtent(pointLayer.Extent, isExactExtent: false, isNewExtent: true);
    }
      
    
    private RelayCommand? _showSpecialPointLayerSampleCommand;

    public RelayCommand ShowSpecialPointLayerSampleCommand
    {
        get
        {
            if (_showSpecialPointLayerSampleCommand == null)
            {
                _showSpecialPointLayerSampleCommand = new RelayCommand(param =>
                {
                    ShowSpecialPointLayerSample();
                });
            }

            return _showSpecialPointLayerSampleCommand;
        }
    }
     
}
