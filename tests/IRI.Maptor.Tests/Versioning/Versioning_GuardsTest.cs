using IRI.Maptor.Core.Versioning;
using Xunit;

namespace IRI.Maptor.Tests.Versioning;

public class Versioning_GuardsTest
{
    [Theory]
    [InlineData(ProposalState.Submitted, true)]
    [InlineData(ProposalState.SelectedForApproval, true)]
    [InlineData(ProposalState.ProvisionallyRejected, true)]
    [InlineData(ProposalState.Committed, false)]
    [InlineData(ProposalState.Rejected, false)]
    [InlineData(ProposalState.Withdrawn, false)]
    public void IsProposalActive_MatchesTheFilteredIndexBoundary(ProposalState state, bool expected)
    {
        Assert.Equal(expected, VersioningGuards.IsProposalActive(state));

        // The database filtered indexes use [State] <= 2; the guard and the numbering
        // must never drift apart.
        Assert.Equal(expected, (byte)state <= 2);
    }

    [Fact]
    public void CanWithdrawProposal_OnlyWhileSubmittedAndCompetitionOpen()
    {
        Assert.True(VersioningGuards.CanWithdrawProposal(ProposalState.Submitted, CompetitionState.Open));

        // a selected winner cannot withdraw during approval
        Assert.False(VersioningGuards.CanWithdrawProposal(ProposalState.SelectedForApproval, CompetitionState.Resolved));
        Assert.False(VersioningGuards.CanWithdrawProposal(ProposalState.Submitted, CompetitionState.Resolved));
        Assert.False(VersioningGuards.CanWithdrawProposal(ProposalState.ProvisionallyRejected, CompetitionState.Resolved));
    }

    [Fact]
    public void CanWithdrawSession_WithdrawnProposalsDoNotBlock()
    {
        var states = new[] { ProposalState.Submitted, ProposalState.Withdrawn };

        Assert.True(VersioningGuards.CanWithdrawSession(SessionState.Submitted, states));
    }

    [Fact]
    public void CanWithdrawSession_AnyDecidedProposalBlocks()
    {
        var states = new[] { ProposalState.Submitted, ProposalState.ProvisionallyRejected };

        Assert.False(VersioningGuards.CanWithdrawSession(SessionState.Submitted, states));
    }

    [Fact]
    public void CanResolveCompetition_QueuedBehindLivePredecessorIsBlocked()
    {
        Assert.False(VersioningGuards.CanResolveCompetition(CompetitionState.Open, CompetitionState.Resolved));

        Assert.True(VersioningGuards.CanResolveCompetition(CompetitionState.Open, CompetitionState.Committed));
        Assert.True(VersioningGuards.CanResolveCompetition(CompetitionState.Open, predecessorState: null));
    }

    [Fact]
    public void CanApproveAndReturn_OnlyWhileResolved()
    {
        Assert.True(VersioningGuards.CanApprove(CompetitionState.Resolved));
        Assert.True(VersioningGuards.CanReturn(CompetitionState.Resolved));

        Assert.False(VersioningGuards.CanApprove(CompetitionState.Open));
        Assert.False(VersioningGuards.CanReturn(CompetitionState.Committed));
    }

    [Fact]
    public void IsStale_DetectsBaseDriftAndDeletedTarget()
    {
        var baseVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 5 };

        Assert.False(VersioningGuards.IsStale(baseVersion, new byte[] { 0, 0, 0, 0, 0, 0, 0, 5 }));
        Assert.True(VersioningGuards.IsStale(baseVersion, new byte[] { 0, 0, 0, 0, 0, 0, 0, 9 }));

        // target deleted in live → stale for gating purposes
        Assert.True(VersioningGuards.IsStale(baseVersion, liveRowVersion: null));

        // creates have no base and are never stale
        Assert.False(VersioningGuards.IsStale(baseRowVersion: null, liveRowVersion: null));
    }

    [Fact]
    public void EditorFacingStatus_ProvisionalOutcomesAreIndistinguishable()
    {
        // Rejection notifications are deferred until commit; the editor must not be able
        // to tell a provisional loser from the selected winner.
        var selected = VersioningGuards.GetEditorFacingStatus(ProposalState.SelectedForApproval, competitorCount: 3);
        var provisionallyRejected = VersioningGuards.GetEditorFacingStatus(ProposalState.ProvisionallyRejected, competitorCount: 3);

        Assert.Equal(selected, provisionallyRejected);
        Assert.Equal(EditorFacingStatus.UnderReview, selected);
    }

    [Fact]
    public void EditorFacingStatus_SingletonVersusCompetition()
    {
        Assert.Equal(EditorFacingStatus.PendingReview, VersioningGuards.GetEditorFacingStatus(ProposalState.Submitted, competitorCount: 1));
        Assert.Equal(EditorFacingStatus.InCompetition, VersioningGuards.GetEditorFacingStatus(ProposalState.Submitted, competitorCount: 2));
    }

    [Fact]
    public void CanDisableLayer_BlockedWhileProposalsAreActive()
    {
        Assert.False(VersioningGuards.CanDisableLayer(hasActiveProposals: true));
        Assert.True(VersioningGuards.CanDisableLayer(hasActiveProposals: false));
    }
}
