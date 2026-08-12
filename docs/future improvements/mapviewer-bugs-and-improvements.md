# MapViewer code review: bugs and improvements

- **Status:** partially implemented — **all bugs (B1–B9) and all perf items (P1–P5) plus S1
  done (2026-08-06)**; S2, S3, S4 still open
- **Area:** `src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Views/Map/MapViewer.xaml.cs` (~5,400 lines)
- **Companion doc:** `mapviewer-job-cancellation.md` (render-job cancellation redesign — items here
  deliberately do not repeat it)
- **Suggested order for what remains:** S2/S4 (small structure), then S3 (the job-cancellation
  redesign — its own doc). No bugs or perf items remain.

Line numbers were correct as of 2026-08-05 and are **stale** after the 2026-08-06 changes — treat
them as anchors, not gospel. Everything listed was verified by reading the code, not speculation.

Already fixed earlier in 2026-08 and therefore *not* in this list: UI-thread rasterization
(moved off-thread via `RenderToBrushAsync`), basemap tiles missing after fast zoom
(`StopUnnecessaryJobs` keep criterion), unsynchronized `jobs` list mutation, cross-thread
Freezable crashes (`VisualParameters.TryFreezeVisuals`).

---

## Bugs — high priority

### B1. Wheel zoom has no zoom limit at all — ✅ DONE (2026-08-06)

> Fixed as part of the configurable-zoom work. `IsGoogleZoomLevelsEnabled` is no longer a dead
> branch: it became a computed shim over a new `IMapSettings.ZoomStepsPerGoogleLevel` (1–8, default
> 2), and *every* stepped zoom path now goes through `GetSteppedScreenScale`, which clamps to
> Min/MaxGoogleZoomLevel. The unbounded `ScreenScale * 1.5` branch and `ZoomToPoint` are gone.
> Region zoom is bounded separately by `ClampScreenScale` (see S1).


`ZoomInPlaceAtWindowPoint` (`:3661-3681`): the `CheckGoogleZoomLevel` clamp
(`MapSettings.MinGoogleZoomLevel`/`MaxGoogleZoomLevel`) is applied **only** inside the
`IsGoogleZoomLevelsEnabled` branch — which is dead code: the setting
(`Jab.Core/Data/Settings/MapSettings.cs:9`) defaults to `false` and nothing in the repo sets it.
The live branch is `newScreenScale = this.ScreenScale * 1.5` with **no bound**. Consequences:
unlimited wheel zoom in both directions, tile requests for absurd zoom levels, double-precision
degradation of the pan/zoom transforms, huge tile lists from
`WebMercatorBoundingBoxToGoogleTileRegions`.

**Fix direction:** a single `ClampScreenScale(double)` helper derived from
Min/MaxGoogleZoomLevel, applied in `ZoomInPlaceAtWindowPoint`, `ZoomToPoint` (`:3847`), and the
double-click zoom. See also S1.

### B2. `ZoomToExtent` is `async void` and rethrows inside itself — ✅ DONE (2026-08-06)

> All three methods (`ZoomToExtent`, `AddLayer(RasterLayer, BoundingBox)`, `ShowGeometryComparison`)
> are now `async Task`; the `catch (Exception ex) { throw; }` is catch-and-trace. Call sites use
> `_ =` discards. `RequestShowGeometryComparison` needed a lambda wrapper instead of a method-group
> assignment. No `MapViewModelBase` delegate signatures changed.


`private async void ZoomToExtent(...)` (`:3710`) contains `catch (Exception ex) { throw; }`
(`:3813-3816`) around the animation block (`Task.WhenAll` of 8 animations). An exception there
rethrows from an `async void` continuation on the dispatcher → **uncatchable process crash**.
Same `async void` pattern: `AddLayer(RasterLayer, sb.BoundingBox)` (`:1598`),
`ShowGeometryComparison` (`:3926`).

**Fix direction:** make them `async Task` (callers await or explicitly discard), replace
`catch { throw; }` with catch-and-trace. Note `ZoomToExtent` has many call sites including via
`MapViewModelBase.RequestZoomToExtent`; signature change ripples but is mechanical.

