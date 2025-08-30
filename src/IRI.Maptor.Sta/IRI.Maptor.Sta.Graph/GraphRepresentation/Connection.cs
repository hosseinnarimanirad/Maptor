// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ
 
namespace IRI.Maptor.Sta.Graph;

public struct Connection<TNode, TWeight>
{
    private TNode m_Node;

    private TWeight m_Weight;

    public TNode Node
    {
        get { return this.m_Node; }

        set { m_Node = value; }
    }

    public TWeight Weight
    {
        get { return this.m_Weight; }

        set { this.m_Weight = value; }
    }

    public Connection(TNode node, TWeight weight)
    {
        this.m_Node = node;

        this.m_Weight = weight;
    }

    public override string ToString()
    {
        return Node.ToString();
    }
}
