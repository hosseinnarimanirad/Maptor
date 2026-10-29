using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.Spatial.Dtos;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Infrastructure.WebApi;

/// <summary>
/// Live mode (<see cref="WebApiSourceParameter.LoadMode"/> = Live): instead of downloading the whole layer,
/// every read first makes sure the web-mercator tiles covering it are fetched from
/// <see cref="WebApiSourceParameter.QueryUrl"/>, then filters in memory as Cache mode does.
/// <para>
/// Edits keep working while the map moves, because there is one feature set for both: fetched tiles only
/// ever <i>add</i> features to it, matched by id, and a feature already there is never replaced. So a pending
/// edit (Updated, Removed, or New) is never overwritten by the server's copy, also when that copy arrives
/// from a request started before the edit; an edited feature shows at its local geometry, wherever that is;
/// and SaveChangesAsync, which collects the changes from the set, sends edits made anywhere on the map.
/// </para>
/// <para>
/// A tile counts as loaded when it, or a tile that contains it, was fetched completely; a truncated tile
/// (more features than the server's cap) does not, so zooming in fetches its smaller tiles. Fetched
/// features stay until the next <see cref="LoadAsync(CancellationToken)"/>; the set is not trimmed.
/// </para>
/// </summary>
public partial class WebApiDataSource : IServerQueryableVectorDataSource
{
    private const double WebMercatorHalfWorld = 20037508.342789244;

    /// <summary>The deepest tile level a fetch uses; smaller requests (identify) reuse a tile of this level or above.</summary>
    private const int MaxLiveZoom = 20;

    private enum LiveTileState { Complete, Truncated }

    private readonly record struct LiveTileKey(int Z, int X, int Y)
    {
        public LiveTileKey Parent(int levelsUp) => new(Z - levelsUp, X >> levelsUp, Y >> levelsUp);
    }

    /// <summary>One fetch per tile, shared by every read that needs it; a failed fetch is removed so a later read retries.</summary>
    private readonly ConcurrentDictionary<LiveTileKey, Lazy<Task<LiveTileState>>> _liveTiles = new();

    /// <summary>The fetched features by id, including those since edited or removed, so the server's copy is never added twice.</summary>
    private readonly Dictionary<int, Feature<Point>> _liveById = new();

    /// <summary>Guards the feature set against reads and merges on different threads.</summary>
    private readonly object _liveSync = new();

    /// <summary>Tile fetches in flight (waiting for the gate included); IsFetching while above zero.</summary>
    private int _liveFetchesInFlight;

    private readonly object _liveFetchingSync = new();

    private void ChangeLiveFetches(int delta)
    {
        lock (_liveFetchingSync)
        {
            _liveFetchesInFlight += delta;
            IsFetching = _liveFetchesInFlight > 0;
        }
    }

    /// <summary>
    /// The context the source was created on (the UI thread). Merges run there, as edits do, so a merge never
    /// changes the set while an edit, a save or the renderer is using it.
    /// </summary>
    private readonly SynchronizationContext? _ownerContext = SynchronizationContext.Current;

    /// <summary>True when the source works in Live mode; without a shared client or a query url it falls back to Cache.</summary>
    public bool IsLive =>
        _parameters?.LoadMode == LayerLoadMode.Live
        && _parameters.HttpClient is not null
        && !string.IsNullOrWhiteSpace(_parameters.QueryUrl);

    /// <summary>Some fetched tile had more features than the server's cap: the map shows only part of them until zoomed in.</summary>
    public bool HasTruncatedLiveTiles { get; private set; }

