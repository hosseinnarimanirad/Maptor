# Spatial Versioning — Stage 4: Architecture & Integration

**Status:** BASELINED 2026-08-13 — Q37–Q40 answered (Q37 and Q40 in modified form) → D34–D37; pilot layers → D38
**Last updated:** 2026-08-13
**Prerequisites:** docs 01–03 (decisions D1–D33)

This document defines *where things live and how they talk*: project layout, the Saba API
surface, the Sync-path gate, client integration seams, authorization mapping, and
cross-cutting concerns. Screen design is Stage 5; milestones are Stage 6.

Markers: **[A]** = follows from a D-decision. Former [P] proposals were answered
2026-08-13 → **[A→D34]**…**[A→D37]**; Q37 and Q40 were accepted in **modified** form
(D34: strict query separation + on-demand pending queries revealing count **and authors**;
D37: no polling — manual refresh only).

---

## 1. Component overview

```
┌───────────────────────────── WPF client ─────────────────────────────┐
│ Saba app (MakanNegarSaba): navigation, role-gated menus, wiring      │
│ Jab.Wpf: MapViewer, EditableFeatureLayer, versioning views (Stage 5) │
│ Sta.Versioning: DTOs, enums, client-side guards (shared with server) │
│ Versioning gateway (typed HTTP client) [A→D36]                       │
└──────────────────────────────┬───────────────────────────────────────┘
                               │ HTTPS (existing Saba API auth)
┌──────────────────────────────▼───────────────────────────────────────┐
│ Saba API host                                                        │
│ Presentation: /versioning/* controllers [A→D35] (auth policies §7)   │
│ Application: MediatR handlers (thin — validate, call services, map)  │
│ Ket.VersioningPersistence: Submission / Review / Commit / Inbox /    │
│   History services + EF configurations (AddMaptorVersioning)         │
│ BargContext: versioning schema + live feature tables, one DbContext, │
│   one transaction at commit [A: doc 03 §5.2]                         │
└──────────────────────────────┬───────────────────────────────────────┘
                               │
                    SQL Server: schema `versioning` (9 tables)
                               + ~100 live feature tables (dbo)
```

The versioning core never references Saba types [A: D3]; Saba references the core.

---

## 2. Project layout and boundaries [A: D29]

| Project | Contains | Must NOT contain |
|---|---|---|
| `IRI.Maptor.Sta.Versioning` (new, netstandard2.1) | Entities, enums, state-machine guard functions (pure: `CanWithdraw(proposal)`, `CanResolve(competition)` …), serialization contract + schema-signature computation, **DTOs** (submission, queue rows, compare payload, decision commands, inbox items) | EF, HTTP, UI, Saba types |
| `IRI.Maptor.Ket.VersioningPersistence` (new) | `IEntityTypeConfiguration`s, `modelBuilder.AddMaptorVersioning(schema)`, services: SubmissionService (validation, collision → competition, supersede D22, overlap scan D30/D32), ReviewService (queues, select/reject/close, manual grouping), CommitService (doc 03 §5.2), InboxService (digests D24), HistoryService (timeline/as-of) | Controllers, Saba types, UI |
| Versioning gateway [A→D36] | Typed HTTP client wrapping the /versioning API; `VersioningWebDataSource : IEditableVectorDataSource` for the save-seam (§4) | UI |
| `IRI.Maptor.Jab.Wpf` | Versioning view models + views (Stage 5), reusing `FeatureChangesViewModel` diff mechanics | Direct DB access |
| Saba API (Presentation/Application) | Thin controllers + MediatR handlers; user-context injection (id + display name, D31); layer-registry admin | Business rules (those live in services/guards) |
| Saba WPF app | Navigation, role-gated menus, gateway registration | Versioning logic |

Guard functions live in `Sta.Versioning` so client UI and server services enforce the
*same* transition rules — the server remains the authority; the client uses them only to
enable/disable actions.

---

## 3. API surface (v1 sketch)

Dedicated `/versioning/*` controllers [A→D35], MediatR behind each. Exact DTO shapes are
implementation detail; semantic content is fixed by docs 02–03.

