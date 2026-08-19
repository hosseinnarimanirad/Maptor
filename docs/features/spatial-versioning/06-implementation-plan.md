# Spatial Versioning — Stage 6: Implementation Plan

**Status:** BASELINED 2026-08-13 — Q45–Q46 answered → D43–D44; **M1 started 2026-08-13**
**Last updated:** 2026-08-13
**Prerequisites:** docs 01–05, all baselined (decisions D1–D42)

This plan turns the design into six milestones over the pilot (D38: **transmission lines
+ substations**). Sizes are relative (S/M/L) — calendar estimates depend on availability
and are deliberately not faked here. Each milestone ends demoable; the doc 02 §9
walkthroughs (A–F) double as the acceptance-test scripts.

---

## 1. Ground rules

- Build strictly in milestone order — each depends on the previous.
- Nothing outside the pilot layers is enabled in production until M6 is evaluated.
- Every deviation from docs 01–05 discovered during implementation goes back into the
  decision log (doc 01 §2) before the code merges — the docs stay the truth.
- Deferred list (doc 01 §5) is off-limits: no merge features, no assignments, no push,
  no persisted undo, no topology enforcement.

---

## 2. Milestones

### M1 — Foundation (size M)
**Scope:** create `IRI.Maptor.Sta.Versioning` (entities, enums, guard functions, DTOs,
serialization contract + schema-signature computation) and
`IRI.Maptor.Ket.VersioningPersistence` (EF configurations, `AddMaptorVersioning()`);
EF migration creating the `versioning` schema — 9 tables incl. the D19/D22 filtered
unique indexes and the Competition↔Proposal circular FK (no cascade); `VersionedLayer`
registry + startup signature computation (D33); minimal registry admin endpoint.
**Spike (do first):** verify the custom provider's `geometry` pass-through mapping and a
spatial index on a `versioning`-schema table (D32) against a copy of the Saba DB.
**Accept:** migration applies cleanly to a Saba DB copy; signatures computed for the two
pilot layers; guard-function unit tests green.

### M2 — Submission + Sync gate (size L)
**Scope:** SubmissionService (validation, collision → join/create competition,
self-supersede D22, overlap scan D30/D32, N1 notifications); Sync gate at the
`UnitOfWork.SyncFeatureEntitiesAsync` choke point (D26); endpoints: sessions, my/*,
layers, pending-status, layer-pending (doc 04 §3); client: `VersioningWebClient` +
`VersioningWebDataSource` in Ket.WebApiPersistence (D36), save routing, Submit + Result
dialogs (doc 05 §2); the four `Version*` permissions + policies (doc 04 §7).
**Accept:** walkthrough **B** (bulk 500-feature session → singletons) passes end-to-end
on a pilot layer; direct Sync to a versioned layer is rejected with
`VersionedLayerWriteRejected`; a second editor's colliding submission joins the
competition and both owners get N1.

### M3 — Review (size L)
**Scope:** review-queue endpoint + view (badges, bulk accept D40, manual grouping D16,
suggestion dismiss); compare-payload endpoint; Compare view (N-way grid D39, map styles,
delete-as-a-side D12); select-winner / close-no-winner with reasons + stale override
(D8/D20); decision records; E4 concurrency handling.
**Accept:** walkthroughs **A** (competition), **E** (delete vs edit), **F** (manual
grouping of creates) pass; a 500-singleton queue is bulk-accepted in one confirmation;
two concurrent reviewers on one competition → second gets `CompetitionAlreadyResolved`.

### M4 — Commit + history (size L)
**Scope:** CommitService (doc 03 §5.2: stale + schema gates, copy-on-write, author id +
display-name stamping D31, all-or-nothing batch E9); approval queue + view; return flow
(D17 reopen); FeatureHistory; history timeline view + as-of (D41 overlay); post-closure
record view (D21).
**Accept:** walkthroughs **C** (stale override at review and again at approval) and
**D** (approver return → re-resolution) pass; timeline reconstructs a feature across
3 commits incl. a delete; a batch with one schema-blocked competition commits nothing.

### M5 — Editor experience (size M)
**Scope:** My Pending panel (status-collapse table, doc 05 §0 — verify provisional states
are indistinguishable); inbox with digests (D24, manual refresh D37); map entry points:
own-pending overlay, pending-status check (D34), history context-menu entry; N2–N6
notification writing; error-code → Persian message mapping.
**Accept:** an editor whose proposal is provisionally rejected sees only "under review"
until commit; pending-status check shows count + authors but never content; all doc 04 §8
error codes render localized messages.

### M6 — Pilot enablement (size S–M)
**Scope:** enable registry rows for transmission lines + substations; role setup for the
pilot cohort; operator walkthrough script (Persian); monitoring queries (queue depth,
oldest pending age, stale count); feedback capture sheet.
**Accept:** a real session by ≥2 editors + 1 reviewer/approver completes the full cycle
on staging (Q46), then on production pilot; evaluation review scheduled (~2–4 weeks of
pilot use) before widening layers.

---

## 3. Test strategy

- **Unit** (Sta.Versioning): guard functions, serialization canonical form, signature
  computation, tolerant-mapping rules (doc 03 §5.3 cases).
- **Integration** (SQL Server, DB copy): commit transaction (gates, copy-on-write,
  rollback), filtered-index race behavior (D19/D22), E4 optimistic concurrency, Sync
  gate, overlap scan correctness.
- **Acceptance:** walkthroughs A–F as scripted scenarios, distributed across M2–M4 as
  listed above.
- **UI:** manual test scripts per view; stub-data window previews where the existing
  render-harness approach applies (verify current state of that harness before relying
  on it).

---

## 4. Risk register (implementation-phase)

| Risk | Mitigation |
|---|---|
| Geometry pass-through or spatial index misbehaves on `versioning`-schema tables | M1 spike, before anything is built on it |
| Circular FK (Competition.Winner ↔ Proposal.Competition) trips EF migrations | Configure winner FK without cascade, add in a follow-up migration step if needed |
| R1 schema drift during the pilot | D33 startup detection + doc 03 §5.3 gate are in from M1; add a pending-proposal compatibility check to the deploy checklist |
| Reviewer throughput worse than assumed (R5) | M3 acceptance includes the 500-singleton bulk case with a stopwatch; revisit UI before M6 if painful |
| Duplicate creates slip past manual grouping (D16 residual risk) | Pilot feedback sheet asks reviewers explicitly; count grouped-vs-missed in monitoring |
| Pilot users bypass versioning out of habit | D26 server gate makes bypass impossible, not just discouraged; walkthrough script explains the new save behavior |

---

## 5. Rollout & rollback

Dev → **the current working system, which serves as staging** (D44 — data loss there is
acceptable) → pilot (2 layers, small cohort) → evaluation → widen layer-by-layer (S6
opt-in was built for exactly this). Rollback for a layer = flip `IsVersioningEnabled`
off, returning it to direct Sync; disabling is **blocked while open proposals exist**
(D43) — the admin resolves or withdraws them first, so history never has a hole.

---

## 6. Definition of done (planning → implementation)

Docs 01–06 baselined; D1–D42 decided; Q45–Q46 are the only open items and are
operational, not design. Implementation starts at M1 on Hossein's go — no code is written
as part of the planning effort.

---

## 7. Stage-6 operational questions (Q45–Q46) — ANSWERED 2026-08-13

| # | Question | Answer |
|---|---|---|
| Q45 | Disabling versioning with open proposals? | **Blocked** until resolved/withdrawn → D43. |
| Q46 | Test environment? | **The current working system is the staging** (data loss acceptable there) → D44. M1 migration test and M6 dry run happen on it directly. |
