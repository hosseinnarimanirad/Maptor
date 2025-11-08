using IRI.Maptor.Ket.KmlFormat;
using IRI.Maptor.Sta.Spatial.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using IRI.Maptor.Sta.Common.Primitives; 
using System.Text;

namespace IRI.Maptor.Extensions;

public static class FeatureExtensions
{

    //private const string NameAttributeKey = "Name";
    //private const string DescriptionAttributeKey = "Description";

    public static KmlFeature? ToKmlFeature(this Feature<Point> feature)
    {
        if (feature?.TheGeometry == null || feature.TheGeometry.IsNullOrEmpty())
        {
            return null;
        }

        var kmlFeature = new KmlFeature
        {
            Geometry = feature.TheGeometry,
            Id = feature.Id.ToString(CultureInfo.InvariantCulture),
            Attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };

        if (feature.Attributes != null)
        {
            foreach (var kvp in feature.Attributes)
            {
                if (kvp.Value is null)
                {
                    continue;
                }

                var value = kvp.Value switch
                {
                    string s => s,
                    IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                    _ => kvp.Value.ToString() ?? string.Empty
                };

                if (kvp.Key.Equals(KmlFeatureExtensions.NameAttributeKey, StringComparison.OrdinalIgnoreCase))
                {
                    kmlFeature.Name = value;
                }
                else if (kvp.Key.Equals(KmlFeatureExtensions.DescriptionAttributeKey, StringComparison.OrdinalIgnoreCase))
                {
                    kmlFeature.Description = value;
                }

                kmlFeature.Attributes[kvp.Key] = value;
            }
        }

        if (kmlFeature.Name.IsNullOrEmpty())
        {
            kmlFeature.Name = feature.Label;
        }

        return kmlFeature;
    }

    public static List<KmlFeature> ToKmlFeatures(this IEnumerable<Feature<Point>> features)
    {
        var result = new List<KmlFeature>();

        if (features == null)
        {
            return result;
        }

        foreach (var feature in features)
        {
            var kmlFeature = feature.ToKmlFeature();
            if (kmlFeature != null)
            {
                result.Add(kmlFeature);
            }
        }

        return result;
    }

    public static List<Feature<Point>> ToFeatures(this IEnumerable<Geometry<Point>> geometries, bool assignSequentialIds = true)
    {
        var result = new List<Feature<Point>>();

        if (geometries == null)
        {
            return result;
        }

        var id = 0;

        foreach (var geometry in geometries)
        {
            if (geometry == null || geometry.IsNullOrEmpty())
            {
                continue;
            }

            var feature = new Feature<Point>(geometry)
            {
                Id = assignSequentialIds ? id : 0
            };

            result.Add(feature);

            if (assignSequentialIds)
            {
                id++;
            }
        }

        return result;
    }
}
