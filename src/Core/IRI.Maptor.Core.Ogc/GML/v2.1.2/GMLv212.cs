using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Ogc.GML.v212;

using System.Collections.Generic;


[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolygonType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(BoxType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LinearRingType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LineStringType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PointType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractGeometryCollectionBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeometryCollectionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPolygonType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiLineStringType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPointType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("_Geometry", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public abstract partial class AbstractGeometryType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/1999/xlink")]
public partial class arcType
{

    #region Private fields
    private List<titleEltType> _title;

    private typeType _type;

    private string _arcrole;

    private string _title1;

    private showType _show;

    private actuateType _actuate;

    private string _from;

    private string _to;
    #endregion

    public arcType()
    {
        this._title = new List<titleEltType>();
        this._type = typeType.arc;
    }

    [System.Xml.Serialization.XmlElementAttribute("title")]
    public List<titleEltType> title
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
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

    [System.Xml.Serialization.XmlAttributeAttribute("title", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
    public string title1
    {
        get
        {
            return this._title1;
        }
        set
        {
            this._title1 = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "NCName")]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "NCName")]
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
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/1999/xlink")]
public partial class titleEltType
{

    #region Private fields
    private List<System.Xml.XmlNode> _any;

    private typeType _type;

    private string _lang;
    #endregion

    public titleEltType()
    {
        this._any = new List<System.Xml.XmlNode>();
        this._type = typeType.title;
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")]
    public string lang
    {
        get
        {
            return this._lang;
        }
        set
        {
            this._lang = value;
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
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/1999/xlink")]
public partial class locatorType
{

    #region Private fields
    private List<titleEltType> _title;

    private typeType _type;

    private string _href;

    private string _role;

    private string _title1;

    private string _label;
    #endregion

    public locatorType()
    {
        this._title = new List<titleEltType>();
        this._type = typeType.locator;
    }

    [System.Xml.Serialization.XmlElementAttribute("title")]
    public List<titleEltType> title
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
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

    [System.Xml.Serialization.XmlAttributeAttribute("title", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
    public string title1
    {
        get
        {
            return this._title1;
        }
        set
        {
            this._title1 = value;
        }
    }

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "NCName")]
    public string label
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
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/1999/xlink")]
public partial class resourceType
{

    #region Private fields
    private List<System.Xml.XmlNode> _any;

    private typeType _type;

    private string _role;

    private string _title;

    private string _label;
    #endregion

    public resourceType()
    {
        this._any = new List<System.Xml.XmlNode>();
        this._type = typeType.resource;
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "NCName")]
    public string label
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
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/1999/xlink")]
public partial class extended
{

    #region Private fields
    private List<object> _items;

    private typeType _type;

    private string _role;

    private string _title;
    #endregion

    public extended()
    {
        this._items = new List<object>();
        this._type = typeType.extended;
    }

    [System.Xml.Serialization.XmlElementAttribute("arc", typeof(arcType))]
    [System.Xml.Serialization.XmlElementAttribute("locator", typeof(locatorType))]
    [System.Xml.Serialization.XmlElementAttribute("resource", typeof(resourceType))]
    [System.Xml.Serialization.XmlElementAttribute("title", typeof(titleEltType))]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
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
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.w3.org/1999/xlink")]
public partial class simple
{

    #region Private fields
    private List<System.Xml.XmlNode> _any;

    private typeType _type;

    private string _href;

    private string _role;

    private string _arcrole;

    private string _title;

    private showType _show;

    private actuateType _actuate;
    #endregion

    public simple()
    {
        this._any = new List<System.Xml.XmlNode>();
        this._type = typeType.simple;
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, DataType = "anyURI")]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
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

    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
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
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public partial class GeometryPropertyType
{

    #region Private fields
    private AbstractGeometryType _GeometryField;

    private typeType _type;

    private string _href;

    private string _role;

    private string _arcrole;

    private string _title;

    private showType _show;

    private actuateType _actuate;

    private string _remoteSchema;
    #endregion

    public GeometryPropertyType()
    {
        this._type = typeType.simple;
    }

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
[System.Xml.Serialization.XmlRootAttribute("featureMember", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class FeatureAssociationType
{

    #region Private fields
    private AbstractFeatureType _FeatureField;

    private typeType _type;

    private string _href;

    private string _role;

    private string _arcrole;

    private string _title;

    private showType _show;

    private actuateType _actuate;

    private string _remoteSchema;
    #endregion

    public FeatureAssociationType()
    {
        this._type = typeType.simple;
    }

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

[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractFeatureCollectionBaseType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractFeatureCollectionType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("_Feature", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public abstract partial class AbstractFeatureType
{

    #region Private fields
    private string _description;

    private string _name;

    private BoundingShapeType _boundedBy;

    private string _fid;
    #endregion

    public AbstractFeatureType()
    {
        this._boundedBy = new BoundingShapeType();
    }

    public string description
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

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("boundedBy", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class BoundingShapeType
{

    #region Private fields
    private object _item;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("Box", typeof(BoxType))]
    [System.Xml.Serialization.XmlElementAttribute("null", typeof(NullType))]
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
[System.Xml.Serialization.XmlRootAttribute("Box", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class BoxType : AbstractGeometryType
{

    #region Private fields
    private List<object> _items;
    #endregion

    public BoxType()
    {
        this._items = new List<object>();
    }

    [System.Xml.Serialization.XmlElementAttribute("coord", typeof(CoordType))]
    [System.Xml.Serialization.XmlElementAttribute("coordinates", typeof(CoordinatesType))]
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
[System.Xml.Serialization.XmlRootAttribute("coord", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
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
[System.Xml.Serialization.XmlRootAttribute("coordinates", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
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
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public enum NullType
{

    /// <remarks/>
    inapplicable,

    /// <remarks/>
    unknown,

    /// <remarks/>
    unavailable,

    /// <remarks/>
    missing,
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(AbstractFeatureCollectionType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractFeatureCollectionBaseType : AbstractFeatureType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("_FeatureCollection", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public abstract partial class AbstractFeatureCollectionType : AbstractFeatureCollectionBaseType
{

    #region Private fields
    private List<FeatureAssociationType> _featureMember;
    #endregion

    public AbstractFeatureCollectionType()
    {
        this._featureMember = new List<FeatureAssociationType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("featureMember")]
    public List<FeatureAssociationType> featureMember
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
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiGeometryPropertyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPolygonPropertyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiLineStringPropertyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPointPropertyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LineStringPropertyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolygonPropertyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PointPropertyType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PolygonMemberType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LineStringMemberType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(PointMemberType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(LinearRingMemberType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("geometryMember", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class GeometryAssociationType
{

    #region Private fields
    private AbstractGeometryType _GeometryField;

    private typeType _type;

    private string _href;

    private string _role;

    private string _arcrole;

    private string _title;

    private showType _show;

    private actuateType _actuate;

    private string _remoteSchema;
    #endregion

    public GeometryAssociationType()
    {
        this._type = typeType.simple;
    }

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
[System.Xml.Serialization.XmlRootAttribute("multiGeometryProperty", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class MultiGeometryPropertyType : GeometryAssociationType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("multiPolygonProperty", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class MultiPolygonPropertyType : GeometryAssociationType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("multiLineStringProperty", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class MultiLineStringPropertyType : GeometryAssociationType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("multiPointProperty", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class MultiPointPropertyType : GeometryAssociationType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("lineStringProperty", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class LineStringPropertyType : GeometryAssociationType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("polygonProperty", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class PolygonPropertyType : GeometryAssociationType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("pointProperty", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class PointPropertyType : GeometryAssociationType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("polygonMember", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class PolygonMemberType : GeometryAssociationType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("lineStringMember", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class LineStringMemberType : GeometryAssociationType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("pointMember", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class PointMemberType : GeometryAssociationType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("outerBoundaryIs", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class LinearRingMemberType : GeometryAssociationType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("Polygon", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class PolygonType : AbstractGeometryType
{

    #region Private fields
    private LinearRingMemberType _outerBoundaryIs;

    private List<LinearRingMemberType> _innerBoundaryIs;
    #endregion

    public PolygonType()
    {
        this._innerBoundaryIs = new List<LinearRingMemberType>();
        this._outerBoundaryIs = new LinearRingMemberType();
    }

    public LinearRingMemberType outerBoundaryIs
    {
        get
        {
            return this._outerBoundaryIs;
        }
        set
        {
            this._outerBoundaryIs = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute("innerBoundaryIs")]
    public List<LinearRingMemberType> innerBoundaryIs
    {
        get
        {
            return this._innerBoundaryIs;
        }
        set
        {
            this._innerBoundaryIs = value;
        }
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("LinearRing", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class LinearRingType : AbstractGeometryType
{

    #region Private fields
    private List<object> _items;
    #endregion

    public LinearRingType()
    {
        this._items = new List<object>();
    }

    [System.Xml.Serialization.XmlElementAttribute("coord", typeof(CoordType))]
    [System.Xml.Serialization.XmlElementAttribute("coordinates", typeof(CoordinatesType))]
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
[System.Xml.Serialization.XmlRootAttribute("LineString", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class LineStringType : AbstractGeometryType
{

    #region Private fields
    private List<object> _items;
    #endregion

    public LineStringType()
    {
        this._items = new List<object>();
    }

    [System.Xml.Serialization.XmlElementAttribute("coord", typeof(CoordType))]
    [System.Xml.Serialization.XmlElementAttribute("coordinates", typeof(CoordinatesType))]
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
[System.Xml.Serialization.XmlRootAttribute("Point", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class PointType : AbstractGeometryType
{

    #region Private fields
    private object _item;
    #endregion

    [System.Xml.Serialization.XmlElementAttribute("coord", typeof(CoordType))]
    [System.Xml.Serialization.XmlElementAttribute("coordinates", typeof(CoordinatesType))]
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

[System.Xml.Serialization.XmlIncludeAttribute(typeof(GeometryCollectionType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPolygonType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiLineStringType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPointType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
public abstract partial class AbstractGeometryCollectionBaseType : AbstractGeometryType
{
}

[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPolygonType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiLineStringType))]
[System.Xml.Serialization.XmlIncludeAttribute(typeof(MultiPointType))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("_GeometryCollection", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class GeometryCollectionType : AbstractGeometryCollectionBaseType
{

    #region Private fields
    private List<GeometryAssociationType> _geometryMember;
    #endregion

    public GeometryCollectionType()
    {
        this._geometryMember = new List<GeometryAssociationType>();
    }

    [System.Xml.Serialization.XmlElementAttribute("geometryMember")]
    public List<GeometryAssociationType> geometryMember
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
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("MultiPolygon", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class MultiPolygonType : GeometryCollectionType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("MultiLineString", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class MultiLineStringType : GeometryCollectionType
{
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2053.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.opengis.net/gml")]
[System.Xml.Serialization.XmlRootAttribute("MultiPoint", Namespace = "http://www.opengis.net/gml", IsNullable = false)]
public partial class MultiPointType : GeometryCollectionType
{
}
