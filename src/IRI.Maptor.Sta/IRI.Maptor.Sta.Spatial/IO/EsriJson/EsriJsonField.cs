using IRI.Maptor.Sta.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;

namespace IRI.Maptor.Sta.Spatial.IO.EsriJson;

public class EsriJsonField
{
    #region Mapping

    private static readonly string _defaultDotNetType = typeof(string).FullName;

    private static readonly Dictionary<string, string> _esriToDotNetTypeMap = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["esriFieldTypeSmallInteger"] = typeof(short).FullName,
        ["esriFieldTypeInteger"] = typeof(int).FullName,
        ["esriFieldTypeLong"] = typeof(long).FullName,
        ["esriFieldTypeSingle"] = typeof(float).FullName,
        ["esriFieldTypeDouble"] = typeof(double).FullName,
        ["esriFieldTypeString"] = typeof(string).FullName,
        ["esriFieldTypeDate"] = typeof(DateTime).FullName,
        ["esriFieldTypeTimestampOffset"] = typeof(DateTimeOffset).FullName,
        //["esriFieldTypeDateOnly"] = typeof(DateOnly).FullName,   // .NET 6+
        //["esriFieldTypeTimeOnly"] = typeof(TimeOnly).FullName,   // .NET 6+
        ["esriFieldTypeGUID"] = typeof(Guid).FullName,
        ["esriFieldTypeGlobalID"] = typeof(string).FullName,     // often stored as string
        ["esriFieldTypeOID"] = typeof(int).FullName,             // or long, depending on your model
        ["esriFieldTypeBigInteger"] = typeof(long).FullName,     // or System.Numerics.BigInteger
                                                                 // Add other types as needed
    };

    private static string MapDotNetTypeToEsriFieldType(string typeFullName)
    {
        if (string.IsNullOrEmpty(typeFullName))
            return "esriFieldTypeString"; // fallback

        // Mapping dictionary
        var mapping = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Integer types
            [typeof(short).FullName] = "esriFieldTypeSmallInteger",
            [typeof(ushort).FullName] = "esriFieldTypeSmallInteger",
            [typeof(int).FullName] = "esriFieldTypeInteger",
            [typeof(uint).FullName] = "esriFieldTypeInteger",
            [typeof(long).FullName] = "esriFieldTypeLong",
            [typeof(ulong).FullName] = "esriFieldTypeLong",

            // Floating point types
            [typeof(float).FullName] = "esriFieldTypeSingle",
            [typeof(double).FullName] = "esriFieldTypeDouble",
            [typeof(decimal).FullName] = "esriFieldTypeDouble", // no direct decimal; store as double

            // Text types
            [typeof(string).FullName] = "esriFieldTypeString",
            [typeof(char).FullName] = "esriFieldTypeString",

            // Date/Time types
            [typeof(DateTime).FullName] = "esriFieldTypeDate",
            [typeof(DateTimeOffset).FullName] = "esriFieldTypeTimestampOffset",
            //[typeof(DateOnly).FullName] = "esriFieldTypeDateOnly",   // .NET 6+
            //[typeof(TimeOnly).FullName] = "esriFieldTypeTimeOnly",   // .NET 6+

            // Boolean (Esri has no native boolean; store as short: 0/1)
            [typeof(bool).FullName] = "esriFieldTypeSmallInteger",

            // Byte and others
            [typeof(byte).FullName] = "esriFieldTypeSmallInteger",
            [typeof(sbyte).FullName] = "esriFieldTypeSmallInteger",
            [typeof(Guid).FullName] = "esriFieldTypeGUID",

            // Special large integer (if you need big integers beyond long)
            // For BigInteger, you could use "esriFieldTypeBigInteger"
            // [typeof(System.Numerics.BigInteger).FullName] = "esriFieldTypeBigInteger",
        };

        if (mapping.TryGetValue(typeFullName, out var esriType))
            return esriType;

        // Default fallback for unknown types
        return "esriFieldTypeString";
    }

    private static string MapEsriTypeToDotNetType(string esriType)
    {
        if (string.IsNullOrEmpty(esriType))
            return _defaultDotNetType;

        if (_esriToDotNetTypeMap.TryGetValue(esriType, out var dotnetType))
            return dotnetType;

        // Fallback for unknown Esri types
        return _defaultDotNetType;
    }

    #endregion
    public string Name { get; set; }

    // The data type (e.g., "esriFieldTypeString", "esriFieldTypeDouble", "esriFieldTypeOID"
    public string Type { get; set; }

    public string? Alias { get; set; }

    public int Length { get; set; }

    public Field AsField()
    {
        return new Field()
        {
            Name = this.Name,
            Alias = this.Alias,
            Length = this.Length,
            TypeFullName = MapEsriTypeToDotNetType(this.Type)
        };
    }

    public override string ToString() => $"Name: {Name}; Length: {Length}; Type: {Type}";

    public static EsriJsonField Parse(Field field)
    {
        return new EsriJsonField()
        {
            Name = field.Name,
            Alias = field.Alias,
            Length = field.Length,
            Type = MapDotNetTypeToEsriFieldType(field.TypeFullName)
        };
    }

}
