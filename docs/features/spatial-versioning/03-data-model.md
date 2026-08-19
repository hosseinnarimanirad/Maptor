# Spatial Versioning — Stage 3: Data Model

**Status:** BASELINED 2026-08-13 — Q30–Q36 confirmed → D27–D33 (Q34 in modified form: display names stored, see D31); Q17 → D26
**Last updated:** 2026-08-13
**Prerequisites:** `01-requirements-and-boundaries.md` (D1–D25), `02-workflow-and-state-machine.md` (states, transitions, constraints in its §13)

This document defines *storage*: entities, columns, constraints, indexes, the history
strategy, the commit algorithm, and the schema-drift gate. It does not define API
endpoints (Stage 4) or UI (Stage 5). Field names/types are the proposal to build EF Core
configurations from — refinement during implementation is expected; *structural* changes
go back through the decision log.

Markers: **[A]** = follows from a D-decision. Former [P] proposals were confirmed
2026-08-13 and now read **[A→D27]**…**[A→D33]**; Q34 was accepted in **modified** form
(D31: display names stored at write time, not resolved at query time).

---

## 1. Design principles

1. **Polymorphic, serialized proposals [A: R1/D16].** One generic proposal store serves
   all ~100 feature tables: target = (versioned-layer id, feature id), proposed state =
   geometry column + attributes JSON. No per-table pending tables.
2. **Copy-on-write history [A→D28].** No baseline snapshot when a layer opts in. At every
   commit, the live row being overwritten/deleted is first copied into `FeatureHistory`.
   History size is proportional to change volume, not data volume; time-travel = walk the
   chain backward from live. Pre-versioning history simply doesn't exist (acceptable: the
   audit obligation starts when versioning starts).
3. **Append-only decisions [A: D13].** `DecisionRecord` rows are never updated or deleted.
4. **Shared-library clean [A: D3].** Core entities live in Maptor shared projects and
   reference Saba users as scalar ids **plus a display name stamped at write time**
   [A→D31]; all versioning tables sit in their own SQL schema `versioning`, away from the
   feature tables.
5. **Provider reality [A: §6 of doc 01].** Geometry uses the custom Maptor provider (no
   NTS). Proposal/history geometry columns are typed as SQL Server **`geography`**
   (pass-through mapping) rather than `varbinary` [A→D32, amended by D45] — matching the
   live SHAPE columns byte-for-byte, enabling spatial indexes and direct raw-SQL
   `STIntersects` against live data. Geography spatial indexes need no bounding box.
6. **Flags are computed, not stored.** Stale (base RowVersion ≠ live) and Orphaned (live
   row gone) are computed in queue queries by joining live tables — they can never go
   out of date.

---

## 2. Entity catalog

All tables in schema `versioning`. `RowVersion` columns are SQL `rowversion` concurrency
tokens. All timestamps UTC.

### 2.1 VersionedLayer — per-layer opt-in registry [A: S6]

| Column | Type | Notes |
|---|---|---|
| Id | int PK identity | |
| LayerKey | uniqueidentifier, unique | Matches client `FeatureSet.LayerId` |
| EntityName | nvarchar(200), unique | Server entity/table identity (e.g. `TransmissionLine`) |
| DisplayName | nvarchar(200) | |
| IsVersioningEnabled | bit | Gate for the Sync path (Q17, Stage 4) |
| SchemaSignature | nvarchar(100) | Current schema hash, §5; recomputed at API startup [A→D33] |
| SchemaSignatureUpdatedAt | datetime2 | |
| EnabledAt | datetime2 | History starts here (principle 2) |

### 2.2 VersionSession — submission batch [A: D6, D9, D11]

| Column | Type | Notes |
|---|---|---|
| Id | bigint PK identity | |
| EditorUserId | bigint | Scalar, no FK [A→D31] |
| EditorDisplayName | nvarchar(200) | Stamped at submission [A→D31] |
| Title | nvarchar(200), null | |
| Comment | nvarchar(1000), null | |
| State | tinyint | Submitted=1, Withdrawn=2 — **no Draft row [A→D27]**: drafts live client-side; the session row is created at submission |
| SubmittedAt | datetime2 | |
| WithdrawnAt | datetime2, null | |
| RowVersion | rowversion | |

Index: (EditorUserId, State). Derived statuses (InReview/PartiallyResolved/Resolved) are
queries over proposals, never columns [A: D6].

