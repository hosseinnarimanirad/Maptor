using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Maui.Localization;

using Microsoft.Maui.Hosting;

namespace IRI.Maptor.Presentation.Maui;

public static class MauiLibraryExtensions
{
    public static MauiAppBuilder UseIRIMaptor(this MauiAppBuilder builder)
    {
        // Make the MAUI UI strings available through the shared localization engine.
        // Registered managers are tried before the Jab.Core fallback table.
        LocalizationManager.Instance.RegisterResourceManager(MauiStrings.ResourceManager);

        // Register custom handlers and services here as the library grows.
        // Example:
        //   builder.ConfigureMauiHandlers(h => h.AddHandler<MapView, MapViewHandler>());

        return builder;
    }
}
