using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;

using IRI.Maptor.Core.Common.Attributes;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Importing;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Infrastructure.EfCore.Extensions;
using IRI.Maptor.Infrastructure.EfCore.Importing;
using IRI.Maptor.Infrastructure.EfCore.SqlServer.Extensions;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Tests.Querying;

/// <summary>
/// The import model an app exports (<see cref="ImportModelBuilder"/>) from a synthetic model shaped like Gas's and
/// Saba's layers: names and types from EF, required from nullable reference types, roles, defaults, both kinds of
/// domain, and a JSON round trip that the converter can use. No database is opened.
/// </summary>
public class ImportModelBuilderTest
{
    /// <summary>Like Gas's [CodedDomain]: values with their data-model codes.</summary>
    private sealed class MaterialDomainAttribute : FieldDomainAttribute
    {
        public override FieldDomain? GetDomain() => new()
        {
            Name = "CI_Material",
            Values = { new FieldDomainValue { Value = "نا مشخص", Code = 0 }, new FieldDomainValue { Value = "فولادی", Code = 1 } },
        };
    }

    [Entity(Schema = "equipment", TableName = "Riser", Alias = "علمک")]
    public class Riser
    {
        public int Id { get; set; }

        public Geometry<Point> SHAPE { get; set; } = null!;

        [Field(Alias = "OBJECTID", CanRead = false)]
        public int ObjectId { get; set; }

        [Field(Alias = "کد علمک", Length = 100)]
        public string Riser_Code { get; set; } = string.Empty;

        [Field(Alias = "شناسه دارایی", Length = 50)]
        public string? Asset_ID { get; set; }

        [Field(Alias = "جنس علمک", Length = 100)]
        [MaterialDomain]
        public string CI_Material { get; set; } = "نا مشخص";

        // Saba's kind of domain: a plain list
        [Field(Alias = "نوع", Length = 10, AllowedValues = new object[] { "دو راهه", "سه راهه" })]
        public string? Kind { get; set; }

        public double? Length { get; set; }

        public string? CreatedByFullName { get; set; }

        public int? CreatedById { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string? LastUpdatedByFullName { get; set; }

        public int? LastUpdatedById { get; set; }

        public DateTime? LastUpdatedAt { get; set; }

        public byte[] RowVersion { get; set; } = [];
    }

    private sealed class RiserContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlServer("Server=none;Database=none", x => x.UseMaptor());

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
            => configurationBuilder.ConfigureMaptorGeometry("GEOGRAPHY");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var riser = modelBuilder.Entity<Riser>();
            riser.ToTable("Riser", "equipment");
            riser.Property(e => e.ObjectId).HasColumnName("OBJECTID");
            riser.Property(e => e.Riser_Code).HasMaxLength(100);
            riser.Property(e => e.Asset_ID).HasMaxLength(50);
            riser.Property(e => e.CI_Material).HasMaxLength(100);
            riser.Property(e => e.Kind).HasMaxLength(10);
            riser.Property(e => e.RowVersion).IsRowVersion();
        }
    }

    private static ImportModelDto Build()
    {
        using var context = new RiserContext();

        return ImportModelBuilder.Build(context, "Gas", "NIGC.DataModel 1.10",
            new[] { new ImportModelLayerSource { LayerId = 1256, Title = "علمک", EntityType = typeof(Riser), DataModelName = "layer_1256", Geometry = "Point" } },
            new DateTime(2026, 10, 23));
    }

    [Fact]
    public void Layer_TableColumnsAndRoles()
    {
        var model = Build();

        Assert.Equal("Gas", model.Application);
        Assert.Equal("SqlServer", model.DatabaseProvider);

        var layer = Assert.Single(model.Layers);
        Assert.Equal(("equipment", "Riser", "علمک", "layer_1256", "Point"), (layer.Schema, layer.Table, layer.Alias, layer.DataModelName, layer.Geometry));

        ImportModelColumnDto Column(string name) => layer.Columns.Single(c => c.Column == name);

        Assert.Equal(ImportColumnRole.Key, Column("Id").Role);
        Assert.Equal(ImportColumnRole.Shape, Column("SHAPE").Role);
        Assert.Equal(ImportColumnRole.Concurrency, Column("RowVersion").Role);
        Assert.Equal(ImportColumnRole.CreatedAt, Column("CreatedAt").Role);
        Assert.Equal(ImportColumnRole.CreatedBy, Column("CreatedByFullName").Role);
        Assert.Equal(ImportColumnRole.UpdatedById, Column("LastUpdatedById").Role);

        // a renamed column keeps its property; OBJECTID is data (the provider has it)
        Assert.Equal(("ObjectId", ImportColumnRole.Data, true), (Column("OBJECTID").Property, Column("OBJECTID").Role, Column("OBJECTID").IsRequired));

        // required from nullable reference types; lengths and types from EF
        Assert.Equal((true, 100, FieldDataType.Text, "nvarchar(100)"), (Column("Riser_Code").IsRequired, Column("Riser_Code").Length, Column("Riser_Code").DataType, Column("Riser_Code").StoreType));
        Assert.False(Column("Asset_ID").IsRequired);
        Assert.Equal(FieldDataType.Double, Column("Length").DataType);
        Assert.Equal("کد علمک", Column("Riser_Code").Alias);

        // a real default only (string.Empty is none)
        Assert.Equal("نا مشخص", Column("CI_Material").DefaultValue);
        Assert.Null(Column("Riser_Code").DefaultValue);
    }

    [Fact]
    public void Domains_NamedWithCodes_AndPlainLists()
    {
        var model = Build();
        var layer = model.Layers[0];

        Assert.Equal("CI_Material", layer.Columns.Single(c => c.Column == "CI_Material").Domain);
        Assert.Equal("Riser.Kind", layer.Columns.Single(c => c.Column == "Kind").Domain);

        var material = model.Domains.Single(d => d.Name == "CI_Material");
        Assert.Equal(new int?[] { 0, 1 }, material.Values.Select(v => v.Code));

        var kind = model.Domains.Single(d => d.Name == "Riser.Kind");
        Assert.All(kind.Values, v => Assert.Null(v.Code));
    }

    [Fact]
    public void File_RoundTrip_FeedsTheConverter()
    {
        var json = Build().ToJson();

        Assert.Contains("\"role\": \"Concurrency\"", json);
        Assert.Contains("نا مشخص", json);                       // Persian kept readable

        var model = ImportModelDto.FromJson(json);
        var layer = model.Layers[0];

        Assert.Equal(GeometryCategory.Point, layer.GetGeometryCategory());

        var fields = layer.GetImportFields(model);
        Assert.DoesNotContain(fields, f => f.Name is "Id" or "SHAPE" or "RowVersion" or "CreatedAt");

        var material = fields.Single(f => f.Name == "CI_Material");
        var converter = new FeatureImportConverter(new[] { new ImportFieldMapping(material, "src", material.Domain) });

        Assert.Equal("فولادی", converter.Convert(new Dictionary<string, object?> { ["src"] = 1 }, null).Values["CI_Material"]);
        Assert.Equal("نا مشخص", converter.Convert(new Dictionary<string, object?> { ["src"] = null }, null).Values["CI_Material"]);

        Assert.Throws<FormatException>(() => ImportModelDto.FromJson(json.Replace("\"formatVersion\": 1", "\"formatVersion\": 99")));
    }
}