### 2.3 Proposal — one proposed create/update/delete [A: FR-1]

| Column | Type | Notes |
|---|---|---|
| Id | bigint PK identity | |
| SessionId | bigint FK → VersionSession | |
| EditorUserId | bigint | Denormalized from session — needed for the D22 unique index and visibility queries |
| EditorDisplayName | nvarchar(200) | Stamped at submission [A→D31] |
| VersionedLayerId | int FK → VersionedLayer | |
| TargetFeatureId | bigint, null | Null for creates |
| ClientKey | uniqueidentifier | From `Feature<T>.Key`; the create's pre-commit identity (candidate mapping to live `gis_id` — note in §5.2) |
| ChangeType | tinyint | Create=1, Update=2, Delete=3 |
| ProposedGeometry | geography, null | Null for Delete [A→D32/D45] |
| ProposedAttributesJson | nvarchar(max), null | Null for Delete; contract in §5.1 |
| BaseRowVersion | binary(8), null | Null for Create [A: FR-1.4] |
| SchemaSignatureAtSubmit | nvarchar(100) | R1 gate input |
| CompetitionId | bigint FK → Competition | Every submitted proposal belongs to exactly one (doc 02 §2) |
| State | tinyint | Submitted=0, SelectedForApproval=1, ProvisionallyRejected=2, Committed=3, Rejected=4, Withdrawn=5 — active states deliberately ≤2 for filtered indexes |
| WithdrawCause | tinyint, null | User=1, SessionWithdrawn=2, Superseded=3 [A: D22/D25] |
| SubmittedAt / DecidedAt / FinalizedAt | datetime2 (last two null) | |
| RowVersion | rowversion | |

Constraints & indexes:
- **D22 (self-supersede backstop):** unique filtered index on
  (VersionedLayerId, TargetFeatureId, EditorUserId) `WHERE State <= 2 AND TargetFeatureId IS NOT NULL`.
  The service layer performs the supersede; the index makes races lose loudly.
- Collision lookup: index (VersionedLayerId, TargetFeatureId) `WHERE State <= 2`.
- Index CompetitionId; index SessionId.
- Spatial index on ProposedGeometry (overlap scan; geography grid) [A→D32/D45].

### 2.4 Competition — the decision unit [A: S3, doc 02 §4]

| Column | Type | Notes |
|---|---|---|
| Id | bigint PK identity | |
| VersionedLayerId | int FK | |
| TargetFeatureId | bigint, null | Null for manually-grouped create competitions [A: D16] |
| Kind | tinyint | IdCollision=1, ManualGroup=2 |
| State | tinyint | Open=0, Resolved=1, Committed=2, ClosedNoWinner=3, Dissolved=4 |
| WinnerProposalId | bigint, null, FK → Proposal | Set at Resolved. Circular FK with Proposal.CompetitionId — configure both without cascade delete |
| PredecessorCompetitionId | bigint, null, self-FK | Queued chain [A: D19] |
| CreatedAt / ResolvedAt / FinalizedAt | datetime2 (last two null) | |
| RowVersion | rowversion | Guards E4 (concurrent reviewers/approvers) |

Constraints (**D19**): two unique filtered indexes on (VersionedLayerId, TargetFeatureId):
one `WHERE State = 0 AND TargetFeatureId IS NOT NULL`, one `WHERE State = 1 AND
TargetFeatureId IS NOT NULL` — at most one Open and one Resolved competition per feature,
enforced by the database itself.

### 2.5 DecisionRecord — append-only audit of every action [A: FR-5.1, D13]

| Column | Type | Notes |
|---|---|---|
| Id | bigint PK identity | |
| CompetitionId | bigint FK | |
| ProposalId | bigint, null, FK | Null for competition-level actions (Return) |
| ActorUserId | bigint | |
| ActorDisplayName | nvarchar(200) | Stamped at decision time [A→D31] |
| Action | tinyint | SelectWinner=1, RejectProposal=2 (one row per loser, with reason), CloseNoWinner=3, Approve=4, Return=5 |
| Reason | nvarchar(1000), null | Required by service for Reject/CloseNoWinner/Return [A: FR-3.5, D17] |
| IsStaleOverride | bit | Set on SelectWinner/Approve when D8 override was exercised |
| CommitBatchId | bigint, null, FK | Set on Approve |
| CreatedAt | datetime2 | |

No UPDATE/DELETE ever (enforce by convention + optionally a DENY or trigger later).
Indexes: CompetitionId; ActorUserId.

