using IRI.Maptor.Core.Spatial.Helpers.MapGrids;

namespace IRI.Maptor.Presentation.Wpf.Layers;

/// <summary>
/// How one grid is drawn. One hue per grid, so two on the map at once can be told apart at a
/// glance — which is the whole reason several may be switched on together.
/// </summary>
/// <remarks>
/// <para>
/// The three weights are the cartographic convention: a principal line that carries a number, a
/// lighter subdivision that does not, and — for UTM only — a heavier meridian where the grid
/// restarts. They are drawn from one layer by three filtered symbolizers rather than three layers,
/// because the renderer applies each symbolizer's filter to the same feature set.
/// </para>
/// <para>
/// These values are a starting point, to be tuned against a real basemap in step 4 of
/// <c>docs/future-improvements/map-grids.md</c>. The MGRS overlay keeps its own warm red and is
/// deliberately not one of these.
/// </para>
/// </remarks>
public sealed record MapGridStyle(
    string Hex,
    double MajorThickness = 1.2,
    double MinorThickness = 0.7,
    double SeamThickness = 2.0,
    double MajorOpacity = 0.85,
    double MinorOpacity = 0.5,
    double SeamOpacity = 0.9,
    int FontSize = 12)
{
    /// <summary>A chart blue, the colour a printed sheet gives its graticule.</summary>
    public static MapGridStyle Geodetic { get; } = new MapGridStyle("#3060C0");

    /// <summary>Near-black: a UTM grid is the one you measure against, so it reads as ink.</summary>
    public static MapGridStyle Utm { get; } = new MapGridStyle("#262626");

    /// <summary>Purple, for any named or custom projected grid — clearly neither of the other two.</summary>
    public static MapGridStyle Projected { get; } = new MapGridStyle("#7A3E9D");

    public static MapGridStyle For(MapGridDefinition definition) => definition.Kind switch
    {
        MapGridKind.Geodetic => Geodetic,
        MapGridKind.Utm => Utm,
        _ => Projected,
    };
}