| Endpoint | Auth (§7) | Purpose |
|---|---|---|
| `GET  /versioning/layers` | any versioning role | Registry: which layers versioned (client routes saves, §4) |
| `POST /versioning/sessions` | VersionEdit | Submit a session (proposals batch). Returns per-proposal: state, competition status + count (D18), editor-facing live-overlap advisories (kind 2, D30), supersede notices (D22) |
| `DELETE /versioning/sessions/{id}` | VersionEdit (owner) | Withdraw session (guard D9) |
| `DELETE /versioning/proposals/{id}` | VersionEdit (owner) | Withdraw one proposal (guard D25) |
| `GET  /versioning/my/sessions`, `GET /versioning/my/proposals` | VersionEdit (owner) | Own submissions: collapsed status + competitor count (+ authors on expand, D34); provisional states render as "under review" (D17) |
| `GET  /versioning/my/layer-pending?layerId=…` | VersionEdit (owner) | Own pending proposals as renderable features — feeds the on-demand overlay [A: D34] |
| `GET  /versioning/features/{layerId}/{featureId}/pending-status` | VersionEdit | On-demand check for one feature: pending-proposal **count + authors** (own and others') [A: D34, amends D18] |
| `GET  /versioning/review/queue?layerId=…` | VersionReview | Queue rows: age, stale/orphan badges, competition sizes, undismissed overlap suggestions |
| `GET  /versioning/review/competitions/{id}` | VersionReview | Compare payload: all proposals (content + author names, D31) + current live state. **Diffs are computed client-side** (reuse `FeatureChangesViewModel` mechanics); server ships raw states |
| `POST /versioning/review/competitions/{id}/select` | VersionReview | Body: winnerProposalId, per-loser reasons, staleOverride flag (D8) |
| `POST /versioning/review/competitions/{id}/close-no-winner` | VersionReview | Reasons per proposal (D20) |
| `POST /versioning/review/group` | VersionReview | Manual grouping: proposalIds[] → one competition (D16) |
| `POST /versioning/review/suggestions/{id}/dismiss` | VersionReview | D30 |
| `GET  /versioning/approval/queue` | VersionApprove | Resolved competitions; fresh stale re-check flags |
| `POST /versioning/approval/commit` | VersionApprove | Body: competitionIds[], staleOverrides. One CommitBatch, all-or-nothing (E9) |
| `POST /versioning/approval/competitions/{id}/return` | VersionApprove | Reason (D17) |
| `GET  /versioning/inbox?unreadOnly=`, `POST /versioning/inbox/{id}/read` | any versioning role | Digest notifications (D15/D24) |
| `GET  /versioning/history/{layerId}/{featureId}` (+ `/as-of?at=`) | VersionHistoryRead | Timeline / point-in-time (doc 03 §5.4), D21 visibility applied |
| `GET  /versioning/competitions/{id}/record` | participant or VersionReview+ | Post-closure full record (D21) |

---

## 4. Gating the direct Sync path [A: D26]

**Server (the authority):** one choke point — `UnitOfWork.SyncFeatureEntitiesAsync`
already funnels every domain Sync endpoint; it consults the cached `VersionedLayer`
registry and rejects writes to enabled layers with error code `VersionedLayerWriteRejected`
(§8). No per-controller changes; ~100 endpoints gated at once.

**Client (the convenience):** at startup/layer-load the client reads
`GET /versioning/layers`. For a versioned layer, `SelectedLayer`'s save command routes to
a `VersioningWebDataSource` (implements `IEditableVectorDataSource`; its
`SaveChangesAsync` builds a session-submission from the in-memory batch instead of a
`FeatureSetChangesDto` sync) — the S5 seam reuse: editors keep the exact same edit
tools and save button. Non-versioned layers keep `WebApiDataSource` untouched.

A stale client that somehow syncs directly gets the server rejection with a message
telling it to submit via versioning — defense in depth, not UX.

---

## 5. Client state after submission [A→D34, Q37 modified by Hossein]

**The map always renders live truth** — the last committed state — and versioning data is
**strictly separated** from normal feature queries: layer listing, feature fetch, and
rendering pipelines are untouched by versioning. Exactly three on-demand entry points
exist on the map side:

1. **Own-pending overlay:** `GET /versioning/my/layer-pending` returns the editor's own
   pending proposals as separate features, rendered in a distinct pending style alongside
   live truth (never merged into the layer). Blind rules intact — others' content is never
   returned.
2. **Per-feature pending-status check:** an explicit action on a selected/edited feature
   (`GET /versioning/features/…/pending-status`) returning the **count + authors** of
   pending proposals, including other editors' (D34, amending D18's author-hiding). No
   automatic call on edit-start — on-demand only.
3. **History** (context menu, §3 history endpoints).

New edits always start from live state; the submission response still carries
self-supersede notices (D22) and live-overlap advisories (D30).

Consequence to accept: after submitting, an editor's change "disappears" from the main map
into the My-Pending panel / on-demand overlay — Stage 5 must make this transition obvious
(post-submit summary dialog + a hint to the overlay toggle).

