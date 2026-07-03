using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using IRI.Maptor.Jab.Common.Assets.Fonts;
using IRI.Maptor.Jab.Common.Models.Print;
using IRI.Maptor.Sta.Pdf;

namespace IRI.Maptor.Jab.Common.Helpers;

/// <summary>
/// Builds <see cref="PdfMapDecorations"/> from the print dialog options. The title is
/// pre-rendered to PNG with WPF (PdfSharpCore has no bidi shaping, and titles are often
/// Persian); graticule/scale-bar labels stay real PDF text via the embedded label font.
/// </summary>
public static class PdfDecorationHelper
{
    private const string MaptorLogoPackUri = "pack://application:,,,/IRI.Maptor.Jab.Common;component/Assets/Images/Logos/maptor.png";
    private const string LabelFontPackUri = "pack://application:,,,/IRI.Maptor.Jab.Common;component/Assets/Fonts/IRANSans.ttf";

    private const double RenderDpi = 192;

    public static PdfMapDecorations BuildDecorations(PrintToPdfDialogOptions options)
    {
        var decorations = new PdfMapDecorations
        {
            ShowScaleBar = options.ShowScaleBar,
            ShowGraticule = options.ShowGraticule,
            LabelFontBytes = LoadLabelFontBytes(),
        };

        if (!string.IsNullOrWhiteSpace(options.MapTitle))
            decorations.TitlePngBytes = RenderTextToPng(options.MapTitle, 48);

        if (options.ShowMaptorLogo)
            decorations.PrimaryLogoPngBytes = LoadMaptorLogoBytes();

        if (options.ShowCompanyLogo && !string.IsNullOrWhiteSpace(options.CompanyLogoPath))
            decorations.SecondaryLogoPngBytes = TryReadAllBytes(options.CompanyLogoPath);

        return decorations;
    }

    /// <summary>
    /// Renders text to a tightly-cropped transparent PNG. WPF does the script shaping,
    /// so RTL/Persian titles come out correct even though the PDF gets a raster.
    /// </summary>
    public static byte[]? RenderTextToPng(string text, double fontSizePx, FontFamily? fontFamily = null)
    {
        try
        {
            var flowDirection = HasRtlCharacters(text) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            var typeface = new Typeface(fontFamily ?? IriFonts.IranSans, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

            var formatted = new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                flowDirection,
                typeface,
                fontSizePx,
                Brushes.Black,
                RenderDpi / 96.0);

            var width = (int)Math.Ceiling(formatted.WidthIncludingTrailingWhitespace) + 2;
            var height = (int)Math.Ceiling(formatted.Height) + 2;

            if (width <= 2 || height <= 2)
                return null;

            var visual = new DrawingVisual();

            using (var context = visual.RenderOpen())
            {
                context.DrawText(formatted, new System.Windows.Point(flowDirection == FlowDirection.RightToLeft ? width - 1 : 1, 1));
            }

            var bitmap = new RenderTargetBitmap(
                (int)Math.Ceiling(width * RenderDpi / 96.0),
                (int)Math.Ceiling(height * RenderDpi / 96.0),
                RenderDpi,
                RenderDpi,
                PixelFormats.Pbgra32);

            bitmap.Render(visual);
            bitmap.Freeze();

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PdfDecorationHelper.RenderTextToPng failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// The Maptor icon resource, or the rendered "Maptor" wordmark when no artwork is shipped
    /// </summary>
    public static byte[]? LoadMaptorLogoBytes()
        => LoadPackResourceBytes(MaptorLogoPackUri) ?? RenderTextToPng("Maptor", 32);

    public static byte[]? LoadLabelFontBytes() => LoadPackResourceBytes(LabelFontPackUri);

    public static byte[]? LoadPackResourceBytes(string packUri)
    {
        try
        {
            var streamInfo = Application.GetResourceStream(new Uri(packUri, UriKind.Absolute));

            if (streamInfo == null)
                return null;

            using var stream = streamInfo.Stream;
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static byte[]? TryReadAllBytes(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool HasRtlCharacters(string text)
        => text.Any(c => (c >= 0x0590 && c <= 0x08FF) || (c >= 0xFB1D && c <= 0xFDFF) || (c >= 0xFE70 && c <= 0xFEFF));
}