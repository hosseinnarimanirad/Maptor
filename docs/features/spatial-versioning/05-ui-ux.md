# Spatial Versioning — Stage 5: UI/UX Specification

**Status:** BASELINED 2026-08-13 — Q41–Q44 confirmed with recommended defaults → D39–D42
**Last updated:** 2026-08-13
**Prerequisites:** docs 01–04 (decisions D1–D38)

WPF views for the versioning workflow. Semantics are fixed by docs 02–04; this document
specifies screens, layouts, affordances, and status language. Visual polish (exact
colors, spacing) follows the existing Jab.Wpf theme at implementation time.

Markers: **[A]** = follows from a D-decision. Former [P] proposals were all confirmed
2026-08-13 and now read **[A→D39]**…**[A→D42]**.

---

## 0. Principles

- **Persian-first**, strings via the existing Jab.Core resx conventions; layouts RTL-aware.
- **No timers [A: D37]:** every list view has a prominent Refresh button and a
  "last refreshed at" label; data loads on open.
- **Strict separation [A: D34]:** the normal map experience is untouched. Versioning
  enters through: the save routing (invisible), three on-demand map actions (own-pending
  overlay, pending-status check, history), and the role-gated Versioning menu.
- **Server is authority:** guard functions from `Sta.Versioning` only enable/disable
  buttons; every action handles a server rejection gracefully (error codes → doc 04 §8).
- **Editor-facing status collapse [A: D17, doc 02 §7 amendment]:** editors never see
  provisional review outcomes. Internal → shown label mapping:

| Internal proposal state | Editor sees |
|---|---|
| Submitted (singleton) | در انتظار بررسی — "Pending review" |
| Submitted (in competition) | در رقابت (N) — "In competition (N)" |
| SelectedForApproval | در حال بررسی — "Under review" |
| ProvisionallyRejected | در حال بررسی — "Under review" (identical, deliberately) |
| Committed | تثبیت‌شده — "Committed" |
| Rejected | رد شده — "Rejected" (+ reason) |
| Withdrawn | پس‌گرفته — "Withdrawn" (+ cause) |

---

## 1. Navigation & map entry points

**Saba shell menu "Versioning"** (items appear per permission, doc 04 §7):
My Pending (VersionEdit) · Review Queue (VersionReview) · Approval Queue (VersionApprove)
· Inbox (any role, unread badge updates on refresh only [A: D37]).

**Map/feature context (versioned layers only, from the D26 registry):**
- *Show my pending changes* — toggles the own-pending overlay [A: D34]: proposals drawn
  as separate features in the pending style; delete-proposals drawn as a hatched ghost
  over the live feature.
- *Check pending status* — on the selected feature; result popup: "N pending proposals:
  ‹author names›; one of them is yours" [A: D34]. No content shown.
- *History…* — opens the feature timeline (§8), permission VersionHistoryRead.

---

## 2. Submit flow

Editing and digitizing are exactly today's tools [A: D27]; the Save command on a
versioned layer opens the **Submit dialog** instead of syncing:

```
┌─ Submit changes — layer: Transmission Lines ─────────────────┐
│ Title    [ optional                       ]                  │
│ Comment  [ optional, multiline            ]                  │
│                                                              │
│  ▸ Adds (2)      ▸ Updates (14)      ▸ Deletes (1)           │
│  ┌──────────────────────────────────────────────────────┐    │
│  │ ✎ Line 4021   update   ⚠ supersedes your pending #88 │    │
│  │ ✚ (new)       create   ⚠ overlaps live feature 3377  │    │
│  │ …                                                    │    │
│  └──────────────────────────────────────────────────────┘    │
│                              [ Submit ]  [ Cancel ]          │
└──────────────────────────────────────────────────────────────┘
```

