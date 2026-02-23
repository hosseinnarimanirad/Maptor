namespace IRI.Maptor.Jab.Common.ViewModels.Dialogs;

/// <summary>
/// Result of the DXF open dialog when user confirms.
/// </summary>
public record DxfOpenDialogResult(string FilePath, int SelectedSrid);
