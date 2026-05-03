using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.ViewModels;
using IRI.Maptor.Jab.Common.ViewModels;
using IRI.Maptor.Jab.Common.ViewModels.LayerSettings;
using IRI.Maptor.Jab.Common.ViewModels.Symbology;
using IRI.Maptor.Sta.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IRI.Maptor.Jab.Controls.Common.Defaults;

public static class DefaultActions
{

    public static Action<IRI.Maptor.Sta.Common.Primitives.Point> GetDefaultGoToAction(Window ownerWindow, MapViewModelBase mapPresenter)
    {
        var result = new Action<IRI.Maptor.Sta.Common.Primitives.Point>((IRI.Maptor.Sta.Common.Primitives.Point webMercatorPoint) =>
        {
            var gotoPresenter = GoToViewModel.Create(mapPresenter);

            var gotoView = new IRI.Maptor.Jab.Controls.Views.GoToMetroWindow(gotoPresenter);

            //gotoView.DataContext = gotoPresenter;
            gotoView.Owner = ownerWindow;
            gotoView.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            gotoView.Show();

            gotoPresenter.SelectDefaultMenu();
            gotoPresenter.SetWebMercatorPoint(webMercatorPoint);
        });

        return result;
    }


    public static void GetDefaultShowSymbologyView(Window ownerWindow, ILayer layer, MapViewModelBase viewModel)
    {
        var view = new IRI.Maptor.Jab.Controls.Views.Symbology.SymbologyView();

        var presenter = new SymbologyViewModel();

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
            viewModel.ClearLayer(layer, remove: true, forceRemove: true, keepEmptyParentGroup: true);

            viewModel.AddLayer(layer);
        };

        //var gotoPresenter = IRI.Maptor.Jab.Controls.Presenter.GoToPresenter.Create(mapPresenter);

        view.DataContext = presenter;
        view.Owner = ownerWindow;
        view.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        view.Show();

        //gotoPresenter.SelectDefaultMenu();            
    }

    public static void GetDefaultShowLayerSettingsView(Window ownerWindow, ILayer layer, MapViewModelBase viewModel)
    {
        var view = new IRI.Maptor.Jab.Controls.Views.Dialogs.LayerSettingsDialogView();

        LayerSettings_VectorExportViewModel exportViewModel = new LayerSettings_VectorExportViewModel(viewModel, layer as VectorLayer,/*viewModel.DialogService,*/ null)
        {
            SelectedDataSourceKind = layer.DataSource?.DataSourceKind ?? DataSourceKind.Shapefile
        };

        LayerSettingsViewModel layerSettingsViewModel = new LayerSettingsViewModel(layer, exportViewModel);


        view.DataContext = layerSettingsViewModel;
        view.Owner = ownerWindow;
        view.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        view.Show();
    }
}
