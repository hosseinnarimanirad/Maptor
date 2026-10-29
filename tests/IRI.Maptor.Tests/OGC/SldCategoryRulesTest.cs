using System.Linq;
using System.Globalization;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Ogc;
using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Core.Spatial.Primitives;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Tests.OGC;

// 2026-10-22: categorized symbology — one rule per value of a field, the template's style in each value's colour.
public class SldCategoryRulesTest
{
    private static Feature<Point> F(string field, object? value) => new(null!, new Dictionary<string, object> { [field] = value! });

    private static string? Css(IEnumerable<CssParameter> parameters, string name) => parameters.FirstOrDefault(p => p.Name == name)?.Value;

    [Fact]
    public void OneRulePerValue_PaletteColours_AndAnOtherValuesRule()
    {
        var rules = SldCategoryRules.Create(
            new[] { new SldCategory { Value = "PE" }, new SldCategory { Value = "Steel", Title = "فولاد" } },
            new SldCategoryOptions { FieldName = "Material", Geometry = SldGeometryKind.Line });

        Assert.Equal(3, rules.Count);

        var pe = rules[0];
        var predicate = Assert.IsType<OgcPropertyIsEqualTo>(pe.Filter.Predicate);
        Assert.Equal("Material", predicate.GetPropertyName());
        Assert.Equal("PE", predicate.GetLiteral());
        Assert.Equal("PE", pe.Title);
        Assert.Equal("فولاد", rules[1].Title);

        Assert.Equal(SldCategoryRules.Palette[0], Css(((LineSymbolizer)pe.Symbolizers[0]).Stroke.CssParameters, "stroke"));
        Assert.Equal(SldCategoryRules.Palette[1], Css(((LineSymbolizer)rules[1].Symbolizers[0]).Stroke.CssParameters, "stroke"));

        Assert.NotNull(rules[2].ElseFilter);
        Assert.Null(rules[2].Filter);
    }

    [Fact]
    public void GivenColours_RgbaBecomesHexAndOpacity_AndPaletteFillsTheRest()
    {
        var rules = SldCategoryRules.Create(
            new[] { new SldCategory { Value = "a", Color = "rgba(79,6,250,0.5)" }, new SldCategory { Value = "b", Color = "#0f0" }, new SldCategory { Value = "c" } },
            new SldCategoryOptions { FieldName = "f", Geometry = SldGeometryKind.Polygon, IncludeOtherValues = false });

        var a = (PolygonSymbolizer)rules[0].Symbolizers[0];
        Assert.Equal("#4f06fa", Css(a.Fill.CssParameters, "fill"));
        Assert.Equal("0.5", Css(a.Fill.CssParameters, "fill-opacity"));

        Assert.Equal("#00ff00", Css(((PolygonSymbolizer)rules[1].Symbolizers[0]).Fill.CssParameters, "fill"));

        // the first value without a colour takes the first palette colour
        Assert.Equal(SldCategoryRules.Palette[0], Css(((PolygonSymbolizer)rules[2].Symbolizers[0]).Fill.CssParameters, "fill"));
    }

    [Fact]
    public void TheTemplatesStyleIsKept_OnlyColoursChange()
    {
        var template = new LineSymbolizer
        {
            Stroke = new Stroke
            {
                CssParameters =
                {
                    new CssParameter { Name = "stroke", Value = "#000000" },
                    new CssParameter { Name = "stroke-width", Value = "3.5" },
                    new CssParameter { Name = "stroke-dasharray", Value = "6 3" },
                },
            },
        };

        var rule = SldCategoryRules.Create(new[] { new SldCategory { Value = 1 } }, new SldCategoryOptions { FieldName = "n", Geometry = SldGeometryKind.Line, Template = template })[0];
        var stroke = ((LineSymbolizer)rule.Symbolizers[0]).Stroke;

        Assert.Equal("3.5", Css(stroke.CssParameters, "stroke-width"));
        Assert.Equal("6 3", Css(stroke.CssParameters, "stroke-dasharray"));
        Assert.Equal(SldCategoryRules.Palette[0], Css(stroke.CssParameters, "stroke"));

        // the template itself is not touched
        Assert.Equal("#000000", Css(template.Stroke.CssParameters, "stroke"));
    }

    [Fact]
    public void PointRules_KeepTheMarkAndSize()
    {
        var template = new PointSymbolizer { Graphic = new Graphic { Size = 14, Marks = { new Mark { WellKnownName = "square" } } } };

        var symbolizer = (PointSymbolizer)SldCategoryRules.Create(new[] { new SldCategory { Value = "x" } },
            new SldCategoryOptions { FieldName = "f", Geometry = SldGeometryKind.Point, Template = template })[0].Symbolizers[0];

        Assert.Equal(14, symbolizer.Graphic.Size);
        Assert.Equal("square", symbolizer.Graphic.Marks[0].WellKnownName);
        Assert.Equal(SldCategoryRules.Palette[0], Css(symbolizer.Graphic.Marks[0].Fill.CssParameters, "fill"));
    }

    [Fact]
    public void Palette_GoesLighterThenDarkerAfterTwelve()
    {
        var colours = Enumerable.Range(0, 36).Select(SldCategoryRules.GetPaletteColor).ToList();

        Assert.Equal(36, colours.Distinct().Count());
        Assert.Equal(SldCategoryRules.Palette[0], colours[0]);
    }

    [Fact]
    public void GeneratedRules_SerializeToAValidSld_AndDrawEachFeatureOnce()
    {
        var rules = SldCategoryRules.Create(
            new[] { new SldCategory { Value = "PE" }, new SldCategory { Value = 4 }, new SldCategory { Value = 0.75 } },
            new SldCategoryOptions { FieldName = "v", Geometry = SldGeometryKind.Polygon });

        var sld = new StyledLayerDescriptor();
        var layer = new NamedLayer { Name = "pipes" };
        var style = new UserStyle { Name = "categorized" };
        var featureTypeStyle = new FeatureTypeStyle();
        featureTypeStyle.Rules.AddRange(rules);
        style.FeatureTypeStyles.Add(featureTypeStyle);
        layer.UserStyles.Add(style);
        sld.NamedLayers.Add(layer);

        var xml = SldHelper.Serialize(sld);
        Assert.True(SldHelper.TryParse(xml, out var parsed, out var error), error);

        var symbolizers = parsed!.NamedLayers[0].UserStyles[0].FeatureTypeStyles[0].Parse();

        // a decimal value matches under a Persian UI too (the comparison is invariant)
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fa-IR");

            foreach (var value in new object?[] { "PE", 4, 0.75, "other", null })
                Assert.Equal(1, symbolizers.Count(s => s.IsFilterPassed(F("v", value))));

            Assert.True(symbolizers[2].IsFilterPassed(F("v", 0.75)));
            Assert.True(symbolizers[3].IsFilterPassed(F("v", "other")));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}
