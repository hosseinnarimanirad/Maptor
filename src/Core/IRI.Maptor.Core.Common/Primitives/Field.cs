using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Attributes;
using IRI.Maptor.Core.Common.Common.JsonConverters;

namespace IRI.Maptor.Core.Common.Primitives;

public class Field
{
    public string Name { get; set; }

    /// <summary>
    /// The producer's native type name: a .NET type name from the servers and the WPF sources, but an SQL type
    /// name (e.g. "geometry", "nvarchar") in the SQL Server / Personal GDB schema readers. Kept as is for them;
    /// clients ask <see cref="ResolveDataType"/> instead of parsing it.
    /// </summary>
    public string TypeFullName { get; set; }

    /// <summary>The logical type of the values; <see cref="FieldDataType.Unknown"/> when the producer did not say.</summary>
    [JsonConverter(typeof(FieldDataTypeJsonConverter))]
    public FieldDataType DataType { get; set; }

    public string? Alias { get; set; }

    public int Length { get; set; }

    public bool IsNullable { get; set; }

    public int Precision { get; set; }

    public int Scale { get; set; }

    public int DateTimePrecision { get; set; }

    public bool CanRead { get; set; } = true;

    public bool CanWrite { get; set; } = true;

    /// <summary>
    /// The values the field may hold, with their codes; null for a free field. Declared with a
    /// <see cref="FieldDomainAttribute"/> or <see cref="FieldAttribute.AllowedValues"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FieldDomain? Domain { get; set; }

    public string? DisplayFormat { get; set; }

    public FieldTextDirection TextDirection { get; set; } = FieldTextDirection.Auto;

    /// <summary>
    /// The field's logical type: <see cref="DataType"/> when the producer sent it, else what
    /// <see cref="TypeFullName"/> names (an older server, a source that only typed by .NET name), else
    /// <see cref="FieldDataType.Unknown"/>.
    /// </summary>
    public FieldDataType ResolveDataType() =>
        DataType != FieldDataType.Unknown ? DataType : FieldDataTypes.FromTypeName(TypeFullName);

    public override string ToString()
    {
        return $"Name: {Name}; Length: {Length}; IsNullable: {IsNullable}; NumericPrecision: {Precision}; NumericScale: {Scale}; Type: {TypeFullName}";
    }

    /// <summary>
    /// Returns the default value for this field based on its Type and IsNullable property.
    /// </summary>
    public object GetDefaultValue()
    {
        if (IsNullable)
            return null!;

        var typeStr = TypeFullName;
        if (string.IsNullOrEmpty(typeStr))
            return null!;

        // Extract inner type from Nullable`1[System.Int32] format when IsNullable is false
        if (typeStr.Contains("Nullable`1[", StringComparison.OrdinalIgnoreCase))
        {
            var start = typeStr.IndexOf('[', StringComparison.Ordinal) + 1;
            var end = typeStr.LastIndexOf(']');
            if (start > 0 && end > start)
                typeStr = typeStr.Substring(start, end - start);
        }

        typeStr = typeStr.Trim();

        // Integer types
        if (typeStr.EndsWith("Int32", StringComparison.OrdinalIgnoreCase) ||
            typeStr.EndsWith("Int64", StringComparison.OrdinalIgnoreCase) ||
            typeStr.EndsWith("Int16", StringComparison.OrdinalIgnoreCase) ||
            typeStr.EndsWith("Byte", StringComparison.OrdinalIgnoreCase) && !typeStr.StartsWith("System.Byte[]", StringComparison.OrdinalIgnoreCase))
            return 0;

        // Floating point
        if (typeStr.EndsWith("Double", StringComparison.OrdinalIgnoreCase) ||
            typeStr.EndsWith("Single", StringComparison.OrdinalIgnoreCase))
            return 0.0;

        if (typeStr.EndsWith("Decimal", StringComparison.OrdinalIgnoreCase))
            return 0m;

        if (typeStr.EndsWith("Boolean", StringComparison.OrdinalIgnoreCase))
            return false;

        if (typeStr.EndsWith("DateTime", StringComparison.OrdinalIgnoreCase))
            return default(DateTime);

        if (typeStr.EndsWith("Guid", StringComparison.OrdinalIgnoreCase))
            return Guid.Empty;

        // Reference types and unknown
        return null!;
    }

    public static List<Field> GetFields<T>()
    {
        var fields = new List<Field>();

        // Get all public properties of the type
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var fieldAttribute = property.GetCustomAttribute<FieldAttribute>();

            if (fieldAttribute?.CanRead == false)
                continue;

            // Determine the underlying type (unwrap Nullable<T>)
            var propertyType = property.PropertyType;

            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            // Determine if the property is nullable
            bool isNullable = Nullable.GetUnderlyingType(propertyType) != null || !propertyType.IsValueType;

            // an application's domain (e.g. coded values) wins; a plain AllowedValues list is a domain too
            var domain = property.GetCustomAttribute<FieldDomainAttribute>()?.GetDomain() ?? FieldDomain.FromValues(fieldAttribute?.AllowedValues);

            fields.Add(new Field
            {
                Name = property.Name,
                Alias = fieldAttribute?.Alias ?? property.Name, // Use property name if no alias
                TypeFullName = propertyType.FullName/*.ToString()*/,
                DataType = FieldDataTypes.FromClrType(propertyType),
                IsNullable = isNullable,
                Length = fieldAttribute?.Length ?? 0,
                CanRead = fieldAttribute?.CanRead ?? true,
                CanWrite = fieldAttribute?.CanWrite ?? true,
                Domain = domain,
                DisplayFormat = fieldAttribute?.DisplayFormat,
                TextDirection = fieldAttribute?.TextDirection ?? FieldTextDirection.Auto,
                Precision = GetDefaultPrecision(underlyingType),
                Scale = GetDefaultScale(underlyingType),
                DateTimePrecision = GetDefaultDateTimePrecision(underlyingType)
            });
        }

        return fields;
    }

    private static int GetDefaultPrecision(Type type)
    {
        return type == typeof(decimal) ? 18 : 0;
    }

    private static int GetDefaultScale(Type type)
    {
        return type == typeof(decimal) ? 2 : 0;
    }

    private static int GetDefaultDateTimePrecision(Type type)
    {
        return (type == typeof(DateTime) || type == typeof(DateTimeOffset)) ? 3 : 0; // milliseconds
    }

    // todo: potentially error-prone
    public static List<Field> FromDictionary(Dictionary<string, object>? dict)
    {
        var fields = new List<Field>();

        if (dict is null)
            return fields;

        foreach (var kvp in dict)
        {
            var value = kvp.Value;
            var type = value?.GetType();

            // Unwrap Nullable<T> if present
            var underlyingType = Nullable.GetUnderlyingType(type ?? typeof(object)) ?? type;

            // Determine nullability
            bool isNullable = value is null || (type != null && Nullable.GetUnderlyingType(type) != null);

            fields.Add(new Field
            {
                Name = kvp.Key,
                Alias = kvp.Key,
                TypeFullName = kvp.Value?.GetType().FullName/*Name*/ ?? "object",
                // an empty value says nothing: Unknown, and readers look at the data
                DataType = FieldDataTypes.FromClrType(kvp.Value?.GetType()),
                IsNullable = isNullable
            });
        }

        return fields;
    }
}
