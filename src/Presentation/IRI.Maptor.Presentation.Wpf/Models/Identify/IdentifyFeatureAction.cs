using System;
using System.Threading;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf.Layers;

namespace IRI.Maptor.Presentation.Wpf.Models.Identify;

/// <summary>
/// A circle button a layer adds to the identify window for one selected feature of that layer, next to
/// Zoom / Copy / Show in table (e.g. a tower's images, with the image count as a badge). Registered on
/// <see cref="VectorLayer.IdentifyFeatureActions"/>; the identify window shows it only while a single
/// feature of that layer is selected, never for a layer row or a drawing.
/// <para>
/// The definition is shared by every feature of the layer; what one feature shows comes from
/// <see cref="GetStateAsync"/>, asked again each time a feature is selected and after each
/// <see cref="ExecuteAsync"/>, so a label such as a count stays current.
/// </para>
/// </summary>
public class IdentifyFeatureAction
{
    /// <summary>Label shown until (or unless) <see cref="GetStateAsync"/> answers.</summary>
    public required string Text { get; init; }

    public string? ToolTip { get; init; }

    /// <summary>Icon geometry as path markup, as in <see cref="IFeatureTableCommand.PathMarkup"/>; optional.</summary>
    public string? PathMarkup { get; init; }

    /// <summary>
    /// The button's label, tooltip and enabled/visible state for one feature; may call a server.
    /// Null (the delegate or its result) keeps the defaults. The token is cancelled when the user
    /// selects something else, and an answer that arrives after that is ignored.
    /// </summary>
    public Func<VectorLayer, Feature<Point>, CancellationToken, Task<IdentifyFeatureActionState?>>? GetStateAsync { get; init; }

    /// <summary>What the button does for the feature.</summary>
    public required Func<VectorLayer, Feature<Point>, Task> ExecuteAsync { get; init; }
}

/// <summary>
/// How an <see cref="IdentifyFeatureAction"/> looks for one feature. The button is a circle icon like its
/// Zoom / Copy neighbours, so <see cref="Text"/> is its accessible name and default tooltip; a short
/// <see cref="Badge"/> (e.g. a count) is drawn on the circle.
/// </summary>
public record IdentifyFeatureActionState(string Text, string? ToolTip = null, bool IsEnabled = true, bool IsVisible = true, string? Badge = null);
