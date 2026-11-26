using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.AdvancedStructures;

/// <summary>
/// A uniform grid over item bounding boxes: each item is listed in every cell its box touches. A cheap
/// prefilter for "what is near here?", never the test itself.
/// <para>
/// It suits data that is fairly even in size, such as the segments of lines or the edges of rings. Index
/// segments or edges rather than whole large features: a big feature then costs cells in proportion to its
/// boundary, not its area.
/// </para>
/// <para>
/// Results are deterministic: cells are visited by column, then by row, and the items of a cell in the
/// order they were added. Coordinates must be finite and each box's minimum must not exceed its maximum.
/// </para>
/// </summary>
public sealed class BoxGrid<TItem> : ISpatialIndex<TItem>
{
    private readonly double _cellSize;
    private readonly IEqualityComparer<TItem> _comparer;
    private readonly Dictionary<(long, long), List<TItem>> _cells = new();

    /// <param name="cellSize">Cell width and height in coordinate units; 1 when not positive.</param>
    /// <param name="comparer">Decides when two items are the same item, so that a query returns it once.</param>
    public BoxGrid(double cellSize, IEqualityComparer<TItem>? comparer = null)
    {
        _cellSize = cellSize > 0 ? cellSize : 1;
        _comparer = comparer ?? EqualityComparer<TItem>.Default;
    }

    public double CellSize => _cellSize;

    public void Add(TItem item, BoundingBox box) => Add(item, box.XMin, box.YMin, box.XMax, box.YMax);

    public void Add(TItem item, double minX, double minY, double maxX, double maxY)
    {
        var (fromX, fromY) = Cell(minX, minY);
        var (toX, toY) = Cell(maxX, maxY);

        for (long x = fromX; x <= toX; x++)
        {
            for (long y = fromY; y <= toY; y++)
            {
                if (!_cells.TryGetValue((x, y), out var bucket))
                {
                    bucket = new List<TItem>();
                    _cells[(x, y)] = bucket;
                }

                bucket.Add(item);
            }
        }
    }

    public IEnumerable<TItem> Query(BoundingBox box) => Query(box.XMin, box.YMin, box.XMax, box.YMax);

    public IEnumerable<TItem> Query(double minX, double minY, double maxX, double maxY)
    {
        var (fromX, fromY) = Cell(minX, minY);
        var (toX, toY) = Cell(maxX, maxY);

        var seen = new HashSet<TItem>(_comparer);

        for (long x = fromX; x <= toX; x++)
        {
            for (long y = fromY; y <= toY; y++)
            {
                if (!_cells.TryGetValue((x, y), out var bucket))
                    continue;

                foreach (var item in bucket)
                {
                    if (seen.Add(item))
                        yield return item;
                }
            }
        }
    }

    private (long, long) Cell(double x, double y) => ((long)Math.Floor(x / _cellSize), (long)Math.Floor(y / _cellSize));
}
