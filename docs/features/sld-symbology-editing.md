# SLD symbology editing

Feature reference for full OGC SLD 1.0.0 symbology editing from the map legend, with
per-project overrides of server-provided defaults. Implemented 2026-08-15 across the
`Sta.Ogc`, `Jab.Core`, `Jab.Wpf` tiers and the MakanNegar Saba WPF client. This document
records what was built, the design decisions and their rationale, and the exact code
surface — intended as the source for future standard docs (SRS/SDD sections, user guide).

## Problem and scope

Before this feature, clicking a layer's symbology swatch in `MapLegendItemView` opened a
three-field dialog (fill, stroke, stroke width) that flattened any multi-rule symbology to
a single style. A complete SLD editor (`SldEditorWindow` / `SldEditorViewModel`, localized
and unit-tested) existed in `Jab.Wpf` but had zero call sites. Server layers in Saba
receive their symbology as SLD XML embedded in layer metadata (`LayerSetting.LayerSld`,
delivered by `GET /LayerSetting/List`), and the `.mtproj` project model already carried
per-layer SLD override slots — but no UI could produce a custom SLD.

Scope decisions (agreed with the product owner before implementation):

| Decision | Choice |
|---|---|
| Legend entry point | Swatch click keeps the quick dialog; it gains an "Advanced (SLD)" button opening the full editor |
| Hardening | Full: data-loss fixes, parse-error surfacing, raster guards, Metro/RTL styling, live per-rule preview |
| Override semantics | `.mtproj` stores an SLD override only for layers the user actually restyled; "Reset to default" restores the server style |
| Server write-back | Out of scope — the `LayerSetting` API stays read-only; overrides are per-project only |

## User-facing behavior

1. **Quick dialog** (swatch click): fill/stroke/width as before, plus
   *Advanced (SLD)* (closes the quick dialog, opens the editor) and
   *Reset to default* (visible only when the layer carries a captured default).
2. **SLD editor** (`SldEditorWindow`): resizable 950×750 `LocalizedMetroWindow` (RTL-aware,
   Persian font), toolbar (Import / Export / Reset to default), rule list with live swatch
   previews, per-rule tabs (properties + scale range + simple filter, symbolizers, XML
   preview), footer with Apply / Cancel / OK. Apply pushes the SLD onto the layer and
   repaints the map without closing, so styles can be iterated visually.
3. **Filter and label property names** are editable combo boxes pre-filled with the
   layer's attribute field names (free text still accepted).
4. **Legend** swatch and the symbology-details popup refresh immediately after apply.
5. **Project save** (`.mtproj`): only layers whose symbology the user modified get an
   `SldXml` override; untouched layers keep tracking whatever default the server serves
   later. Loading a project replays overrides after sign-in and marks those layers
   modified, so re-saving keeps them.

## Architecture

Data flow for a Saba server layer:

- Load: `ApplicationPresenter.CreateLayerFromMetadata` parses `LayerSetting.LayerSld` via
  `SldHelper.TryParse`; on success `sld.ParseToSymbolizers()` becomes the layer's runtime
  symbolizers and the document is kept as `SymbolizableLayer.SourceSld`. On parse failure
  the layer falls back to the hex-color symbology (`HexFill`/`HexStroke`/…) and a
  `Trace` warning is written — the layer no longer silently disappears. Immediately after
  creation, `CaptureDefaultSymbology()` snapshots the style as `DefaultSld`.
- Edit: `DefaultActions.ShowSldEditorView(ownerWindow, layer, viewModel)` seeds
  `SldEditorViewModel.Create(layerName, SourceSld ?? GetSld(), fieldNames, geometryType)`
  and shows the editor modeless with the owner window. Apply/OK executes
  `symbolizable.ReplaceSymbolizers(sld.ParseToSymbolizers(), sld)`, sets
  `IsSymbologyUserModified = true`, and calls `MapViewModelBase.Refresh(false)` — the same
  repaint pair the project-override replay uses.
