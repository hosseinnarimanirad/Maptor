using IRI.Maptor.Sta.Common.Abstractions;

namespace IRI.Maptor.Sta.Spatial.Analysis.Network;

/// <summary>
/// A junction in a <see cref="LineNetwork{T}"/>: a unique coordinate where one or more
/// line features meet. <see cref="IncidentEdgeIds"/> lists every edge that touches this node,
/// either at one of its endpoints or (for T-junctions) at one of its interior vertices.
/// </summary>
public class LineNetworkNode<T> where T : IPoint, new()
{
    public int Id { get; }

    public T Location { get; }

    public List<int> IncidentEdgeIds { get; } = new List<int>();

    public LineNetworkNode(int id, T location)
    {
        Id = id;
        Location = location;
    }

    public override string ToString() => $"Node {Id} ({Location}) — {IncidentEdgeIds.Count} edge(s)";
}
