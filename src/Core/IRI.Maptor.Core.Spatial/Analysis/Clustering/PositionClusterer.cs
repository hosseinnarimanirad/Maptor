using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.AdvancedStructures;
using IRI.Maptor.Core.Spatial.Helpers;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>A group of positions that are "the same place" as each other, directly or through others.</summary>
public sealed class PositionCluster
{
    public PositionCluster(IReadOnlyList<int> members, double diameter, bool isChain)
    {
        Members = members;
        Diameter = diameter;
        IsChain = isChain;
    }

    /// <summary>Indices into the input list, in ascending order.</summary>
    public IReadOnlyList<int> Members { get; }

    /// <summary>The largest distance between two members (metres when geodetic, coordinate units otherwise).</summary>
    public double Diameter { get; }

    /// <summary>
    /// True when the members reach farther apart than the chain limit: they joined through each other, not
    /// because all of them are close. A person should decide which of them really belong together.
    /// </summary>
    public bool IsChain { get; }

    public override string ToString() => $"{Members.Count} positions, {Diameter:0.###} across" + (IsChain ? " (chain)" : string.Empty);
}

/// <summary>
/// Groups positions that lie within a distance of each other. Two positions are linked when they are within
/// the distance; a cluster is every position reachable through links (single linkage), so "within the
/// distance" chains: A near B and B near C puts A, B and C in one cluster even when A is far from C. Such a
/// cluster is marked <see cref="PositionCluster.IsChain"/> when its diameter is more than
/// <c>chainFactor</c> times the distance.
/// <para>
/// Geodetic WGS84 positions are measured in metres with a <see cref="LocalMetricFrame"/>; any other SRS in its
/// own units. Candidate pairs come from a <see cref="BoxGrid{TItem}"/>, so the work grows with the number of
/// positions, not with its square. The result does not depend on the order of the input beyond the indices
/// it reports.
/// </para>
/// </summary>
public static class PositionClusterer
{
    /// <param name="positions">The positions; null or non-finite entries are skipped.</param>
    /// <param name="distance">Positions this close or closer are linked (0 links identical positions only).</param>
    /// <param name="isGeodetic">True for WGS84 longitude/latitude, measured in metres.</param>
    /// <param name="chainFactor">A cluster wider than this many times the distance is a chain.</param>
    /// <returns>Clusters of two or more positions, ordered by their first member.</returns>
    public static IReadOnlyList<PositionCluster> Cluster<T>(IReadOnlyList<T?> positions, double distance, bool isGeodetic, double chainFactor = 2)
        where T : IPoint
    {
        if (positions is null)
            throw new ArgumentNullException(nameof(positions));

        distance = Math.Max(0, distance);

        var valid = new List<int>(positions.Count);

        for (int i = 0; i < positions.Count; i++)
        {
            if (positions[i] is T p && IsFinite(p.X) && IsFinite(p.Y))
                valid.Add(i);
        }

        if (valid.Count < 2)
            return Array.Empty<PositionCluster>();

        var halfSizes = valid.ToDictionary(i => i, i => LocalMetricFrame.At(positions[i]!, isGeodetic).SearchHalfSize(distance));

        var cellSize = halfSizes.Values.Select(h => 2 * Math.Max(h.HalfWidth, h.HalfHeight)).Max();

        var grid = new BoxGrid<int>(cellSize);

        foreach (var i in valid)
            grid.Add(i, positions[i]!.X, positions[i]!.Y, positions[i]!.X, positions[i]!.Y);

        var parent = new Dictionary<int, int>(valid.Count);

        foreach (var i in valid)
            parent[i] = i;

        foreach (var i in valid)
        {
            var p = positions[i]!;
            var (dx, dy) = halfSizes[i];

            foreach (var j in grid.Query(p.X - dx, p.Y - dy, p.X + dx, p.Y + dy))
            {
                if (j <= i)
                    continue;

                if (Distance(p, positions[j]!, isGeodetic) <= distance)
                    Union(parent, i, j);
            }
        }

        var clusters = new List<PositionCluster>();

        foreach (var group in valid.GroupBy(i => Find(parent, i)).Where(g => g.Count() > 1))
        {
            var members = group.OrderBy(i => i).ToList();

            var diameter = 0.0;

            for (int a = 0; a < members.Count; a++)
            {
                for (int b = a + 1; b < members.Count; b++)
                    diameter = Math.Max(diameter, Distance(positions[members[a]]!, positions[members[b]]!, isGeodetic));
            }

            clusters.Add(new PositionCluster(members, diameter, diameter > chainFactor * distance));
        }

        return clusters.OrderBy(c => c.Members[0]).ToList();
    }

    private static double Distance<T>(T first, T second, bool isGeodetic) where T : IPoint
    {
        var (east, north) = LocalMetricFrame.At(first, isGeodetic).ToMetres(second);

        return Math.Sqrt(east * east + north * north);
    }

    private static int Find(Dictionary<int, int> parent, int i)
    {
        var root = i;

        while (parent[root] != root)
            root = parent[root];

        while (parent[i] != root)
        {
            var next = parent[i];
            parent[i] = root;
            i = next;
        }

        return root;
    }

    private static void Union(Dictionary<int, int> parent, int a, int b)
    {
        var rootA = Find(parent, a);
        var rootB = Find(parent, b);

        if (rootA == rootB)
            return;

        // The smaller index becomes the root, so the result does not depend on the order of union.
        if (rootA < rootB)
            parent[rootB] = rootA;
        else
            parent[rootA] = rootB;
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
