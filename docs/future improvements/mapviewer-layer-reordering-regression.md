# OPEN BUG: layer reordering totally broken (regression, 2026-08-05/06 changes)

- **Status:** OPEN — reported 2026-08-06, cause not yet isolated
- **Symptom (user-reported):** reordering layers no longer works ("totally broken"); drawing
  items were reported first, then layer reordering in general. Exact repro not yet captured —
  **first debugging step is to pin it down**: which UI action (legend chevrons? drawing-items
  panel? drag?), which layer types, whether the order is wrong immediately or reverts after a
  refresh/zoom/layer-add.
- **Workspace state when parked:** working tree **clean at `18403a06d`** (2026-08-06 merge).
  All uncommitted 2026-08-06 session work (P2/P4/P3/B7 from
  `mapviewer-bugs-and-improvements.md`, plus a same-day z-index repair to P3) was **discarded
  by the user** — so the breakage being observed comes from **committed** code, i.e. the
  2026-08-05/06 commits below, not from the discarded session edits.

## Suspect commits (newest first)

| Commit | Date | What it touched |
|---|---|---|
| `14e2c9949` fix: mapviewer | 08-06 | MapViewer.xaml.cs (B4–B6, B8, B9 batch: named Register handlers, DPI cache, cancellation contexts) |
| `370052171` fix: MapViewer | 08-06 | MapViewer.xaml.cs (B1–B3, P1, P5, S1 batch: zoom clamp/stepping, async Task, Pan port, extent hoists) |
| `741497e01` feat: mapviewer google zoom settings | 08-06 | zoom settings plumbing |
| `d86a10f18` fix(MapViewer): basemap rendering problem | 08-05 | StopUnnecessaryJobs keep criterion + quick wins: **`UpdateAndGetLayers` materialization**, **`UpdateLayerCanMoveUpDown` index tracking** |
| `0e0852bae` fix(saba): consider layer orders in save project | 08-05 | **layer-order persistence; "TocOrder is authoritative; ZIndex derived" model in `MapProjectService`** |

## Prime hypothesis: TocOrder vs ZIndex divergence

`LayerManager.ArrangeZIndex` derives **every** layer's `ZIndex` sequentially from `TocOrder`
(`GetOrderedLayers().Where(Parent is null).OrderBy(l => l.TocOrder)`), and it runs on **every
`LayerManager.Add`** and on `RearrangeZIndexes()`. The move-up/down paths
(`MapViewModelBase.MoveLayerUp` ~`:2649`, `SwapDrawingItems` ~`:2431`) swap **`ZIndex`
directly**. If they do *not* also swap `TocOrder`, then the next `ArrangeZIndex` (any layer
add, any project-load rearrange) recomputes ZIndex from the unchanged TocOrder and **reverts
the user's reorder**. `0e0852bae` made TocOrder authoritative for project save/load — check
whether it (or the project apply path) changed who maintains TocOrder on a move.
**Check first:** breakpoint `ArrangeZIndex`; after a move-up, compare the two layers'
`TocOrder` vs `ZIndex`; then trigger any refresh/add and see if the swap survives.

## Facts about the reorder machinery (traced 2026-08-06, still valid at `18403a06d`)

- Legend move: `MoveLayerUp/Down` → swap → `RequestUpdateZIndex?.Invoke(first/second)` →
  `MapViewer.UpdateZIndex(ILayer)` (~`:1058`) — scans canvas children, matches
  `tag.Layer == layer` (reference equality), applies `layer.ZIndex`.
- Drawing items: `SwapDrawingItems` swaps `ZIndex` + `DrawingItems.Move` + the same
  `RequestUpdateZIndex` per item. `DrawingItemLayer` ctor sets `ZIndex = int.MaxValue` — if two
  items both still have int.MaxValue, the swap is a visual no-op and stacking falls back to
  canvas child order.
- `RequestRearrangeLayerOrders` is invoked **only** from `MapProjectService` (project
  load/apply); it calls `RearrangeZIndexes()` then reapplies per-layer.
- Complex/point elements (incl. text drawing items) are tagged with their **inner
  `SpecialPointLayer`**, which is *not* in the LayerManager → `UpdateZIndex` never matches
  them; their z-index comes only from add time (`AddToCanvasWithAnimation`).
- Pre-existing committed asymmetry in `AddComplexLayerItem`: animated adds set
  `int.MaxValue` (AlwaysTop) / `specialPointLayer.ZIndex` (else); non-animated adds set a
  z-index **only** for AlwaysTop, and to `specialPointLayer.ZIndex`, not int.MaxValue.
  Vertices/mid-vertices use the non-animated path; edge lengths/labels the animated one.
- During the discarded session it was verified that replacing the rearrange loop with a
  single-pass set lookup is semantically identical — the (discarded) P4 change was ruled out
  as a cause by reasoning, but a clean-tree retest is what actually rules session changes out,
  which is where things stand now.

## Debug plan

1. Capture the exact repro (action, layer type, immediate-vs-reverts-later).
2. Test the TocOrder hypothesis (breakpoint `ArrangeZIndex`, inspect after a move).
3. If not conclusive: `git bisect` between `befb144a2` (2026-08-05, pre-suspects — confirm
   reorder works there) and `18403a06d`, using the captured repro as the test. The five
   suspect commits above are the candidate range; `0e0852bae` and `d86a10f18` are the most
   likely for *layer* reordering, the two big 08-06 MapViewer commits for anything
   drawing/complex-item related.

## Related

- `mapviewer-bugs-and-improvements.md` — B7/P2/P3/P4 remain open again after the session
  revert; if re-implemented, remember the P3 pitfall: an animation-skip threshold must only
  suppress the `DoubleAnimation`, never route items into the other add-branch (different
  z-index policy — this briefly broke drawing-item stacking on 2026-08-06 before being fixed,
  then everything was reverted anyway).
- `mapviewer-slow-network-tile-blocking.md` — separate confirmed issue + plan, unaffected.
