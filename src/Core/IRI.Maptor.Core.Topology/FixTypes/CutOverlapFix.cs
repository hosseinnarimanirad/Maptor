using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Fixes;
using IRI.Maptor.Core.Topology.RuleTypes;

namespace IRI.Maptor.Core.Topology.FixTypes;

/// <summary>
/// Fix for R40 "overlap", "thin overlap" and "inside": the shared area is cut out of one of the two polygons
/// (<see cref="PolygonBoolean.Difference{T}"/>). The lower-ranked layer gives it up (FR-5.4); with equal ranks the
/// expert chooses, so there is one offer per polygon. A polygon that would vanish gets no offer. Message arguments:
/// the polygon cut, the polygon kept.
/// </summary>
public sealed class CutOverlapFix : ITopologyFixType
{
    public const string TypeId = "cutOverlap";

    public const string CutFrom = "cutFrom";

    /// <summary>Boundaries closer than this are one line, as the overlap rule measures them.</summary>
    private const double ExactTolerance = 1e-6;

    private static readonly IReadOnlyCollection<string> FixedKinds = new[] { PolygonsNoOverlapRuleType.Overlap, PolygonsNoOverlapRuleType.ThinOverlap, PolygonsNoOverlapRuleType.Inside };

    public string Id => TypeId;

    public string RuleTypeId => PolygonsNoOverlapRuleType.TypeId;

    public IReadOnlyCollection<string> Kinds => FixedKinds;

    public IReadOnlyList<TopologyFixOffer> Propose(TopologyFixContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        var members = context.Features.Where(m => m.Feature.TheGeometry is not null).ToList();

        if (members.Count != 2)
            return new[] { TopologyFixOffer.NotOffered(Id, FixReasons.FeatureMissing) };

        var lowest = members.Min(m => context.RankOf(m.Reference.EntityName));

        var offers = new List<TopologyFixOffer>();

        for (int i = 0; i < 2; i++)
        {
            var (cut, cutFeature) = members[i];
            var (kept, keptFeature) = members[1 - i];

            if (context.RankOf(cut.EntityName) != lowest)
                continue;

            var after = PolygonBoolean.Difference(cutFeature.TheGeometry, keptFeature.TheGeometry, context.IsGeodetic, ExactTolerance);

            if (after is null)
            {
                offers.Add(TopologyFixOffer.NotOffered(Id, FixReasons.NothingLeft, cut));
                continue;
            }

            var area = Intersection(cutFeature.TheGeometry.GetBoundingBox(), keptFeature.TheGeometry.GetBoundingBox());

            var edit = new FeatureEdit(cut.EntityName, cut.FeatureId, cutFeature.TheGeometry, after);

            offers.Add(TopologyFixOffer.Offered(new TopologyFixProposal(Id, CutFrom, context.Finding, new[] { edit }, area, new object[] { cut, kept })));
        }

        return offers;
    }

    private static BoundingBox Intersection(BoundingBox a, BoundingBox b)
        => new(Math.Max(a.XMin, b.XMin), Math.Max(a.YMin, b.YMin), Math.Min(a.XMax, b.XMax), Math.Min(a.YMax, b.YMax));
}
