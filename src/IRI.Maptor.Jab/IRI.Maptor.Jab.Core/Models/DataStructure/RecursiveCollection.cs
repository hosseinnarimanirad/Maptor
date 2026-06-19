
namespace IRI.Maptor.Jab.Core.Models;

public class RecursiveCollection<T>
{
    //equivalent to points
    public List<T>? Values { get; set; }

    // equivalent to geometries
    public List<RecursiveCollection<T>>? Collections { get; set; }

    public List<T> GetFlattenCollection()
    {
        if (Collections is null)
        {
            return Values;
        }
        else
        {
            return Collections.SelectMany(i => i.GetFlattenCollection()).ToList();
        }
    }
}
