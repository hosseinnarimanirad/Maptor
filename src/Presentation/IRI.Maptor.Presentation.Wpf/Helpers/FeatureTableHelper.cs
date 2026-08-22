using System;
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

public static class FeatureTableHelper
{
    public static readonly string NetTopologySuiteColumnName = "NetTopologySuite";

    /// <summary>
    /// The single rule for "is this field something a user should see as an attribute".
    /// Mirrors the column rules of <c>DataGridDictionaryBehavior</c> (attribute table), so the
    /// identify details pane and the table never disagree about which fields exist:
    /// unresolvable CLR type, NetTopologySuite geometry columns, <c>rowversion</c> and
    /// <see cref="Field.CanRead"/> = false are all hidden.
    /// </summary>
    public static bool IsDisplayableField(Field? field)
    {
        if (field is null || string.IsNullOrWhiteSpace(field.Name))
            return false;

        if (string.IsNullOrWhiteSpace(field.TypeFullName) || Type.GetType(field.TypeFullName) is null)
            return false;

        if (field.TypeFullName.ContainsIgnoreCase(NetTopologySuiteColumnName))
            return false;

        if (field.Name.EqualsIgnoreCase("rowversion"))
            return false;

        return field.CanRead;
    }
}
