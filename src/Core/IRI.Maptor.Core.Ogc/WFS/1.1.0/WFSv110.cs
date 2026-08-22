using System.Collections.Generic;

namespace IRI.Maptor.Core.Ogc.WFS.v110;

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class XlinkPropertyName
{

    #region Private fields
    private string _traverseXlinkDepth;

    private string _traverseXlinkExpiry;

    private string _value;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string traverseXlinkDepth
    {
        get
        {
            return this._traverseXlinkDepth;
        }
        set
        {
            this._traverseXlinkDepth = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string traverseXlinkExpiry
    {
        get
        {
            return this._traverseXlinkExpiry;
        }
        set
        {
            this._traverseXlinkExpiry = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public string Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(TypeName = "GetCapabilitiesType", Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("GetCapabilities", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class GetCapabilitiesType1 : GetCapabilitiesType
{

    #region Private fields
    private string _service;
    #endregion

    public GetCapabilitiesType1()
    {
        this._service = "WFS";
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute("WFS")]
    public string service
    {
        get
        {
            return this._service;
        }
        set
        {
            this._service = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(GetCapabilitiesType1))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class GetCapabilitiesType
{

    #region Private fields
    private List<string> _acceptVersions;

    private List<string> _sections;

    private List<string> _acceptFormats;

    private string _updateSequence;
    #endregion

    public GetCapabilitiesType()
    {
        this._acceptFormats = new List<string>();
        this._sections = new List<string>();
        this._acceptVersions = new List<string>();
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("Version", IsNullable = false)]
    public List<string> AcceptVersions
    {
        get
        {
            return this._acceptVersions;
        }
        set
        {
            this._acceptVersions = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("Section", IsNullable = false)]
    public List<string> Sections
    {
        get
        {
            return this._sections;
        }
        set
        {
            this._sections = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("OutputFormat", IsNullable = false)]
    public List<string> AcceptFormats
    {
        get
        {
            return this._acceptFormats;
        }
        set
        {
            this._acceptFormats = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string updateSequence
    {
        get
        {
            return this._updateSequence;
        }
        set
        {
            this._updateSequence = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("WFS_Capabilities", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class WFS_CapabilitiesType : CapabilitiesBaseType
{

    #region Private fields
    private FeatureTypeListType _featureTypeList;

    private List<GMLObjectTypeType> _servesGMLObjectTypeList;

    private List<GMLObjectTypeType> _supportsGMLObjectTypeList;

    private Filter_Capabilities _filter_Capabilities;
    #endregion

    public WFS_CapabilitiesType()
    {
        this._filter_Capabilities = new Filter_Capabilities();
        this._supportsGMLObjectTypeList = new List<GMLObjectTypeType>();
        this._servesGMLObjectTypeList = new List<GMLObjectTypeType>();
        this._featureTypeList = new FeatureTypeListType();
    }

    public FeatureTypeListType FeatureTypeList
    {
        get
        {
            return this._featureTypeList;
        }
        set
        {
            this._featureTypeList = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("GMLObjectType", IsNullable = false)]
    public List<GMLObjectTypeType> ServesGMLObjectTypeList
    {
        get
        {
            return this._servesGMLObjectTypeList;
        }
        set
        {
            this._servesGMLObjectTypeList = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("GMLObjectType", IsNullable = false)]
    public List<GMLObjectTypeType> SupportsGMLObjectTypeList
    {
        get
        {
            return this._supportsGMLObjectTypeList;
        }
        set
        {
            this._supportsGMLObjectTypeList = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://www.opengis.net/ogc")]
    public Filter_Capabilities Filter_Capabilities
    {
        get
        {
            return this._filter_Capabilities;
        }
        set
        {
            this._filter_Capabilities = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("FeatureTypeList", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class FeatureTypeListType
{

    #region Private fields
    private List<OperationType> _operations;

    private List<FeatureTypeType> _featureType;
    #endregion

    public FeatureTypeListType()
    {
        this._featureType = new List<FeatureTypeType>();
        this._operations = new List<OperationType>();
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("Operation", IsNullable = false)]
    public List<OperationType> Operations
    {
        get
        {
            return this._operations;
        }
        set
        {
            this._operations = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("FeatureType")]
    public List<FeatureTypeType> FeatureType
    {
        get
        {
            return this._featureType;
        }
        set
        {
            this._featureType = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
public enum OperationType
{

    /// <remarks/>
    Insert,

    /// <remarks/>
    Update,

    /// <remarks/>
    Delete,

    /// <remarks/>
    Query,

    /// <remarks/>
    Lock,

    /// <remarks/>
    GetGmlObject,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
public partial class FeatureTypeType
{

    #region Private fields
    private System.Xml.XmlQualifiedName _name;

    private string _title;

    private string _abstract;

    private List<KeywordsType> _keywords;

    private object[] _items;

    private ItemsChoiceType13[] _itemsElementName;

    private List<OperationType> _operations;

    private OutputFormatListType _outputFormats;

    private List<WGS84BoundingBoxType> _wGS84BoundingBox;

    private List<MetadataURLType> _metadataURL;
    #endregion

    public FeatureTypeType()
    {
        this._metadataURL = new List<MetadataURLType>();
        this._wGS84BoundingBox = new List<WGS84BoundingBoxType>();
        this._outputFormats = new OutputFormatListType();
        this._operations = new List<OperationType>();
        this._keywords = new List<KeywordsType>();
    }

    public System.Xml.XmlQualifiedName Name
    {
        get
        {
            return this._name;
        }
        set
        {
            this._name = value;
        }
    }

    public string Title
    {
        get
        {
            return this._title;
        }
        set
        {
            this._title = value;
        }
    }

    public string Abstract
    {
        get
        {
            return this._abstract;
        }
        set
        {
            this._abstract = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Keywords", Namespace = "http://www.opengis.net/ows")]
    public List<KeywordsType> Keywords
    {
        get
        {
            return this._keywords;
        }
        set
        {
            this._keywords = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("DefaultSRS", typeof(string), DataType = "anyURI")]
    [System.Xml.Serialization.XmlElementAttribute("NoSRS", typeof(FeatureTypeTypeNoSRS))]
    [System.Xml.Serialization.XmlElementAttribute("OtherSRS", typeof(string), DataType = "anyURI")]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public object[] Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType13[] ItemsElementName
    {
        get
        {
            return this._itemsElementName;
        }
        set
        {
            this._itemsElementName = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("Operation", IsNullable = false)]
    public List<OperationType> Operations
    {
        get
        {
            return this._operations;
        }
        set
        {
            this._operations = value;
        }
    }

    public OutputFormatListType OutputFormats
    {
        get
        {
            return this._outputFormats;
        }
        set
        {
            this._outputFormats = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("WGS84BoundingBox", Namespace = "http://www.opengis.net/ows")]
    public List<WGS84BoundingBoxType> WGS84BoundingBox
    {
        get
        {
            return this._wGS84BoundingBox;
        }
        set
        {
            this._wGS84BoundingBox = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("MetadataURL")]
    public List<MetadataURLType> MetadataURL
    {
        get
        {
            return this._metadataURL;
        }
        set
        {
            this._metadataURL = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class KeywordsType
{

    #region Private fields
    private List<string> _keyword;

    private CodeType _type;
    #endregion

    public KeywordsType()
    {
        this._type = new CodeType();
        this._keyword = new List<string>();
    }

    [System.Xml.Serialization.XmlElementAttribute("Keyword")]
    public List<string> Keyword
    {
        get
        {
            return this._keyword;
        }
        set
        {
            this._keyword = value;
        }
    }

    public CodeType Type
    {
        get
        {
            return this._type;
        }
        set
        {
            this._type = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class CodeType
{

    #region Private fields
    private string _codeSpace;

    private string _value;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string codeSpace
    {
        get
        {
            return this._codeSpace;
        }
        set
        {
            this._codeSpace = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public string Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/wfs")]
public partial class FeatureTypeTypeNoSRS
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs", IncludeInSchema = false)]
public enum ItemsChoiceType13
{

    /// <remarks/>
    DefaultSRS,

    /// <remarks/>
    NoSRS,

    /// <remarks/>
    OtherSRS,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
public partial class OutputFormatListType
{

    #region Private fields
    private List<string> _format;
    #endregion

    public OutputFormatListType()
    {
        this._format = new List<string>();
    }

    [System.Xml.Serialization.XmlElementAttribute("Format")]
    public List<string> Format
    {
        get
        {
            return this._format;
        }
        set
        {
            this._format = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class WGS84BoundingBoxType : BoundingBoxType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(WGS84BoundingBoxType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class BoundingBoxType
{

    #region Private fields
    private string _lowerCorner;

    private string _upperCorner;

    private string _crs;

    private string _dimensions;
    #endregion

    public string LowerCorner
    {
        get
        {
            return this._lowerCorner;
        }
        set
        {
            this._lowerCorner = value;
        }
    }

    public string UpperCorner
    {
        get
        {
            return this._upperCorner;
        }
        set
        {
            this._upperCorner = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string crs
    {
        get
        {
            return this._crs;
        }
        set
        {
            this._crs = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string dimensions
    {
        get
        {
            return this._dimensions;
        }
        set
        {
            this._dimensions = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
public partial class MetadataURLType
{

    #region Private fields
    private MetadataURLTypeType _type;

    private MetadataURLTypeFormat _format;

    private string _value;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public MetadataURLTypeType type
    {
        get
        {
            return this._type;
        }
        set
        {
            this._type = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public MetadataURLTypeFormat format
    {
        get
        {
            return this._format;
        }
        set
        {
            this._format = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public string Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/wfs")]
public enum MetadataURLTypeType
{

    /// <remarks/>
    TC211,

    /// <remarks/>
    FGDC,

    /// <remarks/>
    [System.Xml.Serialization.XmlEnumAttribute("19115")]
    Item19115,

    /// <remarks/>
    [System.Xml.Serialization.XmlEnumAttribute("19139")]
    Item19139,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/wfs")]
public enum MetadataURLTypeFormat
{

    /// <remarks/>
    [System.Xml.Serialization.XmlEnumAttribute("text/xml")]
    textxml,

    /// <remarks/>
    [System.Xml.Serialization.XmlEnumAttribute("text/html")]
    texthtml,

    /// <remarks/>
    [System.Xml.Serialization.XmlEnumAttribute("text/sgml")]
    textsgml,

    /// <remarks/>
    [System.Xml.Serialization.XmlEnumAttribute("text/plain")]
    textplain,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
public partial class GMLObjectTypeType
{

    #region Private fields
    private System.Xml.XmlQualifiedName _name;

    private string _title;

    private string _abstract;

    private List<KeywordsType> _keywords;

    private OutputFormatListType _outputFormats;
    #endregion

    public GMLObjectTypeType()
    {
        this._outputFormats = new OutputFormatListType();
        this._keywords = new List<KeywordsType>();
    }

    public System.Xml.XmlQualifiedName Name
    {
        get
        {
            return this._name;
        }
        set
        {
            this._name = value;
        }
    }

    public string Title
    {
        get
        {
            return this._title;
        }
        set
        {
            this._title = value;
        }
    }

    public string Abstract
    {
        get
        {
            return this._abstract;
        }
        set
        {
            this._abstract = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Keywords", Namespace = "http://www.opengis.net/ows")]
    public List<KeywordsType> Keywords
    {
        get
        {
            return this._keywords;
        }
        set
        {
            this._keywords = value;
        }
    }

    public OutputFormatListType OutputFormats
    {
        get
        {
            return this._outputFormats;
        }
        set
        {
            this._outputFormats = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/ogc")]
public partial class Filter_Capabilities
{

    #region Private fields
    private Spatial_CapabilitiesType _spatial_Capabilities;

    private Scalar_CapabilitiesType _scalar_Capabilities;

    private object[] _id_Capabilities;
    #endregion

    public Filter_Capabilities()
    {
        this._scalar_Capabilities = new Scalar_CapabilitiesType();
        this._spatial_Capabilities = new Spatial_CapabilitiesType();
    }

    public Spatial_CapabilitiesType Spatial_Capabilities
    {
        get
        {
            return this._spatial_Capabilities;
        }
        set
        {
            this._spatial_Capabilities = value;
        }
    }

    public Scalar_CapabilitiesType Scalar_Capabilities
    {
        get
        {
            return this._scalar_Capabilities;
        }
        set
        {
            this._scalar_Capabilities = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("EID", typeof(EID), IsNullable = false)]
    [System.Xml.Serialization.XmlArrayItemAttribute("FID", typeof(FID), IsNullable = false)]
    public object[] Id_Capabilities
    {
        get
        {
            return this._id_Capabilities;
        }
        set
        {
            this._id_Capabilities = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class Spatial_CapabilitiesType
{

    #region Private fields
    private List<System.Xml.XmlQualifiedName> _geometryOperands;

    private List<SpatialOperatorType> _spatialOperators;
    #endregion

    public Spatial_CapabilitiesType()
    {
        this._spatialOperators = new List<SpatialOperatorType>();
        this._geometryOperands = new List<System.Xml.XmlQualifiedName>();
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("GeometryOperand", IsNullable = false)]
    public List<System.Xml.XmlQualifiedName> GeometryOperands
    {
        get
        {
            return this._geometryOperands;
        }
        set
        {
            this._geometryOperands = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("SpatialOperator", IsNullable = false)]
    public List<SpatialOperatorType> SpatialOperators
    {
        get
        {
            return this._spatialOperators;
        }
        set
        {
            this._spatialOperators = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class SpatialOperatorType
{

    #region Private fields
    private List<System.Xml.XmlQualifiedName> _geometryOperands;

    private SpatialOperatorNameType _name;
    #endregion

    public SpatialOperatorType()
    {
        this._geometryOperands = new List<System.Xml.XmlQualifiedName>();
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("GeometryOperand", IsNullable = false)]
    public List<System.Xml.XmlQualifiedName> GeometryOperands
    {
        get
        {
            return this._geometryOperands;
        }
        set
        {
            this._geometryOperands = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public SpatialOperatorNameType name
    {
        get
        {
            return this._name;
        }
        set
        {
            this._name = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public enum SpatialOperatorNameType
{

    /// <remarks/>
    BBOX,

    /// <remarks/>
    Equals,

    /// <remarks/>
    Disjoint,

    /// <remarks/>
    Intersects,

    /// <remarks/>
    Touches,

    /// <remarks/>
    Crosses,

    /// <remarks/>
    Within,

    /// <remarks/>
    Contains,

    /// <remarks/>
    Overlaps,

    /// <remarks/>
    Beyond,

    /// <remarks/>
    DWithin,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class Scalar_CapabilitiesType
{

    #region Private fields
    private LogicalOperators _logicalOperators;

    private ComparisonOperatorsType _comparisonOperators;

    private object[] _arithmeticOperators;
    #endregion

    public Scalar_CapabilitiesType()
    {
        this._comparisonOperators = new ComparisonOperatorsType();
        this._logicalOperators = new LogicalOperators();
    }

    public LogicalOperators LogicalOperators
    {
        get
        {
            return this._logicalOperators;
        }
        set
        {
            this._logicalOperators = value;
        }
    }

    public ComparisonOperatorsType ComparisonOperators
    {
        get
        {
            return this._comparisonOperators;
        }
        set
        {
            this._comparisonOperators = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("Functions", typeof(FunctionsType), IsNullable = false)]
    [System.Xml.Serialization.XmlArrayItemAttribute("SimpleArithmetic", typeof(SimpleArithmetic), IsNullable = false)]
    public object[] ArithmeticOperators
    {
        get
        {
            return this._arithmeticOperators;
        }
        set
        {
            this._arithmeticOperators = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/ogc")]
public partial class LogicalOperators
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class ComparisonOperatorsType
{

    #region Private fields
    private List<ComparisonOperatorType> _comparisonOperator;
    #endregion

    public ComparisonOperatorsType()
    {
        this._comparisonOperator = new List<ComparisonOperatorType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("ComparisonOperator")]
    public List<ComparisonOperatorType> ComparisonOperator
    {
        get
        {
            return this._comparisonOperator;
        }
        set
        {
            this._comparisonOperator = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public enum ComparisonOperatorType
{

    /// <remarks/>
    LessThan,

    /// <remarks/>
    GreaterThan,

    /// <remarks/>
    LessThanEqualTo,

    /// <remarks/>
    GreaterThanEqualTo,

    /// <remarks/>
    EqualTo,

    /// <remarks/>
    NotEqualTo,

    /// <remarks/>
    Like,

    /// <remarks/>
    Between,

    /// <remarks/>
    NullCheck,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class FunctionsType
{

    #region Private fields
    private FunctionNamesType _functionNames;
    #endregion

    public FunctionsType()
    {
        this._functionNames = new FunctionNamesType();
    }

    public FunctionNamesType FunctionNames
    {
        get
        {
            return this._functionNames;
        }
        set
        {
            this._functionNames = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class FunctionNamesType
{

    #region Private fields
    private List<FunctionNameType> _functionName;
    #endregion

    public FunctionNamesType()
    {
        this._functionName = new List<FunctionNameType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("FunctionName")]
    public List<FunctionNameType> FunctionName
    {
        get
        {
            return this._functionName;
        }
        set
        {
            this._functionName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class FunctionNameType
{

    #region Private fields
    private string _nArgs;

    private string _value;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string nArgs
    {
        get
        {
            return this._nArgs;
        }
        set
        {
            this._nArgs = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public string Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/ogc")]
public partial class SimpleArithmetic
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/ogc")]
public partial class EID
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/ogc")]
public partial class FID
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(WFS_CapabilitiesType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class CapabilitiesBaseType
{

    #region Private fields
    private ServiceIdentification _serviceIdentification;

    private ServiceProvider _serviceProvider;

    private OperationsMetadata _operationsMetadata;

    private string _version;

    private string _updateSequence;
    #endregion

    public CapabilitiesBaseType()
    {
        this._operationsMetadata = new OperationsMetadata();
        this._serviceProvider = new ServiceProvider();
        this._serviceIdentification = new ServiceIdentification();
    }

    public ServiceIdentification ServiceIdentification
    {
        get
        {
            return this._serviceIdentification;
        }
        set
        {
            this._serviceIdentification = value;
        }
    }

    public ServiceProvider ServiceProvider
    {
        get
        {
            return this._serviceProvider;
        }
        set
        {
            this._serviceProvider = value;
        }
    }

    public OperationsMetadata OperationsMetadata
    {
        get
        {
            return this._operationsMetadata;
        }
        set
        {
            this._operationsMetadata = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string version
    {
        get
        {
            return this._version;
        }
        set
        {
            this._version = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string updateSequence
    {
        get
        {
            return this._updateSequence;
        }
        set
        {
            this._updateSequence = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/ows")]
public partial class ServiceIdentification : DescriptionType
{

    #region Private fields
    private CodeType _serviceType;

    private List<string> _serviceTypeVersion;

    private string _fees;

    private List<string> _accessConstraints;
    #endregion

    public ServiceIdentification()
    {
        this._accessConstraints = new List<string>();
        this._serviceTypeVersion = new List<string>();
        this._serviceType = new CodeType();
    }

    public CodeType ServiceType
    {
        get
        {
            return this._serviceType;
        }
        set
        {
            this._serviceType = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ServiceTypeVersion")]
    public List<string> ServiceTypeVersion
    {
        get
        {
            return this._serviceTypeVersion;
        }
        set
        {
            this._serviceTypeVersion = value;
        }
    }

    public string Fees
    {
        get
        {
            return this._fees;
        }
        set
        {
            this._fees = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("AccessConstraints")]
    public List<string> AccessConstraints
    {
        get
        {
            return this._accessConstraints;
        }
        set
        {
            this._accessConstraints = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(IdentificationType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class DescriptionType
{

    #region Private fields
    private string _title;

    private string _abstract;

    private List<KeywordsType> _keywords;
    #endregion

    public DescriptionType()
    {
        this._keywords = new List<KeywordsType>();
    }

    public string Title
    {
        get
        {
            return this._title;
        }
        set
        {
            this._title = value;
        }
    }

    public string Abstract
    {
        get
        {
            return this._abstract;
        }
        set
        {
            this._abstract = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Keywords")]
    public List<KeywordsType> Keywords
    {
        get
        {
            return this._keywords;
        }
        set
        {
            this._keywords = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class IdentificationType : DescriptionType
{

    #region Private fields
    private CodeType _identifier;

    private List<BoundingBoxType> _boundingBox;

    private List<string> _outputFormat;

    private List<string> _availableCRS;

    private List<MetadataType> _metadata;
    #endregion

    public IdentificationType()
    {
        this._metadata = new List<MetadataType>();
        this._availableCRS = new List<string>();
        this._outputFormat = new List<string>();
        this._boundingBox = new List<BoundingBoxType>();
        this._identifier = new CodeType();
    }

    public CodeType Identifier
    {
        get
        {
            return this._identifier;
        }
        set
        {
            this._identifier = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("BoundingBox")]
    public List<BoundingBoxType> BoundingBox
    {
        get
        {
            return this._boundingBox;
        }
        set
        {
            this._boundingBox = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("OutputFormat")]
    public List<string> OutputFormat
    {
        get
        {
            return this._outputFormat;
        }
        set
        {
            this._outputFormat = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("AvailableCRS", DataType = "anyURI")]
    public List<string> AvailableCRS
    {
        get
        {
            return this._availableCRS;
        }
        set
        {
            this._availableCRS = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Metadata")]
    public List<MetadataType> Metadata
    {
        get
        {
            return this._metadata;
        }
        set
        {
            this._metadata = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class MetadataType
{

    #region Private fields
    private object _abstractMetaData;

    private typeType _type;

    private string _href;

    private string _role;

    private string _arcrole;

    private string _title;

    private showType _show;

    private actuateType _actuate;

    private string _about;
    #endregion

    public MetadataType()
    {
        this._type = typeType.simple;
    }

    public object AbstractMetaData
    {
        get
        {
            return this._abstractMetaData;
        }
        set
        {
            this._abstractMetaData = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
    public typeType type
    {
        get
        {
            return this._type;
        }
        set
        {
            this._type = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink", DataType = "anyURI")]
    public string href
    {
        get
        {
            return this._href;
        }
        set
        {
            this._href = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink", DataType = "anyURI")]
    public string role
    {
        get
        {
            return this._role;
        }
        set
        {
            this._role = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink", DataType = "anyURI")]
    public string arcrole
    {
        get
        {
            return this._arcrole;
        }
        set
        {
            this._arcrole = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
    public string title
    {
        get
        {
            return this._title;
        }
        set
        {
            this._title = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
    public showType show
    {
        get
        {
            return this._show;
        }
        set
        {
            this._show = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
    public actuateType actuate
    {
        get
        {
            return this._actuate;
        }
        set
        {
            this._actuate = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string about
    {
        get
        {
            return this._about;
        }
        set
        {
            this._about = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/1999/xlink")]
public enum typeType
{

    /// <remarks/>
    simple,

    /// <remarks/>
    extended,

    /// <remarks/>
    title,

    /// <remarks/>
    resource,

    /// <remarks/>
    locator,

    /// <remarks/>
    arc,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/1999/xlink")]
public enum showType
{

    /// <remarks/>
    @new,

    /// <remarks/>
    replace,

    /// <remarks/>
    embed,

    /// <remarks/>
    other,

    /// <remarks/>
    none,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/1999/xlink")]
public enum actuateType
{

    /// <remarks/>
    onLoad,

    /// <remarks/>
    onRequest,

    /// <remarks/>
    other,

    /// <remarks/>
    none,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/ows")]
public partial class ServiceProvider
{

    #region Private fields
    private string _providerName;

    private OnlineResourceType _providerSite;

    private ResponsiblePartySubsetType _serviceContact;
    #endregion

    public ServiceProvider()
    {
        this._serviceContact = new ResponsiblePartySubsetType();
        this._providerSite = new OnlineResourceType();
    }

    public string ProviderName
    {
        get
        {
            return this._providerName;
        }
        set
        {
            this._providerName = value;
        }
    }

    public OnlineResourceType ProviderSite
    {
        get
        {
            return this._providerSite;
        }
        set
        {
            this._providerSite = value;
        }
    }

    public ResponsiblePartySubsetType ServiceContact
    {
        get
        {
            return this._serviceContact;
        }
        set
        {
            this._serviceContact = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(RequestMethodType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class OnlineResourceType
{

    #region Private fields
    private typeType _type;

    private string _href;

    private string _role;

    private string _arcrole;

    private string _title;

    private showType _show;

    private actuateType _actuate;
    #endregion

    public OnlineResourceType()
    {
        this._type = typeType.simple;
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
    public typeType type
    {
        get
        {
            return this._type;
        }
        set
        {
            this._type = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink", DataType = "anyURI")]
    public string href
    {
        get
        {
            return this._href;
        }
        set
        {
            this._href = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink", DataType = "anyURI")]
    public string role
    {
        get
        {
            return this._role;
        }
        set
        {
            this._role = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink", DataType = "anyURI")]
    public string arcrole
    {
        get
        {
            return this._arcrole;
        }
        set
        {
            this._arcrole = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
    public string title
    {
        get
        {
            return this._title;
        }
        set
        {
            this._title = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
    public showType show
    {
        get
        {
            return this._show;
        }
        set
        {
            this._show = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
    public actuateType actuate
    {
        get
        {
            return this._actuate;
        }
        set
        {
            this._actuate = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class RequestMethodType : OnlineResourceType
{

    #region Private fields
    private List<DomainType> _constraint;
    #endregion

    public RequestMethodType()
    {
        this._constraint = new List<DomainType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("Constraint")]
    public List<DomainType> Constraint
    {
        get
        {
            return this._constraint;
        }
        set
        {
            this._constraint = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class DomainType
{

    #region Private fields
    private List<string> _value;

    private List<MetadataType> _metadata;

    private string _name;
    #endregion

    public DomainType()
    {
        this._metadata = new List<MetadataType>();
        this._value = new List<string>();
    }

    //96.05.27 NARIMANIRAD
    public DomainType(string name, List<string> values)
    {
        this._name = name;

        this._metadata = new List<MetadataType>();

        this._value = values;
    }

    [System.Xml.Serialization.XmlElementAttribute("Value")]
    public List<string> Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Metadata")]
    public List<MetadataType> Metadata
    {
        get
        {
            return this._metadata;
        }
        set
        {
            this._metadata = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string name
    {
        get
        {
            return this._name;
        }
        set
        {
            this._name = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class ResponsiblePartySubsetType
{

    #region Private fields
    private string _individualName;

    private string _positionName;

    private ContactType _contactInfo;

    private CodeType _role;
    #endregion

    public ResponsiblePartySubsetType()
    {
        this._role = new CodeType();
        this._contactInfo = new ContactType();
    }

    public string IndividualName
    {
        get
        {
            return this._individualName;
        }
        set
        {
            this._individualName = value;
        }
    }

    public string PositionName
    {
        get
        {
            return this._positionName;
        }
        set
        {
            this._positionName = value;
        }
    }

    public ContactType ContactInfo
    {
        get
        {
            return this._contactInfo;
        }
        set
        {
            this._contactInfo = value;
        }
    }

    public CodeType Role
    {
        get
        {
            return this._role;
        }
        set
        {
            this._role = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class ContactType
{

    #region Private fields
    private TelephoneType _phone;

    private AddressType _address;

    private OnlineResourceType _onlineResource;

    private string _hoursOfService;

    private string _contactInstructions;
    #endregion

    public ContactType()
    {
        this._onlineResource = new OnlineResourceType();
        this._address = new AddressType();
        this._phone = new TelephoneType();
    }

    public TelephoneType Phone
    {
        get
        {
            return this._phone;
        }
        set
        {
            this._phone = value;
        }
    }

    public AddressType Address
    {
        get
        {
            return this._address;
        }
        set
        {
            this._address = value;
        }
    }

    public OnlineResourceType OnlineResource
    {
        get
        {
            return this._onlineResource;
        }
        set
        {
            this._onlineResource = value;
        }
    }

    public string HoursOfService
    {
        get
        {
            return this._hoursOfService;
        }
        set
        {
            this._hoursOfService = value;
        }
    }

    public string ContactInstructions
    {
        get
        {
            return this._contactInstructions;
        }
        set
        {
            this._contactInstructions = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class TelephoneType
{

    #region Private fields
    private List<string> _voice;

    private List<string> _facsimile;
    #endregion

    public TelephoneType()
    {
        this._facsimile = new List<string>();
        this._voice = new List<string>();
    }

    [System.Xml.Serialization.XmlElementAttribute("Voice")]
    public List<string> Voice
    {
        get
        {
            return this._voice;
        }
        set
        {
            this._voice = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Facsimile")]
    public List<string> Facsimile
    {
        get
        {
            return this._facsimile;
        }
        set
        {
            this._facsimile = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows")]
public partial class AddressType
{

    #region Private fields
    private List<string> _deliveryPoint;

    private string _city;

    private string _administrativeArea;

    private string _postalCode;

    private string _country;

    private List<string> _electronicMailAddress;
    #endregion

    public AddressType()
    {
        this._electronicMailAddress = new List<string>();
        this._deliveryPoint = new List<string>();
    }

    [System.Xml.Serialization.XmlElementAttribute("DeliveryPoint")]
    public List<string> DeliveryPoint
    {
        get
        {
            return this._deliveryPoint;
        }
        set
        {
            this._deliveryPoint = value;
        }
    }

    public string City
    {
        get
        {
            return this._city;
        }
        set
        {
            this._city = value;
        }
    }

    public string AdministrativeArea
    {
        get
        {
            return this._administrativeArea;
        }
        set
        {
            this._administrativeArea = value;
        }
    }

    public string PostalCode
    {
        get
        {
            return this._postalCode;
        }
        set
        {
            this._postalCode = value;
        }
    }

    public string Country
    {
        get
        {
            return this._country;
        }
        set
        {
            this._country = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ElectronicMailAddress")]
    public List<string> ElectronicMailAddress
    {
        get
        {
            return this._electronicMailAddress;
        }
        set
        {
            this._electronicMailAddress = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/ows")]
public partial class OperationsMetadata
{

    #region Private fields
    private List<Operation> _operation;

    private List<DomainType> _parameter;

    private List<DomainType> _constraint;

    private object _extendedCapabilities;
    #endregion

    public OperationsMetadata()
    {
        this._constraint = new List<DomainType>();
        this._parameter = new List<DomainType>();
        this._operation = new List<Operation>();
    }

    [System.Xml.Serialization.XmlElementAttribute("Operation")]
    public List<Operation> Operation
    {
        get
        {
            return this._operation;
        }
        set
        {
            this._operation = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Parameter")]
    public List<DomainType> Parameter
    {
        get
        {
            return this._parameter;
        }
        set
        {
            this._parameter = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Constraint")]
    public List<DomainType> Constraint
    {
        get
        {
            return this._constraint;
        }
        set
        {
            this._constraint = value;
        }
    }

    public object ExtendedCapabilities
    {
        get
        {
            return this._extendedCapabilities;
        }
        set
        {
            this._extendedCapabilities = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/ows")]
public partial class Operation
{

    #region Private fields
    private List<DCP> _dCP;

    private List<DomainType> _parameter;

    private List<DomainType> _constraint;

    private List<MetadataType> _metadata;

    private string _name;
    #endregion

    public Operation()
    {
        this._metadata = new List<MetadataType>();
        this._constraint = new List<DomainType>();
        this._parameter = new List<DomainType>();
        this._dCP = new List<DCP>();
    }

    [System.Xml.Serialization.XmlElementAttribute("DCP")]
    public List<DCP> DCP
    {
        get
        {
            return this._dCP;
        }
        set
        {
            this._dCP = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Parameter")]
    public List<DomainType> Parameter
    {
        get
        {
            return this._parameter;
        }
        set
        {
            this._parameter = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Constraint")]
    public List<DomainType> Constraint
    {
        get
        {
            return this._constraint;
        }
        set
        {
            this._constraint = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Metadata")]
    public List<MetadataType> Metadata
    {
        get
        {
            return this._metadata;
        }
        set
        {
            this._metadata = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string name
    {
        get
        {
            return this._name;
        }
        set
        {
            this._name = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/ows")]
public partial class DCP
{

    #region Private fields
    private HTTP _item;
    #endregion

    public DCP()
    {
        this._item = new HTTP();
    }

    //96.05.27 NARIMANIRAD
    public DCP(string url)
    {
        _item = new HTTP();
        _item.Items = new RequestMethodType[2];
        _item.Items[0] = new RequestMethodType() { href = url };
        _item.Items[1] = new RequestMethodType() { href = url };
        _item.ItemsElementName = new ItemsChoiceType14[2] { ItemsChoiceType14.Get, ItemsChoiceType14.Post };
    }

    [System.Xml.Serialization.XmlElementAttribute("HTTP")]
    public HTTP Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/ows")]
public partial class HTTP
{

    #region Private fields
    private RequestMethodType[] _items;

    private ItemsChoiceType14[] _itemsElementName;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("Get", typeof(RequestMethodType))]
    [System.Xml.Serialization.XmlElementAttribute("Post", typeof(RequestMethodType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public RequestMethodType[] Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType14[] ItemsElementName
    {
        get
        {
            return this._itemsElementName;
        }
        set
        {
            this._itemsElementName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ows", IncludeInSchema = false)]
public enum ItemsChoiceType14
{

    /// <remarks/>
    Get,

    /// <remarks/>
    Post,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("ServesGMLObjectTypeList", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class GMLObjectTypeListType
{

    #region Private fields
    private List<GMLObjectTypeType> _gMLObjectType;
    #endregion

    public GMLObjectTypeListType()
    {
        this._gMLObjectType = new List<GMLObjectTypeType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("GMLObjectType")]
    public List<GMLObjectTypeType> GMLObjectType
    {
        get
        {
            return this._gMLObjectType;
        }
        set
        {
            this._gMLObjectType = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("DescribeFeatureType", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class DescribeFeatureTypeType : BaseRequestType
{

    #region Private fields
    private List<System.Xml.XmlQualifiedName> _typeName;

    private string _outputFormat;
    #endregion

    public DescribeFeatureTypeType()
    {
        this._typeName = new List<System.Xml.XmlQualifiedName>();
        this._outputFormat = "text/xml; subtype=gml/3.1.1";
    }

    [System.Xml.Serialization.XmlElementAttribute("TypeName")]
    public List<System.Xml.XmlQualifiedName> TypeName
    {
        get
        {
            return this._typeName;
        }
        set
        {
            this._typeName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute("text/xml; subtype=gml/3.1.1")]
    public string outputFormat
    {
        get
        {
            return this._outputFormat;
        }
        set
        {
            this._outputFormat = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(TransactionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LockFeatureType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GetFeatureWithLockType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GetGmlObjectType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GetFeatureType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DescribeFeatureTypeType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
public abstract partial class BaseRequestType
{

    #region Private fields
    private string _service;

    private string _version;

    private string _handle;
    #endregion

    public BaseRequestType()
    {
        this._service = "WFS";
        this._version = "1.1.0";
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute("WFS")]
    public string service
    {
        get
        {
            return this._service;
        }
        set
        {
            this._service = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute("1.1.0")]
    public string version
    {
        get
        {
            return this._version;
        }
        set
        {
            this._version = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string handle
    {
        get
        {
            return this._handle;
        }
        set
        {
            this._handle = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("Transaction", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class TransactionType : BaseRequestType
{

    #region Private fields
    private string _lockId;

    private List<object> _items;

    private AllSomeType _releaseAction;
    #endregion

    public TransactionType()
    {
        this._items = new List<object>();
    }

    public string LockId
    {
        get
        {
            return this._lockId;
        }
        set
        {
            this._lockId = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Delete", typeof(DeleteElementType))]
    [System.Xml.Serialization.XmlElementAttribute("Insert", typeof(InsertElementType))]
    [System.Xml.Serialization.XmlElementAttribute("Native", typeof(NativeType))]
    [System.Xml.Serialization.XmlElementAttribute("Update", typeof(UpdateElementType))]
    public List<object> Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public AllSomeType releaseAction
    {
        get
        {
            return this._releaseAction;
        }
        set
        {
            this._releaseAction = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("Delete", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class DeleteElementType
{

    #region Private fields
    private FilterType _filter;

    private string _handle;

    private System.Xml.XmlQualifiedName _typeName;
    #endregion

    public DeleteElementType()
    {
        this._filter = new FilterType();
    }

    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://www.opengis.net/ogc")]
    public FilterType Filter
    {
        get
        {
            return this._filter;
        }
        set
        {
            this._filter = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string handle
    {
        get
        {
            return this._handle;
        }
        set
        {
            this._handle = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public System.Xml.XmlQualifiedName typeName
    {
        get
        {
            return this._typeName;
        }
        set
        {
            this._typeName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class FilterType
{

    #region Private fields
    private object[] _items;

    private ItemsChoiceType12[] _itemsElementName;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("And", typeof(BinaryLogicOpType))]
    [System.Xml.Serialization.XmlElementAttribute("BBOX", typeof(BBOXType))]
    [System.Xml.Serialization.XmlElementAttribute("Beyond", typeof(DistanceBufferType))]
    [System.Xml.Serialization.XmlElementAttribute("Contains", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Crosses", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("DWithin", typeof(DistanceBufferType))]
    [System.Xml.Serialization.XmlElementAttribute("Disjoint", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Equals", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("FeatureId", typeof(FeatureIdType))]
    [System.Xml.Serialization.XmlElementAttribute("GmlObjectId", typeof(GmlObjectIdType))]
    [System.Xml.Serialization.XmlElementAttribute("Intersects", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Not", typeof(UnaryLogicOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Or", typeof(BinaryLogicOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Overlaps", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsBetween", typeof(PropertyIsBetweenType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsEqualTo", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsGreaterThan", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsGreaterThanOrEqualTo", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsLessThan", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsLessThanOrEqualTo", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsLike", typeof(PropertyIsLikeType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsNotEqualTo", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsNull", typeof(PropertyIsNullType))]
    [System.Xml.Serialization.XmlElementAttribute("Touches", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Within", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("_Id", typeof(AbstractIdType))]
    [System.Xml.Serialization.XmlElementAttribute("comparisonOps", typeof(ComparisonOpsType))]
    [System.Xml.Serialization.XmlElementAttribute("logicOps", typeof(LogicOpsType))]
    [System.Xml.Serialization.XmlElementAttribute("spatialOps", typeof(SpatialOpsType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public object[] Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType12[] ItemsElementName
    {
        get
        {
            return this._itemsElementName;
        }
        set
        {
            this._itemsElementName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class BinaryLogicOpType : LogicOpsType
{

    #region Private fields
    private object[] _items;

    private ItemsChoiceType11[] _itemsElementName;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("And", typeof(BinaryLogicOpType))]
    [System.Xml.Serialization.XmlElementAttribute("BBOX", typeof(BBOXType))]
    [System.Xml.Serialization.XmlElementAttribute("Beyond", typeof(DistanceBufferType))]
    [System.Xml.Serialization.XmlElementAttribute("Contains", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Crosses", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("DWithin", typeof(DistanceBufferType))]
    [System.Xml.Serialization.XmlElementAttribute("Disjoint", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Equals", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Function", typeof(FunctionType))]
    [System.Xml.Serialization.XmlElementAttribute("Intersects", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Not", typeof(UnaryLogicOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Or", typeof(BinaryLogicOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Overlaps", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsBetween", typeof(PropertyIsBetweenType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsEqualTo", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsGreaterThan", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsGreaterThanOrEqualTo", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsLessThan", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsLessThanOrEqualTo", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsLike", typeof(PropertyIsLikeType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsNotEqualTo", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsNull", typeof(PropertyIsNullType))]
    [System.Xml.Serialization.XmlElementAttribute("Touches", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Within", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("comparisonOps", typeof(ComparisonOpsType))]
    [System.Xml.Serialization.XmlElementAttribute("logicOps", typeof(LogicOpsType))]
    [System.Xml.Serialization.XmlElementAttribute("spatialOps", typeof(SpatialOpsType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public object[] Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType11[] ItemsElementName
    {
        get
        {
            return this._itemsElementName;
        }
        set
        {
            this._itemsElementName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class BBOXType : SpatialOpsType
{

    #region Private fields
    private PropertyNameType _propertyName;

    private EnvelopeType _envelope;
    #endregion

    public BBOXType()
    {
        this._envelope = new EnvelopeType();
        this._propertyName = new PropertyNameType();
    }

    [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
    public PropertyNameType PropertyName
    {
        get
        {
            return this._propertyName;
        }
        set
        {
            this._propertyName = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://www.opengis.net/gml", Order = 1)]
    public EnvelopeType Envelope
    {
        get
        {
            return this._envelope;
        }
        set
        {
            this._envelope = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class PropertyNameType : ExpressionType
{

    #region Private fields
    private List<string> _text;
    #endregion

    public PropertyNameType()
    {
        this._text = new List<string>();
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public List<string> Text
    {
        get
        {
            return this._text;
        }
        set
        {
            this._text = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(PropertyNameType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LiteralType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(FunctionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(BinaryOperatorType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public abstract partial class ExpressionType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class LiteralType : ExpressionType
{

    #region Private fields
    private List<System.Xml.XmlNode> _any;
    #endregion

    public LiteralType()
    {
        this._any = new List<System.Xml.XmlNode>();
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    [System.Xml.Serialization.XmlAnyElementAttribute()]
    public List<System.Xml.XmlNode> Any
    {
        get
        {
            return this._any;
        }
        set
        {
            this._any = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class FunctionType : ExpressionType
{

    #region Private fields
    private List<ExpressionType> _expression;

    private string _name;
    #endregion

    public FunctionType()
    {
        this._expression = new List<ExpressionType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("expression")]
    public List<ExpressionType> expression
    {
        get
        {
            return this._expression;
        }
        set
        {
            this._expression = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string name
    {
        get
        {
            return this._name;
        }
        set
        {
            this._name = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class BinaryOperatorType : ExpressionType
{

    #region Private fields
    private List<ExpressionType> _expression;
    #endregion

    public BinaryOperatorType()
    {
        this._expression = new List<ExpressionType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("expression")]
    public List<ExpressionType> expression
    {
        get
        {
            return this._expression;
        }
        set
        {
            this._expression = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(EnvelopeWithTimePeriodType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class EnvelopeType
{

    #region Private fields
    private object[] _items;

    private ItemsChoiceType[] _itemsElementName;

    private string _srsName;

    private string _srsDimension;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("coord", typeof(CoordType))]
    [System.Xml.Serialization.XmlElementAttribute("coordinates", typeof(CoordinatesType))]
    [System.Xml.Serialization.XmlElementAttribute("lowerCorner", typeof(DirectPositionType))]
    [System.Xml.Serialization.XmlElementAttribute("pos", typeof(DirectPositionType))]
    [System.Xml.Serialization.XmlElementAttribute("upperCorner", typeof(DirectPositionType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public object[] Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType[] ItemsElementName
    {
        get
        {
            return this._itemsElementName;
        }
        set
        {
            this._itemsElementName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string srsName
    {
        get
        {
            return this._srsName;
        }
        set
        {
            this._srsName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string srsDimension
    {
        get
        {
            return this._srsDimension;
        }
        set
        {
            this._srsDimension = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CoordType
{

    #region Private fields
    private decimal _x;

    private decimal _y;

    private decimal _z;
    #endregion

    public decimal X
    {
        get
        {
            return this._x;
        }
        set
        {
            this._x = value;
        }
    }

    public decimal Y
    {
        get
        {
            return this._y;
        }
        set
        {
            this._y = value;
        }
    }

    public decimal Z
    {
        get
        {
            return this._z;
        }
        set
        {
            this._z = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CoordinatesType
{

    #region Private fields
    private string _decimal;

    private string _cs;

    private string _ts;

    private string _value;
    #endregion

    public CoordinatesType()
    {
        this._decimal = ".";
        this._cs = ",";
        this._ts = " ";
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(".")]
    public string @decimal
    {
        get
        {
            return this._decimal;
        }
        set
        {
            this._decimal = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(",")]
    public string cs
    {
        get
        {
            return this._cs;
        }
        set
        {
            this._cs = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(" ")]
    public string ts
    {
        get
        {
            return this._ts;
        }
        set
        {
            this._ts = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public string Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DirectPositionType
{

    #region Private fields
    private string _srsName;

    private string _srsDimension;

    private List<double> _text;
    #endregion

    public DirectPositionType()
    {
        this._text = new List<double>();
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string srsName
    {
        get
        {
            return this._srsName;
        }
        set
        {
            this._srsName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string srsDimension
    {
        get
        {
            return this._srsDimension;
        }
        set
        {
            this._srsDimension = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public List<double> Text
    {
        get
        {
            return this._text;
        }
        set
        {
            this._text = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml", IncludeInSchema = false)]
public enum ItemsChoiceType
{

    /// <remarks/>
    coord,

    /// <remarks/>
    coordinates,

    /// <remarks/>
    lowerCorner,

    /// <remarks/>
    pos,

    /// <remarks/>
    upperCorner,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class EnvelopeWithTimePeriodType : EnvelopeType
{

    #region Private fields
    private List<TimePositionType> _timePosition;

    private string _frame;
    #endregion

    public EnvelopeWithTimePeriodType()
    {
        this._timePosition = new List<TimePositionType>();
        this._frame = "#ISO-8601";
    }

    [System.Xml.Serialization.XmlElementAttribute("timePosition")]
    public List<TimePositionType> timePosition
    {
        get
        {
            return this._timePosition;
        }
        set
        {
            this._timePosition = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    [System.ComponentModel.DefaultValueAttribute("#ISO-8601")]
    public string frame
    {
        get
        {
            return this._frame;
        }
        set
        {
            this._frame = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimePositionType
{

    #region Private fields
    private string _frame;

    private string _calendarEraName;

    private TimeIndeterminateValueType _indeterminatePosition;

    private string _value;
    #endregion

    public TimePositionType()
    {
        this._frame = "#ISO-8601";
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    [System.ComponentModel.DefaultValueAttribute("#ISO-8601")]
    public string frame
    {
        get
        {
            return this._frame;
        }
        set
        {
            this._frame = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string calendarEraName
    {
        get
        {
            return this._calendarEraName;
        }
        set
        {
            this._calendarEraName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public TimeIndeterminateValueType indeterminatePosition
    {
        get
        {
            return this._indeterminatePosition;
        }
        set
        {
            this._indeterminatePosition = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public string Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum TimeIndeterminateValueType
{

    /// <remarks/>
    after,

    /// <remarks/>
    before,

    /// <remarks/>
    now,

    /// <remarks/>
    unknown,
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(DistanceBufferType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(BBOXType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(BinarySpatialOpType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public abstract partial class SpatialOpsType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class DistanceBufferType : SpatialOpsType
{

    #region Private fields
    private PropertyNameType _propertyName;

    private AbstractGeometryType _GeometryField;

    private DistanceType _distance;
    #endregion

    public DistanceBufferType()
    {
        this._distance = new DistanceType();
        this._propertyName = new PropertyNameType();
    }

    [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
    public PropertyNameType PropertyName
    {
        get
        {
            return this._propertyName;
        }
        set
        {
            this._propertyName = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://www.opengis.net/gml", Order = 1)]
    public AbstractGeometryType _Geometry
    {
        get
        {
            return this._GeometryField;
        }
        set
        {
            this._GeometryField = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
    public DistanceType Distance
    {
        get
        {
            return this._distance;
        }
        set
        {
            this._distance = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeometricAggregateType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPolygonType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiLineStringType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPointType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiGeometryType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GridType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(RectifiedGridType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeometricComplexType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractRingType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(RingType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LinearRingType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeometricPrimitiveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractSolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(SolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompositeSolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OrientableSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(SurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TriangulatedSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TinType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolyhedralSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompositeSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolygonType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OrientableCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompositeCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LineStringType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PointType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractGeometryType : AbstractGMLType
{

    #region Private fields
    private string _gid;

    private string _srsName;

    private string _srsDimension;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string gid
    {
        get
        {
            return this._gid;
        }
        set
        {
            this._gid = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string srsName
    {
        get
        {
            return this._srsName;
        }
        set
        {
            this._srsName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string srsDimension
    {
        get
        {
            return this._srsDimension;
        }
        set
        {
            this._srsDimension = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(BaseStyleDescriptorType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GraphStyleType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TopologyStyleType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LabelStyleType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeometryStyleType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(FeatureStyleType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractStyleType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(StyleType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTopologyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TopoComplexType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TopoVolumeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TopoSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TopoCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TopoPointType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTopoPrimitiveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TopoSolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(FaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EdgeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(NodeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompositeValueType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ValueArrayType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTimeSliceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MovingObjectStatusType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractFeatureType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ObservationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DirectedObservationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DirectedObservationAtDistanceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractDiscreteCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(RectifiedGridCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GridCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSolidCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSurfaceCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiCurveCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPointCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractContinuousCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DynamicFeatureType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(BoundedFeatureType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractFeatureCollectionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(FeatureCollectionType1))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DynamicFeatureCollectionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(FeatureCollectionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTimeObjectType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTimeComplexType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeTopologyComplexType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTimePrimitiveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTimeTopologyPrimitiveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeEdgeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeNodeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTimeGeometricPrimitiveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimePeriodType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeInstantType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeometryType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeometricAggregateType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPolygonType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiLineStringType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPointType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiGeometryType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GridType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(RectifiedGridType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeometricComplexType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractRingType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(RingType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LinearRingType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeometricPrimitiveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractSolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(SolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompositeSolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OrientableSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(SurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TriangulatedSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TinType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolyhedralSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompositeSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolygonType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OrientableCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompositeCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LineStringType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PointType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DefinitionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeCalendarEraType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeOrdinalEraType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTimeReferenceSystemType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeClockType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeCalendarType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeOrdinalReferenceSystemType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeCoordinateSystemType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeneralOperationParameterType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationParameterGroupBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationParameterGroupType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationParameterBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationParameterType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationMethodBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationMethodType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractCoordinateOperationBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractCoordinateOperationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeneralTransformationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TransformationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeneralConversionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConversionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PassThroughOperationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConcatenatedOperationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EllipsoidBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EllipsoidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PrimeMeridianBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PrimeMeridianType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractDatumBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeodeticDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalDatumBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(VerticalDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ImageDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EngineeringDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CoordinateSystemAxisBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CoordinateSystemAxisType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractCoordinateSystemBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractCoordinateSystemType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ObliqueCartesianCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CylindricalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolarCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(SphericalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(UserDefinedCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LinearCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(VerticalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CartesianCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EllipsoidalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractReferenceSystemBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractReferenceSystemType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ImageCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EngineeringCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeneralDerivedCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DerivedCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ProjectedCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeocentricCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(VerticalCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeographicCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompoundCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(UnitDefinitionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConventionalUnitType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DerivedUnitType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(BaseUnitType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DefinitionProxyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DictionaryType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ArrayType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(BagType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractGMLType
{

    #region Private fields
    private List<MetaDataPropertyType> _metaDataProperty;

    private StringOrRefType _description;

    private List<CodeType1> _name;

    private string _id;
    #endregion

    public AbstractGMLType()
    {
        this._name = new List<CodeType1>();
        this._description = new StringOrRefType();
        this._metaDataProperty = new List<MetaDataPropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("metaDataProperty")]
    public List<MetaDataPropertyType> metaDataProperty
    {
        get
        {
            return this._metaDataProperty;
        }
        set
        {
            this._metaDataProperty = value;
        }
    }

    public StringOrRefType description
    {
        get
        {
            return this._description;
        }
        set
        {
            this._description = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("name")]
    public List<CodeType1> name
    {
        get
        {
            return this._name;
        }
        set
        {
            this._name = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "ID")]
    public string id
    {
        get
        {
            return this._id;
        }
        set
        {
            this._id = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MetaDataPropertyType
{

    #region Private fields
    private System.Xml.XmlElement _any;

    private string _remoteSchema;

    private string _about;
    #endregion

    [System.Xml.Serialization.XmlAnyElementAttribute()]
    public System.Xml.XmlElement Any
    {
        get
        {
            return this._any;
        }
        set
        {
            this._any = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string about
    {
        get
        {
            return this._about;
        }
        set
        {
            this._about = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class StringOrRefType
{

    #region Private fields
    private string _remoteSchema;

    private string _value;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public string Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(DerivedCRSTypeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(VerticalDatumTypeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PixelInCellType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(TypeName = "CodeType", Namespace = "http://www.opengis.net/gml")]
public partial class CodeType1
{

    #region Private fields
    private string _codeSpace;

    private string _value;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string codeSpace
    {
        get
        {
            return this._codeSpace;
        }
        set
        {
            this._codeSpace = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public string Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DerivedCRSTypeType : CodeType1
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class VerticalDatumTypeType : CodeType1
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class PixelInCellType : CodeType1
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(GraphStyleType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TopologyStyleType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LabelStyleType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeometryStyleType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class BaseStyleDescriptorType : AbstractGMLType
{

    #region Private fields
    private ScaleType _spatialResolution;

    private List<StyleVariationType> _styleVariation;

    private List<animateType> _animate;

    private List<animateMotionType> _animateMotion;

    private List<animateColorType> _animateColor;

    private List<setType> _set;
    #endregion

    public BaseStyleDescriptorType()
    {
        this._set = new List<setType>();
        this._animateColor = new List<animateColorType>();
        this._animateMotion = new List<animateMotionType>();
        this._animate = new List<animateType>();
        this._styleVariation = new List<StyleVariationType>();
        this._spatialResolution = new ScaleType();
    }

    public ScaleType spatialResolution
    {
        get
        {
            return this._spatialResolution;
        }
        set
        {
            this._spatialResolution = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("styleVariation")]
    public List<StyleVariationType> styleVariation
    {
        get
        {
            return this._styleVariation;
        }
        set
        {
            this._styleVariation = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("animate", Namespace = "http://www.w3.org/2001/SMIL20/")]
    public List<animateType> animate
    {
        get
        {
            return this._animate;
        }
        set
        {
            this._animate = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("animateMotion", Namespace = "http://www.w3.org/2001/SMIL20/")]
    public List<animateMotionType> animateMotion
    {
        get
        {
            return this._animateMotion;
        }
        set
        {
            this._animateMotion = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("animateColor", Namespace = "http://www.w3.org/2001/SMIL20/")]
    public List<animateColorType> animateColor
    {
        get
        {
            return this._animateColor;
        }
        set
        {
            this._animateColor = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("set", Namespace = "http://www.w3.org/2001/SMIL20/")]
    public List<setType> set
    {
        get
        {
            return this._set;
        }
        set
        {
            this._set = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ScaleType : MeasureType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(AngleType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(SpeedType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(VolumeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AreaType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GridLengthType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ScaleType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LengthType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MeasureType
{

    #region Private fields
    private string _uom;

    private double _value;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string uom
    {
        get
        {
            return this._uom;
        }
        set
        {
            this._uom = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public double Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class AngleType : MeasureType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class SpeedType : MeasureType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class VolumeType : MeasureType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class AreaType : MeasureType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GridLengthType : MeasureType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeType : MeasureType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class LengthType : MeasureType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class StyleVariationType
{

    #region Private fields
    private string _styleProperty;

    private string _featurePropertyRange;

    private string _value;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string styleProperty
    {
        get
        {
            return this._styleProperty;
        }
        set
        {
            this._styleProperty = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string featurePropertyRange
    {
        get
        {
            return this._featurePropertyRange;
        }
        set
        {
            this._featurePropertyRange = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public string Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/2001/SMIL20/Language")]
public partial class animateType : animatePrototype
{

    #region Private fields
    private List<System.Xml.XmlElement> _items;

    private string _targetElement;

    private animateTypeCalcMode _calcMode;

    private bool _skipcontent;

    private List<System.Xml.XmlAttribute> _anyAttr;
    #endregion

    public animateType()
    {
        this._anyAttr = new List<System.Xml.XmlAttribute>();
        this._items = new List<System.Xml.XmlElement>();
        this._calcMode = animateTypeCalcMode.linear;
        this._skipcontent = true;
    }

    [System.Xml.Serialization.XmlAnyElementAttribute()]
    public List<System.Xml.XmlElement> Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "IDREF")]
    public string targetElement
    {
        get
        {
            return this._targetElement;
        }
        set
        {
            this._targetElement = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(animateTypeCalcMode.linear)]
    public animateTypeCalcMode calcMode
    {
        get
        {
            return this._calcMode;
        }
        set
        {
            this._calcMode = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute("skip-content")]
    [System.ComponentModel.DefaultValueAttribute(true)]
    public bool skipcontent
    {
        get
        {
            return this._skipcontent;
        }
        set
        {
            this._skipcontent = value;
        }
    }

    [System.Xml.Serialization.XmlAnyAttributeAttribute()]
    public List<System.Xml.XmlAttribute> AnyAttr
    {
        get
        {
            return this._anyAttr;
        }
        set
        {
            this._anyAttr = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.w3.org/2001/SMIL20/")]
public enum animateTypeCalcMode
{

    /// <remarks/>
    discrete,

    /// <remarks/>
    linear,

    /// <remarks/>
    paced,
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(animateType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/2001/SMIL20/")]
public partial class animatePrototype
{

    #region Private fields
    private string _attributeName;

    private animatePrototypeAttributeType _attributeType;

    private animatePrototypeAdditive _additive;

    private animatePrototypeAccumulate _accumulate;

    private string _from;

    private string _by;

    private string _values;
    #endregion

    public animatePrototype()
    {
        this._attributeType = animatePrototypeAttributeType.auto;
        this._additive = animatePrototypeAdditive.replace;
        this._accumulate = animatePrototypeAccumulate.none;
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string attributeName
    {
        get
        {
            return this._attributeName;
        }
        set
        {
            this._attributeName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(animatePrototypeAttributeType.auto)]
    public animatePrototypeAttributeType attributeType
    {
        get
        {
            return this._attributeType;
        }
        set
        {
            this._attributeType = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(animatePrototypeAdditive.replace)]
    public animatePrototypeAdditive additive
    {
        get
        {
            return this._additive;
        }
        set
        {
            this._additive = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(animatePrototypeAccumulate.none)]
    public animatePrototypeAccumulate accumulate
    {
        get
        {
            return this._accumulate;
        }
        set
        {
            this._accumulate = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string from
    {
        get
        {
            return this._from;
        }
        set
        {
            this._from = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string by
    {
        get
        {
            return this._by;
        }
        set
        {
            this._by = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string values
    {
        get
        {
            return this._values;
        }
        set
        {
            this._values = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.w3.org/2001/SMIL20/")]
public enum animatePrototypeAttributeType
{

    /// <remarks/>
    XML,

    /// <remarks/>
    CSS,

    /// <remarks/>
    auto,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.w3.org/2001/SMIL20/")]
public enum animatePrototypeAdditive
{

    /// <remarks/>
    replace,

    /// <remarks/>
    sum,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.w3.org/2001/SMIL20/")]
public enum animatePrototypeAccumulate
{

    /// <remarks/>
    none,

    /// <remarks/>
    sum,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/2001/SMIL20/Language")]
public partial class animateMotionType : animateMotionPrototype
{

    #region Private fields
    private List<System.Xml.XmlElement> _items;

    private string _targetElement;

    private animateTypeCalcMode _calcMode;

    private bool _skipcontent;

    private List<System.Xml.XmlAttribute> _anyAttr;
    #endregion

    public animateMotionType()
    {
        this._anyAttr = new List<System.Xml.XmlAttribute>();
        this._items = new List<System.Xml.XmlElement>();
        this._calcMode = animateTypeCalcMode.linear;
        this._skipcontent = true;
    }

    [System.Xml.Serialization.XmlAnyElementAttribute()]
    public List<System.Xml.XmlElement> Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "IDREF")]
    public string targetElement
    {
        get
        {
            return this._targetElement;
        }
        set
        {
            this._targetElement = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(animateTypeCalcMode.linear)]
    public animateTypeCalcMode calcMode
    {
        get
        {
            return this._calcMode;
        }
        set
        {
            this._calcMode = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute("skip-content")]
    [System.ComponentModel.DefaultValueAttribute(true)]
    public bool skipcontent
    {
        get
        {
            return this._skipcontent;
        }
        set
        {
            this._skipcontent = value;
        }
    }

    [System.Xml.Serialization.XmlAnyAttributeAttribute()]
    public List<System.Xml.XmlAttribute> AnyAttr
    {
        get
        {
            return this._anyAttr;
        }
        set
        {
            this._anyAttr = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(animateMotionType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/2001/SMIL20/")]
public partial class animateMotionPrototype
{

    #region Private fields
    private animatePrototypeAdditive _additive;

    private animatePrototypeAccumulate _accumulate;

    private string _from;

    private string _by;

    private string _values;

    private string _origin;
    #endregion

    public animateMotionPrototype()
    {
        this._additive = animatePrototypeAdditive.replace;
        this._accumulate = animatePrototypeAccumulate.none;
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(animatePrototypeAdditive.replace)]
    public animatePrototypeAdditive additive
    {
        get
        {
            return this._additive;
        }
        set
        {
            this._additive = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(animatePrototypeAccumulate.none)]
    public animatePrototypeAccumulate accumulate
    {
        get
        {
            return this._accumulate;
        }
        set
        {
            this._accumulate = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string from
    {
        get
        {
            return this._from;
        }
        set
        {
            this._from = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string by
    {
        get
        {
            return this._by;
        }
        set
        {
            this._by = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string values
    {
        get
        {
            return this._values;
        }
        set
        {
            this._values = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string origin
    {
        get
        {
            return this._origin;
        }
        set
        {
            this._origin = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/2001/SMIL20/Language")]
public partial class animateColorType : animateColorPrototype
{

    #region Private fields
    private List<System.Xml.XmlElement> _items;

    private string _targetElement;

    private animateTypeCalcMode _calcMode;

    private bool _skipcontent;

    private List<System.Xml.XmlAttribute> _anyAttr;
    #endregion

    public animateColorType()
    {
        this._anyAttr = new List<System.Xml.XmlAttribute>();
        this._items = new List<System.Xml.XmlElement>();
        this._calcMode = animateTypeCalcMode.linear;
        this._skipcontent = true;
    }

    [System.Xml.Serialization.XmlAnyElementAttribute()]
    public List<System.Xml.XmlElement> Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "IDREF")]
    public string targetElement
    {
        get
        {
            return this._targetElement;
        }
        set
        {
            this._targetElement = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(animateTypeCalcMode.linear)]
    public animateTypeCalcMode calcMode
    {
        get
        {
            return this._calcMode;
        }
        set
        {
            this._calcMode = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute("skip-content")]
    [System.ComponentModel.DefaultValueAttribute(true)]
    public bool skipcontent
    {
        get
        {
            return this._skipcontent;
        }
        set
        {
            this._skipcontent = value;
        }
    }

    [System.Xml.Serialization.XmlAnyAttributeAttribute()]
    public List<System.Xml.XmlAttribute> AnyAttr
    {
        get
        {
            return this._anyAttr;
        }
        set
        {
            this._anyAttr = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(animateColorType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/2001/SMIL20/")]
public partial class animateColorPrototype
{

    #region Private fields
    private string _attributeName;

    private animatePrototypeAttributeType _attributeType;

    private animatePrototypeAdditive _additive;

    private animatePrototypeAccumulate _accumulate;

    private string _from;

    private string _by;

    private string _values;
    #endregion

    public animateColorPrototype()
    {
        this._attributeType = animatePrototypeAttributeType.auto;
        this._additive = animatePrototypeAdditive.replace;
        this._accumulate = animatePrototypeAccumulate.none;
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string attributeName
    {
        get
        {
            return this._attributeName;
        }
        set
        {
            this._attributeName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(animatePrototypeAttributeType.auto)]
    public animatePrototypeAttributeType attributeType
    {
        get
        {
            return this._attributeType;
        }
        set
        {
            this._attributeType = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(animatePrototypeAdditive.replace)]
    public animatePrototypeAdditive additive
    {
        get
        {
            return this._additive;
        }
        set
        {
            this._additive = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(animatePrototypeAccumulate.none)]
    public animatePrototypeAccumulate accumulate
    {
        get
        {
            return this._accumulate;
        }
        set
        {
            this._accumulate = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string from
    {
        get
        {
            return this._from;
        }
        set
        {
            this._from = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string by
    {
        get
        {
            return this._by;
        }
        set
        {
            this._by = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string values
    {
        get
        {
            return this._values;
        }
        set
        {
            this._values = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/2001/SMIL20/Language")]
public partial class setType : setPrototype
{

    #region Private fields
    private List<System.Xml.XmlElement> _items;

    private string _targetElement;

    private bool _skipcontent;

    private List<System.Xml.XmlAttribute> _anyAttr;
    #endregion

    public setType()
    {
        this._anyAttr = new List<System.Xml.XmlAttribute>();
        this._items = new List<System.Xml.XmlElement>();
        this._skipcontent = true;
    }

    [System.Xml.Serialization.XmlAnyElementAttribute()]
    public List<System.Xml.XmlElement> Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "IDREF")]
    public string targetElement
    {
        get
        {
            return this._targetElement;
        }
        set
        {
            this._targetElement = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute("skip-content")]
    [System.ComponentModel.DefaultValueAttribute(true)]
    public bool skipcontent
    {
        get
        {
            return this._skipcontent;
        }
        set
        {
            this._skipcontent = value;
        }
    }

    [System.Xml.Serialization.XmlAnyAttributeAttribute()]
    public List<System.Xml.XmlAttribute> AnyAttr
    {
        get
        {
            return this._anyAttr;
        }
        set
        {
            this._anyAttr = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(setType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/2001/SMIL20/")]
public partial class setPrototype
{

    #region Private fields
    private string _attributeName;

    private animatePrototypeAttributeType _attributeType;

    private string _to;
    #endregion

    public setPrototype()
    {
        this._attributeType = animatePrototypeAttributeType.auto;
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string attributeName
    {
        get
        {
            return this._attributeName;
        }
        set
        {
            this._attributeName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(animatePrototypeAttributeType.auto)]
    public animatePrototypeAttributeType attributeType
    {
        get
        {
            return this._attributeType;
        }
        set
        {
            this._attributeType = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string to
    {
        get
        {
            return this._to;
        }
        set
        {
            this._to = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GraphStyleType : BaseStyleDescriptorType
{

    #region Private fields
    private bool _planar;

    private bool _directed;

    private bool _grid;

    private double _minDistance;

    private double _minAngle;

    private GraphTypeType _graphType;

    private DrawingTypeType _drawingType;

    private LineTypeType _lineType;

    private List<AesheticCriteriaType> _aestheticCriteria;
    #endregion

    public GraphStyleType()
    {
        this._aestheticCriteria = new List<AesheticCriteriaType>();
    }

    public bool planar
    {
        get
        {
            return this._planar;
        }
        set
        {
            this._planar = value;
        }
    }

    public bool directed
    {
        get
        {
            return this._directed;
        }
        set
        {
            this._directed = value;
        }
    }

    public bool grid
    {
        get
        {
            return this._grid;
        }
        set
        {
            this._grid = value;
        }
    }

    public double minDistance
    {
        get
        {
            return this._minDistance;
        }
        set
        {
            this._minDistance = value;
        }
    }

    public double minAngle
    {
        get
        {
            return this._minAngle;
        }
        set
        {
            this._minAngle = value;
        }
    }

    public GraphTypeType graphType
    {
        get
        {
            return this._graphType;
        }
        set
        {
            this._graphType = value;
        }
    }

    public DrawingTypeType drawingType
    {
        get
        {
            return this._drawingType;
        }
        set
        {
            this._drawingType = value;
        }
    }

    public LineTypeType lineType
    {
        get
        {
            return this._lineType;
        }
        set
        {
            this._lineType = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("aestheticCriteria")]
    public List<AesheticCriteriaType> aestheticCriteria
    {
        get
        {
            return this._aestheticCriteria;
        }
        set
        {
            this._aestheticCriteria = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum GraphTypeType
{

    /// <remarks/>
    TREE,

    /// <remarks/>
    BICONNECTED,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum DrawingTypeType
{

    /// <remarks/>
    POLYLINE,

    /// <remarks/>
    ORTHOGONAL,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum LineTypeType
{

    /// <remarks/>
    STRAIGHT,

    /// <remarks/>
    BENT,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum AesheticCriteriaType
{

    /// <remarks/>
    MIN_CROSSINGS,

    /// <remarks/>
    MIN_AREA,

    /// <remarks/>
    MIN_BENDS,

    /// <remarks/>
    MAX_BENDS,

    /// <remarks/>
    UNIFORM_BENDS,

    /// <remarks/>
    MIN_SLOPES,

    /// <remarks/>
    MIN_EDGE_LENGTH,

    /// <remarks/>
    MAX_EDGE_LENGTH,

    /// <remarks/>
    UNIFORM_EDGE_LENGTH,

    /// <remarks/>
    MAX_ANGULAR_RESOLUTION,

    /// <remarks/>
    MIN_ASPECT_RATIO,

    /// <remarks/>
    MAX_SYMMETRIES,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TopologyStyleType : BaseStyleDescriptorType
{

    #region Private fields
    private object _item;

    private LabelStylePropertyType _labelStyle;

    private string _topologyProperty;

    private string _topologyType;
    #endregion

    public TopologyStyleType()
    {
        this._labelStyle = new LabelStylePropertyType();
    }

    [System.Xml.Serialization.XmlElementAttribute("style", typeof(string))]
    [System.Xml.Serialization.XmlElementAttribute("symbol", typeof(SymbolType))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    public LabelStylePropertyType labelStyle
    {
        get
        {
            return this._labelStyle;
        }
        set
        {
            this._labelStyle = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string topologyProperty
    {
        get
        {
            return this._topologyProperty;
        }
        set
        {
            this._topologyProperty = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string topologyType
    {
        get
        {
            return this._topologyType;
        }
        set
        {
            this._topologyType = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class SymbolType
{

    #region Private fields
    private List<System.Xml.XmlElement> _any;

    private SymbolTypeEnumeration _symbolType;

    private string _transform;

    private string _about;

    private string _remoteSchema;
    #endregion

    public SymbolType()
    {
        this._any = new List<System.Xml.XmlElement>();
    }

    [System.Xml.Serialization.XmlAnyElementAttribute()]
    public List<System.Xml.XmlElement> Any
    {
        get
        {
            return this._any;
        }
        set
        {
            this._any = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public SymbolTypeEnumeration symbolType
    {
        get
        {
            return this._symbolType;
        }
        set
        {
            this._symbolType = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
    public string transform
    {
        get
        {
            return this._transform;
        }
        set
        {
            this._transform = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string about
    {
        get
        {
            return this._about;
        }
        set
        {
            this._about = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum SymbolTypeEnumeration
{

    /// <remarks/>
    svg,

    /// <remarks/>
    xpath,

    /// <remarks/>
    other,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class LabelStylePropertyType
{

    #region Private fields
    private LabelStyleType _labelStyle;

    private string _about;

    private string _remoteSchema;
    #endregion

    public LabelStylePropertyType()
    {
        this._labelStyle = new LabelStyleType();
    }

    public LabelStyleType LabelStyle
    {
        get
        {
            return this._labelStyle;
        }
        set
        {
            this._labelStyle = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string about
    {
        get
        {
            return this._about;
        }
        set
        {
            this._about = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class LabelStyleType : BaseStyleDescriptorType
{

    #region Private fields
    private string _style;

    private LabelType _label;
    #endregion

    public LabelStyleType()
    {
        this._label = new LabelType();
    }

    public string style
    {
        get
        {
            return this._style;
        }
        set
        {
            this._style = value;
        }
    }

    public LabelType label
    {
        get
        {
            return this._label;
        }
        set
        {
            this._label = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class LabelType
{

    #region Private fields
    private List<string> _labelExpression;

    private List<string> _text;

    private string _transform;
    #endregion

    public LabelType()
    {
        this._text = new List<string>();
        this._labelExpression = new List<string>();
    }

    [System.Xml.Serialization.XmlElementAttribute("LabelExpression")]
    public List<string> LabelExpression
    {
        get
        {
            return this._labelExpression;
        }
        set
        {
            this._labelExpression = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public List<string> Text
    {
        get
        {
            return this._text;
        }
        set
        {
            this._text = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
    public string transform
    {
        get
        {
            return this._transform;
        }
        set
        {
            this._transform = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GeometryStyleType : BaseStyleDescriptorType
{

    #region Private fields
    private object _item;

    private LabelStylePropertyType _labelStyle;

    private string _geometryProperty;

    private string _geometryType;
    #endregion

    public GeometryStyleType()
    {
        this._labelStyle = new LabelStylePropertyType();
    }

    [System.Xml.Serialization.XmlElementAttribute("style", typeof(string))]
    [System.Xml.Serialization.XmlElementAttribute("symbol", typeof(SymbolType))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    public LabelStylePropertyType labelStyle
    {
        get
        {
            return this._labelStyle;
        }
        set
        {
            this._labelStyle = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string geometryProperty
    {
        get
        {
            return this._geometryProperty;
        }
        set
        {
            this._geometryProperty = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string geometryType
    {
        get
        {
            return this._geometryType;
        }
        set
        {
            this._geometryType = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class FeatureStyleType : AbstractGMLType
{

    #region Private fields
    private string _featureConstraint;

    private List<GeometryStylePropertyType> _geometryStyle;

    private List<TopologyStylePropertyType> _topologyStyle;

    private LabelStylePropertyType _labelStyle;

    private string _featureType;

    private string _baseType;

    private QueryGrammarEnumeration _queryGrammar;
    #endregion

    public FeatureStyleType()
    {
        this._labelStyle = new LabelStylePropertyType();
        this._topologyStyle = new List<TopologyStylePropertyType>();
        this._geometryStyle = new List<GeometryStylePropertyType>();
    }

    public string featureConstraint
    {
        get
        {
            return this._featureConstraint;
        }
        set
        {
            this._featureConstraint = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("geometryStyle")]
    public List<GeometryStylePropertyType> geometryStyle
    {
        get
        {
            return this._geometryStyle;
        }
        set
        {
            this._geometryStyle = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("topologyStyle")]
    public List<TopologyStylePropertyType> topologyStyle
    {
        get
        {
            return this._topologyStyle;
        }
        set
        {
            this._topologyStyle = value;
        }
    }

    public LabelStylePropertyType labelStyle
    {
        get
        {
            return this._labelStyle;
        }
        set
        {
            this._labelStyle = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string featureType
    {
        get
        {
            return this._featureType;
        }
        set
        {
            this._featureType = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string baseType
    {
        get
        {
            return this._baseType;
        }
        set
        {
            this._baseType = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public QueryGrammarEnumeration queryGrammar
    {
        get
        {
            return this._queryGrammar;
        }
        set
        {
            this._queryGrammar = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GeometryStylePropertyType
{

    #region Private fields
    private GeometryStyleType _geometryStyle;

    private string _about;

    private string _remoteSchema;
    #endregion

    public GeometryStylePropertyType()
    {
        this._geometryStyle = new GeometryStyleType();
    }

    public GeometryStyleType GeometryStyle
    {
        get
        {
            return this._geometryStyle;
        }
        set
        {
            this._geometryStyle = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string about
    {
        get
        {
            return this._about;
        }
        set
        {
            this._about = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TopologyStylePropertyType
{

    #region Private fields
    private TopologyStyleType _topologyStyle;

    private string _about;

    private string _remoteSchema;
    #endregion

    public TopologyStylePropertyType()
    {
        this._topologyStyle = new TopologyStyleType();
    }

    public TopologyStyleType TopologyStyle
    {
        get
        {
            return this._topologyStyle;
        }
        set
        {
            this._topologyStyle = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string about
    {
        get
        {
            return this._about;
        }
        set
        {
            this._about = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum QueryGrammarEnumeration
{

    /// <remarks/>
    xpath,

    /// <remarks/>
    xquery,

    /// <remarks/>
    other,
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(StyleType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractStyleType : AbstractGMLType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class StyleType : AbstractStyleType
{

    #region Private fields
    private List<FeatureStylePropertyType> _featureStyle;

    private GraphStylePropertyType _graphStyle;
    #endregion

    public StyleType()
    {
        this._graphStyle = new GraphStylePropertyType();
        this._featureStyle = new List<FeatureStylePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("featureStyle")]
    public List<FeatureStylePropertyType> featureStyle
    {
        get
        {
            return this._featureStyle;
        }
        set
        {
            this._featureStyle = value;
        }
    }

    public GraphStylePropertyType graphStyle
    {
        get
        {
            return this._graphStyle;
        }
        set
        {
            this._graphStyle = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class FeatureStylePropertyType
{

    #region Private fields
    private FeatureStyleType _featureStyle;

    private string _about;

    private string _remoteSchema;
    #endregion

    public FeatureStylePropertyType()
    {
        this._featureStyle = new FeatureStyleType();
    }

    public FeatureStyleType FeatureStyle
    {
        get
        {
            return this._featureStyle;
        }
        set
        {
            this._featureStyle = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string about
    {
        get
        {
            return this._about;
        }
        set
        {
            this._about = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GraphStylePropertyType
{

    #region Private fields
    private GraphStyleType _graphStyle;

    private string _about;

    private string _remoteSchema;
    #endregion

    public GraphStylePropertyType()
    {
        this._graphStyle = new GraphStyleType();
    }

    public GraphStyleType GraphStyle
    {
        get
        {
            return this._graphStyle;
        }
        set
        {
            this._graphStyle = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string about
    {
        get
        {
            return this._about;
        }
        set
        {
            this._about = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(TopoComplexType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TopoVolumeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TopoSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TopoCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TopoPointType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTopoPrimitiveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TopoSolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(FaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EdgeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(NodeType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractTopologyType : AbstractGMLType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TopoComplexType : AbstractTopologyType
{

    #region Private fields
    private TopoComplexMemberType _maximalComplex;

    private List<TopoComplexMemberType> _superComplex;

    private List<TopoComplexMemberType> _subComplex;

    private List<TopoPrimitiveMemberType> _topoPrimitiveMember;

    private TopoPrimitiveArrayAssociationType _topoPrimitiveMembers;

    private bool _isMaximal;
    #endregion

    public TopoComplexType()
    {
        this._topoPrimitiveMembers = new TopoPrimitiveArrayAssociationType();
        this._topoPrimitiveMember = new List<TopoPrimitiveMemberType>();
        this._subComplex = new List<TopoComplexMemberType>();
        this._superComplex = new List<TopoComplexMemberType>();
        this._maximalComplex = new TopoComplexMemberType();
        this._isMaximal = false;
    }

    public TopoComplexMemberType maximalComplex
    {
        get
        {
            return this._maximalComplex;
        }
        set
        {
            this._maximalComplex = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("superComplex")]
    public List<TopoComplexMemberType> superComplex
    {
        get
        {
            return this._superComplex;
        }
        set
        {
            this._superComplex = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("subComplex")]
    public List<TopoComplexMemberType> subComplex
    {
        get
        {
            return this._subComplex;
        }
        set
        {
            this._subComplex = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("topoPrimitiveMember")]
    public List<TopoPrimitiveMemberType> topoPrimitiveMember
    {
        get
        {
            return this._topoPrimitiveMember;
        }
        set
        {
            this._topoPrimitiveMember = value;
        }
    }

    public TopoPrimitiveArrayAssociationType topoPrimitiveMembers
    {
        get
        {
            return this._topoPrimitiveMembers;
        }
        set
        {
            this._topoPrimitiveMembers = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(false)]
    public bool isMaximal
    {
        get
        {
            return this._isMaximal;
        }
        set
        {
            this._isMaximal = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TopoComplexMemberType
{

    #region Private fields
    private TopoComplexType _topoComplex;

    private string _remoteSchema;
    #endregion

    public TopoComplexMemberType()
    {
        this._topoComplex = new TopoComplexType();
    }

    public TopoComplexType TopoComplex
    {
        get
        {
            return this._topoComplex;
        }
        set
        {
            this._topoComplex = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TopoPrimitiveMemberType
{

    #region Private fields
    private AbstractTopoPrimitiveType _TopoPrimitiveField;

    private string _remoteSchema;
    #endregion

    public AbstractTopoPrimitiveType _TopoPrimitive
    {
        get
        {
            return this._TopoPrimitiveField;
        }
        set
        {
            this._TopoPrimitiveField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(TopoSolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(FaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EdgeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(NodeType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractTopoPrimitiveType : AbstractTopologyType
{

    #region Private fields
    private List<IsolatedPropertyType> _isolated;

    private ContainerPropertyType _container;
    #endregion

    public AbstractTopoPrimitiveType()
    {
        this._container = new ContainerPropertyType();
        this._isolated = new List<IsolatedPropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("isolated")]
    public List<IsolatedPropertyType> isolated
    {
        get
        {
            return this._isolated;
        }
        set
        {
            this._isolated = value;
        }
    }

    public ContainerPropertyType container
    {
        get
        {
            return this._container;
        }
        set
        {
            this._container = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class IsolatedPropertyType
{

    #region Private fields
    private AbstractTopoPrimitiveType _item;

    private string _remoteSchema;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("Edge", typeof(EdgeType))]
    [System.Xml.Serialization.XmlElementAttribute("Node", typeof(NodeType))]
    public AbstractTopoPrimitiveType Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class EdgeType : AbstractTopoPrimitiveType
{

    #region Private fields
    private List<DirectedNodePropertyType> _directedNode;

    private List<DirectedFacePropertyType> _directedFace;

    private CurvePropertyType _curveProperty;
    #endregion

    public EdgeType()
    {
        this._curveProperty = new CurvePropertyType();
        this._directedFace = new List<DirectedFacePropertyType>();
        this._directedNode = new List<DirectedNodePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("directedNode")]
    public List<DirectedNodePropertyType> directedNode
    {
        get
        {
            return this._directedNode;
        }
        set
        {
            this._directedNode = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("directedFace")]
    public List<DirectedFacePropertyType> directedFace
    {
        get
        {
            return this._directedFace;
        }
        set
        {
            this._directedFace = value;
        }
    }

    public CurvePropertyType curveProperty
    {
        get
        {
            return this._curveProperty;
        }
        set
        {
            this._curveProperty = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DirectedNodePropertyType
{

    #region Private fields
    private NodeType _node;

    private SignType _orientation;

    private string _remoteSchema;
    #endregion

    public DirectedNodePropertyType()
    {
        this._node = new NodeType();
        this._orientation = SignType.Item1;
    }

    public NodeType Node
    {
        get
        {
            return this._node;
        }
        set
        {
            this._node = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(SignType.Item1)]
    public SignType orientation
    {
        get
        {
            return this._orientation;
        }
        set
        {
            this._orientation = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class NodeType : AbstractTopoPrimitiveType
{

    #region Private fields
    private List<DirectedEdgePropertyType> _directedEdge;

    private PointPropertyType _pointProperty;
    #endregion

    public NodeType()
    {
        this._pointProperty = new PointPropertyType();
        this._directedEdge = new List<DirectedEdgePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("directedEdge")]
    public List<DirectedEdgePropertyType> directedEdge
    {
        get
        {
            return this._directedEdge;
        }
        set
        {
            this._directedEdge = value;
        }
    }

    public PointPropertyType pointProperty
    {
        get
        {
            return this._pointProperty;
        }
        set
        {
            this._pointProperty = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DirectedEdgePropertyType
{

    #region Private fields
    private EdgeType _edge;

    private SignType _orientation;

    private string _remoteSchema;
    #endregion

    public DirectedEdgePropertyType()
    {
        this._edge = new EdgeType();
        this._orientation = SignType.Item1;
    }

    public EdgeType Edge
    {
        get
        {
            return this._edge;
        }
        set
        {
            this._edge = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(SignType.Item1)]
    public SignType orientation
    {
        get
        {
            return this._orientation;
        }
        set
        {
            this._orientation = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum SignType
{

    /// <remarks/>
    [System.Xml.Serialization.XmlEnumAttribute("-")]
    Item,

    /// <remarks/>
    [System.Xml.Serialization.XmlEnumAttribute("+")]
    Item1,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class PointPropertyType
{

    #region Private fields
    private PointType _point;

    private string _remoteSchema;
    #endregion

    public PointPropertyType()
    {
        this._point = new PointType();
    }

    public PointType Point
    {
        get
        {
            return this._point;
        }
        set
        {
            this._point = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class PointType : AbstractGeometricPrimitiveType
{

    #region Private fields
    private object _item;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("coord", typeof(CoordType))]
    [System.Xml.Serialization.XmlElementAttribute("coordinates", typeof(CoordinatesType))]
    [System.Xml.Serialization.XmlElementAttribute("pos", typeof(DirectPositionType))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractSolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(SolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompositeSolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OrientableSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(SurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TriangulatedSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TinType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolyhedralSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompositeSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolygonType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OrientableCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompositeCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LineStringType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PointType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractGeometricPrimitiveType : AbstractGeometryType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(SolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompositeSolidType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class AbstractSolidType : AbstractGeometricPrimitiveType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class SolidType : AbstractSolidType
{

    #region Private fields
    private SurfacePropertyType _exterior;

    private List<SurfacePropertyType> _interior;
    #endregion

    public SolidType()
    {
        this._interior = new List<SurfacePropertyType>();
        this._exterior = new SurfacePropertyType();
    }

    public SurfacePropertyType exterior
    {
        get
        {
            return this._exterior;
        }
        set
        {
            this._exterior = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("interior")]
    public List<SurfacePropertyType> interior
    {
        get
        {
            return this._interior;
        }
        set
        {
            this._interior = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class SurfacePropertyType
{

    #region Private fields
    private AbstractSurfaceType _SurfaceField;

    private string _remoteSchema;
    #endregion

    public SurfacePropertyType()
    {
        this._SurfaceField = new AbstractSurfaceType();
    }

    public AbstractSurfaceType _Surface
    {
        get
        {
            return this._SurfaceField;
        }
        set
        {
            this._SurfaceField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(OrientableSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(SurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TriangulatedSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TinType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolyhedralSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompositeSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolygonType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class AbstractSurfaceType : AbstractGeometricPrimitiveType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class OrientableSurfaceType : AbstractSurfaceType
{

    #region Private fields
    private SurfacePropertyType _baseSurface;

    private SignType _orientation;
    #endregion

    public OrientableSurfaceType()
    {
        this._baseSurface = new SurfacePropertyType();
        this._orientation = SignType.Item1;
    }

    public SurfacePropertyType baseSurface
    {
        get
        {
            return this._baseSurface;
        }
        set
        {
            this._baseSurface = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(SignType.Item1)]
    public SignType orientation
    {
        get
        {
            return this._orientation;
        }
        set
        {
            this._orientation = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(TriangulatedSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TinType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolyhedralSurfaceType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class SurfaceType : AbstractSurfaceType
{

    #region Private fields
    private SurfacePatchArrayPropertyType _patches;
    #endregion

    public SurfaceType()
    {
        this._patches = new SurfacePatchArrayPropertyType();
    }

    public SurfacePatchArrayPropertyType patches
    {
        get
        {
            return this._patches;
        }
        set
        {
            this._patches = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(TrianglePatchArrayPropertyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolygonPatchArrayPropertyType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class SurfacePatchArrayPropertyType
{

    #region Private fields
    private List<AbstractSurfacePatchType> _SurfacePatchField;
    #endregion

    public SurfacePatchArrayPropertyType()
    {
        this._SurfacePatchField = new List<AbstractSurfacePatchType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("_SurfacePatch")]
    public List<AbstractSurfacePatchType> _SurfacePatch
    {
        get
        {
            return this._SurfacePatchField;
        }
        set
        {
            this._SurfacePatchField = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractParametricCurveSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGriddedSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(SphereType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CylinderType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(RectangleType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TriangleType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolygonPatchType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractSurfacePatchType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGriddedSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(SphereType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CylinderType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConeType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class AbstractParametricCurveSurfaceType : AbstractSurfacePatchType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(SphereType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CylinderType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConeType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class AbstractGriddedSurfaceType : AbstractParametricCurveSurfaceType
{

    #region Private fields
    private List<AbstractGriddedSurfaceTypeRow> _row;

    private string _rows;

    private string _columns;
    #endregion

    public AbstractGriddedSurfaceType()
    {
        this._row = new List<AbstractGriddedSurfaceTypeRow>();
    }

    [System.Xml.Serialization.XmlElementAttribute("row")]
    public List<AbstractGriddedSurfaceTypeRow> row
    {
        get
        {
            return this._row;
        }
        set
        {
            this._row = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "integer")]
    public string rows
    {
        get
        {
            return this._rows;
        }
        set
        {
            this._rows = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "integer")]
    public string columns
    {
        get
        {
            return this._columns;
        }
        set
        {
            this._columns = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/gml")]
public partial class AbstractGriddedSurfaceTypeRow
{

    #region Private fields
    private DirectPositionListType _posList;

    private List<DirectPositionType> _pos;

    private List<PointPropertyType> _pointProperty;
    #endregion

    public AbstractGriddedSurfaceTypeRow()
    {
        this._pointProperty = new List<PointPropertyType>();
        this._pos = new List<DirectPositionType>();
        this._posList = new DirectPositionListType();
    }

    public DirectPositionListType posList
    {
        get
        {
            return this._posList;
        }
        set
        {
            this._posList = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("pos")]
    public List<DirectPositionType> pos
    {
        get
        {
            return this._pos;
        }
        set
        {
            this._pos = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("pointProperty")]
    public List<PointPropertyType> pointProperty
    {
        get
        {
            return this._pointProperty;
        }
        set
        {
            this._pointProperty = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DirectPositionListType
{

    #region Private fields
    private string _srsName;

    private string _srsDimension;

    private string _count;

    private List<double> _text;
    #endregion

    public DirectPositionListType()
    {
        this._text = new List<double>();
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string srsName
    {
        get
        {
            return this._srsName;
        }
        set
        {
            this._srsName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string srsDimension
    {
        get
        {
            return this._srsDimension;
        }
        set
        {
            this._srsDimension = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string count
    {
        get
        {
            return this._count;
        }
        set
        {
            this._count = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public List<double> Text
    {
        get
        {
            return this._text;
        }
        set
        {
            this._text = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class SphereType : AbstractGriddedSurfaceType
{

    #region Private fields
    private CurveInterpolationType _horizontalCurveType;

    private CurveInterpolationType _verticalCurveType;
    #endregion

    public SphereType()
    {
        this._horizontalCurveType = CurveInterpolationType.circularArc3Points;
        this._verticalCurveType = CurveInterpolationType.circularArc3Points;
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public CurveInterpolationType horizontalCurveType
    {
        get
        {
            return this._horizontalCurveType;
        }
        set
        {
            this._horizontalCurveType = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public CurveInterpolationType verticalCurveType
    {
        get
        {
            return this._verticalCurveType;
        }
        set
        {
            this._verticalCurveType = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum CurveInterpolationType
{

    /// <remarks/>
    linear,

    /// <remarks/>
    geodesic,

    /// <remarks/>
    circularArc3Points,

    /// <remarks/>
    circularArc2PointWithBulge,

    /// <remarks/>
    circularArcCenterPointWithRadius,

    /// <remarks/>
    elliptical,

    /// <remarks/>
    clothoid,

    /// <remarks/>
    conic,

    /// <remarks/>
    polynomialSpline,

    /// <remarks/>
    cubicSpline,

    /// <remarks/>
    rationalSpline,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CylinderType : AbstractGriddedSurfaceType
{

    #region Private fields
    private CurveInterpolationType _horizontalCurveType;

    private CurveInterpolationType _verticalCurveType;
    #endregion

    public CylinderType()
    {
        this._horizontalCurveType = CurveInterpolationType.circularArc3Points;
        this._verticalCurveType = CurveInterpolationType.linear;
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public CurveInterpolationType horizontalCurveType
    {
        get
        {
            return this._horizontalCurveType;
        }
        set
        {
            this._horizontalCurveType = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public CurveInterpolationType verticalCurveType
    {
        get
        {
            return this._verticalCurveType;
        }
        set
        {
            this._verticalCurveType = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ConeType : AbstractGriddedSurfaceType
{

    #region Private fields
    private CurveInterpolationType _horizontalCurveType;

    private CurveInterpolationType _verticalCurveType;
    #endregion

    public ConeType()
    {
        this._horizontalCurveType = CurveInterpolationType.circularArc3Points;
        this._verticalCurveType = CurveInterpolationType.linear;
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public CurveInterpolationType horizontalCurveType
    {
        get
        {
            return this._horizontalCurveType;
        }
        set
        {
            this._horizontalCurveType = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public CurveInterpolationType verticalCurveType
    {
        get
        {
            return this._verticalCurveType;
        }
        set
        {
            this._verticalCurveType = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class RectangleType : AbstractSurfacePatchType
{

    #region Private fields
    private AbstractRingPropertyType _exterior;

    private SurfaceInterpolationType _interpolation;
    #endregion

    public RectangleType()
    {
        this._exterior = new AbstractRingPropertyType();
        this._interpolation = SurfaceInterpolationType.planar;
    }

    public AbstractRingPropertyType exterior
    {
        get
        {
            return this._exterior;
        }
        set
        {
            this._exterior = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public SurfaceInterpolationType interpolation
    {
        get
        {
            return this._interpolation;
        }
        set
        {
            this._interpolation = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class AbstractRingPropertyType
{

    #region Private fields
    private AbstractRingType _RingField;
    #endregion

    public AbstractRingType _Ring
    {
        get
        {
            return this._RingField;
        }
        set
        {
            this._RingField = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(RingType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LinearRingType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractRingType : AbstractGeometryType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class RingType : AbstractRingType
{

    #region Private fields
    private List<CurvePropertyType> _curveMember;
    #endregion

    public RingType()
    {
        this._curveMember = new List<CurvePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("curveMember")]
    public List<CurvePropertyType> curveMember
    {
        get
        {
            return this._curveMember;
        }
        set
        {
            this._curveMember = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CurvePropertyType
{

    #region Private fields
    private AbstractCurveType _CurveField;

    private string _remoteSchema;
    #endregion

    public AbstractCurveType _Curve
    {
        get
        {
            return this._CurveField;
        }
        set
        {
            this._CurveField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(OrientableCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompositeCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LineStringType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractCurveType : AbstractGeometricPrimitiveType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class OrientableCurveType : AbstractCurveType
{

    #region Private fields
    private CurvePropertyType _baseCurve;

    private SignType _orientation;
    #endregion

    public OrientableCurveType()
    {
        this._baseCurve = new CurvePropertyType();
        this._orientation = SignType.Item1;
    }

    public CurvePropertyType baseCurve
    {
        get
        {
            return this._baseCurve;
        }
        set
        {
            this._baseCurve = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(SignType.Item1)]
    public SignType orientation
    {
        get
        {
            return this._orientation;
        }
        set
        {
            this._orientation = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CurveType : AbstractCurveType
{

    #region Private fields
    private List<AbstractCurveSegmentType> _segments;
    #endregion

    public CurveType()
    {
        this._segments = new List<AbstractCurveSegmentType>();
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("_CurveSegment", IsNullable = false)]
    public List<AbstractCurveSegmentType> segments
    {
        get
        {
            return this._segments;
        }
        set
        {
            this._segments = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(BSplineType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(BezierType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CubicSplineType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeodesicStringType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeodesicType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ClothoidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OffsetCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ArcByCenterPointType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CircleByCenterPointType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ArcStringByBulgeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ArcByBulgeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ArcStringType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ArcType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CircleType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LineStringSegmentType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractCurveSegmentType
{

    #region Private fields
    private string _numDerivativesAtStart;

    private string _numDerivativesAtEnd;

    private string _numDerivativeInterior;
    #endregion

    public AbstractCurveSegmentType()
    {
        this._numDerivativesAtStart = "0";
        this._numDerivativesAtEnd = "0";
        this._numDerivativeInterior = "0";
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
    [System.ComponentModel.DefaultValueAttribute("0")]
    public string numDerivativesAtStart
    {
        get
        {
            return this._numDerivativesAtStart;
        }
        set
        {
            this._numDerivativesAtStart = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
    [System.ComponentModel.DefaultValueAttribute("0")]
    public string numDerivativesAtEnd
    {
        get
        {
            return this._numDerivativesAtEnd;
        }
        set
        {
            this._numDerivativesAtEnd = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
    [System.ComponentModel.DefaultValueAttribute("0")]
    public string numDerivativeInterior
    {
        get
        {
            return this._numDerivativeInterior;
        }
        set
        {
            this._numDerivativeInterior = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(BezierType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class BSplineType : AbstractCurveSegmentType
{

    #region Private fields
    private object[] _items;

    private ItemsChoiceType6[] _itemsElementName;

    private string _degree;

    private List<KnotPropertyType> _knot;

    private CurveInterpolationType _interpolation;

    private bool _isPolynomial;

    private KnotTypesType _knotType;
    #endregion

    public BSplineType()
    {
        this._knot = new List<KnotPropertyType>();
        this._interpolation = CurveInterpolationType.polynomialSpline;
    }

    [System.Xml.Serialization.XmlElementAttribute("coordinates", typeof(CoordinatesType))]
    [System.Xml.Serialization.XmlElementAttribute("pointProperty", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pointRep", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pos", typeof(DirectPositionType))]
    [System.Xml.Serialization.XmlElementAttribute("posList", typeof(DirectPositionListType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public object[] Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType6[] ItemsElementName
    {
        get
        {
            return this._itemsElementName;
        }
        set
        {
            this._itemsElementName = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "nonNegativeInteger")]
    public string degree
    {
        get
        {
            return this._degree;
        }
        set
        {
            this._degree = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("knot")]
    public List<KnotPropertyType> knot
    {
        get
        {
            return this._knot;
        }
        set
        {
            this._knot = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(CurveInterpolationType.polynomialSpline)]
    public CurveInterpolationType interpolation
    {
        get
        {
            return this._interpolation;
        }
        set
        {
            this._interpolation = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public bool isPolynomial
    {
        get
        {
            return this._isPolynomial;
        }
        set
        {
            this._isPolynomial = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public KnotTypesType knotType
    {
        get
        {
            return this._knotType;
        }
        set
        {
            this._knotType = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml", IncludeInSchema = false)]
public enum ItemsChoiceType6
{

    /// <remarks/>
    coordinates,

    /// <remarks/>
    pointProperty,

    /// <remarks/>
    pointRep,

    /// <remarks/>
    pos,

    /// <remarks/>
    posList,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class KnotPropertyType
{

    #region Private fields
    private KnotType _knot;
    #endregion

    public KnotPropertyType()
    {
        this._knot = new KnotType();
    }

    public KnotType Knot
    {
        get
        {
            return this._knot;
        }
        set
        {
            this._knot = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class KnotType
{

    #region Private fields
    private double _value;

    private string _multiplicity;

    private double _weight;
    #endregion

    public double value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "nonNegativeInteger")]
    public string multiplicity
    {
        get
        {
            return this._multiplicity;
        }
        set
        {
            this._multiplicity = value;
        }
    }

    public double weight
    {
        get
        {
            return this._weight;
        }
        set
        {
            this._weight = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum KnotTypesType
{

    /// <remarks/>
    uniform,

    /// <remarks/>
    quasiUniform,

    /// <remarks/>
    piecewiseBezier,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class BezierType : BSplineType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CubicSplineType : AbstractCurveSegmentType
{

    #region Private fields
    private object[] _items;

    private ItemsChoiceType5[] _itemsElementName;

    private VectorType _vectorAtStart;

    private VectorType _vectorAtEnd;

    private CurveInterpolationType _interpolation;

    private string _degree;
    #endregion

    public CubicSplineType()
    {
        this._vectorAtEnd = new VectorType();
        this._vectorAtStart = new VectorType();
        this._interpolation = CurveInterpolationType.cubicSpline;
        this._degree = "3";
    }

    [System.Xml.Serialization.XmlElementAttribute("coordinates", typeof(CoordinatesType))]
    [System.Xml.Serialization.XmlElementAttribute("pointProperty", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pointRep", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pos", typeof(DirectPositionType))]
    [System.Xml.Serialization.XmlElementAttribute("posList", typeof(DirectPositionListType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public object[] Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType5[] ItemsElementName
    {
        get
        {
            return this._itemsElementName;
        }
        set
        {
            this._itemsElementName = value;
        }
    }

    public VectorType vectorAtStart
    {
        get
        {
            return this._vectorAtStart;
        }
        set
        {
            this._vectorAtStart = value;
        }
    }

    public VectorType vectorAtEnd
    {
        get
        {
            return this._vectorAtEnd;
        }
        set
        {
            this._vectorAtEnd = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public CurveInterpolationType interpolation
    {
        get
        {
            return this._interpolation;
        }
        set
        {
            this._interpolation = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
    public string degree
    {
        get
        {
            return this._degree;
        }
        set
        {
            this._degree = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml", IncludeInSchema = false)]
public enum ItemsChoiceType5
{

    /// <remarks/>
    coordinates,

    /// <remarks/>
    pointProperty,

    /// <remarks/>
    pointRep,

    /// <remarks/>
    pos,

    /// <remarks/>
    posList,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class VectorType
{

    #region Private fields
    private string _srsName;

    private string _srsDimension;

    private List<double> _text;
    #endregion

    public VectorType()
    {
        this._text = new List<double>();
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string srsName
    {
        get
        {
            return this._srsName;
        }
        set
        {
            this._srsName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string srsDimension
    {
        get
        {
            return this._srsDimension;
        }
        set
        {
            this._srsDimension = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public List<double> Text
    {
        get
        {
            return this._text;
        }
        set
        {
            this._text = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeodesicType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GeodesicStringType : AbstractCurveSegmentType
{

    #region Private fields
    private object _item;

    private CurveInterpolationType _interpolation;
    #endregion

    public GeodesicStringType()
    {
        this._interpolation = CurveInterpolationType.geodesic;
    }

    [System.Xml.Serialization.XmlElementAttribute("pointProperty", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pos", typeof(DirectPositionType))]
    [System.Xml.Serialization.XmlElementAttribute("posList", typeof(DirectPositionListType))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public CurveInterpolationType interpolation
    {
        get
        {
            return this._interpolation;
        }
        set
        {
            this._interpolation = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GeodesicType : GeodesicStringType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ClothoidType : AbstractCurveSegmentType
{

    #region Private fields
    private ClothoidTypeRefLocation _refLocation;

    private decimal _scaleFactor;

    private double _startParameter;

    private double _endParameter;
    #endregion

    public ClothoidType()
    {
        this._refLocation = new ClothoidTypeRefLocation();
    }

    public ClothoidTypeRefLocation refLocation
    {
        get
        {
            return this._refLocation;
        }
        set
        {
            this._refLocation = value;
        }
    }

    public decimal scaleFactor
    {
        get
        {
            return this._scaleFactor;
        }
        set
        {
            this._scaleFactor = value;
        }
    }

    public double startParameter
    {
        get
        {
            return this._startParameter;
        }
        set
        {
            this._startParameter = value;
        }
    }

    public double endParameter
    {
        get
        {
            return this._endParameter;
        }
        set
        {
            this._endParameter = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/gml")]
public partial class ClothoidTypeRefLocation
{

    #region Private fields
    private AffinePlacementType _affinePlacement;
    #endregion

    public ClothoidTypeRefLocation()
    {
        this._affinePlacement = new AffinePlacementType();
    }

    public AffinePlacementType AffinePlacement
    {
        get
        {
            return this._affinePlacement;
        }
        set
        {
            this._affinePlacement = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class AffinePlacementType
{

    #region Private fields
    private DirectPositionType _location;

    private List<VectorType> _refDirection;

    private string _inDimension;

    private string _outDimension;
    #endregion

    public AffinePlacementType()
    {
        this._refDirection = new List<VectorType>();
        this._location = new DirectPositionType();
    }

    public DirectPositionType location
    {
        get
        {
            return this._location;
        }
        set
        {
            this._location = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("refDirection")]
    public List<VectorType> refDirection
    {
        get
        {
            return this._refDirection;
        }
        set
        {
            this._refDirection = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "positiveInteger")]
    public string inDimension
    {
        get
        {
            return this._inDimension;
        }
        set
        {
            this._inDimension = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "positiveInteger")]
    public string outDimension
    {
        get
        {
            return this._outDimension;
        }
        set
        {
            this._outDimension = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class OffsetCurveType : AbstractCurveSegmentType
{

    #region Private fields
    private CurvePropertyType _offsetBase;

    private LengthType _distance;

    private VectorType _refDirection;
    #endregion

    public OffsetCurveType()
    {
        this._refDirection = new VectorType();
        this._distance = new LengthType();
        this._offsetBase = new CurvePropertyType();
    }

    public CurvePropertyType offsetBase
    {
        get
        {
            return this._offsetBase;
        }
        set
        {
            this._offsetBase = value;
        }
    }

    public LengthType distance
    {
        get
        {
            return this._distance;
        }
        set
        {
            this._distance = value;
        }
    }

    public VectorType refDirection
    {
        get
        {
            return this._refDirection;
        }
        set
        {
            this._refDirection = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(CircleByCenterPointType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ArcByCenterPointType : AbstractCurveSegmentType
{

    #region Private fields
    private object _item;

    private ItemChoiceType1 _itemElementName;

    private LengthType _radius;

    private AngleType _startAngle;

    private AngleType _endAngle;

    private CurveInterpolationType _interpolation;

    private string _numArc;
    #endregion

    public ArcByCenterPointType()
    {
        this._endAngle = new AngleType();
        this._startAngle = new AngleType();
        this._radius = new LengthType();
        this._interpolation = CurveInterpolationType.circularArcCenterPointWithRadius;
        this._numArc = "1";
    }

    [System.Xml.Serialization.XmlElementAttribute("coordinates", typeof(CoordinatesType))]
    [System.Xml.Serialization.XmlElementAttribute("pointProperty", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pointRep", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pos", typeof(DirectPositionType))]
    [System.Xml.Serialization.XmlElementAttribute("posList", typeof(DirectPositionListType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemElementName")]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemChoiceType1 ItemElementName
    {
        get
        {
            return this._itemElementName;
        }
        set
        {
            this._itemElementName = value;
        }
    }

    public LengthType radius
    {
        get
        {
            return this._radius;
        }
        set
        {
            this._radius = value;
        }
    }

    public AngleType startAngle
    {
        get
        {
            return this._startAngle;
        }
        set
        {
            this._startAngle = value;
        }
    }

    public AngleType endAngle
    {
        get
        {
            return this._endAngle;
        }
        set
        {
            this._endAngle = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public CurveInterpolationType interpolation
    {
        get
        {
            return this._interpolation;
        }
        set
        {
            this._interpolation = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
    public string numArc
    {
        get
        {
            return this._numArc;
        }
        set
        {
            this._numArc = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml", IncludeInSchema = false)]
public enum ItemChoiceType1
{

    /// <remarks/>
    coordinates,

    /// <remarks/>
    pointProperty,

    /// <remarks/>
    pointRep,

    /// <remarks/>
    pos,

    /// <remarks/>
    posList,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CircleByCenterPointType : ArcByCenterPointType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(ArcByBulgeType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ArcStringByBulgeType : AbstractCurveSegmentType
{

    #region Private fields
    private object[] _items;

    private ItemsChoiceType4[] _itemsElementName;

    private List<double> _bulge;

    private List<VectorType> _normal;

    private CurveInterpolationType _interpolation;

    private string _numArc;
    #endregion

    public ArcStringByBulgeType()
    {
        this._normal = new List<VectorType>();
        this._bulge = new List<double>();
        this._interpolation = CurveInterpolationType.circularArc2PointWithBulge;
    }

    [System.Xml.Serialization.XmlElementAttribute("coordinates", typeof(CoordinatesType))]
    [System.Xml.Serialization.XmlElementAttribute("pointProperty", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pointRep", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pos", typeof(DirectPositionType))]
    [System.Xml.Serialization.XmlElementAttribute("posList", typeof(DirectPositionListType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public object[] Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType4[] ItemsElementName
    {
        get
        {
            return this._itemsElementName;
        }
        set
        {
            this._itemsElementName = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("bulge")]
    public List<double> bulge
    {
        get
        {
            return this._bulge;
        }
        set
        {
            this._bulge = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("normal")]
    public List<VectorType> normal
    {
        get
        {
            return this._normal;
        }
        set
        {
            this._normal = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public CurveInterpolationType interpolation
    {
        get
        {
            return this._interpolation;
        }
        set
        {
            this._interpolation = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
    public string numArc
    {
        get
        {
            return this._numArc;
        }
        set
        {
            this._numArc = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml", IncludeInSchema = false)]
public enum ItemsChoiceType4
{

    /// <remarks/>
    coordinates,

    /// <remarks/>
    pointProperty,

    /// <remarks/>
    pointRep,

    /// <remarks/>
    pos,

    /// <remarks/>
    posList,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ArcByBulgeType : ArcStringByBulgeType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(ArcType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CircleType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ArcStringType : AbstractCurveSegmentType
{

    #region Private fields
    private object[] _items;

    private ItemsChoiceType3[] _itemsElementName;

    private CurveInterpolationType _interpolation;

    private string _numArc;
    #endregion

    public ArcStringType()
    {
        this._interpolation = CurveInterpolationType.circularArc3Points;
    }

    [System.Xml.Serialization.XmlElementAttribute("coordinates", typeof(CoordinatesType))]
    [System.Xml.Serialization.XmlElementAttribute("pointProperty", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pointRep", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pos", typeof(DirectPositionType))]
    [System.Xml.Serialization.XmlElementAttribute("posList", typeof(DirectPositionListType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public object[] Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType3[] ItemsElementName
    {
        get
        {
            return this._itemsElementName;
        }
        set
        {
            this._itemsElementName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public CurveInterpolationType interpolation
    {
        get
        {
            return this._interpolation;
        }
        set
        {
            this._interpolation = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
    public string numArc
    {
        get
        {
            return this._numArc;
        }
        set
        {
            this._numArc = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml", IncludeInSchema = false)]
public enum ItemsChoiceType3
{

    /// <remarks/>
    coordinates,

    /// <remarks/>
    pointProperty,

    /// <remarks/>
    pointRep,

    /// <remarks/>
    pos,

    /// <remarks/>
    posList,
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(CircleType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ArcType : ArcStringType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CircleType : ArcType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class LineStringSegmentType : AbstractCurveSegmentType
{

    #region Private fields
    private object[] _items;

    private ItemsChoiceType2[] _itemsElementName;

    private CurveInterpolationType _interpolation;
    #endregion

    public LineStringSegmentType()
    {
        this._interpolation = CurveInterpolationType.linear;
    }

    [System.Xml.Serialization.XmlElementAttribute("coordinates", typeof(CoordinatesType))]
    [System.Xml.Serialization.XmlElementAttribute("pointProperty", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pointRep", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pos", typeof(DirectPositionType))]
    [System.Xml.Serialization.XmlElementAttribute("posList", typeof(DirectPositionListType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public object[] Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType2[] ItemsElementName
    {
        get
        {
            return this._itemsElementName;
        }
        set
        {
            this._itemsElementName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public CurveInterpolationType interpolation
    {
        get
        {
            return this._interpolation;
        }
        set
        {
            this._interpolation = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml", IncludeInSchema = false)]
public enum ItemsChoiceType2
{

    /// <remarks/>
    coordinates,

    /// <remarks/>
    pointProperty,

    /// <remarks/>
    pointRep,

    /// <remarks/>
    pos,

    /// <remarks/>
    posList,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CompositeCurveType : AbstractCurveType
{

    #region Private fields
    private List<CurvePropertyType> _curveMember;
    #endregion

    public CompositeCurveType()
    {
        this._curveMember = new List<CurvePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("curveMember")]
    public List<CurvePropertyType> curveMember
    {
        get
        {
            return this._curveMember;
        }
        set
        {
            this._curveMember = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class LineStringType : AbstractCurveType
{

    #region Private fields
    private object[] _items;

    private ItemsChoiceType1[] _itemsElementName;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("coord", typeof(CoordType))]
    [System.Xml.Serialization.XmlElementAttribute("coordinates", typeof(CoordinatesType))]
    [System.Xml.Serialization.XmlElementAttribute("pointProperty", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pointRep", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pos", typeof(DirectPositionType))]
    [System.Xml.Serialization.XmlElementAttribute("posList", typeof(DirectPositionListType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public object[] Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType1[] ItemsElementName
    {
        get
        {
            return this._itemsElementName;
        }
        set
        {
            this._itemsElementName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml", IncludeInSchema = false)]
public enum ItemsChoiceType1
{

    /// <remarks/>
    coord,

    /// <remarks/>
    coordinates,

    /// <remarks/>
    pointProperty,

    /// <remarks/>
    pointRep,

    /// <remarks/>
    pos,

    /// <remarks/>
    posList,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class LinearRingType : AbstractRingType
{

    #region Private fields
    private object[] _items;

    private ItemsChoiceType7[] _itemsElementName;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("coord", typeof(CoordType))]
    [System.Xml.Serialization.XmlElementAttribute("coordinates", typeof(CoordinatesType))]
    [System.Xml.Serialization.XmlElementAttribute("pointProperty", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pointRep", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pos", typeof(DirectPositionType))]
    [System.Xml.Serialization.XmlElementAttribute("posList", typeof(DirectPositionListType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public object[] Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType7[] ItemsElementName
    {
        get
        {
            return this._itemsElementName;
        }
        set
        {
            this._itemsElementName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml", IncludeInSchema = false)]
public enum ItemsChoiceType7
{

    /// <remarks/>
    coord,

    /// <remarks/>
    coordinates,

    /// <remarks/>
    pointProperty,

    /// <remarks/>
    pointRep,

    /// <remarks/>
    pos,

    /// <remarks/>
    posList,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum SurfaceInterpolationType
{

    /// <remarks/>
    none,

    /// <remarks/>
    planar,

    /// <remarks/>
    spherical,

    /// <remarks/>
    elliptical,

    /// <remarks/>
    conic,

    /// <remarks/>
    tin,

    /// <remarks/>
    parametricCurve,

    /// <remarks/>
    polynomialSpline,

    /// <remarks/>
    rationalSpline,

    /// <remarks/>
    triangulatedSpline,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TriangleType : AbstractSurfacePatchType
{

    #region Private fields
    private AbstractRingPropertyType _exterior;

    private SurfaceInterpolationType _interpolation;
    #endregion

    public TriangleType()
    {
        this._exterior = new AbstractRingPropertyType();
        this._interpolation = SurfaceInterpolationType.planar;
    }

    public AbstractRingPropertyType exterior
    {
        get
        {
            return this._exterior;
        }
        set
        {
            this._exterior = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public SurfaceInterpolationType interpolation
    {
        get
        {
            return this._interpolation;
        }
        set
        {
            this._interpolation = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class PolygonPatchType : AbstractSurfacePatchType
{

    #region Private fields
    private AbstractRingPropertyType _exterior;

    private List<AbstractRingPropertyType> _interior;

    private SurfaceInterpolationType _interpolation;
    #endregion

    public PolygonPatchType()
    {
        this._interior = new List<AbstractRingPropertyType>();
        this._exterior = new AbstractRingPropertyType();
        this._interpolation = SurfaceInterpolationType.planar;
    }

    public AbstractRingPropertyType exterior
    {
        get
        {
            return this._exterior;
        }
        set
        {
            this._exterior = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("interior")]
    public List<AbstractRingPropertyType> interior
    {
        get
        {
            return this._interior;
        }
        set
        {
            this._interior = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public SurfaceInterpolationType interpolation
    {
        get
        {
            return this._interpolation;
        }
        set
        {
            this._interpolation = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TrianglePatchArrayPropertyType : SurfacePatchArrayPropertyType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class PolygonPatchArrayPropertyType : SurfacePatchArrayPropertyType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(TinType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TriangulatedSurfaceType : SurfaceType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TinType : TriangulatedSurfaceType
{

    #region Private fields
    private List<LineStringSegmentType> _stopLines;

    private List<LineStringSegmentType> _breakLines;

    private LengthType _maxLength;

    private TinTypeControlPoint _controlPoint;
    #endregion

    public TinType()
    {
        this._controlPoint = new TinTypeControlPoint();
        this._maxLength = new LengthType();
        this._breakLines = new List<LineStringSegmentType>();
        this._stopLines = new List<LineStringSegmentType>();
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("LineStringSegment", typeof(LineStringSegmentType), IsNullable = false)]
    public List<LineStringSegmentType> stopLines
    {
        get
        {
            return this._stopLines;
        }
        set
        {
            this._stopLines = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("LineStringSegment", typeof(LineStringSegmentType), IsNullable = false)]
    public List<LineStringSegmentType> breakLines
    {
        get
        {
            return this._breakLines;
        }
        set
        {
            this._breakLines = value;
        }
    }

    public LengthType maxLength
    {
        get
        {
            return this._maxLength;
        }
        set
        {
            this._maxLength = value;
        }
    }

    public TinTypeControlPoint controlPoint
    {
        get
        {
            return this._controlPoint;
        }
        set
        {
            this._controlPoint = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/gml")]
public partial class TinTypeControlPoint
{

    #region Private fields
    private object _item;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("pointProperty", typeof(PointPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("pos", typeof(DirectPositionType))]
    [System.Xml.Serialization.XmlElementAttribute("posList", typeof(DirectPositionListType))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class PolyhedralSurfaceType : SurfaceType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CompositeSurfaceType : AbstractSurfaceType
{

    #region Private fields
    private List<SurfacePropertyType> _surfaceMember;
    #endregion

    public CompositeSurfaceType()
    {
        this._surfaceMember = new List<SurfacePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("surfaceMember")]
    public List<SurfacePropertyType> surfaceMember
    {
        get
        {
            return this._surfaceMember;
        }
        set
        {
            this._surfaceMember = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class PolygonType : AbstractSurfaceType
{

    #region Private fields
    private AbstractRingPropertyType _exterior;

    private List<AbstractRingPropertyType> _interior;
    #endregion

    public PolygonType()
    {
        this._interior = new List<AbstractRingPropertyType>();
        this._exterior = new AbstractRingPropertyType();
    }

    public AbstractRingPropertyType exterior
    {
        get
        {
            return this._exterior;
        }
        set
        {
            this._exterior = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("interior")]
    public List<AbstractRingPropertyType> interior
    {
        get
        {
            return this._interior;
        }
        set
        {
            this._interior = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CompositeSolidType : AbstractSolidType
{

    #region Private fields
    private List<SolidPropertyType> _solidMember;
    #endregion

    public CompositeSolidType()
    {
        this._solidMember = new List<SolidPropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("solidMember")]
    public List<SolidPropertyType> solidMember
    {
        get
        {
            return this._solidMember;
        }
        set
        {
            this._solidMember = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class SolidPropertyType
{

    #region Private fields
    private AbstractSolidType _SolidField;

    private string _remoteSchema;
    #endregion

    public SolidPropertyType()
    {
        this._SolidField = new AbstractSolidType();
    }

    public AbstractSolidType _Solid
    {
        get
        {
            return this._SolidField;
        }
        set
        {
            this._SolidField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DirectedFacePropertyType
{

    #region Private fields
    private FaceType _face;

    private SignType _orientation;

    private string _remoteSchema;
    #endregion

    public DirectedFacePropertyType()
    {
        this._face = new FaceType();
        this._orientation = SignType.Item1;
    }

    public FaceType Face
    {
        get
        {
            return this._face;
        }
        set
        {
            this._face = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(SignType.Item1)]
    public SignType orientation
    {
        get
        {
            return this._orientation;
        }
        set
        {
            this._orientation = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class FaceType : AbstractTopoPrimitiveType
{

    #region Private fields
    private List<DirectedEdgePropertyType> _directedEdge;

    private List<DirectedTopoSolidPropertyType> _directedTopoSolid;

    private SurfacePropertyType _surfaceProperty;
    #endregion

    public FaceType()
    {
        this._surfaceProperty = new SurfacePropertyType();
        this._directedTopoSolid = new List<DirectedTopoSolidPropertyType>();
        this._directedEdge = new List<DirectedEdgePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("directedEdge")]
    public List<DirectedEdgePropertyType> directedEdge
    {
        get
        {
            return this._directedEdge;
        }
        set
        {
            this._directedEdge = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("directedTopoSolid")]
    public List<DirectedTopoSolidPropertyType> directedTopoSolid
    {
        get
        {
            return this._directedTopoSolid;
        }
        set
        {
            this._directedTopoSolid = value;
        }
    }

    public SurfacePropertyType surfaceProperty
    {
        get
        {
            return this._surfaceProperty;
        }
        set
        {
            this._surfaceProperty = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DirectedTopoSolidPropertyType
{

    #region Private fields
    private TopoSolidType _topoSolid;

    private SignType _orientation;

    private string _remoteSchema;
    #endregion

    public DirectedTopoSolidPropertyType()
    {
        this._topoSolid = new TopoSolidType();
        this._orientation = SignType.Item1;
    }

    public TopoSolidType TopoSolid
    {
        get
        {
            return this._topoSolid;
        }
        set
        {
            this._topoSolid = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(SignType.Item1)]
    public SignType orientation
    {
        get
        {
            return this._orientation;
        }
        set
        {
            this._orientation = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TopoSolidType : AbstractTopoPrimitiveType
{

    #region Private fields
    private List<DirectedFacePropertyType> _directedFace;
    #endregion

    public TopoSolidType()
    {
        this._directedFace = new List<DirectedFacePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("directedFace")]
    public List<DirectedFacePropertyType> directedFace
    {
        get
        {
            return this._directedFace;
        }
        set
        {
            this._directedFace = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ContainerPropertyType
{

    #region Private fields
    private AbstractTopoPrimitiveType _item;

    private string _remoteSchema;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("Face", typeof(FaceType))]
    [System.Xml.Serialization.XmlElementAttribute("TopoSolid", typeof(TopoSolidType))]
    public AbstractTopoPrimitiveType Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TopoPrimitiveArrayAssociationType
{

    #region Private fields
    private List<AbstractTopoPrimitiveType> _items;
    #endregion

    public TopoPrimitiveArrayAssociationType()
    {
        this._items = new List<AbstractTopoPrimitiveType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("_TopoPrimitive")]
    public List<AbstractTopoPrimitiveType> Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TopoVolumeType : AbstractTopologyType
{

    #region Private fields
    private List<DirectedTopoSolidPropertyType> _directedTopoSolid;
    #endregion

    public TopoVolumeType()
    {
        this._directedTopoSolid = new List<DirectedTopoSolidPropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("directedTopoSolid")]
    public List<DirectedTopoSolidPropertyType> directedTopoSolid
    {
        get
        {
            return this._directedTopoSolid;
        }
        set
        {
            this._directedTopoSolid = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TopoSurfaceType : AbstractTopologyType
{

    #region Private fields
    private List<DirectedFacePropertyType> _directedFace;
    #endregion

    public TopoSurfaceType()
    {
        this._directedFace = new List<DirectedFacePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("directedFace")]
    public List<DirectedFacePropertyType> directedFace
    {
        get
        {
            return this._directedFace;
        }
        set
        {
            this._directedFace = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TopoCurveType : AbstractTopologyType
{

    #region Private fields
    private List<DirectedEdgePropertyType> _directedEdge;
    #endregion

    public TopoCurveType()
    {
        this._directedEdge = new List<DirectedEdgePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("directedEdge")]
    public List<DirectedEdgePropertyType> directedEdge
    {
        get
        {
            return this._directedEdge;
        }
        set
        {
            this._directedEdge = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TopoPointType : AbstractTopologyType
{

    #region Private fields
    private DirectedNodePropertyType _directedNode;
    #endregion

    public TopoPointType()
    {
        this._directedNode = new DirectedNodePropertyType();
    }

    public DirectedNodePropertyType directedNode
    {
        get
        {
            return this._directedNode;
        }
        set
        {
            this._directedNode = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(ValueArrayType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CompositeValueType : AbstractGMLType
{

    #region Private fields
    private List<ValuePropertyType> _valueComponent;

    private ValueArrayPropertyType _valueComponents;
    #endregion

    public CompositeValueType()
    {
        this._valueComponents = new ValueArrayPropertyType();
        this._valueComponent = new List<ValuePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("valueComponent")]
    public List<ValuePropertyType> valueComponent
    {
        get
        {
            return this._valueComponent;
        }
        set
        {
            this._valueComponent = value;
        }
    }

    public ValueArrayPropertyType valueComponents
    {
        get
        {
            return this._valueComponents;
        }
        set
        {
            this._valueComponents = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(CountPropertyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(QuantityPropertyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CategoryPropertyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(BooleanPropertyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ScalarValuePropertyType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ValuePropertyType
{

    #region Private fields
    private bool _boolean;

    private CodeType1 _category;

    private MeasureType _quantity;

    private string _count;

    private string _booleanList;

    private CodeOrNullListType _categoryList;

    private MeasureOrNullListType _quantityList;

    private string _countList;

    private CategoryExtentType _categoryExtent;

    private QuantityExtentType _quantityExtent;

    private string _countExtent;

    private CompositeValueType _compositeValue;

    private object _ObjectField;

    private string _null;

    private string _remoteSchema;
    #endregion

    public ValuePropertyType()
    {
        this._compositeValue = new CompositeValueType();
        this._quantityExtent = new QuantityExtentType();
        this._categoryExtent = new CategoryExtentType();
        this._quantityList = new MeasureOrNullListType();
        this._categoryList = new CodeOrNullListType();
        this._quantity = new MeasureType();
        this._category = new CodeType1();
    }

    public bool Boolean
    {
        get
        {
            return this._boolean;
        }
        set
        {
            this._boolean = value;
        }
    }

    public CodeType1 Category
    {
        get
        {
            return this._category;
        }
        set
        {
            this._category = value;
        }
    }

    public MeasureType Quantity
    {
        get
        {
            return this._quantity;
        }
        set
        {
            this._quantity = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "integer")]
    public string Count
    {
        get
        {
            return this._count;
        }
        set
        {
            this._count = value;
        }
    }

    public string BooleanList
    {
        get
        {
            return this._booleanList;
        }
        set
        {
            this._booleanList = value;
        }
    }

    public CodeOrNullListType CategoryList
    {
        get
        {
            return this._categoryList;
        }
        set
        {
            this._categoryList = value;
        }
    }

    public MeasureOrNullListType QuantityList
    {
        get
        {
            return this._quantityList;
        }
        set
        {
            this._quantityList = value;
        }
    }

    public string CountList
    {
        get
        {
            return this._countList;
        }
        set
        {
            this._countList = value;
        }
    }

    public CategoryExtentType CategoryExtent
    {
        get
        {
            return this._categoryExtent;
        }
        set
        {
            this._categoryExtent = value;
        }
    }

    public QuantityExtentType QuantityExtent
    {
        get
        {
            return this._quantityExtent;
        }
        set
        {
            this._quantityExtent = value;
        }
    }

    public string CountExtent
    {
        get
        {
            return this._countExtent;
        }
        set
        {
            this._countExtent = value;
        }
    }

    public CompositeValueType CompositeValue
    {
        get
        {
            return this._compositeValue;
        }
        set
        {
            this._compositeValue = value;
        }
    }

    public object _Object
    {
        get
        {
            return this._ObjectField;
        }
        set
        {
            this._ObjectField = value;
        }
    }

    public string Null
    {
        get
        {
            return this._null;
        }
        set
        {
            this._null = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(CategoryExtentType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CodeOrNullListType
{

    #region Private fields
    private string _codeSpace;

    private List<string> _text;
    #endregion

    public CodeOrNullListType()
    {
        this._text = new List<string>();
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string codeSpace
    {
        get
        {
            return this._codeSpace;
        }
        set
        {
            this._codeSpace = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public List<string> Text
    {
        get
        {
            return this._text;
        }
        set
        {
            this._text = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CategoryExtentType : CodeOrNullListType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(QuantityExtentType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MeasureOrNullListType
{

    #region Private fields
    private string _uom;

    private List<string> _text;
    #endregion

    public MeasureOrNullListType()
    {
        this._text = new List<string>();
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string uom
    {
        get
        {
            return this._uom;
        }
        set
        {
            this._uom = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public List<string> Text
    {
        get
        {
            return this._text;
        }
        set
        {
            this._text = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class QuantityExtentType : MeasureOrNullListType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CountPropertyType : ValuePropertyType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class QuantityPropertyType : ValuePropertyType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CategoryPropertyType : ValuePropertyType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class BooleanPropertyType : ValuePropertyType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ScalarValuePropertyType : ValuePropertyType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ValueArrayPropertyType
{

    #region Private fields
    private List<bool> _boolean;

    private List<CodeType1> _category;

    private List<MeasureType> _quantity;

    private List<string> _count;

    private List<string> _booleanList;

    private List<CodeOrNullListType> _categoryList;

    private List<MeasureOrNullListType> _quantityList;

    private List<string> _countList;

    private List<CategoryExtentType> _categoryExtent;

    private List<QuantityExtentType> _quantityExtent;

    private List<string> _countExtent;

    private List<CompositeValueType> _compositeValue;

    private List<object> _ObjectField;

    private List<string> _null;
    #endregion

    public ValueArrayPropertyType()
    {
        this._null = new List<string>();
        this._ObjectField = new List<object>();
        this._compositeValue = new List<CompositeValueType>();
        this._countExtent = new List<string>();
        this._quantityExtent = new List<QuantityExtentType>();
        this._categoryExtent = new List<CategoryExtentType>();
        this._countList = new List<string>();
        this._quantityList = new List<MeasureOrNullListType>();
        this._categoryList = new List<CodeOrNullListType>();
        this._booleanList = new List<string>();
        this._count = new List<string>();
        this._quantity = new List<MeasureType>();
        this._category = new List<CodeType1>();
        this._boolean = new List<bool>();
    }

    [System.Xml.Serialization.XmlElementAttribute("Boolean")]
    public List<bool> Boolean
    {
        get
        {
            return this._boolean;
        }
        set
        {
            this._boolean = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Category")]
    public List<CodeType1> Category
    {
        get
        {
            return this._category;
        }
        set
        {
            this._category = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Quantity")]
    public List<MeasureType> Quantity
    {
        get
        {
            return this._quantity;
        }
        set
        {
            this._quantity = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Count", DataType = "integer")]
    public List<string> Count
    {
        get
        {
            return this._count;
        }
        set
        {
            this._count = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("BooleanList")]
    public List<string> BooleanList
    {
        get
        {
            return this._booleanList;
        }
        set
        {
            this._booleanList = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("CategoryList")]
    public List<CodeOrNullListType> CategoryList
    {
        get
        {
            return this._categoryList;
        }
        set
        {
            this._categoryList = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("QuantityList")]
    public List<MeasureOrNullListType> QuantityList
    {
        get
        {
            return this._quantityList;
        }
        set
        {
            this._quantityList = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("CountList")]
    public List<string> CountList
    {
        get
        {
            return this._countList;
        }
        set
        {
            this._countList = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("CategoryExtent")]
    public List<CategoryExtentType> CategoryExtent
    {
        get
        {
            return this._categoryExtent;
        }
        set
        {
            this._categoryExtent = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("QuantityExtent")]
    public List<QuantityExtentType> QuantityExtent
    {
        get
        {
            return this._quantityExtent;
        }
        set
        {
            this._quantityExtent = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("CountExtent")]
    public List<string> CountExtent
    {
        get
        {
            return this._countExtent;
        }
        set
        {
            this._countExtent = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("CompositeValue")]
    public List<CompositeValueType> CompositeValue
    {
        get
        {
            return this._compositeValue;
        }
        set
        {
            this._compositeValue = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("_Object")]
    public List<object> _Object
    {
        get
        {
            return this._ObjectField;
        }
        set
        {
            this._ObjectField = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Null")]
    public List<string> Null
    {
        get
        {
            return this._null;
        }
        set
        {
            this._null = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ValueArrayType : CompositeValueType
{

    #region Private fields
    private string _codeSpace;

    private string _uom;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string codeSpace
    {
        get
        {
            return this._codeSpace;
        }
        set
        {
            this._codeSpace = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string uom
    {
        get
        {
            return this._uom;
        }
        set
        {
            this._uom = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(MovingObjectStatusType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractTimeSliceType : AbstractGMLType
{

    #region Private fields
    private TimePrimitivePropertyType _validTime;

    private StringOrRefType _dataSource;
    #endregion

    public AbstractTimeSliceType()
    {
        this._dataSource = new StringOrRefType();
        this._validTime = new TimePrimitivePropertyType();
    }

    public TimePrimitivePropertyType validTime
    {
        get
        {
            return this._validTime;
        }
        set
        {
            this._validTime = value;
        }
    }

    public StringOrRefType dataSource
    {
        get
        {
            return this._dataSource;
        }
        set
        {
            this._dataSource = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(RelatedTimeType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimePrimitivePropertyType
{

    #region Private fields
    private AbstractTimePrimitiveType _TimePrimitiveField;

    private string _remoteSchema;
    #endregion

    public AbstractTimePrimitiveType _TimePrimitive
    {
        get
        {
            return this._TimePrimitiveField;
        }
        set
        {
            this._TimePrimitiveField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTimeTopologyPrimitiveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeEdgeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeNodeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTimeGeometricPrimitiveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimePeriodType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeInstantType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractTimePrimitiveType : AbstractTimeObjectType
{

    #region Private fields
    private List<RelatedTimeType> _relatedTime;
    #endregion

    public AbstractTimePrimitiveType()
    {
        this._relatedTime = new List<RelatedTimeType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("relatedTime")]
    public List<RelatedTimeType> relatedTime
    {
        get
        {
            return this._relatedTime;
        }
        set
        {
            this._relatedTime = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class RelatedTimeType : TimePrimitivePropertyType
{

    #region Private fields
    private RelatedTimeTypeRelativePosition _relativePosition;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public RelatedTimeTypeRelativePosition relativePosition
    {
        get
        {
            return this._relativePosition;
        }
        set
        {
            this._relativePosition = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/gml")]
public enum RelatedTimeTypeRelativePosition
{

    /// <remarks/>
    Before,

    /// <remarks/>
    After,

    /// <remarks/>
    Begins,

    /// <remarks/>
    Ends,

    /// <remarks/>
    During,

    /// <remarks/>
    Equals,

    /// <remarks/>
    Contains,

    /// <remarks/>
    Overlaps,

    /// <remarks/>
    Meets,

    /// <remarks/>
    OverlappedBy,

    /// <remarks/>
    MetBy,

    /// <remarks/>
    BegunBy,

    /// <remarks/>
    EndedBy,
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTimeComplexType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeTopologyComplexType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTimePrimitiveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTimeTopologyPrimitiveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeEdgeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeNodeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTimeGeometricPrimitiveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimePeriodType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeInstantType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractTimeObjectType : AbstractGMLType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeTopologyComplexType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractTimeComplexType : AbstractTimeObjectType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeTopologyComplexType : AbstractTimeComplexType
{

    #region Private fields
    private List<TimeTopologyPrimitivePropertyType> _primitive;
    #endregion

    public TimeTopologyComplexType()
    {
        this._primitive = new List<TimeTopologyPrimitivePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("primitive")]
    public List<TimeTopologyPrimitivePropertyType> primitive
    {
        get
        {
            return this._primitive;
        }
        set
        {
            this._primitive = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeTopologyPrimitivePropertyType
{

    #region Private fields
    private AbstractTimeTopologyPrimitiveType _TimeTopologyPrimitiveField;

    private string _remoteSchema;
    #endregion

    public AbstractTimeTopologyPrimitiveType _TimeTopologyPrimitive
    {
        get
        {
            return this._TimeTopologyPrimitiveField;
        }
        set
        {
            this._TimeTopologyPrimitiveField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeEdgeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeNodeType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractTimeTopologyPrimitiveType : AbstractTimePrimitiveType
{

    #region Private fields
    private ReferenceType _complex;
    #endregion

    public AbstractTimeTopologyPrimitiveType()
    {
        this._complex = new ReferenceType();
    }

    public ReferenceType complex
    {
        get
        {
            return this._complex;
        }
        set
        {
            this._complex = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ReferenceType
{

    #region Private fields
    private string _remoteSchema;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeEdgeType : AbstractTimeTopologyPrimitiveType
{

    #region Private fields
    private TimeNodePropertyType _start;

    private TimeNodePropertyType _end;

    private TimePeriodPropertyType _extent;
    #endregion

    public TimeEdgeType()
    {
        this._extent = new TimePeriodPropertyType();
        this._end = new TimeNodePropertyType();
        this._start = new TimeNodePropertyType();
    }

    public TimeNodePropertyType start
    {
        get
        {
            return this._start;
        }
        set
        {
            this._start = value;
        }
    }

    public TimeNodePropertyType end
    {
        get
        {
            return this._end;
        }
        set
        {
            this._end = value;
        }
    }

    public TimePeriodPropertyType extent
    {
        get
        {
            return this._extent;
        }
        set
        {
            this._extent = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeNodePropertyType
{

    #region Private fields
    private TimeNodeType _timeNode;

    private string _remoteSchema;
    #endregion

    public TimeNodePropertyType()
    {
        this._timeNode = new TimeNodeType();
    }

    public TimeNodeType TimeNode
    {
        get
        {
            return this._timeNode;
        }
        set
        {
            this._timeNode = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeNodeType : AbstractTimeTopologyPrimitiveType
{

    #region Private fields
    private List<TimeEdgePropertyType> _previousEdge;

    private List<TimeEdgePropertyType> _nextEdge;

    private TimeInstantPropertyType _position;
    #endregion

    public TimeNodeType()
    {
        this._position = new TimeInstantPropertyType();
        this._nextEdge = new List<TimeEdgePropertyType>();
        this._previousEdge = new List<TimeEdgePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("previousEdge")]
    public List<TimeEdgePropertyType> previousEdge
    {
        get
        {
            return this._previousEdge;
        }
        set
        {
            this._previousEdge = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("nextEdge")]
    public List<TimeEdgePropertyType> nextEdge
    {
        get
        {
            return this._nextEdge;
        }
        set
        {
            this._nextEdge = value;
        }
    }

    public TimeInstantPropertyType position
    {
        get
        {
            return this._position;
        }
        set
        {
            this._position = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeEdgePropertyType
{

    #region Private fields
    private TimeEdgeType _timeEdge;

    private string _remoteSchema;
    #endregion

    public TimeEdgePropertyType()
    {
        this._timeEdge = new TimeEdgeType();
    }

    public TimeEdgeType TimeEdge
    {
        get
        {
            return this._timeEdge;
        }
        set
        {
            this._timeEdge = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeInstantPropertyType
{

    #region Private fields
    private TimeInstantType _timeInstant;

    private string _remoteSchema;
    #endregion

    public TimeInstantPropertyType()
    {
        this._timeInstant = new TimeInstantType();
    }

    public TimeInstantType TimeInstant
    {
        get
        {
            return this._timeInstant;
        }
        set
        {
            this._timeInstant = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeInstantType : AbstractTimeGeometricPrimitiveType
{

    #region Private fields
    private TimePositionType _timePosition;
    #endregion

    public TimeInstantType()
    {
        this._timePosition = new TimePositionType();
    }

    public TimePositionType timePosition
    {
        get
        {
            return this._timePosition;
        }
        set
        {
            this._timePosition = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimePeriodType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeInstantType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractTimeGeometricPrimitiveType : AbstractTimePrimitiveType
{

    #region Private fields
    private string _frame;
    #endregion

    public AbstractTimeGeometricPrimitiveType()
    {
        this._frame = "#ISO-8601";
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    [System.ComponentModel.DefaultValueAttribute("#ISO-8601")]
    public string frame
    {
        get
        {
            return this._frame;
        }
        set
        {
            this._frame = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimePeriodType : AbstractTimeGeometricPrimitiveType
{

    #region Private fields
    private object _item;

    private object _item1;

    private string _duration;

    private TimeIntervalLengthType _timeInterval;
    #endregion

    public TimePeriodType()
    {
        this._timeInterval = new TimeIntervalLengthType();
    }

    [System.Xml.Serialization.XmlElementAttribute("begin", typeof(TimeInstantPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("beginPosition", typeof(TimePositionType))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("end", typeof(TimeInstantPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("endPosition", typeof(TimePositionType))]
    public object Item1
    {
        get
        {
            return this._item1;
        }
        set
        {
            this._item1 = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "duration")]
    public string duration
    {
        get
        {
            return this._duration;
        }
        set
        {
            this._duration = value;
        }
    }

    public TimeIntervalLengthType timeInterval
    {
        get
        {
            return this._timeInterval;
        }
        set
        {
            this._timeInterval = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeIntervalLengthType
{

    #region Private fields
    private string _unit;

    private string _radix;

    private string _factor;

    private decimal _value;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string unit
    {
        get
        {
            return this._unit;
        }
        set
        {
            this._unit = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string radix
    {
        get
        {
            return this._radix;
        }
        set
        {
            this._radix = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
    public string factor
    {
        get
        {
            return this._factor;
        }
        set
        {
            this._factor = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public decimal Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimePeriodPropertyType
{

    #region Private fields
    private TimePeriodType _timePeriod;

    private string _remoteSchema;
    #endregion

    public TimePeriodPropertyType()
    {
        this._timePeriod = new TimePeriodType();
    }

    public TimePeriodType TimePeriod
    {
        get
        {
            return this._timePeriod;
        }
        set
        {
            this._timePeriod = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MovingObjectStatusType : AbstractTimeSliceType
{

    #region Private fields
    private LocationPropertyType _location;

    private MeasureType _speed;

    private DirectionPropertyType _bearing;

    private MeasureType _acceleration;

    private MeasureType _elevation;

    private StringOrRefType _status;
    #endregion

    public MovingObjectStatusType()
    {
        this._status = new StringOrRefType();
        this._elevation = new MeasureType();
        this._acceleration = new MeasureType();
        this._bearing = new DirectionPropertyType();
        this._speed = new MeasureType();
        this._location = new LocationPropertyType();
    }

    public LocationPropertyType location
    {
        get
        {
            return this._location;
        }
        set
        {
            this._location = value;
        }
    }

    public MeasureType speed
    {
        get
        {
            return this._speed;
        }
        set
        {
            this._speed = value;
        }
    }

    public DirectionPropertyType bearing
    {
        get
        {
            return this._bearing;
        }
        set
        {
            this._bearing = value;
        }
    }

    public MeasureType acceleration
    {
        get
        {
            return this._acceleration;
        }
        set
        {
            this._acceleration = value;
        }
    }

    public MeasureType elevation
    {
        get
        {
            return this._elevation;
        }
        set
        {
            this._elevation = value;
        }
    }

    public StringOrRefType status
    {
        get
        {
            return this._status;
        }
        set
        {
            this._status = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(PriorityLocationPropertyType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class LocationPropertyType
{

    #region Private fields
    private object _item;

    private string _remoteSchema;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("LocationKeyWord", typeof(CodeType1))]
    [System.Xml.Serialization.XmlElementAttribute("LocationString", typeof(StringOrRefType))]
    [System.Xml.Serialization.XmlElementAttribute("Null", typeof(string))]
    [System.Xml.Serialization.XmlElementAttribute("_Geometry", typeof(AbstractGeometryType))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class PriorityLocationPropertyType : LocationPropertyType
{

    #region Private fields
    private string _priority;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string priority
    {
        get
        {
            return this._priority;
        }
        set
        {
            this._priority = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DirectionPropertyType
{

    #region Private fields
    private object _item;

    private string _remoteSchema;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("CompassPoint", typeof(CompassPointEnumeration))]
    [System.Xml.Serialization.XmlElementAttribute("DirectionKeyword", typeof(CodeType1))]
    [System.Xml.Serialization.XmlElementAttribute("DirectionString", typeof(StringOrRefType))]
    [System.Xml.Serialization.XmlElementAttribute("DirectionVector", typeof(DirectionVectorType))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum CompassPointEnumeration
{

    /// <remarks/>
    N,

    /// <remarks/>
    NNE,

    /// <remarks/>
    NE,

    /// <remarks/>
    ENE,

    /// <remarks/>
    E,

    /// <remarks/>
    ESE,

    /// <remarks/>
    SE,

    /// <remarks/>
    SSE,

    /// <remarks/>
    S,

    /// <remarks/>
    SSW,

    /// <remarks/>
    SW,

    /// <remarks/>
    WSW,

    /// <remarks/>
    W,

    /// <remarks/>
    WNW,

    /// <remarks/>
    NW,

    /// <remarks/>
    NNW,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DirectionVectorType
{

    #region Private fields
    private object[] _items;

    private ItemsChoiceType9[] _itemsElementName;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("horizontalAngle", typeof(AngleType))]
    [System.Xml.Serialization.XmlElementAttribute("vector", typeof(VectorType))]
    [System.Xml.Serialization.XmlElementAttribute("verticalAngle", typeof(AngleType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public object[] Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType9[] ItemsElementName
    {
        get
        {
            return this._itemsElementName;
        }
        set
        {
            this._itemsElementName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml", IncludeInSchema = false)]
public enum ItemsChoiceType9
{

    /// <remarks/>
    horizontalAngle,

    /// <remarks/>
    vector,

    /// <remarks/>
    verticalAngle,
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(ObservationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DirectedObservationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DirectedObservationAtDistanceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractDiscreteCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(RectifiedGridCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GridCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSolidCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSurfaceCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiCurveCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPointCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractContinuousCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DynamicFeatureType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(BoundedFeatureType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractFeatureCollectionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(FeatureCollectionType1))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DynamicFeatureCollectionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(FeatureCollectionType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractFeatureType : AbstractGMLType
{

    #region Private fields
    private BoundingShapeType _boundedBy;

    private LocationPropertyType _location;
    #endregion

    public AbstractFeatureType()
    {
        this._location = new LocationPropertyType();
        this._boundedBy = new BoundingShapeType();
    }

    public BoundingShapeType boundedBy
    {
        get
        {
            return this._boundedBy;
        }
        set
        {
            this._boundedBy = value;
        }
    }

    public LocationPropertyType location
    {
        get
        {
            return this._location;
        }
        set
        {
            this._location = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class BoundingShapeType
{

    #region Private fields
    private object _item;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("Envelope", typeof(EnvelopeType))]
    [System.Xml.Serialization.XmlElementAttribute("Null", typeof(string))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(DirectedObservationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DirectedObservationAtDistanceType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ObservationType : AbstractFeatureType
{

    #region Private fields
    private TimePrimitivePropertyType _validTime;

    private FeaturePropertyType _using;

    private TargetPropertyType _target;

    private AssociationType _resultOf;
    #endregion

    public ObservationType()
    {
        this._resultOf = new AssociationType();
        this._target = new TargetPropertyType();
        this._using = new FeaturePropertyType();
        this._validTime = new TimePrimitivePropertyType();
    }

    public TimePrimitivePropertyType validTime
    {
        get
        {
            return this._validTime;
        }
        set
        {
            this._validTime = value;
        }
    }

    public FeaturePropertyType @using
    {
        get
        {
            return this._using;
        }
        set
        {
            this._using = value;
        }
    }

    public TargetPropertyType target
    {
        get
        {
            return this._target;
        }
        set
        {
            this._target = value;
        }
    }

    public AssociationType resultOf
    {
        get
        {
            return this._resultOf;
        }
        set
        {
            this._resultOf = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class FeaturePropertyType
{

    #region Private fields
    private AbstractFeatureType _FeatureField;

    private string _remoteSchema;
    #endregion

    public AbstractFeatureType _Feature
    {
        get
        {
            return this._FeatureField;
        }
        set
        {
            this._FeatureField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TargetPropertyType
{

    #region Private fields
    private AbstractGMLType _item;

    private string _remoteSchema;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("_Feature", typeof(AbstractFeatureType))]
    [System.Xml.Serialization.XmlElementAttribute("_Geometry", typeof(AbstractGeometryType))]
    public AbstractGMLType Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class AssociationType
{

    #region Private fields
    private object _ObjectField;

    private string _remoteSchema;
    #endregion

    public object _Object
    {
        get
        {
            return this._ObjectField;
        }
        set
        {
            this._ObjectField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(DirectedObservationAtDistanceType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DirectedObservationType : ObservationType
{

    #region Private fields
    private DirectionPropertyType _direction;
    #endregion

    public DirectedObservationType()
    {
        this._direction = new DirectionPropertyType();
    }

    public DirectionPropertyType direction
    {
        get
        {
            return this._direction;
        }
        set
        {
            this._direction = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DirectedObservationAtDistanceType : DirectedObservationType
{

    #region Private fields
    private MeasureType _distance;
    #endregion

    public DirectedObservationAtDistanceType()
    {
        this._distance = new MeasureType();
    }

    public MeasureType distance
    {
        get
        {
            return this._distance;
        }
        set
        {
            this._distance = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractDiscreteCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(RectifiedGridCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GridCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSolidCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSurfaceCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiCurveCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPointCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractContinuousCoverageType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractCoverageType : AbstractFeatureType
{

    #region Private fields
    private DomainSetType _domainSet;

    private RangeSetType _rangeSet;

    private string _dimension;
    #endregion

    public AbstractCoverageType()
    {
        this._rangeSet = new RangeSetType();
        this._domainSet = new DomainSetType();
    }

    public DomainSetType domainSet
    {
        get
        {
            return this._domainSet;
        }
        set
        {
            this._domainSet = value;
        }
    }

    public RangeSetType rangeSet
    {
        get
        {
            return this._rangeSet;
        }
        set
        {
            this._rangeSet = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string dimension
    {
        get
        {
            return this._dimension;
        }
        set
        {
            this._dimension = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(RectifiedGridDomainType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GridDomainType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSolidDomainType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSurfaceDomainType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiCurveDomainType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPointDomainType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DomainSetType
{

    #region Private fields
    private AbstractGMLType _item;

    private string _remoteSchema;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("_Geometry", typeof(AbstractGeometryType))]
    [System.Xml.Serialization.XmlElementAttribute("_TimeObject", typeof(AbstractTimeObjectType))]
    public AbstractGMLType Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class RectifiedGridDomainType : DomainSetType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GridDomainType : DomainSetType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MultiSolidDomainType : DomainSetType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MultiSurfaceDomainType : DomainSetType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MultiCurveDomainType : DomainSetType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MultiPointDomainType : DomainSetType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class RangeSetType
{

    #region Private fields
    private object[] _items;

    private ItemsChoiceType10[] _itemsElementName;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("BooleanList", typeof(string))]
    [System.Xml.Serialization.XmlElementAttribute("CategoryList", typeof(CodeOrNullListType))]
    [System.Xml.Serialization.XmlElementAttribute("CountList", typeof(string))]
    [System.Xml.Serialization.XmlElementAttribute("DataBlock", typeof(DataBlockType))]
    [System.Xml.Serialization.XmlElementAttribute("File", typeof(FileType))]
    [System.Xml.Serialization.XmlElementAttribute("QuantityList", typeof(MeasureOrNullListType))]
    [System.Xml.Serialization.XmlElementAttribute("ValueArray", typeof(ValueArrayType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public object[] Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType10[] ItemsElementName
    {
        get
        {
            return this._itemsElementName;
        }
        set
        {
            this._itemsElementName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DataBlockType
{

    #region Private fields
    private RangeParametersType _rangeParameters;

    private object _item;
    #endregion

    public DataBlockType()
    {
        this._rangeParameters = new RangeParametersType();
    }

    public RangeParametersType rangeParameters
    {
        get
        {
            return this._rangeParameters;
        }
        set
        {
            this._rangeParameters = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("doubleOrNullTupleList", typeof(string))]
    [System.Xml.Serialization.XmlElementAttribute("tupleList", typeof(CoordinatesType))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class RangeParametersType
{

    #region Private fields
    private bool _boolean;

    private CodeType1 _category;

    private MeasureType _quantity;

    private string _count;

    private string _booleanList;

    private CodeOrNullListType _categoryList;

    private MeasureOrNullListType _quantityList;

    private string _countList;

    private CategoryExtentType _categoryExtent;

    private QuantityExtentType _quantityExtent;

    private string _countExtent;

    private CompositeValueType _compositeValue;

    private string _remoteSchema;
    #endregion

    public RangeParametersType()
    {
        this._compositeValue = new CompositeValueType();
        this._quantityExtent = new QuantityExtentType();
        this._categoryExtent = new CategoryExtentType();
        this._quantityList = new MeasureOrNullListType();
        this._categoryList = new CodeOrNullListType();
        this._quantity = new MeasureType();
        this._category = new CodeType1();
    }

    public bool Boolean
    {
        get
        {
            return this._boolean;
        }
        set
        {
            this._boolean = value;
        }
    }

    public CodeType1 Category
    {
        get
        {
            return this._category;
        }
        set
        {
            this._category = value;
        }
    }

    public MeasureType Quantity
    {
        get
        {
            return this._quantity;
        }
        set
        {
            this._quantity = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "integer")]
    public string Count
    {
        get
        {
            return this._count;
        }
        set
        {
            this._count = value;
        }
    }

    public string BooleanList
    {
        get
        {
            return this._booleanList;
        }
        set
        {
            this._booleanList = value;
        }
    }

    public CodeOrNullListType CategoryList
    {
        get
        {
            return this._categoryList;
        }
        set
        {
            this._categoryList = value;
        }
    }

    public MeasureOrNullListType QuantityList
    {
        get
        {
            return this._quantityList;
        }
        set
        {
            this._quantityList = value;
        }
    }

    public string CountList
    {
        get
        {
            return this._countList;
        }
        set
        {
            this._countList = value;
        }
    }

    public CategoryExtentType CategoryExtent
    {
        get
        {
            return this._categoryExtent;
        }
        set
        {
            this._categoryExtent = value;
        }
    }

    public QuantityExtentType QuantityExtent
    {
        get
        {
            return this._quantityExtent;
        }
        set
        {
            this._quantityExtent = value;
        }
    }

    public string CountExtent
    {
        get
        {
            return this._countExtent;
        }
        set
        {
            this._countExtent = value;
        }
    }

    public CompositeValueType CompositeValue
    {
        get
        {
            return this._compositeValue;
        }
        set
        {
            this._compositeValue = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class FileType
{

    #region Private fields
    private RangeParametersType _rangeParameters;

    private string _fileName;

    private FileValueModelType _fileStructure;

    private string _mimeType;

    private string _compression;
    #endregion

    public FileType()
    {
        this._rangeParameters = new RangeParametersType();
    }

    public RangeParametersType rangeParameters
    {
        get
        {
            return this._rangeParameters;
        }
        set
        {
            this._rangeParameters = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "anyURI")]
    public string fileName
    {
        get
        {
            return this._fileName;
        }
        set
        {
            this._fileName = value;
        }
    }

    public FileValueModelType fileStructure
    {
        get
        {
            return this._fileStructure;
        }
        set
        {
            this._fileStructure = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "anyURI")]
    public string mimeType
    {
        get
        {
            return this._mimeType;
        }
        set
        {
            this._mimeType = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "anyURI")]
    public string compression
    {
        get
        {
            return this._compression;
        }
        set
        {
            this._compression = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum FileValueModelType
{

    /// <remarks/>
    [System.Xml.Serialization.XmlEnumAttribute("Record Interleaved")]
    RecordInterleaved,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml", IncludeInSchema = false)]
public enum ItemsChoiceType10
{

    /// <remarks/>
    BooleanList,

    /// <remarks/>
    CategoryList,

    /// <remarks/>
    CountList,

    /// <remarks/>
    DataBlock,

    /// <remarks/>
    File,

    /// <remarks/>
    QuantityList,

    /// <remarks/>
    ValueArray,
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(RectifiedGridCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GridCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSolidCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSurfaceCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiCurveCoverageType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPointCoverageType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractDiscreteCoverageType : AbstractCoverageType
{

    #region Private fields
    private CoverageFunctionType _coverageFunction;
    #endregion

    public AbstractDiscreteCoverageType()
    {
        this._coverageFunction = new CoverageFunctionType();
    }

    public CoverageFunctionType coverageFunction
    {
        get
        {
            return this._coverageFunction;
        }
        set
        {
            this._coverageFunction = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CoverageFunctionType
{

    #region Private fields
    private object _item;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("GridFunction", typeof(GridFunctionType))]
    [System.Xml.Serialization.XmlElementAttribute("MappingRule", typeof(StringOrRefType))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(IndexMapType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GridFunctionType
{

    #region Private fields
    private SequenceRuleType _sequenceRule;

    private string _startPoint;
    #endregion

    public GridFunctionType()
    {
        this._sequenceRule = new SequenceRuleType();
    }

    public SequenceRuleType sequenceRule
    {
        get
        {
            return this._sequenceRule;
        }
        set
        {
            this._sequenceRule = value;
        }
    }

    public string startPoint
    {
        get
        {
            return this._startPoint;
        }
        set
        {
            this._startPoint = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class SequenceRuleType
{

    #region Private fields
    private IncrementOrder _order;

    private SequenceRuleNames _value;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public IncrementOrder order
    {
        get
        {
            return this._order;
        }
        set
        {
            this._order = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public SequenceRuleNames Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum IncrementOrder
{

    /// <remarks/>
    [System.Xml.Serialization.XmlEnumAttribute("+x+y")]
    xy,

    /// <remarks/>
    [System.Xml.Serialization.XmlEnumAttribute("+y+x")]
    yx,

    /// <remarks/>
    [System.Xml.Serialization.XmlEnumAttribute("+x-y")]
    xy1,

    /// <remarks/>
    [System.Xml.Serialization.XmlEnumAttribute("-x-y")]
    xy2,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum SequenceRuleNames
{

    /// <remarks/>
    Linear,

    /// <remarks/>
    Boustrophedonic,

    /// <remarks/>
    [System.Xml.Serialization.XmlEnumAttribute("Cantor-diagonal")]
    Cantordiagonal,

    /// <remarks/>
    Spiral,

    /// <remarks/>
    Morton,

    /// <remarks/>
    Hilbert,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class IndexMapType : GridFunctionType
{

    #region Private fields
    private string _lookUpTable;
    #endregion

    public string lookUpTable
    {
        get
        {
            return this._lookUpTable;
        }
        set
        {
            this._lookUpTable = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class RectifiedGridCoverageType : AbstractDiscreteCoverageType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GridCoverageType : AbstractDiscreteCoverageType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MultiSolidCoverageType : AbstractDiscreteCoverageType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MultiSurfaceCoverageType : AbstractDiscreteCoverageType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MultiCurveCoverageType : AbstractDiscreteCoverageType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MultiPointCoverageType : AbstractDiscreteCoverageType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractContinuousCoverageType : AbstractCoverageType
{

    #region Private fields
    private CoverageFunctionType _coverageFunction;
    #endregion

    public AbstractContinuousCoverageType()
    {
        this._coverageFunction = new CoverageFunctionType();
    }

    public CoverageFunctionType coverageFunction
    {
        get
        {
            return this._coverageFunction;
        }
        set
        {
            this._coverageFunction = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DynamicFeatureType : AbstractFeatureType
{

    #region Private fields
    private TimePrimitivePropertyType _validTime;

    private HistoryPropertyType _history;

    private StringOrRefType _dataSource;
    #endregion

    public DynamicFeatureType()
    {
        this._dataSource = new StringOrRefType();
        this._history = new HistoryPropertyType();
        this._validTime = new TimePrimitivePropertyType();
    }

    public TimePrimitivePropertyType validTime
    {
        get
        {
            return this._validTime;
        }
        set
        {
            this._validTime = value;
        }
    }

    public HistoryPropertyType history
    {
        get
        {
            return this._history;
        }
        set
        {
            this._history = value;
        }
    }

    public StringOrRefType dataSource
    {
        get
        {
            return this._dataSource;
        }
        set
        {
            this._dataSource = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(TrackType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class HistoryPropertyType
{

    #region Private fields
    private List<AbstractTimeSliceType> _TimeSliceField;
    #endregion

    public HistoryPropertyType()
    {
        this._TimeSliceField = new List<AbstractTimeSliceType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("_TimeSlice")]
    public List<AbstractTimeSliceType> _TimeSlice
    {
        get
        {
            return this._TimeSliceField;
        }
        set
        {
            this._TimeSliceField = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TrackType : HistoryPropertyType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class BoundedFeatureType : AbstractFeatureType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(FeatureCollectionType1))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DynamicFeatureCollectionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(FeatureCollectionType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractFeatureCollectionType : AbstractFeatureType
{

    #region Private fields
    private List<FeaturePropertyType> _featureMember;

    private List<AbstractFeatureType> _featureMembers;
    #endregion

    public AbstractFeatureCollectionType()
    {
        this._featureMembers = new List<AbstractFeatureType>();
        this._featureMember = new List<FeaturePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("featureMember")]
    public List<FeaturePropertyType> featureMember
    {
        get
        {
            return this._featureMember;
        }
        set
        {
            this._featureMember = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("_Feature", IsNullable = false)]
    public List<AbstractFeatureType> featureMembers
    {
        get
        {
            return this._featureMembers;
        }
        set
        {
            this._featureMembers = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(DynamicFeatureCollectionType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(TypeName = "FeatureCollectionType", Namespace = "http://www.opengis.net/gml")]
public partial class FeatureCollectionType1 : AbstractFeatureCollectionType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DynamicFeatureCollectionType : FeatureCollectionType1
{

    #region Private fields
    private TimePrimitivePropertyType _validTime;

    private HistoryPropertyType _history;

    private StringOrRefType _dataSource;
    #endregion

    public DynamicFeatureCollectionType()
    {
        this._dataSource = new StringOrRefType();
        this._history = new HistoryPropertyType();
        this._validTime = new TimePrimitivePropertyType();
    }

    public TimePrimitivePropertyType validTime
    {
        get
        {
            return this._validTime;
        }
        set
        {
            this._validTime = value;
        }
    }

    public HistoryPropertyType history
    {
        get
        {
            return this._history;
        }
        set
        {
            this._history = value;
        }
    }

    public StringOrRefType dataSource
    {
        get
        {
            return this._dataSource;
        }
        set
        {
            this._dataSource = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("FeatureCollection", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class FeatureCollectionType : AbstractFeatureCollectionType
{

    #region Private fields
    private string _lockId;

    private System.DateTime _timeStamp;

    private string _numberOfFeatures;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string lockId
    {
        get
        {
            return this._lockId;
        }
        set
        {
            this._lockId = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public System.DateTime timeStamp
    {
        get
        {
            return this._timeStamp;
        }
        set
        {
            this._timeStamp = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "nonNegativeInteger")]
    public string numberOfFeatures
    {
        get
        {
            return this._numberOfFeatures;
        }
        set
        {
            this._numberOfFeatures = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeCalendarEraType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeOrdinalEraType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractTimeReferenceSystemType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeClockType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeCalendarType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeOrdinalReferenceSystemType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeCoordinateSystemType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeneralOperationParameterType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationParameterGroupBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationParameterGroupType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationParameterBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationParameterType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationMethodBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationMethodType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractCoordinateOperationBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractCoordinateOperationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeneralTransformationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TransformationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeneralConversionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConversionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PassThroughOperationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConcatenatedOperationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EllipsoidBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EllipsoidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PrimeMeridianBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PrimeMeridianType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractDatumBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeodeticDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalDatumBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(VerticalDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ImageDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EngineeringDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CoordinateSystemAxisBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CoordinateSystemAxisType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractCoordinateSystemBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractCoordinateSystemType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ObliqueCartesianCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CylindricalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolarCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(SphericalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(UserDefinedCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LinearCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(VerticalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CartesianCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EllipsoidalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractReferenceSystemBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractReferenceSystemType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ImageCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EngineeringCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeneralDerivedCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DerivedCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ProjectedCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeocentricCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(VerticalCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeographicCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompoundCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(UnitDefinitionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConventionalUnitType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DerivedUnitType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(BaseUnitType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DefinitionProxyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DictionaryType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DefinitionType : AbstractGMLType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeCalendarEraType : DefinitionType
{

    #region Private fields
    private StringOrRefType _referenceEvent;

    private System.DateTime _referenceDate;

    private decimal _julianReference;

    private TimePeriodPropertyType _epochOfUse;
    #endregion

    public TimeCalendarEraType()
    {
        this._epochOfUse = new TimePeriodPropertyType();
        this._referenceEvent = new StringOrRefType();
        this._referenceDate = new System.DateTime(0);
    }

    public StringOrRefType referenceEvent
    {
        get
        {
            return this._referenceEvent;
        }
        set
        {
            this._referenceEvent = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
    [System.ComponentModel.DefaultValueAttribute(typeof(System.DateTime), "0001-01-01")]
    public System.DateTime referenceDate
    {
        get
        {
            return this._referenceDate;
        }
        set
        {
            this._referenceDate = value;
        }
    }

    public decimal julianReference
    {
        get
        {
            return this._julianReference;
        }
        set
        {
            this._julianReference = value;
        }
    }

    public TimePeriodPropertyType epochOfUse
    {
        get
        {
            return this._epochOfUse;
        }
        set
        {
            this._epochOfUse = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeOrdinalEraType : DefinitionType
{

    #region Private fields
    private List<RelatedTimeType> _relatedTime;

    private TimeNodePropertyType _start;

    private TimeNodePropertyType _end;

    private TimePeriodPropertyType _extent;

    private List<TimeOrdinalEraPropertyType> _member;

    private ReferenceType _group;
    #endregion

    public TimeOrdinalEraType()
    {
        this._group = new ReferenceType();
        this._member = new List<TimeOrdinalEraPropertyType>();
        this._extent = new TimePeriodPropertyType();
        this._end = new TimeNodePropertyType();
        this._start = new TimeNodePropertyType();
        this._relatedTime = new List<RelatedTimeType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("relatedTime")]
    public List<RelatedTimeType> relatedTime
    {
        get
        {
            return this._relatedTime;
        }
        set
        {
            this._relatedTime = value;
        }
    }

    public TimeNodePropertyType start
    {
        get
        {
            return this._start;
        }
        set
        {
            this._start = value;
        }
    }

    public TimeNodePropertyType end
    {
        get
        {
            return this._end;
        }
        set
        {
            this._end = value;
        }
    }

    public TimePeriodPropertyType extent
    {
        get
        {
            return this._extent;
        }
        set
        {
            this._extent = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("member")]
    public List<TimeOrdinalEraPropertyType> member
    {
        get
        {
            return this._member;
        }
        set
        {
            this._member = value;
        }
    }

    public ReferenceType group
    {
        get
        {
            return this._group;
        }
        set
        {
            this._group = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeOrdinalEraPropertyType
{

    #region Private fields
    private TimeOrdinalEraType _timeOrdinalEra;

    private string _remoteSchema;
    #endregion

    public TimeOrdinalEraPropertyType()
    {
        this._timeOrdinalEra = new TimeOrdinalEraType();
    }

    public TimeOrdinalEraType TimeOrdinalEra
    {
        get
        {
            return this._timeOrdinalEra;
        }
        set
        {
            this._timeOrdinalEra = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeClockType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeCalendarType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeOrdinalReferenceSystemType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TimeCoordinateSystemType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractTimeReferenceSystemType : DefinitionType
{

    #region Private fields
    private string _domainOfValidity;
    #endregion

    public string domainOfValidity
    {
        get
        {
            return this._domainOfValidity;
        }
        set
        {
            this._domainOfValidity = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeClockType : AbstractTimeReferenceSystemType
{

    #region Private fields
    private StringOrRefType _referenceEvent;

    private System.DateTime _referenceTime;

    private System.DateTime _utcReference;

    private List<TimeCalendarPropertyType> _dateBasis;
    #endregion

    public TimeClockType()
    {
        this._dateBasis = new List<TimeCalendarPropertyType>();
        this._referenceEvent = new StringOrRefType();
    }

    public StringOrRefType referenceEvent
    {
        get
        {
            return this._referenceEvent;
        }
        set
        {
            this._referenceEvent = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "time")]
    public System.DateTime referenceTime
    {
        get
        {
            return this._referenceTime;
        }
        set
        {
            this._referenceTime = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "time")]
    public System.DateTime utcReference
    {
        get
        {
            return this._utcReference;
        }
        set
        {
            this._utcReference = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("dateBasis")]
    public List<TimeCalendarPropertyType> dateBasis
    {
        get
        {
            return this._dateBasis;
        }
        set
        {
            this._dateBasis = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeCalendarPropertyType
{

    #region Private fields
    private TimeCalendarType _timeCalendar;

    private string _remoteSchema;
    #endregion

    public TimeCalendarPropertyType()
    {
        this._timeCalendar = new TimeCalendarType();
    }

    public TimeCalendarType TimeCalendar
    {
        get
        {
            return this._timeCalendar;
        }
        set
        {
            this._timeCalendar = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeCalendarType : AbstractTimeReferenceSystemType
{

    #region Private fields
    private List<TimeCalendarEraPropertyType> _referenceFrame;
    #endregion

    public TimeCalendarType()
    {
        this._referenceFrame = new List<TimeCalendarEraPropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("referenceFrame")]
    public List<TimeCalendarEraPropertyType> referenceFrame
    {
        get
        {
            return this._referenceFrame;
        }
        set
        {
            this._referenceFrame = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeCalendarEraPropertyType
{

    #region Private fields
    private TimeCalendarEraType _timeCalendarEra;

    private string _remoteSchema;
    #endregion

    public TimeCalendarEraPropertyType()
    {
        this._timeCalendarEra = new TimeCalendarEraType();
    }

    public TimeCalendarEraType TimeCalendarEra
    {
        get
        {
            return this._timeCalendarEra;
        }
        set
        {
            this._timeCalendarEra = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeOrdinalReferenceSystemType : AbstractTimeReferenceSystemType
{

    #region Private fields
    private List<TimeOrdinalEraPropertyType> _component;
    #endregion

    public TimeOrdinalReferenceSystemType()
    {
        this._component = new List<TimeOrdinalEraPropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("component")]
    public List<TimeOrdinalEraPropertyType> component
    {
        get
        {
            return this._component;
        }
        set
        {
            this._component = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TimeCoordinateSystemType : AbstractTimeReferenceSystemType
{

    #region Private fields
    private object _item;

    private TimeIntervalLengthType _interval;
    #endregion

    public TimeCoordinateSystemType()
    {
        this._interval = new TimeIntervalLengthType();
    }

    [System.Xml.Serialization.XmlElementAttribute("origin", typeof(TimeInstantPropertyType))]
    [System.Xml.Serialization.XmlElementAttribute("originPosition", typeof(TimePositionType))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    public TimeIntervalLengthType interval
    {
        get
        {
            return this._interval;
        }
        set
        {
            this._interval = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationParameterGroupBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationParameterGroupType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationParameterBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationParameterType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractGeneralOperationParameterType : DefinitionType
{

    #region Private fields
    private string _minimumOccurs;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute(DataType = "nonNegativeInteger")]
    public string minimumOccurs
    {
        get
        {
            return this._minimumOccurs;
        }
        set
        {
            this._minimumOccurs = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationParameterGroupType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class OperationParameterGroupBaseType : AbstractGeneralOperationParameterType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class OperationParameterGroupType : OperationParameterGroupBaseType
{

    #region Private fields
    private List<IdentifierType> _groupID;

    private StringOrRefType _remarks;

    private string _maximumOccurs;

    private List<AbstractGeneralOperationParameterRefType> _includesParameter;
    #endregion

    public OperationParameterGroupType()
    {
        this._includesParameter = new List<AbstractGeneralOperationParameterRefType>();
        this._remarks = new StringOrRefType();
        this._groupID = new List<IdentifierType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("groupID")]
    public List<IdentifierType> groupID
    {
        get
        {
            return this._groupID;
        }
        set
        {
            this._groupID = value;
        }
    }

    public StringOrRefType remarks
    {
        get
        {
            return this._remarks;
        }
        set
        {
            this._remarks = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "positiveInteger")]
    public string maximumOccurs
    {
        get
        {
            return this._maximumOccurs;
        }
        set
        {
            this._maximumOccurs = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("includesParameter")]
    public List<AbstractGeneralOperationParameterRefType> includesParameter
    {
        get
        {
            return this._includesParameter;
        }
        set
        {
            this._includesParameter = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class IdentifierType
{

    #region Private fields
    private CodeType1 _name;

    private string _version;

    private StringOrRefType _remarks;
    #endregion

    public IdentifierType()
    {
        this._remarks = new StringOrRefType();
        this._name = new CodeType1();
    }

    public CodeType1 name
    {
        get
        {
            return this._name;
        }
        set
        {
            this._name = value;
        }
    }

    public string version
    {
        get
        {
            return this._version;
        }
        set
        {
            this._version = value;
        }
    }

    public StringOrRefType remarks
    {
        get
        {
            return this._remarks;
        }
        set
        {
            this._remarks = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class AbstractGeneralOperationParameterRefType
{

    #region Private fields
    private AbstractGeneralOperationParameterType _GeneralOperationParameterField;

    private string _remoteSchema;
    #endregion

    public AbstractGeneralOperationParameterType _GeneralOperationParameter
    {
        get
        {
            return this._GeneralOperationParameterField;
        }
        set
        {
            this._GeneralOperationParameterField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationParameterType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class OperationParameterBaseType : AbstractGeneralOperationParameterType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class OperationParameterType : OperationParameterBaseType
{

    #region Private fields
    private List<IdentifierType> _parameterID;

    private StringOrRefType _remarks;
    #endregion

    public OperationParameterType()
    {
        this._remarks = new StringOrRefType();
        this._parameterID = new List<IdentifierType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("parameterID")]
    public List<IdentifierType> parameterID
    {
        get
        {
            return this._parameterID;
        }
        set
        {
            this._parameterID = value;
        }
    }

    public StringOrRefType remarks
    {
        get
        {
            return this._remarks;
        }
        set
        {
            this._remarks = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(OperationMethodType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class OperationMethodBaseType : DefinitionType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class OperationMethodType : OperationMethodBaseType
{

    #region Private fields
    private List<IdentifierType> _methodID;

    private StringOrRefType _remarks;

    private CodeType1 _methodFormula;

    private string _sourceDimensions;

    private string _targetDimensions;

    private List<AbstractGeneralOperationParameterRefType> _usesParameter;
    #endregion

    public OperationMethodType()
    {
        this._usesParameter = new List<AbstractGeneralOperationParameterRefType>();
        this._methodFormula = new CodeType1();
        this._remarks = new StringOrRefType();
        this._methodID = new List<IdentifierType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("methodID")]
    public List<IdentifierType> methodID
    {
        get
        {
            return this._methodID;
        }
        set
        {
            this._methodID = value;
        }
    }

    public StringOrRefType remarks
    {
        get
        {
            return this._remarks;
        }
        set
        {
            this._remarks = value;
        }
    }

    public CodeType1 methodFormula
    {
        get
        {
            return this._methodFormula;
        }
        set
        {
            this._methodFormula = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "positiveInteger")]
    public string sourceDimensions
    {
        get
        {
            return this._sourceDimensions;
        }
        set
        {
            this._sourceDimensions = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "positiveInteger")]
    public string targetDimensions
    {
        get
        {
            return this._targetDimensions;
        }
        set
        {
            this._targetDimensions = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("usesParameter")]
    public List<AbstractGeneralOperationParameterRefType> usesParameter
    {
        get
        {
            return this._usesParameter;
        }
        set
        {
            this._usesParameter = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractCoordinateOperationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeneralTransformationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TransformationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeneralConversionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConversionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PassThroughOperationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConcatenatedOperationType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractCoordinateOperationBaseType : DefinitionType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeneralTransformationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TransformationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeneralConversionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConversionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PassThroughOperationType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConcatenatedOperationType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractCoordinateOperationType : AbstractCoordinateOperationBaseType
{

    #region Private fields
    private List<IdentifierType> _coordinateOperationID;

    private StringOrRefType _remarks;

    private string _operationVersion;

    private ExtentType _validArea;

    private string _scope;

    private List<AbstractPositionalAccuracyType> _positionalAccuracyField;

    private CRSRefType _sourceCRS;

    private CRSRefType _targetCRS;
    #endregion

    public AbstractCoordinateOperationType()
    {
        this._targetCRS = new CRSRefType();
        this._sourceCRS = new CRSRefType();
        this._positionalAccuracyField = new List<AbstractPositionalAccuracyType>();
        this._validArea = new ExtentType();
        this._remarks = new StringOrRefType();
        this._coordinateOperationID = new List<IdentifierType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("coordinateOperationID")]
    public List<IdentifierType> coordinateOperationID
    {
        get
        {
            return this._coordinateOperationID;
        }
        set
        {
            this._coordinateOperationID = value;
        }
    }

    public StringOrRefType remarks
    {
        get
        {
            return this._remarks;
        }
        set
        {
            this._remarks = value;
        }
    }

    public string operationVersion
    {
        get
        {
            return this._operationVersion;
        }
        set
        {
            this._operationVersion = value;
        }
    }

    public ExtentType validArea
    {
        get
        {
            return this._validArea;
        }
        set
        {
            this._validArea = value;
        }
    }

    public string scope
    {
        get
        {
            return this._scope;
        }
        set
        {
            this._scope = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("_positionalAccuracy")]
    public List<AbstractPositionalAccuracyType> _positionalAccuracy
    {
        get
        {
            return this._positionalAccuracyField;
        }
        set
        {
            this._positionalAccuracyField = value;
        }
    }

    public CRSRefType sourceCRS
    {
        get
        {
            return this._sourceCRS;
        }
        set
        {
            this._sourceCRS = value;
        }
    }

    public CRSRefType targetCRS
    {
        get
        {
            return this._targetCRS;
        }
        set
        {
            this._targetCRS = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ExtentType
{

    #region Private fields
    private StringOrRefType _description;

    private List<object> _items;

    private List<EnvelopeType> _verticalExtent;

    private List<TimePeriodType> _temporalExtent;
    #endregion

    public ExtentType()
    {
        this._temporalExtent = new List<TimePeriodType>();
        this._verticalExtent = new List<EnvelopeType>();
        this._items = new List<object>();
        this._description = new StringOrRefType();
    }

    public StringOrRefType description
    {
        get
        {
            return this._description;
        }
        set
        {
            this._description = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("boundingBox", typeof(EnvelopeType))]
    [System.Xml.Serialization.XmlElementAttribute("boundingPolygon", typeof(PolygonType))]
    public List<object> Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("verticalExtent")]
    public List<EnvelopeType> verticalExtent
    {
        get
        {
            return this._verticalExtent;
        }
        set
        {
            this._verticalExtent = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("temporalExtent")]
    public List<TimePeriodType> temporalExtent
    {
        get
        {
            return this._temporalExtent;
        }
        set
        {
            this._temporalExtent = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(CovarianceMatrixType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(RelativeInternalPositionalAccuracyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbsoluteExternalPositionalAccuracyType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractPositionalAccuracyType
{

    #region Private fields
    private CodeType1 _measureDescription;
    #endregion

    public AbstractPositionalAccuracyType()
    {
        this._measureDescription = new CodeType1();
    }

    public CodeType1 measureDescription
    {
        get
        {
            return this._measureDescription;
        }
        set
        {
            this._measureDescription = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CovarianceMatrixType : AbstractPositionalAccuracyType
{

    #region Private fields
    private List<UnitOfMeasureType> _unitOfMeasure;

    private List<CovarianceElementType> _includesElement;
    #endregion

    public CovarianceMatrixType()
    {
        this._includesElement = new List<CovarianceElementType>();
        this._unitOfMeasure = new List<UnitOfMeasureType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("unitOfMeasure")]
    public List<UnitOfMeasureType> unitOfMeasure
    {
        get
        {
            return this._unitOfMeasure;
        }
        set
        {
            this._unitOfMeasure = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("includesElement")]
    public List<CovarianceElementType> includesElement
    {
        get
        {
            return this._includesElement;
        }
        set
        {
            this._includesElement = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConversionToPreferredUnitType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DerivationUnitTermType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class UnitOfMeasureType
{

    #region Private fields
    private string _uom;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string uom
    {
        get
        {
            return this._uom;
        }
        set
        {
            this._uom = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ConversionToPreferredUnitType : UnitOfMeasureType
{

    #region Private fields
    private object _item;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("factor", typeof(double))]
    [System.Xml.Serialization.XmlElementAttribute("formula", typeof(FormulaType))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class FormulaType
{

    #region Private fields
    private double _a;

    private double _b;

    private double _c;

    private double _d;
    #endregion

    public double a
    {
        get
        {
            return this._a;
        }
        set
        {
            this._a = value;
        }
    }

    public double b
    {
        get
        {
            return this._b;
        }
        set
        {
            this._b = value;
        }
    }

    public double c
    {
        get
        {
            return this._c;
        }
        set
        {
            this._c = value;
        }
    }

    public double d
    {
        get
        {
            return this._d;
        }
        set
        {
            this._d = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DerivationUnitTermType : UnitOfMeasureType
{

    #region Private fields
    private string _exponent;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
    public string exponent
    {
        get
        {
            return this._exponent;
        }
        set
        {
            this._exponent = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CovarianceElementType
{

    #region Private fields
    private string _rowIndex;

    private string _columnIndex;

    private double _covariance;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute(DataType = "positiveInteger")]
    public string rowIndex
    {
        get
        {
            return this._rowIndex;
        }
        set
        {
            this._rowIndex = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "positiveInteger")]
    public string columnIndex
    {
        get
        {
            return this._columnIndex;
        }
        set
        {
            this._columnIndex = value;
        }
    }

    public double covariance
    {
        get
        {
            return this._covariance;
        }
        set
        {
            this._covariance = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class RelativeInternalPositionalAccuracyType : AbstractPositionalAccuracyType
{

    #region Private fields
    private MeasureType _result;
    #endregion

    public RelativeInternalPositionalAccuracyType()
    {
        this._result = new MeasureType();
    }

    public MeasureType result
    {
        get
        {
            return this._result;
        }
        set
        {
            this._result = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class AbsoluteExternalPositionalAccuracyType : AbstractPositionalAccuracyType
{

    #region Private fields
    private MeasureType _result;
    #endregion

    public AbsoluteExternalPositionalAccuracyType()
    {
        this._result = new MeasureType();
    }

    public MeasureType result
    {
        get
        {
            return this._result;
        }
        set
        {
            this._result = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CRSRefType
{

    #region Private fields
    private AbstractReferenceSystemType _CRSField;

    private string _remoteSchema;
    #endregion

    public AbstractReferenceSystemType _CRS
    {
        get
        {
            return this._CRSField;
        }
        set
        {
            this._CRSField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ImageCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EngineeringCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeneralDerivedCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DerivedCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ProjectedCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeocentricCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(VerticalCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeographicCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompoundCRSType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractReferenceSystemType : AbstractReferenceSystemBaseType
{

    #region Private fields
    private List<IdentifierType> _srsID;

    private StringOrRefType _remarks;

    private ExtentType _validArea;

    private string _scope;
    #endregion

    public AbstractReferenceSystemType()
    {
        this._validArea = new ExtentType();
        this._remarks = new StringOrRefType();
        this._srsID = new List<IdentifierType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("srsID")]
    public List<IdentifierType> srsID
    {
        get
        {
            return this._srsID;
        }
        set
        {
            this._srsID = value;
        }
    }

    public StringOrRefType remarks
    {
        get
        {
            return this._remarks;
        }
        set
        {
            this._remarks = value;
        }
    }

    public ExtentType validArea
    {
        get
        {
            return this._validArea;
        }
        set
        {
            this._validArea = value;
        }
    }

    public string scope
    {
        get
        {
            return this._scope;
        }
        set
        {
            this._scope = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractReferenceSystemType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ImageCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EngineeringCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeneralDerivedCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DerivedCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ProjectedCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeocentricCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(VerticalCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeographicCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CompoundCRSType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractReferenceSystemBaseType : DefinitionType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TemporalCRSType : AbstractReferenceSystemType
{

    #region Private fields
    private TemporalCSRefType _usesTemporalCS;

    private TemporalDatumRefType _usesTemporalDatum;
    #endregion

    public TemporalCRSType()
    {
        this._usesTemporalDatum = new TemporalDatumRefType();
        this._usesTemporalCS = new TemporalCSRefType();
    }

    public TemporalCSRefType usesTemporalCS
    {
        get
        {
            return this._usesTemporalCS;
        }
        set
        {
            this._usesTemporalCS = value;
        }
    }

    public TemporalDatumRefType usesTemporalDatum
    {
        get
        {
            return this._usesTemporalDatum;
        }
        set
        {
            this._usesTemporalDatum = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TemporalCSRefType
{

    #region Private fields
    private TemporalCSType _temporalCS;

    private string _remoteSchema;
    #endregion

    public TemporalCSRefType()
    {
        this._temporalCS = new TemporalCSType();
    }

    public TemporalCSType TemporalCS
    {
        get
        {
            return this._temporalCS;
        }
        set
        {
            this._temporalCS = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TemporalCSType : AbstractCoordinateSystemType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(ObliqueCartesianCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CylindricalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolarCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(SphericalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(UserDefinedCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LinearCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(VerticalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CartesianCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EllipsoidalCSType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractCoordinateSystemType : AbstractCoordinateSystemBaseType
{

    #region Private fields
    private List<IdentifierType> _csID;

    private StringOrRefType _remarks;

    private List<CoordinateSystemAxisRefType> _usesAxis;
    #endregion

    public AbstractCoordinateSystemType()
    {
        this._usesAxis = new List<CoordinateSystemAxisRefType>();
        this._remarks = new StringOrRefType();
        this._csID = new List<IdentifierType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("csID")]
    public List<IdentifierType> csID
    {
        get
        {
            return this._csID;
        }
        set
        {
            this._csID = value;
        }
    }

    public StringOrRefType remarks
    {
        get
        {
            return this._remarks;
        }
        set
        {
            this._remarks = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("usesAxis")]
    public List<CoordinateSystemAxisRefType> usesAxis
    {
        get
        {
            return this._usesAxis;
        }
        set
        {
            this._usesAxis = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CoordinateSystemAxisRefType
{

    #region Private fields
    private CoordinateSystemAxisType _coordinateSystemAxis;

    private string _remoteSchema;
    #endregion

    public CoordinateSystemAxisRefType()
    {
        this._coordinateSystemAxis = new CoordinateSystemAxisType();
    }

    public CoordinateSystemAxisType CoordinateSystemAxis
    {
        get
        {
            return this._coordinateSystemAxis;
        }
        set
        {
            this._coordinateSystemAxis = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CoordinateSystemAxisType : CoordinateSystemAxisBaseType
{

    #region Private fields
    private List<IdentifierType> _axisID;

    private StringOrRefType _remarks;

    private CodeType1 _axisAbbrev;

    private CodeType1 _axisDirection;

    private string _uom;
    #endregion

    public CoordinateSystemAxisType()
    {
        this._axisDirection = new CodeType1();
        this._axisAbbrev = new CodeType1();
        this._remarks = new StringOrRefType();
        this._axisID = new List<IdentifierType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("axisID")]
    public List<IdentifierType> axisID
    {
        get
        {
            return this._axisID;
        }
        set
        {
            this._axisID = value;
        }
    }

    public StringOrRefType remarks
    {
        get
        {
            return this._remarks;
        }
        set
        {
            this._remarks = value;
        }
    }

    public CodeType1 axisAbbrev
    {
        get
        {
            return this._axisAbbrev;
        }
        set
        {
            this._axisAbbrev = value;
        }
    }

    public CodeType1 axisDirection
    {
        get
        {
            return this._axisDirection;
        }
        set
        {
            this._axisDirection = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string uom
    {
        get
        {
            return this._uom;
        }
        set
        {
            this._uom = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(CoordinateSystemAxisType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class CoordinateSystemAxisBaseType : DefinitionType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractCoordinateSystemType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ObliqueCartesianCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CylindricalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolarCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(SphericalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(UserDefinedCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LinearCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(VerticalCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(CartesianCSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EllipsoidalCSType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractCoordinateSystemBaseType : DefinitionType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ObliqueCartesianCSType : AbstractCoordinateSystemType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CylindricalCSType : AbstractCoordinateSystemType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class PolarCSType : AbstractCoordinateSystemType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class SphericalCSType : AbstractCoordinateSystemType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class UserDefinedCSType : AbstractCoordinateSystemType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class LinearCSType : AbstractCoordinateSystemType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class VerticalCSType : AbstractCoordinateSystemType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CartesianCSType : AbstractCoordinateSystemType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class EllipsoidalCSType : AbstractCoordinateSystemType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TemporalDatumRefType
{

    #region Private fields
    private TemporalDatumType _temporalDatum;

    private string _remoteSchema;
    #endregion

    public TemporalDatumRefType()
    {
        this._temporalDatum = new TemporalDatumType();
    }

    public TemporalDatumType TemporalDatum
    {
        get
        {
            return this._temporalDatum;
        }
        set
        {
            this._temporalDatum = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TemporalDatumType : TemporalDatumBaseType
{

    #region Private fields
    private System.DateTime _origin;
    #endregion

    public System.DateTime origin
    {
        get
        {
            return this._origin;
        }
        set
        {
            this._origin = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalDatumType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class TemporalDatumBaseType : AbstractDatumType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeodeticDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalDatumBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(VerticalDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ImageDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EngineeringDatumType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractDatumType : AbstractDatumBaseType
{

    #region Private fields
    private List<IdentifierType> _datumID;

    private StringOrRefType _remarks;

    private CodeType1 _anchorPoint;

    private System.DateTime _realizationEpoch;

    private ExtentType _validArea;

    private string _scope;
    #endregion

    public AbstractDatumType()
    {
        this._validArea = new ExtentType();
        this._anchorPoint = new CodeType1();
        this._remarks = new StringOrRefType();
        this._datumID = new List<IdentifierType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("datumID")]
    public List<IdentifierType> datumID
    {
        get
        {
            return this._datumID;
        }
        set
        {
            this._datumID = value;
        }
    }

    public StringOrRefType remarks
    {
        get
        {
            return this._remarks;
        }
        set
        {
            this._remarks = value;
        }
    }

    public CodeType1 anchorPoint
    {
        get
        {
            return this._anchorPoint;
        }
        set
        {
            this._anchorPoint = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
    public System.DateTime realizationEpoch
    {
        get
        {
            return this._realizationEpoch;
        }
        set
        {
            this._realizationEpoch = value;
        }
    }

    public ExtentType validArea
    {
        get
        {
            return this._validArea;
        }
        set
        {
            this._validArea = value;
        }
    }

    public string scope
    {
        get
        {
            return this._scope;
        }
        set
        {
            this._scope = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeodeticDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalDatumBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(TemporalDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(VerticalDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ImageDatumType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(EngineeringDatumType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractDatumBaseType : DefinitionType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GeodeticDatumType : AbstractDatumType
{

    #region Private fields
    private PrimeMeridianRefType _usesPrimeMeridian;

    private EllipsoidRefType _usesEllipsoid;
    #endregion

    public GeodeticDatumType()
    {
        this._usesEllipsoid = new EllipsoidRefType();
        this._usesPrimeMeridian = new PrimeMeridianRefType();
    }

    public PrimeMeridianRefType usesPrimeMeridian
    {
        get
        {
            return this._usesPrimeMeridian;
        }
        set
        {
            this._usesPrimeMeridian = value;
        }
    }

    public EllipsoidRefType usesEllipsoid
    {
        get
        {
            return this._usesEllipsoid;
        }
        set
        {
            this._usesEllipsoid = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class PrimeMeridianRefType
{

    #region Private fields
    private PrimeMeridianType _primeMeridian;

    private string _remoteSchema;
    #endregion

    public PrimeMeridianRefType()
    {
        this._primeMeridian = new PrimeMeridianType();
    }

    public PrimeMeridianType PrimeMeridian
    {
        get
        {
            return this._primeMeridian;
        }
        set
        {
            this._primeMeridian = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class PrimeMeridianType : PrimeMeridianBaseType
{

    #region Private fields
    private List<IdentifierType> _meridianID;

    private StringOrRefType _remarks;

    private AngleChoiceType _greenwichLongitude;
    #endregion

    public PrimeMeridianType()
    {
        this._greenwichLongitude = new AngleChoiceType();
        this._remarks = new StringOrRefType();
        this._meridianID = new List<IdentifierType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("meridianID")]
    public List<IdentifierType> meridianID
    {
        get
        {
            return this._meridianID;
        }
        set
        {
            this._meridianID = value;
        }
    }

    public StringOrRefType remarks
    {
        get
        {
            return this._remarks;
        }
        set
        {
            this._remarks = value;
        }
    }

    public AngleChoiceType greenwichLongitude
    {
        get
        {
            return this._greenwichLongitude;
        }
        set
        {
            this._greenwichLongitude = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class AngleChoiceType
{

    #region Private fields
    private object _item;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("angle", typeof(MeasureType))]
    [System.Xml.Serialization.XmlElementAttribute("dmsAngle", typeof(DMSAngleType))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DMSAngleType
{

    #region Private fields
    private DegreesType _degrees;

    private object[] _items;

    private ItemsChoiceType8[] _itemsElementName;
    #endregion

    public DMSAngleType()
    {
        this._degrees = new DegreesType();
    }

    public DegreesType degrees
    {
        get
        {
            return this._degrees;
        }
        set
        {
            this._degrees = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("decimalMinutes", typeof(decimal))]
    [System.Xml.Serialization.XmlElementAttribute("minutes", typeof(string), DataType = "nonNegativeInteger")]
    [System.Xml.Serialization.XmlElementAttribute("seconds", typeof(decimal))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public object[] Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType8[] ItemsElementName
    {
        get
        {
            return this._itemsElementName;
        }
        set
        {
            this._itemsElementName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DegreesType
{

    #region Private fields
    private string _direction;

    private string _value;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string direction
    {
        get
        {
            return this._direction;
        }
        set
        {
            this._direction = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute(DataType = "nonNegativeInteger")]
    public string Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml", IncludeInSchema = false)]
public enum ItemsChoiceType8
{

    /// <remarks/>
    decimalMinutes,

    /// <remarks/>
    minutes,

    /// <remarks/>
    seconds,
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(PrimeMeridianType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class PrimeMeridianBaseType : DefinitionType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class EllipsoidRefType
{

    #region Private fields
    private EllipsoidType _ellipsoid;

    private string _remoteSchema;
    #endregion

    public EllipsoidRefType()
    {
        this._ellipsoid = new EllipsoidType();
    }

    public EllipsoidType Ellipsoid
    {
        get
        {
            return this._ellipsoid;
        }
        set
        {
            this._ellipsoid = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class EllipsoidType : EllipsoidBaseType
{

    #region Private fields
    private List<IdentifierType> _ellipsoidID;

    private StringOrRefType _remarks;

    private MeasureType _semiMajorAxis;

    private SecondDefiningParameterType _secondDefiningParameter;
    #endregion

    public EllipsoidType()
    {
        this._secondDefiningParameter = new SecondDefiningParameterType();
        this._semiMajorAxis = new MeasureType();
        this._remarks = new StringOrRefType();
        this._ellipsoidID = new List<IdentifierType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("ellipsoidID")]
    public List<IdentifierType> ellipsoidID
    {
        get
        {
            return this._ellipsoidID;
        }
        set
        {
            this._ellipsoidID = value;
        }
    }

    public StringOrRefType remarks
    {
        get
        {
            return this._remarks;
        }
        set
        {
            this._remarks = value;
        }
    }

    public MeasureType semiMajorAxis
    {
        get
        {
            return this._semiMajorAxis;
        }
        set
        {
            this._semiMajorAxis = value;
        }
    }

    public SecondDefiningParameterType secondDefiningParameter
    {
        get
        {
            return this._secondDefiningParameter;
        }
        set
        {
            this._secondDefiningParameter = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class SecondDefiningParameterType
{

    #region Private fields
    private object _item;

    private ItemChoiceType2 _itemElementName;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("inverseFlattening", typeof(MeasureType))]
    [System.Xml.Serialization.XmlElementAttribute("isSphere", typeof(isSphere))]
    [System.Xml.Serialization.XmlElementAttribute("semiMinorAxis", typeof(MeasureType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemElementName")]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemChoiceType2 ItemElementName
    {
        get
        {
            return this._itemElementName;
        }
        set
        {
            this._itemElementName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/gml")]
public enum isSphere
{

    /// <remarks/>
    sphere,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml", IncludeInSchema = false)]
public enum ItemChoiceType2
{

    /// <remarks/>
    inverseFlattening,

    /// <remarks/>
    isSphere,

    /// <remarks/>
    semiMinorAxis,
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(EllipsoidType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class EllipsoidBaseType : DefinitionType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class VerticalDatumType : AbstractDatumType
{

    #region Private fields
    private VerticalDatumTypeType _verticalDatumType;
    #endregion

    public VerticalDatumType()
    {
        this._verticalDatumType = new VerticalDatumTypeType();
    }

    public VerticalDatumTypeType verticalDatumType
    {
        get
        {
            return this._verticalDatumType;
        }
        set
        {
            this._verticalDatumType = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ImageDatumType : AbstractDatumType
{

    #region Private fields
    private PixelInCellType _pixelInCell;
    #endregion

    public ImageDatumType()
    {
        this._pixelInCell = new PixelInCellType();
    }

    public PixelInCellType pixelInCell
    {
        get
        {
            return this._pixelInCell;
        }
        set
        {
            this._pixelInCell = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class EngineeringDatumType : AbstractDatumType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ImageCRSType : AbstractReferenceSystemType
{

    #region Private fields
    private object _item;

    private ImageDatumRefType _usesImageDatum;
    #endregion

    public ImageCRSType()
    {
        this._usesImageDatum = new ImageDatumRefType();
    }

    [System.Xml.Serialization.XmlElementAttribute("usesCartesianCS", typeof(CartesianCSRefType))]
    [System.Xml.Serialization.XmlElementAttribute("usesObliqueCartesianCS", typeof(ObliqueCartesianCSRefType))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    public ImageDatumRefType usesImageDatum
    {
        get
        {
            return this._usesImageDatum;
        }
        set
        {
            this._usesImageDatum = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CartesianCSRefType
{

    #region Private fields
    private CartesianCSType _cartesianCS;

    private string _remoteSchema;
    #endregion

    public CartesianCSRefType()
    {
        this._cartesianCS = new CartesianCSType();
    }

    public CartesianCSType CartesianCS
    {
        get
        {
            return this._cartesianCS;
        }
        set
        {
            this._cartesianCS = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ObliqueCartesianCSRefType
{

    #region Private fields
    private ObliqueCartesianCSType _obliqueCartesianCS;

    private string _remoteSchema;
    #endregion

    public ObliqueCartesianCSRefType()
    {
        this._obliqueCartesianCS = new ObliqueCartesianCSType();
    }

    public ObliqueCartesianCSType ObliqueCartesianCS
    {
        get
        {
            return this._obliqueCartesianCS;
        }
        set
        {
            this._obliqueCartesianCS = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ImageDatumRefType
{

    #region Private fields
    private ImageDatumType _imageDatum;

    private string _remoteSchema;
    #endregion

    public ImageDatumRefType()
    {
        this._imageDatum = new ImageDatumType();
    }

    public ImageDatumType ImageDatum
    {
        get
        {
            return this._imageDatum;
        }
        set
        {
            this._imageDatum = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class EngineeringCRSType : AbstractReferenceSystemType
{

    #region Private fields
    private CoordinateSystemRefType _usesCS;

    private EngineeringDatumRefType _usesEngineeringDatum;
    #endregion

    public EngineeringCRSType()
    {
        this._usesEngineeringDatum = new EngineeringDatumRefType();
        this._usesCS = new CoordinateSystemRefType();
    }

    public CoordinateSystemRefType usesCS
    {
        get
        {
            return this._usesCS;
        }
        set
        {
            this._usesCS = value;
        }
    }

    public EngineeringDatumRefType usesEngineeringDatum
    {
        get
        {
            return this._usesEngineeringDatum;
        }
        set
        {
            this._usesEngineeringDatum = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CoordinateSystemRefType
{

    #region Private fields
    private AbstractCoordinateSystemType _CoordinateSystemField;

    private string _remoteSchema;
    #endregion

    public AbstractCoordinateSystemType _CoordinateSystem
    {
        get
        {
            return this._CoordinateSystemField;
        }
        set
        {
            this._CoordinateSystemField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class EngineeringDatumRefType
{

    #region Private fields
    private EngineeringDatumType _engineeringDatum;

    private string _remoteSchema;
    #endregion

    public EngineeringDatumRefType()
    {
        this._engineeringDatum = new EngineeringDatumType();
    }

    public EngineeringDatumType EngineeringDatum
    {
        get
        {
            return this._engineeringDatum;
        }
        set
        {
            this._engineeringDatum = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(DerivedCRSType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ProjectedCRSType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractGeneralDerivedCRSType : AbstractReferenceSystemType
{

    #region Private fields
    private CoordinateReferenceSystemRefType _baseCRS;

    private GeneralConversionRefType _definedByConversion;
    #endregion

    public AbstractGeneralDerivedCRSType()
    {
        this._definedByConversion = new GeneralConversionRefType();
        this._baseCRS = new CoordinateReferenceSystemRefType();
    }

    public CoordinateReferenceSystemRefType baseCRS
    {
        get
        {
            return this._baseCRS;
        }
        set
        {
            this._baseCRS = value;
        }
    }

    public GeneralConversionRefType definedByConversion
    {
        get
        {
            return this._definedByConversion;
        }
        set
        {
            this._definedByConversion = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CoordinateReferenceSystemRefType
{

    #region Private fields
    private AbstractReferenceSystemType _CoordinateReferenceSystemField;

    private string _remoteSchema;
    #endregion

    public AbstractReferenceSystemType _CoordinateReferenceSystem
    {
        get
        {
            return this._CoordinateReferenceSystemField;
        }
        set
        {
            this._CoordinateReferenceSystemField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GeneralConversionRefType
{

    #region Private fields
    private AbstractGeneralConversionType _GeneralConversionField;

    private string _remoteSchema;
    #endregion

    public AbstractGeneralConversionType _GeneralConversion
    {
        get
        {
            return this._GeneralConversionField;
        }
        set
        {
            this._GeneralConversionField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConversionType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractGeneralConversionType : AbstractCoordinateOperationType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ConversionType : AbstractGeneralConversionType
{

    #region Private fields
    private OperationMethodRefType _usesMethod;

    private List<ParameterValueType> _usesValue;
    #endregion

    public ConversionType()
    {
        this._usesValue = new List<ParameterValueType>();
        this._usesMethod = new OperationMethodRefType();
    }

    public OperationMethodRefType usesMethod
    {
        get
        {
            return this._usesMethod;
        }
        set
        {
            this._usesMethod = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("usesValue")]
    public List<ParameterValueType> usesValue
    {
        get
        {
            return this._usesValue;
        }
        set
        {
            this._usesValue = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class OperationMethodRefType
{

    #region Private fields
    private OperationMethodType _operationMethod;

    private string _remoteSchema;
    #endregion

    public OperationMethodRefType()
    {
        this._operationMethod = new OperationMethodType();
    }

    public OperationMethodType OperationMethod
    {
        get
        {
            return this._operationMethod;
        }
        set
        {
            this._operationMethod = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ParameterValueType : AbstractGeneralParameterValueType
{

    #region Private fields
    private object _item;

    private ItemChoiceType3 _itemElementName;

    private OperationParameterRefType _valueOfParameter;
    #endregion

    public ParameterValueType()
    {
        this._valueOfParameter = new OperationParameterRefType();
    }

    [System.Xml.Serialization.XmlElementAttribute("booleanValue", typeof(bool))]
    [System.Xml.Serialization.XmlElementAttribute("dmsAngleValue", typeof(DMSAngleType))]
    [System.Xml.Serialization.XmlElementAttribute("integerValue", typeof(string), DataType = "positiveInteger")]
    [System.Xml.Serialization.XmlElementAttribute("integerValueList", typeof(string), DataType = "integer")]
    [System.Xml.Serialization.XmlElementAttribute("stringValue", typeof(string))]
    [System.Xml.Serialization.XmlElementAttribute("value", typeof(MeasureType))]
    [System.Xml.Serialization.XmlElementAttribute("valueFile", typeof(string), DataType = "anyURI")]
    [System.Xml.Serialization.XmlElementAttribute("valueList", typeof(MeasureListType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemElementName")]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemChoiceType3 ItemElementName
    {
        get
        {
            return this._itemElementName;
        }
        set
        {
            this._itemElementName = value;
        }
    }

    public OperationParameterRefType valueOfParameter
    {
        get
        {
            return this._valueOfParameter;
        }
        set
        {
            this._valueOfParameter = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MeasureListType
{

    #region Private fields
    private string _uom;

    private List<double> _text;
    #endregion

    public MeasureListType()
    {
        this._text = new List<double>();
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string uom
    {
        get
        {
            return this._uom;
        }
        set
        {
            this._uom = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public List<double> Text
    {
        get
        {
            return this._text;
        }
        set
        {
            this._text = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml", IncludeInSchema = false)]
public enum ItemChoiceType3
{

    /// <remarks/>
    booleanValue,

    /// <remarks/>
    dmsAngleValue,

    /// <remarks/>
    integerValue,

    /// <remarks/>
    integerValueList,

    /// <remarks/>
    stringValue,

    /// <remarks/>
    value,

    /// <remarks/>
    valueFile,

    /// <remarks/>
    valueList,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class OperationParameterRefType
{

    #region Private fields
    private OperationParameterType _operationParameter;

    private string _remoteSchema;
    #endregion

    public OperationParameterRefType()
    {
        this._operationParameter = new OperationParameterType();
    }

    public OperationParameterType OperationParameter
    {
        get
        {
            return this._operationParameter;
        }
        set
        {
            this._operationParameter = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(ParameterValueGroupType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(ParameterValueType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractGeneralParameterValueType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ParameterValueGroupType : AbstractGeneralParameterValueType
{

    #region Private fields
    private List<AbstractGeneralParameterValueType> _includesValue;

    private OperationParameterGroupRefType _valuesOfGroup;
    #endregion

    public ParameterValueGroupType()
    {
        this._valuesOfGroup = new OperationParameterGroupRefType();
        this._includesValue = new List<AbstractGeneralParameterValueType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("includesValue")]
    public List<AbstractGeneralParameterValueType> includesValue
    {
        get
        {
            return this._includesValue;
        }
        set
        {
            this._includesValue = value;
        }
    }

    public OperationParameterGroupRefType valuesOfGroup
    {
        get
        {
            return this._valuesOfGroup;
        }
        set
        {
            this._valuesOfGroup = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class OperationParameterGroupRefType
{

    #region Private fields
    private OperationParameterGroupType _operationParameterGroup;

    private string _remoteSchema;
    #endregion

    public OperationParameterGroupRefType()
    {
        this._operationParameterGroup = new OperationParameterGroupType();
    }

    public OperationParameterGroupType OperationParameterGroup
    {
        get
        {
            return this._operationParameterGroup;
        }
        set
        {
            this._operationParameterGroup = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DerivedCRSType : AbstractGeneralDerivedCRSType
{

    #region Private fields
    private DerivedCRSTypeType _derivedCRSType;

    private CoordinateSystemRefType _usesCS;
    #endregion

    public DerivedCRSType()
    {
        this._usesCS = new CoordinateSystemRefType();
        this._derivedCRSType = new DerivedCRSTypeType();
    }

    public DerivedCRSTypeType derivedCRSType
    {
        get
        {
            return this._derivedCRSType;
        }
        set
        {
            this._derivedCRSType = value;
        }
    }

    public CoordinateSystemRefType usesCS
    {
        get
        {
            return this._usesCS;
        }
        set
        {
            this._usesCS = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ProjectedCRSType : AbstractGeneralDerivedCRSType
{

    #region Private fields
    private CartesianCSRefType _usesCartesianCS;
    #endregion

    public ProjectedCRSType()
    {
        this._usesCartesianCS = new CartesianCSRefType();
    }

    public CartesianCSRefType usesCartesianCS
    {
        get
        {
            return this._usesCartesianCS;
        }
        set
        {
            this._usesCartesianCS = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GeocentricCRSType : AbstractReferenceSystemType
{

    #region Private fields
    private object _item;

    private GeodeticDatumRefType _usesGeodeticDatum;
    #endregion

    public GeocentricCRSType()
    {
        this._usesGeodeticDatum = new GeodeticDatumRefType();
    }

    [System.Xml.Serialization.XmlElementAttribute("usesCartesianCS", typeof(CartesianCSRefType))]
    [System.Xml.Serialization.XmlElementAttribute("usesSphericalCS", typeof(SphericalCSRefType))]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    public GeodeticDatumRefType usesGeodeticDatum
    {
        get
        {
            return this._usesGeodeticDatum;
        }
        set
        {
            this._usesGeodeticDatum = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class SphericalCSRefType
{

    #region Private fields
    private SphericalCSType _sphericalCS;

    private string _remoteSchema;
    #endregion

    public SphericalCSRefType()
    {
        this._sphericalCS = new SphericalCSType();
    }

    public SphericalCSType SphericalCS
    {
        get
        {
            return this._sphericalCS;
        }
        set
        {
            this._sphericalCS = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GeodeticDatumRefType
{

    #region Private fields
    private GeodeticDatumType _geodeticDatum;

    private string _remoteSchema;
    #endregion

    public GeodeticDatumRefType()
    {
        this._geodeticDatum = new GeodeticDatumType();
    }

    public GeodeticDatumType GeodeticDatum
    {
        get
        {
            return this._geodeticDatum;
        }
        set
        {
            this._geodeticDatum = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class VerticalCRSType : AbstractReferenceSystemType
{

    #region Private fields
    private VerticalCSRefType _usesVerticalCS;

    private VerticalDatumRefType _usesVerticalDatum;
    #endregion

    public VerticalCRSType()
    {
        this._usesVerticalDatum = new VerticalDatumRefType();
        this._usesVerticalCS = new VerticalCSRefType();
    }

    public VerticalCSRefType usesVerticalCS
    {
        get
        {
            return this._usesVerticalCS;
        }
        set
        {
            this._usesVerticalCS = value;
        }
    }

    public VerticalDatumRefType usesVerticalDatum
    {
        get
        {
            return this._usesVerticalDatum;
        }
        set
        {
            this._usesVerticalDatum = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class VerticalCSRefType
{

    #region Private fields
    private VerticalCSType _verticalCS;

    private string _remoteSchema;
    #endregion

    public VerticalCSRefType()
    {
        this._verticalCS = new VerticalCSType();
    }

    public VerticalCSType VerticalCS
    {
        get
        {
            return this._verticalCS;
        }
        set
        {
            this._verticalCS = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class VerticalDatumRefType
{

    #region Private fields
    private VerticalDatumType _verticalDatum;

    private string _remoteSchema;
    #endregion

    public VerticalDatumRefType()
    {
        this._verticalDatum = new VerticalDatumType();
    }

    public VerticalDatumType VerticalDatum
    {
        get
        {
            return this._verticalDatum;
        }
        set
        {
            this._verticalDatum = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GeographicCRSType : AbstractReferenceSystemType
{

    #region Private fields
    private EllipsoidalCSRefType _usesEllipsoidalCS;

    private GeodeticDatumRefType _usesGeodeticDatum;
    #endregion

    public GeographicCRSType()
    {
        this._usesGeodeticDatum = new GeodeticDatumRefType();
        this._usesEllipsoidalCS = new EllipsoidalCSRefType();
    }

    public EllipsoidalCSRefType usesEllipsoidalCS
    {
        get
        {
            return this._usesEllipsoidalCS;
        }
        set
        {
            this._usesEllipsoidalCS = value;
        }
    }

    public GeodeticDatumRefType usesGeodeticDatum
    {
        get
        {
            return this._usesGeodeticDatum;
        }
        set
        {
            this._usesGeodeticDatum = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class EllipsoidalCSRefType
{

    #region Private fields
    private EllipsoidalCSType _ellipsoidalCS;

    private string _remoteSchema;
    #endregion

    public EllipsoidalCSRefType()
    {
        this._ellipsoidalCS = new EllipsoidalCSType();
    }

    public EllipsoidalCSType EllipsoidalCS
    {
        get
        {
            return this._ellipsoidalCS;
        }
        set
        {
            this._ellipsoidalCS = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class CompoundCRSType : AbstractReferenceSystemType
{

    #region Private fields
    private List<CoordinateReferenceSystemRefType> _includesCRS;
    #endregion

    public CompoundCRSType()
    {
        this._includesCRS = new List<CoordinateReferenceSystemRefType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("includesCRS")]
    public List<CoordinateReferenceSystemRefType> includesCRS
    {
        get
        {
            return this._includesCRS;
        }
        set
        {
            this._includesCRS = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(TransformationType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractGeneralTransformationType : AbstractCoordinateOperationType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class TransformationType : AbstractGeneralTransformationType
{

    #region Private fields
    private OperationMethodRefType _usesMethod;

    private List<ParameterValueType> _usesValue;
    #endregion

    public TransformationType()
    {
        this._usesValue = new List<ParameterValueType>();
        this._usesMethod = new OperationMethodRefType();
    }

    public OperationMethodRefType usesMethod
    {
        get
        {
            return this._usesMethod;
        }
        set
        {
            this._usesMethod = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("usesValue")]
    public List<ParameterValueType> usesValue
    {
        get
        {
            return this._usesValue;
        }
        set
        {
            this._usesValue = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class PassThroughOperationType : AbstractCoordinateOperationType
{

    #region Private fields
    private List<string> _modifiedCoordinate;

    private OperationRefType _usesOperation;
    #endregion

    public PassThroughOperationType()
    {
        this._usesOperation = new OperationRefType();
        this._modifiedCoordinate = new List<string>();
    }

    [System.Xml.Serialization.XmlElementAttribute("modifiedCoordinate", DataType = "positiveInteger")]
    public List<string> modifiedCoordinate
    {
        get
        {
            return this._modifiedCoordinate;
        }
        set
        {
            this._modifiedCoordinate = value;
        }
    }

    public OperationRefType usesOperation
    {
        get
        {
            return this._usesOperation;
        }
        set
        {
            this._usesOperation = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class OperationRefType
{

    #region Private fields
    private AbstractCoordinateOperationType _OperationField;

    private string _remoteSchema;
    #endregion

    public AbstractCoordinateOperationType _Operation
    {
        get
        {
            return this._OperationField;
        }
        set
        {
            this._OperationField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ConcatenatedOperationType : AbstractCoordinateOperationType
{

    #region Private fields
    private List<SingleOperationRefType> _usesSingleOperation;
    #endregion

    public ConcatenatedOperationType()
    {
        this._usesSingleOperation = new List<SingleOperationRefType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("usesSingleOperation")]
    public List<SingleOperationRefType> usesSingleOperation
    {
        get
        {
            return this._usesSingleOperation;
        }
        set
        {
            this._usesSingleOperation = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class SingleOperationRefType
{

    #region Private fields
    private AbstractCoordinateOperationType _SingleOperationField;

    private string _remoteSchema;
    #endregion

    public AbstractCoordinateOperationType _SingleOperation
    {
        get
        {
            return this._SingleOperationField;
        }
        set
        {
            this._SingleOperationField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(ConventionalUnitType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(DerivedUnitType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(BaseUnitType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class UnitDefinitionType : DefinitionType
{

    #region Private fields
    private StringOrRefType _quantityType;

    private CodeType1 _catalogSymbol;
    #endregion

    public UnitDefinitionType()
    {
        this._catalogSymbol = new CodeType1();
        this._quantityType = new StringOrRefType();
    }

    public StringOrRefType quantityType
    {
        get
        {
            return this._quantityType;
        }
        set
        {
            this._quantityType = value;
        }
    }

    public CodeType1 catalogSymbol
    {
        get
        {
            return this._catalogSymbol;
        }
        set
        {
            this._catalogSymbol = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ConventionalUnitType : UnitDefinitionType
{

    #region Private fields
    private ConversionToPreferredUnitType _item;

    private ItemChoiceType _itemElementName;

    private List<DerivationUnitTermType> _derivationUnitTerm;
    #endregion

    public ConventionalUnitType()
    {
        this._derivationUnitTerm = new List<DerivationUnitTermType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("conversionToPreferredUnit", typeof(ConversionToPreferredUnitType))]
    [System.Xml.Serialization.XmlElementAttribute("roughConversionToPreferredUnit", typeof(ConversionToPreferredUnitType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemElementName")]
    public ConversionToPreferredUnitType Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemChoiceType ItemElementName
    {
        get
        {
            return this._itemElementName;
        }
        set
        {
            this._itemElementName = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("derivationUnitTerm")]
    public List<DerivationUnitTermType> derivationUnitTerm
    {
        get
        {
            return this._derivationUnitTerm;
        }
        set
        {
            this._derivationUnitTerm = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml", IncludeInSchema = false)]
public enum ItemChoiceType
{

    /// <remarks/>
    conversionToPreferredUnit,

    /// <remarks/>
    roughConversionToPreferredUnit,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DerivedUnitType : UnitDefinitionType
{

    #region Private fields
    private List<DerivationUnitTermType> _derivationUnitTerm;
    #endregion

    public DerivedUnitType()
    {
        this._derivationUnitTerm = new List<DerivationUnitTermType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("derivationUnitTerm")]
    public List<DerivationUnitTermType> derivationUnitTerm
    {
        get
        {
            return this._derivationUnitTerm;
        }
        set
        {
            this._derivationUnitTerm = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class BaseUnitType : UnitDefinitionType
{

    #region Private fields
    private ReferenceType _unitsSystem;
    #endregion

    public BaseUnitType()
    {
        this._unitsSystem = new ReferenceType();
    }

    public ReferenceType unitsSystem
    {
        get
        {
            return this._unitsSystem;
        }
        set
        {
            this._unitsSystem = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DefinitionProxyType : DefinitionType
{

    #region Private fields
    private ReferenceType _definitionRef;
    #endregion

    public DefinitionProxyType()
    {
        this._definitionRef = new ReferenceType();
    }

    public ReferenceType definitionRef
    {
        get
        {
            return this._definitionRef;
        }
        set
        {
            this._definitionRef = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DictionaryType : DefinitionType
{

    #region Private fields
    private List<object> _items;
    #endregion

    public DictionaryType()
    {
        this._items = new List<object>();
    }

    [System.Xml.Serialization.XmlElementAttribute("dictionaryEntry", typeof(DictionaryEntryType))]
    [System.Xml.Serialization.XmlElementAttribute("indirectEntry", typeof(IndirectEntryType))]
    public List<object> Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class DictionaryEntryType
{

    #region Private fields
    private DefinitionType _definition;

    private string _remoteSchema;
    #endregion

    public DictionaryEntryType()
    {
        this._definition = new DefinitionType();
    }

    public DefinitionType Definition
    {
        get
        {
            return this._definition;
        }
        set
        {
            this._definition = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class IndirectEntryType
{

    #region Private fields
    private DefinitionProxyType _definitionProxy;
    #endregion

    public IndirectEntryType()
    {
        this._definitionProxy = new DefinitionProxyType();
    }

    public DefinitionProxyType DefinitionProxy
    {
        get
        {
            return this._definitionProxy;
        }
        set
        {
            this._definitionProxy = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class ArrayType : AbstractGMLType
{

    #region Private fields
    private List<object> _members;
    #endregion

    public ArrayType()
    {
        this._members = new List<object>();
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("_Object", IsNullable = false)]
    public List<object> members
    {
        get
        {
            return this._members;
        }
        set
        {
            this._members = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class BagType : AbstractGMLType
{

    #region Private fields
    private List<AssociationType> _member;

    private List<object> _members;
    #endregion

    public BagType()
    {
        this._members = new List<object>();
        this._member = new List<AssociationType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("member")]
    public List<AssociationType> member
    {
        get
        {
            return this._member;
        }
        set
        {
            this._member = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("_Object", IsNullable = false)]
    public List<object> members
    {
        get
        {
            return this._members;
        }
        set
        {
            this._members = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPolygonType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiLineStringType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSolidType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiSurfaceType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiCurveType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPointType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiGeometryType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractGeometricAggregateType : AbstractGeometryType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MultiPolygonType : AbstractGeometricAggregateType
{

    #region Private fields
    private List<PolygonPropertyType> _polygonMember;
    #endregion

    public MultiPolygonType()
    {
        this._polygonMember = new List<PolygonPropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("polygonMember")]
    public List<PolygonPropertyType> polygonMember
    {
        get
        {
            return this._polygonMember;
        }
        set
        {
            this._polygonMember = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class PolygonPropertyType
{

    #region Private fields
    private PolygonType _polygon;

    private string _remoteSchema;
    #endregion

    public PolygonPropertyType()
    {
        this._polygon = new PolygonType();
    }

    public PolygonType Polygon
    {
        get
        {
            return this._polygon;
        }
        set
        {
            this._polygon = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MultiLineStringType : AbstractGeometricAggregateType
{

    #region Private fields
    private List<LineStringPropertyType> _lineStringMember;
    #endregion

    public MultiLineStringType()
    {
        this._lineStringMember = new List<LineStringPropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("lineStringMember")]
    public List<LineStringPropertyType> lineStringMember
    {
        get
        {
            return this._lineStringMember;
        }
        set
        {
            this._lineStringMember = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class LineStringPropertyType
{

    #region Private fields
    private LineStringType _lineString;

    private string _remoteSchema;
    #endregion

    public LineStringPropertyType()
    {
        this._lineString = new LineStringType();
    }

    public LineStringType LineString
    {
        get
        {
            return this._lineString;
        }
        set
        {
            this._lineString = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MultiSolidType : AbstractGeometricAggregateType
{

    #region Private fields
    private List<SolidPropertyType> _solidMember;

    private SolidArrayPropertyType _solidMembers;
    #endregion

    public MultiSolidType()
    {
        this._solidMembers = new SolidArrayPropertyType();
        this._solidMember = new List<SolidPropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("solidMember")]
    public List<SolidPropertyType> solidMember
    {
        get
        {
            return this._solidMember;
        }
        set
        {
            this._solidMember = value;
        }
    }

    public SolidArrayPropertyType solidMembers
    {
        get
        {
            return this._solidMembers;
        }
        set
        {
            this._solidMembers = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class SolidArrayPropertyType
{

    #region Private fields
    private List<AbstractSolidType> _SolidField;
    #endregion

    public SolidArrayPropertyType()
    {
        this._SolidField = new List<AbstractSolidType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("_Solid")]
    public List<AbstractSolidType> _Solid
    {
        get
        {
            return this._SolidField;
        }
        set
        {
            this._SolidField = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MultiSurfaceType : AbstractGeometricAggregateType
{

    #region Private fields
    private List<SurfacePropertyType> _surfaceMember;

    private List<AbstractSurfaceType> _surfaceMembers;
    #endregion

    public MultiSurfaceType()
    {
        this._surfaceMembers = new List<AbstractSurfaceType>();
        this._surfaceMember = new List<SurfacePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("surfaceMember")]
    public List<SurfacePropertyType> surfaceMember
    {
        get
        {
            return this._surfaceMember;
        }
        set
        {
            this._surfaceMember = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("_Surface", IsNullable = false)]
    public List<AbstractSurfaceType> surfaceMembers
    {
        get
        {
            return this._surfaceMembers;
        }
        set
        {
            this._surfaceMembers = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MultiCurveType : AbstractGeometricAggregateType
{

    #region Private fields
    private List<CurvePropertyType> _curveMember;

    private List<AbstractCurveType> _curveMembers;
    #endregion

    public MultiCurveType()
    {
        this._curveMembers = new List<AbstractCurveType>();
        this._curveMember = new List<CurvePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("curveMember")]
    public List<CurvePropertyType> curveMember
    {
        get
        {
            return this._curveMember;
        }
        set
        {
            this._curveMember = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("_Curve", IsNullable = false)]
    public List<AbstractCurveType> curveMembers
    {
        get
        {
            return this._curveMembers;
        }
        set
        {
            this._curveMembers = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MultiPointType : AbstractGeometricAggregateType
{

    #region Private fields
    private List<PointPropertyType> _pointMember;

    private List<PointType> _pointMembers;
    #endregion

    public MultiPointType()
    {
        this._pointMembers = new List<PointType>();
        this._pointMember = new List<PointPropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("pointMember")]
    public List<PointPropertyType> pointMember
    {
        get
        {
            return this._pointMember;
        }
        set
        {
            this._pointMember = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("Point", IsNullable = false)]
    public List<PointType> pointMembers
    {
        get
        {
            return this._pointMembers;
        }
        set
        {
            this._pointMembers = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class MultiGeometryType : AbstractGeometricAggregateType
{

    #region Private fields
    private List<GeometryPropertyType> _geometryMember;

    private List<AbstractGeometryType> _geometryMembers;
    #endregion

    public MultiGeometryType()
    {
        this._geometryMembers = new List<AbstractGeometryType>();
        this._geometryMember = new List<GeometryPropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("geometryMember")]
    public List<GeometryPropertyType> geometryMember
    {
        get
        {
            return this._geometryMember;
        }
        set
        {
            this._geometryMember = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("_Geometry", IsNullable = false)]
    public List<AbstractGeometryType> geometryMembers
    {
        get
        {
            return this._geometryMembers;
        }
        set
        {
            this._geometryMembers = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GeometryPropertyType
{

    #region Private fields
    private AbstractGeometryType _GeometryField;

    private string _remoteSchema;
    #endregion

    public AbstractGeometryType _Geometry
    {
        get
        {
            return this._GeometryField;
        }
        set
        {
            this._GeometryField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(RectifiedGridType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GridType : AbstractGeometryType
{

    #region Private fields
    private GridLimitsType _limits;

    private List<string> _axisName;

    private string _dimension;
    #endregion

    public GridType()
    {
        this._axisName = new List<string>();
        this._limits = new GridLimitsType();
    }

    public GridLimitsType limits
    {
        get
        {
            return this._limits;
        }
        set
        {
            this._limits = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("axisName")]
    public List<string> axisName
    {
        get
        {
            return this._axisName;
        }
        set
        {
            this._axisName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string dimension
    {
        get
        {
            return this._dimension;
        }
        set
        {
            this._dimension = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GridLimitsType
{

    #region Private fields
    private GridEnvelopeType _gridEnvelope;
    #endregion

    public GridLimitsType()
    {
        this._gridEnvelope = new GridEnvelopeType();
    }

    public GridEnvelopeType GridEnvelope
    {
        get
        {
            return this._gridEnvelope;
        }
        set
        {
            this._gridEnvelope = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GridEnvelopeType
{

    #region Private fields
    private string _low;

    private string _high;
    #endregion

    public string low
    {
        get
        {
            return this._low;
        }
        set
        {
            this._low = value;
        }
    }

    public string high
    {
        get
        {
            return this._high;
        }
        set
        {
            this._high = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class RectifiedGridType : GridType
{

    #region Private fields
    private PointPropertyType _origin;

    private List<VectorType> _offsetVector;
    #endregion

    public RectifiedGridType()
    {
        this._offsetVector = new List<VectorType>();
        this._origin = new PointPropertyType();
    }

    public PointPropertyType origin
    {
        get
        {
            return this._origin;
        }
        set
        {
            this._origin = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("offsetVector")]
    public List<VectorType> offsetVector
    {
        get
        {
            return this._offsetVector;
        }
        set
        {
            this._offsetVector = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GeometricComplexType : AbstractGeometryType
{

    #region Private fields
    private List<GeometricPrimitivePropertyType> _element;
    #endregion

    public GeometricComplexType()
    {
        this._element = new List<GeometricPrimitivePropertyType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("element")]
    public List<GeometricPrimitivePropertyType> element
    {
        get
        {
            return this._element;
        }
        set
        {
            this._element = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GeometricPrimitivePropertyType
{

    #region Private fields
    private AbstractGeometricPrimitiveType _GeometricPrimitiveField;

    private string _remoteSchema;
    #endregion

    public AbstractGeometricPrimitiveType _GeometricPrimitive
    {
        get
        {
            return this._GeometricPrimitiveField;
        }
        set
        {
            this._GeometricPrimitiveField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
    public string remoteSchema
    {
        get
        {
            return this._remoteSchema;
        }
        set
        {
            this._remoteSchema = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class DistanceType
{

    #region Private fields
    private string _units;

    private double _value;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string units
    {
        get
        {
            return this._units;
        }
        set
        {
            this._units = value;
        }
    }

    [System.Xml.Serialization.XmlTextAttribute()]
    public double Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class BinarySpatialOpType : SpatialOpsType
{

    #region Private fields
    private PropertyNameType _propertyName;

    private object _item;
    #endregion

    public BinarySpatialOpType()
    {
        this._propertyName = new PropertyNameType();
    }

    [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
    public PropertyNameType PropertyName
    {
        get
        {
            return this._propertyName;
        }
        set
        {
            this._propertyName = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("Envelope", typeof(EnvelopeType), Namespace = "http://www.opengis.net/gml", Order = 1)]
    [System.Xml.Serialization.XmlElementAttribute("_Geometry", typeof(AbstractGeometryType), Namespace = "http://www.opengis.net/gml", Order = 1)]
    [System.Xml.Serialization.XmlElementAttribute("PropertyName", typeof(PropertyNameType), Order = 1)]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class UnaryLogicOpType : LogicOpsType
{

    #region Private fields
    private object _item;

    private ItemChoiceType4 _itemElementName;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("And", typeof(BinaryLogicOpType))]
    [System.Xml.Serialization.XmlElementAttribute("BBOX", typeof(BBOXType))]
    [System.Xml.Serialization.XmlElementAttribute("Beyond", typeof(DistanceBufferType))]
    [System.Xml.Serialization.XmlElementAttribute("Contains", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Crosses", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("DWithin", typeof(DistanceBufferType))]
    [System.Xml.Serialization.XmlElementAttribute("Disjoint", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Equals", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Function", typeof(FunctionType))]
    [System.Xml.Serialization.XmlElementAttribute("Intersects", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Not", typeof(UnaryLogicOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Or", typeof(BinaryLogicOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Overlaps", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsBetween", typeof(PropertyIsBetweenType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsEqualTo", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsGreaterThan", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsGreaterThanOrEqualTo", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsLessThan", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsLessThanOrEqualTo", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsLike", typeof(PropertyIsLikeType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsNotEqualTo", typeof(BinaryComparisonOpType))]
    [System.Xml.Serialization.XmlElementAttribute("PropertyIsNull", typeof(PropertyIsNullType))]
    [System.Xml.Serialization.XmlElementAttribute("Touches", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("Within", typeof(BinarySpatialOpType))]
    [System.Xml.Serialization.XmlElementAttribute("comparisonOps", typeof(ComparisonOpsType))]
    [System.Xml.Serialization.XmlElementAttribute("logicOps", typeof(LogicOpsType))]
    [System.Xml.Serialization.XmlElementAttribute("spatialOps", typeof(SpatialOpsType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemElementName")]
    public object Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }

    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemChoiceType4 ItemElementName
    {
        get
        {
            return this._itemElementName;
        }
        set
        {
            this._itemElementName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class PropertyIsBetweenType : ComparisonOpsType
{

    #region Private fields
    private ExpressionType _expression;

    private LowerBoundaryType _lowerBoundary;

    private UpperBoundaryType _upperBoundary;
    #endregion

    public PropertyIsBetweenType()
    {
        this._upperBoundary = new UpperBoundaryType();
        this._lowerBoundary = new LowerBoundaryType();
    }

    public ExpressionType expression
    {
        get
        {
            return this._expression;
        }
        set
        {
            this._expression = value;
        }
    }

    public LowerBoundaryType LowerBoundary
    {
        get
        {
            return this._lowerBoundary;
        }
        set
        {
            this._lowerBoundary = value;
        }
    }

    public UpperBoundaryType UpperBoundary
    {
        get
        {
            return this._upperBoundary;
        }
        set
        {
            this._upperBoundary = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class LowerBoundaryType
{

    #region Private fields
    private ExpressionType _item;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("expression")]
    public ExpressionType Item
    {
        get
        {
            return this._item;
        }
        set
        {
            this._item = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class UpperBoundaryType
{

    #region Private fields
    private ExpressionType _expression;
    #endregion

    public ExpressionType expression
    {
        get
        {
            return this._expression;
        }
        set
        {
            this._expression = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(PropertyIsBetweenType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PropertyIsNullType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PropertyIsLikeType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(BinaryComparisonOpType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public abstract partial class ComparisonOpsType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class PropertyIsNullType : ComparisonOpsType
{

    #region Private fields
    private PropertyNameType _propertyName;
    #endregion

    public PropertyIsNullType()
    {
        this._propertyName = new PropertyNameType();
    }

    public PropertyNameType PropertyName
    {
        get
        {
            return this._propertyName;
        }
        set
        {
            this._propertyName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class PropertyIsLikeType : ComparisonOpsType
{

    #region Private fields
    private PropertyNameType _propertyName;

    private LiteralType _literal;

    private string _wildCard;

    private string _singleChar;

    private string _escapeChar;

    private bool _matchCase;
    #endregion

    public PropertyIsLikeType()
    {
        this._literal = new LiteralType();
        this._propertyName = new PropertyNameType();
        this._matchCase = true;
    }

    public PropertyNameType PropertyName
    {
        get
        {
            return this._propertyName;
        }
        set
        {
            this._propertyName = value;
        }
    }

    public LiteralType Literal
    {
        get
        {
            return this._literal;
        }
        set
        {
            this._literal = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string wildCard
    {
        get
        {
            return this._wildCard;
        }
        set
        {
            this._wildCard = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string singleChar
    {
        get
        {
            return this._singleChar;
        }
        set
        {
            this._singleChar = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string escapeChar
    {
        get
        {
            return this._escapeChar;
        }
        set
        {
            this._escapeChar = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(true)]
    public bool matchCase
    {
        get
        {
            return this._matchCase;
        }
        set
        {
            this._matchCase = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class BinaryComparisonOpType : ComparisonOpsType
{

    #region Private fields
    private List<ExpressionType> _expression;

    private bool _matchCase;
    #endregion

    public BinaryComparisonOpType()
    {
        this._expression = new List<ExpressionType>();
        this._matchCase = true;
    }

    [System.Xml.Serialization.XmlElementAttribute("expression")]
    public List<ExpressionType> expression
    {
        get
        {
            return this._expression;
        }
        set
        {
            this._expression = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(true)]
    public bool matchCase
    {
        get
        {
            return this._matchCase;
        }
        set
        {
            this._matchCase = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(UnaryLogicOpType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(BinaryLogicOpType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public abstract partial class LogicOpsType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc", IncludeInSchema = false)]
public enum ItemChoiceType4
{

    /// <remarks/>
    And,

    /// <remarks/>
    BBOX,

    /// <remarks/>
    Beyond,

    /// <remarks/>
    Contains,

    /// <remarks/>
    Crosses,

    /// <remarks/>
    DWithin,

    /// <remarks/>
    Disjoint,

    /// <remarks/>
    Equals,

    /// <remarks/>
    Function,

    /// <remarks/>
    Intersects,

    /// <remarks/>
    Not,

    /// <remarks/>
    Or,

    /// <remarks/>
    Overlaps,

    /// <remarks/>
    PropertyIsBetween,

    /// <remarks/>
    PropertyIsEqualTo,

    /// <remarks/>
    PropertyIsGreaterThan,

    /// <remarks/>
    PropertyIsGreaterThanOrEqualTo,

    /// <remarks/>
    PropertyIsLessThan,

    /// <remarks/>
    PropertyIsLessThanOrEqualTo,

    /// <remarks/>
    PropertyIsLike,

    /// <remarks/>
    PropertyIsNotEqualTo,

    /// <remarks/>
    PropertyIsNull,

    /// <remarks/>
    Touches,

    /// <remarks/>
    Within,

    /// <remarks/>
    comparisonOps,

    /// <remarks/>
    logicOps,

    /// <remarks/>
    spatialOps,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc", IncludeInSchema = false)]
public enum ItemsChoiceType11
{

    /// <remarks/>
    And,

    /// <remarks/>
    BBOX,

    /// <remarks/>
    Beyond,

    /// <remarks/>
    Contains,

    /// <remarks/>
    Crosses,

    /// <remarks/>
    DWithin,

    /// <remarks/>
    Disjoint,

    /// <remarks/>
    Equals,

    /// <remarks/>
    Function,

    /// <remarks/>
    Intersects,

    /// <remarks/>
    Not,

    /// <remarks/>
    Or,

    /// <remarks/>
    Overlaps,

    /// <remarks/>
    PropertyIsBetween,

    /// <remarks/>
    PropertyIsEqualTo,

    /// <remarks/>
    PropertyIsGreaterThan,

    /// <remarks/>
    PropertyIsGreaterThanOrEqualTo,

    /// <remarks/>
    PropertyIsLessThan,

    /// <remarks/>
    PropertyIsLessThanOrEqualTo,

    /// <remarks/>
    PropertyIsLike,

    /// <remarks/>
    PropertyIsNotEqualTo,

    /// <remarks/>
    PropertyIsNull,

    /// <remarks/>
    Touches,

    /// <remarks/>
    Within,

    /// <remarks/>
    comparisonOps,

    /// <remarks/>
    logicOps,

    /// <remarks/>
    spatialOps,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class FeatureIdType : AbstractIdType
{

    #region Private fields
    private string _fid;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "ID")]
    public string fid
    {
        get
        {
            return this._fid;
        }
        set
        {
            this._fid = value;
        }
    }
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(GmlObjectIdType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(FeatureIdType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public abstract partial class AbstractIdType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class GmlObjectIdType : AbstractIdType
{

    #region Private fields
    private string _id;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.opengis.net/gml", DataType = "ID")]
    public string id
    {
        get
        {
            return this._id;
        }
        set
        {
            this._id = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc", IncludeInSchema = false)]
public enum ItemsChoiceType12
{

    /// <remarks/>
    And,

    /// <remarks/>
    BBOX,

    /// <remarks/>
    Beyond,

    /// <remarks/>
    Contains,

    /// <remarks/>
    Crosses,

    /// <remarks/>
    DWithin,

    /// <remarks/>
    Disjoint,

    /// <remarks/>
    Equals,

    /// <remarks/>
    FeatureId,

    /// <remarks/>
    GmlObjectId,

    /// <remarks/>
    Intersects,

    /// <remarks/>
    Not,

    /// <remarks/>
    Or,

    /// <remarks/>
    Overlaps,

    /// <remarks/>
    PropertyIsBetween,

    /// <remarks/>
    PropertyIsEqualTo,

    /// <remarks/>
    PropertyIsGreaterThan,

    /// <remarks/>
    PropertyIsGreaterThanOrEqualTo,

    /// <remarks/>
    PropertyIsLessThan,

    /// <remarks/>
    PropertyIsLessThanOrEqualTo,

    /// <remarks/>
    PropertyIsLike,

    /// <remarks/>
    PropertyIsNotEqualTo,

    /// <remarks/>
    PropertyIsNull,

    /// <remarks/>
    Touches,

    /// <remarks/>
    Within,

    /// <remarks/>
    _Id,

    /// <remarks/>
    comparisonOps,

    /// <remarks/>
    logicOps,

    /// <remarks/>
    spatialOps,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("Insert", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class InsertElementType
{

    #region Private fields
    private List<AbstractFeatureType> _FeatureField;

    private IdentifierGenerationOptionType _idgen;

    private string _handle;

    private string _inputFormat;

    private string _srsName;
    #endregion

    public InsertElementType()
    {
        this._FeatureField = new List<AbstractFeatureType>();
        this._idgen = IdentifierGenerationOptionType.GenerateNew;
        this._inputFormat = "text/xml; subtype=gml/3.1.1";
    }

    [System.Xml.Serialization.XmlElementAttribute("_Feature", Namespace = "http://www.opengis.net/gml")]
    public List<AbstractFeatureType> _Feature
    {
        get
        {
            return this._FeatureField;
        }
        set
        {
            this._FeatureField = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(IdentifierGenerationOptionType.GenerateNew)]
    public IdentifierGenerationOptionType idgen
    {
        get
        {
            return this._idgen;
        }
        set
        {
            this._idgen = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string handle
    {
        get
        {
            return this._handle;
        }
        set
        {
            this._handle = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute("text/xml; subtype=gml/3.1.1")]
    public string inputFormat
    {
        get
        {
            return this._inputFormat;
        }
        set
        {
            this._inputFormat = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string srsName
    {
        get
        {
            return this._srsName;
        }
        set
        {
            this._srsName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
public enum IdentifierGenerationOptionType
{

    /// <remarks/>
    UseExisting,

    /// <remarks/>
    ReplaceDuplicate,

    /// <remarks/>
    GenerateNew,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("Native", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class NativeType
{

    #region Private fields
    private string _vendorId;

    private bool _safeToIgnore;
    #endregion

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string vendorId
    {
        get
        {
            return this._vendorId;
        }
        set
        {
            this._vendorId = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public bool safeToIgnore
    {
        get
        {
            return this._safeToIgnore;
        }
        set
        {
            this._safeToIgnore = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("Update", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class UpdateElementType
{

    #region Private fields
    private List<PropertyType> _property;

    private FilterType _filter;

    private string _handle;

    private System.Xml.XmlQualifiedName _typeName;

    private string _inputFormat;

    private string _srsName;
    #endregion

    public UpdateElementType()
    {
        this._filter = new FilterType();
        this._property = new List<PropertyType>();
        this._inputFormat = "x-application/gml:3";
    }

    [System.Xml.Serialization.XmlElementAttribute("Property")]
    public List<PropertyType> Property
    {
        get
        {
            return this._property;
        }
        set
        {
            this._property = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://www.opengis.net/ogc")]
    public FilterType Filter
    {
        get
        {
            return this._filter;
        }
        set
        {
            this._filter = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string handle
    {
        get
        {
            return this._handle;
        }
        set
        {
            this._handle = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public System.Xml.XmlQualifiedName typeName
    {
        get
        {
            return this._typeName;
        }
        set
        {
            this._typeName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute("x-application/gml:3")]
    public string inputFormat
    {
        get
        {
            return this._inputFormat;
        }
        set
        {
            this._inputFormat = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string srsName
    {
        get
        {
            return this._srsName;
        }
        set
        {
            this._srsName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("Property", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class PropertyType
{

    #region Private fields
    private System.Xml.XmlQualifiedName _name;

    private object _value;
    #endregion

    public System.Xml.XmlQualifiedName Name
    {
        get
        {
            return this._name;
        }
        set
        {
            this._name = value;
        }
    }

    public object Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
public enum AllSomeType
{

    /// <remarks/>
    ALL,

    /// <remarks/>
    SOME,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("LockFeature", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class LockFeatureType : BaseRequestType
{

    #region Private fields
    private List<LockType> _lock;

    private string _expiry;

    private AllSomeType _lockAction;
    #endregion

    public LockFeatureType()
    {
        this._lock = new List<LockType>();
        this._expiry = "5";
        this._lockAction = AllSomeType.ALL;
    }

    [System.Xml.Serialization.XmlElementAttribute("Lock")]
    public List<LockType> Lock
    {
        get
        {
            return this._lock;
        }
        set
        {
            this._lock = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    [System.ComponentModel.DefaultValueAttribute("5")]
    public string expiry
    {
        get
        {
            return this._expiry;
        }
        set
        {
            this._expiry = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(AllSomeType.ALL)]
    public AllSomeType lockAction
    {
        get
        {
            return this._lockAction;
        }
        set
        {
            this._lockAction = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
public partial class LockType
{

    #region Private fields
    private FilterType _filter;

    private string _handle;

    private System.Xml.XmlQualifiedName _typeName;
    #endregion

    public LockType()
    {
        this._filter = new FilterType();
    }

    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://www.opengis.net/ogc")]
    public FilterType Filter
    {
        get
        {
            return this._filter;
        }
        set
        {
            this._filter = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string handle
    {
        get
        {
            return this._handle;
        }
        set
        {
            this._handle = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public System.Xml.XmlQualifiedName typeName
    {
        get
        {
            return this._typeName;
        }
        set
        {
            this._typeName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("GetFeatureWithLock", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class GetFeatureWithLockType : BaseRequestType
{

    #region Private fields
    private List<QueryType> _query;

    private string _expiry;

    private ResultTypeType _resultType;

    private string _outputFormat;

    private string _maxFeatures;

    private string _traverseXlinkDepth;

    private string _traverseXlinkExpiry;
    #endregion

    public GetFeatureWithLockType()
    {
        this._query = new List<QueryType>();
        this._expiry = "5";
        this._resultType = ResultTypeType.results;
        this._outputFormat = "text/xml; subtype=gml/3.1.1";
    }

    [System.Xml.Serialization.XmlElementAttribute("Query")]
    public List<QueryType> Query
    {
        get
        {
            return this._query;
        }
        set
        {
            this._query = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    [System.ComponentModel.DefaultValueAttribute("5")]
    public string expiry
    {
        get
        {
            return this._expiry;
        }
        set
        {
            this._expiry = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(ResultTypeType.results)]
    public ResultTypeType resultType
    {
        get
        {
            return this._resultType;
        }
        set
        {
            this._resultType = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute("text/xml; subtype=gml/3.1.1")]
    public string outputFormat
    {
        get
        {
            return this._outputFormat;
        }
        set
        {
            this._outputFormat = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string maxFeatures
    {
        get
        {
            return this._maxFeatures;
        }
        set
        {
            this._maxFeatures = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string traverseXlinkDepth
    {
        get
        {
            return this._traverseXlinkDepth;
        }
        set
        {
            this._traverseXlinkDepth = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string traverseXlinkExpiry
    {
        get
        {
            return this._traverseXlinkExpiry;
        }
        set
        {
            this._traverseXlinkExpiry = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("Query", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class QueryType
{

    #region Private fields
    private List<object> _items;

    private FilterType _filter;

    private List<SortPropertyType> _sortBy;

    private string _handle;

    private List<System.Xml.XmlQualifiedName> _typeName;

    private string _featureVersion;

    private string _srsName;
    #endregion

    public QueryType()
    {
        this._typeName = new List<System.Xml.XmlQualifiedName>();
        this._sortBy = new List<SortPropertyType>();
        this._filter = new FilterType();
        this._items = new List<object>();
    }

    [System.Xml.Serialization.XmlElementAttribute("Function", typeof(FunctionType), Namespace = "http://www.opengis.net/ogc")]
    [System.Xml.Serialization.XmlElementAttribute("PropertyName", typeof(string))]
    [System.Xml.Serialization.XmlElementAttribute("XlinkPropertyName", typeof(XlinkPropertyName))]
    public List<object> Items
    {
        get
        {
            return this._items;
        }
        set
        {
            this._items = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://www.opengis.net/ogc")]
    public FilterType Filter
    {
        get
        {
            return this._filter;
        }
        set
        {
            this._filter = value;
        }
    }

    [System.Xml.Serialization.XmlArrayAttribute(Namespace = "http://www.opengis.net/ogc")]
    [System.Xml.Serialization.XmlArrayItemAttribute("SortProperty", IsNullable = false)]
    public List<SortPropertyType> SortBy
    {
        get
        {
            return this._sortBy;
        }
        set
        {
            this._sortBy = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string handle
    {
        get
        {
            return this._handle;
        }
        set
        {
            this._handle = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public List<System.Xml.XmlQualifiedName> typeName
    {
        get
        {
            return this._typeName;
        }
        set
        {
            this._typeName = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string featureVersion
    {
        get
        {
            return this._featureVersion;
        }
        set
        {
            this._featureVersion = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string srsName
    {
        get
        {
            return this._srsName;
        }
        set
        {
            this._srsName = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public partial class SortPropertyType
{

    #region Private fields
    private PropertyNameType _propertyName;

    private SortOrderType _sortOrder;
    #endregion

    public SortPropertyType()
    {
        this._propertyName = new PropertyNameType();
    }

    public PropertyNameType PropertyName
    {
        get
        {
            return this._propertyName;
        }
        set
        {
            this._propertyName = value;
        }
    }

    public SortOrderType SortOrder
    {
        get
        {
            return this._sortOrder;
        }
        set
        {
            this._sortOrder = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/ogc")]
public enum SortOrderType
{

    /// <remarks/>
    DESC,

    /// <remarks/>
    ASC,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
public enum ResultTypeType
{

    /// <remarks/>
    results,

    /// <remarks/>
    hits,
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("GetGmlObject", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class GetGmlObjectType : BaseRequestType
{

    #region Private fields
    private GmlObjectIdType _gmlObjectId;

    private string _outputFormat;

    private string _traverseXlinkDepth;

    private string _traverseXlinkExpiry;
    #endregion

    public GetGmlObjectType()
    {
        this._gmlObjectId = new GmlObjectIdType();
        this._outputFormat = "GML3";
    }

    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://www.opengis.net/ogc")]
    public GmlObjectIdType GmlObjectId
    {
        get
        {
            return this._gmlObjectId;
        }
        set
        {
            this._gmlObjectId = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute("GML3")]
    public string outputFormat
    {
        get
        {
            return this._outputFormat;
        }
        set
        {
            this._outputFormat = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string traverseXlinkDepth
    {
        get
        {
            return this._traverseXlinkDepth;
        }
        set
        {
            this._traverseXlinkDepth = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string traverseXlinkExpiry
    {
        get
        {
            return this._traverseXlinkExpiry;
        }
        set
        {
            this._traverseXlinkExpiry = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("GetFeature", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class GetFeatureType : BaseRequestType
{

    #region Private fields
    private List<QueryType> _query;

    private ResultTypeType _resultType;

    private string _outputFormat;

    private string _maxFeatures;

    private string _traverseXlinkDepth;

    private string _traverseXlinkExpiry;
    #endregion

    public GetFeatureType()
    {
        this._query = new List<QueryType>();
        this._resultType = ResultTypeType.results;
        this._outputFormat = "text/xml; subtype=gml/3.1.1";
    }

    [System.Xml.Serialization.XmlElementAttribute("Query")]
    public List<QueryType> Query
    {
        get
        {
            return this._query;
        }
        set
        {
            this._query = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute(ResultTypeType.results)]
    public ResultTypeType resultType
    {
        get
        {
            return this._resultType;
        }
        set
        {
            this._resultType = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    [System.ComponentModel.DefaultValueAttribute("text/xml; subtype=gml/3.1.1")]
    public string outputFormat
    {
        get
        {
            return this._outputFormat;
        }
        set
        {
            this._outputFormat = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string maxFeatures
    {
        get
        {
            return this._maxFeatures;
        }
        set
        {
            this._maxFeatures = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string traverseXlinkDepth
    {
        get
        {
            return this._traverseXlinkDepth;
        }
        set
        {
            this._traverseXlinkDepth = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
    public string traverseXlinkExpiry
    {
        get
        {
            return this._traverseXlinkExpiry;
        }
        set
        {
            this._traverseXlinkExpiry = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("LockFeatureResponse", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class LockFeatureResponseType
{

    #region Private fields
    private string _lockId;

    private FeaturesLockedType _featuresLocked;

    private FeaturesNotLockedType _featuresNotLocked;
    #endregion

    public LockFeatureResponseType()
    {
        this._featuresNotLocked = new FeaturesNotLockedType();
        this._featuresLocked = new FeaturesLockedType();
    }

    public string LockId
    {
        get
        {
            return this._lockId;
        }
        set
        {
            this._lockId = value;
        }
    }

    public FeaturesLockedType FeaturesLocked
    {
        get
        {
            return this._featuresLocked;
        }
        set
        {
            this._featuresLocked = value;
        }
    }

    public FeaturesNotLockedType FeaturesNotLocked
    {
        get
        {
            return this._featuresNotLocked;
        }
        set
        {
            this._featuresNotLocked = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
public partial class FeaturesLockedType
{

    #region Private fields
    private List<FeatureIdType> _featureId;
    #endregion

    public FeaturesLockedType()
    {
        this._featureId = new List<FeatureIdType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("FeatureId", Namespace = "http://www.opengis.net/ogc")]
    public List<FeatureIdType> FeatureId
    {
        get
        {
            return this._featureId;
        }
        set
        {
            this._featureId = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
public partial class FeaturesNotLockedType
{

    #region Private fields
    private List<FeatureIdType> _featureId;
    #endregion

    public FeaturesNotLockedType()
    {
        this._featureId = new List<FeatureIdType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("FeatureId", Namespace = "http://www.opengis.net/ogc")]
    public List<FeatureIdType> FeatureId
    {
        get
        {
            return this._featureId;
        }
        set
        {
            this._featureId = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
[System.Xml.Serialization.XmlRootAttribute("TransactionResponse", Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
public partial class TransactionResponseType
{

    #region Private fields
    private TransactionSummaryType _transactionSummary;

    private List<ActionType> _transactionResults;

    private List<InsertedFeatureType> _insertResults;

    private string _version;
    #endregion

    public TransactionResponseType()
    {
        this._insertResults = new List<InsertedFeatureType>();
        this._transactionResults = new List<ActionType>();
        this._transactionSummary = new TransactionSummaryType();
        this._version = "1.1.0";
    }

    public TransactionSummaryType TransactionSummary
    {
        get
        {
            return this._transactionSummary;
        }
        set
        {
            this._transactionSummary = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("Action", IsNullable = false)]
    public List<ActionType> TransactionResults
    {
        get
        {
            return this._transactionResults;
        }
        set
        {
            this._transactionResults = value;
        }
    }

    [System.Xml.Serialization.XmlArrayItemAttribute("Feature", IsNullable = false)]
    public List<InsertedFeatureType> InsertResults
    {
        get
        {
            return this._insertResults;
        }
        set
        {
            this._insertResults = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string version
    {
        get
        {
            return this._version;
        }
        set
        {
            this._version = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
public partial class TransactionSummaryType
{

    #region Private fields
    private string _totalInserted;

    private string _totalUpdated;

    private string _totalDeleted;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute(DataType = "nonNegativeInteger")]
    public string totalInserted
    {
        get
        {
            return this._totalInserted;
        }
        set
        {
            this._totalInserted = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "nonNegativeInteger")]
    public string totalUpdated
    {
        get
        {
            return this._totalUpdated;
        }
        set
        {
            this._totalUpdated = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "nonNegativeInteger")]
    public string totalDeleted
    {
        get
        {
            return this._totalDeleted;
        }
        set
        {
            this._totalDeleted = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
public partial class ActionType
{

    #region Private fields
    private string _message;

    private string _locator;

    private string _code;
    #endregion

    public string Message
    {
        get
        {
            return this._message;
        }
        set
        {
            this._message = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string locator
    {
        get
        {
            return this._locator;
        }
        set
        {
            this._locator = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string code
    {
        get
        {
            return this._code;
        }
        set
        {
            this._code = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/wfs")]
public partial class InsertedFeatureType
{

    #region Private fields
    private List<FeatureIdType> _featureId;

    private string _handle;
    #endregion

    public InsertedFeatureType()
    {
        this._featureId = new List<FeatureIdType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("FeatureId", Namespace = "http://www.opengis.net/ogc")]
    public List<FeatureIdType> FeatureId
    {
        get
        {
            return this._featureId;
        }
        set
        {
            this._featureId = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string handle
    {
        get
        {
            return this._handle;
        }
        set
        {
            this._handle = value;
        }
    }
}
