using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.Topology.Fixes;
using IRI.Maptor.Core.Versioning;
using IRI.Maptor.Presentation.Wpf.Layers;

namespace IRI.Maptor.Presentation.Wpf.Topology;

/// <summary>Why a fix cannot be applied to the map layers; the user-facing text is keyed on these.</summary>
public static class FixApplyReasons
{
    /// <summary>The map does not let this user edit geometries (D34).</summary>
    public const string NoPermission = "noPermission";

    /// <summary>A layer of the fix cannot be edited (argument: its name).</summary>
    public const string NotEditable = "notEditable";

    /// <summary>The fix removes a feature and the target keeps every feature (a custom <see cref="ITopologyFixTarget"/>).</summary>
    public const string CannotRemove = "cannotRemove";
}

/// <summary>
/// Applies a fix to the map layers as ordinary pending edits (D32, D46): <c>UpdateGeometry</c> or <c>Remove</c> on
/// the layer's <see cref="IEditableVectorDataSource"/>, so undo and Save work as for any edit. On a versioned layer it
/// names the next review session after the rule. It saves nothing.
/// </summary>
public sealed class TopologyFixApplier : ITopologyFixTarget
{
    private readonly TopologyWorkspace _workspace;

    public TopologyFixApplier(TopologyWorkspace workspace)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    /// <summary>Null when the fix can be applied; otherwise a reason of <see cref="FixApplyReasons"/> or <see cref="FixReasons.FeatureMissing"/>, and the layer.</summary>
    public async Task<(string Reason, string? LayerName)?> CannotApplyAsync(TopologyFixProposal proposal)
    {
        if (proposal is null)
            throw new ArgumentNullException(nameof(proposal));

        if (!_workspace.Map.CanEditGeometry)
            return (FixApplyReasons.NoPermission, null);

        foreach (var edit in proposal.Edits)
        {
            var found = await FindAsync(edit);

            if (found is null)
                return (FixReasons.FeatureMissing, null);

            if (found.Value.Layer.DataSource is not IEditableVectorDataSource)
                return (FixApplyReasons.NotEditable, found.Value.Layer.LayerName);
        }

        return null;
    }

    /// <summary>Applies every edit of the fix as a pending change and repaints the map.</summary>
    /// <param name="proposal">The fix.</param>
    /// <param name="sessionTitle">The review session's title on a versioned layer, when it has none yet.</param>
    /// <exception cref="InvalidOperationException">The fix cannot be applied (see <see cref="CannotApplyAsync"/>).</exception>
    public async Task ApplyAsync(TopologyFixProposal proposal, string sessionTitle)
    {
        var problem = await CannotApplyAsync(proposal);

        if (problem is not null)
            throw new InvalidOperationException($"The fix cannot be applied: {problem.Value.Reason} {problem.Value.LayerName}".Trim());

        foreach (var edit in proposal.Edits)
        {
            var (layer, feature) = (await FindAsync(edit))!.Value;

            if (layer.DataSource is not IEditableVectorDataSource editable)
                throw new InvalidOperationException($"Layer '{layer.LayerName}' cannot be edited.");

            if (edit.IsRemoval)
                editable.Remove(feature);
            else
                editable.UpdateGeometry(feature, ToLayer(edit.After!, feature.TheGeometry?.Srid ?? SridHelper.WebMercator));

            if (layer.DataSource is IVersionedEditTarget target && string.IsNullOrWhiteSpace(target.NextSessionTitle))
                target.NextSessionTitle = sessionTitle;
        }

        _workspace.Map.Refresh(isNewExtent: true);
    }

    /// <summary>A geometry of the check (WGS84) in the layer's SRS: Web Mercator, keeping the layer's SRID (0 included).</summary>
    public static Geometry<Point> ToLayer(Geometry<Point> geometry, int layerSrid)
    {
        if (geometry is null)
            throw new ArgumentNullException(nameof(geometry));

        if (layerSrid == SridHelper.GeodeticWGS84 || geometry.Srid != SridHelper.GeodeticWGS84)
            return geometry;

        var mapped = geometry.Transform(MapProjects.GeodeticWgs84ToWebMercator, SridHelper.WebMercator);
        mapped.Srid = layerSrid;

        return mapped;
    }

    private async Task<(VectorLayer Layer, Feature<Point> Feature)?> FindAsync(FeatureEdit edit)
    {
        foreach (var layer in _workspace.MapLayersOf(edit.EntityName))
        {
            var set = await layer.GetFeaturesAsync();

            var feature = (set?.Features ?? new List<Feature<Point>>()).FirstOrDefault(f => f.Id == edit.FeatureId);

            if (feature is not null)
                return (layer, feature);
        }

        return null;
    }
}
