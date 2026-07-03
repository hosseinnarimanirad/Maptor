using IRI.Maptor.Sta.Pdf;

namespace IRI.Maptor.Jab.Common.Models.Print;

/// <summary>
/// User selections from the print-to-PDF options dialog (JSON-friendly so apps can persist them)
/// </summary>
public class PrintToPdfDialogOptions
{
    public string? MapTitle { get; set; }

    /// <summary>
    /// False produces the classic plain full-bleed export, ignoring all other options
    /// </summary>
    public bool IncludeDecorations { get; set; } = true;

    public bool ShowScaleBar { get; set; } = true;

    public bool ShowMaptorLogo { get; set; } = true;

    public bool ShowCompanyLogo { get; set; }

    /// <summary>
    /// PNG file of the map producer's company logo
    /// </summary>
    public string? CompanyLogoPath { get; set; }

    public bool ShowGraticule { get; set; } = true;

    public PdfPageSize PageSize { get; set; } = PdfPageSize.A4;

    public PdfPageOrientation PageOrientation { get; set; } = PdfPageOrientation.Landscape;
}