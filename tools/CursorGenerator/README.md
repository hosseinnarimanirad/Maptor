# CursorGenerator

Writes the map cursors in
`src/Presentation/IRI.Maptor.Presentation.Wpf/Assets/Cursors/MaptorCursorSet`.

```
dotnet run --project tools/CursorGenerator
dotnet run --project tools/CursorGenerator -- <output folder> --preview <sheet.png>
```

With no folder it overwrites the files in the library; pass a folder to try a change elsewhere first.
`--preview` also writes a PNG contact sheet: each cursor at 32 and 48 px on a light, a mid and a dark
ground, its hotspot marked in red, plus the 32 px image at 2x.

**Edit this tool, not the .cur files.** Each cursor is drawn from vectors on a 32-unit grid and saved
as one .cur holding 32, 40, 48 and 64 px images (100, 125, 150 and 200 % scaling). `MapCursorHelper.Load`
opens them with `scaleWithDpi: true`, so Windows picks the image that matches the screen.

| File | Shown for |
|---|---|
| `Pan.cur` | Pan, the default tool (open hand) |
| `PanGrabbing.cur` | while the map is dragged in Pan mode (`MapExtraCursor.PanDragging`) |
| `ZoomIn.cur` / `ZoomOut.cur` | Zoom in (and zoom-in rectangle) / Zoom out |
| `information.cur` | Identify (arrow + i) |
| `DrawPoint.cur` | Draw point, and any one-off "pick a point on the map" |
| `DrawPolyline.cur`, `DrawPolygon.cur`, `DrawRectangle.cur` | the matching draw tools |
| `DrawText.cur` | Add text |
| `MeasureLength.cur` / `MeasureArea.cur` | Measure (`MapExtraCursor.MeasureLength` / `MeasureArea`) |

Style: black ink with a white outline, so the cursors read on light and dark maps alike. The two
hands and the Identify arrow follow the Windows pointer look instead (white body, black outline). The
crosshair is built in whole pixels so its 1 px lines stay sharp, and each draw/measure tool adds a
small badge in the lower-right corner.

The hand shapes come from [Phosphor Icons](https://github.com/phosphor-icons/core) (MIT), read from the
`MahApps.Metro.IconPacks.PhosphorIcons` package the library already uses.

A new file must also be added to `IRI.Maptor.Presentation.Wpf.csproj` (both the `None Remove` and the
`Resource` list). This tool is deliberately not in `IRI.Maptor.sln`.
