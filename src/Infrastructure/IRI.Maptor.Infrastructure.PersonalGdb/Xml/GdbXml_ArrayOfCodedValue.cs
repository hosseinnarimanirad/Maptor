using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace IRI.Maptor.Infrastructure.PersonalGdb.Xml;

[Serializable]
[XmlType("ArrayOfCodedValue", Namespace = PersonalGdbInfrastructure.EsriSchemaNamespace)]
public class GdbXml_ArrayOfCodedValue
{
    [XmlAttribute("type", Namespace = System.Xml.Schema.XmlSchema.InstanceNamespace)]
    public string XsiType { get; set; } = "typens:ArrayOfCodedValue";


    [XmlElement("CodedValue", Namespace = "")]
    public List<GdbXml_CodedValue>? Items { get; set; }
}