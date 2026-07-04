using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using IRI.Maptor.Jab.Common.Assets.Fonts;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.Models.Print;
using IRI.Maptor.Jab.Core.Localization;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Pdf;

using StaPoint = IRI.Maptor.Sta.Common.Primitives.Point;

namespace IRI.Maptor.Jab.Common.Helpers;

/// <summary>
/// Builds <see cref="PdfMapDecorations"/> from the print dialog options. The title is
/// pre-rendered to PNG with WPF (PdfSharpCore has no bidi shaping, and titles are often
/// Persian); graticule/scale-bar labels stay real PDF text via the embedded label font.
/// </summary>
public static class PdfDecorationHelper
{
    private const string ShapesPackUri = "pack://application:,,,/IRI.Maptor.Jab.Common;component/Assets/Shapes/IriShapes.xaml";
    private const string LabelFontPackUri = "pack://application:,,,/IRI.Maptor.Jab.Common;component/Assets/Fonts/IRANSans.ttf";

    private const double RenderDpi = 192;

    // Screen px (1/96") -> PDF points (1/72"): keep printed markers at their on-screen physical size.
    private const double PxToPoint = 72.0 / 96.0;

    public static PdfMapDecorations BuildDecorations(PrintToPdfDialogOptions options)
    {
        // Follow the app's current culture: RTL mirrors the sheet, Persian localizes numbers.
        var isRtl = LocalizationManager.Instance.IsRightToLeft;
        var isPersian = LocalizationManager.Instance.IsPersian;

        var decorations = new PdfMapDecorations
        {
            ShowScaleBar = options.ShowScaleBar,
            ShowGraticule = options.ShowGraticule,
            LabelFontBytes = LoadLabelFontBytes(),
            // The legend column is reserved (bordered box + header) even though legend
            // items are not drawn yet, so the three-column sheet keeps its standard shape.
            ShowLegendColumn = true,
            RightToLeft = isRtl,
            UsePersianDigits = isPersian,
        };

        // All decoration text is drawn as filled vector glyph outlines (crisp, resolution-
        // independent, RTL-safe) with a raster fallback if geometry extraction fails.
        var legendCaption = LocalizationManager.Instance["dialog_printPdf_legend"];
        decorations.LegendHeaderVector = RenderTextToVector(legendCaption, 22);
        if (decorations.LegendHeaderVector == null)
            decorations.LegendHeaderPngBytes = RenderTextToPng(legendCaption, 22);

        // Export stamp for the bottom cell: Jalali (Shamsi) date with Persian digits in Persian,
        // Gregorian otherwise. Rendered forced-LTR because a date/time is a numeric run.
        var dateText = isPersian
            ? $"{DateTime.Now.ToPersianDate()} {DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture).LatinNumbersToFarsiNumbers()}"
            : DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        decorations.DateTimeText = dateText;
        decorations.DateTimeVector = RenderTextToVector(dateText, 18, forceRightToLeft: false);

        // Map title sits in a small bordered box atop the map.
        if (!string.IsNullOrWhiteSpace(options.MapTitle))
        {
            decorations.TitleVector = RenderTextToVector(options.MapTitle, 30);
            if (decorations.TitleVector == null)
                decorations.TitlePngBytes = RenderTextToPng(options.MapTitle, 30);
        }

        if (!string.IsNullOrWhiteSpace(options.CompanyTitle))
        {
            decorations.CompanyTitleVector = RenderTextToVector(options.CompanyTitle, 22);
            if (decorations.CompanyTitleVector == null)
                decorations.CompanyTitlePngBytes = RenderTextToPng(options.CompanyTitle, 22);
        }

        if (!string.IsNullOrWhiteSpace(options.CompanySubtitle))
        {
            decorations.CompanySubtitleVector = RenderTextToVector(options.CompanySubtitle, 18);
            if (decorations.CompanySubtitleVector == null)
                decorations.CompanySubtitlePngBytes = RenderTextToPng(options.CompanySubtitle, 18);
        }

        if (options.ShowMaptorLogo)
        {
            // Prefer crisp vector; fall back to a gray raster render if flattening fails.
            decorations.PrimaryVectorLogo = BuildMakanNegarVectorLogo();

            if (decorations.PrimaryVectorLogo == null)
                decorations.PrimaryLogoPngBytes = LoadMaptorLogoBytes();
        }

        if (options.ShowCompanyLogo && !string.IsNullOrWhiteSpace(options.CompanyLogoPath))
            decorations.SecondaryLogoPngBytes = LoadNormalizedPng(options.CompanyLogoPath);

        return decorations;
    }

