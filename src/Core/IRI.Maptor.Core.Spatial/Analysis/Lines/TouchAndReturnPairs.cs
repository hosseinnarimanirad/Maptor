namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>
/// Where one line touches another and comes straight back: two crossings of the same pair of lines, next to each
/// other along the first line, within a distance of each other along both lines. Usually drawing noise (a vertex
/// a few centimetres over the other line), not two real crossings. A crossing left on its own is a real crossing.
/// Used by the topology rule R23 and by any analysis that lists crossings, so both decide the same way.
/// </summary>
public static class TouchAndReturnPairs
{
    /// <summary>The touch-and-return pairs among the crossings of ONE pair of lines.</summary>
    /// <param name="crossings">
    /// Each crossing's part and distance along the first line, and part and distance along the other line (metres).
    /// </param>
    /// <param name="distance">How close (metres, along both lines) the two crossings of a pair must be.</param>
    /// <returns>The two crossings of each pair (indices into <paramref name="crossings"/>) and how far apart they are along the first line.</returns>
    public static IReadOnlyList<(int First, int Second, double Along)> Find(
        IReadOnlyList<(int Part, double Measure, int OtherPart, double OtherMeasure)> crossings,
        double distance)
    {
        if (crossings is null)
            throw new ArgumentNullException(nameof(crossings));

        var ordered = Enumerable.Range(0, crossings.Count)
            .OrderBy(i => crossings[i].Part)
            .ThenBy(i => crossings[i].Measure)
            .ToList();

        var pairs = new List<(int, int, double)>();

        for (int k = 0; k + 1 < ordered.Count; k++)
        {
            var first = crossings[ordered[k]];
            var second = crossings[ordered[k + 1]];

            if (first.Part != second.Part || first.OtherPart != second.OtherPart)
                continue;

            var along = second.Measure - first.Measure;
            var alongOther = Math.Abs(second.OtherMeasure - first.OtherMeasure);

            if (along > distance || alongOther > distance)
                continue;

            pairs.Add((ordered[k], ordered[k + 1], along));

            k++;
        }

        return pairs;
    }
}
