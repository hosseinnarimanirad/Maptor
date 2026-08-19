# Spatial Versioning — Stage 2: Workflow & State Machine

**Status:** BASELINED 2026-08-13 — Q23–Q29 confirmed with recommended defaults → D19–D25 (doc 01 §2)
**Last updated:** 2026-08-13
**Prerequisite:** `01-requirements-and-boundaries.md` (decisions D1–D18; all references like D6, R1, S6 point there)

This document defines *behavior*: objects, their states, every legal transition, who may
trigger it, who sees what, and which notifications fire. It does **not** define storage
schema (Stage 3), API surface (Stage 4), or screen layouts (Stage 5).

Markers: **[A]** = follows directly from a D-decision. Former [P] proposals were all
confirmed 2026-08-13 and now read **[A→D19]**…**[A→D25]**.

---

## 1. Consequences of collision-only competition (D16) — the requested discussion

Hossein chose: no assignment mechanics; editors edit whatever they have access to; the
system detects competition by feature-id collision; spatial overlap produces advisory
warnings. Verdict after analysis: **this is logically sound and does not break the flow**,
with three consequences that must be stated honestly:

1. **"Deliberate" moves outside the system.** D1 said competition is a deliberate business
   process — under D16, the deliberateness is organizational (a manager verbally points two
   editors at the same work); the system only detects the resulting collision and cannot
   distinguish deliberate from accidental. That is acceptable: both were always going to
   funnel into the same resolution mechanism. The system's job is *detection + resolution*,
   not orchestration.
2. **Competing creates lose their systematic detector** (risk R6). Two editors digitizing
   the same new real-world object produce two creates with no shared id — no collision, no
   competition. Compensations, both decided in D16:
   - the reviewer can **manually group** proposals (v1 scope: create-proposals within the
     same layer) into one competition;
   - at submission the system runs a **spatial-overlap scan** and surfaces
     *suggestions* in the reviewer queue ("these 2 pending creates overlap — group them?").
   **Residual risk, accepted for v1:** if the reviewer ignores the suggestion, duplicate
   objects can both be committed. Revisit if it happens in practice.
3. **Overlap warnings must respect blindness (D7/D18).** Two distinct channels:
   - **Editor-facing** warnings compare the edited geometry against **live** features only
     ("your geometry overlaps live feature #123") — live is public, no leak.
   - **Pending-vs-pending** overlaps appear **only** in the reviewer/approver queue. An
     editor must never learn of a rival's pending work through an overlap warning; the only
     competition signal an editor gets is D18's status + count on their *own* proposal.

Implementation note for Stage 3/4: the custom Maptor EF provider has no LINQ spatial
predicates, so the overlap scan is raw SQL (`STIntersects` after a bounding-box
pre-filter), run once per proposal at submission time — bounded cost, no interactive load.

Nothing else in the model depended on assignments. S3's "competition as an entity"
survives: the entity is created lazily on second colliding submission or by manual
grouping (and conceptually every submitted proposal belongs to a competition — see §2).

---

## 2. Object model (conceptual)

| Object | One-line definition |
|---|---|
| **Proposal** | One proposed create/update/delete of one feature: full proposed state + base `RowVersion` + author + timestamps. (Doc 01 calls this a pending change.) |
| **Session** | Single-editor batch of proposals; unit of submission and provenance only (D6, D11). |
| **Competition** | The decision unit: the set of proposals contending for one target. **Every submitted proposal belongs to exactly one competition** — most are size 1 ("singletons"); the fast path (FR-3.6) is bulk-resolving singletons. |
| **Decision record** | Immutable record of a review/approval/return action: actor, timestamp, reason, stale-override flag. Kept forever (D13). |
| **Notification** | Inbox item (D15), aggregated per session where applicable (§8). |

Two computed flags on a proposal (never states): **Stale** — base `RowVersion` ≠ current
live `RowVersion`; **Orphaned** — target feature no longer exists in live.

---

## 3. Proposal state machine

States: `Draft`, `Submitted`, `SelectedForApproval`, `ProvisionallyRejected`,
`Committed` (terminal), `Rejected` (terminal), `Withdrawn` (terminal; carries a cause:
user / session-withdrawn / superseded).

