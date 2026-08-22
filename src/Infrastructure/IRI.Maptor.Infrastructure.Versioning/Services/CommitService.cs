using System.Text.Json;
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Exceptions;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IRI.Maptor.Infrastructure.Versioning.Services;

/// <summary>
/// The approve half of the pipeline (doc 02 P6/P7, C6/C7; algorithm doc 03 §5.2): the
/// approval queue with fresh gate flags, the all-or-nothing commit batch (stale + schema
/// gates, copy-on-write history, editor-stamped live writes, deferred loser
/// notifications), and the return flow that reopens a competition.
/// </summary>
public static class CommitService
{
    // ---------------------------------------------------------- approval queue

    public static async Task<List<ApprovalQueueItemDto>> GetApprovalQueueAsync(
        DbContext context, Guid? layerKey = null, CancellationToken cancellationToken = default)
    {
        var rows = await context.Set<Competition>().AsNoTracking()
            .Where(c => c.State == CompetitionState.Resolved
                     && (layerKey == null || c.Layer!.LayerKey == layerKey))
            .Select(c => new
            {
                c.Id,
                LayerKey = c.Layer!.LayerKey,
                c.Layer.EntityName,
                c.TargetFeatureId,
                c.RowVersion,
                c.ResolvedAt,
                WinnerId = c.WinnerProposalId!.Value,
                WinnerChangeType = c.Winner!.ChangeType,
                WinnerEditor = c.Winner.EditorDisplayName,
                WinnerBaseRowVersion = c.Winner.BaseRowVersion,
                ProposalCount = c.Proposals.Count(p => p.State == ProposalState.SelectedForApproval
                                                    || p.State == ProposalState.ProvisionallyRejected),
                Reviewer = context.Set<DecisionRecord>()
                    .Where(d => d.CompetitionId == c.Id && d.Action == DecisionAction.SelectWinner)
                    .OrderByDescending(d => d.Id)
                    .Select(d => d.ActorDisplayName)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var items = new List<ApprovalQueueItemDto>();

        foreach (var layerGroup in rows.GroupBy(r => r.EntityName))
        {
            var entityType = LiveFeatureReader.FindEntityType(context, layerGroup.Key);

            var targetIds = layerGroup
                .Where(r => r.TargetFeatureId is not null)
                .Select(r => r.TargetFeatureId!.Value)
                .Distinct()
                .ToList();

            var liveRowVersions = entityType is null
                ? new Dictionary<long, byte[]>()
                : await LiveFeatureReader.GetRowVersionsAsync(context, entityType, targetIds, cancellationToken);

            foreach (var row in layerGroup)
            {
                byte[]? live = null;

                var hasLive = row.TargetFeatureId is not null
                    && liveRowVersions.TryGetValue(row.TargetFeatureId.Value, out live);

                items.Add(new ApprovalQueueItemDto
                {
                    CompetitionId = row.Id,
                    LayerKey = row.LayerKey,
                    EntityName = row.EntityName,
                    TargetFeatureId = row.TargetFeatureId,
                    WinnerProposalId = row.WinnerId,
                    WinnerChangeType = row.WinnerChangeType,
                    WinnerEditorDisplayName = row.WinnerEditor,
                    ReviewerDisplayName = row.Reviewer ?? string.Empty,
                    ResolvedAt = row.ResolvedAt ?? default,
                    ProposalCount = row.ProposalCount,
                    IsOrphaned = row.TargetFeatureId is not null && !hasLive,
                    IsStale = hasLive && VersioningGuards.IsStale(row.WinnerBaseRowVersion, live),
                    CompetitionRowVersion = row.RowVersion,
                });
            }
        }

        return items.OrderBy(i => i.ResolvedAt).ToList();
    }

    // ------------------------------------------------------------------ commit

    public static async Task<CommitResultDto> CommitAsync(
        DbContext context, CommitRequestDto request, int approverUserId, string approverDisplayName,
        CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
            throw new VersioningException("InvalidCommit", "the batch contains no competitions.");

        if (context.Database.CurrentTransaction is not null)
            return await CommitCoreAsync(context, request, approverUserId, approverDisplayName, cancellationToken);

        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var result = await CommitCoreAsync(context, request, approverUserId, approverDisplayName, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        });
    }

    private static async Task<CommitResultDto> CommitCoreAsync(
        DbContext context, CommitRequestDto request, int approverUserId, string approverDisplayName,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var warnings = new List<string>();

        var batch = new CommitBatch
        {
            ApproverUserId = approverUserId,
            ApproverDisplayName = approverDisplayName,
            CommittedAt = now,
            CompetitionCount = request.Items.Count,
        };
        context.Add(batch);

        var createdEntries = new List<(Competition Competition, Proposal Winner, EntityEntry Entry, IProperty KeyProperty)>();
        var allWinners = new List<Proposal>();
        var allLosers = new List<Proposal>();

        foreach (var item in request.Items)
        {
            var competition = await context.Set<Competition>()
                    .Include(c => c.Layer)
                    .Include(c => c.Proposals.Where(p => p.State == ProposalState.SelectedForApproval
                                                      || p.State == ProposalState.ProvisionallyRejected))
                    .SingleOrDefaultAsync(c => c.Id == item.CompetitionId, cancellationToken)
                ?? throw new VersioningException("UnknownCompetition", $"no competition {item.CompetitionId}.");

            if (!VersioningGuards.CanApprove(competition.State))
                throw new VersioningException("CompetitionNotResolved", $"competition {competition.Id} is {competition.State}; nothing was committed.");

            if (item.CompetitionRowVersion.Length > 0)
                context.Entry(competition).Property(nameof(Competition.RowVersion)).OriginalValue = item.CompetitionRowVersion;

            var winner = competition.Proposals.SingleOrDefault(p => p.Id == competition.WinnerProposalId)
                ?? throw new VersioningException("UnknownProposal", $"competition {competition.Id} has no selected winner; nothing was committed.");

            var losers = competition.Proposals.Where(p => p.State == ProposalState.ProvisionallyRejected).ToList();

            var entityType = LiveFeatureReader.FindEntityType(context, competition.Layer!.EntityName)
                ?? throw new VersioningException("UnknownVersionedLayer", $"entity '{competition.Layer.EntityName}' is not in the model; nothing was committed.");

            if (winner.ChangeType == ProposalChangeType.Create)
            {
                var entry = LiveEntityWriter.ApplyCreate(
                    context, entityType, winner, winner.EditorUserId, winner.EditorDisplayName, now, warnings);

                var keyProperty = entityType.FindPrimaryKey()!.Properties.First();

                createdEntries.Add((competition, winner, entry, keyProperty));
            }
            else
            {
                var targetId = winner.TargetFeatureId!.Value;

                var liveEntry = LiveEntityWriter.FindLiveEntry(context, entityType, targetId)
                    ?? throw new VersioningException("OrphanedTarget", $"feature {targetId} no longer exists in live; return competition {competition.Id} (D23). Nothing was committed.");

                var liveRowVersion = (byte[])liveEntry.Property("RowVersion").CurrentValue!;

                if (VersioningGuards.IsStale(winner.BaseRowVersion, liveRowVersion) && !item.StaleOverride)
                    throw new VersioningException("StaleBaseRequiresOverride", $"live feature {targetId} changed after proposal {winner.Id} was authored; approving needs a fresh override (D8). Nothing was committed.");

                // Race protection between this read and SaveChanges.
                liveEntry.Property("RowVersion").OriginalValue = liveRowVersion;

                // Copy-on-write BEFORE the live row changes.
                context.Add(BuildHistory(competition, winner, entityType, liveEntry, liveRowVersion, batch, now));

                if (winner.ChangeType == ProposalChangeType.Update)
                    LiveEntityWriter.ApplyUpdate(liveEntry, entityType, winner, winner.EditorUserId, winner.EditorDisplayName, now, warnings);
                else
                    context.Remove(liveEntry.Entity);
            }

            winner.State = ProposalState.Committed;
            winner.FinalizedAt = now;

            foreach (var loser in losers)
            {
                loser.State = ProposalState.Rejected;
                loser.FinalizedAt = now;
            }

            competition.State = CompetitionState.Committed;
            competition.FinalizedAt = now;

            context.Add(new DecisionRecord
            {
                CompetitionId = competition.Id,
                ProposalId = winner.Id,
                ActorUserId = approverUserId,
                ActorDisplayName = approverDisplayName,
                Action = DecisionAction.Approve,
                IsStaleOverride = item.StaleOverride,
                CommitBatch = batch,
                CreatedAt = now,
            });

            allWinners.Add(winner);
            allLosers.AddRange(losers);
        }

        // The review-time rejection reasons, deferred until now (D17).
        var loserIds = allLosers.Select(l => l.Id).ToList();

        var reasons = await context.Set<DecisionRecord>().AsNoTracking()
            .Where(d => d.ProposalId != null && loserIds.Contains(d.ProposalId.Value) && d.Action == DecisionAction.RejectProposal)
            .GroupBy(d => d.ProposalId!.Value)
            .Select(g => new { ProposalId = g.Key, Reason = g.OrderByDescending(d => d.Id).First().Reason })
            .ToDictionaryAsync(x => x.ProposalId, x => x.Reason, cancellationToken);

        await SaveGuardedAsync(context, cancellationToken);

        // Creates now have real ids — backfill so history/timeline queries can find them.
        foreach (var (competition, winner, entry, keyProperty) in createdEntries)
        {
            var newId = Convert.ToInt64(entry.Property(keyProperty.Name).CurrentValue);

            winner.TargetFeatureId = newId;
            competition.TargetFeatureId = newId;
        }

        WriteCommitDigests(context, allWinners, NotificationType.Committed, now, _ => null);
        WriteCommitDigests(context, allLosers, NotificationType.Rejected, now, id => reasons.TryGetValue(id, out var r) ? r : null);

        await SaveGuardedAsync(context, cancellationToken);

        return new CommitResultDto
        {
            CommitBatchId = batch.Id,
            CommittedCompetitionIds = request.Items.Select(i => i.CompetitionId).ToList(),
            Warnings = warnings,
        };
    }

    private static FeatureHistory BuildHistory(
        Competition competition, Proposal winner, IEntityType entityType,
        EntityEntry liveEntry, byte[] liveRowVersion, CommitBatch batch, DateTime now)
    {
        var attributes = new Dictionary<string, object?>();
        Geometry<Point>? geometry = null;

        foreach (var property in entityType.GetProperties())
        {
            var value = liveEntry.Property(property.Name).CurrentValue;

            if (property.ClrType == typeof(Geometry<Point>))
                geometry = (value as Geometry<Point>)?.Clone();
            else if (!string.Equals(property.Name, "RowVersion", StringComparison.OrdinalIgnoreCase))
                attributes[property.Name] = value;
        }

        return new FeatureHistory
        {
            VersionedLayerId = competition.VersionedLayerId,
            FeatureId = winner.TargetFeatureId!.Value,
            Geometry = geometry,
            AttributesJson = CanonicalAttributeSerializer.Serialize(attributes),
            ReplacedRowVersion = liveRowVersion,
            CommitBatch = batch,
            WinningProposal = winner,
            SupersededAt = now,
        };
    }

    private static void WriteCommitDigests(
        DbContext context, List<Proposal> proposals, NotificationType type, DateTime now, Func<long, string?> reasonFor)
    {
        foreach (var editorGroup in proposals.GroupBy(p => p.EditorUserId))
        {
            context.Add(new VersionNotification
            {
                RecipientUserId = editorGroup.Key,
                Type = type,
                SessionId = editorGroup.Select(p => p.SessionId).Distinct().Count() == 1
                    ? editorGroup.First().SessionId
                    : null,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    proposals = editorGroup.Select(p => new
                    {
                        proposalId = p.Id,
                        targetFeatureId = p.TargetFeatureId,
                        reason = reasonFor(p.Id),
                    }),
                }),
                CreatedAt = now,
            });
        }
    }

    // ------------------------------------------------------------------ return

    public static async Task ReturnAsync(
        DbContext context, ReturnRequestDto request, int approverUserId, string approverDisplayName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new VersioningException("ReturnReasonRequired", "a return must explain why (D17).");

        var competition = await context.Set<Competition>()
                .Include(c => c.Proposals.Where(p => p.State == ProposalState.SelectedForApproval
                                                  || p.State == ProposalState.ProvisionallyRejected))
                .SingleOrDefaultAsync(c => c.Id == request.CompetitionId, cancellationToken)
            ?? throw new VersioningException("UnknownCompetition", $"no competition {request.CompetitionId}.");

        if (!VersioningGuards.CanReturn(competition.State))
            throw new VersioningException("CompetitionNotResolved", $"competition {competition.Id} is {competition.State}.");

        if (request.CompetitionRowVersion.Length > 0)
            context.Entry(competition).Property(nameof(Competition.RowVersion)).OriginalValue = request.CompetitionRowVersion;

        var now = DateTime.UtcNow;

        // Everything comes back into play — losers' provisional rejections evaporate (D17).
        foreach (var proposal in competition.Proposals)
        {
            proposal.State = ProposalState.Submitted;
            proposal.DecidedAt = null;
        }

        var reviewerId = await context.Set<DecisionRecord>().AsNoTracking()
            .Where(d => d.CompetitionId == competition.Id && d.Action == DecisionAction.SelectWinner)
            .OrderByDescending(d => d.Id)
            .Select(d => (int?)d.ActorUserId)
            .FirstOrDefaultAsync(cancellationToken);

        competition.State = CompetitionState.Open;
        competition.WinnerProposalId = null;
        competition.ResolvedAt = null;

        context.Add(new DecisionRecord
        {
            CompetitionId = competition.Id,
            ActorUserId = approverUserId,
            ActorDisplayName = approverDisplayName,
            Action = DecisionAction.Return,
            Reason = request.Reason,
            CreatedAt = now,
        });

        if (reviewerId is not null)
        {
            context.Add(new VersionNotification
            {
                RecipientUserId = reviewerId.Value,
                Type = NotificationType.Returned,
                CompetitionId = competition.Id,
                PayloadJson = JsonSerializer.Serialize(new { competitionId = competition.Id, reason = request.Reason }),
                CreatedAt = now,
            });
        }

        await SaveGuardedAsync(context, cancellationToken);
    }

    // ------------------------------------------------------------------ shared

    private static async Task SaveGuardedAsync(DbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new VersioningException("ConcurrentLiveChange", "a competition or live feature changed during the commit; nothing was committed — refresh and retry (E9).");
        }
    }
}