### 2.6 CommitBatch — one approval transaction [A: NFR-3, E9]

| Column | Type | Notes |
|---|---|---|
| Id | bigint PK identity | |
| ApproverUserId | bigint | |
| ApproverDisplayName | nvarchar(200) | Stamped at commit [A→D31] |
| CommittedAt | datetime2 | |
| CompetitionCount | int | Convenience |

### 2.7 FeatureHistory — copy-on-write past states [A→D28]

Written inside the commit transaction, *before* live is overwritten or deleted. Creates
write no history row (nothing was replaced).

| Column | Type | Notes |
|---|---|---|
| Id | bigint PK identity | |
| VersionedLayerId | int FK | |
| FeatureId | bigint | |
| Geometry | geography | The state being replaced [A→D32/D45] |
| AttributesJson | nvarchar(max) | Serialized with the same contract (§5.1) |
| ReplacedRowVersion | binary(8) | The RowVersion this state had |
| CommitBatchId | bigint FK | Which commit replaced it |
| WinningProposalId | bigint FK → Proposal | What replaced it (author, session, competition all reachable from here) |
| SupersededAt | datetime2 | = batch CommittedAt |

Index: (VersionedLayerId, FeatureId, SupersededAt DESC).
Answering FR-5.3 ("what did F look like before commit X"): the row with CommitBatchId = X;
full timeline: live row + chain of history rows descending. A committed *delete* leaves the
last state here with the delete-proposal as its replacer — nothing is ever lost.

### 2.8 VersionNotification — in-app inbox [A: D15, D24]

| Column | Type | Notes |
|---|---|---|
| Id | bigint PK identity | |
| RecipientUserId | bigint | |
| Type | tinyint | N1..N6 from doc 02 §8 |
| SessionId / CompetitionId | bigint, null | Digest anchors |
| PayloadJson | nvarchar(max) | Counts, reasons, feature refs — expandable detail |
| CreatedAt / ReadAt | datetime2 (ReadAt null) | |

Digests [A: D24] are formed at write time: one row per (recipient × session × event batch).
Index: (RecipientUserId, ReadAt).

### 2.9 OverlapSuggestion — reviewer grouping aid [A→D30]

Computed once at submission (raw-SQL bbox + `STIntersects`), persisted so the reviewer
queue is cheap to render and suggestions are dismissable.

| Column | Type | Notes |
|---|---|---|
| Id | bigint PK identity | |
| ProposalId | bigint FK | The newly submitted proposal |
| OverlapKind | tinyint | PendingVsPending=1 (reviewer-only, blind rule E8), PendingVsLive=2 |
| OtherProposalId | bigint, null, FK | For kind 1 |
| LiveFeatureId | bigint, null | For kind 2 |
| ComputedAt | datetime2 | |
| DismissedByUserId / DismissedByDisplayName / DismissedAt | bigint / nvarchar(200) / datetime2, null | Reviewer "not the same object" [A→D31] |

---

## 3. Relationships (summary)

```
VersionedLayer 1──* Proposal            VersionSession 1──* Proposal
VersionedLayer 1──* Competition         Competition    1──* Proposal (CompetitionId)
Competition    0..1──1 Proposal (WinnerProposalId)     Competition 0..1──0..1 Competition (Predecessor)
Competition    1──* DecisionRecord      CommitBatch    1──* DecisionRecord (Approve rows)
CommitBatch    1──* FeatureHistory      Proposal       1──* FeatureHistory (WinningProposalId)
Proposal       1──* OverlapSuggestion   (users: scalar ids + stored display names, no FKs [A→D31])
```

---

## 4. Enums (shared core)

`ProposalChangeType` {Create, Update, Delete} · `ProposalState` {Submitted,
SelectedForApproval, ProvisionallyRejected, Committed, Rejected, Withdrawn} ·
`WithdrawCause` {User, SessionWithdrawn, Superseded} · `SessionState` {Submitted,
Withdrawn} · `CompetitionState` {Open, Resolved, Committed, ClosedNoWinner, Dissolved} ·
`CompetitionKind` {IdCollision, ManualGroup} · `DecisionAction` {SelectWinner,
RejectProposal, CloseNoWinner, Approve, Return} · `NotificationType` {N1..N6}.

Client-side `FeatureStatus` (existing, in `Sta.Common`) stays untouched — it describes the
in-memory draft; `ProposalState` starts where the draft ends [A: Q30 model].

