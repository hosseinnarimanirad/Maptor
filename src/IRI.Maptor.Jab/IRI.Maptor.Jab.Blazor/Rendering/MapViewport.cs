using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Model;

namespace IRI.Maptor.Jab.Blazor.Rendering;

/// <summary>
/// Plain, platform-neutral pan/zoom viewport state and screen&lt;-&gt;WebMercator pixel math.
/// Deliberately free of any Blazor/JS types so it can move into Jab.Core later if a second
/// web-facing rendering backend ever needs the same math (see Jab CLAUDE.md on WPF/MAUI drift).
/// </summary>
public sealed class MapViewport
{
    private const int MinZoomLevel = 2;
    private const int MaxZoomLevel = 19;

    public double CenterLongitude { get; private set; }
    public double CenterLatitude { get; private set; }
    public int ZoomLevel { get; private set; }
    public double ScreenWidth { get; private set; } = 1;
    public double ScreenHeight { get; private set; } = 1;

    public MapViewport(double centerLongitude, double centerLatitude, int zoomLevel)
    {
        CenterLongitude = centerLongitude;
        CenterLatitude = centerLatitude;
        ZoomLevel = Math.Clamp(zoomLevel, MinZoomLevel, MaxZoomLevel);
    }

    public void Resize(double screenWidth, double screenHeight)
    {
        ScreenWidth = Math.Max(1, screenWidth);
        ScreenHeight = Math.Max(1, screenHeight);
    }

    /// <summary>Pans the map by a pixel delta as dragged on screen (screen Y grows downward).</summary>
    public void PanByPixels(double dxPixels, double dyPixels)
    {
        var centerWm = ToWebMercator(CenterLongitude, CenterLatitude);

        var newWmX = centerWm.X - WebMercatorUtility.ToWebMercatorLength(ZoomLevel, dxPixels);
        var newWmY = centerWm.Y + WebMercatorUtility.ToWebMercatorLength(ZoomLevel, dyPixels);

        var newCenter = MapProjects.WebMercatorToGeodeticWgs84(new Point(newWmX, newWmY));

        CenterLongitude = newCenter.X;
        CenterLatitude = newCenter.Y;
    }

    /// <summary>Zooms in/out by <paramref name="levelDelta"/>, keeping the map point under
    /// (pivotScreenX, pivotScreenY) stationary on screen — matches the feel of every mainstream
    /// web map (scroll-wheel zoom anchored at the cursor, not the viewport center).</summary>
    public void ZoomAtScreenPoint(int levelDelta, double pivotScreenX, double pivotScreenY)
    {
        var newLevel = Math.Clamp(ZoomLevel + levelDelta, MinZoomLevel, MaxZoomLevel);
        if (newLevel == ZoomLevel) return;

        var pivotWm = ScreenToWebMercator(pivotScreenX, pivotScreenY);

        ZoomLevel = newLevel;

        // Re-anchor: after changing zoom, shift the center so the same map point is still
        // under the cursor.
        var pivotScreenAfter = WebMercatorToScreen(pivotWm);
        PanByPixels(pivotScreenAfter.x - pivotScreenX, pivotScreenAfter.y - pivotScreenY);
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

    private BoundingBox GetVisibleWebMercatorExtent()
    {
        var centerWm = ToWebMercator(CenterLongitude, CenterLatitude);

        var halfWidthWm = WebMercatorUtility.ToWebMercatorLength(ZoomLevel, ScreenWidth / 2.0);
        var halfHeightWm = WebMercatorUtility.ToWebMercatorLength(ZoomLevel, ScreenHeight / 2.0);

        return new BoundingBox(
            centerWm.X - halfWidthWm,
            centerWm.Y - halfHeightWm,
            centerWm.X + halfWidthWm,
            centerWm.Y + halfHeightWm);
    }

    private (double x, double y) WebMercatorToScreen(Point webMercatorPoint)
    {
        var centerWm = ToWebMercator(CenterLongitude, CenterLatitude);

        var dx = WebMercatorUtility.ToScreenLength(ZoomLevel, webMercatorPoint.X - centerWm.X);
        var dy = WebMercatorUtility.ToScreenLength(ZoomLevel, centerWm.Y - webMercatorPoint.Y); // Y flips: north is up

        return (ScreenWidth / 2.0 + dx, ScreenHeight / 2.0 + dy);
    }

    private Point ScreenToWebMercator(double screenX, double screenY)
    {
        var centerWm = ToWebMercator(CenterLongitude, CenterLatitude);

        var dxWm = WebMercatorUtility.ToWebMercatorLength(ZoomLevel, screenX - ScreenWidth / 2.0);
        var dyWm = WebMercatorUtility.ToWebMercatorLength(ZoomLevel, screenY - ScreenHeight / 2.0);

        return new Point(centerWm.X + dxWm, centerWm.Y - dyWm);
    }

    private static Point ToWebMercator(double longitude, double latitude) =>
        MapProjects.GeodeticWgs84ToWebMercator(new Point(longitude, latitude));
}
