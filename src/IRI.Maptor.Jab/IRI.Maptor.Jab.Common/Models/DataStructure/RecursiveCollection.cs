using System.Linq; 
using System.Collections.Generic;

namespace IRI.Maptor.Jab.Common.Models.DataStructure;

public class RecursiveCollection<T>
{
    //equivalent to points
    public List<T>? Values { get; set; }

    // equivalent to geometries
    public List<RecursiveCollection<T>>? Collections { get; set; }

    public List<T> GetFlattenCollection()
    {
        if (this.Collections is null)
        {
            return this.Values;
        }
        else
        {
            return Collections.SelectMany(i => i.GetFlattenCollection()).ToList();
        }
    }
}
