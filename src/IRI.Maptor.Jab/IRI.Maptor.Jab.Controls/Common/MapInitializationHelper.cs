using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Data;
using IRI.Maptor.Jab.Common.Models.Settings;
using IRI.Maptor.Jab.Common.ViewModels;
using IRI.Maptor.Jab.Controls.Common.Defaults;
using IRI.Maptor.Jab.Controls.Services.Dialog;
using IRI.Maptor.Jab.Controls.Views;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Controls.Common;

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
        IMapSettings? mapSettings,
        IBaseMapSettings baseMapSettings,
        List<IrProvince93>? provinces = null
        /*MapViewerConfiguration? config = null*/) where T : MapViewModelBase
    {
        if (mapView == null)
            throw new ArgumentNullException(nameof(mapView));

        if (ownerWindow == null)
            throw new ArgumentNullException(nameof(ownerWindow));

        if (presenter == null)
            throw new ArgumentNullException(nameof(presenter));

        // Use default configuration if none provided
        //config ??= MapViewerConfiguration.Default;
        mapSettings ??= MapSettings.Default;

        // Register presenter with MapViewer (this sets up all the Request* delegates)
        await mapView.Register(presenter, mapSettings.InitialExtent, provinces);

        // Create default services and actions
        var (dialogService, requestShowGoToView, requestShowSymbologyView) = CreateDefaultServices(ownerWindow, presenter);

        // Initialize presenter with default services
        presenter.Initialize(dialogService, mapSettings, baseMapSettings, requestShowGoToView, requestShowSymbologyView);



        // Configure MapViewer with common settings
        ConfigureMapViewer(mapView, mapSettings);

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
    private static (IDialogService dialogService, Action<Point> requestShowGoToView, Action<ILayer> requestShowSymbologyView)
        CreateDefaultServices(System.Windows.Window ownerWindow, MapViewModelBase presenter)
    {
        var dialogService = new DefaultDialogService(ownerWindow);
        var requestShowGoToView = DefaultActions.GetDefaultGoToAction(ownerWindow, presenter);
        Action<ILayer> requestShowSymbologyView = layer => DefaultActions.GetDefaultShowSymbologyView(ownerWindow, layer, presenter);

        return (dialogService, requestShowGoToView, requestShowSymbologyView);
    }

    /// <summary>
    /// Configures MapViewer with common settings based on the configuration.
    /// </summary>
    private static void ConfigureMapViewer(MapViewer mapView, IMapSettings config)
    {
        //if (config.EnablePan)
        //{
        //    mapView.Pan();
        //}

        if (config.IsMouseWheelZoomEnabled)
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

        if (config.InitialExtent != null)
        {
            mapView.ZoomToExtent(config.InitialExtent.Value);
        }

    }

}

