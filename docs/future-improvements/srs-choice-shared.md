# One coordinate-system choice for every import dialog

| | |
|---|---|
| Found | 2026-11-27, while building the file geodatabase picker |
| Status | **Not done.** The shared piece exists; four dialogs still carry their own copy. |
| Where | `src/Presentation/IRI.Maptor.Presentation.Wpf` |

## Today

The choice "WGS84 / UTM zone (north or south) / Web Mercator → SRID" is written five times:

| Where | Notes |
|---|---|
| `ViewModels/Dialogs/SrsChoiceViewModel.cs` + `Views/Dialogs/SrsChoiceView.xaml` | The shared piece, used by the file geodatabase picker. |
| `ViewModels/Dialogs/ImportOptionsDialogViewModel.cs` | Also adds a "from the file" option, locks the choice for KML/DXF/Esri JSON, and refreshes a preview on change. |
| `ViewModels/Dialogs/CsvTsvOpenDialogViewModel.cs` | |
| `ViewModels/Dialogs/DxfOpenDialogViewModel.cs` | |
| `ViewModels/Dialogs/GeoJsonTopoJsonOpenDialogViewModel.cs` | |

The four older copies have the same options, the same zone clamp (1–60, default 39 north) and the same SRID rule. Their
XAML repeats the CRS and UTM-zone cards.

## Proposal

1. Give `SrsChoiceViewModel` what the import options dialog needs: an optional extra option ("from the file", with
   its SRID), `CanUserChoose`, and `SelectSrid(int)` (the dialog's `SelectSrsInternally`).
2. Move each dialog onto it, one per step, with its XAML switched to `SrsChoiceView`, and re-run its tests
   (`ImportOptionsDialogViewModelTest`, `ImportOptionsDialogViewTest`, …).
3. Check whether `CsvTsvOpenDialog`, `DxfOpenDialog` and `GeoJsonTopoJsonOpenDialog` are still used at all since the
   unified import: if not, delete them instead.
