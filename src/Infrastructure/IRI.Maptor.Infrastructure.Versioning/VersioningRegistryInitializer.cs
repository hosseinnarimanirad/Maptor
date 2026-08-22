using IRI.Maptor.Core.Versioning;
using Microsoft.EntityFrameworkCore;

namespace IRI.Maptor.Infrastructure.Versioning;

/// <summary>Seed row for a layer that participates in versioning (disabled until enabled explicitly).</summary>
public record VersionedLayerSeed(Guid LayerKey, string EntityName, string DisplayName);

/// <summary>
/// Call once at API startup: ensures seed rows exist and stamps every registered layer's
/// current schema signature from the EF model — drift is detected automatically, never
/// declared by hand.
/// </summary>
public static class VersioningRegistryInitializer
{
    /// <returns>Entity names whose schema signature changed (log these — pending proposals there may be affected).</returns>
    public static async Task<IReadOnlyList<string>> InitializeAsync(
        DbContext context,
        IEnumerable<VersionedLayerSeed>? seeds = null,
        CancellationToken cancellationToken = default)
    {
        var layerSet = context.Set<VersionedLayer>();

        if (seeds is not null)
        {
            foreach (var seed in seeds)
            {
                if (!await layerSet.AnyAsync(l => l.EntityName == seed.EntityName, cancellationToken))
                {
                    layerSet.Add(new VersionedLayer
                    {
                        LayerKey = seed.LayerKey,
                        EntityName = seed.EntityName,
                        DisplayName = seed.DisplayName,
                        IsVersioningEnabled = false,
                    });
                }
            }

            // Save before the signature pass: the query below reads the database, and
            // unsaved Added entities would silently skip their first-run stamping.
            await context.SaveChangesAsync(cancellationToken);
        }

        var entityTypesByName = context.Model.GetEntityTypes()
            .GroupBy(t => t.ClrType.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var changedLayers = new List<string>();

        foreach (var layer in await layerSet.ToListAsync(cancellationToken))
        {
            // Unknown entity name: leave the stored signature untouched — a missing
            // entity is itself drift and must stay visible, not be overwritten.
            if (!entityTypesByName.TryGetValue(layer.EntityName, out var entityType))
                continue;

            var signature = EfSchemaSignatureCalculator.Calculate(entityType);

            if (!string.Equals(layer.SchemaSignature, signature, StringComparison.Ordinal))
            {
                layer.SchemaSignature = signature;
                layer.SchemaSignatureUpdatedAt = DateTime.UtcNow;
                changedLayers.Add(layer.EntityName);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return changedLayers;
    }
}
