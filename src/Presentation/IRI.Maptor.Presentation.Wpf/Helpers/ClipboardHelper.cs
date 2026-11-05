using System;
using System.IO;
using System.Windows.Threading;

using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

public static class ClipboardHelper
{

    public static void CopyToClipboard(Point webMercator, CoordinateDisplayMode mode, CopyCoordinateOptions options, bool? isLatitudeFirst = null)
    {
        var format = CoordinateHelper.Format(webMercator, mode, options); 

        if ((isLatitudeFirst ?? true) && (mode == CoordinateDisplayMode.GeodeticDms || mode == CoordinateDisplayMode.GeodeticDecimal))
        {
            System.Windows.Clipboard.SetDataObject($"{format.y},{format.x}");
        }
        else
        {
            System.Windows.Clipboard.SetDataObject($"{format.x},{format.y}");
        }
    }

    public static void CopyText(string text) => System.Windows.Clipboard.SetText(text);

    /// <summary>
    /// Copies a secret (a generated password) to the clipboard: kept out of Windows clipboard history and
    /// cloud clipboard sync, and cleared after <paramref name="clearAfter"/> if the clipboard still holds it
    /// (anything copied since is left alone). False when the clipboard is locked by another process.
    /// </summary>
    public static bool CopySensitiveText(string text, TimeSpan clearAfter)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        var data = new System.Windows.DataObject();

        data.SetText(text);

        // the formats Windows documents for this (Clipboard: "Cloud Clipboard and Clipboard History Formats")
        data.SetData("ExcludeClipboardContentFromMonitorProcessing", new MemoryStream(new byte[4]));
        data.SetData("CanIncludeInClipboardHistory", new MemoryStream(BitConverter.GetBytes(0)));
        data.SetData("CanUploadToCloudClipboard", new MemoryStream(BitConverter.GetBytes(0)));

        try
        {
            System.Windows.Clipboard.SetDataObject(data, copy: true);
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            return false;
        }

        // a running DispatcherTimer is kept alive by its dispatcher, so this outlives a closed dialog
        var timer = new DispatcherTimer { Interval = clearAfter };

        timer.Tick += (_, _) =>
        {
            timer.Stop();

            try
            {
                if (System.Windows.Clipboard.ContainsText() && System.Windows.Clipboard.GetText() == text)
                    System.Windows.Clipboard.Clear();
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                // clipboard locked right now: leave it
            }
        };

        timer.Start();

        return true;
    }
}
