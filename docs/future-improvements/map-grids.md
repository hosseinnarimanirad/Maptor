# Map grids — lat/long graticule and projected (UTM / Web Mercator / Lambert …) grids on the map

- **Status:** **all steps implemented 2026-09-01** (1, 2, 2b, 2c, 2d, 3, 4, 5). Three defects found
  by the user running it in the host application are fixed: recolouring moved only the subdivisions (2b); a grid
  could not be switched off and duplicated itself on every tick (2c — a group layer cannot be
  removed by identity); and the abbreviated UTM values were unreadable (2d). **Custom grids are
  deferred; see *Later*.**
- **Area:** `src/Core/IRI.Maptor.Core.Spatial` (grid engine), `src/Core/IRI.Maptor.Core.Persistence`
  (data sources), `src/Presentation/IRI.Maptor.Presentation.Wpf` (layers, view model, picker),
  the host application (ribbon + settings)
- **Risk:** step 1 none (new files only); steps 2–3 low — additive, following the MGRS grid
  pattern file for file; nothing existing is modified except `MapViewModelBase` and the host application's ribbon
- **Effort:** step 1 ≈ one session; steps 2 and 3 one each; steps 4–5 one together
- **Related:** [`mgrs-support.md`](mgrs-support.md) step 7 is the precedent this copies

## What the user asked for (2026-09-01)

Grid lines the user can switch on in any combination — geodetic **and** UTM at the same time, for
instance — drawn the way a printed sheet draws them: two weights (major / minor), the values
written on the lines at the edges of the view rather than in every cell, and the values
abbreviated to the digits that change rather than spelled out on every line. Lines must be
**polylines, never polygons**. Density follows the zoom.

Decisions taken in that conversation, so they are not re-opened by accident:

| Question | Decision |
|---|---|
| The `2⁹` figures in the middle of ONC cells | **Not now.** They are *Maximum Elevation Figures* — the highest terrain in the cell in thousands + hundreds of feet — derived from a DEM, not from the grid. the host application has no elevation feed today. |
| The `JF` letters in ONC cell corners | **Not now.** They are GEOREF one-degree quadrangle ids (a text encoding of lat/long, as MGRS is of UTM). Cheap to add later as an option of the geodetic grid — see *Later*. |
| UTM zone behaviour | **Follow the view's zone(s)**: each zone strip in view gets its own grid, lines cut at the zone boundary — exactly as the MGRS grid does. No pinned-zone mode. |
| Which edges carry the values | **All four.** Eastings / meridians along the bottom *and* top, northings / parallels up the left *and* right. |
| ONC-style minute ticks between labelled lines | Not decided; treated as **skip** until asked for. |
| Entry point | **Confirmed 2026-09-01: a ribbon `fluent:DropDownButton` with checkable items**, beside the MGRS toggle. The user first suggested Maptor's `MultiSelectItem` or an on-map scalebar-style widget; `MultiSelectItem` is an editable combobox plus an *Add* button plus removable chips with AND/OR mode — a filter-builder at least 200 px wide, right for step 3's dialog and wrong for a ribbon. Per-grid show/hide comes free from each grid's legend checkbox, so no on-map widget is needed. |

## Where this plugs into the existing code

Line anchors verified 2026-09-01 — they drift.

| Existing thing | Role |
|---|---|
| `Core.Spatial/Helpers/MgrsGridHelper.cs` | **the precedent.** Extent → cells + edge labels; `EdgeInset = 0.04`; per-zone walk of the UTM plane with clipping at the zone strip; `GetUtmBounds` samples a geodetic box's edges into UTM; `maxCells` ceilings checked *before* the loop |
| `Core.Spatial/Helpers/GraticuleHelper.cs` | the degree ladder (`IntervalLadderDegrees`: 30° … 1″), `ChooseInterval(span, minLines, maxLines)`, `FormatDegreeLabel`. Used only by `Core.Pdf/PdfMapComposer` — **left untouched**; the new geodetic scheme reuses the ladder |
| `Core.Persistence/DataSources/MemorySources/MgrsGridDataSource.cs`, `MgrsGridLabelDataSource.cs` | the data-source shape to copy: `VectorDataSource` over Web Mercator, `GetAsFeatureSetAsync(BoundingBox)` regenerates from the extent, `SearchAsync` throws |
| `Presentation.Wpf/Layers/MgrsGridLayers.cs` | the layer shape to copy: a `GroupLayer` of `VectorLayer`s, stroke-only `SimpleSymbolizer`, `LabelSymbolizer.Create(...)` with `positionFunc: g => g.GetCentroidPlusPoint()` |
| `Presentation.Wpf/Cartography/Symbologies/Strategies/SimpleSymbolizer.cs:30` | `SimpleSymbolizer(Func<Feature<Point>, bool> filter, VisualParameters)` — **major and minor are two filtered symbolizers on one line layer**, not two layers |
| `Presentation.Wpf/Models/Common/VisualParameters.cs:168` | `DashStyle` is a real, rendered property (`IndexLayers` sets `DashStyles.Dot`) |
| `Presentation.Wpf/Cartography/RenderingStrategies/DrawingVisualRenderStrategy.cs:298–356` | `DrawLabels`: one `FormattedText` per feature, **centred** on `PositionFunc`, white plate `#C8FFFFFF` behind it, culture = current. No rotation, no anchor, no halo |
| `Presentation.Wpf/Models/Common/VisualParameters.cs:621` | `CreateLabel(visibleRange, fontSize, foreground, fontFamily, positionFunc, isRtl)` — sets `IsSelected = true`, without which no label draws (see the MGRS doc) |
| `Presentation.Wpf/ViewModels/Map/MapViewModelBase.cs:7736–7776` | `_mgrsGridLayer` / `IsMgrsGridVisible` / `ToggleMgrsGridCommand` — layer held by reference, `AddLayer(ILayer)` (`:3421`) and `RequestRemoveLayer` (`:1531`) |
| `Presentation.Wpf/Models/GoTo/ProjectionPreset.cs` | the projection catalogue (Web Mercator, Mercator, TM, LCC 1P/2P, CEA; named NIOC / FD58 / Nahrwan systems) with `CreateSrs(ellipsoid, parameters)` — the picker for "projected grid" |
| `Core.SpatialReferenceSystem/MapProjections/SrsBase.cs` | `FromGeodetic<T>` / `ToGeodetic<T>` — every projection the grid can be drawn in speaks this |
| `Core.SpatialReferenceSystem/MapProjects.cs:212, 229, 870, 970` | `FindUtmZone` (boundary-correct since MGRS step 5), `CalculateCentralMeridian`, `GeodeticToUTM(point, ellipsoid, zone, isNorth)`, `UTMToGeodetic(point, ellipsoid, centralMeridian, isNorth)` |
| the host application `MainWindow.xaml:459–461` | the MGRS ribbon toggle — the new picker sits beside it |
| the host application `MainWindow.xaml:629–650` | the *Display* ribbon group of `GeneralSettings.*` toggles |
| the host application `Common/SettingsHelper.cs:36–46` | `GeneralSettingsKeys` — the allow-list any new `IGeneralSettings` property must be added to, or it silently never persists |
| `Presentation.Core/Properties/Resources*.resx` (×15), the host application `Properties/Resources*.resx` (×15) | every new UI string goes in all of them |

