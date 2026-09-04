# MGRS — Military Grid Reference System support

- **Status:** all seven steps implemented 2026-08-29 (committed). Step 3 was rebuilt from
  scratch after the display-mode approach was rejected, and step 4 was rescoped once partial
  references came up. **Step 8 added 2026-09-01: the grid overlay could not be switched off.**
  Only UPS polar support remains deferred.
- **Area:** `src/Core/IRI.Maptor.Core.SpatialReferenceSystem` (core), then
  `src/Presentation/IRI.Maptor.Presentation.{Core,Wpf}` + the host application (the panel option), then
  `src/Core/IRI.Maptor.Core.Common` (text parsing, step 4)
- **Risk:** steps 1–3 low — new files plus one additive, default-off panel field;
  step 5 touches shared projection code used by every UTM display
- **Effort:** steps 1–3 ≈ two sessions; steps 4–6 ≈ one more

## The key architectural point

**MGRS is not a map projection — it is a text encoding of UTM/UPS coordinates.**

There is no continuous x/y plane behind it, so it must *not* derive from `MapProjectionBase`:
`FromGeodetic<TPoint>(TPoint) → TPoint` is meaningless for a value that is a string. It belongs
as an encoder/decoder alongside `MapProjects`. It is deliberately **not** a
`CoordinateDisplayMode` either — see step 3 for why that was tried and rejected — only an
optional field on the coordinate panel, plus a `CoordinateTextParser` hook for input.

```
39S WV 12345 67890
│  │  │  └──────┴── easting / northing digits inside the 100 km square (0–5 digits each)
│  │  └─────────── 100 km square identifier (column letter, row letter)
│  └────────────── latitude band letter (C–X, skipping I and O)
└───────────────── UTM zone number (1–60)
```

## Where this plugs into the existing code

| Existing thing | Role |
|---|---|
| `MapProjects.cs` | static projection math — `GeodeticToUTM`, `UTMToGeodetic`, `FindUtmZone`, `CalculateCentralMeridian` |
| `MapProjections/*` | thin `SrsBase`/`MapProjectionBase` wrappers over that math — **MGRS does not go here** |
| `CoordinateDisplayMode` + `CoordinateHelper.Format(webMercator, mode, options)` | the display/copy path, returns `(string x, string y)` — **MGRS stays out of it** |
| `CoordinatePanelView` `CurrentHeight` / `IsHeightAvailable` | the optional-field pattern MGRS copies |
| `IGeneralSettings` + the host application `SettingsHelper.GeneralSettingsKeys` | where the on/off setting lives, and the allow-list it must be listed in |
| `Core.Common/Helpers/CoordinateTextParser.cs` | free-text coordinate input, used by `GoToViewModel` |
| `tests/IRI.Maptor.Tests/CoordinateSystems/` | xUnit tests |

## Things MGRS trips over in the current code

1. **`MapProjects.FindUtmZone` is off by one at exact zone boundaries.** It computes
   `30 + ceil(lon / 6)`, so `FindUtmZone(48)` returns 38 — but longitude 48.0 belongs to zone 39
   (zones cover `[6n − 186, 6n − 180)`). Same at 0, 6, 12, … MGRS encoding is very sensitive to
   this, so the MGRS code carries its own boundary-correct zone function until step 5 fixes the
   shared one.
2. **Norway / Svalbard zone exceptions.** Zone 32 widens over band V (Norway), and zones
   31/33/35/37 are irregular over band X (Svalbard). Standard MGRS requires them; nothing in the
   codebase handled them.
3. **No polar stereographic projection exists**, so UPS (bands A, B, Y, Z outside 80°S–84°N)
   cannot be encoded. Deferred — see below.
4. **`CoordinateHelper.Format` returns an `(x, y)` pair** but MGRS is a single string. Resolved by
   keeping MGRS out of that path entirely — see step 3.

## The algorithm, as implemented

Validated against two published references, one in an odd zone and one in an even zone, so both
halves of the row-letter rule are covered:

| Reference | Coordinates | MGRS |
|---|---|---|
| Eiffel Tower | 48.8584 N, 2.2945 E | `31U DQ 48251 11932` (odd zone) |
| Washington Monument | 38.8895 N, −77.0353 W | `18S UJ 23383 06479` (even zone) |

