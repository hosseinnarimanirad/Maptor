using IRI.Maptor.Core.Versioning;
using Microsoft.EntityFrameworkCore;

namespace IRI.Maptor.Infrastructure.Versioning;

/// <summary>
/// The direct-sync write gate: layers with versioning enabled must reject the ordinary
/// sync path so every live change provably went through review. Consulted on every sync,
/// so the enabled set is cached per process.
/// </summary>
public static class VersionedLayerGate
{
    private static readonly object _lock = new();
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(60);

    private static HashSet<string>? _enabledEntityNames;
    private static DateTime _loadedAtUtc;

    public static async Task<bool> IsVersioningEnabledAsync(DbContext context, string entityName, CancellationToken cancellationToken = default)
    {
        var cache = _enabledEntityNames;

        if (cache is null || DateTime.UtcNow - _loadedAtUtc > _cacheDuration)
        {
            try
            {
                var enabled = await context.Set<VersionedLayer>()
                    .Where(l => l.IsVersioningEnabled)
                    .Select(l => l.EntityName)
                    .ToListAsync(cancellationToken);

                cache = new HashSet<string>(enabled, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                // Pre-migration database (registry table absent) or transient failure:
                // fail open so non-versioned deployments keep working — a real
                // connectivity problem will surface on the write itself anyway.
                cache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            lock (_lock)
            {
                _enabledEntityNames = cache;
                _loadedAtUtc = DateTime.UtcNow;
            }
        }

        return cache.Contains(entityName);
    }

    /// <summary>Call after any registry-admin change so the gate reflects it immediately.</summary>
    public static void InvalidateCache()
    {
        lock (_lock)
        {
            _enabledEntityNames = null;
        }
    }
}
