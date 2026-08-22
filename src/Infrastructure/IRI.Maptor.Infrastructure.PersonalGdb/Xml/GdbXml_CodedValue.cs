using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace IRI.Maptor.Infrastructure.PersonalGdb.Xml;

[Serializable]
[XmlType("CodedValue", Namespace = PersonalGdbInfrastructure.EsriSchemaNamespace)]
public class GdbXml_CodedValue
{
    [XmlAttribute("type", Namespace = System.Xml.Schema.XmlSchema.InstanceNamespace)]
    public string XsiType { get; set; } = "typens:CodedValue";


    [XmlElement("Code", Namespace = "")]
    public byte Code { get; set; }


    [XmlElement("Name", Namespace = "")]
    public string Name { get; set; }


    public override string ToString() => $"Code:{Code}, Name:{Name}";
}