---

## 6. Sequence flows (component level; semantics in doc 02)

**Submit:** EditableFeatureLayer/FeatureTable edits (in-memory, D27) → save command →
VersioningWebDataSource → `POST /versioning/sessions` → SubmissionService: validate,
per-proposal collision lookup → join/create competition, self-supersede, overlap scan
(raw SQL, D32) → persist → response (statuses, counts, advisories) → result dialog; the
map keeps showing live truth, and the My-Pending panel reflects the new proposals on its
next open/refresh [A→D34, D37].

**Review:** queue GET → reviewer opens competition → compare payload (raw states) →
client-side diffs (geometry on map, attributes in table) → select/close POST →
ReviewService guard-checks + RowVersion (E4) → decision records, state flips, N4 if
no-winner.

**Commit:** approval queue GET (fresh stale flags) → commit POST → CommitService runs doc
03 §5.2 inside one BargContext transaction → response lists per-competition outcome; on
any gate failure nothing committed (E9) → inbox digests written (N2/N3).

---

## 7. Authorization mapping [A: FR-4.3, doc 02 §6]

Four new values in Saba's flat `Permission` enum, new **"Versioning"** display group (next
free Order block per the enum's conventions): `VersionEdit`, `VersionReview`,
`VersionApprove`, `VersionHistoryRead`. The API host's existing pattern (one policy per
enum value) picks them up automatically; controllers take `[Authorize(Policy = …)]` as in
§3. Roles compose them freely (D5/D10); ownership checks (own session/proposal) are
service-level, not policy-level. The user context handed to services carries **id +
display name** so every write stamps both [A: D31].

---

## 8. Cross-cutting

- **Error model:** ProblemDetails with stable codes the client maps to Persian messages:
  `VersionedLayerWriteRejected`, `StaleBase`, `SchemaMismatch`, `CompetitionAlreadyResolved`,
  `CompetitionUnderApproval` (D19 queue), `WithdrawNotAllowed`, `DuplicateActiveProposal`
  (D22 index race), `NotCompetitionParticipant` (D21).
- **Registry caching:** `VersionedLayer` cached in API memory (invalidate on admin
  change) and client session (refresh on login/layer-load).
- **Refresh model [A→D37, Q40 modified]:** **no polling at all in v1** — inbox, own
  statuses, and queues load on open and via explicit Refresh buttons only. A timer or push
  channel can be added later without any API change.
- **Localization:** all user-facing strings via the existing Jab.Core resx conventions
  (Persian primary).
- **Startup:** API recomputes schema signatures [A: D33] and logs any layer whose
  signature changed (pending proposals there get flagged in queues immediately).
- **Audit separation:** the existing hash-chained security `AuditLog` is untouched;
  versioning's audit is `DecisionRecord` (append-only, D13). Role/permission changes for
  versioning flow through the existing user-management audit as today.
- **No push:** nothing in v1 depends on server-initiated messages [A: D15]; if SignalR/gRPC
  streaming arrives later, it augments the manual-refresh model only.

---

## 9. Stage-4 detail questions (Q37–Q40) — all ANSWERED 2026-08-13

| # | Question | Answer |
|---|---|---|
| Q37 | Post-submit client display model | **Modified by Hossein → D34**: live truth always; versioning strictly separated from normal queries; on-demand own-pending overlay + per-feature pending-status check revealing **count + authors** (amends D18). Follow-ups confirmed: on-demand invocation only; overlay = own proposals as separate features (not a merged preview). |
| Q38 | Dedicated `/versioning/*` controllers? | Yes → D35. |
| Q39 | Gateway in `Ket.WebApiPersistence`? | Yes → D36. |
| Q40 | Refresh cadence | **Modified by Hossein → D37**: no polling at all in v1; manual refresh only; may improve later. |

---

## 10. Handoff to Stage 5 (UI/UX)

Views to specify: session submit flow (+ post-submit summary, D34 transition), "My pending
proposals" panel + on-demand overlay toggle + per-feature pending-status check (D34),
review queue (badges: age, stale, orphan, size, suggestions), competition compare view
(map diff + attribute diff + delete-as-a-side, D12; reuse `FeatureChangesViewModel`/
`FeatureChangesView` mechanics), manual grouping interaction, approval queue + batch
commit + return dialog, inbox (manual refresh, D37), history timeline. Editor-facing
status labels must collapse provisional states (doc 02 §7 amendment). Pilot layers are
decided (D38: transmission lines + substations).