    public override async Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox)
    {
        if (!IsLive)
            return await base.GetAsFeatureSetAsync(boundingBox);

        await EnsureLiveTilesAsync(boundingBox, trackView: false);

        return FilterLive(g => g.Intersects(boundingBox));
    }

    /// <summary>The map's read (the renderer passes the scale): it also tracks whether the drawn area is complete.</summary>
    public override async Task<FeatureSet<Point>> GetAsFeatureSetAsync(double mapScale, BoundingBox boundingBox)
    {
        if (!IsLive)
            return await base.GetAsFeatureSetAsync(mapScale, boundingBox);

        await EnsureLiveTilesAsync(boundingBox, trackView: true);

        return FilterLive(g => g.Intersects(boundingBox));
    }

    #region View truncation

    /// <summary>The zoom level of the map's latest read, and its drawn tiles that are incomplete (truncated).</summary>
    private int _viewZoom = -1;

    private readonly HashSet<LiveTileKey> _viewTruncatedTiles = new();

    private bool _isViewTruncated;
    public bool IsViewTruncated
    {
        get => _isViewTruncated;
        private set
        {
            if (_isViewTruncated == value)
                return;

            _isViewTruncated = value;
            ViewTruncatedChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<bool>? ViewTruncatedChanged;

    private void TrackView(int zoom, IReadOnlyList<(LiveTileKey Tile, bool Complete)> tiles)
    {
        lock (_liveSync)
        {
            if (zoom != _viewZoom)
            {
                _viewZoom = zoom;
                _viewTruncatedTiles.Clear();
            }

            foreach (var (tile, complete) in tiles)
            {
                if (complete)
                    _viewTruncatedTiles.Remove(tile);
                else
                    _viewTruncatedTiles.Add(tile);
            }
        }

        IsViewTruncated = _viewTruncatedTiles.Count > 0;
    }

    #endregion

    /// <summary>Best effort, after the start: "zoom to layer" needs the extent of features not loaded yet.</summary>
    private async Task LoadLiveExtentAsync()
    {
        try
        {
            var url = $"{_parameters.QueryUrl!.TrimEnd('/')}/Extent";

            var response = await Task.Run(() => HttpTransport.GetJsonStreamAsync<LayerExtentDto>(
                _parameters.HttpClient!, url, _parameters.BearerToken, _parameters.Headers));

            if (!response.IsSuccess || response.Result is not { MinX: double minX, MinY: double minY, MaxX: double maxX, MaxY: double maxY })
                return;

            var lowerLeft = MapProjects.GeodeticWgs84ToWebMercator(new Point(minX, minY));
            var upperRight = MapProjects.GeodeticWgs84ToWebMercator(new Point(maxX, maxY));

            var extent = new BoundingBox(lowerLeft.X, lowerLeft.Y, upperRight.X, upperRight.Y);

            await RunOnOwnerContextAsync(() => WebMercatorExtent = WebMercatorExtent.IsNaN()
                ? extent
                : BoundingBox.GetMergedBoundingBox(new[] { WebMercatorExtent, extent }));
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Extent of '{_parameters.QueryUrl}' failed: {ex.Message}");
        }
    }

    /// <summary>The attribute table pages on the server in Live mode; in Cache mode it works in memory.</summary>
    public bool CanQueryServer => IsLive;

    public async Task<FeatureQueryPage> QueryPageAsync(FeatureQueryDto query, CancellationToken cancellationToken = default)
    {
        if (!IsLive)
            throw new InvalidOperationException("Server queries need Live mode.");

        var stopwatch = Stopwatch.StartNew();

        var (page, features) = await Task.Run(async () =>
        {
            var response = await WebApiInfrastructure.QueryFeaturesAsync(
                _parameters.HttpClient!, _parameters.QueryUrl!, query, _parameters.BearerToken, _parameters.Headers, cancellationToken);

            if (!response.IsSuccess || response.Result is null)
                throw new Exception(response.Error?.Detail ?? response.Error?.Title ?? "no answer");

            return (response.Result, ToWebMercatorFeatures(response.Result));
        }, cancellationToken);

        // The source's own objects, in the server's order: a feature already known (edited or not) is
        // returned as it is here, an unknown one joins the set so that it can be edited and saved.
        var canonical = new List<Feature<Point>>(features.Count);

        await RunOnOwnerContextAsync(() => MergeLive(features, canonical));

        Trace.TraceInformation($"Live page {query.PageIndex} ({canonical.Count} rows, total {page.TotalCount?.ToString() ?? "-"}) of '{_parameters.QueryUrl}' in {stopwatch.ElapsedMilliseconds} ms");

        return new FeatureQueryPage
        {
            Features = canonical,
            TotalCount = page.TotalCount,
            PageIndex = page.PageIndex ?? query.PageIndex ?? 0,
            PageSize = page.PageSize ?? query.PageSize,
        };
    }

    public async Task<DistinctValuesDto> GetDistinctValuesAsync(string fieldName, CancellationToken cancellationToken = default)
    {
        if (!IsLive)
            throw new InvalidOperationException("Server queries need Live mode.");

        return await GetFromQueryUrlAsync<DistinctValuesDto>($"Distinct/{Uri.EscapeDataString(fieldName)}", cancellationToken);
    }

    public Task<FieldStatisticsDto> GetFieldStatisticsAsync(string fieldName, CancellationToken cancellationToken = default)
        => GetFromQueryUrlAsync<FieldStatisticsDto>($"Statistics/Field/{Uri.EscapeDataString(fieldName)}", cancellationToken);

    public Task<GroupStatisticsDto> GetGroupStatisticsAsync(string fieldName, string? sumFieldName, string? measureKind, CancellationToken cancellationToken = default)
        => GetFromQueryUrlAsync<GroupStatisticsDto>(
            $"Statistics/Group/{Uri.EscapeDataString(fieldName)}{QueryString(("sumField", sumFieldName), ("measure", measureKind))}", cancellationToken);

    public Task<GeometryStatisticsDto> GetGeometryStatisticsAsync(string? measureKind, CancellationToken cancellationToken = default)
        => GetFromQueryUrlAsync<GeometryStatisticsDto>($"Statistics/Geometry{QueryString(("measure", measureKind))}", cancellationToken);

    /// <summary>A GET on the layer's query url (Live mode only); a refusal or no answer throws with the server's message.</summary>
    private async Task<T> GetFromQueryUrlAsync<T>(string relativePath, CancellationToken cancellationToken) where T : class
    {
        if (!IsLive)
            throw new InvalidOperationException("Server queries need Live mode.");

        var url = $"{_parameters.QueryUrl!.TrimEnd('/')}/{relativePath}";

        var response = await Task.Run(() => HttpTransport.GetJsonStreamAsync<T>(
            _parameters.HttpClient!, url, _parameters.BearerToken, _parameters.Headers, cancellationToken), cancellationToken);

        if (!response.IsSuccess || response.Result is null)
            throw new Exception(response.Error?.Detail ?? response.Error?.Title ?? "no answer");

        return response.Result;
    }

    private static string QueryString(params (string Name, string? Value)[] parameters)
    {
        var present = parameters.Where(p => !string.IsNullOrWhiteSpace(p.Value)).Select(p => $"{p.Name}={Uri.EscapeDataString(p.Value!)}").ToList();

        return present.Count == 0 ? string.Empty : "?" + string.Join("&", present);
    }

    /// <summary>Starts Live mode: one 1-row query gives the fields and the geometry type; no features are kept yet.</summary>
    private async Task LoadLiveAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        _liveTiles.Clear();

        var response = await WebApiInfrastructure.QueryFeaturesAsync(
            _parameters.HttpClient!,
            _parameters.QueryUrl!,
            new FeatureQueryDto { PageIndex = 0, PageSize = 1 },
            _parameters.BearerToken,
            _parameters.Headers,
            cancellationToken);

        if (!response.IsSuccess || response.Result is null)
            throw new Exception($"Starting Live mode failed for '{_parameters.QueryUrl}': {response.Error?.Detail ?? response.Error?.Title}");

        var probe = ToWebMercatorFeatures(response.Result);

        var fields = response.Result.Fields ?? Field.FromDictionary(probe.FirstOrDefault()?.Attributes);

        var geometryType = probe.FirstOrDefault()?.GeometryType;

        lock (_liveSync)
        {
            _liveById.Clear();

            _webMercatorFeatureSet = FeatureSet<Point>.CreateEmpty(SridHelper.WebMercator, geometryType ?? Core.Common.Enums.GeometryType.None, fields);
        }

        Fields = fields;
        GeometryType = geometryType;
        WebMercatorExtent = BoundingBox.NaN;
        HasTruncatedLiveTiles = false;

        lock (_liveSync)
        {
            _viewZoom = -1;
            _viewTruncatedTiles.Clear();
        }

        _liveFullyLoaded = false;

        IsViewTruncated = false;

        _ = LoadLiveExtentAsync();

        UpdateHasPendingChanges();

        Trace.TraceInformation($"Live mode for '{_parameters.QueryUrl}': {fields?.Count ?? 0} fields, {geometryType}, started in {stopwatch.ElapsedMilliseconds} ms");
    }

    private async Task<FeatureSet<Point>> GetLiveAsync(Geometry<Point>? geometry)
    {
        var hasGeometry = geometry is not null && !geometry.IsNullOrEmpty();
        var hasFilter = FilterGeometry is not null && !FilterGeometry.IsNullOrEmpty();

        // No geometry: the whole layer (export, analyses): fetched once from the server, then held as in Cache mode.
        if (!hasGeometry)
        {
            await EnsureAllLiveAsync();

            if (!hasFilter)
            {
                lock (_liveSync)
                    return _webMercatorFeatureSet;
            }

            return FilterLive(g => g.Intersects(FilterGeometry!));
        }

        // A drawn selection or an identify: its tiles, and when some are truncated (a large area), all of its box.
        var box = geometry!.GetBoundingBox();

        if (!await EnsureLiveTilesAsync(box, trackView: false))
            await FetchAllLiveAsync(box);

        return hasFilter
            ? FilterLive(g => g.Intersects(FilterGeometry!) && g.Intersects(geometry))
            : FilterLive(g => g.Intersects(geometry));
    }

    private FeatureSet<Point> FilterLive(Predicate<Geometry<Point>> predicate)
    {
        lock (_liveSync)
            return _webMercatorFeatureSet.FilterByGeometry(predicate);
    }

    #region Tiles

    /// <summary>True when every covering tile is complete (or the whole layer is loaded).</summary>
    private async Task<bool> EnsureLiveTilesAsync(BoundingBox webMercatorBox, bool trackView)
    {
        if (webMercatorBox.IsNaN())
            return true;

        var zoom = ChooseZoom(webMercatorBox);

        var tiles = CoveringTiles(webMercatorBox, zoom).ToList();

        var complete = _liveFullyLoaded
            ? tiles.Select(_ => true).ToArray()
            : await Task.WhenAll(tiles.Select(EnsureTileAsync));

        if (trackView)
            TrackView(zoom, tiles.Select((tile, i) => (tile, complete[i])).ToList());

        return complete.All(c => c);
    }

    #region Whole layer and large areas

    /// <summary>Set once the whole layer has been fetched: from then on no tile is fetched (the layer is as in Cache mode).</summary>
    private bool _liveFullyLoaded;

    private Lazy<Task>? _liveFullLoad;

    private Task EnsureAllLiveAsync()
    {
        if (_liveFullyLoaded)
            return Task.CompletedTask;

        var load = _liveFullLoad ??= new Lazy<Task>(async () =>
        {
            try
            {
                await FetchAllLiveAsync(null);
                _liveFullyLoaded = true;
                IsViewTruncated = false;
            }
            finally
            {
                _liveFullLoad = null;
            }
        });

        return load.Value;
    }

    /// <summary>
    /// Every feature of the layer, or of <paramref name="webMercatorBox"/>, in chunks of the server's extent cap
    /// (Id > the last id, ordered by id), merged as tiles are: pending edits are kept. The layer is busy meanwhile.
    /// </summary>
    private async Task FetchAllLiveAsync(BoundingBox? webMercatorBox)
    {
        var stopwatch = Stopwatch.StartNew();

        var query = new FeatureQueryDto();

        if (webMercatorBox is BoundingBox box)
        {
            var lowerLeft = MapProjects.WebMercatorToGeodeticWgs84(new Point(box.XMin, box.YMin));
            var upperRight = MapProjects.WebMercatorToGeodeticWgs84(new Point(box.XMax, box.YMax));

            (query.MinX, query.MinY, query.MaxX, query.MaxY) = (lowerLeft.X, lowerLeft.Y, upperRight.X, upperRight.Y);
        }

        var keyField = Fields?.FirstOrDefault(f => string.Equals(f.Name, "Id", StringComparison.OrdinalIgnoreCase))?.Name ?? "Id";

        IsProcessing = true;

        try
        {
            int? lastId = null;
            int total = 0, chunks = 0;

            while (true)
            {
                query.Filters = lastId is null
                    ? null
                    : new List<FeatureFilterDto> { new() { Field = keyField, Operator = FeatureFilterOperator.Gt, Value = lastId.Value } };

                var (page, features) = await Task.Run(async () =>
                {
                    var response = await WebApiInfrastructure.QueryFeaturesAsync(
                        _parameters.HttpClient!, _parameters.QueryUrl!, query, _parameters.BearerToken, _parameters.Headers);

                    if (!response.IsSuccess || response.Result is null)
                        throw new Exception(response.Error?.Detail ?? response.Error?.Title ?? "no answer");

                    return (response.Result, ToWebMercatorFeatures(response.Result));
                });

                await RunOnOwnerContextAsync(() => MergeLive(features));

                total += features.Count;
                chunks++;

                if (!page.Truncated || page.Rows.Count == 0)
                    break;

                lastId = page.Rows.Max(r => r.Id);
            }

            Trace.TraceInformation($"Live {(webMercatorBox is null ? "whole layer" : "area")} of '{_parameters.QueryUrl}': {total} features in {chunks} requests, {stopwatch.ElapsedMilliseconds} ms");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Map search in Live mode: the server's matches (up to one page, 1,000), as the source's own objects,
    /// plus the unsaved new features matching here (the server does not have them yet).
    /// </summary>
    private async Task<FeatureSet<Point>> LiveSearchAsync(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return FeatureSet<Point>.Empty;

        var page = await QueryPageAsync(new FeatureQueryDto { SearchText = searchText, PageIndex = 0, PageSize = 1000 });

        var lower = searchText.Trim().ToLowerInvariant();

        List<Feature<Point>> matching;

        lock (_liveSync)
        {
            var added = _webMercatorFeatureSet.Features.Where(f => f.Status == FeatureStatus.New &&
                f.Attributes?.Values.Any(v => v is string s && s.ToLowerInvariant().Contains(lower)) == true);

            matching = page.Features.Where(f => f.Status != FeatureStatus.Removed && f.Status != FeatureStatus.CanceledNew).Concat(added).ToList();
        }

        if (matching.Count == 0)
            return FeatureSet<Point>.Empty;

        var result = FeatureSet<Point>.Create(string.Empty, matching);

        result.Fields = Fields;

        return result;
    }

    #endregion

    /// <summary>
    /// True when the tile's features are all here (it or a tile containing it was fetched completely); false
    /// when they were truncated. Never throws: a tile that fails is logged and left out (and counts as
    /// complete, since too many features is not the problem), so the rest of the map still draws.
    /// </summary>
    private async Task<bool> EnsureTileAsync(LiveTileKey tile)
    {
        // Already loaded: this tile or a tile containing it, fetched completely (in flight: wait for it).
        for (int levelsUp = 0; levelsUp <= tile.Z; levelsUp++)
        {
            if (!_liveTiles.TryGetValue(tile.Parent(levelsUp), out var existing))
                continue;

            try
            {
                if (await existing.Value == LiveTileState.Complete)
                    return true;
            }
            catch
            {
                // That fetch failed; it removes itself and the loop goes on.
            }
        }

        var fetch = _liveTiles.GetOrAdd(tile, key => new Lazy<Task<LiveTileState>>(() => FetchTileAsync(key)));

        try
        {
            return await fetch.Value == LiveTileState.Complete;
        }
        catch (Exception ex)
        {
            _liveTiles.TryRemove(new KeyValuePair<LiveTileKey, Lazy<Task<LiveTileState>>>(tile, fetch));

            Trace.TraceWarning($"Live tile {tile} of '{_parameters.QueryUrl}' failed: {ex.Message}");

            return true;
        }
    }

    private async Task<LiveTileState> FetchTileAsync(LiveTileKey tile)
    {
        ChangeLiveFetches(+1);

        try
        {
            return await FetchTileCoreAsync(tile);
        }
        finally
        {
            ChangeLiveFetches(-1);
        }
    }

    private async Task<LiveTileState> FetchTileCoreAsync(LiveTileKey tile)
    {
        var box = TileBox(tile);

        var stopwatch = Stopwatch.StartNew();

        // Download and parsing off the UI thread; only the merge comes back to it.
        var (page, features) = await Task.Run(async () =>
        {
            var lowerLeft = MapProjects.WebMercatorToGeodeticWgs84(new Point(box.XMin, box.YMin));
            var upperRight = MapProjects.WebMercatorToGeodeticWgs84(new Point(box.XMax, box.YMax));

            var query = new FeatureQueryDto { MinX = lowerLeft.X, MinY = lowerLeft.Y, MaxX = upperRight.X, MaxY = upperRight.Y };

            await _loadGate.WaitAsync();

            try
            {
                var response = await WebApiInfrastructure.QueryFeaturesAsync(
                    _parameters.HttpClient!, _parameters.QueryUrl!, query, _parameters.BearerToken, _parameters.Headers);

                if (!response.IsSuccess || response.Result is null)
                    throw new Exception(response.Error?.Detail ?? response.Error?.Title ?? "no answer");

                return (response.Result, ToWebMercatorFeatures(response.Result));
            }
            finally
            {
                _loadGate.Release();
            }
        });

        var added = 0;

        await RunOnOwnerContextAsync(() => added = MergeLive(features));

        if (page.Truncated)
            HasTruncatedLiveTiles = true;

        Trace.TraceInformation($"Live tile {tile} of '{_parameters.QueryUrl}': {features.Count} features ({added} new){(page.Truncated ? ", TRUNCATED" : "")} in {stopwatch.ElapsedMilliseconds} ms");

        return page.Truncated ? LiveTileState.Truncated : LiveTileState.Complete;
    }

    /// <summary>
    /// Adds the features not known yet; a known feature, edited or not, keeps its object and state. With
    /// <paramref name="canonical"/>, also lists the source's object for every input feature, in input order.
    /// </summary>
    private int MergeLive(List<Feature<Point>> features, List<Feature<Point>>? canonical = null)
    {
        var fresh = new List<Feature<Point>>();

        foreach (var feature in features)
        {
            if (_liveById.TryGetValue(feature.Id, out var known))
            {
                canonical?.Add(known);
                continue;
            }

            _liveById[feature.Id] = feature;
            fresh.Add(feature);
            canonical?.Add(feature);
        }

        if (fresh.Count == 0)
            return 0;

        _webMercatorFeatureSet.Attach(fresh);

        if (GeometryType is null)
        {
            GeometryType = fresh[0].GeometryType;
            _webMercatorFeatureSet.GeometryType = fresh[0].GeometryType;
        }

        var freshExtent = BoundingBox.GetMergedBoundingBox(fresh.Select(f => f.TheGeometry.GetBoundingBox()));

        WebMercatorExtent = WebMercatorExtent.IsNaN()
            ? freshExtent
            : BoundingBox.GetMergedBoundingBox(new[] { WebMercatorExtent, freshExtent });

        return fresh.Count;
    }

    /// <summary>After a save: new features now carry their server ids, so a tile fetched later does not add them again.</summary>
    protected void RegisterSavedLiveFeatures()
    {
        if (!IsLive)
            return;

        lock (_liveSync)
        {
            foreach (var feature in _webMercatorFeatureSet.GetAllFeatures())
                _liveById.TryAdd(feature.Id, feature);
        }
    }

    private Task RunOnOwnerContextAsync(Action action)
    {
        if (_ownerContext is null || SynchronizationContext.Current == _ownerContext)
        {
            lock (_liveSync)
                action();

            return Task.CompletedTask;
        }

        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _ownerContext.Post(_ =>
        {
            try
            {
                lock (_liveSync)
                    action();

                done.SetResult();
            }
            catch (Exception ex)
            {
                done.SetException(ex);
            }
        }, null);

        return done.Task;
    }

    private List<Feature<Point>> ToWebMercatorFeatures(FeaturePageDto page)
    {
        var featureSetDto = page.ToFeatureSetDto();

        if (featureSetDto.Features is null || featureSetDto.Features.Count == 0)
            return new List<Feature<Point>>();

        if (featureSetDto.Srid == 0)
            featureSetDto.SetSrid(_parameters.Srid);

        return featureSetDto.AsFeatureSet(IdColumnName).GetAllFeatures().ToList();
    }

    /// <summary>The level whose tile is at least as large as the box, so a box is covered by at most 2 × 2 tiles.</summary>
    private static int ChooseZoom(BoundingBox box)
    {
        var span = Math.Max(box.Width, box.Height);

        if (!(span > 0))
            return MaxLiveZoom;

        var zoom = (int)Math.Floor(Math.Log2(2 * WebMercatorHalfWorld / span));

        return Math.Clamp(zoom, 0, MaxLiveZoom);
    }

    private static IEnumerable<LiveTileKey> CoveringTiles(BoundingBox box, int zoom)
    {
        var count = 1 << zoom;
        var size = 2 * WebMercatorHalfWorld / count;

        // Half-open: a box's max edges are exclusive, so a box that is exactly one tile (as the renderer
        // asks) is that one tile, not also the three neighbours whose edges it touches.
        int First(double offset) => Math.Clamp((int)Math.Floor(offset / size), 0, count - 1);
        int Last(double offset, int first) => Math.Clamp(Math.Max((int)Math.Ceiling(offset / size) - 1, first), 0, count - 1);

        var firstColumn = First(box.XMin + WebMercatorHalfWorld);
        var lastColumn = Last(box.XMax + WebMercatorHalfWorld, firstColumn);
        var firstRow = First(WebMercatorHalfWorld - box.YMax);
        var lastRow = Last(WebMercatorHalfWorld - box.YMin, firstRow);

        for (int x = firstColumn; x <= lastColumn; x++)
            for (int y = firstRow; y <= lastRow; y++)
                yield return new LiveTileKey(zoom, x, y);
    }

    private static BoundingBox TileBox(LiveTileKey tile)
    {
        var size = 2 * WebMercatorHalfWorld / (1 << tile.Z);

        var xMin = -WebMercatorHalfWorld + tile.X * size;
        var yMax = WebMercatorHalfWorld - tile.Y * size;

        return new BoundingBox(xMin, yMax - size, xMin + size, yMax);
    }

    #endregion
}
