using System.Threading.Tasks;

using IRI.Maptor.Core.Topology.Fixes;

namespace IRI.Maptor.Presentation.Wpf.Topology;

/// <summary>
/// Where a fix's edits go. By default <see cref="TopologyFixApplier"/>: pending edits on the map layers' editable
/// sources. An app whose map shows features it keeps itself (in memory, from a file it never changes) gives its own
/// target to <see cref="TopologyFixSession"/>, so a fix changes the app's data and the map follows it.
/// </summary>
public interface ITopologyFixTarget
{
    /// <summary>
    /// Null when the fix can be applied; otherwise a reason (<see cref="FixApplyReasons"/> or
    /// <see cref="FixReasons.FeatureMissing"/>) and, for <see cref="FixApplyReasons.NotEditable"/>, the layer.
    /// </summary>
    Task<(string Reason, string? LayerName)?> CannotApplyAsync(TopologyFixProposal proposal);

    /// <summary>Applies every edit of the fix and repaints the map.</summary>
    /// <param name="proposal">The fix; its geometries are in the check's SRS (WGS84 for map layers).</param>
    /// <param name="sessionTitle">A title for the change (the rule's title), for targets that record one.</param>
    Task ApplyAsync(TopologyFixProposal proposal, string sessionTitle);
}
