using PdfSharpCore.Drawing;

namespace IRI.Maptor.Sta.Pdf;

/// <summary>
/// Page-space layout rectangles (top-left origin, points) for a decorated map export:
/// optional title band on top, map frame in the middle, optional footer band below.
/// </summary>
internal readonly struct PdfMapLayout
{
    public const double OuterMargin = 28;
    public const double TitleBandHeight = 54;
    public const double FooterBandHeight = 44;
    public const double BandGap = 8;

    /// <summary>
    /// Extra space reserved around the map frame for graticule edge labels
    /// </summary>
    public const double GraticuleLabelMargin = 16;

    public XRect TitleBandRect { get; }

    public XRect MapFrameRect { get; }

    public XRect FooterBandRect { get; }

    public bool HasTitleBand { get; }

    public bool HasFooterBand { get; }

    public bool HasGraticuleMargin { get; }

    private PdfMapLayout(XRect title, XRect frame, XRect footer, bool hasTitle, bool hasFooter, bool hasGraticuleMargin)
    {
        TitleBandRect = title;
        MapFrameRect = frame;
        FooterBandRect = footer;
        HasTitleBand = hasTitle;
        HasFooterBand = hasFooter;
        HasGraticuleMargin = hasGraticuleMargin;
    }

    public static PdfMapLayout Create(double pageWidth, double pageHeight, PdfMapDecorations decorations)
    {
        var hasTitleBand = decorations.HasTitle || decorations.SecondaryLogoPngBytes is { Length: > 0 };
        var hasFooterBand = decorations.ShowScaleBar || decorations.PrimaryLogoPngBytes is { Length: > 0 };

        var contentX = OuterMargin;
        var contentWidth = pageWidth - 2 * OuterMargin;

        var top = OuterMargin;
        var bottom = pageHeight - OuterMargin;

        var titleRect = new XRect(contentX, top, contentWidth, hasTitleBand ? TitleBandHeight : 0);

        if (hasTitleBand)
            top += TitleBandHeight + BandGap;

        var footerRect = new XRect(contentX, bottom - (hasFooterBand ? FooterBandHeight : 0), contentWidth, hasFooterBand ? FooterBandHeight : 0);

        if (hasFooterBand)
            bottom -= FooterBandHeight + BandGap;

        var graticuleMargin = decorations.ShowGraticule ? GraticuleLabelMargin : 0;

        var frameRect = new XRect(
            contentX + graticuleMargin,
            top + graticuleMargin,
            Math.Max(1, contentWidth - 2 * graticuleMargin),
            Math.Max(1, bottom - top - 2 * graticuleMargin));

        return new PdfMapLayout(titleRect, frameRect, footerRect, hasTitleBand, hasFooterBand, graticuleMargin > 0);
    }
}