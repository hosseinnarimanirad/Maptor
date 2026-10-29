using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.Cartography.Legend;

namespace IRI.Maptor.Presentation.Wpf.Models.LayerSettings;

/// <summary>
/// One named style of a layer's SLD as the style lists show it (admin window, Symbology tab, legend
/// switcher): its title, whether it is the default or the one drawn, its rule count and a few swatches.
/// </summary>
public class LayerStyleItem : Notifier
{
    private const int MaxSwatches = 5;

    private readonly UserStyle? _style;

    private LayerStyleItem(SldStyleInfo info, UserStyle? style, bool isActive)
    {
        _style = style;
        _title = info.Title;
        _isActive = isActive;

        Index = info.Index;
        Name = info.Name;
        IsDefault = info.IsDefault;
        RuleCount = style?.FeatureTypeStyles?.Sum(f => f.Rules?.Count ?? 0) ?? 0;
    }

    public int Index { get; }

    /// <summary>The style's permanent identity (never shown); null for a legacy unnamed style.</summary>
    public string? Name { get; }

    public bool IsDefault { get; }

    public int RuleCount { get; }

    public string RuleCountText => string.Format(Localize("layerStyles_ruleCount"), RuleCount);

    private string? _title;

    /// <summary>The title the admin types; edits are reported through <see cref="TitleEdited"/>.</summary>
    public string? Title
    {
        get => _title;
        set
        {
            if (string.Equals(_title, value, StringComparison.Ordinal))
                return;

            _title = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(DisplayTitle));

            TitleEdited?.Invoke(this, value);
        }
    }

    /// <summary>The title, else the name, else "Untitled style".</summary>
    public string DisplayTitle => !string.IsNullOrWhiteSpace(Title) ? Title! : Name ?? Localize("layerStyles_untitled");

    private bool _isActive;

    /// <summary>The style the layer is drawn with.</summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value)
                return;

            _isActive = value;
            RaisePropertyChanged();
        }
    }

    private bool _isUserChanged;

    /// <summary>The user changed this style on this computer (it differs from the server's).</summary>
    public bool IsUserChanged
    {
        get => _isUserChanged;
        set
        {
            if (_isUserChanged == value)
                return;

            _isUserChanged = value;
            RaisePropertyChanged();
        }
    }

    internal Action<LayerStyleItem, string?>? TitleEdited { get; set; }

    private IReadOnlyList<ImageSource>? _swatches;

    /// <summary>A swatch for each of the first rules; drawn only when a view asks for them.</summary>
    public IReadOnlyList<ImageSource> Swatches => _swatches ??= CreateSwatches();

    private IReadOnlyList<ImageSource> CreateSwatches()
    {
        var result = new List<ImageSource>();

        var rules = _style?.FeatureTypeStyles?.SelectMany(f => f.Rules ?? new List<Rule>()).Take(MaxSwatches) ?? Enumerable.Empty<Rule>();

        var options = new SldLegendOptions { SwatchWidth = 22, SwatchHeight = 16 };

        foreach (var rule in rules)
        {
            try
            {
                using var bitmap = LegendSwatchFactory.CreateSwatchBitmap(rule, options);

                if (SldLegendBuilder.ToFrozenBitmapImage(bitmap) is { } image)
                    result.Add(image);
            }
            catch
            {
                // a rule the swatch factory cannot draw just has no swatch
            }
        }

        return result;
    }

    /// <summary>The styles of <paramref name="sld"/>, the one named <paramref name="activeStyleName"/> marked active.</summary>
    public static List<LayerStyleItem> FromSld(StyledLayerDescriptor? sld, string? activeStyleName)
    {
        var styles = sld.GetStyles();

        // a layer drawn with its default style may not know the name (legacy unnamed style)
        var activeIndex = styles.FirstOrDefault(s => s.Name is not null && s.Name == activeStyleName)?.Index
            ?? styles.FirstOrDefault(s => s.IsDefault)?.Index;

        return styles.Select(info => new LayerStyleItem(info, sld.FindStyleAt(info.Index), info.Index == activeIndex)).ToList();
    }

    private static string Localize(string key) => LocalizationManager.Instance[key];
}