| # | From | To | Trigger (actor) | Guard | Side effects |
|---|---|---|---|---|---|
| P1 | — | Draft | Editor adds a change to a draft session | Editor has edit access to the layer | none (private) |
| P2 | Draft | Submitted | Session submitted (Editor) | Validation passes | Joins/creates competition (C1/C2); N1 count notices; supersedes the editor's earlier pending proposal on the same target, if any (E1) **[A→D22]** |
| P3 | Submitted | SelectedForApproval | Reviewer selects it as winner | Its competition is Open; if Stale → recorded override (D8) | Competition → Resolved; all siblings → ProvisionallyRejected (P4) |
| P4 | Submitted | ProvisionallyRejected | Sibling selected as winner | — | Rejection reason recorded now; notification deferred to commit (D17) |
| P5 | Submitted | Rejected | Reviewer closes competition with **no winner** **[A→D20]** | Competition Open | Competition → ClosedNoWinner; N4 fires immediately (final) |
| P6 | SelectedForApproval | Committed | Approver approves (commit transaction) | Re-validation: schema still valid (R1 gate), staleness re-checked — if newly stale → approver override (D8) | Live write (original author stamped, R10); history + decision records; siblings → Rejected (P9); N2/N3 digests |
| P7 | SelectedForApproval | Submitted | Approver returns with reason (D17) | — | Competition → Open; siblings → Submitted (P10); N5 to reviewer |
| P8 | Submitted | Withdrawn | Editor withdraws proposal **[A→D25]** / withdraws session / is superseded (E1) | Its competition is still Open | Leaves competition; competition dissolves if now empty (C5) |
| P9 | ProvisionallyRejected | Rejected | Competition committed | — | N3 (reason recorded at P4) |
| P10 | ProvisionallyRejected | Submitted | Approver returns (D17) | — | back in play |

