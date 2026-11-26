using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Topology.Findings;

namespace IRI.Maptor.Core.Topology.Fixes;

/// <summary>
/// What one fix would do to the data, computed without writing anything (FR-5.2): the edits, the area they change,
/// and the values for the sentence that describes it.
/// </summary>
public sealed class TopologyFixProposal
{
    /// <param name="fixTypeId">The fix type that made it.</param>
    /// <param name="variant">Which way the fix type chose (e.g. move the point, or move a vertex of the line).</param>
    /// <param name="finding">The finding it fixes.</param>
    /// <param name="edits">The changes, at least one.</param>
    /// <param name="area">Where the data changes, in the edits' SRS: the fix is checked again here (FR-5.6).</param>
    /// <param name="messageArguments">Values for the sentence of the variant.</param>
    public TopologyFixProposal(string fixTypeId, string variant, TopologyFinding finding, IReadOnlyList<FeatureEdit> edits, BoundingBox area, IReadOnlyList<object>? messageArguments = null)
    {
        if (string.IsNullOrWhiteSpace(fixTypeId))
            throw new ArgumentException("A proposal needs its fix type.", nameof(fixTypeId));

        if (edits is null || edits.Count == 0)
            throw new ArgumentException("A proposal changes at least one feature.", nameof(edits));

        FixTypeId = fixTypeId;
        Variant = variant ?? string.Empty;
        Finding = finding ?? throw new ArgumentNullException(nameof(finding));
        Edits = edits.ToList();
        Area = area;
        MessageArguments = messageArguments?.ToList() ?? (IReadOnlyList<object>)Array.Empty<object>();
    }

    public string FixTypeId { get; }

    public string Variant { get; }

    public TopologyFinding Finding { get; }

    public IReadOnlyList<FeatureEdit> Edits { get; }

    public BoundingBox Area { get; }

    public IReadOnlyList<object> MessageArguments { get; }

    public override string ToString() => $"{FixTypeId}/{Variant}: {string.Join(", ", Edits)}";
}

/// <summary>Why a fix is not offered or is refused; the user-facing sentence is keyed on these.</summary>
public static class FixReasons
{
    /// <summary>A feature of the finding is not in the data any more.</summary>
    public const string FeatureMissing = "featureMissing";

    /// <summary>The place to fix (the crossing, the end, the other line) is not where the check found it; check again.</summary>
    public const string NotFound = "notFound";

    /// <summary>The fix would move something farther than the rule's fix distance (arguments: distance, fix distance).</summary>
    public const string BeyondFixDistance = "beyondFixDistance";

    /// <summary>After the fix the same issue is still found.</summary>
    public const string DoesNotResolve = "doesNotResolve";

    /// <summary>The fix causes another issue at least as severe (FR-5.6).</summary>
    public const string CreatesIssue = "createsIssue";
}

/// <summary>A fix type's answer for one finding: a proposal, or the reason there is none.</summary>
public sealed class TopologyFixOffer
{
    private TopologyFixOffer(string fixTypeId, TopologyFixProposal? proposal, string? reason, IReadOnlyList<object> reasonArguments)
    {
        FixTypeId = fixTypeId;
        Proposal = proposal;
        Reason = reason;
        ReasonArguments = reasonArguments;
    }

    public static TopologyFixOffer Offered(TopologyFixProposal proposal)
        => new((proposal ?? throw new ArgumentNullException(nameof(proposal))).FixTypeId, proposal, null, Array.Empty<object>());

    public static TopologyFixOffer NotOffered(string fixTypeId, string reason, params object[] arguments)
        => new(fixTypeId, null, reason ?? throw new ArgumentNullException(nameof(reason)), arguments ?? Array.Empty<object>());

    public string FixTypeId { get; }

    public TopologyFixProposal? Proposal { get; }

    public bool IsOffered => Proposal is not null;

    /// <summary>One of <see cref="FixReasons"/> when there is no proposal.</summary>
    public string? Reason { get; }

    public IReadOnlyList<object> ReasonArguments { get; }

    public override string ToString() => IsOffered ? Proposal!.ToString() : $"{FixTypeId}: {Reason}";
}
