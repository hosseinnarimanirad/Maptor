using System;
using System.Collections.Generic;
using System.Globalization;
using IRI.Maptor.Sta.KmlFormat;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

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
            if (feature.Attributes.TryGetValue(KmlAttributeKeys.StyleMetadata, out var styleMetadataObj) &&
                styleMetadataObj is KmlStyleMetadata styleMetadata)
            {
                kmlFeature.Style = styleMetadata;
            }
            else
            {
                kmlFeature.Style = null;
            }

            if (feature.Attributes.TryGetValue(KmlAttributeKeys.IconHref, out var iconHrefObj) &&
                iconHrefObj is string iconHref &&
                !string.IsNullOrWhiteSpace(iconHref))
            {
                kmlFeature.Style ??= new KmlStyleMetadata();
                kmlFeature.Style.IconHref = iconHref;
            }

            if (feature.Attributes.TryGetValue(KmlAttributeKeys.IconScale, out var iconScaleObj))
            {
                if (iconScaleObj is double iconScale)
                {
                    kmlFeature.Style ??= new KmlStyleMetadata();
                    kmlFeature.Style.IconScale = iconScale;
                }
                else if (iconScaleObj is string iconScaleString &&
                         double.TryParse(iconScaleString, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedScale))
                {
                    kmlFeature.Style ??= new KmlStyleMetadata();
                    kmlFeature.Style.IconScale = parsedScale;
                }
            }

            if (feature.Attributes.TryGetValue(KmlAttributeKeys.RegionMetadata, out var regionMetadataObj) &&
                regionMetadataObj is KmlRegionMetadata regionMetadata)
            {
                kmlFeature.Region = regionMetadata;
            }

            foreach (var kvp in feature.Attributes)
            {
                if (kvp.Value is null)
                {
                    continue;
                }

                if (kvp.Key.Equals(KmlAttributeKeys.StyleMetadata, StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals(KmlAttributeKeys.RegionMetadata, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = kvp.Value switch
                {
                    string s => s,
                    IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                    _ => kvp.Value.ToString() ?? string.Empty
                };

                if (kvp.Key.Equals(KmlAttributeKeys.NameAttributeKey, StringComparison.OrdinalIgnoreCase))
                {
                    kmlFeature.Name = value;
                }
                else if (kvp.Key.Equals(KmlAttributeKeys.DescriptionAttributeKey, StringComparison.OrdinalIgnoreCase))
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

    public static List<Feature<Point>> ToFeatures(this IEnumerable<IGeometry> geometries, bool assignSequentialIds = true)
    {
        var result = new List<Feature<Point>>();

        if (geometries == null)
        {
            return result;
        }

        var id = 0;

        foreach (var geometry in geometries)
        {
            if (geometry == null || geometry.IsEmpty())
                continue;

            if (geometries as Geometry<Point> is null)
                continue;

            var feature = new Feature<Point>(geometry as Geometry<Point>)
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
