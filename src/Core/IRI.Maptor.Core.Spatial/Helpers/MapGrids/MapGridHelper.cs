using System;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.Spatial.Helpers.MapGrids;

/// <summary>
/// Builds a map grid over a view: the lines, and the values written against the edges.
/// </summary>
/// <remarks>
/// <para>
/// The entry point for the whole engine. Hand it a Web Mercator extent and a
/// <see cref="MapGridDefinition"/> and it returns the grid — how fine, which lines, what is written
/// where. Several definitions may be built over the same view and drawn together; a geodetic
/// graticule and a UTM grid at once is the case this exists for.
/// </para>
/// <para>
/// <strong>Lines, not cells.</strong> Everything returned is a polyline. That is the deliberate
/// difference from <see cref="MgrsGridHelper"/>, which emits polygons because an MGRS square is a
/// named region; here a grid line is a line, and the numbering is a property of the line rather
/// than of a cell.
/// </para>
/// </remarks>
public static class MapGridHelper
{
    /// <summary>
    /// Web Mercator is undefined past about ±85.05°, where y runs to infinity. Every view is clipped
    /// to it before anything is generated.
    /// </summary>
    public const double MaxWebMercatorLatitude = 85.05;

    /// <summary>
    /// The grid over <paramref name="webMercatorExtent"/>.
    /// </summary>
    /// <param name="webMercatorExtent">The visible extent, in EPSG:3857.</param>
    /// <param name="definition">What kind of grid, and how it is labelled.</param>
    /// <param name="options">The engine's knobs; <see cref="MapGridOptions.Default"/> when omitted.</param>
    /// <returns>
    /// A grid, possibly empty — an extent that is NaN, degenerate, or entirely outside Web
    /// Mercator's latitude range yields <see cref="MapGrid.Empty"/> rather than an exception. A
    /// grid layer is asked for an extent on every pan; throwing there would be the wrong contract.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="definition"/> is null.</exception>
    public static MapGrid Create(BoundingBox webMercatorExtent, MapGridDefinition definition, MapGridOptions? options = null)
    {
        if (definition is null)
            throw new ArgumentNullException(nameof(definition));

        var theOptions = options ?? MapGridOptions.Default;

        if (TryGetCached(webMercatorExtent, definition, theOptions, out var cached))
            return cached!;

        var result = Build(webMercatorExtent, definition, theOptions);

        Cache(webMercatorExtent, definition, theOptions, result);

        return result;
    }

    /// <summary>
    /// The interval the grid would use for this view, in the definition's own units — degrees for a
    /// graticule, metres otherwise.
    /// </summary>
    public static double ChooseMajorInterval(BoundingBox webMercatorExtent, MapGridDefinition definition, MapGridOptions? options = null)
        => Create(webMercatorExtent, definition, options).MajorInterval;

    /// <summary>The view in geodetic degrees, clipped to what Web Mercator and the grid can represent.</summary>
    /// <returns><see cref="BoundingBox.NaN"/> when nothing survives the clip.</returns>
    public static BoundingBox ToClippedGeodetic(BoundingBox webMercatorExtent)
    {
        if (webMercatorExtent.IsNaN() || !webMercatorExtent.IsValid())
            return BoundingBox.NaN;

        var geodetic = webMercatorExtent.Transform(MapProjects.WebMercatorToGeodeticWgs84);

        if (geodetic.IsNaN())
            return BoundingBox.NaN;

        var west = Math.Max(geodetic.XMin, -180.0);
        var east = Math.Min(geodetic.XMax, 180.0);
        var south = Math.Max(geodetic.YMin, -MaxWebMercatorLatitude);
        var north = Math.Min(geodetic.YMax, MaxWebMercatorLatitude);

        if (west >= east || south >= north)
            return BoundingBox.NaN;

        return new BoundingBox(west, south, east, north);
    }

    private static MapGrid Build(BoundingBox webMercatorExtent, MapGridDefinition definition, MapGridOptions options)
    {
        var geodetic = ToClippedGeodetic(webMercatorExtent);

        if (geodetic.IsNaN())
            return MapGrid.Empty(definition);

        switch (definition.Kind)
        {
            case MapGridKind.Geodetic:
                return GeodeticGridScheme.Create(geodetic, definition, options);

            case MapGridKind.Utm:
                return UtmGridScheme.Create(geodetic, definition, options);

            case MapGridKind.Projected:
                return ProjectedGridScheme.Create(geodetic, definition, options);

            default:
                return MapGrid.Empty(definition);
        }
    }

    #region Cache

    // One entry, because the two data sources behind a grid layer — the lines and the labels — are
    // asked for the same extent one after the other, so this halves the work for free. Anything
    // larger would need eviction and would not pay for itself: a grid is cheap to build and the
    // extent changes on every pan.
    private static readonly object _cacheLock = new object();

    private static BoundingBox _cachedExtent = BoundingBox.NaN;

    private static MapGridDefinition? _cachedDefinition;

    private static MapGridOptions? _cachedOptions;

    private static double? _cachedInterval;

    private static MapGridSide _cachedSides;

    private static int _cachedTier;

    private static MapGrid? _cachedGrid;

    private static bool TryGetCached(BoundingBox extent, MapGridDefinition definition, MapGridOptions options, out MapGrid? grid)
    {
        lock (_cacheLock)
        {
            // The definition is compared by reference plus its mutable parts: a caller holds one
            // instance per grid and edits its interval or label sides in place, which must miss.
            var hit = _cachedGrid is not null
                && ReferenceEquals(_cachedDefinition, definition)
                && ReferenceEquals(_cachedOptions, options)
                && _cachedInterval == definition.MajorInterval
                && _cachedSides == definition.LabelSides
                && _cachedTier == definition.LabelTier
                && !extent.IsNaN()
                && _cachedExtent == extent;

            grid = hit ? _cachedGrid : null;

            return hit;
        }
    }

    private static void Cache(BoundingBox extent, MapGridDefinition definition, MapGridOptions options, MapGrid grid)
    {
        lock (_cacheLock)
        {
            _cachedExtent = extent;
            _cachedDefinition = definition;
            _cachedOptions = options;
            _cachedInterval = definition.MajorInterval;
            _cachedSides = definition.LabelSides;
            _cachedTier = definition.LabelTier;
            _cachedGrid = grid;
        }
    }

    /// <summary>
    /// Drops the cached grid. Only tests need this — production callers cannot observe the cache,
    /// because a hit is by construction the same answer a miss would compute.
    /// </summary>
    public static void ClearCache()
    {
        lock (_cacheLock)
        {
            _cachedGrid = null;
            _cachedDefinition = null;
            _cachedOptions = null;
            _cachedExtent = BoundingBox.NaN;
        }
    }

    #endregion
}