What those two pin down exactly is the **lettering** — band, column set, row offset — and the
**pure-integer paths**: decoding a reference to UTM, and encoding UTM back to a reference, both
reproduce the published strings digit for digit. The digits produced when encoding *from a
latitude/longitude* are a few tens of metres off those strings, which is the published landmark
coordinates being quoted to a different precision than the MGRS strings were derived from — not a
projection error. The worldwide round-trip below bounds the projection instead.

**Latitude band** — `C`–`X` skipping `I` and `O`, 8° each from −80°, except `X` which is 12°
(72°–84°).

**100 km column letter** — depends on `(zone − 1) mod 3`:
`0 → A–H`, `1 → J–R`, `2 → S–Z` (each 8 letters, skipping `I` and `O`).
Index into the set is `floor(easting / 100000) − 1`.

**100 km row letter** — the 20-letter alphabet `A`–`V` skipping `I` and `O`.
Index is `(floor(northing / 100000) + (zone even ? 5 : 0)) mod 20`.
The even-zone offset of 5 is why the combined pattern repeats every 6 zones, not 3.
`10 000 000 / 100 000 = 100 ≡ 0 (mod 20)`, so the southern-hemisphere false northing does not
disturb the cycle — the lettering runs continuously across the equator.

**Decoding the northing** is the only non-obvious direction: the row letter fixes the northing
only modulo 2 000 000 m, so the latitude band supplies a minimum northing and the candidate is
raised by 2 000 000 until it clears it.

## Step plan

### Step 1 — core encoder / decoder ✅ implemented
`src/Core/IRI.Maptor.Core.SpatialReferenceSystem/MapProjections/Mgrs/`

- `MgrsPrecision.cs` — `Km100 = 0` … `M1 = 5`
- `MgrsCoordinate.cs` — readonly struct: zone, band, column/row letters, easting, northing,
  precision; `ToString(bool withSpaces)`
- `MgrsBands.cs` — the band table, the letter sets, the band → minimum-northing table, and the
  boundary-correct zone function including the Norway/Svalbard exceptions
- `MgrsConverter.cs` — `FromGeodetic` / `ToGeodetic` / `TryToGeodetic` / `FromUtm` / `ToUtm` /
  `TryParse` / `Parse`

Scope: **UTM range only (80°S – 84°N)**; outside it the `Try*` methods return `false` and the
non-`Try` ones throw. No existing file touched, so nothing can regress.

### Step 2 — tests ✅ implemented
`tests/IRI.Maptor.Tests/CoordinateSystems/MgrsTest.cs` — **137 tests, all passing.**

Deliberately structured so most assertions do **not** depend on transverse-Mercator accuracy:

- **decode** (`ToUtm`) and **encode-from-UTM** (`FromUtm`) are pure integer math → exact assertions
- **encode-from-geodetic** asserts the grid zone and 100 km square exactly, and leaves the digits
  to the round-trip tests
- **round-trip** lat/lon → MGRS(1 m) → lat/lon, swept worldwide (≈ 500 positions)
- precision levels, parse/format tolerance, malformed input, Norway/Svalbard, band boundaries,
  southern hemisphere, the equator, and the `FindUtmZone` off-by-one cases

**Measured worst worldwide round-trip error at 1 m precision: 1.39 m.** A 1 m reference names the
square's south-west corner, so pure truncation alone accounts for up to √2 ≈ 1.414 m — the
projection itself contributes under 3 cm.

The suite was mutation-checked: flattening `GetRowOffset` to a constant 0 (removing the even-zone
five-letter offset) fails 7 tests.

### Step 3 — the coordinate-panel option ✅ implemented (redesigned 2026-08-29)

**MGRS is not a `CoordinateDisplayMode`.** The first attempt made it one, which forced it into
every `switch` that consumes the panel's selection — export SRS, clipboard, CSV, the icon and
description converters — because that whole pattern assumes an x/y pair and a projectable SRS.
Thirteen files changed for what is really one extra field on a panel. That version was reverted in
full.

