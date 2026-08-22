using System.Reflection;
using System.Resources;

namespace IRI.Maptor.Presentation.Maui.Localization;

/// <summary>
/// Accessor for the MAUI UI string table (<c>MauiStrings.resx</c> + <c>MauiStrings.fa-IR.resx</c>).
/// Exposes the <see cref="ResourceManager"/> so it can be registered with the shared
/// <see cref="IRI.Maptor.Presentation.Core.Localization.LocalizationManager"/>. Strings are looked up
/// through that manager's indexer, so there are no strongly-typed properties here.
/// </summary>
public static class MauiStrings
{
    private static ResourceManager? _resourceManager;

    /// <summary>The <see cref="System.Resources.ResourceManager"/> backing the MAUI string table.</summary>
    public static ResourceManager ResourceManager =>
        _resourceManager ??= new ResourceManager(
            "IRI.Maptor.Presentation.Maui.Localization.MauiStrings",
            typeof(MauiStrings).GetTypeInfo().Assembly);
}
