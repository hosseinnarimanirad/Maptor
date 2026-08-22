using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IRI.Maptor.Core.Common.Helpers;

public static class EnumHelper
{
    public static T Parse<T>(string value, bool ignoreCase = true)
    {
        return (T)Enum.Parse(typeof(T), value, ignoreCase);
    }


    public static List<T> GetEnums<T>()
    {
        return Enum.GetValues(typeof(T)).Cast<T>().ToList();
    }

    public static List<Enum> GetEnums(Enum value)
    {
        return Enum.GetValues(value.GetType()).Cast<Enum>().ToList();
    }

    public static TAttribute GetAttribute<TEnum, TAttribute>(Enum value) where TAttribute : Attribute
    {
        MemberInfo memberInfo = typeof(TEnum).GetMember(value.ToString()).FirstOrDefault();

        return memberInfo.GetCustomAttribute(typeof(TAttribute)) as TAttribute;
    }

}
