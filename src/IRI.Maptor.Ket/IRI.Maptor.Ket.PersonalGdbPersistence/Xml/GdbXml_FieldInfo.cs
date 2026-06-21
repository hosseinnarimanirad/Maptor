using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace IRI.Maptor.Ket.PersonalGdbPersistence.Xml;


[Serializable]
[XmlType("GPFieldInfoEx", Namespace = PersonalGdbInfrastructure.EsriSchemaNamespace)]
public class GdbXml_FieldInfo
{
    [XmlAttribute("type", Namespace = System.Xml.Schema.XmlSchema.InstanceNamespace)]
    public string XsiType { get; set; } = "typens:GPFieldInfoEx";


    [XmlElement("Name", Namespace = "")]
    public string Name { get; set; }


    [XmlElement("AliasName", Namespace = "")]
    public string AliasName { get; set; }


    [XmlElement("ModelName", Namespace = "")]
    public string ModelName { get; set; }


    [XmlElement("DomainName", Namespace = "")]
    public string DomainName { get; set; }


    [XmlElement("FieldType", Namespace = "")]
    public string FieldType { get; set; }


    [XmlElement("IsNullable", Namespace = "")]
    public bool IsNullable { get; set; }


    [XmlElement("Required", Namespace = "")]
    public bool Required { get; set; }


    [XmlIgnore()]
    public bool RequiredSpecified { get; set; }


    [XmlElement("Editable", Namespace = "")]
    public bool Editable { get; set; }


    [XmlElement("EditableSpecified", Namespace = "")]
    [XmlIgnore()]
    public bool EditableSpecified { get; set; }
}