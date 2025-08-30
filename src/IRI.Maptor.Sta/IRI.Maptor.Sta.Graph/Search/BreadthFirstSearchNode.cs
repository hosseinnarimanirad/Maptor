using System; 

namespace IRI.Maptor.Sta.Graph;

public class BreadthFirstSearchNode<TNode> : IComparable<BreadthFirstSearchNode<TNode>> 
{ 
    private BreadthFirstSearchNode<TNode> m_Predecessor;

    private NodeStatus m_Status;
     
    private double m_Value;

    public BreadthFirstSearchNode<TNode> Predecessor
    {
        get { return this.m_Predecessor; }

        set { this.m_Predecessor = value; }
    }

    public NodeStatus Status
    {
        get { return this.m_Status; }

        set { this.m_Status = value; }
    }

    public double Value
    {
        get { return this.m_Value; }
        set { this.m_Value = value; }
    }

    public BreadthFirstSearchNode(NodeStatus status, double value) : this(status, value, null) { }

    public BreadthFirstSearchNode(NodeStatus status, double value, BreadthFirstSearchNode<TNode> predecessor)
    {
        this.m_Predecessor = predecessor;

        this.m_Status = status;

        this.m_Value = value;
    }

    public override string ToString()
    {
        return string.Format("Status: {0}, Value: {1}", this.Status.ToString(), this.Value.ToString());
    }

    #region IComparable<NodeInfo<TNode,TValue>> Members

    public int CompareTo(BreadthFirstSearchNode<TNode> other)
    {
        return this.Value.CompareTo(other.Value);
    }

    #endregion
}
