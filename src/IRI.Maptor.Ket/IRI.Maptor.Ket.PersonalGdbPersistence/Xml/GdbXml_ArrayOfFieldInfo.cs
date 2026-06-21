using IRI.Maptor.Ket.PersonalGdbPersistence.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace IRI.Maptor.Ket.PersonalGdbPersistence.Xml;

[Serializable]
[XmlType("ArrayOfGPFieldInfoEx", Namespace = PersonalGdbInfrastructure.EsriSchemaNamespace)]
public class GdbXml_ArrayOfFieldInfo
{
    [XmlAttribute("type", Namespace = System.Xml.Schema.XmlSchema.InstanceNamespace)]
    public string XsiType { get; set; } = "typens:ArrayOfGPFieldInfoEx";

    public GdbXml_ArrayOfFieldInfo()
    {
        this.Items = [];
    }

    // Namespace = "" is so important, as in the xml there is no namespace before the element name
    // in order to add array as content use XmlElement instead of XmlArray
    [XmlElement("GPFieldInfoEx", Namespace = "")]
    public List<GdbXml_FieldInfo> Items { get; set; }
}