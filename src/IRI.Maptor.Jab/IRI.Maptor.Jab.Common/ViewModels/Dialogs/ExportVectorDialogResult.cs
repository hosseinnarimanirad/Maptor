using IRI.Maptor.Sta.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.ViewModels.Dialogs; 

/// <summary>
/// Result of the CSV/TSV open dialog.
/// </summary>
/// <param name="FilePath">File path when a file was selected, or null when data was pasted.</param>
/// <param name="RawText">File content or pasted text to import.</param>
/// <param name="SelectedSrid">Selected spatial reference system ID.</param>
/// <param name="IsCsv">True for CSV (comma-delimited), false for TSV (tab-delimited).</param>
/// <param name="IsLongitudeFirst">True for X,Y (longitude,latitude) order, false for Y,X.</param>
/// <param name="UseFirstLineAsHeader">True if first line contains attribute names.</param>
public record ExportVectorDialogResult(
    string? FilePath, 
    int SelectedSrid, 
    bool IsLongitudeFirst);
