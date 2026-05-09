using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Jab.Controls;
using IRI.Maptor.Jab.Common.Layers;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Jab.Common.Defaults;
using IRI.Maptor.Jab.Common.Services;
using IRI.Maptor.Jab.Common.ViewModels;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Common.Models;

/// <summary>
/// Helper class for initializing MapViewer and MapPresenter with common configuration.
/// Reduces boilerplate code required when setting up map components in new projects.
/// </summary>
public static class MapInitializationHelper
{
    /// <summary>
    /// Initializes a MapViewer with a MapPresenter, handling all common setup tasks.
    /// This method encapsulates the standard initialization pattern used across projects.
    /// </summary>
    /// <typeparam name="T">The type of MapPresenter to initialize</typeparam>
    /// <param name="mapView">The MapViewer control to initialize</param>
    /// <param name="ownerWindow">The owner window for dialogs and actions</param>
    /// <param name="presenter">The presenter instance to initialize</param>
    /// <param name="config">Optional configuration for MapViewer settings. If null, uses default configuration.</param>
    /// <returns>The initialized presenter instance</returns>
    public static async Task<T> InitializeMapAsync<T>(
        MapViewer mapView,
        System.Windows.Window ownerWindow,
        T presenter,
        //IProxySettings? proxySettings,
        //IBaseMapSettings? baseMapSettings,
        //IMapSettings? mapSettings,
        //IGeneralSettings? generalSettings,
        List<IriProvince93>? provinces = null
        /*MapViewerConfiguration? config = null*/) where T : MapViewModelBase
    {
        if (mapView == null)
            throw new ArgumentNullException(nameof(mapView));

        if (ownerWindow == null)
            throw new ArgumentNullException(nameof(ownerWindow));

        if (presenter == null)
            throw new ArgumentNullException(nameof(presenter));

        // Register presenter with MapViewer (this sets up all the Request* delegates)
        await mapView.Register(presenter, provinces);

        // Create default services and actions
        var (dialogService, requestShowGoToView, requestShowSymbologyView, requestShowLayerSettingsView) = CreateDefaultServices(ownerWindow, presenter);

        // Initialize presenter with default services
        presenter.Initialize(dialogService, requestShowGoToView, requestShowSymbologyView, requestShowLayerSettingsView);


        // Configure MapViewer with common settings
        ConfigureMapViewer(presenter, mapView/*, mapSettings*/);

        //// Configure presenter settings
        //ConfigurePresenterSettings(presenter, config);

        ownerWindow.DataContext = presenter;


        // initiliaze the map
        // setting initial extent, zoom options, ...

        return presenter;
    }

    /// <summary>
    /// Creates default dialog service and action delegates for the presenter.
    /// </summary>
    private static (IDialogService dialogService,
                        Action<Point> requestShowGoToView,
                        Action<ILayer> requestShowSymbologyView,
                        Action<ILayer> requestShowLayerSettingsView)
            CreateDefaultServices(System.Windows.Window ownerWindow, MapViewModelBase presenter)
    {
        var dialogService = new DefaultDialogService(ownerWindow);

        var requestShowGoToView = DefaultActions.GetDefaultGoToAction(ownerWindow, presenter);

        Action<ILayer> requestShowSymbologyView = layer => DefaultActions.GetDefaultShowSymbologyView(ownerWindow, layer, presenter);

        Action<ILayer> requestShowLayerSettingsView = async layer => await DefaultActions.GetDefaultShowLayerSettingsView(dialogService, ownerWindow, layer, presenter);

        return (dialogService, requestShowGoToView, requestShowSymbologyView, requestShowLayerSettingsView);
    }

    /// <summary>
    /// Configures MapViewer with common settings based on the configuration.
    /// </summary>
    private static void ConfigureMapViewer<T>(T presenter, MapViewer mapView/*, IMapSettings config*/) where T : MapViewModelBase
    {
        //if (config.EnablePan)
        //{
        //    mapView.Pan();
        //}

        if (presenter.MapSettings.IsMouseWheelZoomEnabled)
        {
            mapView.EnableZoomingOnMouseWheel();
        }

        //if (config.IsGoogleZoomLevelsEnabled)
        //{
        //    mapView.IsGoogleZoomLevelsEnabled = true;
        //}

        //if (config.InitialCursor != null)
        //{
        //    mapView.SetCursor(config.InitialCursor);
        //}

        if (presenter.MapSettings.InitialExtent != null)
        {
            presenter.ZoomToExtent(presenter.MapSettings.InitialExtent ?? BoundingBoxes.Mercator_Iran, true, isNewExtent: true);
        }

    }

}

