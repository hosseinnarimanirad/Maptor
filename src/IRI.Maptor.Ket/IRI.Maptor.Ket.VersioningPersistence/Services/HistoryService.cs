using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Exceptions;
using IRI.Maptor.Sta.Versioning;
using Microsoft.EntityFrameworkCore;

namespace IRI.Maptor.Ket.VersioningPersistence.Services;

/// <summary>
/// The feature timeline (doc 03 §5.4): current live state + copy-on-write hops walking
/// backward, each with the provenance of the change that replaced it. As-of lookups are
/// derivable from this data client-side.
/// </summary>
public static class HistoryService
{
    public static async Task<FeatureTimelineDto> GetFeatureTimelineAsync(
        DbContext context, Guid layerKey, long featureId, CancellationToken cancellationToken = default)
    {
        var layer = await context.Set<VersionedLayer>().AsNoTracking()
                .SingleOrDefaultAsync(l => l.LayerKey == layerKey, cancellationToken)
            ?? throw new VersioningException("UnknownVersionedLayer", $"no versioned layer is registered with key {layerKey}.");

        var entityType = LiveFeatureReader.FindEntityType(context, layer.EntityName);

        var live = entityType is null
            ? null
            : await LiveFeatureReader.GetSnapshotAsync(context, entityType, featureId, cancellationToken);

        var rows = await context.Set<FeatureHistory>().AsNoTracking()
            .Where(h => h.VersionedLayerId == layer.Id && h.FeatureId == featureId)
            .OrderByDescending(h => h.SupersededAt)
            .Select(h => new
            {
                h.Id,
                h.SupersededAt,
                h.Geometry,
                h.AttributesJson,
                h.CommitBatchId,
                Approver = h.CommitBatch!.ApproverDisplayName,
                h.WinningProposalId,
                Editor = h.WinningProposal!.EditorDisplayName,
                h.WinningProposal.ChangeType,
                SessionTitle = h.WinningProposal.Session!.Title,
            })
            .ToListAsync(cancellationToken);

        var timeline = new FeatureTimelineDto
        {
            LayerKey = layer.LayerKey,
            EntityName = layer.EntityName,
            FeatureId = featureId,
            Live = live,
        };

        foreach (var row in rows)
        {
            timeline.Entries.Add(new FeatureTimelineEntryDto
            {
                HistoryId = row.Id,
                SupersededAt = row.SupersededAt,
                GeometryWkb = row.Geometry.IsNullOrEmpty() ? null : row.Geometry!.AsWkb(),
                Srid = row.Geometry?.Srid ?? 0,
                Attributes = CanonicalAttributeSerializer.Deserialize(row.AttributesJson),
                CommitBatchId = row.CommitBatchId,
                ApproverDisplayName = row.Approver,
                WinningProposalId = row.WinningProposalId,
                EditorDisplayName = row.Editor,
                ChangeType = row.ChangeType,
                SessionTitle = row.SessionTitle,
            });
        }

        return timeline;
    }
}
