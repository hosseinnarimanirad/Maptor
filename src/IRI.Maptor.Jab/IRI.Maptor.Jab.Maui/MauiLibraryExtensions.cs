using Microsoft.Maui.Hosting;

namespace IRI.Maptor.Jab.Maui;

public static class MauiLibraryExtensions
{
    public static MauiAppBuilder UseIRIMaptor(this MauiAppBuilder builder)
    {
        // Register custom handlers and services here as the library grows.
        // Example:
        //   builder.ConfigureMauiHandlers(h => h.AddHandler<MapView, MapViewHandler>());

        return builder;
    }
}
