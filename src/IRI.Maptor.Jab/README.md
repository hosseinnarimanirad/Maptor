# IRI.Maptor.Jab

The **Jab** tier is Maptor's presentation layer for building desktop GIS applications on WPF. It provides the interactive **`MapViewer`** control, MVVM infrastructure, layer and symbology types, tile-service providers, and a localization system with right-to-left language support. All three projects are published to [NuGet](https://www.nuget.org/packages?q=IRI.Maptor).

## Projects

| Project | NuGet | Target | Description |
|---------|-------|--------|-------------|
| [IRI.Maptor.Jab.Common](IRI.Maptor.Jab.Common/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Jab.Common.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Jab.Common) | net8.0-windows | The WPF map UI library: hosts the central `MapViewer` control, MVVM building blocks (view-model base classes, commands), layer types (feature, raster, grid, group), cartographic rendering and symbology, tile-service providers (Google, Bing, OSM, and more), map markers, converters/behaviors, and Excel/Word export helpers. |
| [IRI.Maptor.Jab.Core](IRI.Maptor.Jab.Core/) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Jab.Core.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Jab.Core) | net8.0 | UI-framework-independent core shared by the presentation stack: tile-service URL factories, layer and data models, helpers, and the localization resource store (English base plus 14 additional cultures, including right-to-left Persian and Arabic). |
| [IRI.Maptor.Jab.IranRepo](IRI.Maptor.Jab.IranRepo/) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Jab.IranRepo.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Jab.IranRepo) | net8.0-windows | Iran-specific data repositories and layer management for Iranian basemaps and datasets. |

## Notes

- `Jab.Common` depends on the [Sta core libraries](../IRI.Maptor.Sta/README.md) and the [Ket adapters](../IRI.Maptor.Ket/README.md); data sources registered through the persistence abstractions appear as map layers.
- Localization resources live in `Jab.Core`; the UI binds resource keys dynamically, so the interface language can be switched at runtime.
- For runnable examples of the `MapViewer`, see the sample applications in the repository's `samples/` folder.

---

[Back to the solution README](../../README.md)
