# MapViewer: slow-network basemap downloads block/stutter vector rendering

- **Status:** Step A implemented 2026-08-08 (build verified, 0 errors; in-app throttled-network
  verification still pending). Steps B and C remain proposed.
- **Area:** `src/IRI.Maptor.Jab/IRI.Maptor.Jab.Common` — `Views/Map/MapViewer.xaml.cs`
  (`RefreshTiles`, `AddTileServiceLayerAsync`), `Layers/TileServiceLayer.cs`,
  `Jab.Core/TileServices/TileCacheAddress.cs`
- **Symptom (user-reported):** on a slow network, map refresh appears to wait for basemap tiles;
  vector layers render late, and the UI has small freezes while tiles arrive.
- **Companion docs:** `mapviewer-job-cancellation.md` (S3 — complements this; see "Relation to
  S3" below), `mapviewer-bugs-and-improvements.md`
- **Risk:** medium (hot render path); **effort:** ~half a day incl. in-app verification

## Confirmed mechanism — three causes, one pipeline

### 1. Per-tile jobs serialize basemap → vector (the structural block)

`ExtentManager_OnTilesAdded` (`MapViewer.xaml.cs:3410`) queues **one dispatcher job per tile**
via `RefreshTiles(infos, tile, l => true)` (`:1838`). That job's async lambda loops over *all*
tiled-mode layers **awaiting each sequentially**:

```csharp
foreach (ILayer item in infos)          // ordered: BaseMap FIRST (see below)
{
    if (item is VectorLayer vectorLayer)
        await AddTiledLayerAsync(vectorLayer, tile);          // waits for the line above
    else if (item is TileServiceLayer tileServiceLayer)
        await AddTileServiceLayerAsync(tileServiceLayer, tile); // full HTTP download awaited
}
```

`LayerManager.UpdateAndGetLayers` (`LayerManager.cs:284`) sorts
`OrderByDescending(i => i.Type == LayerType.BaseMap)` — the basemap is deliberately first in
`infos`. So for every tile, the **full basemap HTTP download** (up to the 15 s default
`HttpClient.Timeout` set in `HttpProtocol.ConfigHttpClient`) completes — or times out — before
any tiled vector layer for that same tile even starts. On a slow network this is precisely
"vector layers wait for basemaps".

The ordering is **not needed for correctness**: basemap paths go to
`mapView.Children.Insert(0, ...)` and other layers to `Children.Add(...)`, so visual stacking
is independent of completion order.

### 2. Real work runs on the UI thread per tile (the micro-freezes)

Nothing in `TileServiceLayer` uses `ConfigureAwait(false)`, and the per-tile job starts on the
dispatcher — so everything except the HTTP wait itself runs on the UI thread:

- **Synchronous disk I/O in the job prefix:** `_cache.GetTile(tile)`
  (`TileCacheAddress.cs:102`) does `File.Exists` + `File.ReadAllBytes` synchronously. On a
  cache *hit* the entire tile pipeline (read + decode + decode + insert) runs synchronously
  inside the job.
- **The image is decoded twice, both times on the UI thread:** once in
  `DownloadTileAsync` as a validation check
  (`ImageUtility.CreateBitmapImage(byteImage) == null`, `TileServiceLayer.cs:238`), and again
  in `AddTileServiceLayerAsync` to build the `ImageBrush`
  (`MapViewer.xaml.cs:1772`). Each is a full PNG/JPEG decode (`BitmapCacheOption.OnLoad`
  decodes eagerly in `EndInit`), typically 5–30 ms per tile.
- Neither the `BitmapImage` nor the `ImageBrush` is frozen (extra render-thread marshaling).

On a slow network, downloads trickle in over seconds, so these decode continuations land as a
scattered stream of UI-thread work — the "small freezing" the user feels. (Contrast: the MAUI
tier already does `GetByteArrayAsync(url).ConfigureAwait(false)` in `TileImageCache`.)

### 3. Queue ordering delays non-tiled vector jobs

Tile jobs are enqueued (Background priority) from the `CurrentTileInfos` update **before**
`Refresh` queues the non-tiled vector jobs, and dispatcher FIFO within a priority means every
tile job's synchronous prefix (including the sync cache disk read) runs before the first
vector job starts.

Non-tiled vector layers are *not* structurally blocked (rasterization already runs off-thread
via `RenderToBrushAsync`) — they are delayed by (3) and stuttered by (2). **Tiled** vector
layers are fully blocked by (1).

## Fix plan

### Step A — stop serializing layers inside the per-tile job

In `RefreshTiles`'s dispatched lambda, collect the per-layer tasks and await them together
instead of one-by-one:

```csharp
var tasks = new List<Task>();
foreach (ILayer item in infos)
{
    ...existing guards...
    if (item is VectorLayer vectorLayer)
    {
        vectorLayer.TileManager.TryAdd(tile);
        tasks.Add(AddTiledLayerAsync(vectorLayer, tile));
    }
    else if (item is TileServiceLayer tileServiceLayer)
        tasks.Add(AddTileServiceLayerAsync(tileServiceLayer, tile));
}
try { await Task.WhenAll(tasks); } catch (Exception ex) { Debug.Print(...); }
```

All bodies start on the UI thread and interleave only at their awaits, so no new concurrency
hazards beyond what already exists between separate tile jobs. `AddTileServiceLayerAsync`
catches internally; `AddTiledLayerAsync` has try/finally but no catch — hence the try/catch
around `WhenAll`. Visual stacking is unaffected (`Insert(0)` vs `Add`, see above).

Result: vector tiles render as soon as their (local) data is ready; the basemap fills in
whenever its download completes; multiple tile-service layers download in parallel.

### Step B — move the tile pipeline off the UI thread, decode once, freeze

In `AddTileServiceLayerAsync`, wrap fetch + decode in one pool-thread hop:

```csharp
var (geoImage, bitmap) = await Task.Run(async () =>
{
    var gi = await layer.GetTileAsync(tile, _presenter.HttpClient);
    var bmp = ImageUtility.CreateBitmapImage(gi.Image);
    bmp?.Freeze();                       // frozen → usable from the UI thread
    return (gi, bmp);
});
```

- Moves off the UI thread: the synchronous cache read (`File.ReadAllBytes`), the validation
  decode inside `DownloadTileAsync`, the cache write scheduling, and the display decode.
- The existing post-await staleness guards (`tile.ZoomLevel != TileZoomLevel`, provider check)
  stay where they are — after the await, back on the UI thread.
- Build the `ImageBrush` from the frozen bitmap and `Freeze()` the brush too; drop the second
  `CreateBitmapImage` call. The UI-thread remainder is just rectangle geometry + `Path` +
  `Children.Insert/Add` — sub-millisecond.
- Keep `DownloadTileAsync`'s validation decode (it prevents caching server error pages as
  tiles); it is now off-thread. Optional later cleanup: single-decode restructure.
