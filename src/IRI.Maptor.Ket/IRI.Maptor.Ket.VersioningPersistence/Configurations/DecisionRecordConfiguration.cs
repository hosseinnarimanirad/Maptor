using IRI.Maptor.Sta.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRI.Maptor.Ket.VersioningPersistence.Configurations;

public class DecisionRecordConfiguration : IEntityTypeConfiguration<DecisionRecord>
{
    private readonly string _schema;

    public DecisionRecordConfiguration(string schema) => _schema = schema;

    public void Configure(EntityTypeBuilder<DecisionRecord> entity)
    {
        entity.ToTable("DecisionRecord", _schema);

        entity.HasKey(e => e.Id);

        entity.Property(e => e.ActorDisplayName).HasMaxLength(200);
        entity.Property(e => e.Reason).HasMaxLength(1000);

        entity.HasOne(e => e.Competition)
            .WithMany()
            .HasForeignKey(e => e.CompetitionId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Proposal)
            .WithMany()
            .HasForeignKey(e => e.ProposalId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CommitBatch)
            .WithMany()
            .HasForeignKey(e => e.CommitBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(e => e.CompetitionId);
        entity.HasIndex(e => e.ActorUserId);
    }
}
