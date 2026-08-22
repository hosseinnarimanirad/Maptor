using System.Xml;
using System.Xml.Serialization;

namespace IRI.Maptor.Core.Ogc.WMS;

//public partial class elementB
//{
//    [XmlAttributeAttribute("schemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
//    public string xsiSchemaLocation = "http://www.acme.com/xml/OrderXML-1-0.xsd";
//}

[XmlType("WMS_Capabilities", Namespace = "http://www.opengis.net/wms")]
[XmlRoot("WMS_Capabilities", Namespace = "http://www.opengis.net/wms")]
public class WMSCapabilities
{
    [XmlAttribute("version")]
    public string version { get; set; }

    [XmlElement("Service")]
    public Service Service { get; set; }

    [XmlElement("Capability")]
    public Capability Capability { get; set; }

    //[XmlAttribute("schemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
    [XmlAttribute("schemaLocation", Namespace = System.Xml.Schema.XmlSchema.InstanceNamespace)]
    public string schemaLocation { get; set; }


    [XmlNamespaceDeclarations]
    public XmlSerializerNamespaces xmlns = new XmlSerializerNamespaces();

    public WMSCapabilities()
    {
        this.Service = new Service();

        this.Capability = new Capability();

        schemaLocation = "http://www.opengis.net/wms http://schemas.opengis.net/wms/1.3.0/capabilities_1_3_0.xsd  http://www.opengis.net/sld http://schemas.opengis.net/sld/1.1.0/sld_capabilities.xsd  http://mapserver.gis.umn.edu/mapserver http://mesonet.agron.iastate.edu/cgi-bin/wms/iowa/rainfall.cgi?service=WMS&version=1.3.0&request=GetSchemaExtension";

        this.version = "1.3.0";

        xmlns.Add("xlink", "http://www.w3.org/1999/xlink");
        xmlns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
    }
}

public class Service
{
    public string Name { get; set; }
    public string Title { get; set; }
    public string Abstract { get; set; }

    [XmlElement("KeywordList", Namespace = "")]
    public string[] KeywordList { get; set; }

    [XmlElement("OnlineResource")]
    public OnlineResource OnlineResource { get; set; }

    [XmlElement("ContactInformation")]
    public ContactInformation ContactInformation { get; set; }
    public string Fees { get; set; } = string.Empty;
    public string AccessConstraints { get; set; } = string.Empty;

    public int? LayerLimit { get; set; }
    public bool ShouldSerializeLayerLimit()
    {
        return LayerLimit.HasValue;
    }

    public int? MaxWidth { get; set; }
    public bool ShouldSerializeMaxWidth()
    {
        return MaxWidth.HasValue;
    }

    public int? MaxHeight { get; set; }
    public bool ShouldSerializeMaxHeight()
    {
        return MaxHeight.HasValue;
    }

    public Service() : this("WMS")
    {
        this.ContactInformation = new ContactInformation();
    }

    public Service(string name)
    {
        this.Name = name;

        this.ContactInformation = new ContactInformation();
    }
}

public class ContactInformation
{
    public Contactpersonprimary ContactPersonPrimary { get; set; }
    public string ContactPosition { get; set; } = string.Empty;
    public Contactaddress ContactAddress { get; set; }
    public string ContactVoiceTelephone { get; set; } = string.Empty;
    public string ContactElectronicMailAddress { get; set; } = string.Empty;

    public ContactInformation()
    {
        this.ContactPersonPrimary = new Contactpersonprimary();

        this.ContactAddress = new Contactaddress();
    }
}

public class Contactpersonprimary
{
    public string ContactPerson { get; set; } = string.Empty;

    public string ContactOrganization { get; set; } = string.Empty;

}

