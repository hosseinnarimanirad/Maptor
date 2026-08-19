namespace IRI.Maptor.Sta.Versioning;

/// <summary>
/// Pure transition-rule functions shared by client UI (enable/disable actions) and
/// server services. The server remains the authority; the client uses these only to
/// avoid offering actions that would be rejected.
/// </summary>
public static class VersioningGuards
{
    /// <summary>Active = not yet terminal; relies on the ProposalState numbering (&lt;= 2).</summary>
    public static bool IsProposalActive(ProposalState state)
        => state <= ProposalState.ProvisionallyRejected;

    public static bool IsProposalTerminal(ProposalState state)
        => !IsProposalActive(state);

    /// <summary>A proposal may be withdrawn only while its competition is undecided; a selected winner cannot withdraw during approval.</summary>
    public static bool CanWithdrawProposal(ProposalState proposalState, CompetitionState competitionState)
        => proposalState == ProposalState.Submitted && competitionState == CompetitionState.Open;

    /// <summary>A session may be withdrawn while none of its proposals has been decided (withdrawn ones don't block).</summary>
    public static bool CanWithdrawSession(SessionState sessionState, IEnumerable<ProposalState> proposalStates)
        => sessionState == SessionState.Submitted
           && proposalStates.All(s => s == ProposalState.Submitted || s == ProposalState.Withdrawn);

    /// <summary>A late submission can only join an Open competition; a Resolved one gets a queued successor instead.</summary>
    public static bool CanJoinCompetition(CompetitionState state)
        => state == CompetitionState.Open;

    /// <summary>A queued competition cannot be resolved until its predecessor is terminal.</summary>
    public static bool CanResolveCompetition(CompetitionState state, CompetitionState? predecessorState)
        => state == CompetitionState.Open
           && (predecessorState is null || IsCompetitionTerminal(predecessorState.Value));

    public static bool CanSelectWinner(CompetitionState competitionState, ProposalState winnerState, CompetitionState? predecessorState)
        => CanResolveCompetition(competitionState, predecessorState)
           && winnerState == ProposalState.Submitted;

    /// <summary>Closing with no winner touches nothing in live, so it needs no approver gate.</summary>
    public static bool CanCloseNoWinner(CompetitionState state, CompetitionState? predecessorState)
        => CanResolveCompetition(state, predecessorState);

    public static bool CanApprove(CompetitionState state)
        => state == CompetitionState.Resolved;

    public static bool CanReturn(CompetitionState state)
        => state == CompetitionState.Resolved;

    public static bool IsCompetitionTerminal(CompetitionState state)
        => state is CompetitionState.Committed or CompetitionState.ClosedNoWinner or CompetitionState.Dissolved;

    /// <summary>Disabling versioning on a layer is blocked while open proposals exist, so history never gains a gap.</summary>
    public static bool CanDisableLayer(bool hasActiveProposals)
        => !hasActiveProposals;

    /// <summary>Stale = the live row moved past the proposal's base; accepting then requires a recorded override.</summary>
    public static bool IsStale(byte[]? baseRowVersion, byte[]? liveRowVersion)
    {
        if (baseRowVersion is null)
            return false; // creates have no base

        if (liveRowVersion is null)
            return true; // target deleted in live (orphaned counts as stale for gating)

        return !baseRowVersion.AsSpan().SequenceEqual(liveRowVersion);
    }

    /// <summary>
    /// Maps internal state to what the editor is allowed to see. Provisional review
    /// outcomes are indistinguishable from selection on purpose — rejection
    /// notifications are deferred until commit.
    /// </summary>
    public static EditorFacingStatus GetEditorFacingStatus(ProposalState state, int competitorCount)
        => state switch
        {
            ProposalState.Submitted when competitorCount > 1 => EditorFacingStatus.InCompetition,
            ProposalState.Submitted => EditorFacingStatus.PendingReview,
            ProposalState.SelectedForApproval => EditorFacingStatus.UnderReview,
            ProposalState.ProvisionallyRejected => EditorFacingStatus.UnderReview,
            ProposalState.Committed => EditorFacingStatus.Committed,
            ProposalState.Rejected => EditorFacingStatus.Rejected,
            _ => EditorFacingStatus.Withdrawn,
        };
}
