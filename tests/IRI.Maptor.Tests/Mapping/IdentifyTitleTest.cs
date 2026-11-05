using System.Collections.Generic;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Wpf.Helpers;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The label of a feature row in the identify results: layer label column → display field →
/// Feature.Label → first text field, empty when none has a value.
/// </summary>
public class IdentifyTitleTest
{
    private static readonly List<Field> Fields =
    [
        new Field { Name = "code", DataType = FieldDataType.Text },
        new Field { Name = "name", DataType = FieldDataType.Text, IsDisplayField = true },
        new Field { Name = "voltage", DataType = FieldDataType.Integer },
    ];

    private static Feature<Point> CreateFeature(Dictionary<string, object> attributes)
    {
        var geometry = Geometry<Point>.Create(10, 20, SridHelper.WebMercator);

        return new Feature<Point>(geometry, attributes) { Id = 7 };
    }

    [Fact]
    public void LabelColumn_WinsOverDisplayField()
    {
        var feature = CreateFeature(new() { ["code"] = "C-1", ["name"] = "North", ["voltage"] = 230 });

        Assert.Equal("230", IdentifyAttributeHelper.ResolveTitle(feature, Fields, labelColumn: "voltage"));
    }

    [Fact]
    public void LabelColumn_MatchesCaseInsensitively()
    {
        var feature = CreateFeature(new() { ["code"] = "C-1", ["name"] = "North" });

        Assert.Equal("C-1", IdentifyAttributeHelper.ResolveTitle(feature, Fields, labelColumn: "CODE"));
    }

    [Fact]
    public void DisplayField_UsedWithoutLabelColumn()
    {
        var feature = CreateFeature(new() { ["code"] = "C-1", ["name"] = "North" });

        Assert.Equal("North", IdentifyAttributeHelper.ResolveTitle(feature, Fields));
    }

    [Fact]
    public void BlankLabelColumnValue_FallsToDisplayField()
    {
        var feature = CreateFeature(new() { ["code"] = "  ", ["name"] = "North" });

        Assert.Equal("North", IdentifyAttributeHelper.ResolveTitle(feature, Fields, labelColumn: "code"));
    }

    [Fact]
    public void NoDisplayValue_FallsToFirstTextField()
    {
        var feature = CreateFeature(new() { ["code"] = "C-1", ["name"] = "" });

        Assert.Equal("C-1", IdentifyAttributeHelper.ResolveTitle(feature, Fields));
    }

    [Fact]
    public void NothingToShow_IsEmpty()
    {
        var feature = CreateFeature(new() { ["voltage"] = 230 });

        Assert.Equal(string.Empty, IdentifyAttributeHelper.ResolveTitle(feature, Fields));
    }
}
