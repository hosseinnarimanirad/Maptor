using IRI.Maptor.Infrastructure.Versioning.Configurations;
using Microsoft.EntityFrameworkCore;

namespace IRI.Maptor.Infrastructure.Versioning;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Registers all spatial-versioning entities under the given schema. The consuming
    /// context must also use the Maptor geometry provider plugin (UseMaptorGeometry).
    /// </summary>
    public static ModelBuilder AddMaptorVersioning(this ModelBuilder modelBuilder, string schema = VersioningDb.DefaultSchema)
    {
        modelBuilder.ApplyConfiguration(new VersionedLayerConfiguration(schema));
        modelBuilder.ApplyConfiguration(new VersionSessionConfiguration(schema));
        modelBuilder.ApplyConfiguration(new ProposalConfiguration(schema));
        modelBuilder.ApplyConfiguration(new CompetitionConfiguration(schema));
        modelBuilder.ApplyConfiguration(new DecisionRecordConfiguration(schema));
        modelBuilder.ApplyConfiguration(new CommitBatchConfiguration(schema));
        modelBuilder.ApplyConfiguration(new FeatureHistoryConfiguration(schema));
        modelBuilder.ApplyConfiguration(new VersionNotificationConfiguration(schema));
        modelBuilder.ApplyConfiguration(new OverlapSuggestionConfiguration(schema));

        return modelBuilder;
    }
}
