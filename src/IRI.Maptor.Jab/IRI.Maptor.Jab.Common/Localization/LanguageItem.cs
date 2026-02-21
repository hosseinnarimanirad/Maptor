using System;
using System.Globalization;
using System.Windows;

namespace IRI.Maptor.Jab.Common.Localization;

public class LanguageItem
{
    public CultureInfo Culture { get; }
    public string EnglishName => Culture.EnglishName;
    public string NativeName => _nativeNameOverride ?? Culture.NativeName;
    /// <summary>
    /// When false, the language cannot be selected. Disabled items are shown grayed out in the UI.
    /// </summary>
    public bool IsEnabled { get; }

    private readonly string? _nativeNameOverride;
    private readonly string _flagKey;

    /// <summary>
    /// Pack URI for the country flag image. Uses region code (e.g. us, ir, sa) when available.
    /// </summary>
    public Uri FlagUri => new Uri(
         $"pack://application:,,,/IRI.Maptor.Jab.Common;component/Assets/Images/Flags/{_flagKey}.png",
         UriKind.Absolute);

    public FlowDirection TextFlowDirection =>
        Culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    /// <param name="culture">The culture for the language.</param>
    /// <param name="isEnabled">Whether the language can be selected.</param>
    /// <param name="nativeNameOverride">Optional override for display. Use when Culture.NativeName uses wrong script (e.g. Kurdish in Latin instead of Sorani).</param>
    /// <param name="flagKey">Optional ISO 3166-1 alpha-2 country code for the flag (e.g. "us", "ir"). Defaults to region from culture name.</param>
    public LanguageItem(CultureInfo culture, bool isEnabled = true, string? nativeNameOverride = null, string? flagKey = null)
    {
        Culture = culture;
        IsEnabled = isEnabled;
        _nativeNameOverride = nativeNameOverride;
        _flagKey = flagKey ?? GetFlagKeyFromCulture(culture);
    }

    private static string GetFlagKeyFromCulture(CultureInfo culture)
    {
        var parts = culture.Name.Split('-');
        return parts.Length > 1 ? parts[^1].ToLowerInvariant() : culture.TwoLetterISOLanguageName.ToLowerInvariant();
    }
} 