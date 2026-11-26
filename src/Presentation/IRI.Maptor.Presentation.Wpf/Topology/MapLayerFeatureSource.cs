using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.Topology.Checking;
using IRI.Maptor.Presentation.Wpf.Layers;

namespace IRI.Maptor.Presentation.Wpf.Topology;

/// <summary>
/// One or more map layers holding one entity (e.g. towers split by region) as a feature source for topology checks.
/// <list type="bullet">
/// <item>Features are returned in <b>geodetic WGS84</b>: the map works in Web Mercator, whose units are not ground
/// metres, and the checks refuse it. Copies are returned; the layer's own features are never changed. A feature shown by
/// two of the layers is returned once (by id).</item>
/// <item>The layer is read on the thread that created this source (the UI thread), because map layers are not
/// thread-safe; a check may ask from a worker thread.</item>
/// <item><see cref="IsLive"/> tells whether the layer holds only what is in view (Live mode), so a panel can offer
/// only partial scopes; <see cref="WatchChanges"/> tells when results may be out of date.</item>
/// </list>
/// </summary>
public sealed class MapLayerFeatureSource : ITopologyFeatureSource
{
    private readonly IReadOnlyList<VectorLayer> _layers;
    private readonly Dispatcher _dispatcher;

    /// <param name="layer">The map layer.</param>
    /// <param name="entityName">The layer's name as the rule set names it (its server entity name).</param>
    public MapLayerFeatureSource(VectorLayer layer, string entityName)
        : this(new[] { layer ?? throw new ArgumentNullException(nameof(layer)) }, entityName)
    {
    }

    /// <param name="layers">The map layers that hold the entity; at least one.</param>
    /// <param name="entityName">The entity's name as the rule set names it.</param>
    public MapLayerFeatureSource(IEnumerable<VectorLayer> layers, string entityName)
    {
        _layers = (layers ?? throw new ArgumentNullException(nameof(layers))).Where(l => l is not null).Distinct().ToList();

        if (_layers.Count == 0)
            throw new ArgumentException("Give at least one map layer.", nameof(layers));

        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("Name the layer as the rule set does.", nameof(entityName));

        EntityName = entityName;
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    public string EntityName { get; }

    public int Srid => SridHelper.GeodeticWGS84;

    /// <summary>The map layers; different objects mean different (reloaded) layers.</summary>
    public IReadOnlyList<VectorLayer> Layers => _layers;

    /// <summary>The first layer object; a different object means a different (reloaded) layer.</summary>
    public object Identity => _layers[0];

    public string LayerName => string.Join(", ", _layers.Select(l => l.LayerName));

    /// <summary>True when a layer holds only what is in view, so a whole-layer check would check the screen.</summary>
    public bool IsLive => _layers.Any(IsLiveLayer);

    public static bool IsLiveLayer(VectorLayer layer) => layer.DataSource is IServerQueryableVectorDataSource { CanQueryServer: true };

    public async Task<IReadOnlyList<Feature<Point>>> GetFeaturesAsync(BoundingBox? box, IReadOnlyCollection<string> fields, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = box is null ? null : WebMercatorRectangle(box.Value);

        var features = await ReadAsync(query).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        return features.Select(ToGeodetic).ToList();
    }

    public async Task<IReadOnlyList<Feature<Point>>> GetFeaturesByIdsAsync(IReadOnlyCollection<long> ids, IReadOnlyCollection<string> fields, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var wanted = new HashSet<long>(ids ?? throw new ArgumentNullException(nameof(ids)));

        var features = await ReadAsync(null).ConfigureAwait(false);

        return features.Where(f => wanted.Contains(f.Id)).Select(ToGeodetic).ToList();
    }

    /// <summary>
    /// Calls <paramref name="onChanged"/> when the layer's data is reloaded or edited, until disposed. A reload flips
    /// <see cref="IDataSource.IsLoaded"/>; an edit flips <see cref="IDataSource.HasPendingChanges"/> when it starts and
    /// again when it is saved.
    /// </summary>
    public IDisposable WatchChanges(Action onChanged)
    {
        var dataSources = _layers.Select(l => l.DataSource).Where(d => d is not null).ToList();

        if (dataSources.Count == 0 || onChanged is null)
            return Subscription.None;

        EventHandler<bool> handler = (_, _) => onChanged();

        foreach (var dataSource in dataSources)
        {
            dataSource!.IsLoadedChanged += handler;
            dataSource.HasPendingChangesChanged += handler;
        }

        return new Subscription(() =>
        {
            foreach (var dataSource in dataSources)
            {
                dataSource!.IsLoadedChanged -= handler;
                dataSource.HasPendingChangesChanged -= handler;
            }
        });
    }

    public override string ToString() => $"{LayerName} as {EntityName} ({(IsLive ? "Live" : "Cache")})";

    private async Task<IReadOnlyList<Feature<Point>>> ReadAsync(Geometry<Point>? webMercatorQuery)
    {
        async Task<IReadOnlyList<Feature<Point>>> Read()
        {
            var result = new List<Feature<Point>>();
            var seen = new HashSet<int>();

            foreach (var layer in _layers)
            {
                var set = await layer.GetFeaturesAsync(webMercatorQuery);

                foreach (var feature in set?.Features ?? Enumerable.Empty<Feature<Point>>())
                {
                    if (_layers.Count == 1 || seen.Add(feature.Id))
                        result.Add(feature);
                }
            }

            return result;
        }

        if (_dispatcher.CheckAccess())
            return await Read();

        return await _dispatcher.InvokeAsync(Read).Task.Unwrap().ConfigureAwait(false);
    }

    /// <summary>A copy of the feature with its geometry in geodetic WGS84.</summary>
    private Feature<Point> ToGeodetic(Feature<Point> feature)
    {
        var geometry = feature.TheGeometry;

        Geometry<Point>? geodetic = geometry;

        if (geometry is not null && geometry.Srid != SridHelper.GeodeticWGS84)
        {
            if (geometry.Srid != SridHelper.WebMercator && geometry.Srid != SridHelper.EsriWebMercator && geometry.Srid != 0)
                throw new NotSupportedException($"Layer '{LayerName}' has a geometry in SRID {geometry.Srid}; only Web Mercator and WGS84 map layers can be checked.");

            // The map's geometries are Web Mercator even when their SRID was left at 0.
            geodetic = geometry.Transform(MapProjects.WebMercatorToGeodeticWgs84, SridHelper.GeodeticWGS84);
        }

        return new Feature<Point>(geodetic!, feature.Attributes ?? new Dictionary<string, object>()) { Id = feature.Id };
    }

    private static Geometry<Point> WebMercatorRectangle(BoundingBox geodetic)
    {
        var corners = new[]
        {
            new Point(geodetic.XMin, geodetic.YMin),
            new Point(geodetic.XMax, geodetic.YMin),
            new Point(geodetic.XMax, geodetic.YMax),
            new Point(geodetic.XMin, geodetic.YMax),
        }.Select(MapProjects.GeodeticWgs84ToWebMercator).ToList();

        return Geometry<Point>.CreatePolygon(corners, SridHelper.WebMercator);
    }

    private sealed class Subscription : IDisposable
    {
        public static readonly IDisposable None = new Subscription(null);

        private Action? _dispose;

        public Subscription(Action? dispose) => _dispose = dispose;

        public void Dispose()
        {
            _dispose?.Invoke();
            _dispose = null;
        }
    }
}
