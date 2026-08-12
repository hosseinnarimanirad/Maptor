using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Model;

namespace IRI.Maptor.Jab.Blazor.Rendering;

/// <summary>
/// Plain, platform-neutral pan/zoom viewport state and screen&lt;-&gt;WebMercator pixel math.
/// Deliberately free of any Blazor/JS types so it can move into Jab.Core later if a second
/// web-facing rendering backend ever needs the same math (see Jab CLAUDE.md on WPF/MAUI drift).
///
/// <para>The center is held in Web Mercator and the pixels-per-map-unit factor is cached per zoom
/// level, because <see cref="WebMercatorToScreen"/> runs once per vertex of every drawn feature.
/// Re-deriving either inside that call — a geodetic projection with its logs and tangents, plus a
/// <c>Math.Pow</c> for the level — is a per-vertex cost that WASM cannot absorb.</para>
/// </summary>
public sealed class MapViewport
{
    private const int MinZoomLevel = 2;
    private const int MaxZoomLevel = 19;

    /// <summary>Level used when framing something with no extent — a point feature, or one smaller
    /// than a pixel. Close enough to identify the site, far enough to keep its surroundings.</summary>
    private const int DegenerateExtentZoomLevel = 16;

    private Point _centerWm;
    private double _pixelsPerMapUnit;

    public double CenterLongitude { get; private set; }
    public double CenterLatitude { get; private set; }
    public int ZoomLevel { get; private set; }
    public double ScreenWidth { get; private set; } = 1;
    public double ScreenHeight { get; private set; } = 1;

    public MapViewport(double centerLongitude, double centerLatitude, int zoomLevel)
    {
        CenterLongitude = centerLongitude;
        CenterLatitude = centerLatitude;
        _centerWm = ToWebMercator(centerLongitude, centerLatitude);

        ZoomLevel = Math.Clamp(zoomLevel, MinZoomLevel, MaxZoomLevel);
        _pixelsPerMapUnit = WebMercatorUtility.ToScreenLength(ZoomLevel, 1);
    }

    public void Resize(double screenWidth, double screenHeight)
    {
        ScreenWidth = Math.Max(1, screenWidth);
        ScreenHeight = Math.Max(1, screenHeight);
    }

    /// <summary>Pans the map by a pixel delta as dragged on screen (screen Y grows downward).</summary>
    public void PanByPixels(double dxPixels, double dyPixels)
    {
        SetCenter(new Point(
            _centerWm.X - dxPixels / _pixelsPerMapUnit,
            _centerWm.Y + dyPixels / _pixelsPerMapUnit));
    }

    /// <summary>Zooms in/out by <paramref name="levelDelta"/>, keeping the map point under
    /// (pivotScreenX, pivotScreenY) stationary on screen — matches the feel of every mainstream
    /// web map (scroll-wheel zoom anchored at the cursor, not the viewport center), and the
    /// WPF MapViewer's ZoomInPlaceAtWindowPoint.</summary>
    public void ZoomAtScreenPoint(int levelDelta, double pivotScreenX, double pivotScreenY)
    {
        var newLevel = Math.Clamp(ZoomLevel + levelDelta, MinZoomLevel, MaxZoomLevel);
        if (newLevel == ZoomLevel) return;

        // Capture the map coordinate under the cursor BEFORE the level changes.
        var pivotWm = ScreenToWebMercator(pivotScreenX, pivotScreenY);

        SetZoomLevel(newLevel);

        // Solve for the center that puts pivotWm back on the very same pixel at the new level, by
        // inverting WebMercatorToScreen at (pivotScreenX, pivotScreenY). Screen Y grows downward,
        // hence the sign flip on the second component.
        SetCenter(new Point(
            pivotWm.X - (pivotScreenX - ScreenWidth / 2.0) / _pixelsPerMapUnit,
            pivotWm.Y + (pivotScreenY - ScreenHeight / 2.0) / _pixelsPerMapUnit));
    }

