# SLD generator — rules from a field's values (categorized symbology)

- **Status (2026-10-22):** **all five steps built (uncommitted)**; decisions: palette by default, values that
  occur in the layer by default (all domain values when the data is not at hand), "other values" rule on by
  default (user: "as you suggest"). Real-app run pending. Separate from the field-metadata contract
  (`field-metadata-contract.md`), but it builds on its Step 2 (`Field.Domain`).
- **Goal:** in the SLD editor, generate one rule per value of a field (a coded-value domain, a plain
  allowed-values list, or the values that occur in the data), each with its own colour, so an admin can
  give a layer categorized symbology and save it as the layer's `LayerSld` for every user.

## What exists (verified 2026-10-22; line anchors drift)

- SLD model: `src/Core/IRI.Maptor.Core.Ogc/SLD` — `Rule` with an `OgcFilter` (Filter Encoding) and
  `ElseFilter`, symbolizers Point/Line/Polygon/Text. `SldHelper.Parse/Serialize`.
- Rendering: `Presentation.Wpf/Extensions/SldExtensions.cs` turns each rule's symbolizers into runtime
  symbolizers with `IsFilterPassed`; `OgcFilterExtensions.ParseFilter` supports `PropertyIsEqualTo`,
  `Or`, `Not`, `PropertyIsNull`, … **`ElseFilter` is ignored**: a rule with no `Filter` passes every
  feature, so an "else" rule draws all features again.
- SLD editor: `ViewModels/Symbology/Sld/SldEditorViewModel.cs` (`Rules`, add/duplicate/move/remove,
  import/export, preview), created by `SldEditorViewModel.Create(layerName, sld, fieldNames, geometryType)`
  — it receives field **names** only. Two entry points:
  - map legend: `Common/Defaults/DefaultActions.cs` (`symbolizable.GetFields()`, `SpatialModelMode`),
    saves to the layer / project override;
  - admin window: `Views/LayerSettingsAdmin/ServerLayerSettingsWindow.xaml.cs` (`EditSld`), saves to the
    server's `LayerSetting.LayerSld`; the row knows `FieldNames` only (server:
    `AdminListLayerSettingsQuery` → `ILayerFeatureQueryService.GetFieldNames`) and **no geometry type**
    (`LayerSetting` has no such column).
- Values and colours: `Field.Domain` (value + code, from Step 2) on every Gas/Barg field; Gas domain colours
  (`Domain.mdb`, `rgba(...)`) in `CodedDomainCatalog`, the `CodedValue` table and `GET /Domain/List?name=`.
  They look arbitrary (e.g. «نا مشخص» purple in one domain, dark red in the next). Barg has no colours.
- No colour palette exists in the code base (only theme accents).

## Plan (one step at a time)

1. **Renderer: honour `ElseFilter`.** In `SldExtensions`, an else rule passes a feature when no other
   rule's filter of the same FeatureTypeStyle does (scale ranges considered as the spec says). Legend
   shows it as "other values". Tests with synthetic features. Fixes hand-written SLDs too.
2. **Generator (no UI)** — a pure function: field name, values `[(value, label, colour?)]`, geometry kind,
   a template symbolizer, options → `List<Rule>`. One rule per value: `PropertyIsEqualTo(field, value)`,
   `Title` = label/value (legend text), the template's style with only the colour changed (polygon: fill,
   stroke a darker shade; line: stroke; point: mark fill). Optional fallback rule (`ElseFilter`, grey) for
   empty / out-of-domain values. A built-in qualitative palette (about 12 clearly distinct colours, then
   lighter/darker variants) for when no colours are given. Unit tests.
3. **SLD editor UI:** "Generate rules from field values…" — field picker (domain fields first), value
   source (domain values / values present in the layer / both), colour source (palette / domain colours
   when available), replace or append rules, include the fallback rule. The result lands in the editor's
   rule list, so the existing preview, editing and Cancel apply.
   Needs `SldEditorViewModel.Create` to take `Field` objects (not names), plus optional providers for
   "values present in the data" and "domain colours".
4. **Gas domain colours:** a provider over `GET /Domain/List?name=` in the Gas client, passed to both entry
   points.
5. **Admin entry point:** give the admin editor fields and geometry — from the loaded map layer with the
   same setting id when it is on the map; otherwise from the server (`Fields` instead of `FieldNames` in
   the admin DTO) with the geometry chosen by the admin in the dialog.

## Progress log

| Date | What |
|---|---|
| 2026-10-22 | Planned (five steps); decisions answered "as suggested" |
| 2026-10-22 | Steps 1–5 built; 15 xUnit tests; dialog rendered offscreen in fa + en; one bug found by the render (fields typed `"object"` were hidden from the generator) and fixed with `IsCategorizable` |
| 2026-10-22 | The user found the editor entry point in the app; a full real-app run is still pending |
| next | Real-app run (legend → SLD editor → generator; admin window on a coded Gas layer); admin fields for layers not on the map when the field contract's Step 5 exists |

## What was built (2026-10-22)

1. `Presentation.Wpf/Extensions/SldExtensions.cs` — an ElseFilter rule passes the features no other rule of
   its FeatureTypeStyle passes (scale ranges not considered). `OgcFilterExtensions` `=`/`≠` now compare the
   attribute as invariant text (a decimal under a Persian UI used to format as "0/75").
2. `Core.Ogc/SLD/1.0.0/SldCategoryRules.cs` — `Create(categories, options)`, `Palette` (12 colours: matplotlib
   tab10 + 2), `GetPaletteColor` (lighter/darker beyond 12), `ParseColor` (hex, rgb/rgba).
3. `ViewModels/Symbology/Sld/SldRuleGeneratorViewModel.cs` + `Views/Symbology/Sld/SldRuleGeneratorWindow.xaml`;
   the editor's toolbar button (AutoFix icon) and `GenerateRulesCommand`; `SldEditorViewModel.Create(...,
   fields)`, `ValuesProvider`, `DomainColorsProvider` / static `DefaultDomainColorsProvider`. The selected
   rule's symbolizer is the template. Fields: `IsCategorizable` (looser than the attribute table's rule, so
   "object"-typed file-layer fields are offered).
4. Gas: `Services/GasDomainColors.cs` (GET Domain/List, cached, falls back to the compiled catalog),
   `GasApiClient.GetDomainAsync`, registered in `App.OnStartup`.
5. Admin: `ServerLayerSettingsWindow.ShowDialog(..., findMapLayer)`; Gas and Barg pass
   `FindLayerByAuxilaryId`, so the admin editor gets the map layer's fields, geometry and values.
   **Deferred:** a layer that is not on the map gets names only (no generator) — sending every layer's
   fields in the admin list would be ~1 MB per call; the field-contract Step 5 schema endpoint is the
   better source.

Tests: `tests/IRI.Maptor.Tests/OGC/SldElseFilterTest.cs` (3), `SldCategoryRulesTest.cs` (6),
`SldRuleGeneratorTest.cs` (6); offscreen renders of the dialog in fa and en (synthetic layer).

## Open decisions (answered 2026-10-22: as suggested)

- Default colour source: built-in palette or domain colours.
- Default value source: all domain values, or only values that occur in the layer (long domains: 79 values).
- Whether to include the fallback rule by default.

## Out of scope

Graduated (numeric range) symbology, label generation, server-side SLD generation.
