using IRI.Maptor.Infrastructure.Sqlite.MbTiles;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Helpers;

using Microsoft.Maui.Graphics;

using BoundingBox = IRI.Maptor.Core.Common.Primitives.BoundingBox;
using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Maui.Layers;

/// <summary>
/// One sub-layer of a vector (MVT/pbf) MBTiles file, drawn as vector geometry. Its
/// <see cref="MapLayer.Parts"/> are recomputed for the current view via the shared
/// <see cref="MbTilesVectorTileProvider"/> (one provider per file, shared by all its sub-layers).
/// Features come back already in WebMercator, so they are flattened with an identity transform.
/// </summary>
internal sealed class MbTilesVectorLayer : MapLayer
{
    // Coalesce the flurry of view updates produced during a pan/zoom into a single query.
    private const int DebounceMilliseconds = 150;

    private readonly MbTilesVectorDataSource _dataSource;
    private int _version;

    public MbTilesVectorLayer(MbTilesVectorTileProvider provider, MvtVectorLayerInfo info, Color color)
        : base(info.Id, color)
    {
        _dataSource = new MbTilesVectorDataSource(provider, info);

        Extent = provider.WebMercatorExtent;
        Description = $"{info.GeometryType?.ToString() ?? "Vector"} (MBTiles)";

        // Geometry-type-aware defaults, echoing the WPF MbTilesVectorSymbology.
        switch (info.GeometryType)
        {
            case GeometryType.LineString:
            case GeometryType.MultiLineString:
                StrokeWidth = 1.2;
                break;

            case GeometryType.Point:
            case GeometryType.MultiPoint:
                PointSize = 6;
                StrokeWidth = 1;
                break;

            default:
                StrokeWidth = 0.8;
                PointSize = 6;
                break;
        }
    }

    /// <summary>
    /// Requests a refresh of the drawn geometry for the given WebMercator view extent and zoom.
    /// Debounced and versioned: rapid calls during a gesture collapse to the latest one, and stale
    /// results are discarded. On success <paramref name="onUpdated"/> is invoked to redraw.
    /// </summary>
    public void RequestUpdate(BoundingBox extent, int zoom, Action onUpdated)
    {
        int version = Interlocked.Increment(ref _version);
        _ = UpdateAsync(extent, zoom, version, onUpdated);
    }

    private async Task UpdateAsync(BoundingBox extent, int zoom, int version, Action onUpdated)
    {
        try
        {
            await Task.Delay(DebounceMilliseconds).ConfigureAwait(false);

            if (version != Volatile.Read(ref _version))
            {
                return; // superseded by a newer request
            }

            double mapScale = WebMercatorUtility.GetGoogleMapScale(zoom);

            var featureSet = await _dataSource.GetAsFeatureSetAsync(mapScale, extent).ConfigureAwait(false);

            if (version != Volatile.Read(ref _version))
            {
                return;
            }

            var parts = new List<RenderPart>();

            foreach (var feature in featureSet.Features)
            {
                var geometry = feature.TheGeometry;

                if (geometry == null)
                {
                    continue;
                }

                RenderPartBuilder.Build(geometry, Identity, parts, NoTrack);
            }

            if (version != Volatile.Read(ref _version))
            {
                return;
            }

            Parts = parts;
            onUpdated?.Invoke();
        }
        catch
        {
            // A failed/cancelled update just leaves the previous geometry; a later view change retries.
        }
    }

    // Features are already WebMercator — no projection needed.
    private static (double X, double Y) Identity(Point p) => (p.X, p.Y);

    private static void NoTrack(double x, double y)
    {
    }
}
