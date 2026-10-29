using System;

using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Common.Primitives;

/// <summary>Maps .NET types to <see cref="FieldDataType"/>.</summary>
public static class FieldDataTypes
{
    /// <summary>
    /// The logical type of values of <paramref name="type"/>; <c>Nullable&lt;T&gt;</c> is unwrapped. Enums are
    /// <see cref="FieldDataType.Text"/>: the APIs send them as names. Unknown CLR types (navigation properties,
    /// <c>object</c>, ...) give <see cref="FieldDataType.Unknown"/>.
    /// </summary>
    public static FieldDataType FromClrType(Type? type)
    {
        if (type is null)
            return FieldDataType.Unknown;

        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum || type == typeof(string) || type == typeof(char))
            return FieldDataType.Text;

        if (type == typeof(bool))
            return FieldDataType.Boolean;

        if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int))
            return FieldDataType.Integer;

        if (type == typeof(long) || type == typeof(uint) || type == typeof(ulong))
            return FieldDataType.Long;

        if (type == typeof(float) || type == typeof(double))
            return FieldDataType.Double;

        if (type == typeof(decimal))
            return FieldDataType.Decimal;

        // DateOnly is not in netstandard2.1: matched by name
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type.FullName == "System.DateOnly")
            return FieldDataType.DateTime;

        if (type == typeof(Guid))
            return FieldDataType.Guid;

        if (type == typeof(byte[]))
            return FieldDataType.Binary;

        if (IsGeometryTypeName(type.FullName))
            return FieldDataType.Geometry;

        return FieldDataType.Unknown;
    }

    /// <summary>
    /// The logical type named by a .NET type name such as <see cref="Field.TypeFullName"/> (assembly-qualified
    /// generic names included). A geometry type is recognised by its name even when its assembly is not loaded.
    /// Other names (e.g. SQL type names some sources put there) give <see cref="FieldDataType.Unknown"/>.
    /// </summary>
    public static FieldDataType FromTypeName(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return FieldDataType.Unknown;

        Type? type = null;

        try
        {
            type = Type.GetType(typeName, throwOnError: false);
        }
        catch
        {
            // a malformed name is just an unknown one
        }

        if (type is not null)
            return FromClrType(type);

        return IsGeometryTypeName(typeName) ? FieldDataType.Geometry : FieldDataType.Unknown;
    }

    /// <summary>Integer, Long, Double or Decimal.</summary>
    public static bool IsNumeric(this FieldDataType type) =>
        type is FieldDataType.Integer or FieldDataType.Long or FieldDataType.Double or FieldDataType.Decimal;

    // NetTopologySuite (the servers' entities), SQL Server types, and the engine's own Geometry<T>
    private static bool IsGeometryTypeName(string? typeName) =>
        typeName is not null &&
        (typeName.IndexOf("NetTopologySuite", StringComparison.OrdinalIgnoreCase) >= 0 ||
         typeName.StartsWith("Microsoft.SqlServer.Types.SqlGeometry", StringComparison.Ordinal) ||
         typeName.StartsWith("Microsoft.SqlServer.Types.SqlGeography", StringComparison.Ordinal) ||
         typeName.StartsWith("IRI.Maptor.Core.Spatial.Primitives.Geometry`1", StringComparison.Ordinal));
}
