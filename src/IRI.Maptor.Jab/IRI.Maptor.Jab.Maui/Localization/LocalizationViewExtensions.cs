using IRI.Maptor.Jab.Core.Localization;

using Microsoft.Maui.Controls;

namespace IRI.Maptor.Jab.Maui.Localization;

/// <summary>
/// Helpers for localizing controls that are built in C# (e.g. the slide-in sidebars), mirroring
/// the XAML <see cref="TranslateExtension"/>. Each binds a property to the shared
/// <see cref="LocalizationManager"/> indexer, so text re-translates when the language changes.
/// </summary>
public static class LocalizationViewExtensions
{
    /// <summary>A one-way binding to the localized string for <paramref name="key"/>.</summary>
    public static BindingBase Loc(string key)
        => new Binding($"[{key}]", BindingMode.OneWay, source: LocalizationManager.Instance);

    /// <summary>Binds <paramref name="property"/> on <paramref name="view"/> to the localized string for <paramref name="key"/>.</summary>
    public static void SetLoc(this BindableObject view, BindableProperty property, string key)
        => view.SetBinding(property, Loc(key));
}
