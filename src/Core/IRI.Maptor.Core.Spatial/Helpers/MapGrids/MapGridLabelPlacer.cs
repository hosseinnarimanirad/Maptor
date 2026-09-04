using System;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.Helpers.MapGrids;

/// <summary>
/// Decides where each line's value is written, and whether it is spelled out or abbreviated.
/// </summary>
/// <remarks>
/// <para>
/// A value is placed where its line <em>crosses</em> the label anchor — a latitude just inside the
/// bottom of the view, a longitude just inside the left, and so on. Interpolating the crossing
/// rather than taking the line's endpoint is what makes a bowed line work: the numbers stay in a
/// straight row along the margin even though the lines they belong to are curves.
/// </para>
/// <para>
/// The spelled-out / abbreviated decision is tracked <em>per side</em>, and per group where a grid
/// restarts (each UTM zone and hemisphere is its own group). So the first value met along the
/// bottom is written in full, and so is the first one met along the top, even though they belong
/// to the same lines — a reader coming in from either edge finds a full reference there.
/// </para>
/// </remarks>
internal sealed class MapGridLabelPlacer
{
    /// <summary>
    /// Tiers are pushed inward and would eventually meet in the middle; this stops them well short.
    /// </summary>
    private const double MaxInset = 0.4;

    private readonly MapGridDefinition _definition;

    private readonly MapGridOptions _options;

    private readonly double _bottomLatitude;

    private readonly double _topLatitude;

    private readonly double _leftLongitude;

    private readonly double _rightLongitude;

    /// <summary>The high part last written on each (group, axis, side), so a rollover can re-spell the value.</summary>
    private readonly Dictionary<string, long> _runs = new Dictionary<string, long>();

    /// <summary>Every value already written, in geodetic degrees, so the next one can avoid it.</summary>
    private readonly List<Point> _placed = new List<Point>();

    private readonly double _minSeparationX;

    private readonly double _minSeparationY;

    internal MapGridLabelPlacer(BoundingBox geodeticView, MapGridDefinition definition, MapGridOptions options)
    {
        _definition = definition;
        _options = options;

        var inset = Math.Min(options.GetInset(definition.LabelTier), MaxInset);

        _bottomLatitude = geodeticView.YMin + geodeticView.Height * inset;
        _topLatitude = geodeticView.YMax - geodeticView.Height * inset;
        _leftLongitude = geodeticView.XMin + geodeticView.Width * inset;
        _rightLongitude = geodeticView.XMax - geodeticView.Width * inset;

        _minSeparationX = Math.Abs(geodeticView.Width) * options.MinLabelSeparationX;
        _minSeparationY = Math.Abs(geodeticView.Height) * options.MinLabelSeparationY;
    }

    internal List<MapGridLabel> Labels { get; } = new List<MapGridLabel>();

    internal bool IsFull => Labels.Count >= _options.MaxLabels;

    /// <summary>
    /// Writes one line's value on every requested edge it reaches.
    /// </summary>
    /// <param name="parts">The line's geodetic parts, already clipped to the view.</param>
    /// <param name="axis">Which family the line belongs to; decides which two edges are candidates.</param>
    /// <param name="kind">Minor lines are left unnumbered unless <see cref="MapGridOptions.LabelMinorLines"/> says otherwise.</param>
    /// <param name="value">The line's value, carried through to the label for callers that need it.</param>
    /// <param name="zone">The UTM zone the line belongs to, or null.</param>
    /// <param name="highPart">
    /// The digits above the changing ones. A label is spelled out in full when this differs from
    /// the last one written on the same side.
    /// </param>
    /// <param name="text">Builds the text; the argument is true for the spelled-out form.</param>
    /// <param name="groupKey">
    /// Which run the line belongs to. Empty for a single-plane grid; per zone and hemisphere for
    /// UTM, where the numbering genuinely restarts.
    /// </param>
    internal void Place(
        List<List<Point>> parts,
        MapGridAxis axis,
        MapGridLineKind kind,
        double value,
        int? zone,
        long highPart,
        Func<bool, string> text,
        string groupKey)
    {
        if (kind == MapGridLineKind.Minor && !_options.LabelMinorLines)
            return;

        if (parts.Count == 0 || IsFull)
            return;

        foreach (var side in GetSides(axis))
        {
            if ((_definition.LabelSides & side) == 0)
                continue;

            var position = FindCrossing(parts, axis, side);

            if (position is null)
                continue;

            // Checked *before* the run state is touched, and that ordering is load-bearing: a
            // suppressed value must not consume the spelled-out slot, or the next line along the
            // edge would print bare digits with no full reference anywhere to read them against.
            if (IsTooCloseToAnythingAlreadyWritten(position))
                continue;

            _placed.Add(position);

            var key = groupKey + "|" + axis + "|" + side;

            var isFull = !_runs.TryGetValue(key, out var previousHigh) || previousHigh != highPart;

            _runs[key] = highPart;

            Labels.Add(new MapGridLabel(
                text(isFull),
                MapGridGeometry.ToWebMercator(position.X, position.Y),
                axis,
                side,
                kind,
                isFull,
                value,
                zone));

            if (IsFull)
                return;
        }
    }

    /// <summary>
    /// Whether a value would print on top of one already written. Both axes have to be close for it
    /// to count: two values far apart along an edge do not overlap however near they are to the same
    /// latitude.
    /// </summary>
    private bool IsTooCloseToAnythingAlreadyWritten(Point candidate)
    {
        foreach (var placed in _placed)
        {
            if (Math.Abs(placed.X - candidate.X) < _minSeparationX
                && Math.Abs(placed.Y - candidate.Y) < _minSeparationY)
            {
                return true;
            }
        }

        return false;
    }

    private static MapGridSide[] GetSides(MapGridAxis axis)
        => axis == MapGridAxis.X
            ? new[] { MapGridSide.Bottom, MapGridSide.Top }
            : new[] { MapGridSide.Left, MapGridSide.Right };

    private Point? FindCrossing(List<List<Point>> parts, MapGridAxis axis, MapGridSide side)
    {
        foreach (var part in parts)
        {
            var crossing = axis == MapGridAxis.X
                ? MapGridGeometry.InterpolateAtLatitude(part, side == MapGridSide.Bottom ? _bottomLatitude : _topLatitude)
                : MapGridGeometry.InterpolateAtLongitude(part, side == MapGridSide.Left ? _leftLongitude : _rightLongitude);

            if (crossing is not null)
                return crossing;
        }

        return null;
    }
}
