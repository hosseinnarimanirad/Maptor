# README style guide

This guide defines the single standard for every README in this repository. When creating or
editing a README, find its type in the [file-to-type assignment](#appendix-file-to-type-assignment),
follow that type's template, and respect the global rules below.

## Scope

Applies to all README files in the repository, with one exclusion:

- `**/ThirdPartyLibraries/**` — vendored third-party documentation; never modify.

## Global rules

1. **Filename** is exactly `README.md` (case-sensitive). Supplementary docs (e.g.
   `USAGE_EXAMPLES.md`) keep their own names and are not covered by this guide.
2. **One H1 on line 1**, naming the thing verbatim: the package id (`# IRI.Maptor.Sta.Spatial`),
   the topic (`# Shortest paths`), or the format (`# SVG`). Badges, if any, come immediately after
   the H1.
3. **Sentence case for all H2+ headings**: `## Getting started`, not `## Getting Started`.
   The H1 keeps the proper name's own casing.
4. **Emoji** are allowed only in the root `README.md` (title and marketing sections). All other
   READMEs use plain text headings and bullets. Back-links use plain text, not `⬅`.
5. **Links**: use relative links within the repo, **except** in READMEs that are packed into a
   NuGet package (`<PackageReadmeFile>` in the csproj) — those must use absolute URLs
   (`https://github.com/hosseinnarimanirad/Maptor/blob/master/...`) because relative links break
   on nuget.org. NuGet-packed READMEs must also avoid HTML that nuget.org strips
   (`<div align>`, `<img>` wrappers) — plain Markdown only.
6. **No unverifiable claims.** Every feature bullet and capability statement must match the code.
   File counts, performance numbers, and superlatives without a source are removed on sight.
   Known precedent: CesiumTerrain is *reader-only* — writing is not implemented.
7. **Language** is English. Bilingual English/Persian is allowed in application-specific
   documentation sets.
8. **Length** targets per type are in the taxonomy table. Shorter is fine; padding is not.
9. **Code samples** must compile against the current API. Known gotcha:
   `Geometry<T>.CreatePointOrLineString(List<T> points, int srid)` is the only valid overload; the
   KML/KMZ namespace is `IRI.Maptor.Sta.KmlFormat` (not `...Sta.Ogc.KML`).
10. **No ASCII folder trees** ("Project structure" sections). Folder layout is visible in the repo
    itself; trees rot silently. The exception is docs whose *subject* is a layout (e.g. a package
    format's directory structure).
11. **Public paths only.** READMEs must reference only paths that exist in the public repository
    (github.com/hosseinnarimanirad/Maptor).

## Badge policy

| README type | Allowed badges | Link target |
|---|---|---|
| A. Root landing | max 5: NuGet (flagship pkg), license, .NET, build/CI | each badge links to what it shows — license badge → LICENSE.txt, .NET Standard badge → the .NET Standard docs (never a different version's download page) |
| C. Package | exactly 2: NuGet version + target framework | NuGet badge → `https://www.nuget.org/packages/<id>/`; `netstandard2.1` → `https://learn.microsoft.com/dotnet/standard/net-standard`; `net8.0` → `https://dotnet.microsoft.com/download/dotnet/8.0` |
| B, D, E, F | none | — |

Badge images use shields.io. A badge whose subject or link is wrong is worse than no badge.

## Type taxonomy

| Type | Applies to | Required sections, in order | Length |
|---|---|---|---|
| **A. Root landing** | `/README.md` | H1 + tagline → badges → What is Maptor → Repository layout → Installation → Quick start → Documentation → License | ≤ 250 |
| **B. Tier index** | `src/IRI.Maptor.{Sta,Ket,Jab}/README.md` | H1 → one-line scope → project table → back-link footer | ≤ 60 |
| **C. Package** | each NuGet-published project root | H1 (package id) → badges → overview paragraph → Installation → Features → Usage → footer | 60–150 |
| **D. Deep-dive** | sub-folder topic/algorithm and format docs | topic: H1 → prose (free-form) → optional References → back-link. format: H1 → overview → Supported capabilities → Usage → Limitations → back-link | ≤ 400 |
| **E. App/ops guide** | application deployment/configuration guides | H1 → purpose paragraph → Prerequisites → numbered steps/config → footer | free |
| **F. Internal/tooling** | `research/`, `samples/` | H1 → purpose paragraph → How to run (if runnable) → back-link | ≤ 40 |

## Templates

### Type A — root landing

The root README is rebuilt rarely; keep its existing section order (above) and apply the global
rules. Emoji allowed. All URLs absolute or repo-relative and verified.

### Type B — tier index

```markdown
# IRI.Maptor.<Tier>

<One sentence: what this tier is and what runtime it targets.>

| Project | NuGet | Target | Description |
|---|---|---|---|
| [IRI.Maptor.<Tier>.<Name>](IRI.Maptor.<Tier>.<Name>/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.<Tier>.<Name>?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.<Tier>.<Name>/) | netstandard2.1 | <one line> |

---
[Back to the solution README](../../README.md)
```

### Type C — package (NuGet-packed: absolute URLs only)

````markdown
# IRI.Maptor.<Tier>.<Name>

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.<Tier>.<Name>?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.<Tier>.<Name>/)
[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

<One paragraph: what the package does and where it sits in the Maptor stack.>

## Installation

```bash
dotnet add package IRI.Maptor.<Tier>.<Name>
```

## Features

- <capability, verified against code>

## Usage

```csharp
<short sample that compiles against the current API>
```

## See also

- [<Sub-topic>](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.<Tier>/IRI.Maptor.<Tier>.<Name>/<Folder>/README.md)

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.<Tier>.<Name>/) ·
[Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) ·
[Back to IRI.Maptor.<Tier>](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.<Tier>/README.md)
````

Optional sections (between Usage and See also): `## Dependencies`, `## Limitations`.

### Type D — deep-dive

Topic/algorithm variant (prose is the asset — structure stays free-form):

```markdown
# <Topic name>

<Explanatory prose, diagrams, code. Sentence-case headings, no emoji, no badges,
no installation section — link the owning package instead.>

## References

- <optional>

---
[Back to IRI.Maptor.<Tier>.<Name>](../README.md)
```

Format reference variant adds a required capability matrix, verified against code:

```markdown
# <Format name>

<Overview: what the format is, why Maptor supports it.>

## Supported capabilities

| Capability | Supported |
|---|---|
| Read | Yes |
| Write | No |

## Usage

<samples>

## Limitations

- <honest list>

---
[Back to IRI.Maptor.Sta.Spatial](../../README.md)
```

### Type E — app/ops guide

```markdown
# <What this guide covers>

<Purpose paragraph: what you end up with when done.>

## Prerequisites

- <item>

## <Step group 1>

1. <numbered steps>

## Troubleshooting

<optional>
```

### Type F — internal/tooling

```markdown
# <Tool/folder name>

<One paragraph: what this is for.>

## How to run

<commands, if runnable>

---
[Back to the solution README](../README.md)
```

## Appendix: file-to-type assignment

Excluded (never touch): `src/IRI.Maptor.Ket/IRI.Maptor.Ket.WindowsBase/ThirdPartyLibraries/ManagedNativeWifi/README.md`.

| File | Type |
|---|---|
| `README.md` | A |
| `src/IRI.Maptor.Sta/README.md` | B |
| `src/IRI.Maptor.Ket/README.md` | B |
| `src/IRI.Maptor.Jab/README.md` | B |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Common/README.md` | C |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Graph/README.md` | C |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.GsmGprs/README.md` | C |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.MachineLearning/README.md` | C |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Ogc/README.md` | C |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Pdf/README.md` | C |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Persistence/README.md` | C |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Security/README.md` | C |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.ShapefileFormat/README.md` | C |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/README.md` | C |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.SpatialReferenceSystem/README.md` | C |
| `src/IRI.Maptor.Ket/IRI.Maptor.Ket.EfCorePersistence/README.md` | C |
| `src/IRI.Maptor.Ket/IRI.Maptor.Ket.GdiPlus/README.md` | C |
| `src/IRI.Maptor.Ket/IRI.Maptor.Ket.PersonalGdbPersistence/README.md` | C |
| `src/IRI.Maptor.Ket/IRI.Maptor.Ket.PostgreSqlPersistence/README.md` | C |
| `src/IRI.Maptor.Ket/IRI.Maptor.Ket.SqlServerPersistence/README.md` | C |
| `src/IRI.Maptor.Ket/IRI.Maptor.Ket.SqlServerSpatialExtension/README.md` | C |
| `src/IRI.Maptor.Ket/IRI.Maptor.Ket.SqlitePersistence/README.md` | C (keeps its extended format sections) |
| `src/IRI.Maptor.Ket/IRI.Maptor.Ket.WebApiPersistence/README.md` | C |
| `src/IRI.Maptor.Ket/IRI.Maptor.Ket.WindowsBase/README.md` | C |
| `src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/README.md` | C |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Common/Mathematics/Algebra/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Graph/Clustering/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Graph/GraphRepresentation/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Graph/MinCut/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Graph/MinimumSpanningTree/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Graph/Search/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Graph/ShortestPaths/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Ogc/GML/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Ogc/KML/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Ogc/KMZ/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Ogc/SLD/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Ogc/WFS/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Ogc/WMS/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/AdvancedStructures/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/Analysis/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/Analysis/DigitalTerrainModeling/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/Analysis/Interpolation/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/Analysis/SFC/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.SpatialReferenceSystem/CoordinateSystems/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.SpatialReferenceSystem/MapProjections/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.SpatialReferenceSystem/Models/README.md` | D (topic) |
| `src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Views/Symbology/Sld/README.md` | D (topic) |
| `samples/IRI.Maptor.Tag.SampleCodes/Geodesy/README.md` | D (topic) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/IO/CesiumTerrain/README.md` | D (format) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/IO/Dxf/README.md` | D (format) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/IO/Eps/README.md` | D (format) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/IO/EsriJson/README.md` | D (format) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/IO/GeoJsonFormat/README.md` | D (format) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/IO/OgcSFA/README.md` | D (format) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/IO/PmTiles/README.md` | D (format) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/IO/SqlServerNativeBinary/README.md` | D (format) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/IO/Svg/README.md` | D (format) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/IO/TopoJson/README.md` | D (format) |
| `src/IRI.Maptor.Sta/IRI.Maptor.Sta.Spatial/IO/VectorTiles/README.md` | D (format) |
| `research/IRI.Maptor.Res.FastSimplification/README.md` | F |
| `samples/IRI.Maptor.Tag.SampleWpfApp/README.md` | F |

## Checklist for new or edited READMEs

- [ ] Filename is exactly `README.md`
- [ ] Single H1 on line 1, naming the package/topic/format verbatim
- [ ] H2+ headings are sentence case
- [ ] No emoji (root README excepted)
- [ ] Sections match the file's type template, in order
- [ ] Badges only where the type allows, with correct link targets
- [ ] NuGet-packed README → all links absolute, no HTML
- [ ] Every capability claim verified against code; no unverifiable statistics
- [ ] Code samples compile against the current API
- [ ] No ASCII folder tree
- [ ] Footer back-link present and resolving
