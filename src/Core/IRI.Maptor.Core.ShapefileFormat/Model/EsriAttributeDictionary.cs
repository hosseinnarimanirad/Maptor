using IRI.Maptor.Core.ShapefileFormat.Dbf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.ShapefileFormat.Model;

public class EsriAttributeDictionary
{
    public List<Dictionary<string, object>> Attributes { get; set; }

    public List<DbfFieldDescriptor> Fields { get; set; }

    public EsriAttributeDictionary()
    {

    }

    public EsriAttributeDictionary(List<Dictionary<string, object>> attributes, List<DbfFieldDescriptor> fields)
    {
        // deleted records may be kept as null entries (DbfFile.Read with skipDeletedRecords: false)
        var first = attributes.FirstOrDefault(a => a != null);

        if (first != null && first.Count != fields.Count)
        {
            throw new NotImplementedException("EsriAttributeDictionary > constructor");
        }

        this.Attributes = attributes;

        this.Fields = fields;
    }

    //public List<ObjectToDbfTypeMap<T>>

}
