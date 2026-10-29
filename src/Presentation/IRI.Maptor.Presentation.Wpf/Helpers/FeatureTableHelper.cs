using System;
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

public static class FeatureTableHelper
{
    public static readonly string NetTopologySuiteColumnName = "NetTopologySuite";

    /// <summary>
    /// The single rule for "is this field something a user should see as an attribute".
    /// Mirrors the column rules of <c>DataGridDictionaryBehavior</c> (attribute table), so the
    /// identify details pane and the table never disagree about which fields exist:
    /// geometry columns, fields of a type the client cannot name (an entity's navigation
    /// property, ...), <c>rowversion</c> and <see cref="Field.CanRead"/> = false are all hidden.
    /// </summary>
    public static bool IsDisplayableField(Field? field)
    {
        if (field is null || string.IsNullOrWhiteSpace(field.Name))
            return false;

        var dataType = field.ResolveDataType();

        if (dataType == FieldDataType.Geometry)
            return false;

        if (dataType == FieldDataType.Unknown && !IsClrTypeName(field.TypeFullName))
            return false;

        if (field.Name.EqualsIgnoreCase("rowversion"))
            return false;

        return field.CanRead;
    }

    /// <summary>A geometry column (by its logical type, or by its .NET type name from an older server).</summary>
    public static bool IsGeometryField(Field? field) => field?.ResolveDataType() == FieldDataType.Geometry;

    private static bool IsClrTypeName(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        try
        {
            return Type.GetType(typeName, throwOnError: false) is not null;
        }
        catch
        {
            return false;
        }
    }
}