### B3. Shared animation counters race between `Pan` and `ZoomToExtent` — ✅ DONE (2026-08-06)

> `Pan` was ported to `AnimateAsync` + `Task.WhenAll` (via a private `PanCoreAsync`, so the public
> `Pan(double, double, Action?)` signature still compiles for RahyabTehran/ZaminNegar), and both
> `counter`/`counterValue` fields are deleted. Two extra defects found while fixing it: `Pan` reset
> the counters *before* its `IsNaN` and `Abs(...) > 2` guards, so even a pan too small to animate
> clobbered an in-flight `ZoomToExtent`; and it reused a single `DoubleAnimation` instance across
> all four `BeginAnimation` calls, which is why one `Completed` handler had to count to 4.


`counter` / `counterValue` are **instance fields** (`:3428`) used by two independent animated
operations: `Pan()` sets `counterValue = 4` (`:3354`), `ZoomToExtent` sets `counterValue = 8`
(`:3743`). Overlapping calls (e.g. `PanTo` twice quickly, or a `Pan` during a `ZoomToExtent`
animation) corrupt each other's completion counting in the `Completed` handler
(`++counter != counterValue`, `:3371`) → the final `UpdateTileInfos()` + `Refresh` either never
fires (map left stale, callback dropped) or fires prematurely.

**Fix direction:** `ZoomToExtent` already has the correct pattern — `AnimateAsync` +
`Task.WhenAll` (`:3875-3884`). Port `Pan` to it and delete both counter fields.

### B4. A failed basemap tile render pops a blocking modal — ✅ DONE (2026-08-06)

> Now traces via `Debug.Print`, matching the `AddNonTiledLayer` handler.


`AddTileServiceLayerAsync` catch handler: `MessageBox.Show("AddLayerAsync " + ex.Message)`
(`:1676`). Any transient exception while rendering a basemap tile — once **per failing tile**,
including during shutdown or sign-out churn — throws a modal message box.

**Fix direction:** trace like the `AddNonTiledLayer` handler (`:1513-1519`) does.

## Bugs — medium

### B5. Unguarded `(LayerTag)` casts in every canvas scan — ✅ DONE (2026-08-06)

> All four sites now use `if (this.mapView.Children[i] is not FrameworkElement { Tag: LayerTag tag })
> continue;`, which guards the `FrameworkElement` cast and the `LayerTag` cast in one pattern, and
> `ClearTileBorder` uses `tag.Tile?.Equals(tile) == true`. No hard `(LayerTag)` cast remains in the
> file. `Find(TileInfo)` was left alone: it looks like the same bug but is already safe — the
> possibly-null `tag.Tile` is only ever passed as an *argument* to `tile.Equals(...)`, never
> dereferenced. `UpdateZIndex` was likewise already guarded (`as LayerTag` + null `continue`).


`ClearOutOfExtent` (`:2310`) and both `Clear` overloads (`:2374`, `:2393`) hard-cast
`(LayerTag)((FrameworkElement)child).Tag`. One child with a foreign/null Tag crashes every
subsequent `Clear`/`Refresh` — and `ClearOutOfExtent` runs inside `ExtentManager.Update`, so the
exception propagates up through the `CurrentTileInfos` setter and aborts the zoom gesture
mid-update (leaving `ExtentManager._currentTiles` mutated but no `Refresh` run). Related:
`ClearTileBorder` (`:3318`) dereferences `tag.Tile.Equals(...)` where `tag.Tile` is null for
every non-tile element.

**Fix direction:** mechanical — `if (child.Tag is not LayerTag tag) continue;` at each site,
null-check `tag.Tile` in `ClearTileBorder`.

### B6. Re-`Register` leaks event handlers — ✅ DONE (2026-08-06)

