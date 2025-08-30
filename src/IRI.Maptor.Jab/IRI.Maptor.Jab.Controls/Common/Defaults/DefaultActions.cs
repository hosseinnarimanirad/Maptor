using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Presenters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IRI.Maptor.Jab.Controls.Common.Defaults;

public static class DefaultActions
{

    public static Action<IRI.Maptor.Sta.Common.Primitives.Point> GetDefaultGoToAction(Window ownerWindow, MapPresenter mapPresenter)
    {
        var result = new Action<IRI.Maptor.Sta.Common.Primitives.Point>((IRI.Maptor.Sta.Common.Primitives.Point webMercatorPoint) =>
        {
            var gotoPresenter = IRI.Maptor.Jab.Controls.Presenter.GoToPresenter.Create(mapPresenter);

            var gotoView = new IRI.Maptor.Jab.Controls.View.GoToMetroWindow(gotoPresenter);

            //gotoView.DataContext = gotoPresenter;
            gotoView.Owner = ownerWindow;
            gotoView.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            gotoView.Show();

            gotoPresenter.SelectDefaultMenu();
            gotoPresenter.SetWebMercatorPoint(webMercatorPoint);
        });

        return result;
    }


    public static void GetDefaultShowSymbologyView(Window ownerWindow, ILayer layer, MapPresenter mapPresenter)
    {
        var view = new IRI.Maptor.Jab.Controls.View.Symbology.SymbologyView();

        var presenter = new SymbologyPresenter();

        //if (layer is DrawingItemLayer)
        //{ 
        //    presenter.Symbology = (layer as DrawingItemLayer).OriginalSymbology.Clone();
        //}
        //else
        //{ 
        //presenter.Symbology = layer.VisualParameters.Clone();
        presenter.Symbology = (layer as SymbolizableLayer)!.GetMainOrDefaultSymbology().Clone();
        //}

        presenter.RequestCloseAction = view.Close;

        presenter.RequestApplyAction = p =>
        {
            var param = (layer as SymbolizableLayer)!.GetMainOrDefaultSymbology();

            param.Fill = p.Symbology.Fill;
            param.Stroke = p.Symbology.Stroke;
            param.StrokeThickness = p.Symbology.StrokeThickness;

            if (layer is DrawingItemLayer drawingItemLayer)
            {
                //update symbology
                if (layer.IsSelectedInToc)
                    drawingItemLayer.RequestHighlightGeometry?.Invoke(drawingItemLayer);
            }

            view.Close();

            //in order to update the symbology for the layer on the map after dialog was closed
            mapPresenter.ClearLayer(layer, true, true);

            mapPresenter.AddLayer(layer);
        };

        //var gotoPresenter = IRI.Maptor.Jab.Controls.Presenter.GoToPresenter.Create(mapPresenter);

        view.DataContext = presenter;
        view.Owner = ownerWindow;
        view.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        view.Show();

        //gotoPresenter.SelectDefaultMenu();            
    }

}
