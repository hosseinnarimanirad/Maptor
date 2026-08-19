# Session worklog — 2026-08-14: M1 → M6-prep in one day

One-stop summary of the implementation session that took spatial versioning from
"planning baselined" to "code-complete, pilot package ready". Details live in the
per-milestone notes (`m1-notes.md` … `m5-notes.md`, `m6-pilot.md`); this file is the
overview and the entry point.

**State at end of session:** M1–M5 code-complete and harness-verified; M6 pilot package
prepared. Day-1 foundation is committed ("feat: versioning" … "feat: versioning - M3");
**everything from mid-M2 on is UNCOMMITTED** — Hossein reviews and commits. Staging holds
only the two seeded registry rows (TrLineSeg enabled, Substat disabled); zero test
residue — the pilot starts from an empty workflow.

---

## What was built, per milestone

### M1 — Foundation
- `IRI.Maptor.Sta.Versioning` (netstandard2.1, EF-free): 9 entities, enums (numeric
  values load-bearing for filtered indexes), `VersioningGuards`, schema-signature
  calculator, DTOs.
- `IRI.Maptor.Ket.VersioningPersistence` (net8.0): EF configurations (schema
  `versioning`), `AddMaptorVersioning()`, canonical attribute serializer, registry
  initializer, `VersionedLayerGate` (60 s cache, fail-open).
- Migration with the D19/D22 filtered unique indexes; spike verified geography
  round-trip, STIntersects, and index races on staging. 23 unit tests.
- Two silent bugs caught: duplicate enum values 300/301 colliding with LayerSettings
  permissions (→ renumbered 400–403, D48) and a lost `HasIndex` overwrite (name must be
  passed inside `HasIndex`).

### M2 — Submission + sync gate
- `SubmissionService` (validation, collision → join/queue competitions, self-supersede
  D22, overlap scan via geography SQL, N1 digests) — harness 12/12.
- D26 sync gate in `UnitOfWork` (`VersionedLayerWriteRejectedException`).
- API: `/Versioning` Layers/Sessions/MyProposals/PendingStatus (gateway → MediatR →
  controller; policies per Permission Display Name).
- Client: `VersioningWebApi` + `VersioningWebDataSource` (save → session submission,
  then revert to live truth per D34); presenter routes versioned layers by EntityName.
- **API acceptance 14/14** through real login + real client path; TrLineSeg pilot
  enabled and left on; direct sync verifiably rejected.

### M3 — Review
- `ReviewService`: queue with age/stale/orphan/suggestion badges, N-way compare payload,
  select-winner (D8 override recorded, D17 silent provisional rejections), close-no-winner
  (D20, immediate N4), manual grouping of creates (D16, new `DecisionAction.GroupProposals`
  → D49), suggestion dismiss, bulk accept (D40) — harness **22/22** incl. E4 concurrency.
- UI: `ReviewQueueView` + `CompetitionCompareView` (Jab.Wpf, reusable, delegate-bundle
  pattern), `VersioningReviewWindow` in Saba, ribbon tab «نسخه‌بندی», map inspection via
  the existing geometry-comparison hook. Persian screenshots pass
  (`m3-review-queue.png`, `m3-competition-compare.png`); found + fixed the
  ContentControl-instantiates-template-for-null-Content WPF trap.

### M4 — Commit + history
- `LiveEntityWriter` (generic proposal→live apply via EF metadata; editor-stamped audit
  columns per R10/D31), `CommitService` (all-or-nothing E9 batch, approval-time stale
  gate with recorded override, copy-on-write `FeatureHistory`, create-id backfill,
  deferred N2/N3 with review reasons; `ReturnAsync` reopen + N5), `HistoryService`
  timeline — harness **25/25** (full lifecycle incl. E9 abort and drift overwrite).
- API: Approval/Queue + Commit + Return, History/{layerKey}/{featureId}.

### M5 — Editor experience + approver UI (5 slices)
1. **Submit-result dialog** (SessionSubmitted → counts summary), **My-Pending** window
   (collapsed statuses), **Approval** window (multi-select commit, stale override,
   per-row return) — screenshots `m5-my-pending.png`, `m5-approval-queue.png`.