> The six `+=` lambdas became named handlers subscribed with `-=`/`+=`, matching the pattern the
> `extentManager` subscriptions and `EnableZoomingOnMouseWheel` already used. They read `_presenter`
> at fire time instead of capturing it, so re-registering with a different presenter routes to the
> new one and no superseded presenter stays pinned. Invocation order is unchanged: the constructor's
> own `OnZoomChanged` handler (which updates `_mapScale` and the tile infos) still runs first.
>
> `PredefinedExtents.CollectionChanged` was the seventh leaked subscription; its lambda moved into
> the previously empty `PredefinedExtents_CollectionChanged` method (also clearing one S4 item) and
> is now `-=`/`+=`'d. The province/bookmark list is detached and cleared before repopulating, since
> a re-`Register` against the same presenter appended a second copy of every entry and `Reset` does
> not report `OldItems`.


`Register` (`:491`) subscribes lambdas to `OnMapMouseMove`, `OnZoomChanged`, `OnExtentChanged`,
`MouseUp`, `CurrentEditingPointChanged` (`:577-590`) with `+=` only — never unsubscribable. The
`extentManager` subscriptions at `:430-434` are defensively `-=`/`+=`'d, and
`presenter.RegisterAction` (`:513`) re-invokes `Register`, so re-registration is anticipated.
A second `Register` (re-login flow) double-fires every one of these per event, duplicates
`PredefinedExtents` province entries (`:783-786`), and pins the old presenter.

**Fix direction:** either make `Register` idempotent (store and remove previous delegates) or
guard with a `_registered` flag + explicit `Unregister`.

### B7. Complex-layer items measured before layout / fragile transform indexing — ✅ DONE (2026-08-06)

> `AddComplexLayerItem` now falls back to `Measure(infinite)` + `DesiredSize` when the resolved
> width/height is 0 (fresh element, no layout pass yet). The `Children[2]` indexing is gone:
> `LayerTag` gained a `PositionTransform` property, `AddComplexLayerItem` stores the
> `TranslateTransform` it builds there, and `Item_OnPositionChanged` mutates that transform's
> X/Y in place — no assumption about the group's structure, and no per-move allocation. If an
> element has no tagged transform (foreign tag), the position update is skipped instead of
> crashing on a bad cast.

`AddComplexLayerItem` (`:1926-1929`) reads `ActualWidth/ActualHeight` of possibly-never-measured
elements (0 for fresh elements without explicit `Width`/`Height`), so `AnchorFunction` and
`RenderTransformOrigin` compute from zeros — markers anchor wrong until repositioned.
`Item_OnPositionChanged` (`:2151`) hard-indexes `RenderTransform` `Children[2]` — valid only
while the 3-child structure built at `:1959-1967` is intact (`AddSpecialLineLayer:1556` shows
other code appending to the same group pattern).

**Fix direction:** `element.Measure(infinite)` fallback when unmeasured; replace `Children[2]`
with a named/looked-up `TranslateTransform`.

### B8. `_unitDistance` cached forever — wrong after DPI change — ✅ DONE (2026-08-06)

> Added an `OnDpiChanged` override that drops the cache and then raises `OnZoomChanged`, which is
> how the whole derived set (`MapScale`, `NearestGoogleZoomLevel`, tile infos, layer scale ranges)
> is recomputed everywhere else, followed by a `Refresh`. Guarded on `ActualWidth/Height > 0`
> because it can fire before layout, in which case there is nothing to recompute and
> `GetUnitDistance` picks the new dpi up on its next call regardless.


`GetUnitDistance` (`:817-839`) caches pixel size on first call. Per-monitor DPI: moving the
window to a different-DPI monitor silently corrupts every `ToScreenScale`/`ToMapScale` and
measurement conversion.

**Fix direction:** invalidate `_unitDistance` in an `OnDpiChanged` override.

### B9. Cancellation callbacks touch UI with `useSynchronizationContext: false` — ✅ DONE (2026-08-06)

> Three of the four callbacks do touch the visual tree and are now `useSynchronizationContext: true`,
> matching the `Measure` token that already did this deliberately: the drawing token (removes
> `drawingRectangle` from `mapView.Children` and clears layers), the select-point token
> (`gesture.End()` detaches mouse handlers), and the editing token (removes the editable feature
> layer). The bezier token is **intentionally left `false`** and now says so in a comment — it only
> touches a `TaskCompletionSource`, an `Interlocked` field and `cts.Dispose()`, so there is nothing
> to marshal.


