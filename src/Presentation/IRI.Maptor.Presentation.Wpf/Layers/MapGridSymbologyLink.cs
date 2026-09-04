using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using IRI.Maptor.Presentation.Core.Models;

namespace IRI.Maptor.Presentation.Wpf.Layers;

/// <summary>
/// Keeps a grid's lighter weights in step with its principal one, so recolouring the grid recolours
/// all of it.
/// </summary>
/// <remarks>
/// <para>
/// A grid is one layer carrying several weights — principal lines, their subdivisions, and the UTM
/// zone seam — and each weight needs its own <see cref="VisualParameters"/> because they differ in
/// thickness and opacity. But the symbology dialog edits a layer through
/// <c>SymbolizableLayer.GetMainOrDefaultSymbology()</c>, which returns the <em>first</em>
/// symbolizer and nothing else. Without this, changing a grid's colour moved one weight and left
/// the rest behind.
/// </para>
/// <para>
/// So the first symbolizer leads and the others follow: colour is copied across verbatim, and
/// thickness is scaled by the ratio each follower started at, so a grid drawn heavier stays a grid
/// rather than becoming three lines of equal weight. Opacity is deliberately not touched — it is
/// what separates a subdivision from a principal line, and it is not something the dialog sets.
/// </para>
/// <para>
/// Driven by <see cref="INotifyPropertyChanged"/> rather than by intercepting the dialog, because
/// the dialog mutates the parameters it is handed in place; there is no call to intercept. That
/// also means this holds for any other route that edits them.
/// </para>
/// </remarks>
internal sealed class MapGridSymbologyLink
{
    /// <summary>Below this a line stops being drawn at all, so a scaled thickness never reaches zero.</summary>
    private const double MinimumThickness = 0.1;

    private readonly VisualParameters _leader;

    private readonly (VisualParameters Parameters, double ThicknessRatio)[] _followers;

    private bool _isUpdating;

    private MapGridSymbologyLink(VisualParameters leader, IEnumerable<VisualParameters> followers)
    {
        _leader = leader;

        // The ratios are captured once, from the style the grid was built with, so they survive any
        // number of later edits instead of drifting with each one.
        var leaderThickness = leader.StrokeThickness > 0 ? leader.StrokeThickness : 1.0;

        _followers = followers
            .Where(follower => follower is not null && !ReferenceEquals(follower, leader))
            .Select(follower => (follower, follower.StrokeThickness / leaderThickness))
            .ToArray();

        leader.PropertyChanged += OnLeaderChanged;
    }

    /// <summary>
    /// Makes <paramref name="followers"/> track <paramref name="leader"/>. The link lives as long as
    /// the leader does — the event subscription holds it — which is the layer's lifetime.
    /// </summary>
    internal static void Attach(VisualParameters? leader, params VisualParameters?[] followers)
    {
        if (leader is null)
            return;

        var live = followers.Where(follower => follower is not null).Select(follower => follower!).ToList();

        if (live.Count == 0)
            return;

        _ = new MapGridSymbologyLink(leader, live);
    }

    private void OnLeaderChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Followers are written below, and writing them raises their own events; the guard is for
        // the day someone links two grids to each other.
        if (_isUpdating)
            return;

        if (e.PropertyName != nameof(VisualParameters.Stroke)
            && e.PropertyName != nameof(VisualParameters.Fill)
            && e.PropertyName != nameof(VisualParameters.StrokeThickness))
        {
            return;
        }

        _isUpdating = true;

        try
        {
            foreach (var (parameters, thicknessRatio) in _followers)
            {
                parameters.Stroke = _leader.Stroke;
                parameters.Fill = _leader.Fill;
                parameters.StrokeThickness = Math.Max(MinimumThickness, _leader.StrokeThickness * thicknessRatio);
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }
}
