# Field metadata contract — `Field` as the one schema for layers

- **Status (2026-10-22):** proposal agreed with the user; built one step at a time. **Steps 1–2 built
  2026-10-22 (uncommitted).** Steps 3–5 below are not started.
- **Why:** clients should not guess a field's kind, and future per-field settings (read-only,
  restricted, display format, units, coded domains) need one home that both server and client honour.

## Progress log

| Date | What |
|---|---|
| 2026-10-22 | Proposal agreed (five steps) |
| 2026-10-22 | **Step 1 built**: `FieldDataType` + tolerant JSON converter, `Field.ResolveDataType()`, consumers switched; 33 xUnit tests |
| 2026-10-22 | **Step 2 built**: `Field.Domain` (value + code; no colours, by decision), `FieldDomainAttribute`, Gas `CodedDomainAttribute` from the catalog; **`Field.AllowedValues` removed from the contract** (user: all clients updated together); `generate.py` changed and 65 entity files regenerated; statistics tab domain checks; 8 xUnit tests + a harness over the real Gas assembly (344/344) |
| 2026-10-22 | Side feature planned and built from Step 2: categorized SLD generator (`sld-from-field-values.md`) |
| next | **Step 3 needs a design discussion first** (security, save path of Gas and Barg). Small fix available any time: fields typed `"object"` are hidden from the attribute table |

Not committed and not yet run in the real apps at the time of writing. **Deploy servers and clients
together**: an older client reads only `AllowedValues`, which new servers no longer send.

## What exists (verified 2026-10-22; line anchors drift)

- `Field` (`src/Core/IRI.Maptor.Core.Common/Primitives/Field.cs`, netstandard2.1): `Name`, `TypeFullName`,
  `Alias`, `Length`, `IsNullable`, `Precision`, `Scale`, `DateTimePrecision`, `CanRead`, `CanWrite`,
  `AllowedValues` (flat `object[]`), `DisplayFormat`, `TextDirection`.
- Filled on the servers by reflection: `Field.GetFields<TEntity>()` reads `[Field(...)]`
  (`Core.Common/Common/Attributes/FieldAttribute.cs`). Gas: every `List*Query.g.cs`
  (`new FeatureSetDto(entities.Parse(), Field.GetFields<T>(), T.LayerCode)`, 94 entity files use `[Field]`);
  Barg: 58 producers. Live pages get fields the same way (`LayerFeatureQueryService.GetFieldsMethod`).
- Transported in `FeatureSetDto.Fields` and `CompactFeatureSetDto.Fields` (also `FeaturePageDto`).
  Both APIs register `JsonStringEnumConverter` globally.
- Client consumers: attribute table (`Presentation.Wpf/Assets/Behaviors/DataGridDictionaryBehavior.cs`:
  `CanRead`, `CanWrite`, `AllowedValues`, `DisplayFormat`, `TextDirection`), identify
  (`Helpers/IdentifyAttributeHelper.cs`), filters (`Models/Map/Filters/FeatureTableFilterManager.cs`),
  statistics tab (`Models/LayerSettings/FieldStatistics.cs`), symbology alias lookup, Blazor feature table.

## Problems found

