using IRI.Maptor.Sta.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRI.Maptor.Ket.VersioningPersistence.Configurations;

public class VersionedLayerConfiguration : IEntityTypeConfiguration<VersionedLayer>
{
    private readonly string _schema;

    public VersionedLayerConfiguration(string schema) => _schema = schema;

    public void Configure(EntityTypeBuilder<VersionedLayer> entity)
    {
        entity.ToTable("VersionedLayer", _schema);

        entity.HasKey(e => e.Id);

        entity.Property(e => e.EntityName).HasMaxLength(200);
        entity.Property(e => e.DisplayName).HasMaxLength(200);
        entity.Property(e => e.SchemaSignature).HasMaxLength(100);

        entity.HasIndex(e => e.LayerKey).IsUnique();
        entity.HasIndex(e => e.EntityName).IsUnique();
    }
}
