using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace IRI.Maptor.Infrastructure.PersonalGdb.Xml;


[Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[XmlType("GPCodedValueDomain2", Namespace = PersonalGdbInfrastructure.EsriSchemaNamespace)]
[XmlRoot("GPCodedValueDomain2")]
public class GdbXml_CodedValueDomain
{
    [XmlNamespaceDeclarations]
    public XmlSerializerNamespaces Xmlns { get; set; }

    [XmlAttribute("type", Namespace = System.Xml.Schema.XmlSchema.InstanceNamespace)]
    public string XsiType { get; set; } = "typens:GPCodedValueDomain2";

    public GdbXml_CodedValueDomain()
    {
        Xmlns = new XmlSerializerNamespaces();
        Xmlns.Add("xsi", System.Xml.Schema.XmlSchema.InstanceNamespace);
        Xmlns.Add("xs", System.Xml.Schema.XmlSchema.Namespace);
        Xmlns.Add("typens", PersonalGdbInfrastructure.EsriSchemaNamespace);
    }


    [XmlElement("DomainName", Namespace = "")]
    public string DomainName { get; set; }


    [XmlElement("FieldType", Namespace = "")]
    public string FieldType { get; set; }


    [XmlElement("MergePolicy", Namespace = "")]
    public string MergePolicy { get; set; }


    [XmlElement("SplitPolicy", Namespace = "")]
    public string SplitPolicy { get; set; }


    [XmlElement("Description", Namespace = "")]
    public string? Description { get; set; }


    [XmlElement("Owner", Namespace = "")]
    public string? Owner { get; set; }


    [XmlElement("CodedValues", Namespace = "")]
    public GdbXml_ArrayOfCodedValue CodedValues { get; set; }
}