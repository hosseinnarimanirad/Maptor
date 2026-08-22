namespace IRI.Maptor.Presentation.Blazor.Toc;

/// <summary>
/// One entry in the layer table of contents — a group or a drawable layer.
///
/// Deliberately app-agnostic: it carries only what a TOC must draw and let the user change
/// (title, nesting, a legend swatch, visibility, opacity, the zoom band a layer is drawn in).
/// Nothing here knows about Saba's LayerSetting, its service URLs or its permissions — a host
/// app maps its own API contract onto this shape, which is what keeps the panel reusable across
/// the Maptor web clients.
///
/// Mutable on purpose: <see cref="IsVisible"/>, <see cref="IsExpanded"/> and
/// <see cref="Opacity"/> are live UI state the panel edits in place, and the host observes
/// through the panel's change callback rather than by rebuilding the tree.
/// </summary>
public sealed class TocLayerNode
{
    /// <summary>Host-assigned identity, unique within one tree. Used as the render key.</summary>
    public required string Id { get; init; }

    public required string Title { get; init; }

    public bool IsGroup { get; init; }

    public IReadOnlyList<TocLayerNode> Children { get; init; } = [];

    /// <summary>Legend chip for a drawable layer; null for groups.</summary>
    public TocSwatch? Swatch { get; init; }

    /// <summary>Inclusive zoom band this layer draws in; null means "no limit at this end".</summary>
    public int? MinZoomLevel { get; init; }

    public int? MaxZoomLevel { get; init; }

    public bool IsVisible { get; set; } = true;

    public bool IsExpanded { get; set; }

    /// <summary>0..1.</summary>
    public double Opacity { get; set; } = 1;

    public bool IsWithinZoomRange(int zoomLevel) =>
        (MinZoomLevel is null || zoomLevel >= MinZoomLevel)
        && (MaxZoomLevel is null || zoomLevel <= MaxZoomLevel);

    public IEnumerable<TocLayerNode> SelfAndDescendants()
    {
        yield return this;

        foreach (var child in Children)
        {
            foreach (var descendant in child.SelfAndDescendants())
                yield return descendant;
        }
    }

    /// <summary>The drawable layers under this node (the node itself when it is not a group).</summary>
    public IEnumerable<TocLayerNode> Leaves() => SelfAndDescendants().Where(node => !node.IsGroup);

    /// <summary>
    /// Ticking a group ticks everything under it — the behaviour every desktop GIS TOC has, and
    /// the reason a group's own checkbox is derived (see <see cref="GetGroupState"/>) rather than
    /// stored independently, which would let the two disagree.
    /// </summary>
    public void SetVisibility(bool isVisible)
    {
        IsVisible = isVisible;

        foreach (var child in Children)
            child.SetVisibility(isVisible);
    }

    /// <summary>
    /// Tri-state for a group's checkbox: true = every leaf on, false = every leaf off,
    /// null = mixed. A group with no leaves reads as off.
    /// </summary>
    public bool? GetGroupState()
    {
        var leaves = Leaves().ToList();

        if (leaves.Count == 0)
            return false;

        if (leaves.All(leaf => leaf.IsVisible))
            return true;

        if (leaves.All(leaf => !leaf.IsVisible))
            return false;

        return null;
    }
}

/// <summary>
/// The legend chip drawn beside a layer's title: its fill, outline and outline weight, in the
/// host's stored colour format (see <see cref="HexColor.ToCss"/>).
/// </summary>
public sealed record TocSwatch(string? FillColor, string? StrokeColor, double StrokeThickness);
