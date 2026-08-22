using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Ogc.Kml;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Extensions;
 
public static partial class KmlFeatureExtensions
{ 
    public static Feature<Point>? ToFeature(this KmlFeature kmlFeature, int? id = null)
    {
        if (kmlFeature == null || kmlFeature.Geometry == null || kmlFeature.Geometry.IsNullOrEmpty())
        {
            return null;
        }

        var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(kmlFeature.Name))
        {
            attributes[KmlAttributeKeys.NameAttributeKey] = kmlFeature.Name!;
        }

        if (!string.IsNullOrWhiteSpace(kmlFeature.Description))
        {
            attributes[KmlAttributeKeys.DescriptionAttributeKey] = kmlFeature.Description!;
        }

        if (kmlFeature.Attributes != null)
        {
            foreach (var kvp in kmlFeature.Attributes)
            {
                if (!attributes.ContainsKey(kvp.Key))
                {
                    attributes[kvp.Key] = kvp.Value ?? string.Empty;
                }
            }
        }

        AppendStyleMetadata(kmlFeature, attributes);
        AppendRegionMetadata(kmlFeature, attributes);

        var feature = new Feature<Point>(kmlFeature.Geometry, attributes);

        if (id.HasValue)
        {
            feature.Id = id.Value;
        }

        if (attributes.ContainsKey(KmlAttributeKeys.NameAttributeKey))
        {
            feature.LabelAttribute = KmlAttributeKeys.NameAttributeKey;
        }

        return feature;
    }

    public static List<Feature<Point>> ToFeatures(this IEnumerable<KmlFeature> kmlFeatures, bool assignSequentialIds = true)
    {
        var result = new List<Feature<Point>>();

        if (kmlFeatures == null)
        {
            return result;
        }

        var id = 0;

        foreach (var kmlFeature in kmlFeatures)
        {
            var feature = kmlFeature.ToFeature(assignSequentialIds ? id : null);
            if (feature != null)
            {
                result.Add(feature);

                if (assignSequentialIds)
                {
                    id++;
                }
            }
        }

        return result;
    }

    private static void AppendStyleMetadata(KmlFeature kmlFeature, Dictionary<string, object> attributes)
    {
        if (kmlFeature.Style == null)
            return;

        attributes[KmlAttributeKeys.StyleMetadata] = kmlFeature.Style;

        if (!string.IsNullOrWhiteSpace(kmlFeature.Style.StyleId))
        {
            attributes[KmlAttributeKeys.StyleId] = kmlFeature.Style.StyleId!;
        }

        if (!string.IsNullOrWhiteSpace(kmlFeature.Style.StyleUrl))
        {
            attributes[KmlAttributeKeys.StyleUrl] = kmlFeature.Style.StyleUrl!;
        }

        if (kmlFeature.Style.IsStyleMap)
        {
            attributes[KmlAttributeKeys.StyleIsMap] = true;
        }

        var styleKey = CreateStyleKey(kmlFeature.Style);

        if (!string.IsNullOrWhiteSpace(styleKey))
        {
            attributes[KmlAttributeKeys.StyleKey] = styleKey!;
        }

        if (!string.IsNullOrWhiteSpace(kmlFeature.Style.IconHref))
        {
            attributes[KmlAttributeKeys.IconHref] = kmlFeature.Style.IconHref!;
        }

        if (kmlFeature.Style.IconScale.HasValue)
        {
            attributes[KmlAttributeKeys.IconScale] = kmlFeature.Style.IconScale.Value;
        }
    }

    private static void AppendRegionMetadata(KmlFeature kmlFeature, Dictionary<string, object> attributes)
    {
        if (kmlFeature.Region == null)
            return;

        attributes[KmlAttributeKeys.RegionMetadata] = kmlFeature.Region;
    }

    private static string? CreateStyleKey(KmlStyleMetadata styleMetadata)
    {
        if (!string.IsNullOrWhiteSpace(styleMetadata.StyleId))
            return styleMetadata.StyleId;

        if (!string.IsNullOrWhiteSpace(styleMetadata.StyleUrl))
            return styleMetadata.StyleUrl.Trim();

        var representativeStyle = styleMetadata.InlineStyle ?? styleMetadata.NormalStyle;
        if (representativeStyle == null)
            return null;

        var xml = representativeStyle.ToString(SaveOptions.DisableFormatting);
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(xml));
        var hashString = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();

        var prefixLength = Math.Min(hashString.Length, 16);
        return $"inline-{hashString.Substring(0, prefixLength)}";
    }
}
