using System;
using System.Windows;

using IRI.Maptor.Jab.Common.ViewModels;
using IRI.Maptor.Jab.Common.ViewModels.Symbology;
using IRI.Maptor.Jab.Common.ViewModels.LayerSettings;
using IRI.Maptor.Jab.Common.Layers;
using IRI.Maptor.Jab.Common.Services;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Defaults;

public static class DefaultActions
{

    public static Action<IRI.Maptor.Sta.Common.Primitives.Point> GetDefaultGoToAction(Window ownerWindow, MapViewModelBase mapPresenter)
    {
        var result = new Action<IRI.Maptor.Sta.Common.Primitives.Point>((IRI.Maptor.Sta.Common.Primitives.Point webMercatorPoint) =>
        {
            var gotoPresenter = GoToViewModel.Create(mapPresenter);

            var gotoView = new IRI.Maptor.Jab.Controls.GoToMetroWindow(gotoPresenter);

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
        var view = new IRI.Maptor.Jab.Controls.Symbology.SymbologyView();

        var presenter = new SymbologyViewModel();
         
        presenter.Symbology = (layer as SymbolizableLayer)!.GetMainOrDefaultSymbology().Clone();
        
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

        //var gotoPresenter = IRI.Maptor.Jab.Common.Presenter.GoToPresenter.Create(mapPresenter);

        view.DataContext = presenter;
        view.Owner = ownerWindow;
        view.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        view.Show();

        //gotoPresenter.SelectDefaultMenu();            
    }

    public static async Task GetDefaultShowLayerSettingsView(IDialogService dialogService, Window ownerWindow, ILayer layer, MapViewModelBase viewModel)
    {
        var view = new IRI.Maptor.Jab.Controls.Dialogs.LayerSettingsDialogView();

        LayerSettings_VectorExportViewModel exportViewModel = new LayerSettings_VectorExportViewModel(viewModel, layer as VectorLayer,/*viewModel.DialogService,*/ null)
        {
            // in the case of webapi setting it to layer.DataSource.DataSourceKind is not good
            //SelectedDataSourceKind = /*layer.DataSource?.DataSourceKind ??*/ DataSourceKind.Shapefile
        };

        LayerSettingsViewModel layerSettingsViewModel = new LayerSettingsViewModel(layer, exportViewModel);

        await dialogService.ShowDialogAsync(ownerWindow, view, layerSettingsViewModel);


        //view.DataContext = layerSettingsViewModel;
        //view.Owner = ownerWindow;
        //view.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        //view.ShowDialog();
    }
}
