using IRI.Maptor.Core.Common.Attributes;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using System;
using System.Collections.Generic;
using System.Text;


namespace IRI.Maptor.Extensions;

public static class IriProvince93Extensions
{
    public static string GetPathMarkup(this IriProvince93 province) => province.GetAttribute<IriProvince93PathMarkupAttribute>()?.PathMarkup ?? string.Empty;

    public static BoundingBox GetWebMercatorExtent(this IriProvince93 province)
    {
        var wmBase64Envelope = province.GetAttribute<IriProvince93WmBoundingBoxAttribute>()?.WmBoundingBox;

        if (string.IsNullOrWhiteSpace(wmBase64Envelope))
            return BoundingBox.NaN;

        byte[] envelope = Convert.FromBase64String(wmBase64Envelope);

        var geometry = Geometry<Point>.FromWkb(envelope, SridHelper.WebMercator);

        return geometry.GetBoundingBox();
    }

    public static string GetTitle(this IriProvince93 province)
    {
        return province.GetDescription();
    }
}