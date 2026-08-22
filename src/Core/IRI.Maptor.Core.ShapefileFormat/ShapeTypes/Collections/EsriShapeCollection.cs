using System;
using System.Linq;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.ShapefileFormat.ShapeTypes.Abstractions;

namespace IRI.Maptor.Core.ShapefileFormat.EsriType;

public class EsriShapeCollection<T> : List<T>, IEsriShapeCollection where T : EsriShapeBase
{
    private MainFileHeader mainHeader;

    public EsriShapeCollection()
    {
       
    }
    
    public EsriShapeCollection(MainFileHeader header)
    {
        this.mainHeader = header;
    }

    public EsriShapeCollection(MainFileHeader header, List<T> values)
    {
        this.AddRange(values);

        this.mainHeader = header;
    }

    public EsriShapeCollection(IEnumerable<T> values)
    {
        if (values is null)
            throw new ArgumentNullException(nameof(values));
        
        if (typeof(EsriPointMCollection).IsAssignableFrom(typeof(T)))
        {
            throw new ArgumentException("EsriPointMCollection types are not supported in this collection.", nameof(T));
        }

        var valuesList = values.ToList();
        if (valuesList.Count == 0)
            throw new ArgumentException("Collection must contain at least one shape.", nameof(values));

        if (valuesList.Select(i => i.EsriType).Distinct().Count() > 1)
        {
            throw new ArgumentException("All shapes in the collection must have the same EsriType.", nameof(values));
        }

        this.AddRange(valuesList);

        var minimumBoundingBox = BoundingBox.GetMergedBoundingBox(valuesList.Select(i => i.MinimumBoundingBox));
       
        //The content length for a record is the length of the record contents section measured in
        //16-bit words. Each record, therefore, contributes (4 + content length) 16-bit words
        //toward the total length of the file, as stored at Byte 24 in the file header.
        var length = valuesList.Sum(i => i.ContentLength + 4);

        this.mainHeader = new MainFileHeader(length, valuesList.First().EsriType, minimumBoundingBox);
    }
      
    #region IShapeCollection Members

    public new EsriShapeBase this[int index]
    {
        get
        {
            return (EsriShapeBase)base[index];
        }
        set
        {
            base[index] = (T)value;
        }
    }

    public MainFileHeader MainHeader
    {
        get { return this.mainHeader; }
    }

    public string AsKml()
    {
        IRI.Maptor.Core.Ogc.Kml.Primitives.KmlType result = new IRI.Maptor.Core.Ogc.Kml.Primitives.KmlType();

        IRI.Maptor.Core.Ogc.Kml.Primitives.DocumentType document = new IRI.Maptor.Core.Ogc.Kml.Primitives.DocumentType();

        var placemarks = ((List<T>)this).Select(i => i.AsPlacemark());

        foreach (var placemark in placemarks.OfType<IRI.Maptor.Core.Ogc.Kml.Primitives.AbstractFeatureType>())
        {
            document.AbstractFeatureGroup.Add(placemark);
        }

        result.KmlObjectExtensionGroup.Add(document);

        return XmlHelper.Parse(result);
    }

    #endregion

    #region IEnumerable<IShape> Members

    public new IEnumerator<EsriShapeBase> GetEnumerator()
    {
        for (int i = 0; i < this.Count; i++)
        {
            yield return base[i];
        }
    }

    #endregion



}
