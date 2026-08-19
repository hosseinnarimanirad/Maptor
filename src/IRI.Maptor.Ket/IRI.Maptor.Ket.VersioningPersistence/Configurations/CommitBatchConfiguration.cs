using IRI.Maptor.Sta.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRI.Maptor.Ket.VersioningPersistence.Configurations;

public class CommitBatchConfiguration : IEntityTypeConfiguration<CommitBatch>
{
    private readonly string _schema;

    public CommitBatchConfiguration(string schema) => _schema = schema;

    public void Configure(EntityTypeBuilder<CommitBatch> entity)
    {
        entity.ToTable("CommitBatch", _schema);

        entity.HasKey(e => e.Id);

        entity.Property(e => e.ApproverDisplayName).HasMaxLength(200);
    }
}
