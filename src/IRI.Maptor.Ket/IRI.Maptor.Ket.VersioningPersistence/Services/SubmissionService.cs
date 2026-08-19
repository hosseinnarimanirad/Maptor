using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Exceptions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Versioning;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text.Json;

namespace IRI.Maptor.Ket.VersioningPersistence.Services;

/// <summary>
/// The submission half of the versioning pipeline (doc 02 P1/P2, C1/C2): validates a
/// session, resolves each proposal into its competition (join an Open one, queue behind
/// a Resolved one, or open a singleton), applies self-supersede, runs the overlap scan,
/// and writes the N1 digests. One transaction per submission; an ambient transaction on
/// the context is honored instead of starting a new one (test harnesses roll back).
/// </summary>
public static class SubmissionService
{
    public static async Task<SessionSubmitResultDto> SubmitAsync(
        DbContext context,
        SessionSubmitDto submission,
        int editorUserId,
        string editorDisplayName,
        CancellationToken cancellationToken = default)
    {
        if (submission.Proposals.Count == 0)
            throw new VersioningException("InvalidSubmission", "the session contains no proposals.");

        if (context.Database.CurrentTransaction is not null)
            return await SubmitCoreAsync(context, submission, editorUserId, editorDisplayName, cancellationToken);

        // Contexts configured with EnableRetryOnFailure reject user transactions outside
        // the execution strategy. The delegate may re-run on transient failures, so the
        // tracker is cleared first — a rolled-back attempt must not leave doubled Adds.
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var result = await SubmitCoreAsync(context, submission, editorUserId, editorDisplayName, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        });
    }

    private static async Task<SessionSubmitResultDto> SubmitCoreAsync(
        DbContext context,
        SessionSubmitDto submission,
        int editorUserId,
        string editorDisplayName,
        CancellationToken cancellationToken)
    {
        var layers = await LoadLayersAsync(context, submission, cancellationToken);

        ValidateNoDuplicateTargets(submission);

        var session = new VersionSession
        {
            EditorUserId = editorUserId,
            EditorDisplayName = editorDisplayName,
            Title = submission.Title,
            Comment = submission.Comment,
            State = SessionState.Submitted,
            SubmittedAt = DateTime.UtcNow,
        };
        context.Set<VersionSession>().Add(session);

        var joinedCompetitionOwners = new Dictionary<Competition, List<int>>();
        var entries = new List<(ProposalSubmitDto Dto, Proposal Proposal, long? SupersededId)>();

        foreach (var dto in submission.Proposals)
        {
            var layer = layers[dto.LayerKey];

            ValidateProposal(dto);

            var geometry = ParseGeometry(dto);

            var supersededId = await ApplySelfSupersedeAsync(context, layer, dto, editorUserId, cancellationToken);

            var competition = await ResolveCompetitionAsync(
                context, layer, dto, editorUserId, joinedCompetitionOwners, cancellationToken);

            var proposal = new Proposal
            {
                Session = session,
                EditorUserId = editorUserId,
                EditorDisplayName = editorDisplayName,
                VersionedLayerId = layer.Id,
                TargetFeatureId = dto.TargetFeatureId,
                ClientKey = dto.ClientKey,
                ChangeType = dto.ChangeType,
                ProposedGeometry = geometry,
                ProposedAttributesJson = dto.Attributes is null
                    ? null
                    : CanonicalAttributeSerializer.Serialize(dto.Attributes),
                BaseRowVersion = dto.BaseRowVersion,
                SchemaSignatureAtSubmit = layer.SchemaSignature ?? string.Empty,
                Competition = competition,
                State = ProposalState.Submitted,
                SubmittedAt = DateTime.UtcNow,
            };

            context.Set<Proposal>().Add(proposal);
            entries.Add((dto, proposal, supersededId));
        }

        await SaveGuardedAsync(context, cancellationToken);

        // Overlap scan + competitor counts need the rows in the database.
        var results = new List<ProposalSubmitResultDto>();

        foreach (var (dto, proposal, supersededId) in entries)
        {
            var layer = layers[dto.LayerKey];

            var competitorCount = await context.Set<Proposal>()
                .CountAsync(p => p.CompetitionId == proposal.CompetitionId
                              && p.State <= ProposalState.ProvisionallyRejected,
                    cancellationToken);

            var overlappingLive = await RunOverlapScanAsync(context, layer, proposal, cancellationToken);

            results.Add(new ProposalSubmitResultDto
            {
                ClientKey = dto.ClientKey,
                ProposalId = proposal.Id,
                Status = VersioningGuards.GetEditorFacingStatus(proposal.State, competitorCount),
                CompetitorCount = competitorCount,
                SupersededProposalId = supersededId,
                OverlappingLiveFeatureIds = overlappingLive,
            });
        }

        WriteJoinDigests(context, session, joinedCompetitionOwners, editorUserId);

        await SaveGuardedAsync(context, cancellationToken);

        return new SessionSubmitResultDto
        {
            SessionId = session.Id,
            Proposals = results,
        };
    }

    // ---------------------------------------------------------------- validation

    private static async Task<Dictionary<Guid, VersionedLayer>> LoadLayersAsync(
        DbContext context, SessionSubmitDto submission, CancellationToken cancellationToken)
    {
        var keys = submission.Proposals.Select(p => p.LayerKey).Distinct().ToList();

        var layers = await context.Set<VersionedLayer>()
            .Where(l => keys.Contains(l.LayerKey))
            .ToDictionaryAsync(l => l.LayerKey, cancellationToken);

        foreach (var key in keys)
        {
            if (!layers.TryGetValue(key, out var layer))
                throw new VersioningException("UnknownVersionedLayer", $"no versioned layer is registered with key {key}.");

            if (!layer.IsVersioningEnabled)
                throw new VersioningException("VersioningNotEnabled", $"layer '{layer.EntityName}' is not under versioning; use the ordinary sync path.");
        }

        return layers;
    }

    private static void ValidateNoDuplicateTargets(SessionSubmitDto submission)
    {
        var duplicateTarget = submission.Proposals
            .Where(p => p.TargetFeatureId is not null)
            .GroupBy(p => (p.LayerKey, p.TargetFeatureId))
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateTarget is not null)
            throw new VersioningException("InvalidSubmission", $"feature {duplicateTarget.Key.TargetFeatureId} is targeted by more than one proposal in this session.");

        var duplicateClientKey = submission.Proposals
            .GroupBy(p => p.ClientKey)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateClientKey is not null)
            throw new VersioningException("InvalidSubmission", $"client key {duplicateClientKey.Key} is used by more than one proposal.");
    }

    private static void ValidateProposal(ProposalSubmitDto dto)
    {
        switch (dto.ChangeType)
        {
            case ProposalChangeType.Create:
                if (dto.TargetFeatureId is not null)
                    throw new VersioningException("InvalidProposal", $"a create must not reference an existing feature (client key {dto.ClientKey}).");
                if (dto.BaseRowVersion is not null)
                    throw new VersioningException("InvalidProposal", $"a create has no base RowVersion (client key {dto.ClientKey}).");
                RequireFullState(dto);
                break;

            case ProposalChangeType.Update:
                RequireTargetAndBase(dto);
                RequireFullState(dto);
                break;

            case ProposalChangeType.Delete:
                RequireTargetAndBase(dto);
                if (dto.GeometryBytes is not null || dto.Attributes is not null)
                    throw new VersioningException("InvalidProposal", $"a delete carries no proposed state (client key {dto.ClientKey}).");
                break;

            default:
                throw new VersioningException("InvalidProposal", $"unknown change type (client key {dto.ClientKey}).");
        }
    }

    private static void RequireTargetAndBase(ProposalSubmitDto dto)
    {
        if (dto.TargetFeatureId is null)
            throw new VersioningException("InvalidProposal", $"{dto.ChangeType} requires a target feature (client key {dto.ClientKey}).");

        if (dto.BaseRowVersion is null || dto.BaseRowVersion.Length == 0)
            throw new VersioningException("InvalidProposal", $"{dto.ChangeType} requires the base RowVersion the edit was made against (client key {dto.ClientKey}).");
    }

    private static void RequireFullState(ProposalSubmitDto dto)
    {
        // A proposal stores the FULL proposed state (FR-1.3), never a delta.
        if (dto.GeometryBytes is null || dto.GeometryBytes.Length == 0)
            throw new VersioningException("InvalidProposal", $"{dto.ChangeType} requires the proposed geometry (client key {dto.ClientKey}).");

        if (dto.Attributes is null)
            throw new VersioningException("InvalidProposal", $"{dto.ChangeType} requires the proposed attributes (client key {dto.ClientKey}).");
    }

    private static Geometry<Point>? ParseGeometry(ProposalSubmitDto dto)
    {
        if (dto.GeometryBytes is null || dto.GeometryBytes.Length == 0)
            return null;

        if (dto.Srid != SridHelper.GeodeticWGS84)
            throw new VersioningException("InvalidProposal", $"geometry must be WGS84 (srid {SridHelper.GeodeticWGS84}), got {dto.Srid} (client key {dto.ClientKey}).");

        var geometry = Geometry<Point>.FromWkb(dto.GeometryBytes, dto.Srid);

        if (geometry is null || geometry.IsNullOrEmpty())
            throw new VersioningException("InvalidProposal", $"geometry could not be parsed (client key {dto.ClientKey}).");

        return geometry;
    }

    // ------------------------------------------------- supersede + competitions

    /// <summary>
    /// D22: one active proposal per editor + target — a newer submission auto-withdraws
    /// the editor's older pending proposal. A proposal whose competition is already
    /// Resolved cannot be superseded (its withdrawal is barred during approval, E5).
    /// </summary>
    private static async Task<long?> ApplySelfSupersedeAsync(
        DbContext context,
        VersionedLayer layer,
        ProposalSubmitDto dto,
        int editorUserId,
        CancellationToken cancellationToken)
    {
        if (dto.TargetFeatureId is null)
            return null;

        var existing = await context.Set<Proposal>()
            .Include(p => p.Competition)
            .Where(p => p.VersionedLayerId == layer.Id
                     && p.TargetFeatureId == dto.TargetFeatureId
                     && p.EditorUserId == editorUserId
                     && p.State <= ProposalState.ProvisionallyRejected)
            .SingleOrDefaultAsync(cancellationToken);

        if (existing is null)
            return null;

        if (!VersioningGuards.CanWithdrawProposal(existing.State, existing.Competition!.State))
            throw new VersioningException("ProposalUnderApproval", $"you already have a proposal for feature {dto.TargetFeatureId} in a resolved competition; wait for its commit or return before resubmitting.");

        existing.State = ProposalState.Withdrawn;
        existing.WithdrawCause = WithdrawCause.Superseded;
        existing.FinalizedAt = DateTime.UtcNow;

        return existing.Id;
    }

    /// <summary>
    /// C1/C2 + the D19 queue rule: join the Open competition on the target if one
    /// exists; otherwise, if a Resolved one is awaiting approval, open a queued
    /// successor; otherwise open a singleton. Creates always open singletons — they have
    /// no shared id to collide on (competing creates are grouped by the reviewer, D16).
    /// </summary>
    private static async Task<Competition> ResolveCompetitionAsync(
        DbContext context,
        VersionedLayer layer,
        ProposalSubmitDto dto,
        int editorUserId,
        Dictionary<Competition, List<int>> joinedCompetitionOwners,
        CancellationToken cancellationToken)
    {
        if (dto.TargetFeatureId is null)
            return NewCompetition(context, layer, targetFeatureId: null, predecessorId: null);

        var candidates = await context.Set<Competition>()
            .Where(c => c.VersionedLayerId == layer.Id
                     && c.TargetFeatureId == dto.TargetFeatureId
                     && (c.State == CompetitionState.Open || c.State == CompetitionState.Resolved))
            .ToListAsync(cancellationToken);

        var open = candidates.FirstOrDefault(c => c.State == CompetitionState.Open);

        if (open is not null)
        {
            var existingOwners = await context.Set<Proposal>()
                .Where(p => p.CompetitionId == open.Id
                         && p.State <= ProposalState.ProvisionallyRejected
                         && p.EditorUserId != editorUserId)
                .Select(p => p.EditorUserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (existingOwners.Count > 0)
                joinedCompetitionOwners[open] = existingOwners;

            return open;
        }

        var resolved = candidates.FirstOrDefault(c => c.State == CompetitionState.Resolved);

        return NewCompetition(context, layer, dto.TargetFeatureId, resolved?.Id);
    }

    private static Competition NewCompetition(DbContext context, VersionedLayer layer, long? targetFeatureId, long? predecessorId)
    {
        var competition = new Competition
        {
            VersionedLayerId = layer.Id,
            TargetFeatureId = targetFeatureId,
            Kind = CompetitionKind.IdCollision,
            State = CompetitionState.Open,
            PredecessorCompetitionId = predecessorId,
            CreatedAt = DateTime.UtcNow,
        };

        context.Set<Competition>().Add(competition);

        return competition;
    }

    // ------------------------------------------------------------- overlap scan

    /// <summary>
    /// D30/D45: one scan per submitted proposal, persisted as dismissable suggestions.
    /// PendingVsLive feeds the editor advisory in the result; PendingVsPending is
    /// reviewer-only (blind rule E8). Direct geography STIntersects — table and key
    /// names come from EF metadata (live tables live in their own schemas, e.g. `sub`).
    /// </summary>
    private static async Task<List<long>> RunOverlapScanAsync(
        DbContext context,
        VersionedLayer layer,
        Proposal proposal,
        CancellationToken cancellationToken)
    {
        if (proposal.ProposedGeometry is null)
            return new List<long>();

        var overlappingLive = new List<long>();

        var entityType = context.Model.GetEntityTypes()
            .FirstOrDefault(t => string.Equals(t.ClrType.Name, layer.EntityName, StringComparison.OrdinalIgnoreCase));

        if (entityType is not null && TryGetTableIdentity(entityType, out var schema, out var table, out var keyColumn))
        {
            var liveSql =
                $"SELECT f.[{keyColumn}] AS [Value] FROM [{schema}].[{table}] f, [versioning].[Proposal] p " +
                "WHERE p.[Id] = @proposalId AND f.[SHAPE] IS NOT NULL " +
                "AND p.[ProposedGeometry].STIntersects(f.[SHAPE]) = 1 " +
                $"AND f.[{keyColumn}] <> ISNULL(p.[TargetFeatureId], -1)";

            overlappingLive = (await context.Database
                    .SqlQueryRaw<int>(liveSql, new SqlParameter("@proposalId", proposal.Id))
                    .ToListAsync(cancellationToken))
                .Select(id => (long)id)
                .ToList();

            foreach (var liveId in overlappingLive)
            {
                context.Set<OverlapSuggestion>().Add(new OverlapSuggestion
                {
                    ProposalId = proposal.Id,
                    Kind = OverlapKind.PendingVsLive,
                    LiveFeatureId = liveId,
                    ComputedAt = DateTime.UtcNow,
                });
            }
        }

        const string pendingSql =
            "SELECT o.[Id] AS [Value] FROM [versioning].[Proposal] o, [versioning].[Proposal] p " +
            "WHERE p.[Id] = @proposalId AND o.[Id] <> p.[Id] " +
            "AND o.[VersionedLayerId] = p.[VersionedLayerId] " +
            "AND o.[EditorUserId] <> p.[EditorUserId] " +
            "AND o.[State] <= 2 AND o.[ProposedGeometry] IS NOT NULL " +
            "AND o.[ProposedGeometry].STIntersects(p.[ProposedGeometry]) = 1";

        var overlappingPending = await context.Database
            .SqlQueryRaw<long>(pendingSql, new SqlParameter("@proposalId", proposal.Id))
            .ToListAsync(cancellationToken);

        foreach (var otherId in overlappingPending)
        {
            context.Set<OverlapSuggestion>().Add(new OverlapSuggestion
            {
                ProposalId = proposal.Id,
                Kind = OverlapKind.PendingVsPending,
                OtherProposalId = otherId,
                ComputedAt = DateTime.UtcNow,
            });
        }

        return overlappingLive;
    }

    private static bool TryGetTableIdentity(IEntityType entityType, out string schema, out string table, out string keyColumn)
    {
        schema = entityType.GetSchema() ?? "dbo";
        table = entityType.GetTableName() ?? string.Empty;
        keyColumn = entityType.FindPrimaryKey()?.Properties.FirstOrDefault()?.GetColumnName() ?? string.Empty;

        return table.Length > 0 && keyColumn.Length > 0;
    }

    // ------------------------------------------------------------ notifications

    /// <summary>
    /// N1 as per-recipient digests (D24): owners whose competition gained a rival learn
    /// the new count — never content, never author names (D18).
    /// </summary>
    private static void WriteJoinDigests(
        DbContext context,
        VersionSession session,
        Dictionary<Competition, List<int>> joinedCompetitionOwners,
        int editorUserId)
    {
        var byRecipient = new Dictionary<int, List<object>>();

        foreach (var (competition, owners) in joinedCompetitionOwners)
        {
            foreach (var owner in owners.Where(o => o != editorUserId))
            {
                if (!byRecipient.TryGetValue(owner, out var items))
                    byRecipient[owner] = items = new List<object>();

                items.Add(new { competitionId = competition.Id, targetFeatureId = competition.TargetFeatureId });
            }
        }

        foreach (var (recipient, items) in byRecipient)
        {
            context.Set<VersionNotification>().Add(new VersionNotification
            {
                RecipientUserId = recipient,
                Type = NotificationType.CompetitionJoined,
                SessionId = session.Id,
                PayloadJson = JsonSerializer.Serialize(new { competitions = items }),
                CreatedAt = DateTime.UtcNow,
            });
        }
    }

    // ------------------------------------------------------------------- saving

    private static async Task SaveGuardedAsync(DbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // The filtered unique indexes are the race backstop (E4-adjacent): two
            // concurrent submissions colliding on the same target lose loudly here.
            throw new VersioningException("ConcurrentSubmission", "another submission touched the same feature at the same moment; refresh and retry.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627);
}