    /// <summary>
    /// Frames <paramref name="extentWm"/>: centres on it and picks the deepest zoom level at which
    /// it still fits inside the screen, less <paramref name="paddingPixels"/> on each side.
    ///
    /// <para>Snapped to the level grid rather than scaled freely, unlike the WPF MapViewer's
    /// ZoomToExtent — the raster basemap only has tiles at whole levels, so a free scale would
    /// resample every tile. Returns false when there is nothing to frame.</para>
    /// </summary>
    public bool ZoomToExtent(BoundingBox extentWm, double paddingPixels = 48)
    {
        if (extentWm.IsNaN())
            return false;

        var availableWidth = Math.Max(1, ScreenWidth - 2 * paddingPixels);
        var availableHeight = Math.Max(1, ScreenHeight - 2 * paddingPixels);

        // A single point (or a feature smaller than a pixel) has no extent to fit, so fitting would
        // run to MaxZoomLevel and leave the user staring at a rooftop with no context around it.
        var level = extentWm.Width <= 0 || extentWm.Height <= 0
            ? DegenerateExtentZoomLevel
            : FindDeepestFittingLevel(extentWm, availableWidth, availableHeight);

        SetZoomLevel(Math.Clamp(level, MinZoomLevel, MaxZoomLevel));

        SetCenter(new Point(
            (extentWm.XMin + extentWm.XMax) / 2.0,
            (extentWm.YMin + extentWm.YMax) / 2.0));

        return true;
    }

    private static int FindDeepestFittingLevel(BoundingBox extentWm, double availableWidth, double availableHeight)
    {
        for (var level = MaxZoomLevel; level > MinZoomLevel; level--)
        {
            var pixelsPerMapUnit = WebMercatorUtility.ToScreenLength(level, 1);

            if (extentWm.Width * pixelsPerMapUnit <= availableWidth
                && extentWm.Height * pixelsPerMapUnit <= availableHeight)
            {
                return level;
            }
        }

        return MinZoomLevel;
    }

    public List<TileInfo> GetVisibleTiles()
    {
        var extent = GetVisibleWebMercatorExtent();

        return WebMercatorUtility.WebMercatorBoundingBoxToGoogleTileRegions(extent, ZoomLevel);
    }

    /// <summary>Destination rect, in on-screen pixels, for the given tile's image.</summary>
    public (double x, double y, double width, double height) GetScreenRect(TileInfo tile)
    {
        var topLeft = WebMercatorToScreen(tile.WebMercatorExtent.TopLeft);
        var bottomRight = WebMercatorToScreen(tile.WebMercatorExtent.BottomRight);

        return (topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y);
    }

    /// <summary>
    /// The map area currently on screen. Public because vector drawing culls against it: paths
    /// fully outside the view must never be projected, let alone handed to the canvas.
    /// </summary>
    public BoundingBox GetVisibleWebMercatorExtent()
    {
        var halfWidthWm = ScreenWidth / 2.0 / _pixelsPerMapUnit;
        var halfHeightWm = ScreenHeight / 2.0 / _pixelsPerMapUnit;

        return new BoundingBox(
            _centerWm.X - halfWidthWm,
            _centerWm.Y - halfHeightWm,
            _centerWm.X + halfWidthWm,
            _centerWm.Y + halfHeightWm);
    }

    /// <summary>
    /// Projects a Web Mercator point to on-screen pixels. Hot path — called once per vertex of
    /// every feature drawn, so it is deliberately nothing but four arithmetic ops.
    /// </summary>
    public (double x, double y) WebMercatorToScreen(Point webMercatorPoint) =>
        (ScreenWidth / 2.0 + (webMercatorPoint.X - _centerWm.X) * _pixelsPerMapUnit,
         ScreenHeight / 2.0 + (_centerWm.Y - webMercatorPoint.Y) * _pixelsPerMapUnit); // Y flips: north is up

    /// <summary>Inverse of <see cref="WebMercatorToScreen"/> — used for hit-testing a click.</summary>
    public Point ScreenToWebMercator(double screenX, double screenY) =>
        new(_centerWm.X + (screenX - ScreenWidth / 2.0) / _pixelsPerMapUnit,
            _centerWm.Y - (screenY - ScreenHeight / 2.0) / _pixelsPerMapUnit);

    /// <summary>Pixel length on screen of one Web Mercator unit at the current level. Lets callers
    /// convert a pixel tolerance to map units without re-deriving the level's scale.</summary>
    public double MapUnitsPerPixel => 1.0 / _pixelsPerMapUnit;

    private void SetZoomLevel(int level)
    {
        ZoomLevel = level;
        _pixelsPerMapUnit = WebMercatorUtility.ToScreenLength(level, 1);
    }

    /// <summary>The one place the center moves. Geodetic lon/lat is derived here — once per
    /// gesture — rather than re-projected on every use of the center.</summary>
    private void SetCenter(Point centerWm)
    {
        _centerWm = centerWm;

        var geodetic = MapProjects.WebMercatorToGeodeticWgs84(centerWm);

        CenterLongitude = geodetic.X;
        CenterLatitude = geodetic.Y;
    }

    private static Point ToWebMercator(double longitude, double latitude) =>
        MapProjects.GeodeticWgs84ToWebMercator(new Point(longitude, latitude));
}
