# Spatial Versioning — Stage 1: Requirements & Boundaries

**Status:** DRAFT — under discussion with Hossein
**Last updated:** 2026-08-13
**Source prompt:** `docs/features/spatial versioning prompt design.txt`

---

## 0. How to resume this work (read this first)

This is a multi-day planning effort. The plan is a sequence of stage documents, all in
`docs/features/spatial-versioning/`:

| Stage | Document | State |
|---|---|---|
| 1 | `01-requirements-and-boundaries.md` (this file) | Baselined 2026-08-13 (D1–D18; Q15/Q17 still open) |
| 2 | `02-workflow-and-state-machine.md` — lifecycle, states, transitions, role matrix | Baselined 2026-08-13 (Q23–Q29 confirmed → D19–D25) |
| 3 | `03-data-model.md` — EF Core entities, history strategy, concurrency | Baselined 2026-08-13 (Q30–Q36 → D27–D33; Q34 modified) |
| 4 | `04-architecture-and-integration.md` — which libraries, Saba API surface, boundaries | Baselined 2026-08-13 (Q37/Q40 modified → D34/D37; Q38/Q39 → D35/D36) |
| 5 | `05-ui-ux.md` — WPF views: session editor, competition compare, review queue | Baselined 2026-08-13 (Q41–Q44 → D39–D42) |
| 6 | `06-implementation-plan.md` — milestones, pilot scope | Baselined 2026-08-13 (Q45–Q46 → D43–D44) — planning complete; **M1 COMPLETE 2026-08-14** (spike passed, see `m1-notes.md`) |

To resume: read §2 (Decisions so far), then §9 (Open questions) and pick the next
**Open** item. Do not start a later stage while its blocking questions are Open.
Every settled decision gets a row in §2 with a date — nothing is decided in chat only.

---

## 1. Problem statement

Editors of a spatial database (features = geometry + attributes) must be able to
propose changes to individual features without touching the live state. Proposed
changes are grouped into **version sessions**, submitted for review, and flow
through a fixed moderation pipeline. Uniquely, the process is **deliberately
competitive**: multiple editors may be assigned the same feature/area and submit
competing proposals; a reviewer compares them side by side (geometry diff +
attribute diff), selects one winner, and rejects the rest with reasons. Selected
changes are then committed to the live state. Full decision history is preserved.

This is explicitly **not** ESRI layer-versioning: granularity is the individual
feature, and there is no long-lived branch of a whole layer.

---

## 2. Decisions so far