---

## 5. Key mechanics

### 5.1 Serialization contract

- **Attributes JSON:** flat object, keys = field names, values as JSON natives; dates ISO
  8601 UTC; numbers invariant-culture; keys sorted ordinally (canonical form → diffable,
  hashable). Excluded: `RowVersion` (own column), identity/computed columns, the audit
  stamps `CreatedBy*/LastUpdated*` (assigned at commit, R10).
- **Geometry:** the provider's native SQL Server binary, stored in a `geography` column
  [A→D32, amended by D45]; SRID preserved; same bytes the map stack already reads/writes.
- **SchemaSignature:** SHA-256 (hex, truncated 32 chars) over the ordered list of
  `FieldName:StoreType:IsNullable` for the entity's non-excluded columns. Computed from
  EF model metadata at API startup and stamped into `VersionedLayer` [A→D33] — no manual
  migration step to forget; a changed signature after deploy is *detected*, not declared.

### 5.2 Commit transaction (approver, batch — implements P6/C6/E9)

One DB transaction per `CommitBatch`:

1. Load the batch's competitions with `RowVersion` check — all must still be `Resolved`.
2. Per winner: load the live row. **Stale gate [A: D8]:** live RowVersion ≠ proposal's
   BaseRowVersion and no covering override recorded at approve time → abort batch,
   report. **Schema gate [A: R1/E3]:** `SchemaSignatureAtSubmit` ≠ layer's current →
   attempt tolerant mapping (§5.3); unresolvable → abort batch, report.
3. Copy-on-write: Update/Delete → insert `FeatureHistory` from the live row.
4. Apply: Create → insert live row, `CreatedBy` = **editor id + display name** [A: R10,
   D31] — matching the existing `FeatureBaseEntity.CreatedByFullName` convention (note:
   candidate — stamp `gis_id` from ClientKey, resolve in implementation); Update →
   overwrite live, `LastUpdatedBy*` = editor id + name; Delete → delete live row.
5. Write `CommitBatch`, Approve `DecisionRecord`s, state flips (winner → Committed,
   provisionally rejected → Rejected [A: D17]), competition → Committed.
6. Write digest notifications (N2/N3).
7. `SaveChanges` — any failure rolls back everything [A: E9].

Live-table writes go through the same `BargContext` (same transaction) using the entity
types — no raw SQL needed for the apply step; the JSON→entity mapping reuses the sync
pipeline's attribute-dictionary mapping.

### 5.3 Schema-drift tolerant mapping [A: E3]

At review-display and at commit, when signatures differ, diff the field lists:
- **Added nullable column** since submit → apply with NULL — warning, allowed.
- **Added non-nullable (no default)** → block.
- **Removed column** → drop the value — warning, allowed.
- **Type change / rename** (appears as remove+add) → block; reviewer/approver sees which
  fields; resolution = return/reject, editor resubmits under the new schema.
Blocks are per-proposal but abort the whole batch (E9); the approver retries without the
blocked competitions.

### 5.4 Time-travel queries

`GetFeatureAsOf(layer, featureId, dateTime)`: latest `FeatureHistory` row with
`SupersededAt > dateTime` gives the state *at* that time (or the live row if none).
`GetFeatureTimeline(layer, featureId)`: history rows ascending + live; each hop links its
`WinningProposalId` → author, session, competition, decisions. Serving FR-5.3 for
audit/inspection UI — mass time-slice *map rendering* is explicitly not a v1 target.

### 5.5 Overlap scan (submission-time, raw SQL)

For each submitted proposal with geometry: one query against pending proposals of the same
layer (`State <= 2`, other editors) and one against the live table, both
`bbox-filter AND geometry.STIntersects(@g) = 1`. Results → `OverlapSuggestion` rows
[A→D30]. Kind 1 feeds the reviewer queue (E8 blind rule); kind 2 returns in the submission
response as the editor-facing advisory (§1 of doc 02).

---

## 6. EF Core & project integration

- **New shared projects [A→D29]:**
  - `IRI.Maptor.Sta.Versioning` (netstandard2.1): entities, enums, state-machine guard
    logic (pure functions: may-transition checks), serialization contract, signature
    computation — no EF dependency.
  - `IRI.Maptor.Ket.VersioningPersistence`: `IEntityTypeConfiguration`s, the
    `modelBuilder.AddMaptorVersioning(schema: "versioning")` extension, repositories/
    services (submission, collision/competition service, review service, commit service,
    overlap scan).
