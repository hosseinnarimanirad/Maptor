# M3 working notes — review milestone

**Last updated:** 2026-08-14. Milestone M3 (doc 06 §2): review services + API done and
verified; the WPF review UI (queue + compare views) remains.

## Server side — DONE (2026-08-14, uncommitted)

### ReviewService (`Ket.VersioningPersistence/Services/ReviewService.cs`)

- **Queue** (`GetQueueAsync`): Open competitions with computed badges — age (oldest
  submission), **stale** (any active proposal's base ≠ live RowVersion, batch-fetched per
  layer via `LiveFeatureReader`), **orphaned** (target missing in live), **blocked by
  predecessor** (D19 queue rule), delete-as-side flag, undismissed suggestion count,
  author names, and the competition RowVersion the client must echo back (E4).
- **Compare** (`GetCompareAsync`): raw proposal states (WKB + canonical-JSON attributes
  deserialized) + the **live snapshot** read generically from the feature table
  (`LiveFeatureReader.GetSnapshotAsync` — columns from EF metadata, geometry via
  `STAsBinary()` so clients parse it like proposal WKB). Diffs stay client-side.
- **SelectWinner**: guards (Open, predecessor terminal, winner active), **orphan =
  reject-only (D23)**, **stale needs recorded override (D8)**, per-loser reasons required
  (FR-3.5), P3/P4 state flips, append-only DecisionRecords, competition → Resolved.
  **No loser notifications** — provisional until commit (D17).
- **CloseNoWinner**: reasons required, final rejections, **immediate N4 digests** (D20),
  CloseNoWinner + per-proposal records.
- **GroupProposals** (D16): ≥2 active create-proposals of one layer merge into the
  lowest-id competition (Kind → ManualGroup); emptied singletons → Dissolved; recorded as
  **DecisionAction.GroupProposals = 6 (new, D49)**; N1 count-digests to owners.
- **DismissSuggestion**: idempotent, stamps dismisser id + display name (D31).
- **BulkAccept** (D40): singletons only; each item commits alone; a failing item reports
  its error and the rest proceed (tracker cleared between items).
- **Concurrency (E4)**: client-echoed competition RowVersion set as OriginalValue →
  `DbUpdateConcurrencyException` → `CompetitionAlreadyResolved`; proposal/competition
  tokens also protect grouping automatically.

### Verified vs STAGING — 22/22 PASS (rolled-back harness)

Walkthrough **A** (two editors → queue badges → compare with live state → select → states
+ 2 decision records + no premature notification), **E4** (stale competition RowVersion
rejected), **E** (delete vs edit; HasDelete flag; delete wins), **stale** (badge; select
blocked without override; override recorded on the decision), **orphan** (badge; select
blocked D23; close-no-winner finalizes + immediate N4), **F** (grouping: one ManualGroup
competition, dissolution, D49 record, resolvable), **bulk** (singleton accepted;
already-resolved item fails without aborting the rest).

### API surface — DONE, smoke-tested

`/Versioning/Review/*` under policy **"Versioning.Review"** (permission 401): `GET Queue`,
`GET Competitions/{id}`, `POST Competitions/{id}/Select`, `POST
Competitions/{id}/CloseNoWinner`, `POST Group`, `POST Suggestions/{id}/Dismiss`, `POST
BulkAccept` — gateway → MediatR → controller, same pattern as M2. Smoke: authorized 200
(empty queue), unknown competition → 400.

## Review UI — slice 1 DONE (2026-08-14, uncommitted): client API + view models

- `VersioningWebApi` gained the seven review methods (queue, compare, select, close,
  group — result wrapped in the new `GroupResultDto`, since the typed HTTP client can't
  deserialize a bare JSON number — dismiss, bulk accept).
- `ReviewQueueItemDto.SingleProposalId` added (server populates it for singletons) so the
  queue can drive manual grouping without a second round-trip.
- **Jab.Wpf `ViewModels/Versioning/`** (project now references `Sta.Versioning`):
  - `ReviewFunctions` — transport delegate bundle; Jab.Wpf stays HTTP-agnostic, Saba
    wires the delegates to `VersioningWebApi` with its shared authenticated client, plus a
    `ShowMessage` sink.
  - `ReviewQueueViewModel` — competitions/singles split, manual refresh (D37),
    master-detail navigation (`CurrentCompare` swaps the window content), bulk accept of
    selected singles with per-item failure messages (D40), group-selected-creates (D16,
    enabled only for ≥2 selected creates).
  - `CompetitionCompareViewModel` — N-way attribute rows built client-side (union of
    fields; per-cell changed-vs-live flags; delete proposals render empty cells), stale/
    orphan/blocked gating (`CanDecide` blocks orphaned targets per D23 and queued
    competitions per D19), ReasonForAll + StaleOverride inputs validated before calling
    the server, and map inspection via the **existing two-way geometry comparison**
    (live vs the chosen proposal, parsed from WKB and projected to WebMercator) —
    `RequestShowGeometryComparison` is wired by the host to
    `MapViewModelBase.RequestShowGeometryComparison`.
- Jab.Wpf note: **no ImplicitUsings** in this project — explicit `using System;…` needed.
- Everything builds green (Jab.Wpf 16 projects; Api 14 projects).