- Persist: Saba's `OnSaveProject` writes `ServerLayerOverride { AuxilaryId, IsVisible,
  Opacity, SldXml }` per server layer, with `SldXml` populated only when
  `IsSymbologyUserModified`. `OnProjectLoadedAsync` stashes overrides and
  `ApplyServerLayerOverrides()` replays them once layers exist (after sign-in).
  Imported (non-server) layers persist symbology via `LayerStateEntry.SldXml` as before.

Key components and their homes:

| Component | Location |
|---|---|
| SLD object model, `SldHelper.Parse/TryParse/Serialize/Save` | `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Ogc/SLD/1.0.0/` |
| SLD → runtime symbolizers (`ParseToSymbolizers`), runtime → SLD (`ParseToSld`, lossy) | `src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Extensions/SldExtensions.cs` |
| Layer symbology state (`SourceSld`, `DefaultSld`, `IsSymbologyUserModified`, `ReplaceSymbolizers`, `ResetSymbologyToDefault`) | `src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Layers/SymbolizableLayer.cs` |
| Editor view models | `src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/ViewModels/Symbology/Sld/` |
| Editor views (window, per-symbolizer editors, filter/scale editors) | `src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Views/Symbology/Sld/` |
| Quick dialog | `Views/Symbology/SymbologyView.xaml` + `ViewModels/Symbology/SymbologyViewModel.cs` |
| Host wiring (both dialogs, apply/reset actions) | `src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Common/Defaults/DefaultActions.cs` |
| Swatch rendering (legend and editor previews) | `src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Cartography/Legend/` (`LegendSwatchFactory`, `SldLegendBuilder`) |
| Project model override slots | `src/IRI.Maptor.Jab/IRI.Maptor.Jab.Core/Models/Project/` (`ServerLayerOverride.SldXml`, `LayerStateEntry.SldXml`) |
| Saba integration | `src/IRI.App/Barg/IRI.App.MakanNegarSaba/ViewModel/ApplicationPresenter.cs` (`CreateLayerFromMetadata`, `OnSaveProject`, `ApplyServerLayerOverrides`) |

## Design decisions and rationale

### Lossless editing of foreign documents

`SldEditorViewModel` keeps a deep clone of the loaded document (`_sourceSld`, cloned via a
serialize/parse round-trip — the same path every SLD takes anyway).
`ToStyledLayerDescriptor()` starts from a fresh clone each call and replaces only what the
editor edits; everything else (additional `NamedLayer`s, `UserStyle`s beyond the first,
all `UserLayer`s, layer constraints) round-trips byte-identical. When such content exists,
`HasPreservedContent` shows a notice in the editor header. At rule level, filters the
simple editor cannot express (spatial, logical, LIKE, BETWEEN), `LegendGraphic` and
`ElseFilter` are retained and re-attached on save.

### All FeatureTypeStyles of the primary style are editable

GeoServer-authored SLDs routinely hold **one rule per FeatureTypeStyle** inside a single
`UserStyle` (each FeatureTypeStyle is a compositing group). The editor therefore surfaces
the rules of *every* FeatureTypeStyle of the first `UserStyle` as one flat list. Each
`RuleViewModel` records `SourceFeatureTypeStyleIndex` and is written back into the same
slot on save, preserving the document's compositing structure; rules created in the
editor go to the last FeatureTypeStyle (rendered topmost); a FeatureTypeStyle left empty
by rule deletion is pruned (SLD requires at least one rule per style). Because save
always re-clones from the pristine loaded document, origin indices stay stable across
repeated Apply calls. Known consequence: reordering rules *across* FeatureTypeStyle
boundaries in the list does not change the saved drawing order (order within each
FeatureTypeStyle does follow the list).

### Default tracking and override-only-when-edited

`SymbolizableLayer` snapshots its creation-time style
(`CaptureDefaultSymbology()` → `DefaultSld`, a clone isolated from all later edits) and
tracks `IsSymbologyUserModified`. Both apply paths set the flag;
`ResetSymbologyToDefault()` hands the layer a clone of the snapshot and clears it. Saba
persists `SldXml` only for flagged layers, so untouched layers keep following future
server default changes. Backward compatibility: projects saved under the previous
snapshot-everything behavior mark all their layers modified on load, keeping their pinned
styles until the user resets a layer. Capture happens at layer creation and override
replay happens later, so reset always returns to the true server default, never the
project override.

### Error surfacing instead of swallowing

`SldHelper.Parse`/`Serialize` used to `catch { return null; }`. `TryParse(xml, out sld,
out error)` was added (with `Parse` delegating to it, old contract intact) and is used by
the editor's import (shows the actual XML error) and the Saba loader (logs and falls back
to hex colors). The former `sourceSld!` null-forgiving dereference in
`CreateLayerFromMetadata` is gone.

### Raster symbolizer guards

`SldExtensions.ParseSymbolizer` throws `NotImplementedException` for `RasterSymbolizer`
(no runtime renderer exists). Both parse loops now skip unsupported symbolizers instead of
failing the whole layer, and the editor hides the "add raster symbolizer" command unless
the layer's geometry type is raster or unknown (`SldEditorViewModel.CanAddRasterSymbolizer`).

### Live rule previews without UI-thread or test cost

`RuleViewModel.SwatchImage` renders the rule through `LegendSwatchFactory` (GDI-based, no
STA requirement) — the identical renderer the legend details popup uses. Generation is
lazy (a dirty flag, generated only when a binding reads the property, so headless usage
such as tests and project serialization never renders) and invalidation is debounced
(300 ms `DispatcherTimer` restarted on every symbolizer property change, collapsing
per-keystroke edits into one refresh).

### Dialog conventions

The editor follows the repo's dialog protocol (`RequestApplyAction`/`RequestCloseAction`
callbacks, wired by the host; message boxes raised via `RequestShowWarning`/`RequestShowError`
callbacks so the view model stays view-free and testable). The shared `DialogFooterView`
gained an optional third (tertiary) button — hidden by default, opted into by the SLD
editor (Apply) and the quick dialog (Advanced) — so existing two-button dialogs are
unaffected. The window uses the `IRI.Maptor.Styles.MetroWindow.Localized` style (RTL
`FlowDirection`, Persian font trigger) rather than `MetroWindow.Dialog`, which caps width
at 550 and disables resizing. The XML preview forces `FlowDirection="LeftToRight"` —
without it the RTL window mirrors the markup into unreadable text.

## Localization

Nine keys were added to the `Jab.Core` resx store (neutral + all 14 satellites: ar, az,
es, fa-IR, fr, hi, hy, it, ku-Arab-IQ, pt, ru, tr, ur, zh-CN), translated per-culture and
inserted by guarded text replacement per the Jab localization rules (UTF-8 BOM, CRLF,
manual escaping — never resx tooling):

- `dialog_common_apply`, `dialog_symbology_advanced`, `dialog_symbology_resetToDefault`
- `sldEditor_common_preservedContentNotice`
- `sldEditor_message_invalidSldFile`, `sldEditor_message_importError` (`{0}`),
  `sldEditor_message_exportError` (`{0}`)
- `sldEditor_filter_noFilter`, `sldEditor_filter_advancedFilter`

The editor view models read runtime strings via `LocalizationManager.Instance[...]`;
import/export dialog and message titles reuse the existing
`sldEditor_common_importTooltip`/`exportTooltip` keys. Pre-existing store fact: only fa-IR
tracks full key parity with neutral; the other satellites are ~127 keys behind and fall
back to English — this feature kept every file's gap unchanged.

## Tests

All in `tests/IRI.Maptor.Tst.Main/OGC/`:

- `SldEditorRoundTripTest` (pre-existing) — editor VM → XML → editor VM preserves all five
  symbolizer types, filter, scales, color map.
- `SldSafetyTest` — `TryParse` error reporting; multi-NamedLayer/UserStyle/UserLayer
  documents round-trip with edits landing and unedited parts intact; the
  one-rule-per-FeatureTypeStyle pattern (all rules editable, structure preserved, empty
  FeatureTypeStyle pruning, new-rule placement); raster symbolizers skipped not thrown.
- `SymbologyDefaultTrackingTest` — capture/reset/modified-flag semantics on a
  `SymbolizableLayer` stub, including snapshot isolation and repeated edit cycles.

## Known limitations

- **Symbolizer-level rebuild loss**: within a rule the user edits, symbolizer view models
  rebuild their SLD objects from editable properties, so `GraphicFill`, `GraphicStroke`
  and `ExternalGraphic` inside an *edited* rule's symbolizers are dropped. (Unedited
  styles/layers are preserved verbatim.)
- **Rendering gaps are pre-existing and unchanged**: spatial filter operators evaluate as
  pass-all stubs; `LabelPlacement`/`Halo` are parsed but not rendered; raster symbolizers
  are not rendered.
- **Label round-trip**: code-side label properties that SLD cannot express
  (`PositionFunc`, RTL flag) do not survive an SLD round-trip, so resetting a
  hex-fallback layer rebuilds labels from the SLD text symbolizer only (same behavior the
  project-override replay always had).
- The simple filter editor covers a single comparison; richer filters are preserved but
  shown as "advanced filter (not editable here)".
- Cross-FeatureTypeStyle rule reordering does not rewrite the saved drawing order (see
  design decision above).

## Deferred work

- **Server write-back**: an authorized-user path to persist an edited SLD into
  `LayerSetting.LayerSld` as the new default for everyone (needs a PUT endpoint,
  permissions, and admin UX; `LayerSettingController` currently exposes only `GET /List`).
- Advanced filter builder (nested AND/OR, spatial, LIKE, BETWEEN).
- Symbolizer-source retention to close the graphic-fill/external-graphic rebuild loss.
- Raster symbolizer rendering; label placement/halo rendering.
- Optionally: rewrite FeatureTypeStyle order when rules are reordered across groups.

## Related documents

- SLD object model: `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Ogc/SLD/README.md`
- Editor views: `src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Views/Symbology/Sld/README.md`
  (plus `IMPLEMENTATION_SUMMARY.md`, `USAGE_EXAMPLES.md` in the same folder)
- Project file feature (`.mtproj`): `src/IRI.Maptor.Jab/IRI.Maptor.Jab.Core/Models/Project/`
- Saba startup/auth/layer-loading sequence: `STARTUP_SEQUENCE.md` (repo root)
