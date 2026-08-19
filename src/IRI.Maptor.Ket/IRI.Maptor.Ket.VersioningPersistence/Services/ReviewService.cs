using System.Text.Json;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Exceptions;
using IRI.Maptor.Sta.Versioning;
using Microsoft.EntityFrameworkCore;

namespace IRI.Maptor.Ket.VersioningPersistence.Services;

/// <summary>
/// The review half of the pipeline (doc 02 C3/C4/P3–P5 + D16 grouping + D40 bulk accept).
/// Every mutation re-validates guards server-side and rides EF's RowVersion concurrency
/// tokens — a concurrent reviewer loses loudly (E4), never silently.
/// </summary>
public static class ReviewService
{
    // ------------------------------------------------------------------ queue

    public static async Task<List<ReviewQueueItemDto>> GetQueueAsync(
        DbContext context, Guid? layerKey = null, CancellationToken cancellationToken = default)
    {
        var rows = await context.Set<Competition>().AsNoTracking()
            .Where(c => c.State == CompetitionState.Open
                     && (layerKey == null || c.Layer!.LayerKey == layerKey))
            .Select(c => new
            {
                c.Id,
                LayerKey = c.Layer!.LayerKey,
                c.Layer.EntityName,
                c.TargetFeatureId,
                c.Kind,
                c.RowVersion,
                PredecessorState = (CompetitionState?)c.Predecessor!.State,
                Proposals = c.Proposals
                    .Where(p => p.State <= ProposalState.ProvisionallyRejected)
                    .Select(p => new { p.Id, p.EditorDisplayName, p.SubmittedAt, p.BaseRowVersion, p.ChangeType })
                    .ToList(),
                SuggestionCount = context.Set<OverlapSuggestion>()
                    .Count(s => s.DismissedAt == null && s.Proposal!.CompetitionId == c.Id),
            })
            .ToListAsync(cancellationToken);

        var items = new List<ReviewQueueItemDto>();

        foreach (var layerGroup in rows.Where(r => r.Proposals.Count > 0).GroupBy(r => r.EntityName))
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
                var live = row.TargetFeatureId is not null && liveRowVersions.TryGetValue(row.TargetFeatureId.Value, out var rv)
                    ? rv
                    : null;

                items.Add(new ReviewQueueItemDto
                {
                    CompetitionId = row.Id,
                    LayerKey = row.LayerKey,
                    EntityName = row.EntityName,
                    TargetFeatureId = row.TargetFeatureId,
                    Kind = row.Kind,
                    CompetitionRowVersion = row.RowVersion,
                    ProposalCount = row.Proposals.Count,
                    OldestSubmittedAt = row.Proposals.Min(p => p.SubmittedAt),
                    IsOrphaned = row.TargetFeatureId is not null && live is null,
                    IsStale = live is not null && row.Proposals.Any(p => VersioningGuards.IsStale(p.BaseRowVersion, live)),
                    IsBlockedByPredecessor = row.PredecessorState is not null
                        && !VersioningGuards.IsCompetitionTerminal(row.PredecessorState.Value),
                    HasDelete = row.Proposals.Any(p => p.ChangeType == ProposalChangeType.Delete),
                    UndismissedSuggestionCount = row.SuggestionCount,
                    AuthorDisplayNames = row.Proposals.Select(p => p.EditorDisplayName).Distinct().ToList(),
                    SingleProposalId = row.Proposals.Count == 1 ? row.Proposals[0].Id : null,
                });
            }
        }

        return items.OrderBy(i => i.OldestSubmittedAt).ToList();
    }

    // ---------------------------------------------------------------- compare

    public static async Task<CompetitionCompareDto> GetCompareAsync(
        DbContext context, long competitionId, CancellationToken cancellationToken = default)
    {
        var competition = await context.Set<Competition>().AsNoTracking()
                .Include(c => c.Layer)
                .Include(c => c.Predecessor)
                .Include(c => c.Proposals.Where(p => p.State <= ProposalState.ProvisionallyRejected))
                .ThenInclude(p => p.Session)
                .SingleOrDefaultAsync(c => c.Id == competitionId, cancellationToken)
            ?? throw new VersioningException("UnknownCompetition", $"no competition {competitionId}.");

        var entityType = LiveFeatureReader.FindEntityType(context, competition.Layer!.EntityName);

        LiveFeatureStateDto? live = null;

        if (competition.TargetFeatureId is not null && entityType is not null)
            live = await LiveFeatureReader.GetSnapshotAsync(context, entityType, competition.TargetFeatureId.Value, cancellationToken);

        var dto = new CompetitionCompareDto
        {
            CompetitionId = competition.Id,
            LayerKey = competition.Layer.LayerKey,
            EntityName = competition.Layer.EntityName,
            TargetFeatureId = competition.TargetFeatureId,
            Kind = competition.Kind,
            State = competition.State,
            CompetitionRowVersion = competition.RowVersion,
            IsBlockedByPredecessor = competition.Predecessor is not null
                && !VersioningGuards.IsCompetitionTerminal(competition.Predecessor.State),
            IsOrphaned = competition.TargetFeatureId is not null && live is null,
            Live = live,
        };

        foreach (var proposal in competition.Proposals.OrderBy(p => p.SubmittedAt))
        {
            dto.Proposals.Add(new ProposalCompareDto
            {
                ProposalId = proposal.Id,
                SessionId = proposal.SessionId,
                SessionTitle = proposal.Session?.Title,
                SessionComment = proposal.Session?.Comment,
                EditorUserId = proposal.EditorUserId,
                EditorDisplayName = proposal.EditorDisplayName,
                ChangeType = proposal.ChangeType,
                GeometryWkb = proposal.ProposedGeometry?.AsWkb(),
                Srid = proposal.ProposedGeometry?.Srid ?? 0,
                Attributes = proposal.ProposedAttributesJson is null
                    ? null
                    : CanonicalAttributeSerializer.Deserialize(proposal.ProposedAttributesJson),
                IsStale = VersioningGuards.IsStale(proposal.BaseRowVersion, live?.RowVersion),
                SubmittedAt = proposal.SubmittedAt,
            });
        }

        return dto;
    }

    // ---------------------------------------------------------- select winner

    public static async Task SelectWinnerAsync(
        DbContext context, SelectWinnerRequestDto request, int reviewerUserId, string reviewerDisplayName,
        CancellationToken cancellationToken = default)
    {
        var (competition, actives) = await LoadOpenCompetitionAsync(context, request.CompetitionId, cancellationToken);

        var winner = actives.SingleOrDefault(p => p.Id == request.WinnerProposalId)
            ?? throw new VersioningException("UnknownProposal", $"proposal {request.WinnerProposalId} is not an active member of competition {competition.Id}.");

        await GuardTargetAsync(context, competition, winner, request.StaleOverride, cancellationToken);

        var losers = actives.Where(p => p.Id != winner.Id).ToList();
        var reasons = ResolveReasons(losers, request.LoserReasons, request.ReasonForAll);

        var now = DateTime.UtcNow;

        winner.State = ProposalState.SelectedForApproval;
        winner.DecidedAt = now;

        context.Set<DecisionRecord>().Add(new DecisionRecord
        {
            CompetitionId = competition.Id,
            ProposalId = winner.Id,
            ActorUserId = reviewerUserId,
            ActorDisplayName = reviewerDisplayName,
            Action = DecisionAction.SelectWinner,
            IsStaleOverride = request.StaleOverride,
            CreatedAt = now,
        });

        foreach (var loser in losers)
        {
            loser.State = ProposalState.ProvisionallyRejected;
            loser.DecidedAt = now;

            context.Set<DecisionRecord>().Add(new DecisionRecord
            {
                CompetitionId = competition.Id,
                ProposalId = loser.Id,
                ActorUserId = reviewerUserId,
                ActorDisplayName = reviewerDisplayName,
                Action = DecisionAction.RejectProposal,
                Reason = reasons[loser.Id],
                CreatedAt = now,
            });
        }

        competition.State = CompetitionState.Resolved;
        competition.WinnerProposalId = winner.Id;
        competition.ResolvedAt = now;

        // Loser notifications are deliberately absent here: rejections stay provisional
        // until commit (D17); N3 fires from the commit service (M4).

        await SaveWithConcurrencyGuardAsync(context, competition, request.CompetitionRowVersion, cancellationToken);
    }

    // -------------------------------------------------------- close no winner

    public static async Task CloseNoWinnerAsync(
        DbContext context, CloseNoWinnerRequestDto request, int reviewerUserId, string reviewerDisplayName,
        CancellationToken cancellationToken = default)
    {
        var (competition, actives) = await LoadOpenCompetitionAsync(context, request.CompetitionId, cancellationToken);

        var reasons = ResolveReasons(actives, request.Reasons, request.ReasonForAll);

        var now = DateTime.UtcNow;

        context.Set<DecisionRecord>().Add(new DecisionRecord
        {
            CompetitionId = competition.Id,
            ActorUserId = reviewerUserId,
            ActorDisplayName = reviewerDisplayName,
            Action = DecisionAction.CloseNoWinner,
            CreatedAt = now,
        });

        foreach (var proposal in actives)
        {
            proposal.State = ProposalState.Rejected;
            proposal.DecidedAt = now;
            proposal.FinalizedAt = now;

            context.Set<DecisionRecord>().Add(new DecisionRecord
            {
                CompetitionId = competition.Id,
                ProposalId = proposal.Id,
                ActorUserId = reviewerUserId,
                ActorDisplayName = reviewerDisplayName,
                Action = DecisionAction.RejectProposal,
                Reason = reasons[proposal.Id],
                CreatedAt = now,
            });
        }

        competition.State = CompetitionState.ClosedNoWinner;
        competition.FinalizedAt = now;

        // No live change happened, so rejection is final immediately (D20) — N4 digests now.
        foreach (var editorGroup in actives.GroupBy(p => p.EditorUserId))
        {
            context.Set<VersionNotification>().Add(new VersionNotification
            {
                RecipientUserId = editorGroup.Key,
                Type = NotificationType.ClosedNoWinner,
                CompetitionId = competition.Id,
                SessionId = editorGroup.Select(p => p.SessionId).Distinct().Count() == 1
                    ? editorGroup.First().SessionId
                    : null,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    proposals = editorGroup.Select(p => new
                    {
                        proposalId = p.Id,
                        targetFeatureId = p.TargetFeatureId,
                        reason = reasons[p.Id],
                    }),
                }),
                CreatedAt = now,
            });
        }

        await SaveWithConcurrencyGuardAsync(context, competition, request.CompetitionRowVersion, cancellationToken);
    }

    // -------------------------------------------------------- manual grouping

    public static async Task<long> GroupProposalsAsync(
        DbContext context, GroupProposalsRequestDto request, int reviewerUserId, string reviewerDisplayName,
        CancellationToken cancellationToken = default)
    {
        if (request.ProposalIds.Distinct().Count() < 2)
            throw new VersioningException("InvalidGrouping", "grouping needs at least two distinct proposals.");

        var proposals = await context.Set<Proposal>()
            .Include(p => p.Competition)
            .Where(p => request.ProposalIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (proposals.Count != request.ProposalIds.Distinct().Count())
            throw new VersioningException("InvalidGrouping", "one or more proposals were not found.");

        if (proposals.Any(p => p.State != ProposalState.Submitted || p.Competition!.State != CompetitionState.Open))
            throw new VersioningException("InvalidGrouping", "every proposal must be pending in an Open competition.");

        // v1 scope (D16): creates within one layer — the only case with no shared id.
        if (proposals.Any(p => p.ChangeType != ProposalChangeType.Create))
            throw new VersioningException("InvalidGrouping", "only create-proposals can be grouped manually.");

        if (proposals.Select(p => p.VersionedLayerId).Distinct().Count() != 1)
            throw new VersioningException("InvalidGrouping", "proposals must belong to one layer.");

        var target = proposals.OrderBy(p => p.CompetitionId).First().Competition!;
        var emptiedCompetitions = proposals
            .Select(p => p.Competition!)
            .Where(c => c.Id != target.Id)
            .DistinctBy(c => c.Id)
            .ToList();

        var now = DateTime.UtcNow;

        foreach (var proposal in proposals)
            proposal.CompetitionId = target.Id;

        target.Kind = CompetitionKind.ManualGroup;

        foreach (var emptied in emptiedCompetitions)
        {
            emptied.State = CompetitionState.Dissolved;
            emptied.FinalizedAt = now;
        }

        context.Set<DecisionRecord>().Add(new DecisionRecord
        {
            CompetitionId = target.Id,
            ActorUserId = reviewerUserId,
            ActorDisplayName = reviewerDisplayName,
            Action = DecisionAction.GroupProposals,
            Reason = $"Grouped proposals: {string.Join(", ", proposals.Select(p => p.Id))}",
            CreatedAt = now,
        });

        // The owners are now in competition — count-only digests (D18/N1).
        foreach (var owner in proposals.Select(p => p.EditorUserId).Distinct())
        {
            context.Set<VersionNotification>().Add(new VersionNotification
            {
                RecipientUserId = owner,
                Type = NotificationType.CompetitionJoined,
                CompetitionId = target.Id,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    competitions = new[] { new { competitionId = target.Id, targetFeatureId = (long?)null } },
                }),
                CreatedAt = now,
            });
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new VersioningException("ConcurrentModification", "the competition or a proposal changed while grouping; refresh and retry.");
        }

        return target.Id;
    }

    // ------------------------------------------------------------ suggestions

    public static async Task DismissSuggestionAsync(
        DbContext context, long suggestionId, int reviewerUserId, string reviewerDisplayName,
        CancellationToken cancellationToken = default)
    {
        var suggestion = await context.Set<OverlapSuggestion>()
                .SingleOrDefaultAsync(s => s.Id == suggestionId, cancellationToken)
            ?? throw new VersioningException("UnknownSuggestion", $"no overlap suggestion {suggestionId}.");

        if (suggestion.DismissedAt is not null)
            return; // idempotent

        suggestion.DismissedByUserId = reviewerUserId;
        suggestion.DismissedByDisplayName = reviewerDisplayName;
        suggestion.DismissedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }

    // ------------------------------------------------------------ bulk accept

    /// <summary>
    /// D40: singleton competitions only, one confirmation client-side; per-item outcomes —
    /// a failing item is reported and the rest still succeed (each item commits alone).
    /// </summary>
    public static async Task<List<BulkAcceptResultItemDto>> BulkAcceptAsync(
        DbContext context, BulkAcceptRequestDto request, int reviewerUserId, string reviewerDisplayName,
        CancellationToken cancellationToken = default)
    {
        var results = new List<BulkAcceptResultItemDto>();

        foreach (var item in request.Items)
        {
            var result = new BulkAcceptResultItemDto { CompetitionId = item.CompetitionId };

            try
            {
                var (competition, actives) = await LoadOpenCompetitionAsync(context, item.CompetitionId, cancellationToken);

                if (actives.Count != 1)
                    throw new VersioningException("NotASingleton", $"competition {item.CompetitionId} has {actives.Count} active proposals; use the compare view.");

                var winner = actives[0];

                await GuardTargetAsync(context, competition, winner, item.StaleOverride, cancellationToken);

                var now = DateTime.UtcNow;

                winner.State = ProposalState.SelectedForApproval;
                winner.DecidedAt = now;

                competition.State = CompetitionState.Resolved;
                competition.WinnerProposalId = winner.Id;
                competition.ResolvedAt = now;

                context.Set<DecisionRecord>().Add(new DecisionRecord
                {
                    CompetitionId = competition.Id,
                    ProposalId = winner.Id,
                    ActorUserId = reviewerUserId,
                    ActorDisplayName = reviewerDisplayName,
                    Action = DecisionAction.SelectWinner,
                    IsStaleOverride = item.StaleOverride,
                    CreatedAt = now,
                });

                await SaveWithConcurrencyGuardAsync(context, competition, item.CompetitionRowVersion, cancellationToken);

                result.Succeeded = true;
                result.WinnerProposalId = winner.Id;
            }
            catch (DomainException ex)
            {
                result.Error = ex.Message;
                context.ChangeTracker.Clear();
            }

            results.Add(result);
        }

        return results;
    }

    // ---------------------------------------------------------------- shared

    private static async Task<(Competition Competition, List<Proposal> Actives)> LoadOpenCompetitionAsync(
        DbContext context, long competitionId, CancellationToken cancellationToken)
    {
        var competition = await context.Set<Competition>()
                .Include(c => c.Layer)
                .Include(c => c.Predecessor)
                .Include(c => c.Proposals.Where(p => p.State <= ProposalState.ProvisionallyRejected))
                .SingleOrDefaultAsync(c => c.Id == competitionId, cancellationToken)
            ?? throw new VersioningException("UnknownCompetition", $"no competition {competitionId}.");

        if (competition.State != CompetitionState.Open)
            throw new VersioningException("CompetitionAlreadyResolved", $"competition {competitionId} is {competition.State}.");

        if (!VersioningGuards.CanResolveCompetition(competition.State, competition.Predecessor?.State))
            throw new VersioningException("CompetitionBlockedByPredecessor", $"competition {competitionId} is queued behind an unresolved predecessor (D19).");

        var actives = competition.Proposals.Where(p => p.State <= ProposalState.ProvisionallyRejected).ToList();

        if (actives.Count == 0)
            throw new VersioningException("EmptyCompetition", $"competition {competitionId} has no active proposals.");

        return (competition, actives);
    }

    /// <summary>Orphan (D23: reject-only) and stale (D8: recorded override) gates for the winner.</summary>
    private static async Task GuardTargetAsync(
        DbContext context, Competition competition, Proposal winner, bool staleOverride, CancellationToken cancellationToken)
    {
        if (competition.TargetFeatureId is null)
            return;

        var entityType = LiveFeatureReader.FindEntityType(context, competition.Layer!.EntityName)
            ?? throw new VersioningException("UnknownVersionedLayer", $"entity '{competition.Layer.EntityName}' is not in the model.");

        var live = await LiveFeatureReader.GetRowVersionsAsync(
            context, entityType, new[] { competition.TargetFeatureId.Value }, cancellationToken);

        if (!live.TryGetValue(competition.TargetFeatureId.Value, out var liveRowVersion))
            throw new VersioningException("OrphanedTargetRejectOnly", $"feature {competition.TargetFeatureId} no longer exists in live; only close-no-winner is allowed (D23).");

        if (VersioningGuards.IsStale(winner.BaseRowVersion, liveRowVersion) && !staleOverride)
            throw new VersioningException("StaleBaseRequiresOverride", $"the live feature changed after proposal {winner.Id} was authored; accepting requires an explicit override (D8).");
    }

    private static Dictionary<long, string> ResolveReasons(
        IReadOnlyList<Proposal> proposals, Dictionary<long, string>? perProposal, string? reasonForAll)
    {
        var reasons = new Dictionary<long, string>();

        foreach (var proposal in proposals)
        {
            var reason = perProposal is not null && perProposal.TryGetValue(proposal.Id, out var specific) && !string.IsNullOrWhiteSpace(specific)
                ? specific
                : reasonForAll;

            if (string.IsNullOrWhiteSpace(reason))
                throw new VersioningException("RejectionReasonRequired", $"proposal {proposal.Id} needs a rejection reason (FR-3.5).");

            reasons[proposal.Id] = reason!;
        }

        return reasons;
    }

    private static async Task SaveWithConcurrencyGuardAsync(
        DbContext context, Competition competition, byte[] expectedRowVersion, CancellationToken cancellationToken)
    {
        if (expectedRowVersion.Length > 0)
            context.Entry(competition).Property(nameof(Competition.RowVersion)).OriginalValue = expectedRowVersion;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new VersioningException("CompetitionAlreadyResolved", $"competition {competition.Id} was modified by another reviewer (E4); refresh and retry.");
        }
    }
}
