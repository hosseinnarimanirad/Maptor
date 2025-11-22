using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Presenters;
using IRI.Maptor.Jab.Controls.Common.Defaults;
using IRI.Maptor.Jab.Controls.Services.Dialog;
using IRI.Maptor.Jab.Controls.Views;
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
        Window ownerWindow,
        T presenter,
        MapViewerConfiguration? config = null) where T : MapPresenter
    {
        if (mapView == null)
            throw new ArgumentNullException(nameof(mapView));
        if (ownerWindow == null)
            throw new ArgumentNullException(nameof(ownerWindow));
        if (presenter == null)
            throw new ArgumentNullException(nameof(presenter));

        // Use default configuration if none provided
        config ??= MapViewerConfiguration.Default;

        // Register presenter with MapViewer (this sets up all the Request* delegates)
        await mapView.Register(presenter);

        // Create default services and actions
        var (dialogService, requestShowGoToView, requestShowSymbologyView) = CreateDefaultServices(ownerWindow, presenter);

        // Initialize presenter with default services
        presenter.Initialize(dialogService, requestShowGoToView, requestShowSymbologyView);

        // Configure MapViewer with common settings
        ConfigureMapViewer(mapView, config);

        // Configure presenter settings
        ConfigurePresenterSettings(presenter, config);

        return presenter;
    }

    /// <summary>
    /// Creates default dialog service and action delegates for the presenter.
    /// </summary>
    private static (IDialogService dialogService, Action<IRI.Maptor.Sta.Common.Primitives.Point> requestShowGoToView, Action<ILayer> requestShowSymbologyView) CreateDefaultServices(
        Window ownerWindow,
        MapPresenter presenter)
    {
        var dialogService = new DefaultDialogService(ownerWindow);
        var requestShowGoToView = DefaultActions.GetDefaultGoToAction(ownerWindow, presenter);
        Action<ILayer> requestShowSymbologyView = layer => DefaultActions.GetDefaultShowSymbologyView(ownerWindow, layer, presenter);

        return (dialogService, requestShowGoToView, requestShowSymbologyView);
    }

    /// <summary>
    /// Configures MapViewer with common settings based on the configuration.
    /// </summary>
    private static void ConfigureMapViewer(MapViewer mapView, MapViewerConfiguration config)
    {
        if (config.EnablePan)
        {
            mapView.Pan();
        }

        if (config.EnableMouseWheelZoom)
        {
            mapView.EnableZoomingOnMouseWheel();
        }

        if (config.EnableGoogleZoomLevels)
        {
            mapView.IsGoogleZoomLevelsEnabled = true;
        }

        if (config.InitialCursor != null)
        {
            mapView.SetCursor(config.InitialCursor);
        }

        if (config.InitialExtent != null)
        {
            mapView.ZoomToExtent(config.InitialExtent.Value);
        }
    }

    /// <summary>
    /// Configures presenter settings based on the configuration.
    /// </summary>
    private static void ConfigurePresenterSettings(MapPresenter presenter, MapViewerConfiguration config)
    {
        if (config.MinGoogleZoomLevel > 0)
        {
            presenter.MapSettings.MinGoogleZoomLevel = config.MinGoogleZoomLevel;
        }

        if (config.MaxGoogleZoomLevel > 0)
        {
            presenter.MapSettings.MaxGoogleZoomLevel = config.MaxGoogleZoomLevel;
        }
    }
}

