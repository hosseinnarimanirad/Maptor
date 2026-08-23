# IRI.Maptor.Presentation.Wpf

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Presentation.Wpf?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Presentation.Wpf/)
[![Target](https://img.shields.io/badge/net8.0--windows-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

The WPF UI tier of the Maptor stack. It hosts the central `MapViewer` control together with the
MVVM infrastructure, layer model, cartography primitives, map-marker controls, and WPF resource
dictionaries that Maptor-based desktop applications build on. Targets .NET 8.0 on Windows with
both WPF and Windows Forms enabled.

## Installation

```bash
dotnet add package IRI.Maptor.Presentation.Wpf
```

Requires Windows (WPF). Alternatively, add `IRI.Maptor.Presentation.Wpf.csproj` to your solution and
reference it from your WPF app.

## Features

- `MapViewer` control (`IRI.Maptor.Presentation.Wpf.Controls` XAML namespace) with companion views: sketch bar,
  geometry editor, coordinate panel, go-to (geodetic/projected), map extent panel, legends, and
  scalebar.
- MVVM building blocks: `ViewModelBase`, `RelayCommand`, and the abstract `MapViewModelBase` that
  drives the map, plus the `IDialogService` dialog abstraction.
- Layer model: `BaseLayer` and concrete layers such as `FeatureLayer`, `VectorLayer`,
  `RasterLayer`, `TileServiceLayer`, `GridLayer`, `GroupLayer`, `DrawingLayer`,
  `EditableFeatureLayer`, and `ClusteredPointLayer`.
- Cartography primitives: `VisualParameters`, symbologies, and rendering helpers.
- Map markers: WPF user controls for location, label, photo, and textbox markers.
- Localization: RTL-aware language switching (`LanguageCombo`, WPF binding extensions) on top of
  the `LocalizationManager` in IRI.Maptor.Presentation.Core.
- WPF assets: resource dictionaries for converters, colors, fonts, and animations.
- Office helpers: minimal Excel/Word export utilities built on OpenXML.

## Usage

Place a `MapViewer` in a window and initialize it with a presenter derived from
`MapViewModelBase`:

```xml
<Window xmlns:maptor="clr-namespace:IRI.Maptor.Presentation.Wpf.Controls;assembly=IRI.Maptor.Presentation.Wpf" ...>
    <maptor:MapViewer x:Name="map" />
</Window>
```

```csharp
var presenter = new AppViewModel(); // your class derived from MapViewModelBase

await MapInitializationHelper.InitializeMapAsync(this.map, this, presenter);

this.DataContext = presenter;
```

See the WPF gallery for a window per feature - measurement, drawing, legends, markers, identify,
localization: https://github.com/hosseinnarimanirad/Maptor/blob/master/samples/IRI.Maptor.Samples.Wpf.Gallery/README.md

### Bring the resources into your app

Merge the built-in resource dictionaries to unlock the package's colors, fonts, converters, and
animations:

```xml
<!-- App.xaml -->
<Application x:Class="YourApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/IRI.Maptor.Converters.xaml"/>
                <ResourceDictionary Source="pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/IRI.Maptor.Fonts.xaml"/>
                <ResourceDictionary Source="pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/IRI.Maptor.Colors.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

Converters are then available by key, e.g. `boolToVisibilityConverter` and
`stringToColorConverter`. An `IRI.Maptor.Animations.xaml` dictionary is also available.

## Dependencies

- Windows only: .NET 8.0 (`net8.0-windows`) with WPF and Windows Forms.
- Builds on the Maptor Sta packages (Spatial, SpatialReferenceSystem, ShapefileFormat, Ogc, ...)
  and IRI.Maptor.Presentation.Core (tile providers, localization store).
- Third-party: MahApps.Metro (+ icon packs), WriteableBitmapEx, Microsoft.Xaml.Behaviors.Wpf,
  DocumentFormat.OpenXml, Stateless.

## See also

- [SLD symbology editor](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Presentation/IRI.Maptor.Presentation.Wpf/Views/Symbology/Sld/README.md)

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Presentation.Wpf/) ·
[Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) ·
[Back to IRI.Maptor.Presentation](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Presentation/README.md)