Self-supersede warnings (D22) and live-overlap advisories (D30, kind 2) appear inline
per row — advisories are informational, never blocking. After submission, the **Result
dialog** lists per-proposal outcomes (pending / in competition (N) / superseded #id), then
the map simply continues showing live truth [A: D34]; a status-bar hint points to the
overlay toggle and My Pending. If the submission was rejected wholesale
(`VersionedLayerWriteRejected` race, validation), the draft stays intact locally.

---

## 3. My Pending panel (dockable)

Tree grouped by session (title · submitted at · comment) → proposal rows: feature label,
change-type icon, collapsed status (§0 table), competition count with expandable author
list [A: D34]. Toolbar: Refresh · Withdraw proposal (guard D25, confirm) · Withdraw
session (guard D9, confirm) · Zoom to feature · Show on map (overlay). A closed
competition row links to its full record (D21). Rejected rows show the reason inline.

---

## 4. Review Queue (VersionReview)

```
┌─ Review queue ── layer [All ▾]  age [Any ▾]  ⟳ Refresh (12:41) ─┐
│ COMPETITIONS (3)                                                │
│  ⚔ 2  Line 4021       3d  ⚠stale        [ Compare ]            │
│  ⚔ 3  Substation 77   1d                [ Compare ]            │
│  ⚔ 2  (creates) 🔗suggested-group       [ Compare ] [Dismiss]  │
│ SINGLES (481)                 [☑ select all] [ Accept selected ]│
│  ☑ ✎ Line 3980        2h                [ Compare ]            │
│  ☑ ✚ (new substation) 2h  ⚠overlaps-pending → [ Group… ]       │
│  …                                                             │
└────────────────────────────────────────────────────────────────┘
```

- Badges: age, stale (D8), orphan (D23 — opens in reject-only mode), schema-mismatch
  (doc 03 §5.3), overlap suggestion (D30).
- **Bulk accept** of selected singles = one confirmation dialog with count + expandable
  list [A→D40]; per-row failures (E4 races, guards) reported in the result, the rest
  succeed.
- **Group…** merges selected create-proposals of the same layer into one competition
  (D16); suggestion chips offer one-click grouping of the suggested pair; Dismiss records
  the reviewer + name (D30/D31).

---

## 5. Competition Compare view (the centerpiece)

```
┌─ Competition — Line 4021 (2 proposals) ─────────── ⚠ STALE: live changed since base ─┐
│ ┌ proposals ─────────────┐ ┌ map ──────────────────────────────────────────────────┐ │
│ │ ● live (neutral)   ☑   │ │   live geometry + each visible proposal in its color, │ │
│ │ ● A: Ali R.  3d    ☑   │ │   vertex markers on differences                       │ │
│ │ ● B: Sara M. 1d    ☑   │ │                                                       │ │
│ │   (DELETE renders as   │ └───────────────────────────────────────────────────────┘ │
│ │    hatched ghost)      │ ┌ attributes ───────────────────────────────────────────┐ │
│ │ [Select A as winner]   │ │ field      │ live      │ A          │ B               │ │
│ │ [Select B as winner]   │ │ voltage    │ 230       │ 230        │ ▌400▐           │ │
│ │ [Close — no winner]    │ │ status     │ active    │ ▌repair▐   │ active          │ │
│ └────────────────────────┘ │ …changed cells highlighted per column vs live         │ │
│                            └───────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────────────────┘
```

- **N-way attribute grid** [A→D39]: one column per proposal beside live; changed cells
  highlighted (reuse `DictionaryHelper.GetChangedAttributes` per column). Session comment
  and author (D31) in each column header tooltip.
- Delete proposals appear as an explicit DELETE side (D12): hatched ghost on map, struck
  column in the grid.
- **Select winner** → dialog: rejection reason per loser (or one applied to all) +
  stale-override checkbox when the stale banner is up (D8) → confirm. **Close — no
  winner** → reasons dialog (D20, immediate finality warning). Orphaned competitions
  (D23) show only reject actions.
- Banner area: stale (base vs live info), orphan, schema-mismatch (lists problem fields,
  doc 03 §5.3).

---

## 6. Approval Queue (VersionApprove)

Rows: feature/layer · winner author · resolving reviewer · resolved at · fresh-stale
badge (re-checked on refresh). Multi-select → **Commit** (one confirmation with count;
all-or-nothing result per E9 — failures listed with codes, nothing partial). Row detail =
Compare view read-only + decision summary → **Return** with reason (D17). Same-person
review+approve is permitted; the UI shows both identities on the record (D10).

---

## 7. Inbox

Manual refresh only [A: D37]. Digest rows (D24): type icon · title ("Session 'X': 480
committed, 15 rejected") · expandable per-feature detail with reasons · read/unread.
Row actions: jump to session (My Pending) or competition record (D21 rules).

---

## 8. History timeline (per feature)

Opened from the feature context menu. Newest-first rows: commit timestamp · editor name ·
approver name (D31) · session comment · competition-size chip; topmost row = current live.
Actions: **Show this version on map** — historical geometry drawn alongside current in a
distinct style [A→D41]; **Compare two versions** — 2-way reuse of the compare view;
**As-of…** date picker (doc 03 §5.4). Competition-size chip links to the closed record
(access per D21). A committed delete appears as the terminal row.

---

## 9. Color & badge semantics (theme tokens at implementation)

| Meaning | Convention |
|---|---|
| Pending (own overlay, queue rows) | orange family |
| In competition | orange + count badge ⚔ |
| Committed / accepted | green |
| Rejected / delete-side | red; deletes hatched |
| Stale | amber ⚠ |
| Orphaned | gray ⚠ |
| Historical geometry | desaturated blue, dashed outline |
| Proposal colors in compare | fixed distinct palette (A, B, C…), colorblind-safe |

---

## 10. Failure & empty states

Every server rejection maps an error code (doc 04 §8) to a Persian message with the next
step ("این عارضه در حال تأیید است — پس از تثبیت دوباره ارسال کنید" for
`CompetitionUnderApproval`, etc.). Empty states say what the view is *for* ("No pending
proposals — changes you submit on versioned layers appear here"). Destructive actions
(withdraw, reject-all, commit, return) always confirm and always echo the count.

---

## 11. Stage-5 detail questions (Q41–Q44) — all ANSWERED 2026-08-13

Confirmed with the recommended defaults → **D39–D42** (doc 01 §2). Table kept for
rationale/impact notes.

| # | Question | Recommendation | Impact if changed |
|---|---|---|---|
| Q41 | Compare view uses a single **N-way attribute grid** (live + one column per proposal), not pairwise tabs? | Yes (§5) — N is almost always 2–3 | Pairwise tabs scale to large N but hide the overview; N-way grid is the reviewer's whole job on one screen |
| Q42 | Bulk-accepting singles takes **one confirmation** (count + expandable list), no per-item step? | Yes (§4) — R5 throughput | Per-item ceremony recreates the 500-click problem the fast path exists to solve |
| Q43 | History "show version on map" **overlays** the historical geometry beside current (distinct style), rather than temporarily replacing the live rendering? | Yes (§8) — the map never lies (D34 spirit) | Replace-mode previews are clearer for big changes but show non-live data as the layer |
| Q44 | Versioning views live **inside `IRI.Maptor.Jab.Wpf`** (own folder, beside `FeatureChangesView`) rather than a new UI project? | Yes — lowest friction, matches precedent | A separate `Jab.Wpf.Versioning` project isolates dependencies but adds packaging overhead for one consumer (v1) |

---

## 12. Handoff to Stage 6 (implementation plan)

All design inputs are decided once Q41–Q44 are confirmed. Stage 6 turns docs 02–05 into
milestones over the pilot (D38: transmission lines + substations); suggested build order
to elaborate there: shared model + persistence → submission path + Sync gate → review
(queue, compare, decisions) → commit + history → notifications/inbox → map entry points
(overlay, status check, timeline) → pilot enablement + operator walkthrough.
