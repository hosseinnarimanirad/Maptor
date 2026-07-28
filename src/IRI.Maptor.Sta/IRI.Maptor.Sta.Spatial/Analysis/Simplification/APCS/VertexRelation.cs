using IRI.Maptor.Sta.Common.Abstractions;

public class VertexRelation<T> where T : IPoint
{
    public List<int> ID { get; set; }
    public List<T> XY { get; set; }
    public List<int> Error { get; set; }
    public List<int> Parent { get; set; }
    public List<int> LC { get; set; }
    public List<int> RC { get; set; }
    public List<int> LS { get; set; }
    public List<int> RS { get; set; }
    public List<bool> Current { get; set; }

    public VertexRelation()
    {
        ID = new List<int>();
        XY = new List<T>();
        Error = new List<int>();
        Parent = new List<int>();
        LC = new List<int>();
        RC = new List<int>();
        LS = new List<int>();
        RS = new List<int>();
        Current = new List<bool>();
    }
}
