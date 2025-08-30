
namespace IRI.Maptor.Sta.Spatial.Topology;


public enum PoinTriangleRelation
{
    In = -13,
    OnFirstEdge = -12,
    OnSecondEdge = -10,
    OnThirdEdge = -4,
    AlongFirstEdgeNegative = 6,
    AlongFirstEdgePositive = -6,
    AlongSecondEdgeNegative = -8,
    AlongSecondEdgePositive = 8,
    AlongThirdEdgeNegative = 2,
    AlongThirdEdgePositive = -2,
    RightOfFirstEdge = -11,
    RightOfSecondEdge = -7,
    RightOfThirdEdge = 5,
    BehindFirstVertex = 7,
    BehindSecondVertex = -5,
    BehindThirdVertex = 11,
}
