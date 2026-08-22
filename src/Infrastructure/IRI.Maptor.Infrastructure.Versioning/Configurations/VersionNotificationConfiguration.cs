using IRI.Maptor.Core.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRI.Maptor.Infrastructure.Versioning.Configurations;

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
