using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IRI.Maptor.Core.Ogc.Kml;
using IRI.Maptor.Core.Ogc.Kml.Primitives;

namespace IRI.Maptor.Core.ShapefileFormat;

static class OgcKmlMapFunctions
{
    internal static string AsKml(PlacemarkType placemark)
    {
        return AsKml(new AbstractFeatureType[] { placemark });
    }

    internal static string AsKml(AbstractFeatureType[] abstractFeatureType)
    {
        IRI.Maptor.Core.Ogc.Kml.Primitives.KmlType result = new KmlType();

        IRI.Maptor.Core.Ogc.Kml.Primitives.DocumentType document = new DocumentType();

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

        return IRI.Maptor.Core.Common.Helpers.XmlHelper.Parse(result);
    }
}
