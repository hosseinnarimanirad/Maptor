using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.Analysis.Network;

/// <summary>
/// An edge in a <see cref="LineNetwork{T}"/>: one line feature, connecting the junction nodes
/// at its first and last vertex. <see cref="Length"/> is measured with the SRID-appropriate
/// metric (ellipsoidal for WGS84, Euclidean otherwise).
/// </summary>
public class LineNetworkEdge<T> where T : IPoint, new()
{
    public int Id { get; }

    /// <summary>Index of the source feature in the list passed to the builder.</summary>
    public int SourceIndex { get; }

    public Geometry<T> Feature { get; }

    public int StartNodeId { get; }

    public int EndNodeId { get; }

    public double Length { get; }

    /// <summary>
    /// Every node this edge touches: its two endpoints, plus any interior vertex that coincides with
    /// another feature's endpoint (a T-junction). Used to report adjacency symmetrically.
    /// </summary>
    public List<int> IncidentNodeIds { get; } = new List<int>();

    public LineNetworkEdge(int id, int sourceIndex, Geometry<T> feature, int startNodeId, int endNodeId, double length)
    {
        Id = id;
        SourceIndex = sourceIndex;
        Feature = feature;
        StartNodeId = startNodeId;
        EndNodeId = endNodeId;
        Length = length;

        IncidentNodeIds.Add(startNodeId);

        if (endNodeId != startNodeId)
            IncidentNodeIds.Add(endNodeId);
    }

    /// <summary>Given one endpoint node of this edge, returns the node at the other end.</summary>
    public int GetOtherNode(int nodeId) => nodeId == StartNodeId ? EndNodeId : StartNodeId;

    public override string ToString() => $"Edge {Id} (feature {SourceIndex}): {StartNodeId} -> {EndNodeId}, length {Length:0.###}";
}
