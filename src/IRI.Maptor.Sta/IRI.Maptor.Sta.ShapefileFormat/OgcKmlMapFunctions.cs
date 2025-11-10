using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IRI.Maptor.Ket.KmlFormat;
using IRI.Maptor.Ket.KmlFormat.Primitives;

namespace IRI.Maptor.Sta.ShapefileFormat;

static class OgcKmlMapFunctions
{
    internal static string AsKml(PlacemarkType placemark)
    {
        return AsKml(new AbstractFeatureType[] { placemark });
    }

    internal static string AsKml(AbstractFeatureType[] abstractFeatureType)
    {
        IRI.Maptor.Ket.KmlFormat.Primitives.KmlType result = new KmlType();

        IRI.Maptor.Ket.KmlFormat.Primitives.DocumentType document = new DocumentType();

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
