using IRI.Maptor.Sta.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRI.Maptor.Ket.VersioningPersistence.Configurations;

public class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    private readonly string _schema;

    public CompetitionConfiguration(string schema) => _schema = schema;

    public void Configure(EntityTypeBuilder<Competition> entity)
    {
        entity.ToTable("Competition", _schema);

        entity.HasKey(e => e.Id);

        entity.Property(e => e.RowVersion).IsRowVersion();

        entity.HasOne(e => e.Layer)
            .WithMany()
            .HasForeignKey(e => e.VersionedLayerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Circular with Proposal.CompetitionId — both sides Restrict, winner set only
        // after the proposal row exists.
        entity.HasOne(e => e.Winner)
            .WithMany()
            .HasForeignKey(e => e.WinnerProposalId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Predecessor)
            .WithMany()
            .HasForeignKey(e => e.PredecessorCompetitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // At most one Open and one Resolved competition per feature — the queued-
        // competition rule enforced by the database itself. Filters depend on
        // CompetitionState numbering (Open = 0, Resolved = 1).
        // The name must be passed INSIDE HasIndex: EF keys indexes by property set, so a
        // second HasIndex on the same columns would silently replace the first.
        entity.HasIndex(e => new { e.VersionedLayerId, e.TargetFeatureId }, "UX_Competition_OpenPerTarget")
            .IsUnique()
            .HasFilter("[State] = 0 AND [TargetFeatureId] IS NOT NULL");

        entity.HasIndex(e => new { e.VersionedLayerId, e.TargetFeatureId }, "UX_Competition_ResolvedPerTarget")
            .IsUnique()
            .HasFilter("[State] = 1 AND [TargetFeatureId] IS NOT NULL");

        entity.HasIndex(e => e.State);
    }
}
