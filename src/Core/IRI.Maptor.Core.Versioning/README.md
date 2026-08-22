# IRI.Maptor.Core.Versioning

Core model for **feature-level spatial versioning with competitive review**: edits to
individual features are captured as *proposals* grouped in single-editor *sessions*;
proposals targeting the same feature form a *competition* that a reviewer resolves by
selecting one winner; an approver commits the winner to the live state, with full
decision history preserved.

This project is EF/UI-free (netstandard2.1) and holds:

- **Entities** — `VersionedLayer`, `VersionSession`, `Proposal`, `Competition`,
  `DecisionRecord`, `CommitBatch`, `FeatureHistory`, `VersionNotification`,
  `OverlapSuggestion`.
- **Enums** — proposal/competition/session states, decision actions, notification types.
- **Guards** (`VersioningGuards`) — pure transition-rule functions shared by client UI
  (enable/disable actions) and server services (the authority).
- **SchemaSignatureCalculator** — hash of a layer's field schema, used to detect drift
  between a proposal's serialized attributes and the current table schema.
- **DTOs** — submission/result contracts shared between client and server.

Persistence lives in `IRI.Maptor.Infrastructure.Versioning`. The full design (decisions
D1–D44, workflow, data model, architecture, UI) is documented in
`docs/features/spatial-versioning/` at the repository root.
