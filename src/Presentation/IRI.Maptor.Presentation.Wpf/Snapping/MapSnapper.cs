using System;
using System.Collections.Generic;
using System.Windows.Input;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Presentation.Wpf.Layers;

namespace IRI.Maptor.Presentation.Wpf.Snapping;

/// <summary>
/// Turns the map point under the pointer into the point to use while drawing: snapped when snapping is on, Ctrl is not
/// held, and a vertex or edge of a checked layer is within <see cref="TolerancePixels"/>. It keeps the layer list and
/// the cache in step with the map; it does not load features (<see cref="MapSnapCache"/>) or draw (<see cref="SnapMarker"/>).
/// </summary>
public sealed class MapSnapper
{
    public const double TolerancePixels = 10;

    private readonly SnapLayerChoice _choice;
    private readonly MapSnapCache _cache = new();
    private readonly Func<BoundingBox> _currentExtent;
    private readonly Func<double, double> _screenToMapDistance;
    private readonly Func<IEnumerable<VectorLayer>> _snappableLayers;

    // one-entry memo: a mouse move asks for the same point twice (current point, then hover)
    private Point? _lastRaw;
    private SnapResult<Point>? _lastResult;

    /// <param name="choice">The user's choice (on/off, layers).</param>
    /// <param name="currentExtent">The map extent in Web Mercator.</param>
    /// <param name="screenToMapDistance">Pixels to Web Mercator units at the current scale.</param>
    /// <param name="snappableLayers">The vector layers that could be snapped to now (visible, in scale range).</param>
    public MapSnapper(SnapLayerChoice choice, Func<BoundingBox> currentExtent, Func<double, double> screenToMapDistance, Func<IEnumerable<VectorLayer>> snappableLayers)
    {
        _choice = choice ?? throw new ArgumentNullException(nameof(choice));
        _currentExtent = currentExtent;
        _screenToMapDistance = screenToMapDistance;
        _snappableLayers = snappableLayers;

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

    /// <summary>
    /// A new drawing starts: the layer list is refreshed and the cache rebuilt, so features saved since the last
    /// drawing are included.
    /// </summary>
    public void BeginSession()
    {
        _choice.Update(_snappableLayers());
        _cache.Invalidate();
        Forget();
    }

    /// <summary>The snap for <paramref name="mapPoint"/>, or null to use the point as it is.</summary>
    public SnapResult<Point>? Snap(Point mapPoint)
    {
        if (!_choice.IsEnabled || (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            return null;

        if (_lastRaw is { } last && last.X == mapPoint.X && last.Y == mapPoint.Y)
            return _lastResult;

        var extent = _currentExtent();

        // visibility and scale range change with zoom; Update does nothing when the list is unchanged
        _choice.Update(_snappableLayers());

        _cache.EnsureFor(extent, _choice.CheckedLayers);

        var index = _cache.Index;

        var result = index is null ? null : SnapEngine.Snap(index, mapPoint, _screenToMapDistance(TolerancePixels));

        _lastRaw = mapPoint;
        _lastResult = result;

        return result;
    }

    private void Forget()
    {
        _lastRaw = null;
        _lastResult = null;
    }
}
