using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IRI.Maptor.Sta.KmlFormat;
using IRI.Maptor.Sta.KmlFormat.Primitives;

namespace IRI.Maptor.Sta.ShapefileFormat;

static class OgcKmlMapFunctions
{
    internal static string AsKml(PlacemarkType placemark)
    {
        return AsKml(new AbstractFeatureType[] { placemark });
    }

    internal static string AsKml(AbstractFeatureType[] abstractFeatureType)
    {
        IRI.Maptor.Sta.KmlFormat.Primitives.KmlType result = new KmlType();

        IRI.Maptor.Sta.KmlFormat.Primitives.DocumentType document = new DocumentType();

        if (abstractFeatureType != null)
        {
            foreach (var feature in abstractFeatureType)
            {
                if (feature != null)
                {
                    document.AbstractFeatureGroup.Add(feature);
                }
            }
        }

        result.KmlObjectExtensionGroup.Add(document);

        return IRI.Maptor.Sta.Common.Helpers.XmlHelper.Parse(result);
    }
}
