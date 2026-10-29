using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Attributes;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Dtos;
using IRI.Maptor.Presentation.Wpf.Helpers;

namespace IRI.Maptor.Tests.Common;

// 2026-10-22: Field carries a logical FieldDataType so clients stop parsing TypeFullName (which some sources fill
// with SQL type names). Old servers do not send it; newer ones may add members. Both must keep working.
public class FieldDataTypeTest
{
    private enum Status { Active, Retired }

    private class Owner { }

    private class Pipe
    {
        public int Id { get; set; }

        public short? Code { get; set; }

        public long Counter { get; set; }

        public double? Diameter { get; set; }

        public decimal Cost { get; set; }

        public bool IsActive { get; set; }

        [Field(Alias = "Material", AllowedValues = ["PE", "Steel"])]
        public string? Material { get; set; }

        public DateTime? InstalledAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public Guid ObjectId { get; set; }

        public byte[]? RowVersion { get; set; }

        public Status State { get; set; }

        public Microsoft.SqlServer.Types.SqlGeometry SHAPE { get; set; } = null!;

        public Owner? CreatedBy { get; set; }
    }

    [Theory]
    [InlineData(typeof(int), FieldDataType.Integer)]
    [InlineData(typeof(short?), FieldDataType.Integer)]
    [InlineData(typeof(byte), FieldDataType.Integer)]
    [InlineData(typeof(long), FieldDataType.Long)]
    [InlineData(typeof(uint), FieldDataType.Long)]
    [InlineData(typeof(float), FieldDataType.Double)]
    [InlineData(typeof(double?), FieldDataType.Double)]
    [InlineData(typeof(decimal), FieldDataType.Decimal)]
    [InlineData(typeof(bool?), FieldDataType.Boolean)]
    [InlineData(typeof(string), FieldDataType.Text)]
    [InlineData(typeof(char), FieldDataType.Text)]
    [InlineData(typeof(Status), FieldDataType.Text)]
    [InlineData(typeof(DateTime), FieldDataType.DateTime)]
    [InlineData(typeof(DateTimeOffset?), FieldDataType.DateTime)]
    [InlineData(typeof(DateOnly), FieldDataType.DateTime)]
    [InlineData(typeof(Guid), FieldDataType.Guid)]
    [InlineData(typeof(byte[]), FieldDataType.Binary)]
    [InlineData(typeof(Microsoft.SqlServer.Types.SqlGeography), FieldDataType.Geometry)]
    [InlineData(typeof(object), FieldDataType.Unknown)]
    [InlineData(typeof(Owner), FieldDataType.Unknown)]
    [InlineData(null, FieldDataType.Unknown)]
    public void FromClrType_MapsToTheLogicalType(Type? type, FieldDataType expected)
    {
        Assert.Equal(expected, FieldDataTypes.FromClrType(type));
    }

    [Fact]
    public void GetFields_FillsDataTypeFromThePropertyTypes()
    {
        var fields = Field.GetFields<Pipe>().ToDictionary(f => f.Name);

        Assert.Equal(FieldDataType.Integer, fields["Id"].DataType);
        Assert.Equal(FieldDataType.Integer, fields["Code"].DataType);
        Assert.Equal(FieldDataType.Long, fields["Counter"].DataType);
        Assert.Equal(FieldDataType.Double, fields["Diameter"].DataType);
        Assert.Equal(FieldDataType.Decimal, fields["Cost"].DataType);
        Assert.Equal(FieldDataType.Boolean, fields["IsActive"].DataType);
        Assert.Equal(FieldDataType.Text, fields["Material"].DataType);
        Assert.Equal(FieldDataType.DateTime, fields["InstalledAt"].DataType);
        Assert.Equal(FieldDataType.DateTime, fields["UpdatedAt"].DataType);
        Assert.Equal(FieldDataType.Guid, fields["ObjectId"].DataType);
        Assert.Equal(FieldDataType.Binary, fields["RowVersion"].DataType);
        Assert.Equal(FieldDataType.Text, fields["State"].DataType);
        Assert.Equal(FieldDataType.Geometry, fields["SHAPE"].DataType);
        Assert.Equal(FieldDataType.Unknown, fields["CreatedBy"].DataType);

        // the native name is kept as it was
        Assert.Equal(typeof(double?).FullName, fields["Diameter"].TypeFullName);
    }

    [Fact]
    public void FromDictionary_TypesFromValues_AndAnEmptyValueSaysNothing()
    {
        var fields = Field.FromDictionary(new Dictionary<string, object> { ["a"] = 2.5, ["b"] = "x", ["c"] = null! }).ToDictionary(f => f.Name);

        Assert.Equal(FieldDataType.Double, fields["a"].DataType);
        Assert.Equal(FieldDataType.Text, fields["b"].DataType);
        Assert.Equal(FieldDataType.Unknown, fields["c"].DataType);
        Assert.Equal("object", fields["c"].TypeFullName);
    }

