// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ


namespace IRI.Maptor.Sta.Graph;

public struct Edge<TNode, TWeight>
{
    private TNode m_node;

    private Connection<TNode, TWeight> m_connection;

    public TNode Node
    {
        get { return this.m_node; }
    }

    public Connection<TNode, TWeight> Connection
    {
        get { return this.m_connection; }
    }

    public Edge(TNode node, Connection<TNode, TWeight> connection)
    {
        this.m_node = node;

        this.m_connection = connection;
    }

    public override string ToString()
    {
        return string.Format("({0})-{1}-({2})", Node.ToString(), Connection.Weight.ToString(), Connection.Node.ToString());
    } 
}
