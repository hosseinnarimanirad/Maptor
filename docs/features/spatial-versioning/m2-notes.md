# M2 working notes — submission + Sync gate + API + gateway

**Last updated:** 2026-08-14 — **M2 COMPLETE, acceptance 14/14 PASS** (see the closing
section). Milestone M2 (doc 06 §2). The Sync-gate half shipped with M1.

## ✅ M2 CLOSED 2026-08-14 — acceptance passed end-to-end

Run with Hossein's staging test account against the API booted from source (localhost:5140),
driving the REAL client code path (`VersioningWebDataSource` over HTTP):

- Login → `GET /Versioning/Layers` (TrLineSeg enabled, TableName `Tr_Line_Seg`) →
  `LoadAsync` (11 live features) → create + attribute-update in memory →
  `SaveChangesAsync` → session persisted, both proposals pending singletons, **local
  edits reverted (D34)** and the updated feature's attribute back to its live value.
- Resubmission of the same target → `SupersededProposalId` correct through the API.
- `PendingStatus` (count 1, own, author name) and `MyProposals` correct over HTTP.
- **Direct sync rejected** by the D26 gate — server log shows the full
  `VersionedLayerWriteRejected` message; the client receives the distinct resource key
  `message_error_versionedLayerWriteRejected` from the new
  `VersionedLayerWriteRejectedException : DomainException` (the house error contract is
  exception-type subclassing + `MessageResourceKey`, as `ConcurrencyException` does —
  discovered during acceptance; the generic-message DomainExceptions in
  SubmissionService reach clients as the generic key until they get typed/keyed, an M5
  localization task alongside the client resx entry for the new key).
- Collision → N1 was verified at service level (12/12 harness); a true two-account API
  collision test awaits a second test user (M6 pilot walkthrough covers it).
- Test data fully deleted from staging afterward (0 sessions/proposals/competitions
  remain); **TrLineSeg stays versioning-enabled** — the pilot is on, direct sync for it
  stays closed. Identity gaps in the tables are from rolled-back/deleted test rows.

**Next: M3** — review services (queue, compare payload, select/reject/close, manual
grouping, bulk accept) per doc 06 §2.

## Done 2026-08-14 (uncommitted — Hossein reviews & commits)

### SubmissionService (`Ket.VersioningPersistence/Services/SubmissionService.cs`)

`SubmissionService.SubmitAsync(context, dto, editorId, editorName)` — static, one
transaction per submission (honors an ambient transaction so harnesses/tests can roll
back). Pipeline per doc 02 P1/P2 + C1/C2:

1. Layer resolution by LayerKey; rejects unknown layers and layers not under versioning
   (`UnknownVersionedLayer:` / `VersioningNotEnabled:`).
2. Validation: duplicate targets / client keys within a session; per-change-type rules
   (create = full state, no target/base; update = target + base + full state; delete =
   target + base, no state); WKB parse, WGS84 only (matches `FeatureDto` conventions).
