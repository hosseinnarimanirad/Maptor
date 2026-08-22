using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace IRI.Maptor.Infrastructure.PersonalGdb.Xml;


[Serializable]
[XmlType("DEFeatureClassInfo", Namespace = PersonalGdbInfrastructure.EsriSchemaNamespace)]
[XmlRoot("DEFeatureClassInfo", IsNullable = false)]
public class GdbXml_FeatureClass
{
    [XmlNamespaceDeclarations]
    public XmlSerializerNamespaces Xmlns { get; set; }

    //[XmlAttribute("type", Namespace = System.Xml.Schema.XmlSchema.InstanceNamespace)]
    [XmlAttribute("type", Namespace = System.Xml.Schema.XmlSchema.InstanceNamespace)]
    public string XsiType { get; set; } = "typens:DEFeatureClassInfo";

    public GdbXml_FeatureClass()
    {
        Xmlns = new XmlSerializerNamespaces();
        Xmlns.Add("xs", System.Xml.Schema.XmlSchema.Namespace);
        Xmlns.Add("xsi", System.Xml.Schema.XmlSchema.InstanceNamespace);
        Xmlns.Add("typens", PersonalGdbInfrastructure.EsriSchemaNamespace);
        Xmlns.Add("", "");
    }

    #region Properties

    [XmlElement("CatalogPath", Namespace = "")]
    public string CatalogPath { get; set; }


    [XmlElement("Name", Namespace = "")]
    public string Name { get; set; }


    [XmlElement("ChildrenExpanded", Namespace = "")]
    public bool ChildrenExpanded { get; set; }


    [XmlElement("DatasetType", Namespace = "")]
    public string DatasetType { get; set; }


    [XmlElement("DSID", Namespace = "")]
    public ushort DSID { get; set; }


    [XmlElement("Versioned", Namespace = "")]
    public bool Versioned { get; set; }


    [XmlElement("CanVersion", Namespace = "")]
    public bool CanVersion { get; set; }


    [XmlElement("RequiredGeodatabaseClientVersion", Namespace = "")]
    public decimal RequiredGeodatabaseClientVersion { get; set; }


    [XmlElement("HasOID", Namespace = "")]
    public bool HasOID { get; set; }


    [XmlElement("OIDFieldName", Namespace = "")]
    public string OIDFieldName { get; set; }


    [XmlElement("CLSID", Namespace = "")]
    public string CLSID { get; set; }


    [XmlElement("AliasName", Namespace = "")]
    public string AliasName { get; set; }


    [XmlElement("HasGlobalID", Namespace = "")]
    public bool HasGlobalID { get; set; }


    [XmlElement("EditorTrackingEnabled", Namespace = "")]
    public bool EditorTrackingEnabled { get; set; }


    [XmlElement("IsTimeInUTC", Namespace = "")]
    public bool IsTimeInUTC { get; set; }


    [XmlElement("FeatureType", Namespace = "")]
    public string FeatureType { get; set; }


    [XmlElement("ShapeType", Namespace = "")]
    public string ShapeType { get; set; }


    [XmlElement("ShapeFieldName", Namespace = "")]
    public string ShapeFieldName { get; set; }


    [XmlElement("HasM", Namespace = "")]
    public bool HasM { get; set; }


    [XmlElement("HasZ", Namespace = "")]
    public bool HasZ { get; set; }


    [XmlElement("HasSpatialIndex", Namespace = "")]
    public bool HasSpatialIndex { get; set; }


    [XmlElement("LengthFieldName", Namespace = "")]
    public string LengthFieldName { get; set; }


    [XmlElement("ChangeTracked", Namespace = "")]
    public bool ChangeTracked { get; set; }


    [XmlElement("FieldFilteringEnabled", Namespace = "")]
    public bool FieldFilteringEnabled { get; set; }

    #endregion


    // Namespace = "" is so important, as in the xml there is no namespace before the element name
    [XmlElement("GPFieldInfoExs", Namespace = "")]
    public GdbXml_ArrayOfFieldInfo GPFieldInfoExs { get; set; }


    //[XmlElement("Extent", Namespace = "")]
    //public DEFeatureClassInfoExtent Extent
    //{
    //    get
    //    {
    //        return this.extentField;
    //    }
    //    set
    //    {
    //        this.extentField = value;
    //    }
    //}


    //[XmlElement("SpatialReference", Namespace = "")]
    //public DEFeatureClassInfoSpatialReference SpatialReference
    //{
    //    get
    //    {
    //        return this.spatialReferenceField;
    //    }
    //    set
    //    {
    //        this.spatialReferenceField = value;
    //    }
    //}

}
