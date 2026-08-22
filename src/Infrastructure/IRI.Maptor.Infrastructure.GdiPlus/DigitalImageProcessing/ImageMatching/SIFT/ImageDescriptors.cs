// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.Common.DataStructures;
using System.Xml.Serialization;

namespace IRI.Maptor.Infrastructure.GdiPlus.DigitalImageProcessing.ImageMatching;

[Serializable()]
public class ImageDescriptors 
{
    [XmlArray]
    public List<Descriptor> items;

    public ImageDescriptors()
    {
        this.items = new List<Descriptor>();
    }

    public ImageDescriptors(List<Descriptor> descriptors)
    {
        this.items = descriptors;
    }

    public int Count
    {
        get { return this.items.Count; }
    }

    public Descriptor this[int index]
    {
        get { return this.items[index];}
    }

    public void Serialize(string path)
    {
        System.Xml.Serialization.XmlSerializer serializer = new XmlSerializer(typeof(ImageDescriptors));

        System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(path, System.Text.Encoding.UTF8);

        serializer.Serialize(writer, this);

        writer.Close();
    }

    public static ImageDescriptors Deserialize(string path)
    {

        System.Xml.Serialization.XmlSerializer serializer = new XmlSerializer(typeof(ImageDescriptors));

        System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(path);

        ImageDescriptors result = (ImageDescriptors)serializer.Deserialize(reader);

        reader.Close();

        return result;

    }

}