1. **`TypeFullName` is overloaded.** Servers and the WPF client put a .NET type name there
   (``System.Nullable`1[[System.Double, System.Private.CoreLib, …]]``), parsed with `Type.GetType` in
   about 8 client places. The SQL Server and Personal GDB infrastructure and SpatialDataManagement put
   **SQL type names** there (`"geometry"`, `"nvarchar"`; see the `todo` in
   `Infrastructure.SqlServer/SqlServerInfrastructure.cs` near "add another field for sql server specific
   types"). File/memory layers type fields from the first row (`Field.FromDictionary`), giving `"object"`
   when that value is empty.
2. **Read-only is not enforced from the metadata.** The Gas save path (`Gas/Infrastructure/.../Repositories/UnitOfWork.cs`)
   picks writable properties with `PropertyInfo.CanWrite` plus a hard-coded `ReadOnlyProperties` list, not
   `[Field(CanWrite = false)]`. Today they agree; nothing keeps them in step, and a crafted request can
   change a field that only the attribute marks read-only.
3. **Two domain systems:** `AllowedValues` (labels only) and `[CodedDomain]` + `DomainController`
   (code → label) that never reaches `Field`. Clients show raw codes.
4. **The schema is repeated** in every list response and every Live page.

## The plan

### Step 1 — logical data type (`FieldDataType`) — BUILT 2026-10-22
- `Core.Common/Common/Enums/FieldDataType.cs`: `Unknown, Text, Boolean, Integer, Long, Double, Decimal,
  DateTime, Guid, Binary, Geometry` plus `FieldDataTypeJsonConverter` — reads names or numbers, turns
  anything unknown into `Unknown` (never throws), writes the name. It sits on the `Field.DataType`
  **property**, because a property converter wins over the `JsonStringEnumConverter` that the client's
  `JsonHelper.DefaultOptions` register (that one throws on unknown names — a newer server adding a member
  would otherwise break whole feature sets for older clients).
- `Core.Common/Primitives/FieldDataTypes.cs`: `FromClrType` (Nullable unwrapped; enums → Text because the
  APIs send names; NTS / SqlGeometry / SqlGeography / `Geometry<T>` → Geometry), `FromTypeName`,
  `IsNumeric()`. `Field.ResolveDataType()` = `DataType`, else `FromTypeName(TypeFullName)`.
- Filled by `Field.GetFields<T>` (all Gas/Barg list queries and Live pages, no `.g.cs` change),
  `Field.FromDictionary`, DBF fields, `SqlServerDataSource`. SQL-type-name producers stay Unknown.
- Client switched to the resolver: `FeatureTableHelper.IsDisplayableField` (now the one display rule,
  also used by `DataGridDictionaryBehavior`), `IsGeometryField`, filters, identify, export headers,
  statistics tab. Behaviour for current payloads unchanged (verified by tests).
- Tests: `tests/IRI.Maptor.Tests/Common/FieldDataTypeTest.cs` (33). Full suite: 57 failures, all the
  documented baseline (GeoJson/EsriJson/Dxf round-trips, MVT) plus the known OleDb flake
  (`Esri.PersonalGdbTest`, passes alone).
- Found, not changed: fields typed `"object"` (file/memory layer whose first value is empty) are
  **hidden from the attribute table** — `Type.GetType("object")` is null (C# keyword, not a .NET name).
  It was so before Step 1 too. Fix candidates: `FromDictionary` typing from the first non-empty value, or
  showing Unknown fields as plain text columns.

### Step 2 — field domains — BUILT 2026-10-22
Findings that reshaped it: Gas stores the domain **title**, not the gdb code, so clients already showed
labels; `[Field(AllowedValues)]` (generated) duplicated `[CodedDomain]` (344 fields, 65 layers, all equal);
the catalog's colours (`Domain.mdb`) have no consumer and look arbitrary — layers draw from `LayerSld`.
- `Core.Common/Primitives/FieldDomain.cs`: `FieldDomain { Name, Description, Values }`,
  `FieldDomainValue { Value (CLR primitive via ObjectValueConverter), Code, Label }`, `Contains` (numbers by
  value whatever their CLR type, text ordinal, null never a member), `FromValues`. **No colours** (user
  decision 2026-10-22: symbology belongs in the layer's SLD).
- `Core.Common/Common/Attributes/FieldDomainAttribute.cs` (abstract `GetDomain()`); Gas's
  `CodedDomainAttribute` derives from it and answers from `CodedDomainCatalog` (cached, duplicate titles
  listed once with their first code — same as the generator).
- `Field.Domain` (omitted from JSON when null); `Field.GetFields<T>` fills it from a domain attribute, else
  from `[Field(AllowedValues = …)]` (Barg's 270 plain lists, incl. numeric ones).
- **`Field.AllowedValues` removed from the contract** (user decision: all clients are updated together).
  `[Field(AllowedValues = …)]` stays as the declaration syntax for plain lists. `generate.py` no longer
  emits it beside `[CodedDomain]`; the 65 entity files were regenerated (verified first that the unmodified
  generator reproduces all 578 files byte for byte; the diff was exactly the 344 `AllowedValues` lines).
- Clients read `Field.Domain` (attribute-table combo, filters). Statistics tab: "outside the domain" tile,
  "N of M domain values used", warning rows, group-by note.
- **Deploy servers and clients together:** an older client reads only `AllowedValues`, which new servers
  no longer send (its attribute-table combos and fixed filter lists for domain fields would disappear).
- Tests: `tests/IRI.Maptor.Tests/Common/FieldDomainTest.cs` (8); a scratchpad harness over the real Gas
  assembly: all 344 coded fields get a domain identical to the catalog; Fields JSON per layer max 31.6 KB
  (Valve), median 9.9 KB.
- Generator note: run it with `python -X utf8` (it prints Persian notes; `-I` ignores PYTHONIOENCODING).

### Step 3 — server-side enforcement and per-user access (design discussion first)
`Access = Hidden | ReadOnly | Editable` computed per user's role; hidden fields are **omitted** from
responses (values included), not hidden by the client. The save path derives writability from the same
merged metadata (fixes problem 2). Touches security and the save path of both Gas and Barg.

### Step 4 — display and analysis extras, admin overrides
`Unit`, `Order`, `Description` (tooltip), required/min/max for editing, and a measure-vs-category hint
(numeric codes such as a pressure class get frequencies, not a mean). Defaults stay in `[Field]`
attributes; per-layer overrides in the database, edited in the existing layer-settings admin UI.

### Step 5 — schema endpoint
`GET /Layers/{id}/Schema` (or inside the layer-settings response) with a version/ETag, fetched once;
keep `Fields` in feature responses for a while, then trim. It would also give the admin SLD editor the
fields of layers that are not on the admin's map (see `sld-from-field-values.md`, deferred).

## Compatibility rule for every step
Additions are new JSON properties only. Old clients ignore unknown properties (System.Text.Json
default); a new client against an old server sees defaults and falls back to the old behaviour.
