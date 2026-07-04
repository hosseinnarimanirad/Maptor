using PdfSharpCore.Drawing;

namespace IRI.Maptor.Sta.Pdf;

/// <summary>
/// Page-space layout rectangles (top-left origin, points) for a decorated map export laid out
/// as a standard three-column map sheet:
/// <list type="bullet">
/// <item>left column — reserved legend box (bordered header, empty body for now);</item>
/// <item>center column — the map, with the title in a small bordered box on top;</item>
/// <item>right column — company logo, then company title and subtitle;</item>
/// <item>bottom band — export date/time (left), scale bar (centered under the map), Maptor logo (right).</item>
/// </list>
/// The center column's <see cref="MapFrameRect"/> is what the map transform draws into.
/// </summary>
internal readonly struct PdfMapLayout
{
    public const double OuterMargin = 28;

    /// <summary>Reserved width of the left (legend) column.</summary>
    public const double LegendColumnWidth = 120;

    /// <summary>Reserved width of the right (company info) column.</summary>
    public const double CompanyColumnWidth = 132;

    /// <summary>Horizontal gap between a side column and the center column.</summary>
    public const double ColumnGap = 12;

    /// <summary>Height of the bordered map-title box atop the center column.</summary>
    public const double TitleBoxHeight = 26;

    /// <summary>Height of the bottom band holding date/time, scale bar and Maptor logo.</summary>
    public const double FooterBandHeight = 44;

    /// <summary>Vertical gap between the title box / map / bottom band.</summary>
    public const double BandGap = 8;

    /// <summary>
    /// Extra space reserved around the map frame for graticule edge labels
    /// </summary>
    public const double GraticuleLabelMargin = 16;

    /// <summary>Small bordered box centered over the map holding the map title.</summary>
    public XRect TitleBoxRect { get; }

    /// <summary>The map draw area (center column, below the title box), inset for graticule labels.</summary>
    public XRect MapFrameRect { get; }

    /// <summary>Reserved legend column (left in LTR, right in RTL).</summary>
    public XRect LegendColumnRect { get; }

    /// <summary>Company-info column (right in LTR, left in RTL): logo, title, subtitle.</summary>
    public XRect CompanyColumnRect { get; }

    /// <summary>Bottom cell holding the export date/time, aligned under the legend column.</summary>
    public XRect DateTimeCellRect { get; }

    /// <summary>Bottom-center cell (scale bar), aligned under the map column.</summary>
    public XRect ScaleBarCellRect { get; }

    /// <summary>Bottom cell holding the Maptor logo, aligned under the company column.</summary>
    public XRect MaptorCellRect { get; }

    public bool HasTitleBox { get; }

    public bool HasLegendColumn { get; }

    public bool HasCompanyColumn { get; }

    public bool HasBottomBand { get; }

    public bool HasGraticuleMargin { get; }

    private PdfMapLayout(
        XRect titleBox, XRect frame, XRect legendColumn, XRect companyColumn,
        XRect dateTimeCell, XRect scaleBarCell, XRect maptorCell,
        bool hasTitleBox, bool hasLegendColumn, bool hasCompanyColumn, bool hasBottomBand, bool hasGraticuleMargin)
    {
        TitleBoxRect = titleBox;
        MapFrameRect = frame;
        LegendColumnRect = legendColumn;
        CompanyColumnRect = companyColumn;
        DateTimeCellRect = dateTimeCell;
        ScaleBarCellRect = scaleBarCell;
        MaptorCellRect = maptorCell;
        HasTitleBox = hasTitleBox;
        HasLegendColumn = hasLegendColumn;
        HasCompanyColumn = hasCompanyColumn;
        HasBottomBand = hasBottomBand;
        HasGraticuleMargin = hasGraticuleMargin;
    }

    /// <summary>
    /// Column/gutter metrics shared by <see cref="Create"/> and <see cref="PageSizeForFrame"/> so the
    /// forward and inverse computations stay in exact sync. The legend gutter is reserved when the
    /// legend column has content or a date/time stamp exists; the company gutter when the company
    /// column has content or the Maptor logo exists — keeping the bottom row aligned under the columns.
    /// These are <b>semantic</b> gutters; <see cref="Create"/> maps them to physical sides by RTL.
    /// </summary>
    private static (double LegendGutter, double CompanyGutter, double LegendGap, double CompanyGap, double GraticuleMargin, bool HasTitleBox, bool HasBottomBand) Metrics(PdfMapDecorations decorations)
    {
        var hasLegendColumn = decorations.ShowLegendColumn;
        var hasCompanyColumn = decorations.HasCompanyInfo;

        var dateTimePresent = decorations.DateTimeVector is { IsValid: true } || !string.IsNullOrWhiteSpace(decorations.DateTimeText);
        var maptorPresent = decorations.PrimaryVectorLogo is { IsValid: true } || decorations.PrimaryLogoPngBytes is { Length: > 0 };

        var legendGutter = (hasLegendColumn || dateTimePresent) ? LegendColumnWidth : 0;
        var companyGutter = (hasCompanyColumn || maptorPresent) ? CompanyColumnWidth : 0;

        var legendGap = legendGutter > 0 ? ColumnGap : 0;
        var companyGap = companyGutter > 0 ? ColumnGap : 0;

        var graticuleMargin = decorations.ShowGraticule ? GraticuleLabelMargin : 0;

        return (legendGutter, companyGutter, legendGap, companyGap, graticuleMargin, decorations.HasTitle, decorations.HasBottomBand);
    }

    public static PdfMapLayout Create(double pageWidth, double pageHeight, PdfMapDecorations decorations)
    {
        var m = Metrics(decorations);

        var rtl = decorations.RightToLeft;
        var hasLegendColumn = decorations.ShowLegendColumn;
        var hasCompanyColumn = decorations.HasCompanyInfo;

        // Map the semantic legend/company gutters onto physical sides. LTR keeps the sketch
        // (legend left, company right); RTL mirrors them (legend right, company left).
        var leftGutter = rtl ? m.CompanyGutter : m.LegendGutter;
        var rightGutter = rtl ? m.LegendGutter : m.CompanyGutter;
        var leftGap = rtl ? m.CompanyGap : m.LegendGap;
        var rightGap = rtl ? m.LegendGap : m.CompanyGap;

        var contentLeft = OuterMargin;
        var contentRight = pageWidth - OuterMargin;
        var contentTop = OuterMargin;
        var contentBottom = pageHeight - OuterMargin;

        // Bottom band reserved off the bottom of the content area.
        var bottomBandTop = m.HasBottomBand ? contentBottom - FooterBandHeight : contentBottom;
        var columnsTop = contentTop;
        var columnsBottom = m.HasBottomBand ? bottomBandTop - BandGap : contentBottom;
        var columnsHeight = Math.Max(1, columnsBottom - columnsTop);

        // Horizontal split: left gutter | gap | center | gap | right gutter.
        var centerX = contentLeft + leftGutter + leftGap;
        var centerRight = contentRight - rightGutter - rightGap;
        var centerWidth = Math.Max(1, centerRight - centerX);

        var leftColumnRect = new XRect(contentLeft, columnsTop, leftGutter, columnsHeight);
        var rightColumnRect = new XRect(contentRight - rightGutter, columnsTop, rightGutter, columnsHeight);
        var leftBottomCell = new XRect(contentLeft, bottomBandTop, leftGutter, FooterBandHeight);
        var rightBottomCell = new XRect(contentRight - rightGutter, bottomBandTop, rightGutter, FooterBandHeight);

        // Assign columns/cells to their content by reading direction.
        var legendColumnRect = rtl ? rightColumnRect : leftColumnRect;
        var companyColumnRect = rtl ? leftColumnRect : rightColumnRect;
        var dateTimeCell = rtl ? rightBottomCell : leftBottomCell;   // date/time sits under the legend
        var maptorCell = rtl ? leftBottomCell : rightBottomCell;     // Maptor logo sits under the company

        // Center column: title box on top, map frame below (inset for graticule labels).
        var titleBoxRect = new XRect(centerX, columnsTop, centerWidth, m.HasTitleBox ? TitleBoxHeight : 0);
        var mapTop = columnsTop + (m.HasTitleBox ? TitleBoxHeight + BandGap : 0);

        var frameRect = new XRect(
            centerX + m.GraticuleMargin,
            mapTop + m.GraticuleMargin,
            Math.Max(1, centerWidth - 2 * m.GraticuleMargin),
            Math.Max(1, columnsBottom - mapTop - 2 * m.GraticuleMargin));

        var scaleBarCell = new XRect(centerX, bottomBandTop, centerWidth, FooterBandHeight);

        return new PdfMapLayout(
            titleBoxRect, frameRect, legendColumnRect, companyColumnRect,
            dateTimeCell, scaleBarCell, maptorCell,
            m.HasTitleBox, hasLegendColumn, hasCompanyColumn, m.HasBottomBand, m.GraticuleMargin > 0);
    }

    /// <summary>
    /// Inverse of <see cref="Create"/>: the page size whose map frame is exactly
    /// <paramref name="frameWidth"/> × <paramref name="frameHeight"/> for the given decorations
    /// (same margins/columns/bands). Used by the preserve-map-scale export to size a custom page.
    /// </summary>
    public static (double PageWidth, double PageHeight) PageSizeForFrame(double frameWidth, double frameHeight, PdfMapDecorations decorations)
    {
        var m = Metrics(decorations);

        var pageWidth = frameWidth + 2 * m.GraticuleMargin
                        + m.LegendGutter + m.CompanyGutter + m.LegendGap + m.CompanyGap
                        + 2 * OuterMargin;

        var pageHeight = frameHeight + 2 * m.GraticuleMargin
                         + (m.HasTitleBox ? TitleBoxHeight + BandGap : 0)
                         + (m.HasBottomBand ? FooterBandHeight + BandGap : 0)
                         + 2 * OuterMargin;

        return (pageWidth, pageHeight);
    }
}
