using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Topology.Fixes;

/// <summary>
/// One change a fix makes to one feature: a new geometry, or its removal. The feature is named by layer and id, as
/// findings name it; the geometries are in the SRS the check read (WGS84 for map layers). It maps one to one onto
/// <c>IEditableVectorDataSource.UpdateGeometry</c> / <c>Remove</c>, so it has no change type of its own. A fix never
/// changes attributes.
/// </summary>
public sealed class FeatureEdit
{
    /// <param name="entityName">The layer of the feature.</param>
    /// <param name="featureId">The feature's id.</param>
    /// <param name="before">Its geometry as the check read it (for the preview).</param>
    /// <param name="after">Its new geometry; null removes the feature.</param>
    public FeatureEdit(string entityName, long featureId, Geometry<Point> before, Geometry<Point>? after)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("An edit needs its layer.", nameof(entityName));

        EntityName = entityName;
        FeatureId = featureId;
        Before = before ?? throw new ArgumentNullException(nameof(before));
        After = after;
    }

    public string EntityName { get; }

    public long FeatureId { get; }

    public Geometry<Point> Before { get; }

    public Geometry<Point>? After { get; }

    public bool IsRemoval => After is null;

    public override string ToString() => $"{EntityName}:{FeatureId} {(IsRemoval ? "removed" : "moved")}";
}
