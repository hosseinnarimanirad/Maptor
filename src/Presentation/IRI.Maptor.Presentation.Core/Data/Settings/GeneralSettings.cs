
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Core.Models;

namespace IRI.Maptor.Presentation.Core.Data;

public class GeneralSettings : IGeneralSettings
{
    public LanguageType CurrentLanguage { get; set; } = LanguageType.fa_IR;

    public List<LanguageType> AvailableLanguages { get; set; } = [LanguageType.en_US, LanguageType.fa_IR];

    public double LegendFontSize { get; set; } = 10;

    public bool Scalebar_ShowScalebar { get; set; } = true;

    public bool Scalebar_ShowScaleValue { get; set; } = true;

    public bool Scalebar_ShowZoomLevel { get; set; } = true;

    public bool CoordinatePanel_ShowCoordinatePanel { get; set; } = true;

    public bool Legend_ShowLegendTools { get; set; } = false;

    public bool Legend_ShowLayerColors { get; set; } = false;

    public bool FeatureTable_UseMultiRowTabs { get; set; } = true;

    public MahAppsThemeColor? MahAppsTheme { get; set; } = MahAppsThemeColor.Amber;

    /// <summary>
    /// Light or dark. Nullable so settings files written before dark mode existed
    /// deserialize to null and fall back to Light.
    /// </summary>
    public ThemeMode? MahAppsThemeMode { get; set; } = ThemeMode.Light;

    public static GeneralSettings Default => new GeneralSettings();
}
