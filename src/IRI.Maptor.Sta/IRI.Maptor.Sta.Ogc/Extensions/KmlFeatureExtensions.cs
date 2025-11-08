using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Ket.KmlFormat;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Extensions;
 
public static class KmlFeatureExtensions
{
    internal const string NameAttributeKey = "Name";
    internal const string DescriptionAttributeKey = "Description";

    public static Feature<Point>? ToFeature(this KmlFeature kmlFeature, int? id = null)
    {
        if (kmlFeature == null || kmlFeature.Geometry == null || kmlFeature.Geometry.IsNullOrEmpty())
        {
            return null;
        }

        var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(kmlFeature.Name))
        {
            attributes[NameAttributeKey] = kmlFeature.Name!;
        }

        if (!string.IsNullOrWhiteSpace(kmlFeature.Description))
        {
            attributes[DescriptionAttributeKey] = kmlFeature.Description!;
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

        var feature = new Feature<Point>(kmlFeature.Geometry, attributes);

        if (id.HasValue)
        {
            feature.Id = id.Value;
        }

        if (attributes.ContainsKey(NameAttributeKey))
        {
            feature.LabelAttribute = NameAttributeKey;
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

}

