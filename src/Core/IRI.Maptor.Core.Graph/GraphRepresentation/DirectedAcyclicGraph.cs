using System;

namespace IRI.Maptor.Core.Graph;

public class DirectedAcyclicGraph<TNode, TWeight> : AdjacencyList<TNode, TWeight>
    where TWeight : IComparable
{
    public override string ToString()
    {
        return base.ToString();
    }
}
