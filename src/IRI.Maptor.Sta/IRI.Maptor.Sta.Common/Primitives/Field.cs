using System.Reflection;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Attributes;

namespace IRI.Maptor.Sta.Common.Primitives;

public class Field
{
    public string Name { get; set; }

    public string Type { get; set; }

    public string? Alias { get; set; }

    public int Length { get; set; }

    public bool IsNullable { get; set; }

    public int Precision { get; set; }

    public int Scale { get; set; }

    public int DateTimePrecision { get; set; }

    public override string ToString()
    {
        return $"Name: {Name}; Type: {Type}; Length: {Length}; IsNullable: {IsNullable}; NumericPrecision: {Precision}; NumericScale: {Scale}; DateTimePrecision: {DateTimePrecision}";
    }

    /// <summary>
    /// Returns the default value for this field based on its Type and IsNullable property.
    /// </summary>
    public object GetDefaultValue()
    {
        if (IsNullable)
            return null!;

        var typeStr = Type;
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

            fields.Add(new Field
            {
                Name = property.Name,
                Alias = fieldAttribute?.Alias ?? property.Name, // Use property name if no alias
                Type = property.PropertyType.ToString(),
            });
        }

        return fields;
    }

    public static List<Field> FromDictionary(Dictionary<string, object>? dict)
    {
        var fields = new List<Field>();

        if (dict is null)
            return fields;

        foreach (var kvp in dict)
        {
            fields.Add(new Field
            {
                Name = kvp.Key,
                Alias = kvp.Key,
                Type = kvp.Value?.GetType().Name ?? "object",
                IsNullable = kvp.Value == null
            });
        }

        return fields;
    }
}