3. **Self-supersede (D22):** the editor's older active proposal on the same target is
   withdrawn (`Superseded`) and the new one takes its place in the same competition;
   resubmitting while the own proposal sits in a Resolved competition is rejected
   (`ProposalUnderApproval:` — supersede would defeat E5's winner-withdrawal bar).
4. **Competition resolution:** join the Open competition on (layer, target) if present;
   else if a Resolved one is awaiting approval, open a **queued** successor
   (Predecessor set, D19); else open a singleton. Creates always open singletons (D16).
5. **Overlap scan (D30/D45):** two raw-SQL geography `STIntersects` probes per proposal —
   vs the live table (schema/table/pk from **EF metadata**, never hardcoded — live tables
   sit in e.g. `sub.*`), excluding the proposal's own target; and vs other editors'
   active proposals on the layer. Persisted as `OverlapSuggestion` rows (kind 2 also
   returned as the editor advisory).
6. **N1 digests (D24/D18):** one notification per affected recipient per submission,
   payload = competition/feature list + nothing else (no content, no author names).
7. Filtered unique indexes are the race backstop: unique violations surface as
   `ConcurrentSubmission:` (DomainException), nothing half-written.

### Verified against the STAGING database (rolled-back harness, 12/12 PASS)

Scenario: editor A submits create+update → both pending singletons; editor B submits a
crossing update on the same target → joins (count 2), exactly one N1 digest to A, none to
B; A resubmits → old proposal Withdrawn/Superseded, new one joins the same competition
(count still 2); single Open competition invariant holds; B's crossing geometry produces
the PendingVsPending suggestion against A's proposal (positive `STIntersects` hit).

## API surface — DONE (later 2026-08-14, uncommitted)

- **Wiring pattern** (matches the house conventions): `IVersioningGateway`
  (Application/Gateways) → `VersioningGateway` (Ef, injects BargContext +
  ICurrentUserService, registered in `ConfigureEfContext` beside IUnitOfWork) → thin
  MediatR features under `Application/Features/Versioning/` → `VersioningController`
  (`[Route("[controller]")]`, policies by Permission Display Name, e.g. "Versioning.Edit").
- **Endpoints live**: `GET /Versioning/Layers` (any authenticated — clients need it to
  route saves; returns TableName resolved from EF metadata, D46), `POST
  /Versioning/Sessions`, `GET /Versioning/MyProposals` (collapsed statuses + competitor
  counts, newest 500), `GET /Versioning/PendingStatus?layerKey=&featureId=` (count +
  authors, D34).
- **Two bugs found by runtime testing, fixed and verified:**
  1. `EnableRetryOnFailure` on the API's context rejects user-initiated transactions —
     SubmissionService now runs its transaction inside `CreateExecutionStrategy()` with a
     `ChangeTracker.Clear()` at delegate start (retries must not double the Adds). The
     ambient-transaction path (tests/harnesses) is unchanged.
  2. First-run registry seeding never stamped signatures: the stamping loop queried the
     database before the seeded rows were saved. Fixed (save after seeding); verified by
     deleting the seed rows and re-booting — one boot now seeds AND stamps, logging the
     changed-signature warning.
- **Runtime smoke test** (API booted from source, staging DB): controller discovered,
  `GET /Versioning/Layers` → 401 without a token, Swagger up. Kestrel note: appsettings'
  endpoint config overrides ASPNETCORE_URLS — the host always binds localhost:5140.
- **Staging state**: `versioning.VersionedLayer` now holds the two pilot rows
  (TrLineSeg, Substat), **disabled**, signatures stamped.
- Submission harness re-run after the refactor: **12/12 PASS** (now reusing the seeded
  TrLineSeg row, so the live-table overlap path runs against `tl`-schema data).

## Client gateway — DONE (2026-08-14, third slice, uncommitted)

- **`VersioningWebApi`** (Ket.WebApiPersistence): static client for the four endpoints,
  mirroring `WebApiInfrastructure` (shared authenticated HttpClient preferred — Saba's
  `MakanNegarSabaServices.SharedClient` carries the token).
- **`VersioningWebDataSource : WebApiDataSource`**: editing/loading inherited unchanged
  (the S5 seam); only `SaveChangesAsync` differs — builds a `SessionSubmitDto` from the
  in-memory batch (Create/Update/Delete from FeatureStatus; WGS84 WKB via
  `FeatureDto.Parse`; `RowVersion` attribute → `BaseRowVersion`, handling both byte[]
  and base64-string forms; RowVersion stripped from proposed attributes), POSTs to
  `/Versioning/Sessions`, and on success **undoes local edits** (D34 — the map returns to
  live truth) and raises `SessionSubmitted` with the result (the M5 dialog's hook).
  `NextSessionTitle`/`NextSessionComment` are set by the UI before saving. Id-only
  deletions (no RowVersion) are rejected client-side with a clear message.
- **Routing in `ApplicationPresenter`**: after login, `LoadApiLayers` fetches
  `GET /Versioning/Layers` once (fail-safe: unreachable versioning falls back to direct
  sync — the server gate still protects); `CreateLayerFromMetadata` derives the entity
  name from the `List{Entity}` ServiceUrl convention (`ListTrLineSeg` → `TrLineSeg` —
  same convention the Sync-URL derivation uses; note: matching is by **EntityName**, not
  TableName, since the client layer model has no TableName) and constructs
  `VersioningWebDataSource` for enabled versioned layers.
- Whole WPF client builds green (23 projects).

## Remaining in M2 — acceptance only

**Walkthrough B end-to-end** (doc 06 §2) through the real API (and ideally the client):
bulk session → singletons; direct sync rejected (`VersionedLayerWriteRejected`);
collision → N1. Needs: (a) versioning **enabled** on a pilot layer on staging — while
enabled, direct sync for that layer is rejected for every client, which is the intended
gate but affects anyone editing that layer; (b) an authenticated session (test
credentials, or Hossein drives the login/client). Unlike the harness, API-path
submissions persist (no rollback) — acceptable on staging per D44.

Untested surface the acceptance run covers: HTTP JSON round-trip of the DTOs
(byte[]/base64, attribute dictionaries arriving server-side as JsonElement — the
canonical serializer handles JsonElement — enum-as-string), JWT policy checks
("Versioning.Edit" needs a role carrying permission 400), and the client mapping.

Standing rules: ask Hossein before any EF migration; never hand-edit the model snapshot.
