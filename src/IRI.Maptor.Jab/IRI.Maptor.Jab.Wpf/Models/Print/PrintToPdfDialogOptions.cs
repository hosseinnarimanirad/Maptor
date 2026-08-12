using IRI.Maptor.Sta.Pdf;

namespace IRI.Maptor.Jab.Wpf.Models.Print;

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

    /// <summary>
    /// Company title shown in the right column, provided in code (not from dialog input);
    /// <see cref="ViewModels.Map.MapViewModelBase.PrintCompanyTitle"/> populates it per app.
    /// </summary>
    public string? CompanyTitle { get; set; }

    /// <summary>
    /// Company subtitle shown under <see cref="CompanyTitle"/>, provided in code (not from dialog input).
    /// </summary>
    public string? CompanySubtitle { get; set; }

    public PdfPageSize PageSize { get; set; } = PdfPageSize.A4;

    public PdfPageOrientation PageOrientation { get; set; } = PdfPageOrientation.Landscape;

    /// <summary>
    /// When true, the export keeps the map's current on-screen scale by sizing a custom page,
    /// instead of rescaling the map to fit <see cref="PageSize"/>. Page size/orientation are ignored.
    /// </summary>
    public bool PreserveMapScale { get; set; }
}