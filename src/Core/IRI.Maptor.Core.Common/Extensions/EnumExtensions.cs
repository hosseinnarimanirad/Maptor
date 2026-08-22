using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        // variables  
        var enumType = value.GetType();

        var field = enumType.GetField(value.ToString());

        if (field == null)
            return value.ToString();

        var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);

        return attributes.Length == 0 ? value.ToString() : ((DescriptionAttribute)attributes[0]).Description;

        //return enumValue.GetType()
        //     .GetMember(enumValue.ToString())
        //     ?.First()
        //     ?.GetCustomAttribute<DescriptionAttribute>()
        //     ?.Description ?? string.Empty;
    }

    public static string GetDescription(object value)
    {
        return value.GetType()
              .GetMember(value.ToString())
              ?.First()
              ?.GetCustomAttribute<DescriptionAttribute>()
              ?.Description ?? string.Empty;
    }

    //public static IEnumerable<T> GetEnumValues<T>(this T input) where T : struct
    //{
    //    if (!typeof(T).IsEnum)
    //        throw new NotSupportedException();

    //    return Enum.GetValues(input.GetType()).Cast<T>();
    //}

    public static List<T> GetEnums<T>()
    {
        if (!typeof(T).IsEnum)
            throw new NotSupportedException();

        return Enum.GetValues(typeof(T)).Cast<T>().ToList();
    }

    public static string GetName(this Enum enumValue)
    {
        return Enum.GetName(enumValue.GetType(), enumValue);
    }


    public static List<EnumInfo> Parse<T>() where T : struct, IConvertible
    {
        var type = typeof(T);

        return GetEnums<T>().Select(e => new EnumInfo()
        {
            Id = (int)(object)e,
            PropertyNameEn = Enum.GetName(type, e),
            PropertyNameFa = GetDescription(e)
        }).ToList();
    }


    //public static List<T> GetEnums<T>()
    //{
    //    return Enum.GetValues(typeof(T)).Cast<T>().ToList();
    //}

    public static List<Enum> GetValues(this Enum value)
    {
        return Enum.GetValues(value.GetType()).Cast<Enum>().ToList();
    }


    public static IEnumerable<T> GetEnumFlags<T>(this T value) where T : struct
    {
        if (!typeof(T).IsEnum)
            throw new NotSupportedException();

        foreach (var enumItem in Enum.GetValues(value.GetType()))
            if ((value as Enum).HasFlag(enumItem as Enum))
                yield return (T)enumItem;
    }

    //public static string ToDisplay(this Enum value, DisplayProperty property = DisplayProperty.Name)
    //{
    //    //Assert.NotNull(value, nameof(value));

    //    var attribute = value.GetType().GetField(value.ToString())
    //        .GetCustomAttributes<DisplayAttribute>(false).FirstOrDefault();

    //    if (attribute == null)
    //        return value.ToString();

    //    var propValue = attribute.GetType().GetProperty(property.ToString()).GetValue(attribute, null);
    //    return propValue.ToString();
    //}

    //public static Dictionary<int, string> ToDictionary(this Enum value)
    //{
    //    return Enum.GetValues(value.GetType()).Cast<Enum>().ToDictionary(p => Convert.ToInt32(p), q => ToDisplay(q));
    //}


    public static T ConvertToFlag<T>(this IEnumerable<T> flags) where T : struct, IConvertible
    {
        if (!typeof(T).IsEnum)
            throw new NotSupportedException($"{typeof(T).ToString()} must be an enumerated type");

        return (T)(object)flags.Cast<int>().Aggregate(0, (c, n) => c |= n);
    }

    public static TAttribute? GetAttribute<TAttribute>(this Enum value) where TAttribute : Attribute
    {
        var type = value.GetType();
        var memberInfo = type.GetMember(value.ToString());

        if (memberInfo.Length == 0)
            return null;

        var attributes = memberInfo[0].GetCustomAttributes(typeof(TAttribute), false);
        return attributes.Length > 0 ? (TAttribute)attributes[0] : null;
    }
}
 
//public enum DisplayProperty
//{
//    Description,
//    GroupName,
//    Name,
//    Prompt,
//    ShortName,
//    Order
//}
