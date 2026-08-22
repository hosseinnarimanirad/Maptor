using IRI.Maptor.Core.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRI.Maptor.Infrastructure.Versioning.Configurations;

public class ProposalConfiguration : IEntityTypeConfiguration<Proposal>
{
    private readonly string _schema;

    public ProposalConfiguration(string schema) => _schema = schema;

    public void Configure(EntityTypeBuilder<Proposal> entity)
    {
        entity.ToTable("Proposal", _schema);

        entity.HasKey(e => e.Id);

        entity.Property(e => e.EditorDisplayName).HasMaxLength(200);
        entity.Property(e => e.SchemaSignatureAtSubmit).HasMaxLength(100);
        entity.Property(e => e.BaseRowVersion).HasMaxLength(8);
        // geography, matching the live SHAPE columns: byte-fidelity on commit and
        // direct STIntersects against live data in the overlap scan (D45).
        entity.Property(e => e.ProposedGeometry).HasColumnType("geography");
        entity.Property(e => e.RowVersion).IsRowVersion();

        entity.HasOne(e => e.Session)
            .WithMany(s => s.Proposals)
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Layer)
            .WithMany()
            .HasForeignKey(e => e.VersionedLayerId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Competition)
            .WithMany(c => c.Proposals)
            .HasForeignKey(e => e.CompetitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-supersede backstop: one active proposal per editor + target. The filter
        // depends on ProposalState numbering (active states are <= 2).
        entity.HasIndex(e => new { e.VersionedLayerId, e.TargetFeatureId, e.EditorUserId })
            .IsUnique()
            .HasFilter("[State] <= 2 AND [TargetFeatureId] IS NOT NULL")
            .HasDatabaseName("UX_Proposal_ActivePerEditorTarget");

        entity.HasIndex(e => new { e.VersionedLayerId, e.TargetFeatureId })
            .HasFilter("[State] <= 2")
            .HasDatabaseName("IX_Proposal_ActiveByTarget");

        entity.HasIndex(e => e.CompetitionId);
        entity.HasIndex(e => e.SessionId);

        // The spatial index on ProposedGeometry cannot be declared via EF — add
        // CREATE SPATIAL INDEX ... (geography grid, no bounding box) to the migration by hand.
    }
}
