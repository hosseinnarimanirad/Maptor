namespace IRI.Maptor.Core.Spatial.Helpers.MapGrids;

/// <summary>
/// The knobs the grid engine turns. Every default here is a cartographic judgement rather than an
/// arbitrary number; the ones worth re-tuning against a real window are marked.
/// </summary>
public sealed class MapGridOptions
{
    /// <summary>
    /// The fewest major lines the chosen interval must put across the view. Below three the grid
    /// stops reading as a grid. <em>Tune against a real window.</em>
    /// </summary>
    public int MinMajorLines { get; set; } = 3;

    /// <summary>
    /// The most major lines the chosen interval should put across the view before the next coarser
    /// step is preferred. <em>Tune against a real window.</em>
    /// </summary>
    public int MaxMajorLines { get; set; } = 6;

    /// <summary>
    /// How far in from the edge of the view the values sit, as a fraction of the view. Far enough
    /// not to be clipped, close enough to read as a margin. The MGRS overlay uses 0.04 for its
    /// single row.
    /// </summary>
    public double EdgeInset { get; set; } = 0.03;

    /// <summary>
    /// How much further in each successive <see cref="MapGridDefinition.LabelTier"/> sits. With two
    /// grids on the map the second one's numbers land inside the first one's instead of on top of
    /// them.
    /// </summary>
    public double TierInset { get; set; } = 0.045;

    /// <summary>
    /// Vertices per line. A line of constant easting is a curve in Web Mercator and spans the whole
    /// view, so it needs far more than the four samples an MGRS cell edge gets. Meridians and
    /// parallels ignore this — they are straight in Web Mercator and use two.
    /// </summary>
    public int SamplesPerLine { get; set; } = 32;

    /// <summary>Samples along each edge of the view when finding the view's range in a projection's plane.</summary>
    public int SamplesPerViewEdge { get; set; } = 16;

    /// <summary>A hard ceiling on the lines one request may produce, so a pathological extent truncates rather than hangs.</summary>
    public int MaxLines { get; set; } = 400;

    /// <summary>A hard ceiling on the labels one request may produce.</summary>
    public int MaxLabels { get; set; } = 400;

    /// <summary>
    /// How much of the view two values must be apart, horizontally, before both are written.
    /// Roughly the width of a label in a window about a thousand pixels across.
    /// </summary>
    /// <remarks>
    /// A grid crowds its own margin in two places a fixed interval cannot prevent: a UTM zone seam,
    /// where the last easting of one zone, the first of the next and the seam's own caption all land
    /// within a few kilometres of each other, and the corners, where a row of eastings runs into the
    /// column of northings. Both are suppressed by proximity rather than by rule, because both are
    /// accidents of where the view happens to sit. <em>Tune against a real window.</em>
    /// </remarks>
    public double MinLabelSeparationX { get; set; } = 0.05;

    /// <summary>How much of the view two values must be apart vertically. See <see cref="MinLabelSeparationX"/>.</summary>
    public double MinLabelSeparationY { get; set; } = 0.035;

    /// <summary>Whether the subdivisions between major lines are drawn at all.</summary>
    public bool ShowMinorLines { get; set; } = true;

    /// <summary>
    /// Whether minor lines are numbered too. Off, and deliberately so: a topographic sheet numbers
    /// the principal lines only, and numbering five times as many would crowd the margin into
    /// illegibility.
    /// </summary>
    public bool LabelMinorLines { get; set; } = false;

    /// <summary>Whether a UTM grid draws the meridians where it restarts.</summary>
    public bool ShowZoneSeams { get; set; } = true;

    /// <summary>The defaults, shared. Do not mutate this instance; construct your own to change anything.</summary>
    public static MapGridOptions Default { get; } = new MapGridOptions();

    /// <summary>The inset of a given tier, as a fraction of the view.</summary>
    public double GetInset(int tier) => EdgeInset + TierInset * (tier < 0 ? 0 : tier);
}