`GetDrawing`'s `cts.Token.Register(..., useSynchronizationContext: false)` (`:4177-4209`)
mutates `mapView.Children` and layers. Works today because `Cancel()` only happens on the UI
thread; the flag documents the opposite. Any future worker-thread cancel corrupts the visual
tree.

**Fix direction:** drop the flag, or `Dispatcher.Invoke` inside the callback.

## Improvements — performance

### P1. `CurrentExtent` recomputed inside loops — ✅ DONE (2026-08-06)

> Hoisted in `ClearOutOfExtent` and in `AddComplexLayer`'s `Intersects` filter. Deliberately **not**
> hoisted at the raster-layer `Action` (old `:1222`): that one re-reads the *live* extent at
> dispatch time to detect layers that escaped the viewport, and the value it does need is already
> hoisted one line above.


`CurrentExtent` is a computed property (matrix inversion + 2 transforms, `:370-389`),
re-evaluated per canvas child in `ClearOutOfExtent` (up to 5× per child, `:2326-2348`) and per
item in `AddComplexLayer`'s `Intersects` filter (`:1903`). Hoist to a local before each loop —
these run on every pan/zoom.

### P2. Linear `Children.Contains` scans per complex item — ✅ DONE (2026-08-06)

> Both `AddComplexLayer` batch sites (the main item loop and the `CollectionChanged → Add`
> handler) now snapshot `mapView.Children` into a `HashSet<UIElement>` once per batch and pass
> it through `AddComplexLayerItem` → `AddToCanvasWithAnimation`; newly added elements go into
> the set so later items in the batch see them. The set is per-batch/local — no long-lived
> mirror of the canvas to keep in sync — which is safe because both methods have no other
> callers. Chose this over the doc's "flag on `Locateable`" alternative to avoid a
> cross-project change and stale-flag risk. The `CollectionChanged → Remove` branch keeps its
> `Contains` + `Remove` (small batches, and `Remove` is inherently O(n) anyway), as do the
> single-element `Contains` sites elsewhere in the file — they're not in loops.


`:1919`, `:2006`, `:2021` → O(items × children) per layer add. Track membership in a
`HashSet<UIElement>` or a flag on `Locateable`.

### P3. One 1-second animation clock per complex item — ✅ DONE (2026-08-06)

