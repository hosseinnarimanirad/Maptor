using IRI.Maptor.Core.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRI.Maptor.Infrastructure.Versioning.Configurations;

public class VersionSessionConfiguration : IEntityTypeConfiguration<VersionSession>
{
    private readonly string _schema;

    public VersionSessionConfiguration(string schema) => _schema = schema;

    public void Configure(EntityTypeBuilder<VersionSession> entity)
    {
        entity.ToTable("VersionSession", _schema);

        entity.HasKey(e => e.Id);

        entity.Property(e => e.EditorDisplayName).HasMaxLength(200);
        entity.Property(e => e.Title).HasMaxLength(200);
        entity.Property(e => e.Comment).HasMaxLength(1000);
        entity.Property(e => e.RowVersion).IsRowVersion();

        entity.HasIndex(e => new { e.EditorUserId, e.State });
    }
}