It is now an **optional field on the coordinate panel**, following the existing *height* field
exactly: a value plus a gate, both dependency properties on `CoordinatePanelView`, the pair of
`TextBlock`s bound through `ElementName=root`, and the label pulled from the shared localization
store. One deliberate difference — `CurrentHeight` is supplied by the host because only the app
can look up elevation, whereas MGRS is derived from the position the panel already receives, so
`CurrentMgrs` is filled in by `SetCoordinates` rather than bound in.

| File | Change |
|---|---|
| `Views/Map/CoordinatePanelView.xaml.cs` | `ShowMgrs` (bool DP, default false) and `CurrentMgrs` (string DP); `UpdateMgrs` called from `SetCoordinates` |
| `Views/Map/CoordinatePanelView.xaml` | label + value block copied from the height block, declared first among the right-docked children so it reads last on the line |
| `Presentation.Core/Properties/Resources*.resx` (×15) | `map_coordinatePanel_mgrs` = `MGRS` |
| `Data/Settings/{IGeneralSettings,GeneralSettings}.cs`, `Models/Settings/GeneralSettingsModel.cs` | `CoordinatePanel_ShowMgrs`, default **false** |
| the host application `Common/SettingsHelper.cs` | the new key added to `GeneralSettingsKeys` |
| the host application `MainWindow.xaml` | `ShowMgrs` bound on the panel; a `ToggleButton` in the Display ribbon group |
| the host application `Properties/Resources*.resx` (×15) | `app_saba_ribbon_showMgrs` |

`CoordinatePanelViewModel`, `SpatialReferenceItem` and `SpatialReferenceItems` are **untouched**,
and so is `CoordinateDisplayMode`. The other five hosts (those applications) need no change — `ShowMgrs` defaults to false.

`Resources.Designer.cs` needed no edit this time: both labels resolve through
`LocalizationManager`'s string indexer, not `nameof`. The reverted version only needed Designer
entries because `SpatialReferenceItems` used `nameof(srs_…)`.

Two details that keep the mouse-move path honest: `UpdateMgrs` returns immediately when the gate is
off (this runs on every mouse move), and outside 80°S–84°N the field goes blank rather than
throwing. The digits are never converted to Persian numerals — unlike `SpatialReferenceItem`,
which does convert — because a grid reference is an identifier carrying letters, and converting
half of it would break copy-paste into anything expecting the standard form.

#### The trap this design walks into

`SettingsHelper` filters **both** save and load through an explicit `GeneralSettingsKeys` allow-list.
A new property on `IGeneralSettings` that is not listed there is silently never written and never
read back — no error, the setting just never sticks.

`MahAppsThemeMode` was in exactly that state, which is why the host application's dark/light choice did not survive
a restart: the load, apply and save chain was all present and correct
(`App.xaml.cs` applies it at startup, `ThemeSelectionViewModel` writes it, `ApplicationPresenter`
reacts and saves), and only the allow-list entry was missing. Added 2026-08-29 alongside this
work.

### Step 3b — tests ✅ implemented

`tests/IRI.Maptor.Tests/Mapping/CoordinatePanelMgrsTest.cs` — 6 tests on an STA thread via
`WpfTestHost`, driving the real control: gate off → nothing computed; gate on → `39S WV…` for
Tehran; past the top and bottom of the grid → blank, no throw; both defaults off.

Mutation-checked: removing the `ShowMgrs` gate fails 1 test, swapping the longitude/latitude
arguments fails 3. The latter matters because `MapViewer.CurrentPoint` is
`ScreenToGeodetic(…)` — a swapped pair would silently put Tehran in the wrong zone.

Full suite after the change: **1738 tests, 59 failing, every failure inside the documented
pre-existing GeoJson / EsriJson / EsriShape / MVT suites** — nothing new, and the 58/59 wobble is
the known flakiness in those same parameterized round-trips. 143 MGRS-related tests pass
(137 converter + 6 panel).

### Step 4 — MGRS input and “go to a square” ✅ implemented

A partial reference names a **region, not a point**, which is why this is its own panel rather
than a branch of the Go To dialog. `39` is a whole zone, `39S` a grid zone cell, `39S WV` a 100 km
square, `39S WV 53516 39501` a metre.

**Core** — `MgrsConverter` now parses every level and resolves any of them to an extent:

| Added | Notes |
|---|---|
| the band and square groups in `MgrsRegex` are optional | so a reference may stop early |
| `TryParseParts` | the shared parser behind both `TryParse` and the extent API |
| `GetBoundingBox` / `TryGetBoundingBox` | the region a reference names, as a geodetic box |
| `MgrsBands.GetGridZoneLongitudeRange` / `GetZoneLongitudeRange` / `GetWidestZoneLongitudeRange` | cell widths, including the irregular ones |

`TryParse` keeps its old contract — it yields an `MgrsCoordinate`, so it still requires at least a
100 km square. Only the extent API takes the coarser two.

Three things the implementation has to get right:

- **A square with no band is not a legal reference**, but the regex reaches that reading anyway by
  backtracking past the now-optional band, so `39WV` is rejected explicitly rather than by the
  pattern.
- **A UTM square is not a latitude/longitude rectangle** — its edges bow. The box is found by
  walking the edges, not just the corners; for a 100 km square at Tehran's latitude that bulge is
  about 1.1 km, so corners alone would cut the square off.
- **Longitude is measured continuously either side of the zone's central meridian**, so a square
  spilling over the antimeridian yields a box running past ±180 rather than one that appears to
  wrap around the world.

Grid zone cells honour the exceptions: `31V` is [0, 3], `32V` is [3, 12], `31X` is [0, 9],
`33X` is [9, 21], and `32X` / `34X` / `36X` are rejected because they do not exist. A bare zone
number reports the widest span the zone ever reaches, so `32` is [3, 12] rather than [6, 12].

**UI** — a dedicated modeless panel, built on the Go To window's pattern
(`LocalizedMetroWindow` + a body `UserControl` + `DialogFooterView`):

| File | |
|---|---|
| `ViewModels/Dialogs/MgrsGoToViewModel.cs` | reference in, extent out, one `ZoomToCommand` |
| `Views/Mgrs/MgrsGoToView.xaml` (+ `.cs`) | one box and one status line |
| `Views/Dialogs/MgrsGoToMetroWindow.xaml` (+ `.cs`) | modeless, Esc closes, focus starts in the box |
| `Common/Defaults/DefaultActions.cs` | `GetDefaultMgrsGoToAction` |
| `Models/Map/MapInitializationHelper.cs` | assigns it after `Initialize` |
| `ViewModels/Map/MapViewModelBase.cs` | `RequestShowMgrsGoToView` + `MgrsGoToCommand` |
| `Presentation.Core/Properties/Resources*.resx` (×15) | six `dialog_mgrs_*` keys |
| the host application `MainWindow.xaml` | a ribbon button beside Go To |

`RequestShowMgrsGoToView` is a field assigned *after* `Initialize` rather than a new parameter on
it, because every application presenter overrides that signature; adding a fifth argument would
have touched all of them for nothing.

The status line reports the region and its size in invariant-culture Latin digits, and turns red
on a reference that does not resolve. It stays empty while the box is empty, so an untouched panel
does not read as an error.

**`CoordinateTextParser` was not touched**, which was the point: `39S 534123 3950123` matches its
UTM pattern too, since `S` is both a band letter and a hemisphere letter. That collision never
arises now, and the hazard is recorded here in case anyone folds MGRS into the shared parser later.

**Not built, by decision:** no outline is drawn and nothing persists after the zoom. Worth
revisiting once step 7 exists — with the grid drawn, the square you land on is visible anyway.
`GetOutline` (the true quadrilateral, densified) is the piece that would be needed.

52 new tests: 40 in `MgrsTest.cs` for the extent API, 12 in `Mapping/MgrsGoToViewModelTest.cs` for
the panel. Full suite **1841 tests, 58 failing**, every failure inside the documented pre-existing
suites.

### Step 5 — fix the shared UTM zone helpers ✅ implemented

Both corrected private copies inside the MGRS code are gone: `MgrsBands.GetZone` now delegates to
`MapProjects.FindUtmZone` and only layers the Norway/Svalbard exceptions on top, and
`MgrsConverter` uses `MapProjects.CalculateCentralMeridian` directly.

