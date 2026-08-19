using IRI.Maptor.Sta.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRI.Maptor.Ket.VersioningPersistence.Configurations;

public class VersionNotificationConfiguration : IEntityTypeConfiguration<VersionNotification>
{
    private readonly string _schema;

    public VersionNotificationConfiguration(string schema) => _schema = schema;

    public void Configure(EntityTypeBuilder<VersionNotification> entity)
    {
        entity.ToTable("VersionNotification", _schema);

        entity.HasKey(e => e.Id);

        entity.HasIndex(e => new { e.RecipientUserId, e.ReadAt });
    }
}
