# Layer settings — Statistics (آمار) tab: status and deferred work

- **Status (2026-10-21):** the Statistics tab of the layer settings dialog is built (uncommitted at
  the time of writing) and shared by every client that uses the dialog (every WPF host application). It
  has two modes:
  - **Field statistics:** count, empty and distinct values, the 10 most frequent values with their
    share; min/max/sum/mean/median/standard deviation for numeric fields; earliest/latest for dates.
  - **Group by field:** per value of a field, the feature count and share, the total geodesic
    length (line layers) or area (polygon layers), and optionally the total of a numeric field, with
    a totals row. At most 1,000 groups are shown.
  - **Geometry (added 2026-10-22):** feature count, empty geometries, total length/area, and the
    per-feature min/max/mean/median/standard deviation with a histogram. Point layers: counts only.
  - **Histograms (added 2026-10-22):** numeric fields show a distribution chart by default, with a
    Chart / Top values switch in the panel header; geometry mode charts the per-feature length/area.
    Equal-width bins on "nice" edges (1/2/5 × 10^k), about log2(n)+1 bins within 5..20.
- **Scope:** the whole layer only. Cache and file layers are computed **in memory**; since 2026-10-23
  a **Live** layer (which holds only part of its features) is computed **by the server** over all its
  features, without the user's unsaved edits (item 5 below).
