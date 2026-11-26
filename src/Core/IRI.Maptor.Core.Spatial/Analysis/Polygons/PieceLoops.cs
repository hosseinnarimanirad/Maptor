namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>Chains boundary pieces end to start into closed loops (shared by <see cref="EnclosedGaps"/> and <see cref="PolygonBoolean"/>).</summary>
internal static class PieceLoops
{
    /// <summary>The closed loops, each in chain order; pieces that close no loop are dropped.</summary>
    /// <param name="items">The pieces, with whatever each carries.</param>
    /// <param name="pieceOf">The piece of an item.</param>
    /// <param name="tolerance">How close (metres) an end and the next start must be.</param>
    public static IEnumerable<List<TItem>> Chain<TItem>(IReadOnlyList<TItem> items, Func<TItem, RingPiece> pieceOf, double tolerance)
    {
        var used = new bool[items.Count];

        double Gap((double X, double Y) p, (double X, double Y) q) => Math.Sqrt((p.X - q.X) * (p.X - q.X) + (p.Y - q.Y) * (p.Y - q.Y));

        for (int first = 0; first < items.Count; first++)
        {
            if (used[first])
                continue;

            var loop = new List<int> { first };
            used[first] = true;

            var closed = false;

            while (true)
            {
                var end = pieceOf(items[loop[loop.Count - 1]]).End;

                if (loop.Count > 2 && Gap(end, pieceOf(items[first]).Start) <= tolerance)
                {
                    closed = true;
                    break;
                }

                var next = -1;
                var best = double.MaxValue;

                for (int k = 0; k < items.Count; k++)
                {
                    if (used[k])
                        continue;

                    var gap = Gap(end, pieceOf(items[k]).Start);

                    if (gap <= tolerance && gap < best)
                    {
                        best = gap;
                        next = k;
                    }
                }

                if (next < 0)
                    break;

                used[next] = true;
                loop.Add(next);
            }

            if (closed)
                yield return loop.Select(k => items[k]).ToList();
        }
    }
}
