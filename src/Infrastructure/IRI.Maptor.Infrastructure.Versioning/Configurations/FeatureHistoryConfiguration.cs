using IRI.Maptor.Core.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRI.Maptor.Infrastructure.Versioning.Configurations;

public class FeatureHistoryConfiguration : IEntityTypeConfiguration<FeatureHistory>
{
    private readonly string _schema;

    public FeatureHistoryConfiguration(string schema) => _schema = schema;

    public void Configure(EntityTypeBuilder<FeatureHistory> entity)
    {
        entity.ToTable("FeatureHistory", _schema);

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Geometry).HasColumnType("geography");
        entity.Property(e => e.ReplacedRowVersion).HasMaxLength(8);

        entity.HasOne(e => e.Layer)
            .WithMany()
            .HasForeignKey(e => e.VersionedLayerId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CommitBatch)
            .WithMany()
            .HasForeignKey(e => e.CommitBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.WinningProposal)
            .WithMany()
            .HasForeignKey(e => e.WinningProposalId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(e => new { e.VersionedLayerId, e.FeatureId, e.SupersededAt })
            .IsDescending(false, false, true);
    }
}
