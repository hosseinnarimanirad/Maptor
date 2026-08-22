using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IRI.Maptor.Core.Common.Helpers;

public static class XsdHelper
{

    static Dictionary<string, string> clrToXsdTypeMap = new Dictionary<string, string> {
        { "string", "xsd:string" },
        { "double", "xsd:double" },
        { "long", "xsd:long" },
        { "sbyte", "xsd:byte" },
        { "boolean", "xsd:boolean" },
        { "datetime", "xsd:dateTime" },
        { "sqlgeometry", "gml:GeometryPropertyType" },
    };
    //<?xml version = "1.0" encoding="UTF-8"?>
    //<xsd:schema xmlns:SqlWs="http://hosseinnarimanirad.ir" 
    //            xmlns:gml="http://www.opengis.net/gml" 
    //            xmlns:xsd="http://www.w3.org/2001/XMLSchema" 
    //            elementFormDefault="qualified" 
    //            targetNamespace="http://hosseinnarimanirad.ir">
    //<xsd:import namespace="http://www.opengis.net/gml" 
    //            schemaLocation="http://localhost:8888/geoserver/schemas/gml/3.1.1/base/gml.xsd"/>


    public static string GetWfsDescribeFeatureXsd<T>(T classValue, XNamespace theNamespace, string gmlSchemaLocation, XNamespace targetNamespace, string name, string _type)
    {
        var properties = (typeof(T)).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        XDocument doc = new XDocument();

        //XNamespace xsdNamespace = "http://www.w3.org/2001/XMLSchema";
        XNamespace xsdNamespace = System.Xml.Schema.XmlSchema.InstanceNamespace;
        XNamespace gmlNamespace = XNamespace.Get("http://www.opengis.net/gml");


        var root = new XElement(XName.Get("schema", xsdNamespace.ToString()),
                    new XAttribute(XNamespace.Xmlns + "gml", gmlNamespace),
                    new XAttribute(XNamespace.Xmlns + "xsd", xsdNamespace),
                    new XAttribute("elementFormDefault", "qualified"),
                    new XAttribute("targetNamespace", targetNamespace.ToString()),
                    new XElement(XName.Get("import", xsdNamespace.ToString()),
                        new XAttribute("namespace", gmlNamespace.ToString()),
                        new XAttribute("schemaLocation", gmlSchemaLocation.ToString())));
         
        List<XElement> elements = new List<XElement>();

        foreach (var property in properties)
        {
            elements.Add(new XElement(GetXsdXName("element"),
                                                    new XAttribute("maxOccurs", 1),
                                                    new XAttribute("minOccurs", 0),
                                                    new XAttribute("name", property.Name),
                                                    new XAttribute("nillable", "true"),
                                                    new XAttribute("type", clrToXsdTypeMap[property.PropertyType.Name.ToLower()])));
        }
         

        XElement content = new XElement(GetXsdXName("complexType"),
                                new XAttribute("name", name + "Type"),       //e.g. GIS.StructureViewType
                                new XElement(GetXsdXName("complexContent"),
                                    new XElement(GetXsdXName("extension"),
                                        new XAttribute("base", "gml:AbstractFeatureType"),
                                        new XElement(GetXsdXName("sequence"), elements))));

        root.Add(content);

        root.Add(new XElement(GetXsdXName("element"),
                    new XAttribute("name", name),
                    new XAttribute("substitutionGroup", "gml:_Feature"),
                    new XAttribute("type", _type)));

        doc.Add(root);

        return doc.ToString();
    }

    private static XName GetXsdXName(string name)
    {
        return XName.Get(name, System.Xml.Schema.XmlSchema.InstanceNamespace);
    }

}
