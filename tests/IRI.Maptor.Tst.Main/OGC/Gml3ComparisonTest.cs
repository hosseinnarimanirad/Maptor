//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Xml.Linq;
//using Microsoft.SqlServer.Types;
//using IRI.Maptor.Extensions;
//using IRI.Maptor.Sta.Ogc.Extensions;
//using IRI.Maptor.Sta.Spatial.Primitives;
//using IRI.Maptor.Sta.Common.Primitives;
//using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
//using Xunit;

//namespace IRI.Maptor.Tst.Main.OGC;

//public class Gml3ComparisonTest
//{
//    public static IEnumerable<object[]> Gml3TestData =>
//    [
//        // 2D Geometries
//        [ "POINT (1 2)", @"<Point xmlns=""http://www.opengis.net/gml""><pos>1 2</pos></Point>" ],
//        [ "POINT (-10.5 20.75)", @"<Point xmlns=""http://www.opengis.net/gml""><pos>-10.5 20.75</pos></Point>" ],
//        [ "MULTIPOINT ((0 0), (0 3), (3 3), (3 0))", @"<MultiPoint xmlns=""http://www.opengis.net/gml""><pointMembers><Point><pos>0 0</pos></Point><Point><pos>0 3</pos></Point><Point><pos>3 3</pos></Point><Point><pos>3 0</pos></Point></pointMembers></MultiPoint>" ],
//        [ "LINESTRING (1 1, 2 0, 2 4, 3 3)" , @"<LineString xmlns=""http://www.opengis.net/gml""><posList>1 1 2 0 2 4 3 3</posList></LineString>" ],
//        [ "LINESTRING (0 0, 10 10)", @"<LineString xmlns=""http://www.opengis.net/gml""><posList>0 0 10 10</posList></LineString>"],
//        [ "MULTILINESTRING ((1 1, 3 5), (-5 3, -8 -2))", @"<MultiCurve xmlns=""http://www.opengis.net/gml""><curveMembers><LineString><posList>1 1 3 5</posList></LineString><LineString><posList>-5 3 -8 -2</posList></LineString></curveMembers></MultiCurve>" ],
//        [ "POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0))", @"<Polygon xmlns=""http://www.opengis.net/gml""><exterior><LinearRing><posList>0 0 30 0 30 30 0 30 0 0</posList></LinearRing></exterior></Polygon>" ],
//        [ "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (5 5, 5 15, 15 15, 15 5, 5 5))", @"<Polygon xmlns=""http://www.opengis.net/gml""><exterior><LinearRing><posList>0 0 10 0 10 10 0 10 0 0</posList></LinearRing></exterior><interior><LinearRing><posList>5 5 5 15 15 15 15 5 5 5</posList></LinearRing></interior></Polygon>" ],
//        [ "MULTIPOLYGON (((0 0, 3 0, 3 3, 0 3, 0 0)), ((9 9, 10 9, 9 10, 9 9)))", @"<MultiSurface xmlns=""http://www.opengis.net/gml""><surfaceMembers><Polygon><exterior><LinearRing><posList>0 0 3 0 3 3 0 3 0 0</posList></LinearRing></exterior></Polygon><Polygon><exterior><LinearRing><posList>9 9 10 9 9 10 9 9</posList></LinearRing></exterior></Polygon></surfaceMembers></MultiSurface>" ],

//    ];

//    [Theory]
//    [MemberData(nameof(Gml3TestData))]
//    public void CompareGml3_GeometryVsSqlGeometry(string wktString, string expectedGmlString)
//    {
//        // Arrange
//        const int srid = 4326;

//        // Parse WKT to Geometry<Point>
//        var geometry = SqlServerWktReader.Parse(wktString, srid);

//        // Parse WKT to SqlGeometry
//        var sqlGeometry = SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(wktString));
//        sqlGeometry.STSrid = srid;

//        // Act - Get GML3 from both
//        var geometryGml3 = geometry.AsGml3(includeSrid: false);
//        var sqlGeometryGml3 = sqlGeometry.AsGml3(writeSrid: false);

//        // Normalize both GML strings for comparison
//        var normalizedGeometryGml = NormalizeGml(geometryGml3);
//        var normalizedSqlGml = NormalizeGml(sqlGeometryGml3);

//        // Assert - Compare normalized GML strings
//        Assert.Equal(normalizedSqlGml, normalizedGeometryGml);
//    }

//    /// <summary>
//    /// Normalizes GML XML strings for comparison by:
//    /// - Parsing and reformatting XML
//    /// - Removing namespace prefixes differences
//    /// - Normalizing whitespace
//    /// </summary>
//    private static string NormalizeGml(string gmlString)
//    {
//        if (string.IsNullOrWhiteSpace(gmlString))
//            return string.Empty;

//        try
//        {
//            var doc = XDocument.Parse(gmlString);

//            //// Remove all namespace prefixes and use default namespace
//            //foreach (var element in doc.Descendants())
//            //{
//            //    // Remove namespace prefixes from element names
//            //    if (element.Name.Namespace != XNamespace.None)
//            //    {
//            //        element.Name = XNamespace.None + element.Name.LocalName;
//            //    }

//            //    // Remove namespace prefixes from attributes
//            //    var attributesToRemove = element.Attributes()
//            //        .Where(a => a.IsNamespaceDeclaration || a.Name.Namespace != XNamespace.None)
//            //        .ToList();

//            //    foreach (var attr in attributesToRemove)
//            //    {
//            //        if (!attr.IsNamespaceDeclaration)
//            //        {
//            //            // Keep the attribute but remove namespace prefix
//            //            var newName = XNamespace.None + attr.Name.LocalName;
//            //            element.SetAttributeValue(newName, attr.Value);
//            //        }
//            //        attr.Remove();
//            //    }
//            //}

//            // Normalize the XML (removes extra whitespace, standardizes formatting)
//            return doc.ToString(SaveOptions.DisableFormatting);
//        }
//        catch
//        {
//            // If parsing fails, return original string
//            return gmlString;
//        }
//    }
//}

