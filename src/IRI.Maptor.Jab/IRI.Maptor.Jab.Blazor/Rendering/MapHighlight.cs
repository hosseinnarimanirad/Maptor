using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Jab.Blazor.Rendering;

/// <summary>
/// Geometry drawn on top of every layer to mark what is currently selected — the feature under a
/// click, or the search result the user picked.
///
/// <para>Deliberately not a <see cref="MapVectorLayer"/>: a highlight has no TOC row, no zoom band
/// and no visibility toggle, and it is drawn with a halo rather than the layer's own symbology so
/// it stays legible over whatever it sits on. Host-agnostic, like everything else in this folder —
/// what a selection *means* is the host's business, this only knows how to show one.</para>
/// </summary>
public sealed class MapHighlight
{
    /// <summary>Stored as <c>#AARRGGBB</c>, the convention the whole Maptor stack uses; converted
    /// at draw time by <see cref="Toc.HexColor"/>. Amber reads clearly over both the OSM basemap
    /// and the layer palette Saba seeds.</summary>
    public const string DefaultColor = "#FFFF6D00";

    private BoundingBox? _extent;

    public required IReadOnlyList<Geometry<Point>> Geometries { get; init; }

    public string Color { get; init; } = DefaultColor;

    /// <summary>
    /// Merged Web Mercator extent of everything highlighted, or <see cref="BoundingBox.NaN"/> when
    /// there is nothing to show. Cached — a highlight is immutable, and this is what zoom-to-result
    /// asks for.
    /// </summary>
    public BoundingBox Extent => _extent ??= CalculateExtent();

    private BoundingBox CalculateExtent()
    {
        var merged = BoundingBox.NaN;

        foreach (var geometry in Geometries)
        {
            if (geometry is null || geometry.IsNullOrEmpty())
                continue;

            var box = geometry.GetBoundingBox();

            if (box.IsNaN())
                continue;

            merged = merged.IsNaN() ? box : BoundingBox.Add(merged, box);
        }

        return merged;
    }
}
