using IRI.Maptor.Core.Common.Attributes;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Presentation.Wpf.Models.Identify;

/// <summary>
/// One name/value line in the identify details pane. Immutable; built by
/// <c>IdentifyAttributeHelper.BuildRows</c>.
/// </summary>
public class IdentifyAttributeRow
{
    public IdentifyAttributeRow(string name, string displayName, object? value, string displayText, Field? field, bool isNumeric, bool isDateTime)
    {
        Name = name;
        DisplayName = displayName;
        Value = value;
        DisplayText = displayText;
        Field = field;
        IsNumeric = isNumeric;
        IsDateTime = isDateTime;
    }

    /// <summary>Attribute key as stored on the feature.</summary>
    public string Name { get; }

    /// <summary>Field alias when the schema has one, otherwise <see cref="Name"/>.</summary>
    public string DisplayName { get; }

    public object? Value { get; }

    /// <summary>Formatted text (numbers/dates localized, null shown as an en dash).</summary>
    public string DisplayText { get; }

    /// <summary>Schema field; null when the feature carries an attribute the layer's fields do not declare.</summary>
    public Field? Field { get; }

    public bool IsInSchema => Field is not null;

    public bool IsNumeric { get; }

    public bool IsDateTime { get; }

    public bool IsNull => Value is null || Value is System.DBNull;

    public FieldTextDirection TextDirection => Field?.TextDirection ?? FieldTextDirection.Auto;

    public override string ToString() => $"{DisplayName}: {DisplayText}";
}
