using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Extensions;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// What the drawings list's Open reads: Maptor's saved drawings with their style, the drawings list's own "Export as
/// GeoJSON", and any other GeoJSON (a FeatureCollection with properties before the geometry, a feature without
/// properties, a bare geometry). Synthetic coordinates in Tehran.
/// </summary>
[Collection(WpfCollection.Name)]
public class DrawingItemFileReaderTest
{
    private const string Line3 = "{\"type\":\"LineString\",\"coordinates\":[[51.30,35.70],[51.31,35.71],[51.32,35.70]]}";

    private static string Collection(params string[] features)
        => "{\"type\":\"FeatureCollection\",\"features\":[" + string.Join(",", features) + "]}";

    private static string Feature(string geometry, string properties = "{\"name\":\"Sample\",\"vertices\":3}")
        => "{\"type\":\"Feature\",\"properties\":" + properties + ",\"geometry\":" + geometry + "}";

    [Fact]
    public void AFeatureCollection_WithPropertiesBeforeTheGeometry_GivesItsDrawing() => WpfTestHost.Run(() =>
    {
        var entries = DrawingItemFileReader.Read("sample", Collection(Feature(Line3)));

        var entry = Assert.Single(entries);

        Assert.Equal("sample", entry.Name);
        Assert.Null(entry.Styled);
        Assert.Equal(GeometryType.LineString, entry.Geometry!.Type);
        Assert.Equal(3, entry.Geometry.Points.Count);
        Assert.Equal(SridHelper.WebMercator, entry.Geometry.Srid);

        // longitude first: 51.30 E is about 5.71 million metres east in Web Mercator
        Assert.InRange(entry.Geometry.Points[0].X, 5_710_000, 5_712_000);
    });

    [Fact]
    public void AFeatureCollectionOfSeveral_GivesNumberedDrawings() => WpfTestHost.Run(() =>
    {
        var entries = DrawingItemFileReader.Read("roads", Collection(Feature(Line3), Feature(Line3)));

        Assert.Equal(new[] { "roads (1)", "roads (2)" }, entries.Select(e => e.Name));
    });

    [Fact]
    public void AFeatureWithoutProperties_AndABareGeometry_AreRead() => WpfTestHost.Run(() =>
    {
        var withoutProperties = "{\"type\":\"Feature\",\"geometry\":" + Line3 + "}";

        Assert.Equal(3, Assert.Single(DrawingItemFileReader.Read("a", withoutProperties)).Geometry!.Points.Count);

        Assert.Equal(3, Assert.Single(DrawingItemFileReader.Read("b", Line3)).Geometry!.Points.Count);
    });

    [Fact]
    public void ABigPolygon_KeepsItsVertices() => WpfTestHost.Run(() =>
    {
        // 5,000 distinct positions, closed by repeating the first, as GeoJSON writes a ring
        var ring = new StringBuilder();

        for (int i = 0; i <= 5_000; i++)
        {
            var a = 2 * System.Math.PI * (i % 5_000) / 5_000;

            ring.Append(i == 0 ? "" : ",").Append(string.Format(CultureInfo.InvariantCulture, "[{0:0.0000000},{1:0.0000000}]",
                51.42 + 0.015 * System.Math.Cos(a), 35.76 + 0.012 * System.Math.Sin(a)));
        }

        var polygon = "{\"type\":\"Polygon\",\"coordinates\":[[" + ring + "]]}";

        var entry = Assert.Single(DrawingItemFileReader.Read("polygon", Collection(Feature(polygon))));

        Assert.Equal(GeometryType.Polygon, entry.Geometry!.Type);

        // the closing position is not a vertex of its own (rings are never closed inside Maptor)
        Assert.Equal(5_000, entry.Geometry.Geometries[0].Points.Count);
    });

    [Fact]
    public void ADrawingSavedByMaptor_IsReadWithItsStyle() => WpfTestHost.Run(() =>
    {
        var geometry = Geometry<Point>.Create(new List<Point> { new(5_711_000, 4_260_000), new(5_712_000, 4_261_000) }, GeometryType.LineString, SridHelper.WebMercator);

        var drawing = DrawingItemLayer.Create("saved", new Feature<Point>(geometry, "saved"),
            new[] { new SimpleSymbolizer(VisualParameters.GetDefaultForDrawingItems()) })!;

        // what Save writes to an .mtd file
        var saved = drawing.Serialize();

        var entry = Assert.Single(DrawingItemFileReader.Read("saved", saved));

        Assert.NotNull(entry.Styled);
        Assert.Null(entry.Geometry);
        Assert.Equal(2, entry.Styled!.Geometry.Points.Count);
    });

    [Fact]
    public void ADrawingExportedAsGeoJsonFromTheDrawingsList_IsReadBack() => WpfTestHost.Run(() =>
    {
        var geometry = Geometry<Point>.Create(new List<Point> { new(5_711_000, 4_260_000), new(5_712_000, 4_261_000) }, GeometryType.LineString, SridHelper.WebMercator);

        // what "Export as GeoJSON" of a drawing writes (LegendCommand.CreateExportDrawingItemLayerAsGeoJson)
        var feature = GeoJsonFeature.Create(geometry.Project(SrsBases.GeodeticWgs84).AsGeoJson());

        var exported = JsonHelper.Serialize(new GeoJsonFeatureSet { Features = new List<GeoJsonFeature> { feature }, TotalFeatures = 1 });

        var entry = Assert.Single(DrawingItemFileReader.Read("exported", exported));

        Assert.Equal(2, entry.Geometry!.Points.Count);
        Assert.InRange(entry.Geometry.Points[0].X, 5_710_999, 5_711_001);
        Assert.InRange(entry.Geometry.Points[0].Y, 4_259_999, 4_260_001);
    });

    [Fact]
    public void WhatIsNotGeoJson_GivesNothing() => WpfTestHost.Run(() =>
    {
        Assert.Empty(DrawingItemFileReader.Read("x", "{\"name\":\"no type\"}"));
        Assert.Empty(DrawingItemFileReader.Read("x", "[1,2,3]"));
        Assert.Empty(DrawingItemFileReader.Read("x", Collection()));
    });

    [Fact]
    public void TheOldReader_NoLongerThrows_OnAFeatureCollection() => WpfTestHost.Run(() =>
    {
        Assert.Null(DrawingItemLayer.Deserialize("x", Collection(Feature(Line3))));
        Assert.Equal(string.Empty, new GeoJsonFeature().RetrieveSldAttribute());
    });
}