| Function | Was | Now |
|---|---|---|
| `FindUtmZone` | `30 + ceil(lon / 6)` — answers the zone to the *west* at every exact multiple of six (`FindUtmZone(48)` → 38, `FindUtmZone(0)` → 30) | `floor(normalized / 6) + 31` after normalizing to [-180, 180) |
| `CalculateCentralMeridian` | 183–357 for zones 1–29 | `6 * zone - 183`, signed degrees on [-177, 177] |
| — | — | new public `NormalizeLongitude` |

Zones 30–60 are unchanged, so nothing in active use shifts — Iran is 38–41. Only the western zones
move, and their old values were broken wherever they were used: `GoToViewModel.UtmZoneHint`
displayed “183° E” for zone 1, `CoordinateSystemExtensions.AsEsriPrjFile` wrote that value into
`.prj` files, and `GeodeticToUTM(point, ellipsoid, zone, …)` handed the transverse Mercator
formulas a 360-degree longitude difference.

**A hang was found and fixed on the way.** The old implementation normalized with
`while (lambda < 0) lambda += 360;`, which never terminates for `double.NegativeInfinity`.
`NormalizeLongitude` is arithmetic rather than a loop, and `FindUtmZone` now throws
`ArgumentOutOfRangeException` for any non-finite longitude — which is what the old code already did
for NaN and +∞, just not for -∞.

Two smaller contract changes: `CalculateCentralMeridian` rejects zone 0 (it used to return 177) and
throws `ArgumentOutOfRangeException` rather than `NotImplementedException`; and longitude 180
answers zone 1, not 60, because the antimeridian normalizes to -180 and the half-open rule puts
every boundary in the zone to its east.

`tests/IRI.Maptor.Tests/CoordinateSystems/UtmZoneHelpersTest.cs` — 51 tests over the boundaries,
turn-shifted longitudes, the non-finite contract, and a round-trip asserting every zone's central
meridian finds its own zone back.

### Step 6 — docs ✅ implemented

- `MapProjections/README.md` — an MGRS section: why it is not in the projections table, the
  anatomy of a reference, worked `MgrsConverter` examples, the three lettering rules, the Norway
  and Svalbard exceptions, and the coverage limit.
- `IRI.Maptor.Core.SpatialReferenceSystem/README.md` — MGRS in the feature list and the see-also.
- `docs/features/goto-dialog/README.md` — the MGRS future-work line rewritten now that the encoder
  exists, naming the partial-reference question and the `UtmRegex` ordering hazard, and split from
  the USNG item it was bundled with.

Every code sample in the README was executed and its output pasted back rather than written from
memory.

### Step 7 — MGRS grid overlay on the map ✅ implemented

A single layer whose square size follows the zoom: grid zone cells → 100 km → 10 km → 1 km →
100 m → 10 m.

**Cells are polygons, not grid lines**, and that decision is what made the hard part tractable.
MGRS squares are metric inside a UTM zone, so their edges are not straight in Web Mercator and the
grid restarts at every zone boundary. As polygons each zone simply contributes its own cells, and
the seam falls out of clipping each cell to its zone's longitude strip — drawn as continuous lines
those seams would have to be stitched by hand.

| File | |
|---|---|
| `Core.Spatial/Helpers/MgrsGridHelper.cs` | `MgrsGridLevel`, `MgrsGridCell`, `MgrsGrid`, `ChooseLevel`, `Create` |
| `Core.Persistence/DataSources/MemorySources/MgrsGridDataSource.cs` | one polygon feature per cell, labelled with its reference |
| `Presentation.Wpf/Layers/MgrsGridLayers.cs` | the `VectorLayer` — stroke-only symbolizer plus a centred label |
| `Presentation.Wpf/ViewModels/Map/MapViewModelBase.cs` | `ToggleMgrsGridCommand`, `IsMgrsGridVisible` |
| `Presentation.Core/Properties/Resources*.resx` (×15) | `layer_mgrsGrid_title`, `cmd_general_toggleMgrsGrid` |
| the host application `MainWindow.xaml` | a ribbon toggle beside the MGRS panel button |

`MgrsConverter` gained the public API the generator needs, which was previously locked inside the
`internal` `MgrsBands`: `BandLetters`, `GetBandLatitudeRange`, `GetGridZoneLongitudeRange`,
`GetZone`.

