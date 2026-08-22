using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Infrastructure.PersonalGdb.Model;


public class GdbCodedValueDomain
{
    public string DomainName { get; set; }

    public string FieldType { get; set; }

    public List<GdbCodedValue> Values { get; set; }

    public override string ToString()
    {
        return $"{DomainName} [{FieldType}] - ({Values.Count}) Values";
    }
}
