using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Extensions;

public static class TypeExtensions
{
    //public static bool IsBool(this Type type)
    //{
    //    Type underlying = Nullable.GetUnderlyingType(type) ?? type;

    //    return underlying == typeof(bool);
    //}
    public static bool IsBool(this Type type) => IsOfType<bool>(type);

    public static bool IsInt(this Type type) => IsOfType<int>(type);

    public static bool IsDouble(this Type type) => IsOfType<double>(type);

    public static bool IsDateTime(this Type type) => IsOfType<DateTime>(type);


    public static bool IsOfType<T>(this Type type) where T : struct => (Nullable.GetUnderlyingType(type) ?? type) == typeof(T);

    public static bool IsNumeric(this Type type)
    {
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;

        return underlying == typeof(byte) || underlying == typeof(sbyte) ||
               underlying == typeof(short) || underlying == typeof(ushort) ||
               underlying == typeof(int) || underlying == typeof(uint) ||
               underlying == typeof(long) || underlying == typeof(ulong) ||
               underlying == typeof(float) || underlying == typeof(double) ||
               underlying == typeof(decimal);
    }

    public static bool IsNullable(this Type type)
    {
        return Nullable.GetUnderlyingType(type) != null;
    }

    public static Type GetNonNullableType(this Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }
}
