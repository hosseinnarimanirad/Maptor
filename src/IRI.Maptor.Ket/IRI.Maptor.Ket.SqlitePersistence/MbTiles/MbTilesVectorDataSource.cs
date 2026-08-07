using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.IO.VectorTiles;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Persistence.Model;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Ket.SqlitePersistence.MbTiles;

/// <summary>
/// Exposes a single layer of a vector MBTiles file as an <see cref="IVectorDataSource"/> in Web
/// Mercator. On each extent/scale request it picks the closest available zoom, gathers the visible
/// tiles from the shared <see cref="MbTilesVectorTileProvider"/>, decodes only the matching MVT
/// layer and returns the features. Several instances (one per MVT layer) share one provider, so a
/// physical tile is read and decoded once per extent.
/// </summary>
public class MbTilesVectorDataSource : VectorDataSource
{
    private readonly MbTilesVectorTileProvider _provider;
    private readonly string _layerName;

    // Safety cap so a pathological extent/zoom combination cannot request an unbounded tile grid.
    private const int MaxTilesPerQuery = 4096;

    public override int Srid => SridHelper.WebMercator;

    public override DataSourceKind DataSourceKind => DataSourceKind.MBTiles;

    public override SourceLocation? Location => new FileLocation { Path = _provider.FilePath, TableName = _layerName };

    public MbTilesVectorDataSource(MbTilesVectorTileProvider provider, MvtVectorLayerInfo info)
        : base(info.Fields ?? new List<Field>())
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _layerName = info.Id;

        GeometryType = info.GeometryType;
        WebMercatorExtent = provider.WebMercatorExtent;
        IsLoaded = true;
    }

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(double mapScale, BoundingBox boundingBox) =>
        Task.Run(() => BuildFeatureSet(mapScale, boundingBox));

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox) =>
        GetAsFeatureSetAsync(double.NaN, boundingBox);

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry) =>
        Task.FromResult(FeatureSet<Point>.Empty);

    public override Task<FeatureSet<Point>> SearchAsync(string searchText) =>
        Task.FromResult(FeatureSet<Point>.Empty);

    private FeatureSet<Point> BuildFeatureSet(double mapScale, BoundingBox webMercatorExtent)
    {
        if (_provider.AvailableZoomLevels == null || _provider.AvailableZoomLevels.Count == 0 ||
            webMercatorExtent.IsNaN())
            return FeatureSet<Point>.Empty;

        int requestedZoom = double.IsNaN(mapScale)
            ? _provider.AvailableZoomLevels.Max()
            : WebMercatorUtility.GetZoomLevel(mapScale);

        int zoom = ClosestAvailableZoom(requestedZoom);

        var features = new List<Feature<Point>>();

        foreach (var (column, row) in EnumerateVisibleTiles(webMercatorExtent, zoom))
        {
            try
            {
                var tile = _provider.GetDecodedTile(zoom, column, row);

                var layer = tile?.Layers.FirstOrDefault(l => l.Name == _layerName);
                if (layer == null)
                    continue;

                var toPoint = MvtTileTransform.LocalToWebMercator(zoom, column, row, layer.Extent);

                foreach (var mvtFeature in layer.Features)
                {
                    var geometry = MvtGeometryDecoder.ToGeometry(mvtFeature, toPoint, SridHelper.WebMercator);

                    if (geometry == null || geometry.IsNullOrEmpty())
                        continue;

                    features.Add(new Feature<Point>(geometry, ToAttributeDictionary(mvtFeature)));
                }
            }
            catch (Exception ex)
            {
                // A single malformed tile must not blank the whole layer.
                System.Diagnostics.Trace.WriteLine(
                    $"MbTilesVector: failed to decode tile z{zoom}/{column}/{row} for layer '{_layerName}': {ex.Message}");
            }
        }

        System.Diagnostics.Trace.WriteLine(
            $"MbTilesVector: layer '{_layerName}' z{zoom} produced {features.Count} features");

        return FeatureSet<Point>.Create(_layerName, features);
    }

    private IEnumerable<(int Column, int Row)> EnumerateVisibleTiles(BoundingBox webMercatorExtent, int zoom)
    {
        int tileCount = 1 << zoom;
        double max = MvtTileTransform.MaxExtent;
        double tileSpan = (2.0 * max) / tileCount;

        int columnMin = Clamp((int)Math.Floor((webMercatorExtent.XMin + max) / tileSpan), tileCount);
        int columnMax = Clamp((int)Math.Floor((webMercatorExtent.XMax + max) / tileSpan), tileCount);

        // Web Mercator Y increases northward; XYZ rows increase southward (origin at top).
        int rowMin = Clamp((int)Math.Floor((max - webMercatorExtent.YMax) / tileSpan), tileCount);
        int rowMax = Clamp((int)Math.Floor((max - webMercatorExtent.YMin) / tileSpan), tileCount);

        int emitted = 0;

        for (int column = columnMin; column <= columnMax; column++)
        {
            for (int row = rowMin; row <= rowMax; row++)
            {
                if (emitted++ >= MaxTilesPerQuery)
                    yield break;

                yield return (column, row);
            }
        }
    }

    private int ClosestAvailableZoom(int requestedZoom)
    {
        var levels = _provider.AvailableZoomLevels;

        if (levels == null || levels.Count == 0)
            return requestedZoom;

        return levels.OrderBy(z => Math.Abs(z - requestedZoom)).First();
    }

    private static int Clamp(int value, int tileCount)
    {
        if (value < 0)
            return 0;

        int maxIndex = tileCount - 1;
        return value > maxIndex ? maxIndex : value;
    }

    private static Dictionary<string, object> ToAttributeDictionary(MvtFeature feature)
    {
        var result = new Dictionary<string, object>();

        foreach (var pair in feature.Attributes)
        {
            if (pair.Value != null)
                result[pair.Key] = pair.Value;
        }

        return result;
    }
}
