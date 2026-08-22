using IRI.Maptor.Core.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRI.Maptor.Infrastructure.Versioning.Configurations;

public class OverlapSuggestionConfiguration : IEntityTypeConfiguration<OverlapSuggestion>
{
    private readonly string _schema;

    public OverlapSuggestionConfiguration(string schema) => _schema = schema;

    public void Configure(EntityTypeBuilder<OverlapSuggestion> entity)
    {
        entity.ToTable("OverlapSuggestion", _schema);

        entity.HasKey(e => e.Id);

        entity.Property(e => e.DismissedByDisplayName).HasMaxLength(200);

        entity.HasOne(e => e.Proposal)
            .WithMany()
            .HasForeignKey(e => e.ProposalId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OtherProposal)
            .WithMany()
            .HasForeignKey(e => e.OtherProposalId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(e => e.ProposalId);
    }
}