**How it differs from the NCC precedent it copies.** `IndexLayers` registers one layer per fixed
scale and gates each with a `ScaleInterval`. This is one layer, always visible, whose *data source*
picks the level from the extent it is handed — which is what "automatic by zoom" has to mean if
the user is not to manage six layers.

Measured behaviour around Tehran, where a degree of longitude is about 90 km:

| view | level | cells |
|---|---|---|
| 60°, 20° | grid zone | — |
| 6° | 100 km | 49 |
| 0.5° | 10 km | 30 |
| 0.05° | 1 km | 42 |
| 0.0006° | 10 m | — |

At the zone 38/39 seam, `38S QE` ends at longitude 48.0000 and `39S TV` begins there — they meet
exactly, with no overlap and no gap.

Two approximations, both deliberate and documented in the source:

- **Cell edges are sampled, four points a side**, because a straight line in UTM bows in Web
  Mercator. The bow is far below a pixel at any zoom where the cell is large enough to see.
- **Clipping at a zone boundary clamps the sampled vertices' longitude** rather than solving each
  edge against the meridian. That is exact in the limit and already sub-pixel at four samples.

A `maxCells` ceiling (4000 by default) means a pathological extent returns a truncated grid rather
than trying to build millions of cells; the estimate is checked before the loop, not after.

#### Labelling, the way a map sheet does it

The first cut captioned every cell with its full reference — forty-two squares all reading
`39S WV 36 47`. No map does that. The conventions are: a square's own identifier printed once
inside it, grid *values* printed once per line rather than per cell, and those values set against
the edge of the sheet rather than scattered through the middle.

**A second pass was needed.** Bare digits are unreadable on their own: `36` means nothing without
knowing which square it counts within. On paper that context lives in the *collar* — "Grid Zone
Designation 39S, 100,000-m Square Identification WV" — printed outside the neatline. A screen
overlay has no collar, so the square's name has to go on the map.

Three label families, three sub-layers under one `GroupLayer`:

| Level | Square name | On the lines |
|---|---|---|
| grid zone | `36P` `37P` `38P` | — |
| 100 km | `39S UT` `39S UU` | — |
| 10 km | `39S WV` | `39S WV 2` then `3` `4` `5` |
| 1 km | `39S WV` | `39S WV 35` then `36` `37` `38` |
| 100 m | `39S WV` | `39S WV 361` then `362` `363` |

Line values are the principal digits of the easting or northing inside the 100 km square, padded to
the level's digit count — exactly the digits that position contributes to a full reference.
Eastings sit along the bottom of the view and northings up the left, four percent in from the edge,
so they hold still while the map is panned. The **first line met inside each square is spelled out
in full**, which is what a sheet does in its corners and shows the reader how a bare `36` composes
into `39S WV 36`.

**Square names are placed at the centre of the square's *visible part*, not of the square.** This
is the detail that makes it work at both ends of the zoom: a grid zone cell is 6° × 8° and a
100 km square is 100 km across, so at most zooms one of them is bigger than the view and its true
centre is off screen — which would put the only piece of context where nobody can see it. Anchoring
to the visible part keeps exactly one name per square in sight, whether the view sits inside one
square or spans four. Nothing is written inside the cells themselves any more; every label is a
point feature, so each family can be positioned and styled on its own.

One consequence worth knowing: cell digits and line values are deliberately *not* the same set.
Cells snap outward so they cover the view; line values snap inward so they stay on screen. The
outermost cell's line is therefore off the edge and goes unlabelled.

#### Two pre-existing rendering bugs found underneath this

**No label anywhere in the application was being drawn.** Both render strategies gate a label on
`Param.IsInScaleRangeAndSelected(1 / mapScale)`, which is `VisibleRange.IsInRange(…) && IsSelected`.
`VisualParameters.CreateLabel` builds its result through an object initializer instead of the
constructor every other factory calls with `isOn: true`, so `IsSelected` stayed at its default of
`false` and the gate rejected every label — the MGRS grid, the NCC sheet indexes, all of them. One
line in `CreateLabel`. `tests/IRI.Maptor.Tests/Mapping/LabelSymbolizerTest.cs` guards it: removing
the line again fails all five of its tests.

#### The label backing plate

