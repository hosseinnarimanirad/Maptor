using IRI.Maptor.Core.Common.Attributes;
using IRI.Maptor.Core.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Extensions;

public static class GeometryTypeExtensions
{
    public static GeometryCategory GetCategory(this GeometryType type)
    {
        return type.GetAttribute<GeometryAttribute>()?.Category ?? GeometryCategory.None;
    }

    public static bool IsMultiPartGeometry(this GeometryType type)
    {
        return type.GetAttribute<GeometryAttribute>()?.IsMultiPart ?? false;
    }

    public static bool IsRingBase(this GeometryType type)
    {
        return type.GetAttribute<GeometryAttribute>()?.IsRingBase ?? false;
    }
}
