using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Exceptions;
using IRI.Maptor.Core.Versioning;
using Microsoft.EntityFrameworkCore;

namespace IRI.Maptor.Infrastructure.Versioning.Services;

/// <summary>Read-side queries for the editor-facing endpoints (layers, pending status, own proposals).</summary>
public static class VersioningQueryService
{
    public static async Task<List<VersionedLayerInfoDto>> GetLayersAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        var layers = await context.Set<VersionedLayer>().AsNoTracking().ToListAsync(cancellationToken);

        var tablesByEntityName = context.Model.GetEntityTypes()
            .GroupBy(t => t.ClrType.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().GetTableName() ?? g.Key, StringComparer.OrdinalIgnoreCase);

        return layers
            .Select(l => new VersionedLayerInfoDto
            {
                LayerKey = l.LayerKey,
                EntityName = l.EntityName,
                TableName = tablesByEntityName.TryGetValue(l.EntityName, out var table) ? table : l.EntityName,
                DisplayName = l.DisplayName,
                IsVersioningEnabled = l.IsVersioningEnabled,
            })
            .ToList();
    }

    /// <summary>
    /// The on-demand per-feature check (D34): count + author names of pending proposals,
    /// including other editors' — content stays hidden.
    /// </summary>
    public static async Task<PendingStatusDto> GetPendingStatusAsync(
        DbContext context, Guid layerKey, long featureId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var layer = await context.Set<VersionedLayer>().AsNoTracking()
                .SingleOrDefaultAsync(l => l.LayerKey == layerKey, cancellationToken)
            ?? throw new VersioningException("UnknownVersionedLayer", $"no versioned layer is registered with key {layerKey}.");

        var active = await context.Set<Proposal>().AsNoTracking()
            .Where(p => p.VersionedLayerId == layer.Id
                     && p.TargetFeatureId == featureId
                     && p.State <= ProposalState.ProvisionallyRejected)
            .Select(p => new { p.EditorUserId, p.EditorDisplayName })
            .ToListAsync(cancellationToken);

        return new PendingStatusDto
        {
            Count = active.Count,
            Authors = active.Select(a => a.EditorDisplayName).Distinct().ToList(),
            HasOwn = active.Any(a => a.EditorUserId == currentUserId),
        };
    }

    /// <summary>
    /// Own ACTIVE proposals for one layer, with geometry — feeds the on-demand
    /// own-pending map overlay. Deletes are skipped (no proposed geometry to draw).
    /// </summary>
    public static async Task<List<MyLayerPendingFeatureDto>> GetMyLayerPendingAsync(
        DbContext context, Guid layerKey, int userId, CancellationToken cancellationToken = default)
    {
        var layer = await context.Set<VersionedLayer>().AsNoTracking()
                .SingleOrDefaultAsync(l => l.LayerKey == layerKey, cancellationToken)
            ?? throw new VersioningException("UnknownVersionedLayer", $"no versioned layer is registered with key {layerKey}.");

        var rows = await context.Set<Proposal>().AsNoTracking()
            .Where(p => p.VersionedLayerId == layer.Id
                     && p.EditorUserId == userId
                     && p.State <= ProposalState.ProvisionallyRejected
                     && p.ProposedGeometry != null)
            .Select(p => new
            {
                p.Id,
                p.TargetFeatureId,
                p.ChangeType,
                p.State,
                p.SubmittedAt,
                p.ProposedGeometry,
                CompetitorCount = context.Set<Proposal>()
                    .Count(o => o.CompetitionId == p.CompetitionId && o.State <= ProposalState.ProvisionallyRejected),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new MyLayerPendingFeatureDto
            {
                ProposalId = r.Id,
                TargetFeatureId = r.TargetFeatureId,
                ChangeType = r.ChangeType,
                Status = VersioningGuards.GetEditorFacingStatus(r.State, r.CompetitorCount),
                SubmittedAt = r.SubmittedAt,
                GeometryWkb = r.ProposedGeometry!.AsWkb(),
                Srid = r.ProposedGeometry!.Srid,
            })
            .ToList();
    }

    public static async Task<List<MyProposalDto>> GetMyProposalsAsync(
        DbContext context, int userId, CancellationToken cancellationToken = default)
    {
        // Newest 500: the panel is a working list, not an archive — history views come later.
        var rows = await context.Set<Proposal>().AsNoTracking()
            .Where(p => p.EditorUserId == userId)
            .OrderByDescending(p => p.SubmittedAt)
            .Take(500)
            .Select(p => new
            {
                p.Id,
                p.SessionId,
                SessionTitle = p.Session!.Title,
                EntityName = p.Layer!.EntityName,
                p.TargetFeatureId,
                p.ClientKey,
                p.ChangeType,
                p.State,
                p.SubmittedAt,
                CompetitorCount = context.Set<Proposal>()
                    .Count(o => o.CompetitionId == p.CompetitionId && o.State <= ProposalState.ProvisionallyRejected),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new MyProposalDto
            {
                ProposalId = r.Id,
                SessionId = r.SessionId,
                SessionTitle = r.SessionTitle,
                EntityName = r.EntityName,
                TargetFeatureId = r.TargetFeatureId,
                ClientKey = r.ClientKey,
                ChangeType = r.ChangeType,
                Status = VersioningGuards.GetEditorFacingStatus(r.State, r.CompetitorCount),
                CompetitorCount = r.CompetitorCount,
                SubmittedAt = r.SubmittedAt,
            })
            .ToList();
    }
}
