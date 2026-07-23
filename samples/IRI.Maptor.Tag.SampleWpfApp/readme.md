# Maptor sample WPF application

<img width="884" height="592" alt="image" src="https://github.com/user-attachments/assets/94218afd-f706-4cc8-b819-73f260d2b147" />

A demonstration of building a functional GIS application with minimal code using the Maptor
spatial library: a `MapViewer` with tile basemaps (selectable provider), measurement tools
(length and area), drawing tools (point, polyline, polygon, text), go-to navigation, layer and
drawing legends, shapefile loading, an attribute table, a scalebar, a coordinate panel, and
RTL-aware language switching.

## How to run

Prerequisites: .NET 8 SDK on Windows (WPF); Visual Studio 2022 recommended.

1. Clone the repository:

   ```bash
   git clone https://github.com/hosseinnarimanirad/Maptor.git
   ```

2. Open the root solution `IRI.Maptor.sln` and set `IRI.Maptor.Tag.SampleWpfApp` as the startup
   project, or run the project directly:

   ```powershell
   dotnet run --project samples\IRI.Maptor.Tag.SampleWpfApp
   ```

---
[Back to the solution README](../../README.md)