    [Fact]
    public void ResolveDataType_PrefersTheSentType_ThenTheTypeName()
    {
        // sent: wins even over a misleading name
        Assert.Equal(FieldDataType.Double, new Field { Name = "x", TypeFullName = "nvarchar", DataType = FieldDataType.Double }.ResolveDataType());

        // an older server: only the .NET name (assembly-qualified generic names included)
        Assert.Equal(FieldDataType.Double, new Field { Name = "x", TypeFullName = typeof(double?).FullName! }.ResolveDataType());
        Assert.Equal(FieldDataType.Geometry, new Field { Name = "x", TypeFullName = "NetTopologySuite.Geometries.Geometry, NetTopologySuite, Version=2.0.0.0" }.ResolveDataType());

        // names that are not .NET types (SQL type names, "object") stay unknown
        Assert.Equal(FieldDataType.Unknown, new Field { Name = "x", TypeFullName = "nvarchar" }.ResolveDataType());
        Assert.Equal(FieldDataType.Unknown, new Field { Name = "x", TypeFullName = "object" }.ResolveDataType());
        Assert.Equal(FieldDataType.Unknown, new Field { Name = "x", TypeFullName = null! }.ResolveDataType());
    }

    [Fact]
    public void Json_WritesTheName_AndRoundTrips()
    {
        var field = new Field { Name = "Diameter", TypeFullName = typeof(double?).FullName!, DataType = FieldDataType.Double };

        var json = JsonSerializer.Serialize(field, JsonHelper.DefaultOptions);

        Assert.Contains("\"dataType\":\"Double\"", json);
        Assert.Equal(FieldDataType.Double, JsonSerializer.Deserialize<Field>(json, JsonHelper.DefaultOptions)!.DataType);
    }

    [Theory]
    [InlineData("{\"name\":\"a\",\"typeFullName\":\"System.Double\"}", FieldDataType.Unknown)]          // an older server
    [InlineData("{\"name\":\"a\",\"dataType\":\"Date\"}", FieldDataType.Unknown)]                       // a member added later
    [InlineData("{\"name\":\"a\",\"dataType\":\"double\"}", FieldDataType.Double)]                      // case
    [InlineData("{\"name\":\"a\",\"dataType\":5}", FieldDataType.Double)]                               // a number
    [InlineData("{\"name\":\"a\",\"dataType\":99}", FieldDataType.Unknown)]                             // an unknown number
    [InlineData("{\"name\":\"a\",\"dataType\":null}", FieldDataType.Unknown)]
    public void Json_NeverFailsOnWhatItDoesNotKnow(string json, FieldDataType expected)
    {
        // the client's own options register JsonStringEnumConverter, which would throw on "Date"
        Assert.Equal(expected, JsonSerializer.Deserialize<Field>(json, JsonHelper.DefaultOptions)!.DataType);
        Assert.Equal(expected, JsonSerializer.Deserialize<Field>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!.DataType);
    }

    [Fact]
    public void Json_AnUnknownTypeDoesNotBreakTheFeatureSet()
    {
        var json = "{\"features\":[],\"srid\":3857,\"count\":0,\"fields\":[{\"name\":\"a\",\"dataType\":\"Date\"},{\"name\":\"b\",\"dataType\":\"Text\"}]}";

        var dto = JsonSerializer.Deserialize<FeatureSetDto>(json, JsonHelper.DefaultOptions)!;

        Assert.Equal(new[] { FieldDataType.Unknown, FieldDataType.Text }, dto.Fields!.Select(f => f.DataType));
    }

    [Fact]
    public void AttributeTableRule_IsUnchangedForOldPayloads_AndUsesTheSentType()
    {
        // as before: geometry and types the client cannot name are hidden; "object" (a C# keyword, not a .NET
        // type name, so Type.GetType gives null) was hidden too and still is
        Assert.False(FeatureTableHelper.IsDisplayableField(new Field { Name = "SHAPE", TypeFullName = "NetTopologySuite.Geometries.Geometry, NetTopologySuite" }));
        Assert.False(FeatureTableHelper.IsDisplayableField(new Field { Name = "CreatedBy", TypeFullName = "Contoso.Gas.Core.Entities.User" }));
        Assert.False(FeatureTableHelper.IsDisplayableField(new Field { Name = "u", TypeFullName = "object" }));
        Assert.True(FeatureTableHelper.IsDisplayableField(new Field { Name = "u", TypeFullName = "System.Object" }));
        Assert.True(FeatureTableHelper.IsDisplayableField(new Field { Name = "d", TypeFullName = typeof(double).FullName! }));
        Assert.False(FeatureTableHelper.IsDisplayableField(new Field { Name = "RowVersion", TypeFullName = typeof(byte[]).FullName! }));
        Assert.False(FeatureTableHelper.IsDisplayableField(new Field { Name = "d", TypeFullName = typeof(double).FullName!, CanRead = false }));

        // new: a sent type is enough, whatever the name
        Assert.False(FeatureTableHelper.IsDisplayableField(new Field { Name = "SHAPE", TypeFullName = "x", DataType = FieldDataType.Geometry }));
        Assert.True(FeatureTableHelper.IsDisplayableField(new Field { Name = "State", TypeFullName = "Server.Only.StatusEnum", DataType = FieldDataType.Text }));
    }
}