> Threshold approach (the doc's second option): a `MaxItemsWithAnimation` const (50). Both
> `AddComplexLayer` batch sites compute `animateBatch = count <= threshold` and pass it as a
> **separate flag** alongside the original `withAnimation`; `AddToCanvasWithAnimation` skips
> only the `DoubleAnimation` when `animateBatch` is false. `Flash(List<Point>)` does the same,
> with `AddFlash` gaining a `withAnimation` flag that skips creating the three animation
> clocks. Safe because `FillBehavior.Stop` means an animated add/flash ends at base
> opacity/scale anyway — a static add is the same end state, minus the transition.
>
> **Regression found and fixed same day:** the first version rerouted >50-item batches into
> `AddComplexLayerItem`'s *non-animated branch*, which has a different z-index policy than the
> animated one (it sets z-index only for `AlwaysTop` layers, and to `specialPointLayer.ZIndex`
> instead of `int.MaxValue`) — large complex batches (e.g. a long drawing item's vertex/edge
> labels) lost their z-index and broke drawing-item stacking/reordering. The threshold must
> only suppress the animation, never change which add-branch runs. The two branches'
> conflicting z-index policies are a pre-existing asymmetry left as-is.

`AddToCanvasWithAnimation` (`:2024-2045`) starts a `DoubleAnimation` per item — hundreds of
concurrent clocks after a big layer add; each `AddFlash` adds three more with 5× repeat
(`:2726-2749`). Animate a shared parent, or skip animation above an item-count threshold.

### P4. `UpdateZIndex` is O(layers × children) — ✅ DONE (2026-08-06)

> `RequestRearrangeLayerOrders` now calls a new `UpdateZIndexes(IEnumerable<ILayer>)` that
> builds a `HashSet<ILayer>` (with `ReferenceEqualityComparer`, preserving the old `==`
> semantics against any `Equals` override) and walks the canvas children once —
> O(layers + children). The single-layer `UpdateZIndex` remains for `RequestUpdateZIndex`,
> where one pass per call is already optimal.

`UpdateZIndex` scans all children per layer (`:969-985`); `RequestRearrangeLayerOrders`
(`:625-633`) calls it per layer after a project load. Build one layer→elements lookup per pass.

### P5. Basemap tiles render stretched between zoom levels — ✅ DONE (2026-08-06)

> Solved by exposing snapping as a real user setting rather than enabling the dead flag:
> `ZoomStepsPerGoogleLevel` = 1 snaps every step to a Google level (crispest); the default 2 makes
> every second notch land exactly on one. Surfaced in the Saba settings dialog and persisted.


Free-scale wheel zoom (see B1) means tiles rarely display at native resolution. Enabling the
dead `IsGoogleZoomLevelsEnabled` snapping — or exposing it as a user setting — gives crisper
basemaps and structurally eliminates the same-tile-list/empty-diff hazard class that caused the
2026-08 missing-tiles bug.

## Improvements — structure

### S1. Centralize zoom policy — ✅ DONE (2026-08-06)

> Two helpers, deliberately separate: `GetSteppedScreenScale(bool)` is the single policy for every
> stepped entry point (wheel, double-click, toolbar, right-click zoom-out, both degenerate
> `ZoomToExtent` branches) — it snaps *and* clamps. `ClampScreenScale(double)` only bounds, and is
> applied at the one place `ZoomToExtent` computes a scale from a bbox, which covers `ZoomAndCenter`,
> `FullExtent` and `ZoomToFeature`. Region zoom therefore keeps its free scale but can no longer run
> past the levels that have tiles.


One `ClampScreenScale` used by every zoom entry point (wheel, double-click, `ZoomToPoint`,
`ZoomAndCenter`) instead of a clamp that exists only in a dead branch. Pairs with B1.

### S2. Unify the two drag-rectangle state machines

`zoomRectangle` (`:3495-3572`) and `drawingRectangle` (`:4261-4388`) are near-identical
MouseDown/Move/Up machines. One `RectangleDragGesture` in the spirit of the existing
`ClickOrPanGesture` (`:2915-3051`) halves the surface.

### S3. Job cancellation

Covered by `mapviewer-job-cancellation.md`. Implementing it also absorbs B2's `async void`
render entry points and B9's ad-hoc token handling style.

### S4. Dead-code cleanup and file split

Large commented-out blocks (`:145-174`, `:2550-2564`, `:3432-3438`, many more), unused locals in
`mapView_MouseMoveForZoom` (`:3539-3541`), empty `PredefinedExtents_CollectionChanged` (`:804`).
The `#region` structure (navigation / rendering / drawing / editing / interaction sessions)
already maps cleanly onto `partial class` files.

## What is deliberately fine

The interaction-session layer (`IMapInteraction`, `ClickOrPanGesture`, token-based draw/edit
sessions with `Interlocked` swaps and stale-gesture guards, `:2812-3051` and `:3993-4250`) is
modern, cancellable, re-entrancy-guarded code — the model the navigation/render core should
converge on. Don't "improve" it while fixing the rest.

## Verification (when implementing)

1. Build `IRI.Maptor.Jab.Wpf` + `IRI.App.MakanNegarSaba` (close any running Saba first — it
   locks output DLLs).
2. B1/S1: wheel-zoom to both extremes — scale stops at the configured levels; tiles stay sane.
3. B3: spam `PanTo` / zoom-to-extent operations back-to-back — map always ends refreshed,
   callbacks always fire exactly once.
4. B4: point the basemap at an unreachable URL — tiles fail silently to the not-found image, no
   message boxes.
5. B6: sign out and back in (re-`Register` path) — mouse-move handlers fire once (breakpoint),
   provinces list not duplicated.
6. Regression sweep: fast wheel-zoom tile fill-in (2026-08 fix), DXF layer render, drawing and
   editing sessions, measure, right-click options.
