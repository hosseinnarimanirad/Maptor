using System;

using IRI.Maptor.Jab.Common.Abstractions;

namespace IRI.Maptor.Jab.Common.Data;

public class GeneralSettings : IGeneralSettings
{
    public double LegendFontSize { get; set; } = 10;

    public bool Scalebar_ShowScalebar { get; set; } = true;

    public bool Scalebar_ShowScaleValue { get; set; } = true;

    public bool Scalebar_ShowZoomLevel { get; set; } = true;

    public bool CoordinatePanel_ShowCoordinatePanel { get; set; } = true;

    public bool Legend_ShowLegendTools { get; set; } = false;    

    public string MahAppsTheme { get; set; } = "Light.Amber";

    public static GeneralSettings Default => new GeneralSettings();
}
