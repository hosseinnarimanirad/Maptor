# MapViewer render-job cancellation: migrate to CancellationToken

- **Status:** proposed (analysis done 2026-08-05, not implemented)
- **Area:** `src/Presentation/IRI.Maptor.Presentation.Wpf` â€” the WPF `MapViewer` control
- **Risk:** medium (touches the hottest render path); no caller-visible behavior change
- **Effort:** ~half a day including in-app verification

## Where this lives in the repo

The WPF map control is `src/Presentation/IRI.Maptor.Presentation.Wpf/Views/Map/MapViewer.xaml.cs`
(~5400 lines), backed by `ViewModels/Map/MapViewModelBase.cs`. Render work ("jobs") is queued
onto the WPF dispatcher and tracked in a `List<Job> jobs` field; `Job` is
`Models/Map/Job.cs` (a `LayerTag` + a `DispatcherOperation`). Tile extents are diffed by
`Models/Map/ExtentManager.cs`, which fires `OnTilesAdded`/`OnTilesRemoved`. Basemap tiles are
`Layers/TileServiceLayer.cs`; vector rasterization goes through
`Cartography/RenderingStrategies/` (`RenderStrategy`, `GdiBitmapRenderStrategy`, ...). HTTP
goes through `Common/IHttpProtocol.cs`.

Line numbers below were correct as of 2026-08-05 (after the fast-zoom tile fix); treat them
as anchors, not gospel.

## Current mechanism

Render delegates are queued as
`Dispatcher.BeginInvoke(action, DispatcherPriority.Background)` and remembered as `Job`s.
Cancellation is `job.Operation.Abort()` from exactly two purge sites:

- `StopUnnecessaryJobs()` (~`MapViewer.xaml.cs:2267`) â€” called only from `Refresh`
  (~`:1816`); keeps a job iff its tile is still in `CurrentTileInfos` at the current
  `NearestGoogleZoomLevel` (this keep criterion was fixed 2026-08-05; it used to be exact
  `MapScale` equality, which caused permanent basemap holes after fast zoom).
- `ExtentManager_OnTilesRemoved` (~`:3237`) â€” aborts jobs for tiles leaving the extent.

## Why it is structurally broken

1. **`Abort()` only cancels a job that has not started.** Every dispatched delegate is an
   `async` lambda typed as `Action` â€” i.e. *async void*. The `DispatcherOperation` reports
   `Completed` at the delegate's **first `await`**, so once a job starts, `Abort()` is a
   silent no-op and the work runs to completion. The `jobs` list is therefore not an
   inventory of running work: a started job looks `Completed` while its continuations are
   still executing.

2. **The real cancellation is three ad-hoc substitutes**, grown over time:
   - snapshot-compare guards after every `await` (`MapScale != mapScale`,
     `CurrentExtent != extent`, `tile.ZoomLevel != NearestGoogleZoomLevel`) in
     `AddNonTiledLayer` (~`:1444/:1461`), `AddTiledLayerAsync` (~`:1377/:1394`),
     `AddTileServiceLayerAsync` (~`:1593/:1599`);
   - `_nonTiledGeneration`/`_tiledGeneration` counters (bumped by
     `ClearNonTiled`/`ClearTiled`) guarding stale appends after a same-scale re-render;
   - per-tile `IsProcessing` flags (vector tiled path only, via `VectorLayer.TileManager`).

   That set is exactly the job description of one `CancellationToken`.

3. **Guard-cancelled work still runs to completion.** A stale tile still finishes its HTTP
   download and then discards the image; a stale vector render still rasterizes a full-window
   bitmap off-thread and then discards the brush. During fast navigation this wastes CPU,
   bandwidth, and connection-pool slots that the *wanted* tiles are queuing behind.

4. **Known smaller defects:**
   - Completed jobs are never removed from `jobs` â€” only the purge passes prune it. Since the
     2026-08 keep-criterion fix, a completed tile job whose tile stays in view lingers
     indefinitely (harmless but wrong; the list should be pending/running work only).
   - Queueing used `Task.Run(() => { BeginInvoke(...); lock { jobs.Add(...) } })` â€” the add
     can land *after* the purge pass that should have evaluated the job, escaping one abort
     cycle. (The `lock (locker)` around every `jobs` access was restored 2026-08-05; the
     ordering race remains.) The `Task.Run` buys nothing â€” `BeginInvoke` is thread-safe.
   - async-void delegates: an exception escaping one would crash the process; the bodies rely
     on their own try/catch.

## Why CancellationToken fits this codebase specifically

- `Common/IHttpProtocol.cs:19` **already declares**
  `Task<byte[]> GetByteArrayAsync(string? requestUrl, CancellationToken cancellationToken)` â€”
  the tile download in `TileServiceLayer.GetTileAsync` (~`:236`) simply calls the token-less
  overload.
- The data-load side already follows the token pattern:
  `LayerManager._loadCancellationToken` â†’ `IDataSource.LoadAsync(ct)`, and the host application's
  `ApplicationPresenter._loadApiLayersCts` around `LoadApiLayers()`. The render path is the
  odd one out, not the norm.

## Recommended change (Phase A)

Keep `Dispatcher.BeginInvoke` as the scheduler and keep `Abort()` as the fast path for
not-yet-started jobs. Add a per-job CTS as the real cancellation channel.

### 1. Queue inline â€” drop the `Task.Run` wrappers

At all 7 job-queue sites (the `AddLayer` branches for clustered/grid/complex/vector/raster,
`AddTiledLayer`, `RefreshTiles`): call `Dispatcher.BeginInvoke` and `lock (locker) { jobs.Add }`
directly. All callers are already on the UI thread. This makes queue-vs-purge ordering
deterministic and closes the escape-one-cycle race.

