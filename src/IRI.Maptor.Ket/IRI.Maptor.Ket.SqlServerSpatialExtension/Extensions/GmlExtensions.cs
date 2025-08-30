using Microsoft.SqlServer.Types;
using System.Data.SqlTypes;
using System.Xml;
using System.Xml.Linq;

namespace IRI.Maptor.Extensions;

public static class GmlExtensions
{ 
    public static string AsGml3(this SqlGeometry geometry, bool writeSrid = false)
    {
        var gml = geometry?.AsGml();

        if (gml == null || gml.IsNull)
        {
            return string.Empty;
        }

        var doc = XDocument.Parse(geometry.AsGml().Value);

        doc.Root.SetAttributeValue(XNamespace.Xmlns + "gml", "http://www.opengis.net/gml");
        doc.Root.SetAttributeValue("xmlns", null);

        if (writeSrid)
        {
            doc.Root.SetAttributeValue("srsName", $"http://www.opengis.net/gml/srs/epsg.xml#{geometry.STSrid.Value}");
        }

        //doc.Declaration = null;

        return doc.ToString(SaveOptions.OmitDuplicateNamespaces);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gmlString">should not contain srsName attribute</param>
    /// <param name="srid"></param>
    /// <returns></returns>
    public static SqlGeometry ParseGML3(string gmlString, int srid)
    {
        var reader = XmlReader.Create(new StringReader(gmlString));

        SqlXml sqlXml = new SqlXml(reader);

        return SqlGeometry.GeomFromGml(sqlXml, srid);
    }
}
