# IRI.Maptor.Samples.Wpf.HelloMap

The smallest complete Maptor WPF application: a window with a `MapViewer`, a base-map picker, a
scale bar and a coordinate panel. Read `App.xaml` and `MainWindow.xaml.cs` and you have seen the
whole startup path of every Maptor app.

## What it shows

- `App.xaml` — the four dictionaries a Maptor app merges: the MahApps.Metro baseline (controls,
  fonts, a theme) and then `Assets/Maptor.All.xaml`, which carries every Maptor dictionary, the
  semantic status palette and the `Localization` provider. Copy this block verbatim; the MahApps
  lines have to stay above `Maptor.All.xaml` and outside it.
- `App.xaml.cs` — `Encoding.RegisterProvider`, needed to read legacy code pages in shapefile `.dbf` files.
- `MainWindow.xaml` — `MapViewer`, `Scalebar`, `CoordinatePanelView`, and a `ComboBox` over `MapProviders`.
- `MainWindow.xaml.cs` — the four initialization steps below.

## The essential code

```csharp
var presenter = new HelloMapViewModel();                         // 1. MapViewModelBase subclass

presenter.InitializeSettings(                                    // 2. settings
    ProxySettings.Default,
    BaseMapSettings.Default,
    new MapSettings { InitialExtent = BoundingBoxes.WebMercator_Europe },
    GeneralSettings.Default);

await MapInitializationHelper.InitializeMapAsync(map, this, presenter);   // 3. bind control + dialogs

DataContext = presenter;

presenter.SelectedMapProvider = TileMapProviderFactory.GoogleRoadMap;    // 4. base map
```

```csharp
public class HelloMapViewModel : MapViewModelBase { }
```

## How to run

```bash
dotnet run --project samples/IRI.Maptor.Samples.Wpf.HelloMap
```

Next step: the [WPF gallery](../IRI.Maptor.Samples.Wpf.Gallery/README.md) adds one feature per page
on top of exactly this skeleton.

---
[Back to the samples index](../README.md)
