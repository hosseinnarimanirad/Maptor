using System.Globalization;

namespace IRI.Maptor.Core.FileGdb.Catalog.Model;

/// <summary>
/// An attribute domain: a coded-value list or a numeric range.
/// </summary>
public abstract class FileGdbDomain
{
    private protected FileGdbDomain(string name, string? description, string? fieldType)
    {
        Name = name;
        Description = description;
        FieldType = fieldType;
    }

    public string Name { get; }

    public string? Description { get; }

    /// <summary>Esri field type the domain applies to, e.g. <c>esriFieldTypeSmallInteger</c>.</summary>
    public string? FieldType { get; }

    public override string ToString() => Name;
}

public sealed class FileGdbCodedValue
{
    internal FileGdbCodedValue(object code, string name)
    {
        Code = code;
        Name = name;
    }

    /// <summary>Typed as declared in the XML (<see cref="short"/>, <see cref="int"/>, <see cref="string"/>, ...).</summary>
    public object Code { get; }

    public string Name { get; }

    public override string ToString() => $"{Code} = {Name}";
}

public sealed class FileGdbCodedValueDomain : FileGdbDomain
{
    private readonly Dictionary<object, string> _nameByCode;

    internal FileGdbCodedValueDomain(string name, string? description, string? fieldType, IReadOnlyList<FileGdbCodedValue> codedValues)
        : base(name, description, fieldType)
    {
        CodedValues = codedValues;

        _nameByCode = new Dictionary<object, string>();

        // first occurrence wins, as a duplicate code is a data error
        foreach (var codedValue in codedValues)
        {
            var key = NormalizeCode(codedValue.Code);

            if (key is not null && !_nameByCode.ContainsKey(key))
                _nameByCode.Add(key, codedValue.Name);
        }
    }

    public IReadOnlyList<FileGdbCodedValue> CodedValues { get; }

    /// <summary>
    /// The name of a row value. The row value's type does not need to match the declared code
    /// type: a field read as <see cref="short"/> finds a code declared as <c>xs:int</c>.
    /// </summary>
    public bool TryGetName(object? value, out string name)
    {
        var key = NormalizeCode(value);

        if (key is not null && _nameByCode.TryGetValue(key, out var found))
        {
            name = found;
            return true;
        }

        name = string.Empty;
        return false;
    }

    /// <summary>
    /// Integral numbers (of any CLR type, including whole floating values) become <see cref="long"/>,
    /// other numbers <see cref="double"/>, so equal values compare equal regardless of storage type.
    /// </summary>
    internal static object? NormalizeCode(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case short or int or long or byte or sbyte or ushort or uint:
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            case float or double or decimal:
                var d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                // the object casts matter: without them the conditional is typed double and boxes 1.0, not 1L
                return d == Math.Floor(d) && d >= long.MinValue && d <= long.MaxValue ? (object)(long)d : (object)d;
            default:
                return value;
        }
    }
}

public sealed class FileGdbRangeDomain : FileGdbDomain
{
    internal FileGdbRangeDomain(string name, string? description, string? fieldType, object? minValue, object? maxValue)
        : base(name, description, fieldType)
    {
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public object? MinValue { get; }

    public object? MaxValue { get; }
}
