using System.Linq; 
using System.Collections.Generic;

namespace IRI.Maptor.Jab.Common.Models.DataStructure;

public class RecursiveCollection<T>
{
    private List<T>? _values;

    public List<T>? Values
    {
        get { return _values; }
        set { _values = value; }
    }

    private List<RecursiveCollection<T>>? _collections;

    public List<RecursiveCollection<T>>? Collections
    {
        get { return _collections; }
        set { _collections = value; }
    }

    public List<T> GetFlattenCollection()
    {
        if (this.Collections is null)
        {
            return Values;
        }
        else
        {
            return Collections.SelectMany(i => i.GetFlattenCollection()).ToList();
        }
    }
}
