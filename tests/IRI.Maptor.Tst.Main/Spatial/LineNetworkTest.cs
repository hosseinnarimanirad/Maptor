using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.Analysis.Network;

using Xunit;

namespace IRI.Maptor.Tst.Spatial;

public class LineNetworkTest
{
    private static Geometry<Point> Line(string wkt) => Geometry<Point>.FromWkt(wkt, 0);

    private static LineNetwork<Point> Build(params string[] wkts)
        => new LineNetworkBuilder<Point>(1e-6).Build(wkts.Select(Line).ToList());

    [Fact]
    public void TwoLinesSharingEndpoint_AreAdjacent()
    {
        var network = Build("LINESTRING(0 0, 10 0)", "LINESTRING(10 0, 20 0)");

        Assert.Equal(3, network.Nodes.Count);
        Assert.Equal(2, network.Edges.Count);

        Assert.Contains(1, network.GetAdjacentEdges(0).Select(e => e.Id));
        Assert.Contains(0, network.GetAdjacentEdges(1).Select(e => e.Id));

        Assert.Single(network.GetConnectedComponents());
    }

    [Fact]
    public void EndpointOnInteriorVertex_IsTJunctionAdjacency()
    {
        // Edge 0 passes straight through (10,0) as an interior vertex; edge 1 starts there.
        var network = Build("LINESTRING(0 0, 10 0, 20 0)", "LINESTRING(10 0, 10 10)");

        // Adjacency must be symmetric in both directions.
        Assert.Contains(1, network.GetAdjacentEdges(0).Select(e => e.Id));
        Assert.Contains(0, network.GetAdjacentEdges(1).Select(e => e.Id));

        Assert.Single(network.GetConnectedComponents());
    }

    [Fact]
    public void DisconnectedLines_FormSeparateComponents()
    {
        var network = Build(
            "LINESTRING(0 0, 10 0)",
            "LINESTRING(10 0, 20 0)",
            "LINESTRING(100 100, 110 100)");

        var components = network.GetConnectedComponents();

        Assert.Equal(2, components.Count);
        Assert.Contains(components, c => c.Count == 2);
        Assert.Contains(components, c => c.Count == 1);
    }

    [Fact]
    public void ToAdjacencyList_HasNodePerJunction_AndWeightsEqualLengths()
    {
        var network = Build("LINESTRING(0 0, 10 0)", "LINESTRING(10 0, 20 0)");

        var graph = network.ToAdjacencyList();

        Assert.Equal(3, graph.NumberOfNodes);
        // Undirected: each feature contributes two directed connections.
        Assert.Equal(4, graph.NumberOfEdges);

        Assert.Equal(10.0, network.Edges[0].Length, 6);
        Assert.Equal(10.0, network.Edges[1].Length, 6);
    }

    [Fact]
    public void NearCoincidentEndpoints_SnapWithinTolerance()
    {
        // Second line starts 0.0000005 away from the first line's end — inside the 1e-6 tolerance.
        var network = Build("LINESTRING(0 0, 10 0)", "LINESTRING(10.0000005 0, 20 0)");

        Assert.Equal(3, network.Nodes.Count);
        Assert.Single(network.GetConnectedComponents());
    }
}
