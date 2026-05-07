using System.Reflection;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Attributes;

namespace IRI.Maptor.Sta.Common.Primitives;

public class Field
{
    public string Name { get; set; }

    public string TypeFullName { get; set; }

    public string? Alias { get; set; }

    public int Length { get; set; }

    public bool IsNullable { get; set; }

    public int Precision { get; set; }

    public int Scale { get; set; }

    public int DateTimePrecision { get; set; }

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

            fields.Add(new Field
            {
                Name = property.Name,
                Alias = fieldAttribute?.Alias ?? property.Name, // Use property name if no alias
                TypeFullName = propertyType.FullName/*.ToString()*/,
                IsNullable = isNullable,
                Length = fieldAttribute?.Length ?? 0,
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
                IsNullable = isNullable
            });
        }

        return fields;
    }
}
