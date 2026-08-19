# M4 working notes — commit + history

**Last updated:** 2026-08-14 — **M4 COMPLETE at service+API level, harness 25/25 PASS.**

## What was built (uncommitted — Hossein reviews & commits)

### LiveEntityWriter (`Ket.VersioningPersistence/Services/`)

Generic "winning proposal → live table" apply via EF metadata — no per-entity code for
~100 tables. Creates instantiate the CLR type and stamp `CreatedBy*`/`LastUpdatedBy*`
with the **editor's** id + display name (R10/D31 — never the approver's); updates
overwrite the full state + `LastUpdatedBy*`; canonical-JSON primitives convert to column
CLR types (ISO dates, base64 byte[], Guid, invariant numerics) — a failed conversion is a
`SchemaMismatch` that blocks the whole batch; unknown attribute keys are dropped with a
warning (tolerant mapping, doc 03 §5.3). **gis_id is deliberately NOT stamped from
ClientKey** — the column is 20 chars, a Guid string is 36 (doc 03 §5.2's candidate,
resolved: skip). Live audit columns are **FK'd to dbo.User**, so editor ids are always
real user ids (harness lesson).

### CommitService

- **Approval queue**: Resolved competitions with the resolving reviewer's name (from the
  SelectWinner decision record) and **freshly re-checked** stale/orphan flags.
- **CommitAsync** (doc 03 §5.2, all-or-nothing E9, execution-strategy + ambient-aware):
  per item — competition RowVersion echo, `CanApprove` guard, orphan → blocked (return is
  the only path, D23), **approval-time stale gate** with its own recorded override (D8),
  live-row RowVersion set as OriginalValue (race window closed to SaveChanges),
  **copy-on-write `FeatureHistory`** captured from the tracked live row before the
  overwrite/delete, apply via LiveEntityWriter, state flips (winner Committed, provisional
  losers → finally Rejected per D17), Approve decision record per competition, one
  CommitBatch row. After the first save, **create ids are backfilled** onto
  proposal+competition `TargetFeatureId` so timelines can find them. Then the deferred
  notifications: N2 digests to winners, N3 digests to losers **with the review-time
  rejection reasons**, second save, one transaction around everything.
- **ReturnAsync**: Resolved → Open, winner cleared, all proposals back to Submitted
  (provisional rejections evaporate, D17), Return decision record with mandatory reason,
  N5 digest to the resolving reviewer.

### HistoryService

`GetFeatureTimelineAsync`: live snapshot + copy-on-write hops newest-first, each with the
replacing proposal's provenance (editor, change type, session title, approver, batch).
As-of lookups derive from this client-side (doc 03 §5.4).

### Verified vs STAGING — 25/25 PASS (rolled-back harness)

Full lifecycle: two editors compete → select → approval queue → **commit** → live value +
editor name stamp + advanced RowVersion + history hop with old value/RowVersion/winner
linkage + final loser rejection + N2/N3 (reason included) + correct timeline. Create
commit (id backfill, creator stamps, no history hop). Delete commit of the created
feature (live gone; timeline Live=null with the last state as a Delete hop). Live drift
after resolution → queue freshly stale → **batch with a healthy item aborts entirely
(E9)** → fresh override commits and is recorded → the drift is overwritten (documented D8
behavior). Return → reopen → N5 → re-resolvable.

### API surface — wired, builds green

`GET /Versioning/Approval/Queue`, `POST /Versioning/Approval/Commit`,
`POST /Versioning/Approval/Competitions/{id}/Return` under policy **"Versioning.Approve"**
(permission 402); `GET /Versioning/History/{layerKey}/{featureId}` under
**"Versioning.HistoryRead"** (403). Gateway → MediatR → controller as before.

## Remaining after M4

- **M5 — editor experience + approver/history UI**: My-Pending panel (status collapse),
  inbox (digests, manual refresh), map entry points (own-pending overlay, pending-status
  check, history context menu), submit-result dialog (SessionSubmitted event is waiting),
  approval-queue window (reuse the review-window pattern), timeline view.
- **M6 — pilot**: role setup, operator walkthrough (covers UI-level acceptance), Substat
  enablement, monitoring.