`DrawingVisualRenderStrategy` draws a translucent white plate behind every label. For
left-to-right text it placed the plate's top-left at the label's centre point while drawing the
text half its own size up and to the left — so every label in the application carried a white box
floating half a label down and to the right of it. The right-to-left branch was correct, because
`DrawText` takes the top-*right* corner for RTL and the code already accounted for that. Both
branches now derive the plate from the same text origin. This affects every labelled layer,
including the NCC sheet indexes.

#### The live readout in the MGRS panel

The grid alone still cannot say what the whole reference is, so the panel shows the position under
the pointer at one metre, with its own copy button. No new plumbing was needed:
`MapViewModelBase.CurrentPoint` is already published geodetic on every mouse move by
`MapViewer.MouseMove` → `FireMouseMove`, and had no consumers; the panel just subscribes to
`PropertyChanged`. The subscription is wired in `Create` and torn down in `Dispose`, so the view
model stays testable without a map. Off the grid the last good reading is kept rather than blanked,
which would otherwise flicker as the cursor crosses the poles or the map edge.

Note the coordinate panel already offered this — the `CoordinatePanel_ShowMgrs` toggle from step 3,
off by default.

38 tests in `tests/IRI.Maptor.Tests/CoordinateSystems/MgrsGridTest.cs`: the zone seam, the widened
Norway cell, the missing Svalbard cells, the equator straddle, that the level never steps backwards
as the view narrows, and the whole labelling contract above — including that a square name lands
inside the view when the square is larger than it.

## Deferred — UPS polar support

Bands `A`, `B` (south of 80°S) and `Y`, `Z` (north of 84°N) need Universal Polar Stereographic,
which needs a `PolarStereographic` projection class that does not exist in the library. That is a
bigger job than the rest of MGRS combined and irrelevant for Iran-focused use, so it is a separate
feature.

### Step 8 — the grid overlay can be switched off ✅ implemented 2026-09-01

**The ribbon toggle could not remove the grid.** Clicking it unchecked the button and left the grid
drawn; clicking again put a second copy on the map.

The cause is in shared code. `LayerManager.Remove` tests its rule only against *non-group* layers:

```csharp
if (layer.IsGroupLayer)      { Remove(layer.SubLayers, rule, …); }   // group itself never tested
else if (… && rule(layer))   { layers.Remove(layer); }
…
if (layer.IsGroupLayer && layer.SubLayers.Count == 0) { layers.Remove(layer); }
```

A group is recursed into and never matched, so `Remove(theGroup, …)` matches no sub-layer, the
sub-layers stay, `SubLayers.Count` never reaches zero, and the empty-group branch never fires. Step
7 built this overlay as a `GroupLayer` of three sub-layers, so it was unremovable from the day it
was written. It surfaced only when the map-grids feature copied the pattern and its author hit the
same wall.

**Fixed by making the overlay one `VectorLayer`**, the shape the map grids had already been moved
to. The squares, the square names and the line values now come from `MgrsGridDataSource` as one
feature set, told apart by a new `Kind` attribute (`Cell` / `SquareId` / `AxisValue`) that each of
the three symbolizers filters on — a stroke, a 20 pt pale caption and a 12 pt red value. Nothing
about how the grid *looks* changed; the legend now shows one row instead of a group of three.

`MgrsGridLabelDataSource` is **deleted** — its only purpose was to feed the two label sub-layers, and
it had no other caller.

**A second, independent reason the grid stayed visible:** the toggle removed through
`RequestRemoveLayer`, which is wired to `MapViewer.RemoveLayer` and only drops the layer from the
layer manager — it does not clear what is already drawn on the canvas. The toggle now calls
`ClearLayer(layer, remove: true, forceRemove: true)`.

`tests/IRI.Maptor.Tests/Mapping/LayerRemovalTest.cs` pins all of it: that a group layer cannot be
removed by identity (with a non-group child — an *empty group* child would be auto-removed and the
test would pass for the wrong reason), that a plain layer can, and that both the MGRS overlay and a
map grid are single removable layers. The MGRS test was written first and failed, which is what
proved the bug rather than inferring it from the map-grids case.

The 238 existing MGRS tests still pass. Full suite: 1 992 tests, 58 failing, all in the documented
pre-existing GeoJson / MVT / Esri / Dxf round-trip suites.
