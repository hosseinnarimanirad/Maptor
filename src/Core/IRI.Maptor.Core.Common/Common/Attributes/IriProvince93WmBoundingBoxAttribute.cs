using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class IriProvince93WmBoundingBoxAttribute : Attribute
{
    /// <summary>
    /// Base64 envelope in WebMercator
    /// </summary>
    public string WmBoundingBox { get; set; }

    public IriProvince93WmBoundingBoxAttribute(string wmBoundingBox)
    {
        this.WmBoundingBox = wmBoundingBox;
    }
}
