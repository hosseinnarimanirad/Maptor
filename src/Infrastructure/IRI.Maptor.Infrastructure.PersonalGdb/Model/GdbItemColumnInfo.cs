using IRI.Maptor.Infrastructure.PersonalGdb.Enums;
using IRI.Maptor.Infrastructure.PersonalGdb.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Infrastructure.PersonalGdb.Model;

public class GdbItemColumnInfo
{
    public const string GeometryFieldType = "esriFieldTypeGeometry";

    public string Name { get; set; }

    public string AliasName { get; set; }

    public string ModelName { get; set; }

    public string DomainName { get; set; }

    public string FieldType { get; set; }

    public bool IsNullable { get; set; }

    public bool Required { get; set; }

    public bool IsGeometry => FieldType == GdbEsriFieldType.esriFieldTypeGeometry.ToString();

    public int Order { get; set; }

    public static GdbItemColumnInfo Parse(GdbXml_FieldInfo info)
    {
        return new GdbItemColumnInfo()
        {
            Name = info.Name,
            AliasName = info.AliasName,
            ModelName = info.ModelName,
            DomainName = info.DomainName,
            FieldType = info.FieldType,
            IsNullable = info.IsNullable,
            Required = info.Required,
            
        };
    }

    public override string ToString()
    {
        return $"Name: {Name}, AliasName: {AliasName}, DomainName: {DomainName}, FieldType: {FieldType}, IsNullable: {IsNullable}, IsGeometry: {IsGeometry}";
    }
}
