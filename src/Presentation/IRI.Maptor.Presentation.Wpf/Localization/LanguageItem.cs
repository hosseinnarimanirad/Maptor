using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using System;
using System.Globalization;
using System.Windows;

namespace IRI.Maptor.Presentation.Wpf.Localization;

public class LanguageItem : Notifier
{
    //public CultureInfo Culture { get; }
    public string Name { get; init; }
    public string EnglishName { get; init; } //=> Culture.EnglishName;
    public string NativeName { get; init; }// => /*_nativeNameOverride ??*/ Culture.NativeName;

    /// <summary>The language in its own script, without the region: «فارسی», «Français», «English».</summary>
    public string LanguageName { get; init; }

    /// <summary>The language's English name, without the region: "Persian". Empty for English itself.</summary>
    public string EnglishLanguageName { get; init; }
    /// <summary>
    /// When false, the language cannot be selected. Disabled items are shown grayed out in the UI.
    /// </summary>
    private bool _isEnabled;
    public bool IsEnabled
    {
        get { return _isEnabled; }
        set
        {
            _isEnabled = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>Whether this is the language the application is in (the selector highlights it).</summary>
    private bool _isCurrent;
    public bool IsCurrent
    {
        get { return _isCurrent; }
        set
        {
            if (_isCurrent == value)
                return;

            _isCurrent = value;
            RaisePropertyChanged();
        }
    }

    public LanguageType LanguageType { get; }

    //private readonly string? _nativeNameOverride;

    private readonly string _flagKey;

    /// <summary>
    /// Pack URI for the country flag image. Uses region code (e.g. us, ir, sa) when available.
    /// </summary>
    public Uri FlagUri => new Uri($"pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/Images/Flags/{_flagKey}.png", UriKind.Absolute);

    public FlowDirection TextFlowDirection { get; init; }//=>Culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    /// <param name="culture">The culture for the language.</param>
    /// <param name="isEnabled">Whether the language can be selected.</param>
    /// <param name="nativeNameOverride">Optional override for display. Use when Culture.NativeName uses wrong script (e.g. Kurdish in Latin instead of Sorani).</param>
    /// <param name="flagKey">Optional ISO 3166-1 alpha-2 country code for the flag (e.g. "us", "ir"). Defaults to region from culture name.</param>
    private LanguageItem(CultureInfo culture, LanguageType language, /*string? nativeNameOverride = null, */string? flagKey = null)
    {
        //Culture = culture;
        Name = culture.Name;
        EnglishName = culture.EnglishName;
        NativeName = culture.NativeName;

        // the neutral culture names the language alone: fa-IR → fa «فارسی», az-Latn-AZ → az-Latn «azərbaycan»
        var neutral = culture.IsNeutralCulture || string.IsNullOrEmpty(culture.Parent.Name) ? culture : culture.Parent;
        LanguageName = Capitalize(neutral.NativeName, culture);
        EnglishLanguageName = culture.TwoLetterISOLanguageName == "en" ? string.Empty : neutral.EnglishName;

        TextFlowDirection = culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        IsEnabled = true;
        //_nativeNameOverride = nativeNameOverride;
        _flagKey = flagKey ?? GetFlagKeyFromCulture(culture);
        LanguageType = language;
    }

    // "français" → "Français"; scripts without case are unchanged
    private static string Capitalize(string text, CultureInfo culture)
        => string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0], culture) + text.Substring(1);

    private static string GetFlagKeyFromCulture(CultureInfo culture)
    {
        var parts = culture.Name.Split('-');
        return parts.Length > 1 ? parts[^1].ToLowerInvariant() : culture.TwoLetterISOLanguageName.ToLowerInvariant();
    }

    public CultureInfo GetCultureInfo() => GetCultureInfo(LanguageType);

    public static LanguageItem Create(LanguageType language)
    {
        CultureInfo culture = GetCultureInfo(language);

        return new LanguageItem(culture, language);
    }

    private static CultureInfo GetCultureInfo(LanguageType language)
    {
        try
        {
            var name = language.ToString().Replace('_', '-');

            return new CultureInfo(name);
        }
        catch (Exception)
        {
            return new CultureInfo("en-US");
        }
    }

    /// <summary>
    /// Switches the application to <paramref name="language"/> (English for a culture this PC does not know). The
    /// one place the apps change their culture: at start-up and when the user picks a language.
    /// </summary>
    public static void ApplyCulture(LanguageType language) => LocalizationManager.Instance.SetCulture(GetCultureInfo(language));
}