| # | Date | Decision | Rationale / consequence |
|---|---|---|---|
| D1 | 2026-08-13 | Competition is a **deliberate business process**, not just accidental-conflict resolution. | Competition (and probably work assignment) becomes a first-class concept. Accidental collisions still occur and must funnel into the same compare-and-select mechanism. |
| D2 | 2026-08-13 | **Central DB, always online.** All editors reach one SQL Server through the Saba API. | Pending changes live server-side from submission. Offline/check-out editing is out of scope. |
| D3 | 2026-08-13 | **Saba is the first consumer; the core is reusable.** Versioning core (change model, diff, workflow state machine) goes into shared IRI.Maptor libraries; Saba-specific glue stays in Saba. | Core must not depend on Saba entity types. |
| D4 | 2026-08-13 | Pipeline is **fixed, not configurable**. Number of stages still undecided (→ Q5). | No workflow-configuration engine. One person may hold multiple roles (small-team collapse) unless Q20 decides otherwise. |
| D5 | 2026-08-13 | Pipeline = **3 fixed stages**: Edit → Review → Approve & Commit. | Approve is the final gate (stale re-check, transactional batch commit). Role overlap per D10. Resolves Q5. |
| D6 | 2026-08-13 | **Session = unit of submission only; decisions are per-feature.** Sessions may end partially accepted; session state is derived, not authoritative. | Resolves R2/Q6 as recommended. |
| D7 | 2026-08-13 | **Blind competition.** Editors never see rival pending proposals; only reviewers/approvers see competitors. | Visibility detail in D18. Post-closure visibility still open (→ Q25 in doc 02). |
| D8 | 2026-08-13 | Stale proposals: **warn + explicit recorded override** at acceptance; staleness is re-checked (and overridable) again at approval/commit. | Resolves R3/Q14. |
| D9 | 2026-08-13 | Session lifetime is **flexible**: multiple drafts per editor; withdraw while nothing decided; no auto-expiry; proposal age shown prominently in the review queue. | Resolves Q13; partially mitigates R9. |
| D10 | 2026-08-13 | The same person **may** review and approve the same competition; both decisions are identity-stamped. | Separation of duty = organizational policy, not a system constraint. Resolves Q20. |
| D11 | 2026-08-13 | Sessions are **single-editor**. | Resolves Q19. |
| D12 | 2026-08-13 | Delete proposals are **ordinary competitors**; the compare UI renders deletion as a first-class side. | Resolves Q9/R7. |
| D13 | 2026-08-13 | History (rejected + committed) is kept **forever**; no purge in v1. | Resolves Q11. |
| D14 | 2026-08-13 | Draft undo/redo stays the existing **in-memory** mechanism; nothing persisted. | Resolves Q12. |
| D15 | 2026-08-13 | v1 notifications = **in-app inbox, polling**. | Resolves Q16. |
| D16 | 2026-08-13 | **No assignment mechanics.** Editors edit whatever they have access to; competitions form **implicitly by feature-id collision**. Spatial overlap is advisory only: warnings to editors against **live** features; overlap **suggestions** (incl. pending-vs-pending) go to reviewers, who may **manually group** proposals (esp. competing creates) into one competition. | "Deliberate" competition is orchestrated outside the system. Consequences analyzed in doc 02 §1. Replaces the assignment half of S3; resolves Q8/Q10. |
| D17 | 2026-08-13 | Approver **return reopens** the competition: losers' rejections stay provisional until commit; final rejection notifications fire only at commit; on return all proposals revert to Submitted and the reviewer sees the return reason. | Resolves Q21. |
| D18 | 2026-08-13 | Blind detail: a competing editor sees **status + competitor count** on their own proposal — never rival content or author names. | Resolves Q22. *(Author-hiding later amended by D34.)* |
| D19 | 2026-08-13 | Late arrivals during approval open a **queued competition**: at most one Resolved + one Open competition per feature; the queued one cannot resolve until its predecessor is terminal. | Resolves Q23. Stage-3 constraint: filtered unique indexes per competition state. |
| D20 | 2026-08-13 | Reviewer may close a competition **with no winner, without approver involvement** — rejections are final immediately (live is untouched). | Resolves Q24. Fully recorded in decision history. |
| D21 | 2026-08-13 | Post-closure visibility: **participants** see the full record of competitions they took part in (rival content + authors); reviewers/approvers see all; non-participants see committed history only. | Resolves Q25. Widening later is a policy switch. |
| D22 | 2026-08-13 | **Self-supersede**: one active proposal per (editor, feature); a newer submission auto-withdraws the editor's older pending proposal on that feature. | Resolves Q26. Stage-3 filtered unique index. |
| D23 | 2026-08-13 | **Orphaned proposals** (target deleted in live) are reject-only in v1; no accept-as-recreate. | Resolves Q27. |
| D24 | 2026-08-13 | Notifications aggregate as **per-session digests** — never one inbox row per feature. | Resolves Q28. Required by R5 throughput. |
| D25 | 2026-08-13 | **Per-proposal withdraw** allowed while the proposal's competition is Open; a selected winner cannot withdraw during approval. | Resolves Q29. |
| D26 | 2026-08-13 | **Per-layer Sync gate**: layers with versioning enabled reject the direct `Sync` write path with a clear error; non-versioned layers keep today's behavior. | Resolves Q17/S6. Every live write on a versioned layer provably went through review; copy-on-write history stays complete. |
| D27 | 2026-08-13 | Draft sessions are **client-side only**; the server session row is created at submission. | Resolves Q30. A crash loses only the local draft — same as today. |
| D28 | 2026-08-13 | History = **copy-on-write at commit**; no baseline snapshot; a layer's timeline starts at its enablement. | Resolves Q31. |
| D29 | 2026-08-13 | Packaging: new shared projects **`IRI.Maptor.Sta.Versioning`** (EF-free model) + **`IRI.Maptor.Ket.VersioningPersistence`** (EF + services); all tables in SQL schema `versioning`. | Resolves Q32; implements D3. |
| D30 | 2026-08-13 | Overlap suggestions are **persisted** as dismissable rows, computed once at submission. | Resolves Q33. |
| D31 | 2026-08-13 | User references = scalar **UserId + display name stored explicitly at write time**; no FK to Saba Users; names are NOT resolved at query time. | Q34 accepted **in modified form** (Hossein). Matches the existing `FeatureBaseEntity.CreatedByFullName` convention; history survives any account lifecycle. |
| D32 | 2026-08-13 | Proposal/history geometry columns are typed **SQL `geometry`** with spatial indexes. | Resolves Q35. Enables the overlap scan. |
| D33 | 2026-08-13 | Schema signatures are **auto-computed from the EF model at API startup** and stamped into the layer registry. | Resolves Q36. Drift is detected, never declared. |
| D34 | 2026-08-13 | **Live-truth display + strict query separation** (Q37 modified by Hossein): the map always renders the last committed state; versioning data never rides on normal feature/list endpoints. Two on-demand entry points only: (1) a layer query returning the editor's **own** pending proposals as separate overlay features; (2) a per-feature **pending-status check** returning the **count + authors** of pending proposals (usable while editing, even for others' proposals). No automatic versioning calls inside the normal edit flow. | **Amends D18**: rival *authors* become discoverable on demand; rival *content* stays hidden until closure (D21) — the anti-anchoring core of blind competition survives. |
| D35 | 2026-08-13 | Dedicated **`/versioning/*` controllers** — one bounded context, no changes to domain controllers. | Resolves Q38. |
| D36 | 2026-08-13 | Client gateway (**`VersioningWebClient`** + **`VersioningWebDataSource`**) lives in **`IRI.Maptor.Ket.WebApiPersistence`**, beside `WebApiDataSource`. | Resolves Q39; serves the D3 reuse goal. |
| D37 | 2026-08-13 | **No polling in v1** (Q40 modified by Hossein): inbox, own-proposal statuses, and queues refresh manually / on open only. May be improved later. | A timer or push can be added later without API changes. |
| D38 | 2026-08-13 | **Pilot layers: transmission lines + substations.** | Resolves Q15. Exercises vertex-heavy line edits, point/polygon edits, and all change types. |
| D39 | 2026-08-13 | Compare view uses a single **N-way attribute grid** (live + one column per proposal). | Resolves Q41. |
| D40 | 2026-08-13 | Bulk-accepting singleton proposals takes **one confirmation** (count + expandable list); per-row failures reported afterward. | Resolves Q42; serves R5 throughput. |
| D41 | 2026-08-13 | History versions render as an **overlay** beside current, in a distinct style — never replacing live rendering. | Resolves Q43; consistent with D34's live-truth principle. |
| D42 | 2026-08-13 | Versioning views live **inside `IRI.Maptor.Jab.Wpf`** (own folder, beside `FeatureChangesView`); no separate UI project for v1. | Resolves Q44. |
| D43 | 2026-08-13 | Disabling versioning on a layer is **blocked while open proposals exist** — the admin resolves or withdraws them first. | Resolves Q45. No frozen-proposal state; history never gains a gap. |
| D44 | 2026-08-13 | **The current working system serves as staging** (Hossein: data loss there is acceptable); M1's migration test and M6's dry run happen on it directly. | Resolves Q46. |
| D45 | 2026-08-13 | Versioning geometry columns are **`geography`, hardcoded** — amends D32's `geometry`. The extent columns are dropped from VersionedLayer (geography spatial indexes need no bounding box). | M1 recon: Saba live SHAPE columns are geography and the Maptor provider supports both types (defaulting to geography). Matching live means byte-fidelity on commit and direct STIntersects against live in the overlap scan. A future geometry-based consumer would need a parameter added. |
| D46 | 2026-08-14 | **Client-layer matching by TableName** (amends doc 03's LayerKey interpretation): Saba's LayerSetting has no Guids, so `VersionedLayer.LayerKey` is a stable **server-assigned** Guid (hardcoded in `VersioningSeeds`), and the client matches versioned layers by SQL TableName, which the layers endpoint resolves from EF metadata at runtime. | No schema change needed. |
| D47 | 2026-08-14 | **Pilot entities: `TrLineSeg`** (تکه مسیر خط, the main editable line-path layer) **+ `Substat`** (ایستگاه انتقال و فوق توزیع). | Refines D38 to concrete entities; seeded disabled via `VersioningSeeds`. |
| D48 | 2026-08-14 | Versioning permission values moved to **400–403** — 300/301 silently collided with `LayerSettingsView`/`LayerSettingsEdit` (their Display Orders are 7/8, so Order is no guide to free values; C# permits duplicate enum values without warning). | Caught by the corrective migration seeding only 2 of 4 admin grants. Standing rules added the same day: **always ask Hossein before running EF migrations; never hand-edit the model snapshot.** |
| D49 | 2026-08-14 | `DecisionAction` gains **GroupProposals = 6** so manual grouping (D16) lands in the append-only decision log like every other reviewer action. | Additive to doc 03 §4's enum list; no schema change (tinyint column). |

---

## 3. Glossary

| Term | Meaning |
|---|---|
| **Feature** | One spatial record: geometry + attribute set, living in one live table. In Saba: a row of a `FeatureBaseEntity` descendant. |
| **Live state** | The current committed row in the real feature table — what every viewer sees on the map. |
| **Pending change** | One proposed create / update / delete of one feature, not yet applied to live. Carries the full proposed state, its author, and the live version it was based on. |
| **Version session** | A named batch of pending changes by one editor, with timestamp and optional comment. Unit of *submission*, not necessarily of *review* (→ Q6). |
| **Competition** | The set of pending changes targeting the same feature (or the same assignment). Resolved by selecting exactly one winner and rejecting the rest. |
| **Promotion** | Moving a pending change forward one pipeline stage (e.g., selected by reviewer → awaiting approval). |
| **Commit** | Applying an approved change to the live table. The only write path to live data under this system. |
| **Rejection** | A recorded decision against a pending change, with a mandatory reason; the change is archived, never physically deleted. |

---

## 4. What we need — requirements

Marked **[A]** = agreed (follows from D1–D4 or the prompt), **[P]** = proposed by
Claude, needs Hossein's confirmation.

### FR-1 Pending changes at feature granularity
1. **[A]** An edit produces a pending change against one feature; live state is untouched until commit.
2. **[A]** Change types: create, update (geometry and/or attributes), delete.
3. **[P]** A pending change stores the **full proposed state** (geometry + all attributes), not a delta. Deltas are computed for display only. (Robust against base drift; trivially applies on commit; costs storage — see S2.)
4. **[A]** Each pending change records the live version it was based on (base `RowVersion`), so staleness is detectable.

### FR-2 Version sessions
1. **[A]** A session groups pending changes by **one** editor (multi-editor sessions rejected — see R2/Q19) with created/submitted timestamps and an optional comment.
2. **[A]** Sessions support bulk content (hundreds of features).
3. **[A]** Lifecycle at minimum: draft (editable, private) → submitted (visible to reviewers) → fully resolved (every contained change accepted/rejected). Exact states in Stage 2.
4. **[A]** (D9) Sessions can be withdrawn by their editor while no contained change has been decided; multiple draft sessions per editor are allowed; no auto-expiry.

### FR-3 Competition & review
1. **[A]** Multiple pending changes may target the same feature simultaneously; the system never blocks this with locks.
2. **[A]** Reviewers can discover, per feature, all competing pending changes.
3. **[A]** Side-by-side comparison: geometry diff (visual, on map) + attribute diff (field-level table).
4. **[A]** Reviewer accepts exactly **one** competitor; all others in that competition are rejected. N-way, not just binary.
5. **[A]** Every rejection carries a mandatory reason; the author is notified.
6. **[P]** Non-competing changes (single proposal for a feature — expected to be the majority) go through a fast path: bulk review/accept at session level, not feature-by-feature. Without this, reviewers drown (see R5).
7. **[P]** Merging non-overlapping competitors (one edits geometry, other edits attributes) is **deferred** — not in stage 1 (see Q18).

### FR-4 Fixed promotion pipeline & roles
1. **[A]** (D5) Fixed 3-stage pipeline: Editor → Senior Reviewer → Approver → live.
2. **[A]** Roles: Editor, Senior Reviewer, Approver, Viewer.
3. **[A]** Roles map onto Saba's existing `Role` / `Permission`-enum authorization model — no parallel permission system.
4. **[A]** Detailed action/visibility matrix is a Stage 2 deliverable.

### FR-5 History & auditability
1. **[A]** Every decision (accept / reject / approve / commit) is recorded: who, when, why.
2. **[A]** Rejected changes are archived, never deleted.
3. **[A]** Committed history allows answering "what did this feature look like before commit X, and who changed it".
4. **[A]** (D13) Retention is indefinite; revisit only if volume forces it.

### FR-6 Notifications
1. **[A]** Editors are notified when their change is rejected (with reason) or committed.
2. **[A]** (D15) Stage 1 delivery = in-app inbox (polling). No push infrastructure exists in the WPF client today; building one is out of scope (see §6).

### NFR (non-functional)
1. **[A]** No layer-level locking, ever. Concurrency is optimistic; divergence is resolved by review, not prevented.
2. **[A]** Bulk sessions of ~500 features must be practical to submit and to review (drives FR-3.6).
3. **[P]** Commit must be transactional per accepted group: an approved batch either fully applies or not at all.
4. **[P]** The versioning core must not reference Saba assemblies (D3); it operates on a generic feature representation (table identifier + serialized geometry/attributes).

---

## 5. Boundaries

### In scope (this feature, first delivery)
- Saba (MakanNegar Saba) as first consumer; central SQL Server behind the existing API (D2, D3).
- Feature-level pending changes, sessions, competitive review, fixed pipeline, decision history, in-app notifications.
- WPF UI in the existing client stack (which library hosts it → Stage 4/5).

### Out of scope (explicitly rejected)
- ESRI-style layer versioning / long-lived layer branches.
- Offline editing, check-out/check-in, replica sync (D2).
- Configurable workflow engine (D4).
- Automatic merge of competing changes (deferred, Q18).
- Multi-step persisted undo/redo for editors (in-session undo stays the existing in-memory mechanism; D14).
- Real-time push notifications (SignalR/gRPC streaming) — stage 1 is polling (D15).
- Assignment/task management (D16) — competitions form by feature-id collision only; work orchestration stays outside the system.

### Deferred (worth doing later, not now)
- Topology-aware validation of accepted sets (shared edges, connectivity) — stage 1 gives advisory warnings only (R4).
- Attribute/geometry merge of non-overlapping competitors (Q18).
- Extending versioning to other apps (AlborzNegar, NiocExp, …) — design for it (D3), don't build it.

---

## 6. Current state of the codebase (constraints we must design around)

Found 2026-08-13 by code exploration; verify before relying on details.

- **Client edit flow today:** `MapViewer` / `MapViewModelBase.EditAsync` → `SelectedLayer` commands → `EditableFeatureLayer` (geometry) / `FeatureTable` (attributes) → `BaseLayer.SaveChangesAsync()` → `IEditableVectorDataSource.SaveChangesAsync()`.
- **Feature model:** `Feature<T>` (`IRI.Maptor.Sta.Spatial/Primitives/FeatureSets/FeatureOfT.cs`) already has `Status` (`FeatureStatus`), a single in-memory `OldVersion` snapshot, `Guid Key`, and `AreTheSame` comparison. All change tracking is in-memory and discarded on save — nothing is persisted.
- **Wire unit of work:** `FeatureSetChangesDto` (Added/Updated/Deleted) — conceptually very close to a "version session"; the natural seam for capturing sessions.
- **Saba write path:** `WebApiDataSource.SaveChangesAsync` → `PUT /<Domain>/Sync<Entity>` → MediatR → `UnitOfWork.SyncFeatureEntitiesAsync` → `BargContext.SaveChangesAsync`, with optimistic concurrency via `RowVersion` round-tripped inside the attribute dictionary. **This direct-to-live path must be closed (or gated) for versioned layers** — otherwise versioning is advisory only (→ Q17).
- **Saba entity model:** ~100 concrete feature tables inherit `FeatureBaseEntity` (`Id`, `SHAPE`, `ObjectId`, `gis_id`, audit stamps, `RowVersion`). A pending-change store must reference features **polymorphically** (table/entity identifier + id) — one FK per table is impossible (R1).
- **Spatial stack:** custom Maptor EF Core provider (no NetTopologySuite), geometry as SQL Server native binary/WKB. Any history/diff storage must use the same representation.
- **No temporal tables** anywhere today; the existing hash-chained `AuditLog` covers *security* events only, not feature edits.
- **Diff UI seed:** `FeatureChangesViewModel` + `FeatureChangesView` already do attribute diff and geometry compare for unsaved edits — a starting point for the competition compare view.
- **Roles:** `User`/`Role`/`UserRole`/`RolePermission` with a flat `Permission` enum, one API policy per value. New versioning permissions = new enum entries in a new group.

---

## 7. Critique of the proposed model

### Strengths
- Feature-level granularity fits reality: edits are sparse; ESRI layer versioning would version mostly-unchanged data and impose heavyweight reconcile/post semantics.
- Sessions match both the editor's mental model ("today's work package") and the existing `FeatureSetChangesDto` seam — low-friction capture.
- Deliberate competition is an unusual but coherent QA mechanism, and it subsumes accidental conflicts: a collision is just an unplanned competition.
- Storing pending changes server-side (D2) avoids the entire sync/replica problem space.

### Weaknesses & risks (numbered; referenced elsewhere)

- **R1 — Polymorphic target + serialized state.** With ~100 feature tables, pending changes must store proposed state generically (geometry blob + attribute JSON + table identifier). This is unavoidable, but it creates a **schema-drift hazard**: an EF migration adds/renames a column while pending changes exist → their serialized attributes no longer match the schema. Mitigation: version-tolerant apply + validation gate at accept/commit time; a migration checklist item. This is the single biggest hidden cost of the design.
- **R2 — Session is the wrong unit of review.** THE central tension. If review decisions are per-feature (they must be, for competition), a 500-feature session will be *partially* accepted — some features win, some lose, some still pending. So a session cannot be atomic, and "session state" is derived, not authoritative. Recommendation: session = unit of **submission and provenance** only; competition = unit of **decision**; approved batch = unit of **commit**. If Hossein wants all-or-nothing sessions instead, the competition mechanic breaks — the two are mutually exclusive (→ Q6). *(Accepted 2026-08-13 → D6.)*
- **R3 — Base drift / lost updates via review.** A proposal is made against live version X. Before it's reviewed, a competing change gets committed → live is now X+1. Accepting the old proposal silently overwrites X+1's content. Since full proposed state is stored (FR-1.3), the overwrite is total, not partial. Mitigation: every pending change carries its base `RowVersion`; stale changes are flagged at review, and accepting a stale change requires an explicit override (→ Q14). *(Accepted 2026-08-13 → D8.)*
- **R4 — Topology blindness.** Accepting competitor A's version of a substation while rejecting the same session's connected line edits can break shared geometry/connectivity. Full topological validation at decision time is a project in itself. Stage-1 position: advisory warnings ("this feature's session contains N related undecided changes"), no hard constraints. Must be stated honestly as a known limitation.
- **R5 — Reviewer throughput.** With deliberate competition, most features will still have exactly one proposal. If the UI forces per-feature ceremony on those, a 500-feature session means 500 clicks and the system dies of friction. The non-competing fast path (FR-3.6) is a hard requirement, not a nicety.
- **R6 — New features can't compete by identity.** Two editors independently *creating* "the same" real-world object produce two creates with no shared feature id — competition detection by id fails. Options: spatial proximity heuristics, or (better, fits D1) an explicit **assignment/task** that both creates were made under (→ Q8, Q10). *(Decided 2026-08-13 → D16: no assignments; reviewer manual grouping + spatial-overlap suggestions; residual duplicate-create risk accepted — see doc 02 §1.)*
- **R7 — Delete vs. edit is a normal competition.** A delete proposal and an update proposal for the same feature are just two competitors; accepting delete rejects the update. No special case needed — but the compare UI must render "deletion" as a first-class side.
- **R8 — Approver value is unproven.** If the Approver never overturns reviewers, the third stage is latency, not safety. Counter-argument: a distinct commit stage gives a natural place for batch-consistent commits (R4 mitigation) and organizational sign-off. Decision needed (Q5) — see S1 for the recommendation. *(Resolved 2026-08-13 → D5: 3 stages.)*
- **R9 — Long-lived pending changes rot.** Proposals nobody reviews accumulate, drift ever further from live, and clog the queue. Need at least: an age/staleness indicator in the review queue; possibly an expiry policy (→ Q13).
- **R10 — Editor identity of truth.** Denormalized `CreatedBy/LastUpdatedBy` stamps on live rows will now reflect the *committer* pipeline, not the hand that drew the geometry, unless commit explicitly writes the original author. Small, but wrong-by-default if forgotten.

---

## 8. Suggestions (preliminary — proposals, not decisions)

- **S1 — Pipeline: 3 fixed stages, overlapping roles allowed.** `Draft/Edit → Review (resolve competition, technical QC) → Approve & Commit (authority sign-off, batch-consistent commit)`. Rationale: the approve stage is where R3 staleness and R4 consistency get a final gate, and where commits are batched transactionally; in small teams the same person holds both roles, so the cost is one extra click, not one extra person. If in practice approval becomes a rubber stamp, collapsing to 2 stages later is easy; adding a stage later is hard. *(Accepted 2026-08-13 → D5.)*
- **S2 — Full-state pending changes, diff-on-display.** Store the complete proposed feature (geometry + attributes + base RowVersion); compute diffs only in the UI. Simpler apply, robust against base drift, at the cost of storage — acceptable given text attributes and 2D geometries.
- **S3 — Competition as an entity, not a query.** Because competition is deliberate (D1), model it explicitly (a competition/assignment record that proposals attach to) rather than deriving it as "all pending changes with the same target id". Accidental collisions auto-create a competition on second submission. This also solves R6 for creates. *(2026-08-13, D16: the assignment half is dropped — the competition entity remains, created on id-collision or by reviewer manual grouping.)*
- **S4 — Custom history tables, not SQL Server temporal tables.** Temporal tables capture *what/when* but not *why/who-decided* (decision, reason, session, competition linkage), and they'd version live tables only — pending states never touch live tables. The decision/history model must be custom anyway; temporal tables would add a second, redundant history mechanism. Final call in Stage 3.
- **S5 — Reuse the session seam.** Client-side, a "version session" is essentially today's in-memory edit batch (`FeatureSetChangesDto`) made persistent and named. Capture at that seam; don't invent a parallel edit pipeline in the client.
- **S6 — Per-layer opt-in.** Versioning is enabled per feature table; non-versioned tables keep the current direct Sync path. Gives a pilot path (Q15) and avoids a big-bang cutover (Q17). *(Accepted 2026-08-13 → D26.)*

---

## 9. Open questions

Statuses: **Open** (blocks a stage), **Answered** (see §2), **Deferred**.

| # | Question | Status | Notes / recommendation |
|---|---|---|---|
| Q1 | Competition deliberate or accidental? | **Answered** → D1 | Deliberate; accidental collisions funnel into same mechanism. |
| Q2 | Connectivity model? | **Answered** → D2 | Central DB, always online. |
| Q3 | First target / reuse? | **Answered** → D3 | Saba first, shared core. |
| Q4 | Configurable workflow? | **Answered** → D4 | Fixed. Stage count open → Q5. |
| Q5 | How many pipeline stages, and which? | **Answered** → D5 | 3 stages: Edit → Review → Approve & Commit. |
| Q6 | Session = unit of submission only; per-feature decisions? | **Answered** → D6 | Yes; sessions may end partially accepted. |
| Q7 | Can editors see each other's pending proposals? | **Answered** → D7, D18 | Blind competition; own-competition status + count only. |
| Q8 | Explicit assignment/task step creating competitions? | **Answered** → D16 | **No assignment mechanics.** Collision-by-id + advisory overlap warnings. Consequences: doc 02 §1. |
| Q9 | Delete-vs-edit competition treatment? | **Answered** → D12 | Delete is an ordinary competitor. |
| Q10 | Competing **creates** (no shared id) detection? | **Answered** → D16 | No auto-competition; reviewer manual grouping, aided by spatial-overlap suggestions. Residual duplicate risk accepted (doc 02 §1). |
| Q11 | Retention: keep history forever? | **Answered** → D13 | Forever; no purge in v1. |
| Q12 | Undo/redo inside a draft session? | **Answered** → D14 | Existing in-memory mechanism only. |
| Q13 | Session/proposal lifetime rules? | **Answered** → D9 | Flexible: multiple drafts, withdraw until first decision, no auto-expiry, age shown. |
| Q14 | Stale proposal handling at acceptance? | **Answered** → D8 | Warn + recorded override; re-checked at approval. |
| Q15 | Pilot scope: which Saba layers/tables first? | **Answered** → D38 | Transmission lines + substations. |
| Q16 | In-app inbox (polling) acceptable for v1? | **Answered** → D15 | Yes. |
| Q17 | For versioned layers, is the current direct `Sync` write path **closed**? | **Answered** → D26 | Per-layer gate; versioned layers reject direct sync. |
| Q18 | Merge of non-overlapping competitors (geometry-only + attributes-only)? | **Deferred** | Out of stage 1 (FR-3.7); rejected editor can resubmit on top of the winner. |
| Q19 | Multi-editor sessions? | **Answered** → D11 | No — single editor per session. |
| Q20 | Same person reviews and approves the same competition? | **Answered** → D10 | Allowed; both decisions identity-stamped. |
| Q21 | Approver-return semantics? | **Answered** → D17 | Return reopens the competition; losers stay provisional until commit. |
| Q22 | Blind-competition visibility detail? | **Answered** → D18 | Status + competitor count; never rival content/authors. |
| Q23–Q29 | Stage-2 detail questions (late arrivals during approval, no-winner closure, post-closure visibility, self-supersede, orphaned proposals, digest notifications, per-proposal withdraw) | **Answered** → D19–D25 | All confirmed 2026-08-13 with the recommended defaults; see `02-workflow-and-state-machine.md` §12. |
| Q30–Q36 | Stage-3 detail questions (draft storage, history strategy, project packaging, overlap-suggestion persistence, user references, geometry column typing, schema-signature computation) | **Answered** → D27–D33 | Confirmed 2026-08-13; **Q34 modified** — display names stored at write time (D31). See `03-data-model.md` §8. |
| Q37–Q40 | Stage-4 detail questions (post-submit client display, endpoint style, client-gateway placement, refresh model) | **Answered** → D34–D37 | **Q37 and Q40 modified by Hossein** (strict query separation + on-demand pending queries with authors; no polling, manual refresh only). See `04-architecture-and-integration.md` §9. |
| Q41–Q44 | Stage-5 detail questions (N-way attribute grid, bulk-accept confirmation, history rendering, view hosting) | **Answered** → D39–D42 | All confirmed 2026-08-13 with the recommended defaults. |
| Q45–Q46 | Stage-6 operational questions (layer-disable policy; staging environment) | **Answered** → D43–D44 | Disable blocked while proposals open; current working system = staging. |

---

## 10. Next steps

**Planning is complete** (2026-08-13): docs 01–06 baselined, D1–D44 decided, no open
questions. Implementation began the same day at milestone **M1** (doc 06 §2). Any
deviation discovered while building gets a new D-row here before the code merges.
