using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace IRI.Maptor.Sta.Common.Helpers;

public static class XmlHelper
{
    static readonly XmlWriterSettings _indentedSettings;

    static XmlHelper()
    {
        _indentedSettings = new XmlWriterSettings()
        {
            Indent = true,
            IndentChars = "  ", // 2 spaces for indentation
            Encoding = Encoding.UTF8,
            CloseOutput = true,
            NewLineOnAttributes = true, 
        };
    }

    public static void Serialize<T>(string path, T value, bool isIndented = false)
    {
        Serialize(path, value, isIndented ? _indentedSettings : null);
    }

    public static void Serialize<T>(string path, T value, XmlWriterSettings? settings)
    {
        XmlWriter? writer = null;

        try
        {
            //// Configure XML writer settings for indentation
            //var settings = new XmlWriterSettings
            //{
            //    Indent = true,
            //    IndentChars = "  ", // 2 spaces for indentation
            //    Encoding = Encoding.UTF8,
            //    CloseOutput = true
            //};

            // Create the writer with proper settings
            writer = XmlWriter.Create(path, settings);

            // Create the serializer
            var serializer = new XmlSerializer(typeof(T));

            //// Add XML namespace declarations if needed (for SLD files)
            //var namespaces = new XmlSerializerNamespaces();
            //namespaces.Add("", "http://www.opengis.net/sld");
            //namespaces.Add("ogc", "http://www.opengis.net/ogc");
            //namespaces.Add("xlink", "http://www.w3.org/1999/xlink");
            //namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");

            // Serialize with namespaces
            serializer.Serialize(writer, value/*, namespaces*/);
        }
        catch (Exception ex)
        {
            // Wrap in a more descriptive exception
            throw new InvalidOperationException($"Failed to serialize XML to {path}", ex);
        }
        finally
        {
            writer?.Dispose(); // Proper cleanup using Dispose instead of Close
        }

        //System.Xml.XmlTextWriter writer = null;

        //try
        //{
        //    writer = new System.Xml.XmlTextWriter(path, Encoding.UTF8);

        //    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

        //    serializer.Serialize(writer, value);
        //}
        //catch (Exception ex)
        //{
        //    throw ex;
        //}
        //finally
        //{
        //    writer.Close();
        //}
    }

    public static string Parse<T>(T value)
    {
        //System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

        //System.IO.StringWriter writer = new System.IO.StringWriter();

        //serializer.Serialize(writer, value);

        //return writer.ToString();


        //System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

        //System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings();
        //settings.Encoding = Encoding.UTF8;
        //settings.Indent = true;
        //settings.OmitXmlDeclaration = false;

        //using (StringWriter textWriter = new StringWriter())
        //{
        //    using (System.Xml.XmlWriter xmlWriter = System.Xml.XmlWriter.Create(textWriter, settings))
        //    {
        //        serializer.Serialize(xmlWriter, value);
        //    }

        //    return textWriter.ToString();
        //}


        //System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

        //using (var stringWriter = new StringWriter())
        //{
        //    System.Xml.XmlWriterSettings setting = new System.Xml.XmlWriterSettings();
        //    setting.Indent = true;
        //    setting.Encoding = Encoding.UTF8;

        //    using (var xw = System.Xml.XmlTextWriter.Create(stringWriter, setting))
        //    {
        //        serializer.Serialize(xw, value);
        //        xw.Flush();

        //    }
        //    return stringWriter.ToString();
        //}

        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

        var memoryStream = new MemoryStream();
        var streamWriter = new StreamWriter(memoryStream, System.Text.Encoding.UTF8);

        serializer.Serialize(streamWriter, value);

        byte[] utf8EncodedXml = memoryStream.ToArray();
        return Encoding.UTF8.GetString(utf8EncodedXml);

        //System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
        //System.IO.Stream stream = new System.IO.MemoryStream();
        //System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(stream, Encoding.UTF8);
        //serializer.Serialize(writer, value);
        //writer.Flush();

        //return writer.ToString();
    }

    public static T DeserializeFromFile<T>(string path)
    {
        XmlTextReader? reader = null;

        try
        {
            reader = new XmlTextReader(path);

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            T result = (T)serializer.Deserialize(reader);

            return result;
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            reader.Close();
        }
    }

    public static T DeserializeFromXmlString<T>(string xmlString)
    {
        if (string.IsNullOrEmpty(xmlString))
            throw new ArgumentException("XML string cannot be null or empty", nameof(xmlString));

        var serializer = new XmlSerializer(typeof(T));

        using (var reader = new StringReader(xmlString))
        {
            try
            {
                return (T)serializer.Deserialize(reader);
            }
            catch (InvalidOperationException ex)
            {
                // Wrap the exception with more context
                throw new InvalidOperationException($"Failed to deserialize XML into type {typeof(T).Name}", ex);
            }
        }
    }

}
