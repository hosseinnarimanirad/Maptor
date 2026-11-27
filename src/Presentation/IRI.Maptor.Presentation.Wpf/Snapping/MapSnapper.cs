using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf.Layers;

namespace IRI.Maptor.Presentation.Wpf.Snapping;

/// <summary>
/// Turns the map point under the pointer into the point to use while drawing: snapped when snapping is on, Ctrl is not
/// held, and a vertex or edge of a checked layer is within the snap distance (map settings, in pixels). It keeps the
/// layer list and the cache in step with the map; it does not load features (<see cref="MapSnapCache"/>) or draw
/// (<see cref="SnapMarker"/>).
/// </summary>
public sealed class MapSnapper
{
    /// <summary>The snap distance when no setting is given.</summary>
    public const double DefaultTolerancePixels = 10;

    private readonly SnapLayerChoice _choice;
    private readonly MapSnapCache _cache = new();
    private readonly Func<BoundingBox> _currentExtent;
    private readonly Func<double, double> _screenToMapDistance;
    private readonly Func<IEnumerable<VectorLayer>> _snappableLayers;
    private readonly Func<double> _tolerancePixels;
    private readonly Func<SnapOptions> _options;
    private readonly Func<IEnumerable<DrawingItemLayer>> _drawings;

    // one-entry memo: a mouse move asks for the same point twice (current point, then hover); a change of distance or
    // kinds is part of the key, so a new setting applies at once
    private Point? _lastRaw;
    private (double Tolerance, bool Ends, bool Vertices, bool Intersections, bool Midpoints, bool Edges) _lastSettings;
    private SnapResult<Point>? _lastResult;

    /// <param name="choice">The user's choice (on/off, layers).</param>
    /// <param name="currentExtent">The map extent in Web Mercator.</param>
    /// <param name="screenToMapDistance">Pixels to Web Mercator units at the current scale.</param>
    /// <param name="snappableLayers">The vector layers that could be snapped to now (visible, in scale range).</param>
    /// <param name="tolerancePixels">The snap distance in pixels, read at each snap; <see cref="DefaultTolerancePixels"/> when null.</param>
    /// <param name="options">The kinds to snap to, read at each snap; all when null.</param>
    /// <param name="drawings">
    /// The drawings that could be snapped to now (visible, not text labels); none when null. They are used while the
    /// choice's "Drawings" entry is checked.
    /// </param>
    public MapSnapper(
        SnapLayerChoice choice,
        Func<BoundingBox> currentExtent,
        Func<double, double> screenToMapDistance,
        Func<IEnumerable<VectorLayer>> snappableLayers,
        Func<double>? tolerancePixels = null,
        Func<SnapOptions>? options = null,
        Func<IEnumerable<DrawingItemLayer>>? drawings = null)
    {
        _choice = choice ?? throw new ArgumentNullException(nameof(choice));
        _currentExtent = currentExtent;
        _screenToMapDistance = screenToMapDistance;
        _snappableLayers = snappableLayers;
        _tolerancePixels = tolerancePixels ?? (() => DefaultTolerancePixels);
        _options = options ?? (() => SnapOptions.Default);
        _drawings = drawings ?? (() => Array.Empty<DrawingItemLayer>());

        _choice.Changed += () =>
        {
            _cache.Invalidate();
            Forget();
        };

        _cache.Built += () =>
        {
            _choice.IsOverLimit = _cache.IsOverLimit;
            Forget();
        };
    }

    // the geometry being edited (its stored copy in the layer is left out) and the positions in the current index
    // that hold that copy, worked out once per index
    private Geometry<Point>? _edited;
    private SnapIndex<Point>? _excludedFor;
    private HashSet<int>? _excluded;

    /// <summary>
    /// A new drawing or edit starts: the layer list is refreshed and the cache rebuilt, so features saved since the
    /// last session are included.
    /// </summary>
    /// <param name="edited">
    /// When editing, the geometry as it was handed to the edit: its stored copy in the layer is not snapped to, or
    /// the old position of a dragged vertex would pull it back. A copy is recognised by having the same points.
    /// </param>
    public void BeginSession(Geometry<Point>? edited = null)
    {
        _edited = edited;
        _excludedFor = null;
        _excluded = null;

        _choice.Update(_snappableLayers());
        _cache.Invalidate();
        Forget();
    }

    /// <summary>The snap for <paramref name="mapPoint"/>, or null to use the point as it is.</summary>
    public SnapResult<Point>? Snap(Point mapPoint)
    {
        if (!_choice.IsEnabled || (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            return null;

        var options = _options();
        var settings = (_tolerancePixels(), options.SnapToEnds, options.SnapToVertices, options.SnapToIntersections, options.SnapToMidpoints, options.SnapToEdges);

        if (_lastRaw is { } last && last.X == mapPoint.X && last.Y == mapPoint.Y && settings == _lastSettings)
            return _lastResult;

        var extent = _currentExtent();

        // visibility and scale range change with zoom; Update does nothing when the list is unchanged
        _choice.Update(_snappableLayers());

        _cache.EnsureFor(extent, _choice.CheckedLayers, _choice.IsDrawingsChecked ? _drawings().ToList() : null);

        var index = _cache.Index;

        var result = index is null ? null : SnapEngine.Snap(index, mapPoint, _screenToMapDistance(settings.Item1), options, Accept(index));

        _lastRaw = mapPoint;
        _lastSettings = settings;
        _lastResult = result;

        return result;
    }

    /// <summary>
    /// The layer the snapped geometry belongs to (for a drawing, its <see cref="DrawingItemLayer"/>), or null when
    /// <paramref name="snap"/> is not from the current index.
    /// </summary>
    public VectorLayer? SourceOf(SnapResult<Point> snap)
    {
        var sources = _cache.Sources;

        return snap is not null && snap.GeometryIndex >= 0 && snap.GeometryIndex < sources.Count ? sources[snap.GeometryIndex] : null;
    }

    private Func<int, bool>? Accept(SnapIndex<Point> index)
    {
        if (_edited is null)
            return null;

        if (!ReferenceEquals(_excludedFor, index))
        {
            _excludedFor = index;
            _excluded = CopiesOf(_edited, _cache.Geometries);
        }

        var excluded = _excluded!;

        return excluded.Count == 0 ? null : g => !excluded.Contains(g);
    }

    /// <summary>The positions in <paramref name="geometries"/> that hold the same points as <paramref name="edited"/>.</summary>
    private static HashSet<int> CopiesOf(Geometry<Point> edited, IReadOnlyList<Geometry<Point>?> geometries)
    {
        var result = new HashSet<int>();

        var points = edited.GetAllPoints();

        if (points is null || points.Count == 0)
            return result;

        var box = edited.GetBoundingBox();

        for (int g = 0; g < geometries.Count; g++)
        {
            var candidate = geometries[g];

            if (candidate is null)
                continue;

            if (ReferenceEquals(candidate, edited))
            {
                result.Add(g);
                continue;
            }

            if (candidate.Type != edited.Type || !candidate.GetBoundingBox().Equals(box))
                continue;

            var others = candidate.GetAllPoints();

            if (others is null || others.Count != points.Count)
                continue;

            var same = true;

            for (int i = 0; i < points.Count && same; i++)
                same = points[i].X == others[i].X && points[i].Y == others[i].Y;

            if (same)
                result.Add(g);
        }

        return result;
    }

    private void Forget()
    {
        _lastRaw = null;
        _lastResult = null;
    }
}