## Things this trips over

1. **A naming collision.** `GeodeticGridDataSource`, `UtmGridDataSource` and `GridLayer` already
   exist and are the **NCC sheet-index** grids (Iran's 1:250k / 1:50k / 1:25k sheets), not
   graticules. Everything new is prefixed `MapGrid` so the two never get confused.
2. **Labels are horizontal only.** The renderer centres a single `FormattedText`; there is no
   rotation. A printed sheet sets northings vertically up the side, but reading them horizontally
   is what QGIS / ArcGIS Pro do on screen and it is fine. Rotated text is *Later*.
3. **Data sources see an extent, not pixels.** `VectorDataSource.GetAsFeatureSetAsync(BoundingBox)`
   has no scale or viewport size, so density is chosen as "lines across the view", not
   "pixels between lines" — the same compromise the MGRS grid makes, and it behaves well because a
   window is always a few hundred to a couple of thousand pixels wide.
4. **Straight in UTM bows in Web Mercator.** A grid line of constant easting is a curve on the
   map, so lines are sampled (32 points) and each vertex re-projected. MGRS cells get away with
   4 samples per edge because a cell is small; a line spans the whole view.
5. **Cutting, not clamping, at a zone seam.** MGRS *clamps* sampled vertices to the zone strip,
   which is fine for a polygon (the clamped vertices lie on the boundary and the shape stays
   closed). Clamping a *polyline* would draw a spurious segment running along the meridian. The
   grid scheme drops vertices outside the strip and interpolates the crossing on the boundary.
6. **Web Mercator's edges.** Latitude past ±85.05° and longitude past ±180 produce infinities;
   the extent is clipped first, as `GraticuleHelper` does.
7. **Unicode superscripts for the small digits.** A topo sheet prints the 100 km digit small
   (`⁵34`). The renderer has one font size per label, so the small digit is a Unicode superscript
   character (U+2070–2079). Consolas carries them; if a fallback font ever renders them as boxes
   the formatter has a plain-digits switch.
8. **Two data sources, one grid.** Lines and labels are separate layers (they style apart), so a
   view change builds the grid twice. Cheap — a few dozen lines — but a one-entry cache keyed on
   `(extent, definition)` inside `MapGridHelper` halves it for free.
9. **Norway / Svalbard.** MGRS honours the irregular zones. A plain UTM grid does **not** — it
   uses nominal 6° strips, which is what a UTM grid means; the MGRS layer is where the exceptions
   belong.

## The design

### One definition, many grids

A **grid definition** is what the user picks; the engine turns a definition plus a view extent
into lines and labels.

```
MapGridDefinition
  Kind          Geodetic | Utm | Projected
  Srs           SrsBase?      – null for Geodetic and Utm; the projection for Projected
  Key           string        – stable id for settings: "geodetic", "utm", "webMercator", "lccNioc", …
  Title         string        – legend / picker text
  MajorInterval double?       – null = automatic from the extent
  LabelSides    Bottom | Top | Left | Right   (flags; default all four)
  LabelTier     int           – 0 for the first grid on the map, 1 for the second: pushes its labels inward so two grids never overprint
```

| Kind | Lines | Auto ladder | Minor | Label text |
|---|---|---|---|---|
| **Geodetic** | constant longitude / latitude; straight in Web Mercator so **2 vertices** | the degree ladder in `GraticuleHelper` | next finer ladder step that divides the major evenly (1° → 30′ or 20′ / 10′; 10′ → 5′ / 2′ …) | DMS via `FormatDegreeLabel`; **full on the first line of each side and whenever the degree changes, only the changing part (`30′`) otherwise** |
| **Utm** | constant easting / northing **per zone strip and hemisphere**, sampled in that zone's plane, cut at the strip; plus the **zone seam** meridian itself as a heavy line when one is in view | metres: 1-2-5 × 10ⁿ, 10 m … 1000 km | 1 → ÷5, 2 → ÷4, 5 → ÷5 (always round: 1 km → 200 m, 2 km → 500 m, 5 km → 1 km) | topo style: full `⁵34 000 mE` on the first line met per side per zone and whenever the 100·D digit rolls over; **principal digits** otherwise (`35`, `36`) |
| **Projected** (Web Mercator, Mercator, TM, LCC, CEA, named NIOC / FD58 / Nahrwan) | constant x / y in the projection plane, sampled and re-projected (Web Mercator's happen to be straight) | same metric ladder | same | same, with the unit `m` and no hemisphere letter |

**Principal digits, defined for any interval.** With major interval `M` and its decade
`D = 10^floor(log10 M)` (1 km, 2 km and 5 km all have `D = 1 km`; 10 km, 20 km, 50 km have
`D = 10 km`), a line at value `v` reads as the two digits `(v / D) mod 100`, zero-padded — for
`D = 1 km`, 534 000 → `34`, 535 000 → `35`. The digit above them, `(v / D) / 100 mod 10`, is the
one a sheet prints small; it goes in front as a superscript on the *full* labels.

**Automatic interval.** For the view's larger span, pick the coarsest ladder step that puts at
least `minLines` major lines across, stepping back one if that overshoots `maxLines`
(`GraticuleHelper.ChooseInterval`'s rule, parametrised). Start at 3–6; tune in step 4 against the
real window. Both grids on the map choose independently, so lat/long may be at 1° while UTM is at
10 km — which is right, they are different ladders.

**Four edges.** A vertical line (constant X) is labelled where it meets the bottom and top of the
view, a horizontal line at the left and right. Labels are point features inset `EdgeInset`
(0.03 for tier 0, 0.075 for tier 1) from the edge, so they hold still while panning and the second
grid's row sits inside the first's. The first-line-is-spelled-out rule runs **per side**, so a
reader can start from any edge.

**Polylines only.** Every line is a `LineString` feature with attributes
`Axis` (`X` / `Y`), `Kind` (`Major` / `Minor` / `ZoneSeam`), `Value` (native units), `Label`
(full text). Nothing is a polygon.

### The layers

`MapGridLayers.Create(definition)` → one `GroupLayer` (the legend entry, user-removable) holding:

| Sub-layer | Source | Symbolizers |
|---|---|---|
| lines | `MapGridDataSource` | `SimpleSymbolizer(f => Kind == Major, …)` solid ≈ 1.2 px; `SimpleSymbolizer(f => Kind == Minor, …)` ≈ 0.7 px, paler; `SimpleSymbolizer(f => Kind == ZoneSeam, …)` ≈ 2 px |
| values | `MapGridLabelDataSource` | `LabelSymbolizer` — major labels 12 pt; minor 10 pt via a second filtered symbolizer, if the label path honours filters (checked in step 2) |

Suggested default colours, one hue per grid so two can be told apart at a glance: geodetic a
chart blue (`#3060C0`), UTM near-black (`#262626`), any projected grid a purple (`#7A3E9D`). The
MGRS layer keeps its warm red. Labels take their grid's colour; the renderer's white plate behind
each label masks the line under it, which is what makes edge values legible.

### The view model and the picker

`MapViewModelBase` gets a `MapGridItems` collection — one `MapGridItemViewModel` per catalogue
entry (`Title`, `Definition`, `IsChecked`, the live `GroupLayer` or null) — built from a fixed
list (geodetic, UTM, Web Mercator) plus the *named* entries of `ProjectionPreset.CreateDefaults()`
(NIOC Clarke 1880, FD58, Nahrwan). Checking an item builds the layer and `AddLayer`s it;
unchecking `RequestRemoveLayer`s the instance it holds — the MGRS toggle's pattern, held by
reference so a renamed layer is still the one removed. Tiers are assigned in check order and
re-packed when one is removed. Removing the group from the legend must uncheck the item
(hook the existing layer-removed path).

**Recommended entry point** (to confirm): a `fluent:DropDownButton` "Grids" in the host application's *Tools*
ribbon group beside the MGRS toggle, `ItemsSource = MapGridItems`, each item a checkable menu
entry — the multi-select combobox the user asked for, in the ribbon's own idiom, no new control.
Show/hide of an individual grid is also the legend checkbox on its group, so an on-map widget is
not needed for that. The custom-parameter case ("a Lambert grid with these constants") is step 3's
"Custom grid…" item at the bottom of the same drop-down, which opens a small dialog reusing the
`ProjectionPreset` picker from the Go To window and appends a new item.

Why not `MultiSelectItem`: Maptor's control (`Views/MultiSelectItem/`) is an editable combobox
plus an *Add* button plus a row of removable chips with AND/OR mode — a filter-builder form
control, ~200 px wide at minimum. It would be a good fit inside the step-3 dialog, not in a
ribbon.

**Persisting the choice:** `IGeneralSettings.MapGrids_SelectedKeys` (comma-joined `Key`s), added to
the host application's `GeneralSettingsKeys` allow-list, restored at startup. Step 3.

## Step plan

### Step 1 — the grid engine, with tests ✅ implemented 2026-09-01

`src/Core/IRI.Maptor.Core.Spatial/Helpers/MapGrids/`, namespace
`IRI.Maptor.Core.Spatial.Helpers.MapGrids`. **Twelve new files; no existing file touched**, so
nothing in the tree can regress.

| File | Contents |
|---|---|
| `MapGridDefinition.cs` | `MapGridKind`, `MapGridSide` (flags), and the definition itself with factories `Geodetic()`, `Utm()`, `Projected(srs, key, title)` |
| `MapGridModels.cs` | `MapGridAxis`, `MapGridLineKind` (Major / Minor / ZoneSeam), `MapGridLine`, `MapGridLabel`, `MapGrid` |
| `MapGridOptions.cs` | the knobs, all defaulted; `GetInset(tier)` |
| `MapGridLadders.cs` | `Degrees` (reused from `GraticuleHelper`), `Metres` (1 000 km – 10 m in 1-2-5), `ChooseMajor`, `MinorOf` |
| `MapGridLabelFormatter.cs` | the geodetic and metric label rules, `GetGeodeticHighPart` / `GetMetricHighPart`, `ToSuperscript` |
| `MapGridGeometry.cs` | *(internal)* plane bounds by edge sampling, Liang-Barsky polyline clipping, crossing interpolation, the ground-span estimate |
| `MapGridLabelPlacer.cs` | *(internal)* where each value goes, and whether it is spelled out |
| `MapGridPlaneWalker.cs` | *(internal)* the shared metric walk: one plane in, lines and labels out |
| `GeodeticGridScheme.cs` | *(internal)* meridians and parallels |
| `ProjectedGridScheme.cs` | *(internal)* one projection over the whole view |
| `UtmGridScheme.cs` | *(internal)* the walk run per zone strip and hemisphere, plus the seams |
| `MapGridHelper.cs` | the public entry point: `Create`, `ChooseMajorInterval`, `ToClippedGeodetic`, the cache |

`tests/IRI.Maptor.Tests/CoordinateSystems/MapGridTest.cs` — **53 tests, all passing.** Full suite
afterwards: **1 960 tests, 59 failing, every failure inside the documented pre-existing GeoJson /
MVT / PersonalGdb suites** — nothing new.

**Mutation-checked.** Three deliberate breakages, each caught: clipping UTM lines to the whole view
instead of to the zone strip (fails the seam test); keying the spelled-out/abbreviated run on
axis alone instead of axis *and* side (fails both four-edge label tests); returning the first
qualifying subdivision from `MinorOf` instead of the finest (fails 10 tests). Thirteen failures in
all; reverted.

#### What the implementation settled that the plan left open

1. **UTM is the projected walk, run in a loop.** `MapGridPlaneWalker` takes a plane and emits its
   lines; `ProjectedGridScheme` calls it once, `UtmGridScheme` once per zone strip × hemisphere.
   The zone seam is therefore a property of *where the walk is called from*, not a special case
   buried inside it.
2. **UTM picks its interval from the ground span, not a projected one.** Transverse Mercator
   diverges badly more than a few degrees off its central meridian, so measuring a wide view inside
   one zone's plane would hand the ladder a meaningless number. Projected grids keep the plane span,
   which is the correct basis there: Web Mercator's plane distances are inflated by 1/cos(latitude)
   and it is *plane* spacing that decides how many lines cross the screen.
3. **Plane bounds are taken per strip.** A strip is exactly the ±3° band UTM is designed for, so the
   projection stays well conditioned however wide the view gets.
4. **Minor lines are not numbered** (`MapGridOptions.LabelMinorLines = false`). A topographic sheet
   numbers the principal lines only; five times as many numbers would crowd the margin into
   illegibility. The option exists, off.
5. **A bug found in the clipper, and fixed.** Handing an untouched segment end back as
   `a + 1·(b - a)` is a rounding error away from `b`, and it nudged *every* vertex in the grid about
   a tenth of a millimetre off the line it was computed to sit on. Untouched ends are now returned
   verbatim, and a sampled vertex lands on its line to **2 µm** — double-precision noise and nothing
   else.
6. **Cutting at a seam costs about 2 m, on the widest view.** The crossing is interpolated along a
   sampled chord, so a cut point sits slightly off the true curve. Measured worst case on a 4° view:
   **1.93 m — a twentieth of a pixel** at that zoom. The error falls quadratically as the samples
   shorten while a pixel only halves, so it is *largest* where a pixel is hundreds of metres wide.
   That is what makes 32 samples a line enough, and why the ends are not snapped back onto the curve
   afterwards. Both conversions the engine leans on — the Lambert grid and Web Mercator — round-trip
   with **zero** error at double precision, so this is the only approximation in the pipeline.
7. **`MapGridDefinition.Projected` rejects a geographic system.** A "projected" grid over
   `SrsBases.GeodeticWgs84` would draw a grid of degrees while every label called them metres, so it
   throws and points at `Geodetic()`.
8. **`MinorOf` is defined once for both ladders:** the finest ladder step below the major that
   divides it evenly into no more than five parts. That yields 1 km → 200 m, 2 km → 500 m,
   5 km → 1 km, 1° → 15′, 30′ → 10′, 10′ → 2′, and null at the finest step. Requiring the divisor to
   be a ladder member too means zooming in *promotes* minor lines to major ones instead of shifting
   the whole pattern.
9. **The cache is keyed on the definition's mutable parts** — interval, label sides, tier — not just
   its reference, because the UI edits one definition instance in place when the user changes the
   interval. `ClearCache()` exists for tests only.

#### Measured behaviour

Around Tehran, where a degree of longitude is about 90 km:

| view | geodetic major | geodetic minor | UTM major | UTM minor |
|---|---|---|---|---|
| 4° | 1° | 15′ | 100 km | 20 km |
| 1° | 20′ | 5′ | 20 km | 5 km |
| 0.5° | 10′ | 2′ | 10 km | 2 km |

Both grids step finer monotonically as the view narrows, and never coarsen.

### Step 2 — data sources, layers, ribbon picker ✅ implemented 2026-09-01

| File | |
|---|---|
| `Core.Persistence/DataSources/MemorySources/MapGridDataSource.cs` | one **LineString** feature per line; `Kind` / `Axis` / `Value` / `Zone` attributes |
| `Core.Persistence/DataSources/MemorySources/MapGridLabelDataSource.cs` | one Point feature per value; `Label` is the label attribute |
| `Presentation.Wpf/Layers/MapGridStyle.cs` | one hue and three weights per grid kind |
| `Presentation.Wpf/Layers/MapGridLayers.cs` | `Create(definition, style, options)` → the layer (a `GroupLayer` at first; **one `VectorLayer` since step 2c**) |
| `Presentation.Wpf/Models/Map/MapGridCatalog.cs` | the six offered grids |
| `Presentation.Wpf/ViewModels/Map/MapGridsViewModel.cs` | what is on, and the label tiers |
| `Presentation.Wpf/ViewModels/Map/MapGridItemViewModel.cs` | one drop-down entry |
| `Presentation.Wpf/ViewModels/Map/MapViewModelBase.cs` *(modified)* | `MapGrids` / `MapGridItems`, the reconcile hook in `Layers_CollectionChanged`, `ReconcileMgrsGridLayer` |
| the host application `MainWindow.xaml` *(modified)* | the `fluent:DropDownButton`, beside the MGRS toggle |
| `Presentation.Core/Properties/Resources*.resx` (×15) | `cmd_general_mapGrids`, `layer_mapGrid_geodetic`, `layer_mapGrid_utm`, `layer_mapGrid_lines`, `layer_mapGrid_labels` |

**14 new tests** — 4 on the data sources in `MapGridTest.cs`, 10 in
`tests/IRI.Maptor.Tests/Mapping/MapGridsViewModelTest.cs` — for **67 map-grid tests**, all passing.
Full suite: **1 974 tests, 59 failing, all in the documented pre-existing `GeoJson_ComplianceTest`
and `MvtTileDecoderTest` suites**; none are map-grid tests.

**Mutation-checked:** commenting out the two reconcile calls in `Layers_CollectionChanged` fails
exactly the two tests that cover them, and nothing else.

#### What the implementation settled

1. **The ribbon container is `Fluent.MenuItem`, and a test now pins it.** This was the one part of
   the feature that compiling cannot check: XAML compiles an `ItemContainerStyle` to BAML without
   ever resolving its `TargetType` against the container, so a wrong type is silent at build time
   and throws the first time the menu is opened.
   `FluentDropDownButton_GeneratesTheContainerTheRibbonStyleTargets` invokes
   `DropDownButton.GetContainerForItemOverride` by reflection and asserts the result, so a future
   Fluent.Ribbon upgrade that changes the container breaks a test rather than the ribbon.
2. **Two properties keep the menu open, not one.** `StaysOpenOnClick` is the WPF `MenuItem`
   behaviour and `IsDefinitive` is Fluent's own "this click finishes the command" flag; a
   multi-select menu needs both off.
3. **The MGRS toggle's latent bug is fixed in the same pass.** `IsMgrsGridVisible` was
   `_mgrsGridLayer is not null` on a `CanUserDelete = true` group: delete it from the legend and the
   ribbon went on reporting the grid as visible, so the next click removed an already-gone layer
   instead of putting the grid back. Both it and the new picker now reconcile against `Layers` from
   `Layers_CollectionChanged` — chosen over subscribing from the view models because `Layers` is
   *replaced wholesale* when the map view attaches, which would strand a subscription on the old
   collection.
4. **Checked state is derived, never cached.** `MapGridItemViewModel.IsChecked` reads through to
   whether the layer is still on the map. One truth, so the menu cannot disagree with the legend.
5. **Three weights, one layer.** Major, minor and zone seam are three *filtered* `SimpleSymbolizer`s
   over the same feature set rather than three layers — the renderer computes `filteredFeatures`
   once per symbolizer and uses it for shapes and labels alike.
6. **The catalogue reads `SrsBases` directly rather than `ProjectionPreset`.** It is the same set of
   instances the Go To picker offers for these named entries, without dragging a dialog's
   editable-parameter machinery into a menu that needs none of it. Step 3's custom-grid dialog is
   where `ProjectionPreset` belongs.
7. **Both sub-layers share one `MapGridDefinition` instance**, which is what will let step 3 change
   an interval with nothing to rewire — and `MapGridHelper`'s one-entry cache means the label source
   is served from the line source's build rather than recomputing the grid.
8. **`GroupLayer.AddSubLayer` inserts by `ZIndex`**, so with both sub-layers at the default the
   second one added lands *first* in `SubLayers`. Harmless — the MGRS group behaves the same — but
   tests must not assume insertion order.
9. **Re-packing a tier calls `map.Refresh(false)`.** The definitions are already live in the data
   sources, so the map only has to be asked to draw again; the extent has not changed, so nothing is
   refetched.

#### What still needs a real run

None of the following can be checked from a test host, and none has been seen yet:

- **Consolas rendering the Unicode superscripts** rather than boxes. `MapGridOptions.UseSuperscripts`
  is the fallback switch if it does not.
- **The colours and weights on a real basemap**, and whether two grids' label rows are far enough
  apart at the default `TierInset` of 0.045. This is step 4's job.
- **The seam behaviour on screen** near longitude 48, and that the graticule runs unbroken through
  it while the UTM grid restarts.

Run the host application, switch *Lat/Long* and *UTM* on together from the Grids drop-down, and pan across
longitude 48 near Qom.

### Step 2b — recolouring a grid recolours all of it ✅ implemented 2026-09-01

**Defect, found by the user running the app: changing a grid's colour appears to do nothing.**

Confirmed by driving the real layer in a test host. The line layer's symbolizers came out as
`[Simple(t=0.7, o=0.5), Simple(t=1.2, o=0.85), Simple(t=2, o=0.9)]` — subdivisions, principal,
seam — and `SymbolizableLayer.GetMainOrDefaultSymbology()` is
`_symbolizers.FirstOrDefault(v => v is SimpleSymbolizer)`, so it returned the **subdivision**.
`DefaultActions.GetDefaultShowSymbologyView` seeds the dialog from that *and* writes back to it, so
a colour change landed on the thinnest, faintest lines in the grid and the principal lines never
moved. It read as "nothing happened".

**The fix keeps one layer per grid**, on the user's instruction — splitting the weights into three
sibling layers was proposed and **rejected**: a grid should be one thing in the legend, not three.

Two changes, both inside this feature's own code:

1. **The principal weight goes first**, so the dialog seeds from and edits the weight the user can
   actually see. Stacking follows the same order — subdivisions and the seam now draw over the
   principal lines. Subdivisions never coincide with a principal line, only cross one, and at 50 %
   opacity a crossing is a faint dot; the seam genuinely belongs on top.
2. **`MapGridSymbologyLink`** makes the other weights follow the first. Colour is copied verbatim;
   thickness is scaled by the ratio each follower started at, so a grid drawn heavier stays a grid
   instead of collapsing into three lines of equal weight; opacity is left alone, because that is
   what separates a subdivision from a principal line and the dialog does not set it.

It is driven by `INotifyPropertyChanged` on `VisualParameters` rather than by intercepting the
dialog, because the dialog mutates the parameters it is handed **in place** — there is no call to
intercept. That also means the link holds for any other route that edits them.

4 new tests (71 map-grid tests in all): the dialog's seed symbolizer is the principal weight;
recolouring changes all three; thickening keeps the weights apart and in order; opacity survives.
Mutation-checked — removing the `Attach` call fails exactly the two tests that cover it.

#### A second defect, still open

The dialog's *Advanced* button opens the SLD editor, which applies with
`ReplaceSymbolizers(sld.ParseToSymbolizers(), sld)`. Symbolizer filters cannot survive that round
trip — `SymbolizableLayer.SourceSld`'s own doc comment says rule names, **filters** and mark shapes
are lost — and `ParseToSymbolizers` then sets `IsFilterPassed = f => true` for every filterless
rule. All three symbolizers would draw *every* line, collapsing the three weights into three
overlapping strokes.

**Not fixed, and not fixable without one of the two things already ruled out** — splitting the
weights into separate layers, or teaching the SLD round trip to carry a filter (a change to shared
symbology code that every layer type would feel). The cheap containment, if it ever bites, is to
suppress the *Advanced* option for grid layers; that needs one new flag on `ILayer` and one line in
`DefaultActions`. Left alone for now because the simple dialog now does what the user wanted and
the SLD editor is a power-user path.

### Step 2c — one layer per grid, and it can be switched off ✅ implemented 2026-09-01

**Two defects, one cause, both found by the user running the app:** unticking a grid did not take it
off the map, and ticking it again added a second copy every time.

**The cause is in shared code, not in this feature.** `LayerManager.Remove` tests its rule only
against *non-group* layers:

```csharp
if (layer.IsGroupLayer)      { Remove(layer.SubLayers, rule, …); }   // group itself never tested
else if (… && rule(layer))   { layers.Remove(layer); }
…
if (layer.IsGroupLayer && layer.SubLayers.Count == 0) { layers.Remove(layer); }
```

A group is recursed into and never matched, so removing one *by identity* does nothing: the rule
`lyr => lyr == theGroup` matches no sub-layer, the sub-layers stay, `SubLayers.Count` never reaches
zero, and the empty-group branch never fires. The grid stayed drawn; the view model — which derives
checked state from live membership — saw the layer still in `Layers` and left the item ticked; and
because `Hide` had already cleared its own reference, the next tick built a second grid.

**The fix is what the user asked for two rounds earlier, taken literally: one `VectorLayer` per
grid.** The lines and the values now come from one data source and one layer, told apart by the
`Kind` attribute every symbolizer already filtered on — three line weights plus one label
symbolizer. `MapGridLabelDataSource` is deleted. A plain vector layer is matched by the rule, so
removal works, and the legend shows exactly one row per grid.

**A second, independent reason the grid stayed visible.** `MapViewer.RemoveLayer` — what
`RequestRemoveLayer` is wired to — only drops the layer from the layer manager; it does not clear
what is already drawn on the canvas. `Hide` now calls `MapViewModelBase.ClearLayer(layer,
remove: true, forceRemove: true)`, which clears the visuals *and* removes the layer.

**The same bug was live in the MGRS grid overlay**, which this feature was modelled on. Fixed the
same way at the user's request — see `mgrs-support.md`, step 8.

Tests: a grid layer must not be a group (`Assert.False(layer.IsGroupLayer)`, with the reason in the
message), ticking on and off three times leaves exactly one layer or none and goes through the
clearing path, and the merged data source carries lines as `LineString` and values as `Point` with
the text, never mixing the two under one symbolizer.

### Step 2d — metric values are written out in full ✅ implemented 2026-09-01

**"The long numbers for UTM is not ok."** Offered four readings of what was wrong, the user picked
*show every value in full, plainly*: the abbreviation was the problem, not the length.

| | first line of an edge | the rest |
|---|---|---|
| was | `⁵34⁰⁰⁰ mE` | `35` `36` `37` |
| now | `534000` | `535000` `536000` `537000` |

The topographic-sheet convention the grid was built on — the value spelled out once per edge, the
hundred-kilometre digit and trailing zeros set small as Unicode superscripts, everything after it
abbreviated to the two digits at the interval's decade — is right on paper and wrong here. On paper
the collar carries the full grid reference and the abbreviation has somewhere to be read against; on
a screen with no collar, `35` is two digits with no unit and no anchor.

It works for the **graticule** and stays there, because the short form keeps its own unit mark: a
bare `30′` still reads as minutes. Only the metric families changed.

Removed rather than left switchable, so there is one way for a metric label to read:
`MapGridOptions.UseSuperscripts`, `MapGridLabelFormatter.ToSuperscript`, `GetMetricHighPart` and
`GetDecade`. `FormatMetric` now takes only the value. No unit suffix either — position already says
which axis a value belongs to, eastings along the bottom and top and northings up the sides.

One consequence worth knowing: every metric value is now six or seven characters instead of two, so
the crowding rule from step 4 drops more of them. `MinLabelSeparationX` at 5 % of the view is about
a `534000` wide in a thousand-pixel window, which is the right order — but it is the first number to
turn if the margin now looks thin.

Tests: the plain form for five values, and culture invariance — no thousands separator and no
Persian digits, because a value read off the map has to be typeable back into a coordinate box. The
"first surviving value on an edge is spelled out" test moved to the graticule, which is now the only
family that abbreviates.

### Step 3 — remember which grids were on ✅ implemented 2026-09-01

**Custom grids are deferred** — the user's call, 2026-09-01: "forget the custom grids for now, we
may implement it later." The dialog, the `ProjectionPreset` picker and editable projection constants
all moved to *Later*, which left this step as just the persistence.

| File | |
|---|---|
| `Presentation.Core/Data/Settings/IGeneralSettings.cs`, `GeneralSettings.cs` | `MapGrids_SelectedKeys`, default empty |
| `Presentation.Core/Models/Settings/GeneralSettingsModel.cs` | the notifying pass-through |
| `Presentation.Wpf/ViewModels/Map/MapGridsViewModel.cs` | `RestoreFromSettings()`, and a private `Persist()` called from show, hide and a legend-driven removal |
| `Presentation.Wpf/Models/Map/MapInitializationHelper.cs` | the restore call |
| the host application `Common/SettingsHelper.cs` | the key added to `GeneralSettingsKeys` |

**Keys, not indices.** A grid added to or removed from the catalogue later would otherwise restore a
different one; an unrecognised key is simply dropped.

**Saving is one assignment.** The host already saves off the settings' `PropertyChanged` — the host application's
`ApplicationPresenter` does exactly that — so writing `MapGrids_SelectedKeys` *is* the save path.
A guard suppresses the write while a restore is running, so a restore cannot rewrite what it just
read.

**Restore is called from `MapInitializationHelper.InitializeMapAsync`**, after `MapViewer.Register`
has wired the layer delegates — restoring a grid means adding a layer, and the grids view model is
built the first time the ribbon binds to it, which is not guaranteed to be late enough. Every WPF
host gets it, not just the host application. It is idempotent.

**The trap this walks into was already documented in `mgrs-support.md`:** the host application's
`SettingsHelper.GeneralSettingsKeys` is an explicit allow-list filtering **both** save and load, so a
new `IGeneralSettings` property that is not listed there is silently never written and never read
back, with no error. The key is listed.

8 new tests (79 map-grid tests in all): the keys are recorded on switch-on, switch-off and a legend
delete; restore brings the grids back in catalogue order so the label tiers are stable; restore is
idempotent and does not rewrite the setting; unrecognised, empty and whitespace keys are ignored.
Mutation-checked — suppressing the write fails the round-trip test.

One thing a test could not cover: the single call site in `MapInitializationHelper` is wiring, not
logic, and reaching it needs a real `MapViewer`. Verified by reading.

**Found while writing the tests:** `MapViewModelBase.GeneralSettings` is **null** until
`InitializeSettings` runs, and its setter is private — so a test double has to call
`InitializeSettings(null, null, null, null)`. The production paths here are all null-guarded.

### Step 4 — cartographic polish ✅ implemented 2026-09-01

Split into what is objectively decidable and what is taste. Only the first half was built; the
second is listed below rather than guessed at.

#### Values no longer print on top of each other

The one real defect this step found. A grid crowds its own margin in two places no choice of
interval prevents, because both are accidents of where the view happens to sit:

- **a UTM zone seam**, where the last easting of one zone, the first easting of the next, and the
  seam's own caption all land within a few kilometres of each other;
- **the corners**, where the row of eastings along the bottom runs into the column of northings up
  the left.

`MapGridLabelPlacer` now rejects a value that would land within `MinLabelSeparationX` × `…Y` of one
already written — 5 % and 3.5 % of the view, roughly a label's width and height in a window about a
thousand pixels across. Both axes have to be close for it to count, since two values far apart along
an edge do not overlap however near they are to the same latitude.

**Two orderings are load-bearing, and both are pinned by tests.**

1. **The collision check runs before the run state is updated.** A suppressed value must not consume
   the spelled-out slot, or the next line along that edge would print bare digits with no full
   reference anywhere to read them against. Whichever value actually survives first on an edge is
   the one written in full.
2. **Zone seams are emitted before the grid lines**, so where the margin is crowded it is a grid
   value that gives way and not the caption naming the two zones — the one label on the map a reader
   cannot work out from the others. Drawing order is unaffected: the renderer stacks by symbolizer,
   and the seam's is last. Reverting this ordering fails
   `TheZoneSeamCaptionSurvivesACrowdedMargin`, so it was a real bug and not a precaution.

Crowding only ever removes numbers. Every line is still drawn.

#### Persian digits: no change, and why

The grid's values stay in Latin digits, and that turns out to need no code either way.
`GdiBitmapRenderStrategy` converts a label's digits only when the label is right-to-left **and the
whole string parses as a number**; `DrawingVisualRenderStrategy`, which these layers use, never
converts. A grid value is neither — `51°30′E` and `⁵34⁰⁰⁰ mE` carry degree marks, hemisphere letters
and a unit — so both paths already agree.

It is also the right answer independently. Converting would half-translate the metric form: the
principal digits would turn Persian while the Unicode superscripts around them stayed Latin, giving
`⁵۳۴⁰⁰⁰`. And it matches the decision already taken for MGRS, that a grid reference is an identifier
rather than a quantity.

#### Left to taste

Nothing below is known to be wrong. Each is a number that was chosen from reasoning rather than from
looking, and each is one line in `MapGridOptions` or `MapGridStyle`:

| If it looks wrong | Turn |
|---|---|
| lines too sparse or too dense | `MinMajorLines` / `MaxMajorLines` (3 and 6) |
| the two grids' rows of numbers too close, or too far in | `TierInset` (0.045), `EdgeInset` (0.03) |
| numbers still crowded, or too many dropped | `MinLabelSeparationX` / `…Y` (0.05, 0.035) |
| a grid's colour, or the weights' relative thickness | `MapGridStyle.Geodetic` / `.Utm` / `.Projected` |
| subdivisions too faint or too strong | `MinorOpacity` (0.5) |

One known weak case worth a look on imagery: the UTM grid's near-black `#262626` on a dark
basemap. The *labels* are safe either way — the renderer paints a white plate behind every one — but
the lines could disappear. A casing (a wider translucent stroke under each line) is the standard fix
and would be a fourth weight per grid.

### Step 5 — docs ✅ implemented 2026-09-01

Following the MGRS precedent, which put its documentation in library READMEs and `docs/features/`
rather than in the Barg document set:

- **`Core.Spatial/Helpers/MapGrids/README.md`** — the engine: the three kinds, the ladders and the
  subdivision rule, how labels are placed and abbreviated, why lines are sampled, how UTM is the
  projected walk run per zone, the measured accuracy, and the limits. Every code sample matches an
  assertion in `MapGridTest` — the 4° view really does choose 1° and 15′.
- **`Core.Spatial/README.md`** — a feature bullet and a *See also* link.
- **`docs/features/map-grids.md`** — the user-facing note: what the drop-down does, how to read the
  abbreviated values, what the zone seam means, how to restyle a grid, **the warning not to use the
  symbology dialog's *Advanced* button on a grid layer**, and what is deliberately not included.

Nothing was added to `src/IRI.App/Barg/Docs/` — that set's changelog covers changes to the documents
in that folder, and the MGRS feature added nothing there either.

## Not in scope (decided)

- **Maximum Elevation Figures** — needs a DEM source the host application does not have; revisit if one arrives.
- **GEOREF letters** — encoder is trivial, but not wanted yet.
- **Minute ticks** between labelled lines — not asked for.
- **Pinned UTM zone** extended across zone boundaries — the user chose follow-the-view.
- **Rotated (vertical) edge labels** — needs a renderer change; horizontal reads fine on screen.
- **Grids in the `.mtproj` project file** — belongs to that feature's plan.
- **Replacing the PDF composer's `GraticuleHelper`** with the new engine — sensible later, not now.
- **The MGRS grid** stays its own toggle; folding it into the drop-down is a one-line change
  whenever wanted.

## Later (cheap once the engine exists)

- **Fixing `LayerManager.Remove` itself**, so that a group layer *can* be removed by identity. Both
  this feature and the MGRS overlay work around it by being single layers, which is the right shape
  for them anyway — but the next feature that genuinely wants a group will hit the same wall. The
  change is small (test the rule on group layers too, and clear their sub-layers from `_allLayers`)
  and every layer type would feel it, so it wants its own think and its own tests.

- **Custom grids** — deferred by the user 2026-09-01, was step 3: a "Custom grid…" item at the
  bottom of the drop-down opening a dialog that reuses the Go To window's `ProjectionPreset` picker
  (family, ellipsoid, projection constants) plus interval, label-side and colour options, appending
  the result to `MapGridCatalog`'s list at run time. `MapGridDefinition.Projected(srs, key, title)`
  already takes anything an `SrsBase` can express, so the engine needs no change — this is a dialog
  and a list, nothing more.

- GEOREF ids in the geodetic grid's cells at 1° and 15° (the ONC `JF`).
- A "Grids" entry for MGRS in the same drop-down.
- On-map overlay widget (scalebar-style) if the ribbon turns out too far from the map.

## Verification

- Step 1: `dotnet test tests/IRI.Maptor.Tests/IRI.Maptor.Tests.csproj --filter FullyQualifiedName~MapGridTest`
  — build the test project directly, not the solution (see `solution-build-masks-errors`).
- Step 2 onward: run the host application, switch on *Lat/Long* and *UTM* together, pan across longitude 48 near
  Qom — the UTM grid must restart at the seam with a heavy seam line, lat/long must be unbroken,
  the two label rows must not overlap, and zooming from country to street must step both grids
  finer without ever stepping coarser.