- **Verify during implementation:** `GetNotFoundImage` and everything else reachable from
  `GetTileAsync` touches no `DispatcherObject` (expected: byte[] + file I/O only, `GeoReferencedImage`
  lives in WPF-free `Jab.Core`); `HttpClient` is thread-safe.

`GetCachedBaseMapsAsync`/`PdfHelper` also call `GetTileAsync` — unaffected (the method itself
doesn't change; only `MapViewer` wraps it in `Task.Run`).

### Step C — explicitly out of scope, covered by S3 (`mapviewer-job-cancellation.md`)

On slow networks, stale tile downloads (from zoom levels already left) still run to completion
and hog connection slots ahead of wanted tiles. That is the job-cancellation redesign:
per-job CTS → `GetByteArrayAsync(url, ct)`. Implementing S3 after A+B compounds the benefit;
nothing in A/B conflicts with it. A shorter, tile-specific HTTP timeout could also be
considered there (15 s is long for a 256px tile).

## Verification

1. Build `IRI.Maptor.Jab.Common` + `IRI.App.MakanNegarSaba` (close running Saba first).
2. Throttle the network (e.g. temporarily wrap `HttpProtocol.GetByteArrayAsync` with
   `await Task.Delay(2000)` or use an OS/proxy throttle). Load a project with vector layers +
   basemap:
   - vector layers (tiled and non-tiled) appear promptly while basemap tiles trickle in;
   - no UI stutter while tiles arrive (drag the map continuously during fill-in — it should
     stay smooth; before the fix each arriving tile causes a hitch);
   - basemap tiles still land *under* vector layers (Insert(0) stacking intact).
3. Cache behavior: tiles cached on first view load instantly (and still stutter-free) on
   revisit; no garbage files in the cache after pointing the provider at an error URL.
4. Regression sweep: fast wheel-zoom tile fill-in (2026-08 fix), provider switching while
   zooming, offline mode (not-found image), PDF export (uses `GetTileAsync` directly).