A Draft proposal that is simply deleted from its draft session ceases to exist — no state,
no record (D14: drafts are the editor's private workspace).

---

## 4. Competition state machine

States: `Open`, `Resolved`, `Committed` (terminal), `ClosedNoWinner` (terminal),
`Dissolved` (terminal — emptied by withdrawals, no decisions were made).

| # | From | To | Trigger | Guard | Notes |
|---|---|---|---|---|---|
| C1 | — | Open | First proposal submitted for a target with no open competition | — | Size 1 (singleton) |
| C2 | Open | Open | Colliding submission joins; or reviewer manually groups proposals/competitions (D16) | Still Open; manual grouping v1 = creates within same layer | N1 count notices to all owners |
| C3 | Open | Resolved | Reviewer selects winner | ≥1 proposal; stale override if needed (D8) | Drives P3/P4 |
| C4 | Open | ClosedNoWinner | Reviewer rejects **all** proposals | — | No live change ⇒ no approver gate **[A→D20]**; drives P5 |
| C5 | Open | Dissolved | Last proposal withdrawn | Size 0 | No decision records |
| C6 | Resolved | Committed | Approver approves | Commit transaction succeeds (E9) | Drives P6/P9 |
| C7 | Resolved | Open | Approver returns with reason | — | Drives P7/P10; return reason visible to reviewer |

**Late arrivals [A→D19]:** while a competition on target F is `Resolved` (sitting in the
approval queue), a new submission targeting F does **not** join or reopen it — it opens a
new, *queued* competition on F. Guard: a queued competition cannot be Resolved until its
predecessor reaches a terminal state. At most one Resolved + one Open competition per
target can exist. (Alternative — late arrival reopens the Resolved competition — was
rejected: it would make the approval queue unstable under editor activity.)

---

## 5. Session state machine

Stored states: `Draft` → `Submitted`; `Withdrawn` (terminal).
Everything else is **derived, read-only** (D6): *InReview* (any proposal undecided),
*PartiallyResolved* (mixed outcomes so far), *Resolved* (all proposals terminal).

Rules: editable only in Draft **[A]**; submit is one-way **[A]**; withdraw allowed while
no contained proposal has left `Submitted` (D9) and withdraws all its proposals (P8);
multiple concurrent drafts per editor (D9); exactly one editor per session (D11).

---

## 6. Roles and permitted actions

New permission-enum group (names indicative, final in Stage 3/4): `VersionEdit`,
`VersionReview`, `VersionApprove`, `VersionHistoryRead` — composed into Saba roles via the
existing `Role`/`RolePermission` model (FR-4.3). One person may hold several (D10).

| Action | Editor | Reviewer | Approver | Viewer |
|---|---|---|---|---|
| Create/edit/discard draft sessions (own; within layer access) | ✓ | — | — | — |
| Submit session / withdraw session (guards §5) | ✓ | — | — | — |
| Withdraw own pending proposal **[A→D25]** | ✓ | — | — | — |
| See own proposals' status + competitor count (D18) | ✓ | — | — | — |
| See review queue, all pending content + authors | — | ✓ | ✓ | — |
| Compare competitors side-by-side (geometry + attribute diff) | — | ✓ | ✓ | — |
| Select winner / reject-all (with stale override, D8) | — | ✓ | — | — |
| Manually group proposals into a competition (D16) | — | ✓ | — | — |
| Approve & commit (single or batch), with re-validation override | — | — | ✓ | — |
| Return a Resolved competition with reason (D17) | — | — | ✓ | — |
| Read live state + committed history | ✓ | ✓ | ✓ | ✓ |
| Receive inbox notifications | ✓ | ✓ | ✓ | — |

---

## 7. Visibility matrix (blind rules)

| Artifact | Owner-editor | Other editors | Reviewer | Approver | Viewer |
|---|---|---|---|---|---|
| Draft session content | ✓ | — | — | — | — |
| Own submitted proposal (content, status, competitor **count**) | ✓ | — | ✓ | ✓ | — |
| Rival pending proposals (content, authors) | — | — | ✓ | ✓ | — |
| Spatial-overlap warnings vs **live** features | ✓ (own edits) | — | ✓ | ✓ | — |
| Pending-vs-pending overlap suggestions | — | — | ✓ | ✓ | — |
| Live features | ✓ | ✓ | ✓ | ✓ | ✓ |
| Committed history (winning versions + commit decisions) | ✓ | ✓ | ✓ | ✓ | ✓ |
| Full record of a **closed** competition (incl. losing proposals, authors, reasons) | **[A→D21]** own outcome always; full record proposed for participants + reviewer/approver only | — **[A→D21]** | ✓ | ✓ | — **[A→D21]** |

Q25 default (confirmed → D21): blindness protects the *pending* phase; after closure, each
participant sees the full record of the competitions they took part in (including the
winner's content — that is now committed history anyway); non-participants see only
committed history. Widening to full transparency is a policy switch later, not a redesign.

**Amendment 2026-08-13 (D34):** rival *authors* are discoverable on demand via the
per-feature pending-status check (count + authors), even before the editor submits their
own proposal — the author cells marked "—" for editors above are superseded accordingly.
Rival *content* cells stand: content stays hidden until closure. Additionally,
editor-facing status labels must collapse the provisional states (`SelectedForApproval` /
`ProvisionallyRejected` both render as "under review"), so D17's deferred rejection
notification is not defeated by the My-Pending panel.

---

## 8. Notifications (in-app inbox, polling — D15)

**Digest rule [A→D24]:** notifications aggregate per (session × event type × batch) — a
500-feature session produces "480 committed, 15 rejected, 5 still pending" with expandable
detail, not 500 inbox rows. Required by reviewer/editor throughput (R5).

| # | Event | Recipients | Content |
|---|---|---|---|
| N1 | Proposal enters / gains competitors | Owners of all proposals in that competition | "Your proposal for ⟨feature⟩ now competes with N−1 others" — count only (D18) |
| N2 | Winner committed | Winning editor | Committed confirmation (digest) |
| N3 | Final rejection (at commit — D17) | Each losing editor | Rejection reason recorded at review time (digest) |
| N4 | Competition closed with no winner | All owners | Reasons (immediate — nothing provisional remains) |
| N5 | Approver return | The reviewer who resolved | Return reason |
| N6 | Proposal became Orphaned (target deleted in live) | Owner + reviewer queue badge | Advisory; see E2 |

Withdrawals notify no one (the actor already knows; queues update silently). Staleness is
a queue badge, not an inbox event — it can flip repeatedly and would spam.

---

## 9. End-to-end walkthroughs

**A — Deliberate competition, happy path.** Editors E1, E2 are (verbally) pointed at
feature F. Each drafts a session, edits F, submits. E1's submission creates a singleton
competition (C1); E2's joins it (C2) — both get N1 ("competing with 1 other"), neither
sees the rival's work. Reviewer opens the compare view (geometry + attribute diff,
authors visible), selects E2 (P3); E1 → ProvisionallyRejected with reason (P4). Approver
sees the Resolved competition, re-validation passes, approves (C6): commit writes E2's
state to live stamped with E2 as author, E1 → Rejected, N2 to E2, N3 to E1.

**B — Bulk session, fast path.** Editor submits 500 proposals; 498 become singletons, 2
join existing competitions. Reviewer bulk-selects the 498 singletons → accept-all (498 ×
P3 in one action); the 2 contested ones go through compare. Approver batch-approves →
one transaction (E9), one digest each way.

**C — Stale winner.** Competition on F resolved for E1; before approval, a queued
competition's winner on neighboring feature G commits and — separately — F itself was
committed last week by another competition, so E1's base is old. Reviewer already recorded
a stale override at selection (D8). At approval, staleness is re-checked: live moved
*again* since review → approver sees a fresh warning and must override again (or return).
Both overrides are in the decision records.

**D — Approver return.** Approver disagrees with the winner, returns with reason (C7).
All proposals revert to Submitted, competition is Open again, reviewer gets N5, losers
never received a rejection notice (D17) — nothing was wasted. Reviewer re-resolves
(possibly picking a previous loser).

**E — Delete vs edit (D12).** E1 proposes deleting F; E2 proposes an update. Same
competition; compare view renders "deletion" as one side. Accepting the delete commits a
live delete; E2's update → Rejected. All other pending proposals on F become Orphaned (E2 flag).

**F — Competing creates (D16).** E1 and E2 both digitize the same new substation. Two
singleton competitions — no id collision. The submission-time overlap scan flags the pair
in the reviewer queue; reviewer manually groups them (C2) into one competition and
resolves normally. If the reviewer skips grouping, both commit — accepted v1 risk (§1).

---

## 10. Overall flow (text flowchart)

```
 EDITOR                       SYSTEM                            SENIOR REVIEWER                APPROVER
────────                     ────────                          ─────────────────             ──────────
create draft session(s)
edit features
(in-memory undo, D14)
     │ submit session
     ▼
                 persist proposals; per target:
                 open competition? ──yes──► join (C2), N1 counts
                       │ no
                       ▼
                 create singleton (C1)
                 overlap scan:
                   vs live → editor warning
                   vs pending → reviewer suggestion
                       │
                       ▼
                                                review queue (age, stale,
                                                orphan badges):
                                                ├─ singletons → bulk accept
                                                ├─ overlap suggestions →
                                                │    manual grouping (C2)
                                                └─ compare view (diffs,
                                                   authors, delete-as-side)
                                                      │
                                          ┌───────────┴───────────┐
                                          ▼                       ▼
                                    select winner (P3)      reject all (C4)
                                    stale? → recorded       → ClosedNoWinner,
                                    override (D8)             N4 final
                                          │
                                          ▼
                                    Resolved: winner → SelectedForApproval
                                              losers → ProvisionallyRejected
                                          │
                                          ▼
                                                                          approval queue
                                                                    ┌───────────┴───────────┐
                                                                    ▼                       ▼
                                                              approve (C6)            return + reason (C7)
                                                              COMMIT TX (E9):              │
                                                              re-validate (R1 gate,        ▼
                                                              staleness → override)   competition reopens,
                                                              live write (author=editor)   all → Submitted,
                                                              history + decisions          N5 to reviewer
                                                              losers → Rejected
                                                              N2/N3 digests
```

---

## 11. Edge cases & rules

- **E1 — Self-supersede [A→D22].** One active proposal per (editor, target). Submitting a
  new proposal for a target where the editor already has a pending one auto-withdraws the
  old (`Withdrawn`, cause=superseded). No notification. Rationale: an editor competing
  with themself is noise; "my latest word on this feature" is the intuitive contract.
- **E2 — Orphaned proposals [A→D23].** If the target is deleted in live (a delete
  committed), remaining pending update-proposals on it are flagged Orphaned (N6). In v1
  they can only be rejected (with an automatic system note); "accept as re-create" is out
  of scope — the editor resubmits as a create if still relevant.
- **E3 — Schema migration with pending proposals (R1).** Commit (P6) re-validates the
  serialized attributes against the current schema; mismatch blocks the commit with a
  clear error → approver returns, reviewer/editor deal with it. An EF-migration checklist
  item ("check pending-proposal compatibility") is part of the ops docs. Details Stage 3.
- **E4 — Concurrent reviewers.** Two reviewers resolving the same competition: optimistic
  concurrency on the competition row; second save fails with a "already resolved" error.
  Same for two approvers on one competition.
- **E5 — Withdraw guards.** Session withdraw: only while all its proposals are still
  `Submitted`/`Draft` (D9). Proposal withdraw: only while its competition is Open — a
  winner cannot withdraw during approval (await commit or return).
- **E6 — Deactivated editor.** Pending proposals remain and are decided normally;
  notifications accumulate in the inactive inbox. No special handling in v1.
- **E7 — Queued competitions [A→D19].** See §4 late-arrival rule. When the predecessor
  commits, the queued competition's proposals are typically Stale by construction —
  handled by the normal D8 override flow, not a special case.
- **E8 — Blind overlap channels.** Restated from §1: editor warnings reference live
  features only; pending-vs-pending only ever surfaces to reviewer/approver.
- **E9 — Batch commit failure.** A batch approval is one transaction: if any competition
  in it fails re-validation, nothing commits; failures are listed; the approver deselects
  the failing ones and retries. Guarantees NFR-3 (all-or-nothing per approved batch).

---

## 12. Stage-2 detail questions (Q23–Q29) — all ANSWERED 2026-08-13

All confirmed with the recommended defaults → decisions **D19–D25** in doc 01 §2. Table
kept for the rationale/impact notes.

| # | Question | Recommendation | Impact if changed |
|---|---|---|---|
| Q23 | Late submissions while a competition is in approval → new **queued** competition (max one Resolved + one Open per target)? | Yes (§4) | Affects Stage-3 uniqueness constraints ("one open competition per target"). |
| Q24 | Reviewer may close a competition **with no winner** (all finally rejected) without approver involvement, since live is untouched? | Yes (C4/P5) | If approver oversight is wanted even for no-winner, add a Resolved-NoWinner state + gate. |
| Q25 | Post-closure visibility: participants see the full record of their own competitions; non-participants see committed history only? | Yes (§7) | Pure policy; widening later is trivial. |
| Q26 | Self-supersede: one active proposal per (editor, target); newer submission auto-withdraws the older? | Yes (E1) | Affects a Stage-3 unique index. |
| Q27 | Orphaned proposals (target deleted in live) can only be rejected in v1 (no accept-as-recreate)? | Yes (E2) | Accept-as-recreate needs id-reuse semantics — real design work. |
| Q28 | Notifications aggregate as per-session digests? | Yes (§8) | Without it, bulk sessions spam inboxes (R5). |
| Q29 | Editors may withdraw an individual pending proposal (not just the whole session), while its competition is Open? | Yes (P8/E5) | Removing it simplifies guards slightly, at UX cost. |

---

## 13. Handoff to Stage 3 (data model)

Entities implied by this document: `Proposal` (polymorphic target: layer/table id +
feature id + serialized state + base RowVersion), `Session`, `Competition` (+ membership),
`DecisionRecord`, `Notification`, per-layer versioning flag (S6/Q17), committed-history
store. Constraints to carry: one open competition per target (Q23), one active proposal
per editor+target (Q26), optimistic concurrency on competition (E4). Unresolved before
Stage 4/6: Q15 (pilot layers), Q17 (closing direct Sync). Biggest open design problem for
Stage 3: the R1 schema-drift gate and the shape of the committed-history tables (S4:
custom, not temporal).
