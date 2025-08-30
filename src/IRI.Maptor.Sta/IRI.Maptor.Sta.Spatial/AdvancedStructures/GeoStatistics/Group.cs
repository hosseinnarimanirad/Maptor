namespace IRI.Maptor.Sta.Spatial.AdvancedStructures;

public class Group<T> 
{
    public T Center { get; set; }

    public int Frequency { get { return this.Members.Count; } }

    public Group(T center)
    {
        this.Members = new HashSet<T>();

        this.Center = center;

        this.Members.Add(center);
    }

    public bool DoseBelongTo(T item, Func<T, T, bool> groupLogic)
    {
        return groupLogic(this.Center, item);
    }

    public void Add(T item)
    {
        this.Members.Add(item);
    }

    private HashSet<T> _members;

    public HashSet<T> Members
    {
        get { return this._members; }
        private set { this._members = value; }
    }

     
}