- **Saba integration:** `BargContext` calls `AddMaptorVersioning()`; one EF migration adds
  the `versioning` schema. Controllers/handlers (Stage 4) sit in the existing
  Presentation/Application layers. WPF UI (Stage 5) goes into Jab per doc 01 FR/Stage 5.
- **User references:** scalar `…UserId` columns plus explicit `…DisplayName` columns
  stamped at write time; no FKs to Saba's Users table [A→D31]. Names are historical
  facts, not lookups — decision history outlives any user-account lifecycle and renders
  without joins.
- **Migrations note (R1):** the versioning tables themselves are ordinary EF migrations;
  the *feature-table* migration checklist gains one automatic behavior — signatures are
  recomputed at startup [A→D33] and pending proposals against changed layers get flagged
  in the review queue immediately.

---

## 7. Sizing & performance notes

- Proposal row ≈ 1–10 KB (JSON) + geometry. Even pessimistic pilot use (10 editors ×
  500 features/day) is ~50 MB/month — negligible for SQL Server.
- History grows only with commits (copy-on-write) — proportional to real change, forever
  retention (D13) is viable; revisit only if a bulk-recapture project lands.
- Queue queries join live tables per layer for stale/orphan flags — indexed PK joins,
  fine at pilot scale; if the pending set ever reaches 10⁵+, add a cached flag refreshed
  on commit (not now).
- Spatial index on `Proposal.ProposedGeometry` keeps the overlap scan sub-second; it runs
  once per submission, never interactively.

---

## 8. Stage-3 detail questions (Q30–Q36) — all ANSWERED 2026-08-13

Confirmed with the recommended defaults → **D27–D33** (doc 01 §2), except **Q34 accepted
in modified form**: display names are stored explicitly at write time alongside the ids
(D31), not resolved at query time. Table kept for rationale/impact notes.

| # | Question | Recommendation | Impact if changed |
|---|---|---|---|
| Q30 | Draft sessions are **client-side only** — the server session row is created at submission (no server-side draft autosave)? | Yes — matches D14 and today's edit flow; a crash loses only the local draft, as it does today | Server-side drafts = an autosave/sync subsystem: chattier API, draft privacy handling, real scope growth |
| Q31 | History = **copy-on-write at commit**, no baseline snapshot at layer enablement (history starts empty at EnabledAt)? | Yes — storage ∝ change volume; baseline of ~100 tables would duplicate the whole DB | Baseline gives pre-versioning time-travel at heavy storage + capture-job cost |
| Q32 | New shared projects `IRI.Maptor.Sta.Versioning` (model, no EF) + `IRI.Maptor.Ket.VersioningPersistence` (EF + services), tables in SQL schema `versioning`? | Yes — mirrors the existing Sta/Ket split (D3) | Building inside Saba first = faster start, extraction debt later |
| Q33 | Overlap suggestions **persisted** (dismissable rows, computed once at submission) rather than computed on the fly in the queue? | Yes — stable, cheap queue, dismissals remembered | On-the-fly = no table, but repeated spatial queries per queue load and no dismiss memory |
| Q34 | User references as **scalar ids only** (no FK to Saba Users), names resolved at query time? | Yes — shared-lib clean, history survives account lifecycle | FKs give referential comfort but couple core tables to Saba and complicate user archival |
| Q35 | Proposal/history geometry columns typed **SQL `geometry`** (spatial index + STIntersects) instead of varbinary? | Yes — the pass-through mapping already exists; overlap scan needs it | varbinary = no spatial queries on pending data; overlap scan would need client-side filtering |
| Q36 | SchemaSignature **computed automatically from the EF model at API startup** and stamped into VersionedLayer? | Yes — drift is detected, never declared; no manual step to forget | Manual/migration-time stamping risks the exact silent failure R1 warns about |

---

## 9. Handoff to Stage 4 (architecture & integration)

Q17 is answered → **D26** (per-layer gate; the `IsVersioningEnabled` flag in §2.1 anchors
it) and Q30–Q36 → D27–D33. Stage 4
then covers: API surface (submission, queues, compare payloads, decisions, commit, inbox),
where competition/diff logic runs (server vs client), Jab.Wpf hosting of the versioning
views vs a new UI library, and the authorization mapping of the four `Version*` permissions
into Saba's Permission enum. Q15 (pilot layers) stays open until Stage 6.
