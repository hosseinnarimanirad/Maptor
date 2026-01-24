using System;

using IRI.Maptor.Jab.Common.Abstractions;

namespace IRI.Maptor.Jab.Common.Data;

public class GeneralSettings : IGeneralSettings
{
    public double LegendFontSize { get; set; } = 10;


    public static GeneralSettings Default => new GeneralSettings();
}
