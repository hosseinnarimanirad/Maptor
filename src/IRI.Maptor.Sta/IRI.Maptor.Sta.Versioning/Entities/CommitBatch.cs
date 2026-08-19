namespace IRI.Maptor.Sta.Versioning;

/// <summary>One approval transaction: its competitions commit all-or-nothing.</summary>
public class CommitBatch
{
    public long Id { get; set; }

    public int ApproverUserId { get; set; }

    public string ApproverDisplayName { get; set; } = string.Empty;

    public DateTime CommittedAt { get; set; }

    public int CompetitionCount { get; set; }
}