2. **Feature timeline** window (layer/feature picker, bold current-state row +
   copy-on-write hops with provenance, attribute detail, show-on-map) —
   `m5-timeline.png`.
3. **Notification inbox** end-to-end: `InboxService` (server-side payload
   normalization; recipient-scoped reads; mark-read), endpoints (auth-only — recipients
   span roles), client, `InboxView` + Saba window — service harness **18/18**;
   `m5-inbox.png`.
4. **Error localization**: one `VersioningException : DomainException` with stable codes
   (all **52** coded throws converted, 24 codes), mechanical resource keys, client-side
   envelope reconstruction in both data sources, presenter-side key resolution, **26**
   `message_error_*` keys (fa + en) — M4 harness re-verified 25/25.
5. **Map entry points** (Hossein: context menu; overlay now): shared FeatureTable row
   **context menu** over the per-feature command strip + right-click row selection;
   three per-feature commands on versioned layers (pending-status D34 dialog, history
   pre-targeted window, own-pending overlay); new `MyLayerPending` endpoint + amber
   overlay layer — harness **6/6**; `m5-context-menu.png`.

### M6 — Pilot package (`m6-pilot.md`)
Pre-flight checklist, role setup (3 roles over permissions 400–403 via the existing
Role/User Management UI), runtime smoke list, **Persian operator scripts** for
walkthroughs A/E/F + C/D checkpoints, monitoring SQL (**all validated read-only against
staging — baseline fully clean**), feedback sheet, Substat enablement + D43 rollback.

### Pre-pilot verification sweep
- **HTTP acceptance 15/15**: first HTTP exercise of every M4/M5 endpoint (real login,
  401 gate, all reads, typed error envelopes over the wire).
- **Context menu 14/14** in a live WPF `FeatureTable` instance (DataContext inheritance,
  headers, Command/CommandParameter) + screenshot.

---

## Verification scoreboard

| Harness | Result |
|---|---|
| Sta.Versioning unit tests | 23/23 |
| M2 SubmissionService (staging, rolled back) | 12/12 |
| M2 API acceptance (real login + client path) | 14/14 |
| M3 ReviewService | 22/22 |
| M4 commit lifecycle | 25/25 (re-run after slice 4: 25/25) |
| M5 InboxService | 18/18 |
| M5 MyLayerPending | 6/6 |
| HTTP acceptance (M4/M5 endpoints) | 15/15 |
| FeatureTable context-menu binding | 14/14 |
| Render-harness screenshots (fa-IR, RTL) | 7 views, all pass |
| Resx audits | Jab.Core 702/702; Saba 327/326 (pre-existing `unknow` gap only) |

## Decisions taken during implementation (added to doc 01 log)
D43 (disable blocked while open proposals), D44 (current system = staging), D45
(hardcode geography), D46 (LayerKey Guid + client matches by TableName/EntityName), D47
(pilot = TrLineSeg + Substat, seeded disabled), D48 (permissions renumbered 400–403),
D49 (`DecisionAction.GroupProposals = 6`).

## Areas touched (for the commit review)
- `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Versioning/` — new project (entities, enums,
  guards, DTOs, `VersioningException`, `VersionedLayerWriteRejectedException`).
- `src/IRI.Maptor.Ket/IRI.Maptor.Ket.VersioningPersistence/` — new project (configs +
  6 services).
- `src/IRI.Maptor.Ket/IRI.Maptor.Ket.WebApiPersistence/` — `VersioningWebApi`,
  `VersioningWebDataSource`, typed-error mapping in `WebApiDataSource`.
- `src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/` — `ViewModels/Versioning/` +
  `Views/Versioning/` (6 view/VM pairs), FeatureTable context menu, `Jab.Core` resx
  (~95 new versioning keys, neutral + fa-IR).
- `src/IRI.App/Barg/` — Core Permission enum block; Application gateway + MediatR
  features; Ef `VersioningGateway` + UnitOfWork gate; Presentation `VersioningController`;
  WPF client: presenter (commands, factories, overlay, submit dialog), MainWindow ribbon
  + handlers, 5 versioning windows, Saba resx keys.