    // Company logos only occupy a ~64pt box, so we downscale well below source size.
    private const int MaxLogoPixels = 128;

    /// <summary>
    /// Loads an arbitrary user image file and re-encodes it as a small, fully <b>opaque</b>
    /// (white-composited) 24-bit PNG with no alpha channel. PdfSharpCore 1.3.0 renders only the
    /// top portion of any image that carries a soft mask (SMask) — regardless of size — so we
    /// flatten transparency onto white by direct pixel math (no <see cref="System.Windows.Media.DrawingVisual"/>,
    /// which rendered blank in an earlier attempt) and emit a truecolor PNG that carries no SMask.
    /// </summary>
    public static byte[]? LoadNormalizedPng(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            var decoded = new BitmapImage();
            decoded.BeginInit();
            decoded.CacheOption = BitmapCacheOption.OnLoad;
            decoded.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            decoded.UriSource = new Uri(path, UriKind.Absolute);
            decoded.EndInit();
            decoded.Freeze();

            BitmapSource source = decoded;

            var longestSide = Math.Max(decoded.PixelWidth, decoded.PixelHeight);
            if (longestSide > MaxLogoPixels)
            {
                var scale = (double)MaxLogoPixels / longestSide;
                var reduced = new TransformedBitmap(decoded, new ScaleTransform(scale, scale));
                reduced.Freeze();
                source = reduced;
            }

            // Read straight (non-premultiplied) BGRA pixels, then composite each over white into a
            // BGR24 buffer, dropping alpha entirely so the encoded PNG carries no soft mask.
            var bgra = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            bgra.Freeze();

            int width = bgra.PixelWidth;
            int height = bgra.PixelHeight;
            int srcStride = width * 4;
            var src = new byte[srcStride * height];
            bgra.CopyPixels(src, srcStride, 0);

            int dstStride = width * 3;
            var dst = new byte[dstStride * height];

            for (int y = 0; y < height; y++)
            {
                int srcRow = y * srcStride;
                int dstRow = y * dstStride;
                for (int x = 0; x < width; x++)
                {
                    int s = srcRow + x * 4;
                    int d = dstRow + x * 3;
                    int a = src[s + 3];
                    int inv = 255 - a;
                    // out = channel*a/255 + white(255)*(255-a)/255
                    dst[d] = (byte)((src[s] * a + 255 * inv) / 255);       // B
                    dst[d + 1] = (byte)((src[s + 1] * a + 255 * inv) / 255); // G
                    dst[d + 2] = (byte)((src[s + 2] * a + 255 * inv) / 255); // R
                }
            }

            var opaque = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr24, null, dst, dstStride);
            opaque.Freeze();

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(opaque));

            using var memory = new MemoryStream();
            encoder.Save(memory);
            return memory.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PdfDecorationHelper.LoadNormalizedPng failed: {ex.Message}");
            return TryReadAllBytes(path);
        }
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
    /// Renders text to filled vector glyph outlines as a <see cref="PdfVectorLogo"/>, so the PDF
    /// draws crisp resolution-independent text (no raster blur). WPF shapes the run — RTL/Persian
    /// comes out correct — and we flatten the fill geometry to polygon figures (same technique as
    /// <see cref="BuildMakanNegarVectorLogo"/>). Returns null if the text yields no geometry.
    /// </summary>
    public static PdfVectorLogo? RenderTextToVector(string text, double fontSizePx, FontFamily? fontFamily = null, bool? forceRightToLeft = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var isRtl = forceRightToLeft ?? HasRtlCharacters(text);
            var flowDirection = isRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            var typeface = new Typeface(fontFamily ?? IriFonts.IranSans, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

            var formatted = new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                flowDirection,
                typeface,
                fontSizePx,
                Brushes.Black,
                1.0);

            var geometry = formatted.BuildGeometry(new System.Windows.Point(0, 0));
            var flattened = geometry.GetFlattenedPathGeometry();

            if (flattened.Figures.Count == 0)
                return null;

            var bounds = flattened.Bounds;

            if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
                return null;

            var figures = new List<PdfMarkerFigure>();

            foreach (var figure in flattened.Figures)
            {
                var wpfPoints = new List<System.Windows.Point> { figure.StartPoint };

                foreach (var segment in figure.Segments)
                {
                    if (segment is PolyLineSegment poly)
                        wpfPoints.AddRange(poly.Points);
                    else if (segment is LineSegment line)
                        wpfPoints.Add(line.Point);
                }

                if (wpfPoints.Count < 2)
                    continue;

                // Normalize to the text block's own top-left origin (0..W / 0..H).
                figures.Add(new PdfMarkerFigure
                {
                    IsClosed = figure.IsClosed,
                    IsFilled = figure.IsFilled,
                    Points = wpfPoints.Select(p => new StaPoint(p.X - bounds.Left, p.Y - bounds.Top)).ToList(),
                });
            }

            return figures.Count > 0
                ? new PdfVectorLogo { Figures = figures, SourceWidth = bounds.Width, SourceHeight = bounds.Height }
                : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PdfDecorationHelper.RenderTextToVector failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// The Maptor producer brand mark: the <c>makanNegar</c> vector logo rendered gray to PNG
    /// (a subtle "powered by Maptor" note, not a headline element).
    /// </summary>
    public static byte[]? LoadMaptorLogoBytes()
    {
        var geometry = LoadShapeGeometry("makanNegar");

        return geometry != null
            ? RenderGeometryToPng(geometry, Brushes.Gray)
            : RenderTextToPng("Maptor", 32);
    }

    /// <summary>
    /// The Maptor producer brand mark as vector outlines (flattened from the <c>makanNegar</c>
    /// geometry), so it prints crisp. Returns null if the shape can't be loaded/flattened.
    /// </summary>
    public static PdfVectorLogo? BuildMakanNegarVectorLogo()
    {
        var geometry = LoadShapeGeometry("makanNegar");

        if (geometry == null)
            return null;

        var flattened = geometry.GetFlattenedPathGeometry();

        if (flattened.Figures.Count == 0)
            return null;

        var bounds = geometry.GetRenderBounds(new Pen(Brushes.Black, 0));

        if (bounds.Width <= 0 || bounds.Height <= 0)
            return null;

        var figures = new List<PdfMarkerFigure>();

        foreach (var figure in flattened.Figures)
        {
            var wpfPoints = new List<System.Windows.Point> { figure.StartPoint };

            foreach (var segment in figure.Segments)
            {
                if (segment is PolyLineSegment poly)
                    wpfPoints.AddRange(poly.Points);
                else if (segment is LineSegment line)
                    wpfPoints.Add(line.Point);
            }

            if (wpfPoints.Count < 2)
                continue;

            // Normalize to the logo's own top-left origin (source coords 0..W / 0..H).
            figures.Add(new PdfMarkerFigure
            {
                IsClosed = figure.IsClosed,
                IsFilled = figure.IsFilled,
                Points = wpfPoints.Select(p => new StaPoint(p.X - bounds.Left, p.Y - bounds.Top)).ToList(),
            });
        }

        return figures.Count > 0
            ? new PdfVectorLogo { Figures = figures, SourceWidth = bounds.Width, SourceHeight = bounds.Height }
            : null;
    }

    public static byte[]? LoadLabelFontBytes() => LoadPackResourceBytes(LabelFontPackUri);

    /// <summary>
    /// Loads a named vector shape from the shared IriShapes resource dictionary.
    /// </summary>
    public static Geometry? LoadShapeGeometry(string resourceKey)
    {
        try
        {
            var dictionary = new ResourceDictionary { Source = new Uri(ShapesPackUri, UriKind.Absolute) };
            return dictionary[resourceKey] as Geometry;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PdfDecorationHelper.LoadShapeGeometry({resourceKey}) failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Renders a WPF geometry to a tightly-cropped transparent PNG at the given fill/stroke.
    /// </summary>
    public static byte[]? RenderGeometryToPng(Geometry geometry, Brush fill, Pen? stroke = null, int targetHeightPx = 128)
    {
        try
        {
            var bounds = geometry.GetRenderBounds(stroke ?? new Pen(Brushes.Black, 0));

            if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
                return null;

            var scale = targetHeightPx / bounds.Height;
            var pixelWidth = (int)Math.Ceiling(bounds.Width * scale);
            var pixelHeight = (int)Math.Ceiling(bounds.Height * scale);

            var visual = new DrawingVisual();

            using (var context = visual.RenderOpen())
            {
                context.PushTransform(new ScaleTransform(scale, scale));
                context.PushTransform(new TranslateTransform(-bounds.X, -bounds.Y));
                context.DrawGeometry(fill, stroke, geometry);
                context.Pop();
                context.Pop();
            }

            var bitmap = new RenderTargetBitmap(Math.Max(1, pixelWidth), Math.Max(1, pixelHeight), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var memory = new MemoryStream();
            encoder.Save(memory);
            return memory.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PdfDecorationHelper.RenderGeometryToPng failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Extracts a reusable point-marker from a symbolizer's point symbol so the PDF stamps the
    /// same shape shown on screen: a vector template from <c>GeometrySymbol</c> (preferred) or a
    /// raster stamp from <c>ImageSymbol</c>. Returns null to fall back to a circle.
    /// </summary>
    public static PdfPointMarker? BuildPointMarker(SimplePointSymbolizer? pointSymbol)
    {
        if (pointSymbol == null)
            return null;

        if (pointSymbol.GeometrySymbol != null)
            return BuildVectorMarker(pointSymbol);

        if (pointSymbol.ImageSymbol is BitmapSource bitmap)
            return BuildImageMarker(pointSymbol, bitmap);

        return null;
    }

    private static PdfPointMarker? BuildVectorMarker(SimplePointSymbolizer pointSymbol)
    {
        var symbol = pointSymbol.GeometrySymbol!;

        var flattened = symbol.GetFlattenedPathGeometry();

        if (flattened.Figures.Count == 0)
            return null;

        // Match the on-screen centering (DrawingVisualRenderStrategy.AddPoint): the marker is
        // offset by the render-bounds min and the symbol size, then drawn at the point location.
        var bounds = symbol.GetRenderBounds(new Pen(Brushes.Black, 0));
        var offsetX = bounds.Left + pointSymbol.SymbolWidth / 2.0;
        var offsetY = bounds.Bottom - pointSymbol.SymbolHeight / 2.0;

        var figures = new List<PdfMarkerFigure>();

        foreach (var figure in flattened.Figures)
        {
            var wpfPoints = new List<System.Windows.Point> { figure.StartPoint };

            foreach (var segment in figure.Segments)
            {
                if (segment is PolyLineSegment poly)
                    wpfPoints.AddRange(poly.Points);
                else if (segment is LineSegment line)
                    wpfPoints.Add(line.Point);
            }

            if (wpfPoints.Count < 2)
                continue;

            figures.Add(new PdfMarkerFigure
            {
                IsClosed = figure.IsClosed,
                IsFilled = figure.IsFilled,
                Points = wpfPoints
                    .Select(p => new StaPoint((p.X - offsetX) * PxToPoint, (p.Y - offsetY) * PxToPoint))
                    .ToList(),
            });
        }

        return figures.Count > 0 ? new PdfPointMarker { Figures = figures } : null;
    }

    private static PdfPointMarker? BuildImageMarker(SimplePointSymbolizer pointSymbol, BitmapSource bitmap)
    {
        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var memory = new MemoryStream();
            encoder.Save(memory);

            return new PdfPointMarker
            {
                ImagePngBytes = memory.ToArray(),
                Width = pointSymbol.SymbolWidth * PxToPoint,
                Height = pointSymbol.SymbolHeight * PxToPoint,
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PdfDecorationHelper.BuildImageMarker failed: {ex.Message}");
            return null;
        }
    }

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