using IRI.Maptor.Jab.Core.Localization;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace IRI.Maptor.Jab.Maui.Localization;

/// <summary>
/// XAML markup extension that binds a property to a localized string by key, e.g.
/// <c>Text="{loc:Translate layersPanel_title}"</c>. The binding targets the shared
/// <see cref="LocalizationManager"/> indexer, so values refresh automatically when the
/// language changes.
/// </summary>
[ContentProperty(nameof(Key))]
public sealed class TranslateExtension : IMarkupExtension<BindingBase>
{
    /// <summary>The resource key to look up.</summary>
    public string Key { get; set; } = string.Empty;

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
        => new Binding($"[{Key}]", BindingMode.OneWay, source: LocalizationManager.Instance);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        => ProvideValue(serviceProvider);
}
