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
