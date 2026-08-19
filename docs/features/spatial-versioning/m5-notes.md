# M5 working notes — editor experience + approver UI

**Last updated:** 2026-08-14 — **M5 CODE-COMPLETE (slices 1–5).** Submit-result dialog +
My-Pending + Approval windows (slice 1), feature-timeline window (slice 2), notification
inbox end-to-end (slice 3, harness 18/18), error localization (slice 4 — typed
VersioningException + 26 keys, M4 harness re-verified 25/25), map entry points +
own-pending overlay (slice 5, harness 6/6). All builds green. Remaining: runtime/visual
testing of map-integrated pieces → M6 walkthrough.

## What was built (uncommitted — Hossein reviews & commits)

### Client transport (`Ket.WebApiPersistence/VersioningWebApi.cs`)

Approval + history methods following the existing dual url/httpClient pattern:
`GetApprovalQueueAsync`, `CommitAsync`, `ReturnCompetitionAsync`, `GetFeatureTimelineAsync`.

### Reusable UI (`Jab.Wpf`, namespace `IRI.Maptor.Jab.Controls.Versioning`)

- **MyPendingViewModel + MyPendingFunctions** — the editor's own proposals with the
  **collapsed** editor-facing statuses (doc 02 §7: provisional review outcomes stay
  indistinguishable until commit); `versioning_status_*` labels, InCompetition shows the
  competitor count. Manual refresh only (D37).
- **ApprovalQueueViewModel + ApprovalFunctions** — Resolved competitions with fresh
  stale/orphan flags; checkbox multi-select → one `CommitRequestDto` (per-row RowVersion,
  `StaleOverride` applied only to stale rows, all-or-nothing E9 server-side); per-row
  **Return** with a mandatory reason; stale rows without the override are blocked
  client-side with `versioning_approval_staleNeedsOverride`.
- **MyPendingView.xaml / ApprovalQueueView.xaml** — toolbar (refresh + last-refresh time +
  progress ring), DataGrid; approval adds badge column (stale/orphaned/delete), return
  button column, and a decision footer (reason TextBox, commit-selected button,
  stale-override CheckBox reusing `versioning_compare_staleOverride`).
- **Jab.Core resx**: ~26 new keys (`versioning_approval_*`, `versioning_myPending_*`,
  `versioning_status_*`, `versioning_submit_*`), neutral + fa-IR, guarded tail appends.

### Saba wiring (mirrors the M3 review-window pattern)

- **ApplicationPresenter**: `CanEditVersions` (400) / `CanApproveVersions` (402) +
  `RaiseUserPermissionsChange`; lazy `ShowMyPendingCommand` / `ApproveVersionsCommand`;
  `OnRequestShowMyPending` / `OnRequestShowVersioningApproval`;
  `CreateMyPendingFunctions()` / `CreateVersioningApprovalFunctions()` over
  `VersioningWebApi` with `MakanNegarSabaServices.BaseUrl/SharedClient`.
