using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Fixes;

namespace IRI.Maptor.Core.Topology.FixTypes;

/// <summary>
/// Fix for duplicates (R01 points, R22 whole lines, R40 duplicate polygons): keep one feature of the group and delete the others. The expert
/// chooses which one stays, so there is one offer per feature (doc 05 §7.3). Attributes are not merged. Message
/// arguments: the feature kept, the features deleted.
/// </summary>
public sealed class DeleteDuplicateFix : ITopologyFixType
{
    public const string PointsTypeId = "deleteDuplicatePoints";
    public const string LinesTypeId = "deleteDuplicateLines";
    public const string PolygonsTypeId = "deleteDuplicatePolygons";

    public const string Keep = "keep";

    /// <param name="id">The fix type's id, e.g. <see cref="PointsTypeId"/>.</param>
    /// <param name="ruleTypeId">The duplicate rule type it serves.</param>
    /// <param name="kinds">Its kinds that are true duplicates (never a chain).</param>
    public DeleteDuplicateFix(string id, string ruleTypeId, params string[] kinds)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A fix type needs an id.", nameof(id));

        Id = id;
        RuleTypeId = ruleTypeId ?? throw new ArgumentNullException(nameof(ruleTypeId));
        Kinds = kinds?.ToList() ?? throw new ArgumentNullException(nameof(kinds));
    }

    public string Id { get; }

    public string RuleTypeId { get; }

    public IReadOnlyCollection<string> Kinds { get; }

    public IReadOnlyList<TopologyFixOffer> Propose(TopologyFixContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        var members = context.Features.Where(m => m.Feature.TheGeometry is not null).ToList();

        if (members.Count < 2 || members.Count < context.Finding.Features.Count)
            return new[] { TopologyFixOffer.NotOffered(Id, FixReasons.FeatureMissing) };

        var area = FixGeometry.Box(members.Select(m => m.Feature.TheGeometry));

        return members
            .Select(kept =>
            {
                var deleted = members.Where(m => !ReferenceEquals(m.Feature, kept.Feature)).ToList();

                var edits = deleted.Select(m => new FeatureEdit(m.Reference.EntityName, m.Reference.FeatureId, m.Feature.TheGeometry, null)).ToList();

                return TopologyFixOffer.Offered(new TopologyFixProposal(Id, Keep, context.Finding, edits, area,
                    new object[] { kept.Reference, deleted.Select(m => m.Reference).ToList() }));
            })
            .ToList();
    }
}
