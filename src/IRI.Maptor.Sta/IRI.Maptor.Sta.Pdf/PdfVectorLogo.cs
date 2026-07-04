namespace IRI.Maptor.Sta.Pdf;

/// <summary>
/// A logo expressed as vector outlines (flattened from a WPF geometry), so it prints crisp at
/// any size. Figure points are in the logo's own source coordinate space (top-left origin,
/// spanning <see cref="SourceWidth"/> × <see cref="SourceHeight"/>); the composer scales them
/// into the target region and fills with even-odd rule so counters/holes render correctly.
/// </summary>
public class PdfVectorLogo
{
    public List<PdfMarkerFigure> Figures { get; set; } = new();

    public double SourceWidth { get; set; }

    public double SourceHeight { get; set; }

    public bool IsValid => Figures.Count > 0 && SourceWidth > 0 && SourceHeight > 0;
}