## Review UI — slice 2 DONE (2026-08-14, uncommitted): XAML + localization + Saba wiring

- **Views** (Jab.Wpf `Views/Versioning/`, class namespace `IRI.Maptor.Jab.Controls.Versioning`
  like the FeatureChanges precedent): `ReviewQueueView` (toolbar with refresh + last-refresh
  + busy ring; competitions DataGrid with badge chips — stale/orphaned/queued/delete —
  authors, age, overlap counts, per-row Review button; singles DataGrid with selection
  checkboxes + Accept-selected + Group-as-competition buttons; compare detail swaps in via
  ContentControl on `CurrentCompare`) and `CompetitionCompareView` (back button, orphan/
  queued banners, proposal cards with DELETE/stale chips + show-on-map + select-winner,
  **N-way attribute DataGrid whose per-proposal columns are built in code-behind** with a
  changed-cell highlight trigger per column, reason textbox + reject-all button +
  stale-override checkbox shown only when relevant).
- **Localization**: 30 `versioning_*` keys appended to Jab.Core neutral + fa-IR (617/617
  parity, BOM/CRLF preserved, verified with ResXResourceReader + regex + byte checks;
  other 13 cultures fall back to English); 4 `app_saba_*` keys in the Saba store (ribbon
  tab/button/tooltip + window title).
- **Saba wiring** (all following the RoleManagement pattern): `CanReviewVersions` gated on
  `Versioning_Review` + RaiseUserPermissionsChange, `ReviewVersionsCommand` →
  `OnRequestShowVersioningReview` → `VersioningReviewWindow` (MetroWindow dialog hosting
  the queue view, initial refresh on Loaded); ribbon tab "نسخه‌بندی" after user management;
  `ApplicationPresenter.CreateVersioningReviewFunctions()` binds the delegate bundle to
  `VersioningWebApi` over the shared client, messages to `DialogService.ShowMessageAsync`,
  and map inspection to `RequestShowGeometryComparison` (live vs proposal on the main map).
- Gotchas hit: Saba csproj has `EnableDefaultItems=False` — new Page/Compile entries must
  be added explicitly; `ApplicationPresenter` lives in namespace
  `IRI.App.MakanNegarSaba.Presenters` (not .ViewModel); `IDialogService.ShowErrorMessage`
  takes a DomainException, use `ShowMessageAsync(string, title)` for plain text.
- Whole stack builds green (Jab.Wpf 16 projects; Saba client 22 projects).

## Visual verification — DONE (2026-08-14): render-harness screenshots PASS

Harness rebuilt (scratchpad `uiharness/`): instantiates the **compiled** views with real
view models + stub Persian data, fa-IR culture via `LocalizationManager.SetCulture`, RTL
window, app-level resource merge copied from Saba's App.xaml (MahApps Controls/Fonts/
Light.Amber + Jab.Wpf Controls.All/Converters/Fonts/Colors — `iranSans` lives in
`IRI.Maptor.Fonts.xaml` at APP level, not in the view-local dictionaries).

- **Screenshots saved for review**: `m3-review-queue.png`, `m3-competition-compare.png`
  (this folder). Compare view: RTL N-way grid with correct changed-cell highlights,
  delete column as dashes, chips, cards, decision footer. Queue: sections, badges,
  Persian-digit ages, disabled bulk buttons while nothing is selected.
- **One real bug found and fixed**: a `ContentControl` instantiates its ContentTemplate
  even when Content is null, so the empty compare view painted over the queue (null-
  DataContext bindings left banner Visibility at its Visible default). Fixed with a
  style trigger collapsing the control when Content is null (comment in the XAML).
- Minor polish candidates left as-is: age suffixes are Latin d/h/m with Persian digits;
  badge column has no header (by design).

## Remaining in M3 (superseded original list follows)

1. **XAML**: `Views/Versioning/ReviewQueueView.xaml` + `CompetitionCompareView.xaml`
   (UserControls in Jab.Wpf, D42) — queue sections + badges + toolbar; compare = proposal
   cards + N-way grid (DataGrid columns built in code-behind from the VM's proposal
   count) + banners + reason/override inputs. Localization keys `versioning_*` into the
   **Jab.Core** resx store (neutral + fa-IR only; other cultures fall back) following the
   byte-level rules in `src/IRI.Maptor.Jab/CLAUDE.md`.
2. **Saba wiring**: `CanReviewVersions => HasPermission(Permission.Versioning_Review)` (+
   RaiseUserPermissionsChange), `ReviewVersionsCommand` → `OnRequestShowVersioningReview`
   action → MainWindow opens a MetroWindow (style `IRI.Maptor.Styles.MetroWindow.Dialog`)
   hosting the queue view; ribbon button in a new "نسخه‌بندی" tab (Fluent pattern like the
   user-management tab); `ReviewFunctions` implementation over `VersioningWebApi`
   (baseUrl + `MakanNegarSabaServices.SharedClient`); geometry-comparison hookup to the
   main map presenter.
3. Visual check via the render-harness recipe (memory: saba-wpf-render-harness) or
   Hossein runs the app.
4. M3 acceptance at UI level (walkthroughs A/E/F through the views) — or fold into the M6
   pilot walkthrough (Hossein's call, still open).

Standing rules: ask before EF migrations; never hand-edit the model snapshot.
