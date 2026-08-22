# IRI.Maptor.Presentation

The **Jab** tier is Maptor's presentation layer for building desktop GIS applications on WPF. It provides the interactive **`MapViewer`** control, MVVM infrastructure, layer and symbology types, tile-service providers, and a localization system with right-to-left language support. `IRI.Maptor.Presentation.Wpf` and `IRI.Maptor.Presentation.Core` are published to [NuGet](https://www.nuget.org/packages?q=IRI.Maptor); `IranRepo`, `Maui` and `Blazor` are not packaged.

## Projects

| Project | NuGet | Target | Description |
|---------|-------|--------|-------------|
| [IRI.Maptor.Presentation.Wpf](IRI.Maptor.Presentation.Wpf/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Presentation.Wpf.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Presentation.Wpf) | net8.0-windows | The WPF map UI library: hosts the central `MapViewer` control, MVVM building blocks (view-model base classes, commands), layer types (feature, raster, grid, group), cartographic rendering and symbology, tile-service providers (Google, Bing, OSM, and more), map markers, converters/behaviors, and Excel/Word export helpers. |
| [IRI.Maptor.Presentation.Core](IRI.Maptor.Presentation.Core/) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Presentation.Core.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Presentation.Core) | net8.0 | UI-framework-independent core shared by the presentation stack: tile-service URL factories, layer and data models, helpers, and the localization resource store (English base plus 14 additional cultures, including right-to-left Persian and Arabic). |
| [IRI.Maptor.Presentation.IranRepo](IRI.Maptor.Presentation.IranRepo/) | not published | net8.0-windows | Iran-specific data repositories and layer management for Iranian basemaps and datasets. |

## Notes

- `Jab.Wpf` depends on the [Sta core libraries](../Core/README.md) and the [Ket adapters](../Infrastructure/README.md); data sources registered through the persistence abstractions appear as map layers.
- Localization resources live in `Jab.Core`; the UI binds resource keys dynamically, so the interface language can be switched at runtime.
- For runnable examples of the `MapViewer`, see the sample applications in the repository's `samples/` folder.

---

[Back to the solution README](../../README.md)
