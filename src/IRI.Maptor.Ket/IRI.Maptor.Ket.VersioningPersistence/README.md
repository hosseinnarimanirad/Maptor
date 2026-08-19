# IRI.Maptor.Ket.VersioningPersistence

EF Core persistence for `IRI.Maptor.Sta.Versioning` (feature-level spatial versioning
with competitive review). A consuming `DbContext` opts in with one call:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddMaptorVersioning(); // schema "versioning" by default
}
```

The provider plugin from `IRI.Maptor.Ket.EfCorePersistence` (`UseMaptorGeometry()`) must
be active on the context — proposal/history geometry columns are typed `geography`
(matching typical live SHAPE columns byte-for-byte, and letting the overlap scan run
STIntersects directly against live data) and materialize into `Geometry<Point>` through it.

Contents:

- `Configurations/` — one `IEntityTypeConfiguration` per entity. Workflow constraints
  are enforced by the database itself via filtered unique indexes: at most one Open and
  one Resolved competition per feature, and one active proposal per editor + feature.
- `CanonicalAttributeSerializer` — attribute dictionaries ⇄ canonical JSON (ordinally
  sorted keys, invariant culture, ISO-8601 dates) so stored state is diffable and stable.
- `EfSchemaSignatureCalculator` — computes a layer's schema signature from EF model
  metadata (call at API startup and stamp into `VersionedLayer`).

Notes:

- The **spatial index** on `Proposal.ProposedGeometry` / `FeatureHistory.Geometry`
  cannot be declared through EF — add `CREATE SPATIAL INDEX` statements to the migration
  by hand (geography grid; no bounding box required).
- Services (submission, review, commit, inbox, history) arrive with milestone M2+ of
  `docs/features/spatial-versioning/06-implementation-plan.md`.
