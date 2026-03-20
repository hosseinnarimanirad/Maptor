using System;
using System.Globalization;
using System.Windows;

namespace IRI.Maptor.Jab.Common.Localization;

public class LanguageItem : Notifier
{
    //public CultureInfo Culture { get; }
    public string Name { get; init; }
    public string EnglishName { get; init; } //=> Culture.EnglishName;
    public string NativeName { get; init; }// => /*_nativeNameOverride ??*/ Culture.NativeName;
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

    public LanguageType LanguageType { get; }

    //private readonly string? _nativeNameOverride;

    private readonly string _flagKey;

    /// <summary>
    /// Pack URI for the country flag image. Uses region code (e.g. us, ir, sa) when available.
    /// </summary>
    public Uri FlagUri => new Uri($"pack://application:,,,/IRI.Maptor.Jab.Common;component/Assets/Images/Flags/{_flagKey}.png", UriKind.Absolute);

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
        TextFlowDirection = culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        IsEnabled = true;
        //_nativeNameOverride = nativeNameOverride;
        _flagKey = flagKey ?? GetFlagKeyFromCulture(culture);
        LanguageType = language;
    }

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
}