- `docs/features/spatial-versioning/` — milestone notes m1–m5, m6-pilot, 7 screenshots.

---

## Follow-up session (same day): UI pass, submit affordance, demo data

Triggered by three findings from Hossein's first hands-on run.

### 1. "I edited a feature and it went straight into the DB"
Not a defect. The edited row was `sub.Substat` #126 — **Substat is seeded
`IsVersioningEnabled = 0`** on purpose (D47), so direct sync is the correct path.
`tr.Tr_Line_Seg`, the one enabled layer, had not been touched since 2026-05-24 and the
workflow tables were empty, so nothing was lost. The deployment is healthy: every
`/Versioning/*` route on `makanegar.ir` answers 401 (present, auth-required) while an
invented route answers 404, so the deployed API carries the current M5 code; SuperAdmin
already holds 400–403.

**The real gap it exposed:** Save *is* the submit action on a versioned layer (D34), and
nothing said so. Fixed — see §3.

### 2. Design-system pass (the views were stock WPF)
Audit: the six versioning views referenced **zero** `IRI.Maptor.Styles.*` keys — only
`Localization`, a converter and the font. Root cause of "flat": there was no shared
DataGrid style anywhere in the system, so every queue rendered as raw WPF.

Added to the Jab design system (keyed, so nothing else changes appearance unless it opts
in): `Controls.DataGrid.xaml` (grid + header/row/cell, zebra, themed selection — the cell
owns its template because MahApps paints selection from a template trigger, which outranks
style triggers), `Controls.Pill.xaml` (status badges in the semantic palette),
`Border.Panel/PanelHeader/Toolbar`, `TextBlock.EmptyState/Caption`, and
`IRI.Maptor.Brushes.Warning(.Fill)` in both Status dictionaries.

All six views rebuilt on those tokens: panel framing, styled toolbars/buttons/inputs, theme
brushes instead of literal greys, status pills, per-section empty states. Also fixed:
**the five Saba windows were using `MetroWindow.Dialog`, whose `MaxWidth=550` fought their
own `MinWidth`, so they opened far narrower than designed** — they now use
`MetroWindow.Localized` and are maximizable. Localization gaps closed: `ChangeType` was
rendering raw `Update`/`Delete`, and the age column rendered `3d`/`40m`.

### 3. Submit-for-review affordance
`IVersionedEditTarget` (Sta.Versioning) lets the UI tier recognise a versioned source
without referencing persistence. Two uses: a legend badge marks versioned layers, and
`MapViewModelBase.HandleRequestSaveChanges` now states — before the write — how many edits
will be submitted and that the map keeps showing the approved state. Backing out cancels;
ordinary layers pass straight through. The session title defaults to the layer name so the
review/history "session" column is never blank.

### 4. Demo data seeded into production
Driven through the real services (not hand-written rows) by a scratchpad harness, after a
dry run proved a commit touches only `comments` + the three audit stamps with the geometry
byte-identical. Ten scenarios on TrLineSeg: open competition, resolved-awaiting-approval
(×2), committed-with-history (×2), single update, single delete, closed-no-winner,
returned, queued-behind-approval, stale, and two groupable creates.

Verified through the deployed API: review queue **8**, approval queue **2**, inbox **4**,
my-proposals **5**, two timelines with one hop each. Personas: a@a.com = رضا موسوی
(editor/reviewer/approver), b@b.com = علی رضایی, c@c.com = سارا محمدی — display names set
because live edits take the name from `dbo.User`. b@b.com already had all four permissions
via SuperAdmin; c@c.com got a new role «ویرایشگر نسخه‌بندی» (Edit + HistoryRead).
Integrity stamps left null, which `IntegrityRestampService` documents as "not yet stamped"
and the guard treats as no violation — the state every seeded RBAC row is already in.

---

## Next steps (Hossein)
1. Review + commit the uncommitted tree; **rebuild and redeploy the client** — the seeded
   data is already live behind the deployed API, but the restyled UI is not.
2. Role setup + runtime smoke (`m6-pilot.md` §2–§3 — overlay drawing, the legend badge and
   the submit confirmation are the pieces never exercised in the running app).
3. Cohort walkthrough (§4), then production pilot + 2–4-week evaluation.
