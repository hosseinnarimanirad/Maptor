using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.AdvancedStructures;

/// <summary>
/// Finds the items whose bounding boxes may intersect a query box. It is a prefilter, never the test
/// itself: callers still measure the items they get back. Boxes are in the data's own coordinate units.
/// </summary>
public interface ISpatialIndex<TItem>
{
    /// <summary>Adds an item with its bounding box.</summary>
    void Add(TItem item, BoundingBox box);

    /// <summary>
    /// Every item whose box may intersect <paramref name="box"/>, each once. It may also return items whose
    /// boxes only come near it.
    /// </summary>
    IEnumerable<TItem> Query(BoundingBox box);
}
