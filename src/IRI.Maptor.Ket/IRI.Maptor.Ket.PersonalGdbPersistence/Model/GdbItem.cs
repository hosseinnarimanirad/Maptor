using IRI.Maptor.Ket.PersonalGdbPersistence.Xml;
using IRI.Maptor.Sta.Common.Helpers;

namespace IRI.Maptor.Ket.PersonalGdbPersistence.Model;

public class GdbItem
{
    public string Name { get; set; }

    public string PhysicalName { get; set; }

    public string Path { get; set; }

    public string Definition { get; set; }

    public GdbXml_FeatureClass? DefinitionInfo => string.IsNullOrWhiteSpace(Definition) ?
                                                null :
                                                XmlHelper.DeserializeFromXmlString<GdbXml_FeatureClass>(Definition);

    public string? AliasName => DefinitionInfo?.AliasName;

    public string? ShapeType => DefinitionInfo?.ShapeType;

    public string? ShapeFieldName => DefinitionInfo?.ShapeFieldName;

    public bool HasM => DefinitionInfo?.HasM ?? false;

    public bool HasZ => DefinitionInfo?.HasZ ?? false;


    public override string ToString() => $"Name: {Name}, AliasName: {AliasName}, Path:{Path}, ShapeType: {ShapeType}";
}
