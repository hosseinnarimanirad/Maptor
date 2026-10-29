using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using IRI.Maptor.Core.Common.Common.JsonConverters;

namespace IRI.Maptor.Core.Common.Primitives;

/// <summary>
/// The set of values a field may hold, in their natural order: a coded-value domain of a data model, or a
/// plain list of allowed values (<see cref="Attributes.FieldAttribute.AllowedValues"/>). Sent as <see cref="Field.Domain"/>.
/// </summary>
public class FieldDomain
{
    /// <summary>The data model's name of the domain (e.g. "CI_Material_Anode"); null for a plain list.</summary>
    public string? Name { get; set; }

    public string? Description { get; set; }

    public List<FieldDomainValue> Values { get; set; } = new();

    /// <summary>The stored values, for an editor or a filter list.</summary>
    public IEnumerable<object?> GetStoredValues() => Values.Select(v => v.Value);

    /// <summary>
    /// Whether <paramref name="value"/> is one of the domain's stored values. Numbers compare by value whatever
    /// their CLR type (a short 4 from the server, an int 4 after JSON), text compares ordinally.
    /// Null is not a member: an empty field is a question of nullability, not of the domain.
    /// </summary>
    public bool Contains(object? value)
    {
        var key = GetComparableKey(value);

        if (key is null)
            return false;

        _keys ??= new HashSet<string>(Values.Select(v => GetComparableKey(v.Value)).Where(k => k is not null)!, StringComparer.Ordinal);

        return _keys.Contains(key);
    }

    // built on the first Contains: a domain is filled once (by the server, or by deserialization) and then read
    private HashSet<string>? _keys;

    private static string? GetComparableKey(object? value) => value switch
    {
        null or DBNull => null,
        string text => "s:" + text,
        bool flag => "b:" + flag,
        IConvertible number when IsNumber(number) => "n:" + number.ToDouble(CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture),
        _ => "o:" + Convert.ToString(value, CultureInfo.InvariantCulture),
    };

    private static bool IsNumber(IConvertible value) => value.GetTypeCode() is
        TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or
        TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal;

    /// <summary>A plain list (no name, no codes); null for no values.</summary>
    public static FieldDomain? FromValues(IEnumerable<object?>? values)
    {
        var list = values?.Select(v => new FieldDomainValue { Value = v }).ToList();

        return list is null || list.Count == 0 ? null : new FieldDomain { Values = list };
    }
}

/// <summary>One value of a <see cref="FieldDomain"/>.</summary>
public class FieldDomainValue
{
    /// <summary>What the field stores (text, or a number for some plain lists).</summary>
    [JsonConverter(typeof(ObjectValueConverter))]
    public object? Value { get; set; }

    /// <summary>The source's code for the value (e.g. a geodatabase coded-value code); informational.</summary>
    public int? Code { get; set; }

    /// <summary>Display text when it differs from <see cref="Value"/> (a source that stores codes); null otherwise.</summary>
    public string? Label { get; set; }

    /// <summary>What to show for the value.</summary>
    public string? GetDisplayText() => Label ?? Value?.ToString();
}