public class Contactaddress
{
    public string AddressType { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StateOrProvince { get; set; } = string.Empty;
    public string PostCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class Capability
{
    public Request Request { get; set; }

    [XmlElement("Exception", Namespace = "")]
    public string[] Exception { get; set; }
    public Layer Layer { get; set; }

    public Capability()
    {
        this.Request = new Request();

        this.Layer = new Layer();
    }
}

public class Request
{
    public GetCapabilities GetCapabilities { get; set; }
    public GetMap GetMap { get; set; }
    public GetFeatureInfo GetFeatureInfo { get; set; }

    public Request() : this(string.Empty)
    {

    }

    public Request(string url)
    {
        this.GetCapabilities = new GetCapabilities(url);

        this.GetMap = new GetMap(url);

        this.GetFeatureInfo = new GetFeatureInfo(url);
    }
}

public class GetCapabilities
{

    [XmlElement("Format", Namespace = "")]
    public string[] Format { get; set; }


    [XmlElement("DCPType", Namespace = "")]
    public DcpType[] DCPType { get; set; }

    public GetCapabilities() : this(string.Empty)
    {
    }

    public GetCapabilities(string url)
    {
        this.Format = new string[] { "text/xml" };

        this.DCPType = new DcpType[] { DcpType.Create(url) };
    }
}

public class DcpType
{
    public HTTP HTTP { get; set; }

    public DcpType()
    {
        this.HTTP = new HTTP();
    }

    public static DcpType Create(string url)
    {
        return new DcpType() { HTTP = new HTTP() { Get = new Get() { OnlineResource = new OnlineResource() { Type = "simple", href = url } } } };
    }
}

public class HTTP
{
    public Get Get { get; set; }

    public bool ShouldSerializeGet()
    {
        return !string.IsNullOrWhiteSpace(Get?.OnlineResource?.href);
    }

    public Post Post { get; set; }

    public bool ShouldSerializePost()
    {
        return !string.IsNullOrWhiteSpace(Post?.OnlineResource?.href);
    }

    public HTTP()
    {
        this.Get = new Get();

        this.Post = new Post();
    }
}

public class Get
{
    [XmlElement]
    public OnlineResource OnlineResource { get; set; }

    public Get()
    {
        this.OnlineResource = new OnlineResource();
    }
}

public class Post
{
    [XmlElement]
    public OnlineResource OnlineResource { get; set; }

    public Post()
    {
        this.OnlineResource = new OnlineResource();
    }
}

public class GetMap
{
    [XmlElement("Format", Namespace = "")]
    public string[] Format { get; set; }


    [XmlElement("DCPType", Namespace = "")]
    public DcpType[] DCPType { get; set; }

    public GetMap() : this(string.Empty)
    {

    }

    public GetMap(string url)
    {
        this.Format = new string[] { "image/png" };

        this.DCPType = new DcpType[] { DcpType.Create(url) };
    }
}



public class GetFeatureInfo
{
    [XmlElement("Format", Namespace = "")]
    public string[] Format { get; set; }


    [XmlElement("DCPType", Namespace = "")]
    public DcpType[] DCPType { get; set; }

    public GetFeatureInfo() : this(string.Empty)
    {

    }

    public GetFeatureInfo(string url)
    {
        this.Format = new string[] { "text/plain" };

        this.DCPType = new DcpType[] { DcpType.Create(url) };
    }
}


public class Layer
{
    public string Name { get; set; }
    public bool ShouldSerializeName()
    {
        return !string.IsNullOrWhiteSpace(Name);
    }

    public string Title { get; set; }

    public string Abstract { get; set; }

    [XmlElement("CRS", Namespace = "")]
    public string[] CRS { get; set; }

    [XmlElement("Authorityurl", Namespace = "")]
    public Authorityurl[] AuthorityURL { get; set; }

    public EX_GeographicBoundingBox EX_GeographicBoundingBox { get; set; }

    [XmlElement("BoundingBox", Namespace = "")]
    public Boundingbox[] BoundingBox { get; set; }

    [XmlElement("Layer")]
    public Layer[] Layers { get; set; }

    public Attribution Attribution { get; set; }


    [XmlElement("Identifier", Namespace = "")]
    public string[] Identifier { get; set; }


    [XmlElement("FeatureListURL", Namespace = "")]
    public FeatureListUrl[] FeatureListURL { get; set; }


    [XmlElement("Style", Namespace = "")]
    public Style[] Style { get; set; }
    public bool ShouldSerializeStyle()
    {
        return !string.IsNullOrWhiteSpace(Name);
    }

    public double MinScaleDenominator { get; set; } = -1;

    public bool ShouldSerializeMinScaleDenominator()
    {
        return MinScaleDenominator >= 0;
    }


    public double MaxScaleDenominator { get; set; } = -1;

    public bool ShouldSerializeMaxScaleDenominator()
    {
        return MaxScaleDenominator >= 0;
    }


    //[XmlElement("Layer")]
    //public Layer2[] Layer { get; set; }

    //[XmlElement("Layer")]
    //public Layer[] Layer { get; set; }

    [XmlIgnore]
    private bool? _queryable;

    [XmlAttribute("queryable", Namespace = "")]
    public string queryable
    {
        get { return this._queryable.Value ? "1" : "0"; }
        set { this._queryable = XmlConvert.ToBoolean(value); }
    }

    public bool ShouldSerializequeryable()
    {
        return _queryable.HasValue;
    }


    [XmlIgnore]
    private bool? _opaque;

    [XmlAttribute("opaque", Namespace = "")]
    public string opaque
    {
        get { return this._opaque.Value ? "1" : "0"; }
        set { this._opaque = XmlConvert.ToBoolean(value); }
    }

    public bool ShouldSerializeopaque()
    {
        return _opaque.HasValue;
    }


    [XmlIgnore]
    private uint? _cascaded;

    [XmlAttribute("cascaded", Namespace = "")]
    public string cascaded
    {
        get { return this._cascaded.HasValue ? "1" : "0"; }
        set { this._cascaded = XmlConvert.ToUInt32(value); }
    }

    public bool ShouldSerializecascaded()
    {
        return _cascaded.HasValue;
    }


    [XmlIgnore]
    private bool? _noSubsets;

    [XmlAttribute("noSubsets", Namespace = "")]
    public string noSubsets
    {
        get { return this._noSubsets.Value ? "1" : "0"; }
        set { this._noSubsets = XmlConvert.ToBoolean(value); }
    }

    public bool ShouldSerializenoSubsets()
    {
        return _noSubsets.HasValue;
    }


    [XmlIgnore]
    private uint? _fixedWidth;

    [XmlAttribute("fixedWidth", Namespace = "")]
    public string fixedWidth
    {
        get { return this._fixedWidth.HasValue ? "1" : "0"; }
        set { this._fixedWidth = XmlConvert.ToUInt32(value); }
    }

    public bool ShouldSerializefixedWidth()
    {
        return _fixedWidth.HasValue;
    }


    [XmlIgnore]
    private uint? _fixedHeight;

    [XmlAttribute("fixedHeight", Namespace = "")]
    public string fixedHeight
    {
        get { return this._fixedHeight.HasValue ? "1" : "0"; }
        set { this._fixedHeight = XmlConvert.ToUInt32(value); }
    }

    public bool ShouldSerializefixedHeight()
    {
        return _fixedHeight.HasValue;
    }


    //[XmlElement("AuthorityURL", Namespace = "")]
    //public Authorityurl[] AuthorityURL { get; set; }


    [XmlElement("Dimension", Namespace = "")]
    public Dimension[] Dimension { get; set; }
}

public class EX_GeographicBoundingBox
{
    public double westBoundLongitude { get; set; }
    public double eastBoundLongitude { get; set; }
    public double southBoundLatitude { get; set; }
    public double northBoundLatitude { get; set; }

}



public class Authorityurl
{
    [XmlElement]
    public OnlineResource OnlineResource { get; set; }
    public string name { get; set; }
}

public class Boundingbox
{
    [XmlAttribute("CRS", Namespace = "")]
    public string crs { get; set; }

    [XmlAttribute("minx", Namespace = "")]
    public double minx { get; set; }

    [XmlAttribute("miny", Namespace = "")]
    public double miny { get; set; }

    [XmlAttribute("maxx", Namespace = "")]
    public double maxx { get; set; }

    [XmlAttribute("maxy", Namespace = "")]
    public double maxy { get; set; }

    //[XmlAttribute("res", Namespace = "")]
    //public double[] res { get; set; }

    //public bool ShouldSerializeres()
    //{
    //    return res != null;
    //}
}
  
public class Attribution
{
    public string Title { get; set; }

    [XmlElement]
    public OnlineResource OnlineResource { get; set; }

    public Logourl LogoURL { get; set; }
}

public class Logourl
{

    [XmlElement("Format", Namespace = "")]
    public string Format { get; set; }

    [XmlElement]
    public OnlineResource OnlineResource { get; set; }

    //public int[] size { get; set; }
    [XmlAttribute("width", Namespace = "")]
    public double width { get; set; }

    [XmlAttribute("height", Namespace = "")]
    public double height { get; set; }
}


public class FeatureListUrl
{
    [XmlElement("Format", Namespace = "")]
    public string Format { get; set; }
    public OnlineResource OnlineResource { get; set; }
}

public class Style
{
    public string Name { get; set; }
    public string Title { get; set; }
    public string Abstract { get; set; }

    [XmlElement("LegendURL", Namespace = "")]
    public Legendurl[] LegendURL { get; set; }
    public StyleSheetUrl StyleSheetURL { get; set; }
}

public class StyleSheetUrl
{
    [XmlElement("Format", Namespace = "")]
    public string Format { get; set; }

    [XmlElement]
    public OnlineResource OnlineResource { get; set; }
}

public class Legendurl
{
    [XmlAttribute("width", Namespace = "")]
    public double width { get; set; }

    [XmlAttribute("height", Namespace = "")]
    public double height { get; set; }

    [XmlElement("Format", Namespace = "")]
    public string Format { get; set; }

    [XmlElement]
    public OnlineResource OnlineResource { get; set; }

}

public class OnlineResource
{
    [XmlAttribute("href", Namespace = "http://www.w3.org/1999/xlink", DataType = "string")]
    public string href { get; set; }

    [XmlAttribute("type", Namespace = "http://www.w3.org/1999/xlink")]
    public string Type { get; set; }
}
 
public class MetadataUrl
{
    [XmlElement("Format", Namespace = "")]
    public string Format { get; set; }

    [XmlElement]
    public OnlineResource OnlineResource { get; set; }
    public string type { get; set; }
}

public class Dimension
{
    public string name { get; set; }
    public string units { get; set; }
    public object unitSymbol { get; set; }
    public string _default { get; set; }
    public string values { get; set; }
    public bool nearestValue { get; set; }
}