- **Submit-result dialog** (the slice's headline): in `LoadApiLayers`, every layer whose
  DataSource is a `VersioningWebDataSource` gets `SessionSubmitted` subscribed →
  `ShowSubmissionSummary` composes counts (proposals, in-competition = CompetitorCount>1,
  superseded, overlap advisories) from `versioning_submit_*` keys and shows one dialog.
  Without it the D34 revert-to-live-truth looks like the edits silently vanished.
- **MainWindow**: two new ribbon buttons in the versioning tab (My-Pending →
  ClipboardListOutline, gated by CanEditVersions; Approve → CheckDecagram, gated by
  CanApproveVersions) + the two OnRequestShow* handlers.
- **New windows** `View/Versioning/VersioningPendingWindow.xaml(.cs)` and
  `VersioningApprovalWindow.xaml(.cs)` — MetroWindow Dialog style, host the Jab views,
  refresh on Loaded; explicit Page/Compile csproj entries (EnableDefaultItems=False).
- **Saba resx**: 6 keys (`app_saba_ribbon_myPending(+Tooltip)`,
  `app_saba_ribbon_approveVersions(+Tooltip)`, `app_saba_versioningPending_title`,
  `app_saba_versioningApproval_title`), English + Persian.

### Verified

- Saba client builds green (22 projects, 0 errors); Jab.Wpf builds green.
- Render harness (fa-IR, RTL): `m5-my-pending.png` (all five collapsed statuses, Jalali
  dates, competitor count) and `m5-approval-queue.png` (selection, badges, per-row return,
  decision footer) — both correct.

## Slice 2 — feature timeline (doc 03 §5.4)

- **Jab.Wpf**: `FeatureTimelineViewModel` + `TimelineFunctions`
  (LoadLayersAsync/LoadTimelineAsync/ShowMessage/ShowGeometryComparison) and
  `FeatureTimelineView.xaml` — toolbar (versioned-layer ComboBox by DisplayName, feature-id
  TextBox, load button), result banner with deleted badge (Live == null), master grid
  (bold synthetic "current state" row when live exists + copy-on-write hops newest-first:
  superseded-at, replacing change type/editor/session/approver/commit-batch), detail pane
  (selected state's attribute values + show-on-map comparing that state against live).
  `LoadForAsync(layerKey, featureId)` is the pre-seeded entry point for the future map
  context menu. 11 new `versioning_timeline_*` keys (neutral + fa-IR); reuses
  colLayer/colType/colSession/showOnMap/field keys.
- **Saba**: `CanReadVersionHistory` (403), lazy `ShowVersionHistoryCommand`,
  `OnRequestShowVersionHistory`, `CreateVersionHistoryFunctions()` (GetLayersAsync +
  GetFeatureTimelineAsync; geometry hook → RequestShowGeometryComparison), ribbon button
  (History icon), `VersioningHistoryWindow` (+csproj entries), 3 `app_saba_*` keys.
- Verified: builds green; `m5-timeline.png` render-harness screenshot (fa-IR, RTL) —
  current-state row bold, hop provenance columns, attribute detail, Jalali dates.
- Resx audit after slice 2: Jab.Core 653/653 parity, BOM+CRLF intact; Saba 324/323 —
  the single gap is the pre-existing untranslated `unknow` key, untouched.

## Slice 3 — notification inbox (doc 02 §8)

- **InboxService** (`Ket.VersioningPersistence/Services/`): `GetInboxAsync` (recipient-
  scoped, newest first, optional unreadOnly, take 200) and `MarkReadAsync` (listed ids or
  All; only own unread rows — foreign ids are silently ignored). Payloads (the writers'
  anonymous JSON: N1 competitions[], N2/N3/N4 proposals[] with reasons, N5 root reason)
  are **normalized server-side** into `InboxItemDto` (ItemCount, distinct
  TargetFeatureIds, distinct Reasons) so clients never parse PayloadJson; a malformed
  payload still shows the row, just without detail.
- **DTOs**: `Dtos/InboxDtos.cs` (InboxItemDto, InboxMarkReadRequestDto/ResultDto).
- **API**: `GET /Versioning/Inbox?unreadOnly=` + `POST /Versioning/Inbox/MarkRead` —
  class-level `[Authorize]` only (recipients span all versioning roles); gateway + MediatR
  pair (`Features/Versioning/Inbox/`).
- **Client**: `VersioningWebApi.GetInboxAsync` / `MarkInboxReadAsync`.
- **Jab.Wpf**: `InboxViewModel` + `InboxFunctions` + `InboxView` — unread count label,
  mark-all-read, unread rows bold with an amber dot, event labels via
  `versioning_inbox_type*`, details composed from count/features/reasons. 14 new
  `versioning_inbox_*` keys (neutral + fa-IR).
- **Saba**: `CanUseVersioningInbox` (= edit ∨ review ∨ approve), `ShowVersioningInboxCommand`,
  `CreateVersioningInboxFunctions()`, ribbon button (BellOutline), `VersioningInboxWindow`
  (+csproj entries), 3 `app_saba_*` keys.
- **Verified**: service harness **18/18 PASS** vs staging (rolled back; fictional
  recipient ids are safe — no FK per D31): normalization of all payload shapes,
  malformed-payload tolerance, recipient isolation, unreadOnly filter, single/all
  mark-read, no-op re-mark, foreign-id rejection. Client + API builds green;
  `m5-inbox.png` render-harness screenshot (fa-IR, RTL) passes. Resx audit: Jab.Core
  667/667; Saba 327/326 (pre-existing `unknow` gap only).

## Slice 4 — error localization (the m2-notes task, done for ALL services)

- **`VersioningException : DomainException`** (Sta.Versioning): carries the stable error
  code (doc 04 §8); `Message` keeps the exact `"Code: technical message"` wire format the
  plain throws used, and `MessageResourceKey` derives mechanically as
  `message_error_versioning{Code}`. One class instead of ~20 — new codes localize by
  adding a resx key only.
- All **52 coded `DomainException` throws** across SubmissionService, ReviewService,
  CommitService, VersioningQueryService, HistoryService, LiveEntityWriter converted
  (24 distinct codes). The API middleware already writes `MessageResourceKey` + type
  name into the envelope — no server plumbing changes.
- **Client-side reconstruction** (`VersioningApiErrors.ToException(typeName, resourceKey)`):
  `VersioningWebDataSource.SaveChangesAsync` and `WebApiDataSource`'s sync error branch
  (beside the ConcurrencyException check) rethrow the typed exception, so the standard
  `catch (DomainException)` → `ShowErrorMessage` save path localizes submit errors and
  the D26 direct-sync gate automatically.
- **Versioning dialogs** (`ApplicationPresenter.ResolveVersioningApiError`): the envelope's
  Detail (a resource key per the middleware contract) resolves through
  LocalizationManager; an unkeyed versioning code falls back to the generic domain
  message plus the stable code in parentheses (for support calls); non-key Details pass
  through raw. All five function factories switched to it — previously they displayed the
  raw key string.
- **Resx**: 26 keys neutral + fa-IR — `message_error_versionedLayerWriteRejected` (the
  m2-notes item) + 25 `message_error_versioning*` covering every current code. Note the
  mechanical key `message_error_versioningVersioningNotEnabled` (code
  "VersioningNotEnabled") — the doubled word is intentional; predictable derivation wins.
- **Verified**: M4 lifecycle harness re-run **25/25** (error-path assertions rely on the
  preserved message format); versioning unit tests 23/23; client + API builds green;
  resx audit 692/692, BOM/CRLF intact.

## Slice 5 — map entry points + own-pending overlay (Hossein: context menu; build overlay now)

- **Shared FeatureTable** (Jab.Wpf, affects all apps — deliberate generalization): the
  DataGrid got a **row context menu** over the same `AssociatedLayer.FeatureTableCommands`
  the footer strip renders (ContextMenu inherits the SelectedLayer DataContext from the
  grid; header = command ToolTip, no icons — a shared-instance Setter can't carry per-item
  visuals). `grid_PreviewMouseDown` now selects the row under a right-click unless it is
  already part of the selection (keeps multi-select for bulk actions).
- **Per-feature commands for versioned layers** (`ApplicationPresenter`, attached in
  `LoadApiLayers` beside the SessionSubmitted subscription; defaults + 3, so they appear
  in both the strip and the context menu):
  - «وضعیت پیشنهادها» (CanEditVersions): D34 on-demand check on the first highlighted
    feature → count + author names + own-marker dialog.
  - «تاریخچه عارضه» (CanReadVersionHistory): `OnRequestShowVersionHistoryFor(layerKey,
    featureId)` → `VersioningHistoryWindow(presenter, layerKey, featureId)` (new
    pre-targeted ctor → `FeatureTimelineViewModel.LoadForAsync`).
  - «پیشنهادهای من روی نقشه» (CanEditVersions): the own-pending overlay.
- **Own-pending overlay**: new `GET /Versioning/MyLayerPending?layerKey=` under
  "Versioning.Edit" — own ACTIVE proposals for the layer with WKB geometry (deletes
  skipped: no proposed geometry; documented). `MyLayerPendingFeatureDto`;
  `VersioningQueryService.GetMyLayerPendingAsync`; gateway + MediatR + client method.
  Presenter `ShowMyPendingOverlayAsync`: WKB → WebMercator features →
  `MemoryDataSource` + amber `VectorLayer` («پیشنهادهای من — {layer}»), tracked per
  layerKey — re-invoke refreshes (removes + re-adds), zero pending removes it with a
  message. The overlay layer is user-removable via the TOC.
- **Resx**: 10 `versioning_map_*` keys (neutral + fa-IR); audit 702/702, BOM/CRLF intact.
- **Verified**: harness **6/6 PASS** vs staging (rolled back): update visible with
  WKB/srid/status, delete excluded, editor isolation, unknown layer key throws the typed
  `VersioningException` with the right code + resource key. Client + API builds green.
- **Context menu verified in a live WPF instance** (render harness, real `FeatureTable` +
  real `SelectedLayer` over a `MemoryDataSource` layer, 14/14 checks): the ContextMenu
  inherits the SelectedLayer DataContext, generates one MenuItem per command with the
  ToolTip as header, binds Command, and resolves CommandParameter to the SelectedLayer —
  screenshot `m5-context-menu.png` (four Persian items, RTL). Harness caveat: the grid
  body was empty because stub `Fields` were null — column generation needs the real
  layer's field list; strip buttons and pager rendered fine.
- **HTTP acceptance 15/15 PASS** (API booted from source on localhost:5140, real login):
  401 unauthenticated; layers (TrLineSeg enabled/Substat disabled, TableName resolved
  from EF metadata); myProposals, pendingStatus, **myLayerPending**, inbox (+unreadOnly),
  no-op MarkRead, history timeline, review queue, approval queue all 200 on the clean
  baseline; **typed error envelopes over HTTP confirmed** — empty commit →
  `Title=VersioningException, Detail=message_error_versioningInvalidCommit`, unknown
  return → `…UnknownCompetition`. This was the first HTTP exercise of every M4/M5
  endpoint. Note: the user controller is `/api/User/Login` while versioning is
  `/Versioning/...` (differing route prefixes are pre-existing).
- **Still needing the real app** (M6 walkthrough): overlay drawing on the actual map,
  dialog flows, and end-to-end interaction — the remaining smoke items in m6-pilot.md §3.

## Remaining in M5

- Nothing — M5 is code-complete. Possible later additions (not blocking M6): withdraw
  session/proposal endpoints (guards exist, endpoints not built).

## Runtime testing still pending

The new windows and the submit dialog have not been exercised against the running app —
covered by the M6 operator walkthrough (UI-level A/E/F acceptance).
