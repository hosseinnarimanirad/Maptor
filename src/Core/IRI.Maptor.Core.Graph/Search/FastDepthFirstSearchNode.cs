using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Graph;

class FastDepthFirstSearchNode<TNode>
{
    private FastDepthFirstSearchNode<TNode> m_Predecessor;

    private int? m_DiscoverTime;

    private int? m_FinishTime;

    private TNode m_Value;

    public int? DiscoverTime
    {
        get { return this.m_DiscoverTime; }
        set { this.m_DiscoverTime = value; }
    }

    public int? FinishTime
    {
        get { return this.m_FinishTime; }
        set { this.m_FinishTime = value; }
    }

    public FastDepthFirstSearchNode<TNode> Predecessor
    {
        get { return this.m_Predecessor; }

        set { this.m_Predecessor = value; }
    }

    public TNode Value
    {
        get { return this.m_Value; }
        set { this.m_Value = value; }
    }

    public FastDepthFirstSearchNode(TNode node) : this(node, null, null, null) { }

    public FastDepthFirstSearchNode(TNode node, int? discoverTime, int? finishTime) : this(node, discoverTime, finishTime, null) { }

    public FastDepthFirstSearchNode(TNode node, int? discoverTime, int? finishTime, FastDepthFirstSearchNode<TNode> predecessor)
    {
        this.m_Value = node;

        this.m_Predecessor = predecessor;

        this.m_DiscoverTime = discoverTime;

        this.m_FinishTime = finishTime;
    }

    public override string ToString()
    {
        return string.Format("D.T.: {0}, F.T.: {1}", this.DiscoverTime.ToString(), this.FinishTime.ToString());
    }

    internal bool IsWhite()
    {
        return !this.DiscoverTime.HasValue;
    }

    internal bool IsGray()
    {
        return !this.FinishTime.HasValue && this.DiscoverTime.HasValue;
    }

    internal bool IsBlack()
    {
        return this.FinishTime.HasValue && this.DiscoverTime.HasValue;
    }

}
