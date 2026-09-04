using System;
using System.Collections.Generic;

namespace IRI.Maptor.Core.Spatial.Helpers.MapGrids;

/// <summary>
/// The rounds a grid interval is allowed to take, and how one is picked for a view.
/// </summary>
/// <remarks>
/// <para>
/// Two ladders, because the two families of grid count differently: degrees run
/// 30° 20° 10° 5° 2° 1° 30′ 20′ 15′ … 1″, and metres run 1-2-5 through every decade. The degree
/// ladder is <see cref="GraticuleHelper.IntervalLadderDegrees"/> — the same one the PDF composer
/// has used all along, so a printed sheet and the screen agree.
/// </para>
/// </remarks>
public static class MapGridLadders
{
    /// <summary>Degrees, coarse to fine: 30° down to 1″.</summary>
    public static IReadOnlyList<double> Degrees { get; } = GraticuleHelper.IntervalLadderDegrees;

    /// <summary>Metres, coarse to fine: 1 000 km down to 10 m, in 1-2-5 steps.</summary>
    public static IReadOnlyList<double> Metres { get; } = BuildMetres();

    private static IReadOnlyList<double> BuildMetres()
    {
        var result = new List<double>();

        // 1 000 km is coarser than any view that still shows a metric grid usefully; 10 m is the
        // finest a map ever draws. Below that, and the map is measuring, not gridding.
        // Every decade contributes its 1, its 5 and its 2 — 1000, 500, 200 — so the loop stops at
        // the hundreds and 10 m is added on its own rather than dragging 5 m and 2 m in with it.
        for (var decade = 1_000_000.0; decade >= 100.0; decade /= 10.0)
        {
            result.Add(decade);
            result.Add(decade / 2.0);
            result.Add(decade / 5.0);
        }

        result.Add(10.0);

        return result;
    }

    /// <summary>
    /// The interval for a view: the coarsest ladder step that still puts at least
    /// <paramref name="minLines"/> lines across <paramref name="span"/>, stepping back one when
    /// that first step overshoots <paramref name="maxLines"/>.
    /// </summary>
    /// <remarks>
    /// This is <see cref="GraticuleHelper.ChooseInterval"/>'s rule, generalized over a ladder.
    /// Stepping back matters because the ladders are coarse: a step of 2.5× can jump straight from
    /// "too few lines" to "far too many", and the coarser of the two is then the closer fit. The
    /// rule stays monotonic — narrowing the view never yields a coarser interval — because the
    /// fallback returns a step the larger span would have returned too.
    /// </remarks>
    /// <param name="span">The larger of the view's two sides, in the ladder's units.</param>
    /// <param name="ladder">The rounds this grid is allowed to take, coarse to fine.</param>
    /// <param name="minLines">The fewest lines the chosen step must put across the span.</param>
    /// <param name="maxLines">Above this the next coarser step is preferred.</param>
    public static double ChooseMajor(double span, IReadOnlyList<double> ladder, int minLines = 3, int maxLines = 6)
    {
        if (ladder is null || ladder.Count == 0)
            throw new ArgumentException("A ladder must have at least one step.", nameof(ladder));

        if (double.IsNaN(span) || double.IsInfinity(span) || span <= 0)
            return ladder[0];

        for (var i = 0; i < ladder.Count; i++)
        {
            var interval = ladder[i];

            var count = span / interval;

            if (count < minLines)
                continue;

            if (count > maxLines && i > 0)
                return ladder[i - 1];

            return interval;
        }

        return ladder[ladder.Count - 1];
    }

    /// <summary>
    /// The subdivision of <paramref name="major"/>: the finest ladder step below it that divides it
    /// evenly into no more than <paramref name="maxSubdivisions"/> parts. Null when the ladder has
    /// nothing finer to offer.
    /// </summary>
    /// <remarks>
    /// The cap is what keeps the result round and the map readable — 1 km subdivides into 200 m
    /// (five parts) rather than 100 m (ten), 2 km into 500 m, 1° into 15′, 10′ into 2′. Requiring
    /// the divisor to be a ladder member too means a minor line always sits on a value the grid
    /// would itself draw one step finer, so zooming in promotes minor lines to major ones instead
    /// of shifting the whole pattern.
    /// </remarks>
    public static double? MinorOf(double major, IReadOnlyList<double> ladder, int maxSubdivisions = 5)
    {
        if (ladder is null || double.IsNaN(major) || major <= 0)
            return null;

        double? best = null;

        foreach (var candidate in ladder)
        {
            if (candidate >= major)
                continue;

            var ratio = major / candidate;

            var count = Math.Round(ratio);

            if (count < 2 || count > maxSubdivisions)
                continue;

            // Ladder values such as 30/60.0 are not exact in binary, so evenness is a tolerance
            // test rather than a modulo.
            if (Math.Abs(ratio - count) > 1e-6 * count)
                continue;

            // The ladder runs coarse to fine, so later qualifying candidates are finer; the last
            // one is the finest subdivision still within the cap.
            best = candidate;
        }

        return best;
    }

    /// <summary>The ladder a definition counts on.</summary>
    public static IReadOnlyList<double> For(MapGridDefinition definition)
        => definition.IsAngular ? Degrees : Metres;
}