- **Domain-aware (added 2026-10-22, with the field contract's Step 2):** for a field with a domain, an
  "outside the domain" tile, "N of M domain values used", warning rows, and a group-by note.
- Verified with a synthetic-data harness (76 checks: statistics, group-by, geodesic measures in
  WGS84 / Web Mercator / UTM, histograms, untyped fields, cancellation, 1M-feature timing), xUnit
  (`tests/IRI.Maptor.Tests/Common/FieldDomainTest.cs`), and offscreen renders in fa + en through the
  real view model on a synthetic layer. **Not yet run in the real app; uncommitted.**

## Progress log

| Date | What |
|---|---|
| 2026-10-21 | Step 1: field statistics (Cache layers only; Live refused with a banner) |
| 2026-10-21 | Step 2: group-by (count, geodesic length/area, numeric field total, totals row) |
| 2026-10-21 | UI polish: segmented mode switch, tiles, details panel, table with share bars, hover, muted "(empty)" row; tab text colour fixed (tab content inherited the selected header's white) |
| 2026-10-22 | Scope + Copy **dismissed** by the user |
| 2026-10-22 | Geometry mode and histograms built; `FieldStatistics.ResolveKind` infers the kind of fields typed `"object"` |
| 2026-10-22 | Kind detection moved to `Field.ResolveDataType()` (field contract Step 1); domain features (Step 2) |
| 2026-10-22 | Item 4 built: "Layer summary" card on the General tab (`LayerSettings_GeneralSummaryViewModel`); pending edits left out by the user (own tab). Legend: the visibility checkbox now hides while a Live layer fetches (`BaseLayer.HideLoadingIndicator`) |
| 2026-10-23 | General tab: the summary card in two columns (the two extents side by side), labels no longer cut off, "Source Address:" colon in all 15 cultures. Same dialog: the Labeling tab removed and the Symbology tab enabled (see `docs/features/sld-symbology-editing.md`, "Multiple named styles per layer") |
| 2026-10-23 | **Item 5 built: Live layers via the server** (all steps at once, by the user's choice; exact median; EF's default 30 s command timeout kept). See "5. Live layers via the server" below |
| next | Real-app run by the user (both host applications, Live layers; the endpoints have not been called through a real API yet) |

## Where the code is

All under `src/Presentation/IRI.Maptor.Presentation.Wpf/`:

| File | Role |
|---|---|
| `Models/LayerSettings/FieldStatistics.cs` | Pure calculator for one field; `TryGetKey` normalises a value (double for numeric, DateTime for dates, null for empty) and is shared with group-by |
| `Models/LayerSettings/GroupStatistics.cs` | Pure group-by calculator; takes per-feature measures aligned with the features |
| `Models/LayerSettings/NumericSummary.cs` | `NumericSummary` (min/max/sum/mean/median/std) and `Histogram` (nice equal-width bins), shared by field and geometry statistics |
| `Models/LayerSettings/GeometryStatistics.cs` | Empty-geometry count and the per-feature measure summary |
| `Models/LayerSettings/GeometryMeasure.cs` | Geodesic length (m) / area (m²) of a geometry in any SRID `SridHelper.AsSrsBase` knows; SRID 0 is treated as Web Mercator |
| `ViewModels/LayerSettings/LayerSettings_StatisticsViewModel.cs` | Tab view model: `Mode`, field pickers, compute/cancel, tiles, details, table rows, totals row; features and measures are read once per dialog |
| `Views/LayerSettings/LayerSettings_StatisticsView.xaml` | The tab UI (segmented mode switch, tiles, details panel, custom table with share bars) |
| `Views/Dialogs/LayerSettingsDialogView.xaml`, `Common/Defaults/DefaultActions.cs` (`GetDefaultShowLayerSettingsView`) | Wiring |

Localization: `layerSettings_statistics_*` keys in `src/Presentation/IRI.Maptor.Presentation.Core/Properties/Resources.resx`
and `Resources.fa-IR.resx` only (other cultures fall back to English, as for other recent features).

## Deferred work, in the order the user saw it

The user asked for these to be planned one step at a time; each item below is a separate step to
discuss before building.

### Done or dropped (2026-10-22)

- **Scope selector + Copy** (old item 1): **dismissed by the user** — do not build unless asked again.
- **Geometry measures** (old item 2) and **histograms** (old item 3): built, see the status above.

### 4. Layer summary → General tab — built 2026-10-22

Built as a "Layer summary" card under Opacity: load mode (Web API layers only), feature count
(counted off the UI thread; Live layers say "not available"), field count, geometry type, SRS (and the
source SRS when it differs), extent in WGS84 and in the UTM zone of the extent's centre. **Pending edits
are not shown, by the user's decision: they have their own tab.** Code:
`ViewModels/LayerSettings/LayerSettings_GeneralSummaryViewModel.cs`, exposed as
`LayerSettingsViewModel.Summary`; `layerSettings_general_*` keys (13) in en + fa-IR. The original
request, kept for reference:

The user wants the layer-level facts on the **General** tab, not here: feature count, geometry type,
SRS, extent (WGS84 and UTM), field count, source, Cache/Live mode, pending edits
(`FeatureSet.GetPendingChangesCounts`). Some of these are already on the General tab
(`Views/LayerSettings/LayerSettings_GeneralView.xaml`: name, source address, source type, feature
type, opacity) — add only the missing ones.

### 5. Live layers via the server — built 2026-10-23

**Built (uncommitted):**

- **Endpoints** in both `LayerQueryController`s (same per-layer authorization as `Distinct`/`Extent`, 400 for a bad
  field or measure): `GET /LayerQuery/{layerId}/Statistics/Field/{field}`,
  `GET .../Statistics/Group/{field}?sumField=&measure=Length|Area`, `GET .../Statistics/Geometry?measure=`.
  `ILayerFeatureQueryService` and both `LayerFeatureQueryService`s call the shared
  `src/Infrastructure/IRI.Maptor.Infrastructure.EfCore/Querying/FeatureStatisticsExecutor.cs`.
- **SQL** (SQL Server / PostGIS): `COUNT`, `COUNT(DISTINCT)`, `MIN`/`MAX`/`SUM`/`AVG`, `STDEV`/`stddev_samp`,
  exact median with `PERCENTILE_CONT(0.5)` (it sorts the column: a few seconds on millions of rows), values with
  their counts by `GROUP BY` (largest first, capped at `MaxDistinctValues` = 1,000), histogram counts by
  `GROUP BY FLOOR((v - start) / width)` over the bins of `Core.Spatial.Analysis.HistogramLayout` (shared with the
  client's `Histogram`, so both bin identically), geodesic length/area per feature with `STLength`/`STArea`
  (geography) or `ST_Length`/`ST_Area` (PostGIS geography). Empty = NULL, or blank text (`LTRIM(RTRIM())`/`btrim`).
- **DTOs** (`Core.Spatial/Dtos/LayerStatisticsDto.cs`): `FieldStatisticsDto`, `GroupStatisticsDto`,
  `GeometryStatisticsDto`, `NumericSummaryDto`, `HistogramDto`, `GeometryMeasureKinds`. Values travel as
  `object?[]` with `ObjectArrayConverter`.
- **Client**: `IServerQueryableVectorDataSource` gets the three calls (`WebApiDataSource.Live`);
  `Models/LayerSettings/ServerStatistics.cs` turns the DTOs into the same `FieldStatistics` / `GroupStatistics` /
  `GeometryStatistics` the calculators give (values normalised like feature values, dates read as invariant ISO text,
  the empty value as a row, same order, same domain checks), so the tab shows them unchanged. A Live layer's tab now
  computes (the old warning banner became an accent note, `layerSettings_statistics_liveOnServer`); the General tab
  counts a Live layer on the server (`QueryPageAsync` with `IncludeCount`, note `layerSettings_general_featureCountServer`).
  `GroupStatistics.GroupCount` / `TotalGroupCount` carry the server's group count when it sent only the largest 1,000.
- **Tests**: `tests/IRI.Maptor.Tests/Querying/FeatureStatisticsExecutorTest.cs` — on LocalDB and on PostGIS (Docker),
  300 synthetic rows: every field, group-by with/without sum field, the three measure kinds, the caps and bad input;
  server results compared with the in-memory calculators (exact for counts/numbers/histograms, 0.5 % for geodesic
  measures). Plus a JSON round trip under fa-IR and the tab/General wiring with a fake Live source. Scratch harness
  `LiveStatsHarness` renders the tab and the General card (en/fa, 0 binding errors).

**Differences from the in-memory numbers, by design or accepted:**

- Unsaved edits are not included (the note says so).
- SQL Server's default collation compares text case-insensitively and ignores trailing spaces, so "PE" and "pe" are
  one value there (distinct count, groups); PostgreSQL and the client keep them apart.
- The domain checks ("outside the domain", "N of M used") look only at the values sent (the 1,000 most frequent).
- A geometry column (not geography) is read as WGS84.
- The old key `layerSettings_statistics_liveNotSupported` and `layerSettings_general_featureCountLive` are no longer
  used (left in the resx).

The original plan, kept for reference:

So Live layers (e.g. millions of customer rows) get correct numbers: a new endpoint beside
`Distinct` and `Extent` in both `LayerQueryController`s
of the host applications,
e.g. `POST /LayerQuery/{layerId}/Statistics`, implemented once in the shared
`src/Infrastructure/IRI.Maptor.Infrastructure.EfCore/Querying/FeatureQueryExecutor.cs` so both
apps get it. SQL aggregates: `COUNT`, `MIN`, `MAX`, `SUM`, `AVG`, `STDEV`, `GROUP BY` with counts;
geodesic length/area via SQL Server geography (`STLength`/`STArea`) or PostGIS
`ST_Length(geom::geography)` / `ST_Area(geom::geography)`. Then
`IServerQueryableVectorDataSource` gets a client method, and the view model uses it when
`IsLiveLayer` instead of showing the banner. Median and top-N need care on the server (window
functions / `PERCENTILE_CONT`).

### 6. Domain labels — resolved, not needed (2026-10-22)

Gas stores the domain **title**, not the gdb code, so the values shown are already labels. Domains now
reach the client as `Field.Domain` (field contract Step 2) and the tab uses them for the
out-of-domain checks. `FieldDomainValue.Label` exists for a future source that stores codes.

## Decisions already made

| Topic | Decision |
|---|---|
| Live layers | Computed by the server over the whole layer (item 5, 2026-10-23); never partial numbers from the loaded tiles |
| Median on the server | Exact (`PERCENTILE_CONT`), accepted cost of a few seconds on millions of rows (user, 2026-10-23) |
| Server query time | EF's default 30 s command timeout; a timeout shows as the tab's error message (user, 2026-10-23) |
| Field picker | One field at a time, computed only on "Compute" (opening the dialog stays fast) |
| Domain / bool fields | Treated as categories (frequencies), not numbers |
| Number display | Invariant culture, left-to-right inside the RTL page; dates via `LocalizedDateTimeConverter` (Persian calendar in fa) |
| Length / area units | One unit per column, picked from its total: m or km; m², ha or km² |
| Group rows | Ordered by count, largest first; capped at 1,000 with a note |

## Gotchas found while building it

- **Web API layers drop features without geometry** before they reach the client
  (`FeatureDto.AsFeature` returns null for an empty shape and `FeatureSetDto.AsFeatureSet` skips it),
  so for server layers the "empty geometries" tile is always 0 and the feature count excludes such
  rows. Only file layers can show a non-zero count. Showing them would need a change in that DTO path.
- File/memory layers type their fields from the **first** feature's values (`Field.FromDictionary`);
  an empty first value gives type `"object"`. `FieldStatistics.ResolveKind` then infers the kind from
  the values. The group-by **sum-field combo** is still built from declared types only, so such a
  numeric field is not offered there.

- Tab content inherits the selected TabItem header's white `Foreground`: every TextBlock style in
  a tab must set its own `Foreground`.
- A number forced `FlowDirection="LeftToRight"` inside an RTL page: align it with a `TextAlignment`
  that follows `LocalizationManager.IsRightToLeft`, not with `HorizontalAlignment`.
- To render an RTL WPF window offscreen, use `RenderTargetBitmap.Render(window)`; drawing
  `window.Content` through a `VisualBrush` drops the RTL mirror and scrambles Persian glyphs.
- `SpatialUtility.GetEllipsoidalArea(geometry, toWgs84)` transforms unconditionally — do not pass a
  transform for geometries that are already WGS84 (`GeometryMeasure` handles this).