### 2. `Job` gains a cancellation channel (`Models/Map/Job.cs`)

```csharp
public class Job
{
    public LayerTag Tag { get; }
    public DispatcherOperation Operation { get; }
    public CancellationTokenSource Cancellation { get; } = new();

    public void Cancel()
    {
        Cancellation.Cancel();
        Operation.Abort();   // a not-yet-started op never runs at all
    }
}
```

Both purge sites call `job.Cancel()` then dispose the CTS after removing the job.

Construction-order note: the delegate needs the token, and the `Job` needs the
`DispatcherOperation` â€” create the CTS first, close the lambda over its token, then
`BeginInvoke`, then construct the `Job` with both.

### 3. Thread the token through the render bodies

- `AddTileServiceLayerAsync(layer, tile, ct)`: check `ct.IsCancellationRequested` beside the
  existing zoom/provider guards; pass `ct` to `GetTileAsync` â†’ the existing
  `GetByteArrayAsync(url, ct)` overload, so a stale tile download actually stops.
- `AddTiledLayerAsync` / `AddNonTiledLayer(layer, ct)`: check the token beside the existing
  guards; pass it into `RenderToBrushAsync` (the off-thread render helper added 2026-08)
  and down into the strategy.
- `RenderStrategy.Render` (+ `GdiBitmapRenderStrategy`): optional
  `CancellationToken ct = default` parameter, checked between symbolizers in the per-symbolizer
  loop (~`GdiBitmapRenderStrategy.cs:67`) so a cancelled off-thread render bails instead of
  finishing a bitmap nobody wants.
- Treat `OperationCanceledException` as the expected path (swallow), consistent with
  `LoadApiLayers`.

**Keep the snapshot guards and generation counters.** The token answers "does anyone still
want this job?"; the guards answer "is my output still valid for the *current* view?" â€” a job
can be un-cancelled yet stale-output (view moved after the last check). They are
complementary; removing the guards would reintroduce the stale-append bug class.

### 4. Self-removal on completion

Wrap each dispatched body:
`try { ... } finally { lock (locker) { jobs.Remove(thisJob); } thisJob.Cancellation.Dispose(); }`.
The `jobs` list then contains only pending/running work; when the map is idle,
`jobs.Count == 0`.

## Explicitly out of scope (Phase B, only if a new loss mode appears)

- Replacing dispatcher scheduling with Tasks/`System.Threading.Channels` â€” rewrite-level risk
  for little gain now that rasterization runs off-thread.
- `ExtentManager` failure feedback / re-report channel (its one-shot `Except` diff records a
  tile as "added" at diff time with no channel back; the 2026-08 keep-criterion fix addressed
  the harmful interaction).
- Per-tile in-flight dedup for basemap downloads (duplicate `GetTileAsync` calls under churn).
- The unguarded `(LayerTag)` cast in `ClearOutOfExtent` (~`:2310`).

## Files to touch

| File | Change |
|---|---|
| `Jab.Wpf/Models/Map/Job.cs` | CTS + `Cancel()` |
| `Jab.Wpf/Views/Map/MapViewer.xaml.cs` | inline queueing; `job.Cancel()` at both purge sites; tokens through the three render bodies + `RenderToBrushAsync`; self-removal `finally` |
| `Jab.Wpf/Layers/TileServiceLayer.cs` | `GetTileAsync(..., CancellationToken)` â†’ token overload of `GetByteArrayAsync` |
| `Jab.Wpf/Cartography/RenderingStrategies/RenderStrategy.cs`, `GdiBitmapRenderStrategy.cs` | optional `ct`, checked between symbolizers |

## Verification

1. Build `IRI.Maptor.Presentation.Wpf` + `your host application` â€” 0 errors (close any running the host application
   first; it locks output DLLs).
2. Harness (pattern: a `net8.0-windows`/`UseWPF` console project referencing `Jab.Wpf`, as
   used for the 2026-08 off-thread render verification): `Render` with a pre-cancelled token
   returns promptly with no brush; with a live token, output pixels are byte-identical to a
   token-less baseline.
3. In-app: fast wheel-zoom bursts â€” all basemap tiles fill in (the 2026-08 fix must stay
   intact); Output window shows no first-chance exceptions other than
   `OperationCanceledException`.
4. In-app: rapid basemap provider switching while zooming â€” no stale tiles of the previous
   provider (their downloads now actually cancel).
5. Breakpoint after the map goes idle: `jobs.Count == 0`.

## Related history (context for whoever implements this)

Three bugs were already fixed in this seam during 2026-08, all traceable to the
Abort-cannot-cancel model:

1. UI freezes during layer render â†’ rasterization moved off the UI thread
   (`RenderToBrushAsync`, `RenderStrategy.CanRenderOffUiThread`,
   `VisualParameters.TryFreezeVisuals` â€” symbolizer brushes/DashStyle/GeometrySymbol must be
   frozen on the UI thread before a pool thread reads them).
2. Basemap tiles permanently missing after fast zoom â†’ `StopUnnecessaryJobs` keep criterion
   changed from exact-`MapScale` to tile-in-extent + zoom-level match; `lock (locker)`
   restored around all pool-thread `jobs.Add` calls.
3. Cross-thread crash on dashed (SLD `stroke-dasharray`) layers â†’ `TryFreezeVisuals` extended
   to `DashStyle` and `GeometrySymbol`, with an `InvalidOperationException` fallback to
   UI-thread rendering.
