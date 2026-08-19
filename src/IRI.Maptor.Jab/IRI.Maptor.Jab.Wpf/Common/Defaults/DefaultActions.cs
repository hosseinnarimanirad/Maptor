using System;
using System.Linq;
using System.Windows;

using IRI.Maptor.Extensions;

using IRI.Maptor.Jab.Wpf.ViewModels;
using IRI.Maptor.Jab.Wpf.ViewModels.Symbology;
using IRI.Maptor.Jab.Wpf.ViewModels.LayerSettings;
using IRI.Maptor.Jab.Wpf.Layers;
using IRI.Maptor.Jab.Wpf.Services;
using System.Threading.Tasks;
using IRI.Maptor.Jab.Core.Layers;

namespace IRI.Maptor.Jab.Wpf.Defaults;

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
        if (layer is not SymbolizableLayer symbolizable)
            return;

        var view = new IRI.Maptor.Jab.Controls.Symbology.SymbologyView();

        var presenter = new SymbologyViewModel();

        presenter.Symbology = symbolizable.GetMainOrDefaultSymbology().Clone();

        presenter.RequestCloseAction = view.Close;

        presenter.ShowAdvancedOption = true;

        presenter.RequestShowAdvancedAction = () =>
        {
            view.Close();

            ShowSldEditorView(ownerWindow, layer, viewModel);
        };

        presenter.ShowResetOption = symbolizable.CanResetSymbology;

        presenter.RequestResetAction = () =>
        {
            if (!symbolizable.ResetSymbologyToDefault())
                return;

            // re-seed the dialog fields from the restored symbology
            presenter.Symbology = symbolizable.GetMainOrDefaultSymbology().Clone();

            viewModel.Refresh(false);
        };

        presenter.RequestApplyAction = p =>
        {
            var param = symbolizable.GetMainOrDefaultSymbology();

            param.Fill = p.Symbology.Fill;
            param.Stroke = p.Symbology.Stroke;
            param.StrokeThickness = p.Symbology.StrokeThickness;

            symbolizable.IsSymbologyUserModified = true;

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

        view.DataContext = presenter;
        view.Owner = ownerWindow;
        view.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        view.Show();
    }

    /// <summary>
    /// Opens the full SLD editor for a layer, seeded with its current symbology
    /// (the pristine <see cref="SymbolizableLayer.SourceSld"/> when the layer was styled
    /// from an SLD, otherwise the <see cref="SymbolizableLayer.GetSld"/> reconstruction).
    /// Apply/OK pushes the edited SLD back onto the layer and repaints the map.
    /// </summary>
    public static void ShowSldEditorView(Window ownerWindow, ILayer layer, MapViewModelBase viewModel)
    {
        if (layer is not SymbolizableLayer symbolizable)
            return;

        var fieldNames = symbolizable.GetFields()?
            .Select(f => f.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        var presenter = SldEditorViewModel.Create(
            layer.LayerName,
            symbolizable.SourceSld ?? symbolizable.GetSld(),
            fieldNames,
            symbolizable.SpatialModelMode);

        var view = new IRI.Maptor.Jab.Controls.Symbology.Sld.SldEditorWindow(presenter)
        {
            Owner = ownerWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        presenter.RequestApplyAction = p =>
        {
            var sld = p.ToStyledLayerDescriptor();

            symbolizable.ReplaceSymbolizers(sld.ParseToSymbolizers(), sld);

            symbolizable.IsSymbologyUserModified = true;

            if (layer is DrawingItemLayer drawingItemLayer && layer.IsSelectedInToc)
                drawingItemLayer.RequestHighlightGeometry?.Invoke(drawingItemLayer);

            // same repaint path the project-file override replay uses after ReplaceSymbolizers
            viewModel.Refresh(false);
        };

        presenter.ShowResetOption = symbolizable.CanResetSymbology;

        presenter.RequestResetToDefaultAction = () =>
        {
            if (!symbolizable.ResetSymbologyToDefault())
                return;

            // re-seed the editor from the restored symbology
            presenter.FromStyledLayerDescriptor(symbolizable.SourceSld ?? symbolizable.GetSld());
            presenter.RefreshPreview();
            presenter.SelectedRule = presenter.Rules.FirstOrDefault();

            viewModel.Refresh(false);
        };

        view.Show();
    }

    public static async Task GetDefaultShowLayerSettingsView(IDialogService dialogService, Window ownerWindow, ILayer layer, MapViewModelBase viewModel)
    {
        var view = new IRI.Maptor.Jab.Controls.Dialogs.LayerSettingsDialogView();

        LayerSettings_VectorExportViewModel exportViewModel = new LayerSettings_VectorExportViewModel(viewModel, layer as VectorLayer)
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
