using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Sta.Common.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class GeometryAttribute : Attribute
{
    public GeometryCategory Category { get; set; }

    public bool IsMultiPart { get; set; }

    public bool IsRingBase { get; set; }

    public GeometryAttribute(GeometryCategory category, bool isMultiPart, bool isRingBase)
    {
        this.Category = category;
        IsMultiPart = isMultiPart;
        IsRingBase = isRingBase;
    }
}
