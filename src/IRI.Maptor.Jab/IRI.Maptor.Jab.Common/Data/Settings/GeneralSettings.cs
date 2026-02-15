using System;

using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Models.Themes;

namespace IRI.Maptor.Jab.Common.Data;

public class GeneralSettings : IGeneralSettings
{
    public double LegendFontSize { get; set; } = 10;

    public bool Scalebar_ShowScalebar { get; set; } = true;

    public bool Scalebar_ShowScaleValue { get; set; } = true;

    public bool Scalebar_ShowZoomLevel { get; set; } = true;

    public bool CoordinatePanel_ShowCoordinatePanel { get; set; } = true;

    public bool Legend_ShowLegendTools { get; set; } = false;

    public bool Legend_ShowLayerColors { get; set; } = false;

    public MahAppsThemeColor? MahAppsTheme { get; set; } = MahAppsThemeColor.Amber;

    public static GeneralSettings Default => new GeneralSettings();
}
