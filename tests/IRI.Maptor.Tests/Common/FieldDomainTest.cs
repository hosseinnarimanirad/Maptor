using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Attributes;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf.Models.LayerSettings;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Tests.Common;

// 2026-10-22: a field's allowed values travel as Field.Domain (value + source code), declared by an application's
// FieldDomainAttribute (Gas: coded-value domains) or by [Field(AllowedValues = ...)] (Barg's plain lists).
// Field.AllowedValues is no longer sent.
public class FieldDomainTest
{
    private sealed class MaterialDomainAttribute : FieldDomainAttribute
    {
        public override FieldDomain? GetDomain() => new()
        {
            Name = "CI_Material",
            Values = [new() { Value = "PE", Code = 1 }, new() { Value = "Steel", Code = 2 }, new() { Value = "Copper", Code = 5 }],
        };
    }

    private sealed class MissingDomainAttribute : FieldDomainAttribute
    {
        public override FieldDomain? GetDomain() => null;
    }

    private class Pipe
    {
        [Field(Alias = "Material")]
        [MaterialDomain]
        public string? Material { get; set; }

        [Field(Alias = "Circuits", AllowedValues = [(short)1, (short)2, (short)4])]
        public short Circuits { get; set; }

        [Field(Alias = "Status", AllowedValues = ["Active", "Retired"])]
        [MissingDomain]
        public string? Status { get; set; }

        public string? Note { get; set; }
    }

    private static Dictionary<string, Field> PipeFields() => Field.GetFields<Pipe>().ToDictionary(f => f.Name);

    [Fact]
    public void GetFields_TakesTheApplicationsDomain_InOrder_WithCodes()
    {
        var domain = PipeFields()["Material"].Domain!;

        Assert.Equal("CI_Material", domain.Name);
        Assert.Equal(new object?[] { "PE", "Steel", "Copper" }, domain.GetStoredValues());
        Assert.Equal(new int?[] { 1, 2, 5 }, domain.Values.Select(v => v.Code));
    }

    [Fact]
    public void GetFields_TurnsAllowedValuesIntoAPlainDomain()
    {
        var fields = PipeFields();

        Assert.Null(fields["Circuits"].Domain!.Name);
        Assert.Equal(new object?[] { (short)1, (short)2, (short)4 }, fields["Circuits"].Domain!.GetStoredValues());
        Assert.All(fields["Circuits"].Domain!.Values, v => Assert.Null(v.Code));

        // an application attribute that finds nothing falls back to the declared list
        Assert.Equal(new object?[] { "Active", "Retired" }, fields["Status"].Domain!.GetStoredValues());

        Assert.Null(fields["Note"].Domain);
    }

    [Fact]
    public void Contains_ComparesNumbersByValue_TextOrdinally_AndNullIsNotAMember()
    {
        var circuits = PipeFields()["Circuits"].Domain!;

        Assert.True(circuits.Contains((short)2));
        Assert.True(circuits.Contains(2));          // an int after JSON
        Assert.True(circuits.Contains(2L));
        Assert.True(circuits.Contains(2.0));
        Assert.False(circuits.Contains(3));
        Assert.False(circuits.Contains("2"));        // text is not the number
        Assert.False(circuits.Contains(null));

        var material = PipeFields()["Material"].Domain!;

        Assert.True(material.Contains("Steel"));
        Assert.False(material.Contains("steel"));
        Assert.False(material.Contains("Steel "));
    }

    [Fact]
    public void Json_RoundTripsValuesAsClrPrimitives_AndDropsAllowedValues()
    {
        var fields = Field.GetFields<Pipe>();

        var json = JsonSerializer.Serialize(fields, JsonHelper.DefaultOptions);

        Assert.DoesNotContain("allowedValues", json);

        var read = JsonSerializer.Deserialize<List<Field>>(json, JsonHelper.DefaultOptions)!.ToDictionary(f => f.Name);

        Assert.Equal(new object?[] { "PE", "Steel", "Copper" }, read["Material"].Domain!.GetStoredValues());
        Assert.Equal(new int?[] { 1, 2, 5 }, read["Material"].Domain!.Values.Select(v => v.Code));

        // numbers come back as int, like attribute values do, and still match
        Assert.Equal(new object?[] { 1, 2, 4 }, read["Circuits"].Domain!.GetStoredValues());
        Assert.True(read["Circuits"].Domain!.Contains((short)4));

        Assert.Null(read["Note"].Domain);
    }

    [Fact]
    public void Json_AnOldPayloadWithOnlyAllowedValuesHasNoDomain()
    {
        // servers and clients are deployed together; an old payload is read without failing
        var field = JsonSerializer.Deserialize<Field>("{\"name\":\"a\",\"allowedValues\":[\"x\",\"y\"]}", JsonHelper.DefaultOptions)!;

        Assert.Null(field.Domain);
    }

    private static Feature<Point> F(object? material) =>
        new(null!, new Dictionary<string, object> { ["Material"] = material! });

    [Fact]
    public void FieldStatistics_CountsValuesOutsideTheDomain()
    {
        var field = PipeFields()["Material"];
        var features = new List<Feature<Point>> { F("PE"), F("PE"), F("Steel"), F("Plastic"), F("Plastic"), F("Plastic"), F(null), F("") };

        var statistics = FieldStatistics.Calculate(features, field);

        Assert.Equal(FieldStatisticsKind.Text, statistics.Kind);
        Assert.Same(field.Domain, statistics.Domain);
        Assert.Equal(3, statistics.OutOfDomainCount);           // "Plastic" × 3; empty values are not out of the domain
        Assert.Equal(2, statistics.UsedDomainValueCount);       // PE, Steel (Copper unused)
        Assert.True(statistics.TopValues.Single(v => Equals(v.Value, "Plastic")).IsOutOfDomain);
        Assert.False(statistics.TopValues.Single(v => Equals(v.Value, "PE")).IsOutOfDomain);
        Assert.False(statistics.TopValues.Single(v => v.Value is null).IsOutOfDomain);
    }

    [Fact]
    public void FieldStatistics_AFreeFieldHasNoDomainNumbers()
    {
        var statistics = FieldStatistics.Calculate(new List<Feature<Point>> { new(null!, new Dictionary<string, object> { ["Note"] = "x" }) }, PipeFields()["Note"]);

        Assert.Null(statistics.Domain);
        Assert.Equal(0, statistics.OutOfDomainCount);
        Assert.All(statistics.TopValues, v => Assert.False(v.IsOutOfDomain));
    }

    [Fact]
    public void GroupStatistics_FlagsGroupsOutsideTheDomain()
    {
        var features = new List<Feature<Point>> { F("PE"), F("Plastic"), F("Plastic"), F(null) };

        var statistics = GroupStatistics.Calculate(features, PipeFields()["Material"], GeometryMeasureKind.None, null, null);

        Assert.Equal(2, statistics.OutOfDomainCount);
        Assert.True(statistics.Rows.Single(r => Equals(r.Key, "Plastic")).IsOutOfDomain);
        Assert.False(statistics.Rows.Single(r => Equals(r.Key, "PE")).IsOutOfDomain);
        Assert.False(statistics.Rows.Single(r => r.Key is null).IsOutOfDomain);
    }
}
