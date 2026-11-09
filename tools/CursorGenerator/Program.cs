using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.IconPacks;

namespace CursorGenerator;

// Writes the Maptor map cursors as multi-size .cur files. Every cursor is drawn from vectors on a
// 32-unit design grid and rendered at each size in Sizes, so Windows can pick the frame that matches
// the screen DPI (the WPF side loads them with scaleWithDpi: true).
//
// Style: black ink with a 1-unit white outline, so a cursor reads on light and dark maps alike. The
// two hands and the Identify arrow follow the Windows pointer convention instead (white body, black
// outline), like the Windows Hand and Arrow they replace or stand next to.
internal static class Program
{
    // 100 %, 125 %, 150 % and 200 % of the 32 px Windows cursor
    private static readonly int[] Sizes = { 32, 40, 48, 64 };

    private const string CursorFolder = "src/Presentation/IRI.Maptor.Presentation.Wpf/Assets/Cursors/MaptorCursorSet";

    [STAThread]
    private static int Main(string[] args)
    {
        string? preview = null;
        string? outDir = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--preview" && i + 1 < args.Length)
                preview = args[++i];
            else
                outDir = args[i];
        }

        outDir ??= Path.Combine(FindRepositoryRoot(), CursorFolder);

        if (!Directory.Exists(outDir))
        {
            Console.Error.WriteLine($"Output folder not found: {outDir}");
            return 1;
        }

        var cursors = CursorSet.All();

        foreach (var cursor in cursors)
        {
            var path = Path.Combine(outDir, cursor.FileName);

            File.WriteAllBytes(path, CurWriter.Write(Sizes.Select(cursor.Render).ToList()));

            Console.WriteLine($"{cursor.FileName,-20} {cursor.Description}");
        }

        if (preview != null)
        {
            PreviewSheet.Write(cursors, preview);
            Console.WriteLine($"preview: {preview}");
        }

        return 0;
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IRI.Maptor.sln")))
            dir = dir.Parent;

        return dir?.FullName ?? throw new InvalidOperationException("IRI.Maptor.sln not found above the tool's folder.");
    }
}

/// <summary>One rendered size of a cursor: straight-alpha BGRA pixels, top-down, plus the hotspot.</summary>
internal sealed record CursorFrame(int Size, byte[] Bgra, int HotX, int HotY);

/// <summary>A drawing step in pixel space: fill and/or stroke one geometry.</summary>
internal sealed record Op(Geometry Geometry, Brush? Fill, Pen? Pen);

internal sealed class CursorSpec
{
    public CursorSpec(string fileName, string description, Func<int, (List<Op> Ops, int HotX, int HotY)> build)
    {
        FileName = fileName;
        Description = description;
        _build = build;
    }

    private readonly Func<int, (List<Op> Ops, int HotX, int HotY)> _build;

    public string FileName { get; }

    public string Description { get; }

    public CursorFrame Render(int size)
    {
        var (ops, hotX, hotY) = _build(size);

        var visual = new DrawingVisual();

        using (var dc = visual.RenderOpen())
        {
            foreach (var op in ops)
                dc.DrawGeometry(op.Fill, op.Pen, op.Geometry);
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var pixels = new byte[size * size * 4];
        bitmap.CopyPixels(pixels, size * 4, 0);

        // Pbgra32 is premultiplied; a cursor DIB wants straight alpha
        for (int i = 0; i < pixels.Length; i += 4)
        {
            int a = pixels[i + 3];

            if (a == 0 || a == 255)
                continue;

            for (int c = 0; c < 3; c++)
                pixels[i + c] = (byte)Math.Min(255, (pixels[i + c] * 255 + a / 2) / a);
        }

        return new CursorFrame(size, pixels, Math.Clamp(hotX, 0, size - 1), Math.Clamp(hotY, 0, size - 1));
    }
}

/// <summary>Builds the drawing steps. Design units are a 32-unit grid; s = size / 32.</summary>
internal static class Ink
{
    public static readonly Brush Black = Freeze(new SolidColorBrush(Colors.Black));
    public static readonly Brush White = Freeze(new SolidColorBrush(Colors.White));

    /// <summary>Width of the white outline around black ink, in design units, on each side.</summary>
    public const double Halo = 1.0;

    private static T Freeze<T>(T f) where T : Freezable { f.Freeze(); return f; }

    public static Geometry Scaled(Geometry g, double s)
    {
        var copy = g.Clone();
        copy.Transform = new ScaleTransform(s, s);
        copy.Freeze();
        return copy;
    }

    public static Pen Pen(Brush brush, double thickness, PenLineCap cap = PenLineCap.Round)
        => Freeze(new Pen(brush, thickness) { StartLineCap = cap, EndLineCap = cap, LineJoin = PenLineJoin.Round });

    /// <summary>A black stroke (design units) with its white outline, as two ops.</summary>
    public static (Op Halo, Op Ink) Stroke(Geometry design, double width, double s, PenLineCap cap = PenLineCap.Round)
    {
        var g = Scaled(design, s);
        return (new Op(g, null, Pen(White, (width + 2 * Halo) * s, PenLineCap.Round)),
                new Op(g, null, Pen(Black, width * s, cap)));
    }

    /// <summary>A black fill (design units) with its white outline, as two ops.</summary>
    public static (Op Halo, Op Ink) Fill(Geometry design, double s)
    {
        var g = Scaled(design, s);
        return (new Op(g, White, Pen(White, 2 * Halo * s)), new Op(g, Black, null));
    }

    /// <summary>All halos first, then all ink, so no outline paints over another part's ink.</summary>
    public static List<Op> Compose(params (Op Halo, Op Ink)[] parts)
        => parts.Select(p => p.Halo).Concat(parts.Select(p => p.Ink)).ToList();

    public static Geometry Line(double x1, double y1, double x2, double y2)
        => new LineGeometry(new Point(x1, y1), new Point(x2, y2));

    public static Geometry Poly(bool closed, params double[] xy)
    {
        var figure = new PathFigure { StartPoint = new Point(xy[0], xy[1]), IsClosed = closed, IsFilled = closed };

        for (int i = 2; i < xy.Length; i += 2)
            figure.Segments.Add(new LineSegment(new Point(xy[i], xy[i + 1]), true));

        return new PathGeometry(new[] { figure });
    }

    public static Geometry Rect(double x1, double y1, double x2, double y2)
        => new RectangleGeometry(new Rect(new Point(x1, y1), new Point(x2, y2)));

    public static Geometry Circle(double cx, double cy, double r)
        => new EllipseGeometry(new Point(cx, cy), r, r);

    public static Geometry Group(params Geometry[] items)
    {
        var group = new GeometryGroup { FillRule = FillRule.Nonzero };
        foreach (var item in items)
            group.Children.Add(item);
        return group;
    }
}

internal static class CursorSet
{
    public static List<CursorSpec> All() => new()
    {
        // file names of the four original cursors are kept: MapCursorHelper.Load is public
        new("Pan.cur", "open hand: Pan (the default map tool)", s => Hand(s, grabbing: false)),
        new("PanGrabbing.cur", "closed hand: while the map is being dragged", s => Hand(s, grabbing: true)),
        new("ZoomIn.cur", "magnifier +: Zoom in / Zoom in rectangle", s => Magnifier(s, plus: true)),
        new("ZoomOut.cur", "magnifier -: Zoom out", s => Magnifier(s, plus: false)),
        new("information.cur", "arrow + (i): Identify", Identify),
        new("DrawPoint.cur", "crosshair + dot: Draw point, pick a point", s => Crosshair(s, PointBadge)),
        new("DrawPolyline.cur", "crosshair + line: Draw polyline", s => Crosshair(s, PolylineBadge)),
        new("DrawPolygon.cur", "crosshair + polygon: Draw polygon", s => Crosshair(s, PolygonBadge)),
        new("DrawRectangle.cur", "crosshair + rectangle: Draw rectangle", s => Crosshair(s, RectangleBadge)),
        new("DrawText.cur", "crosshair + T: Add text", s => Crosshair(s, TextBadge)),
        new("MeasureLength.cur", "crosshair + ruler: Measure length", s => Crosshair(s, RulerBadge)),
        new("MeasureArea.cur", "crosshair + grid square: Measure area", s => Crosshair(s, AreaBadge)),
    };

    // ── Crosshair ────────────────────────────────────────────────────────────
    // Built in whole pixels so the 1 px lines stay sharp at every size. The four arms leave a gap
    // around the hotspot so the clicked spot stays visible; the badge sits in the free lower-right
    // quadrant.

    private static (List<Op>, int, int) Crosshair(int size, Func<double, List<Op>> badge)
    {
        double s = size / 32.0;

        int lw = Math.Max(1, (int)Math.Round(s));          // line width, px
        int halo = Math.Max(1, (int)Math.Round(s * Ink.Halo));
        int c0 = (int)Math.Round(15.5 * s - lw / 2.0);    // first pixel of the line
        int hot = c0 + (lw - 1) / 2;

        int near = (int)Math.Round(4.5 * s);              // gap: distance from the line centre
        int far = (int)Math.Round(12.5 * s);              // arm length from the line centre
        double mid = c0 + lw / 2.0;

        var arms = new List<Rect>
        {
            new(mid - far, c0, far - near, lw),           // left
            new(mid + near, c0, far - near, lw),          // right
            new(c0, mid - far, lw, far - near),           // top
            new(c0, mid + near, lw, far - near),          // bottom
        };

        var ops = new List<Op>();

        // halos of the arms and the badge first, then ink
        foreach (var arm in arms)
        {
            var outline = arm;
            outline.Inflate(halo, halo);
            ops.Add(new Op(Frozen(new RectangleGeometry(outline)), Ink.White, null));
        }

        var badgeOps = badge(s);
        int badgeHalos = badgeOps.Count / 2;

        ops.AddRange(badgeOps.Take(badgeHalos));

        foreach (var arm in arms)
            ops.Add(new Op(Frozen(new RectangleGeometry(arm)), Ink.Black, null));

        ops.AddRange(badgeOps.Skip(badgeHalos));

        return (ops, hot, hot);
    }

    private static Geometry Frozen(Geometry g) { g.Freeze(); return g; }

    // Badges live in the lower-right box (19.5..31) of the design grid. Each returns halos then ink,
    // the same number of each (Crosshair splits the list in half).

    private static List<Op> PointBadge(double s)
        => Ink.Compose(Ink.Fill(Ink.Circle(25.5, 25.5, 3.4), s));

    private static List<Op> PolylineBadge(double s)
        => Ink.Compose(Ink.Stroke(Ink.Poly(false, 20.5, 30, 23.5, 22.5, 27, 27.5, 30.5, 20.5), 1.6, s));

    private static List<Op> PolygonBadge(double s)
        => Ink.Compose(Ink.Stroke(Ink.Poly(true, 20.5, 25.5, 24.5, 20.5, 30.5, 22, 30, 30, 22, 30.5), 1.6, s));

    private static List<Op> RectangleBadge(double s)
        => Ink.Compose(Ink.Stroke(Ink.Rect(20.5, 21.5, 30.5, 29.5), 1.6, s, PenLineCap.Square));

    private static List<Op> TextBadge(double s)
        => Ink.Compose(Ink.Fill(Ink.Group(Ink.Rect(20, 20, 31, 22.6), Ink.Rect(24.2, 20, 26.8, 31)), s));

    // A white tile with black ink: the white body is the outline, so halo = tile, ink = lines.
    private static List<Op> Tile(double s, Geometry frame, params Geometry[] lines)
    {
        var tile = Ink.Scaled(frame, s);
        var ink = Ink.Scaled(Ink.Group(lines), s);

        return new List<Op>
        {
            new(tile, Ink.White, Ink.Pen(Ink.White, 2 * Ink.Halo * s, PenLineCap.Square)),
            new(Ink.Group(tile, ink), null, Ink.Pen(Ink.Black, 1.0 * s, PenLineCap.Flat)),
        };
    }

    private static List<Op> RulerBadge(double s)
        => Tile(s, Ink.Rect(19.5, 22.5, 31, 29.5),
                Ink.Line(22.5, 22.5, 22.5, 25.5),
                Ink.Line(25.25, 22.5, 25.25, 27),
                Ink.Line(28, 22.5, 28, 25.5));

    private static List<Op> AreaBadge(double s)
        => Tile(s, Ink.Rect(19.5, 19.5, 31, 31),
                Ink.Line(23.33, 19.5, 23.33, 31), Ink.Line(27.17, 19.5, 27.17, 31),
                Ink.Line(19.5, 23.33, 31, 23.33), Ink.Line(19.5, 27.17, 31, 27.17));

    // ── Magnifier ────────────────────────────────────────────────────────────

    private static (List<Op>, int, int) Magnifier(int size, bool plus)
    {
        double s = size / 32.0;

        var parts = new List<(Op, Op)>
        {
            Ink.Stroke(Ink.Circle(12.5, 12.5, 7.5), 2.2, s),
            Ink.Stroke(Ink.Line(18.2, 18.2, 26.5, 26.5), 3.6, s),
            Ink.Stroke(Ink.Line(9, 12.5, 16, 12.5), 2.2, s, PenLineCap.Flat),
        };

        if (plus)
            parts.Add(Ink.Stroke(Ink.Line(12.5, 9, 12.5, 16), 2.2, s, PenLineCap.Flat));

        int hot = (int)Math.Floor(12.5 * s);

        return (Ink.Compose(parts.ToArray()), hot, hot);
    }

    // ── Identify: Windows-style arrow with an (i) badge ─────────────────────

    private static (List<Op>, int, int) Identify(int size)
    {
        double s = size / 32.0;

        var arrow = Ink.Scaled(Ink.Poly(true,
            1.5, 1.5,
            1.5, 18.5,
            5.5, 14.7,
            8.3, 21,
            10.9, 19.9,
            8.2, 13.7,
            13.6, 13.7), s);

        var badge = Ink.Circle(24.5, 24.5, 6.2);
        var iGlyph = Ink.Scaled(Ink.Group(
            Ink.Circle(24.5, 21.4, 1.25),
            Ink.Rect(23.45, 23.4, 25.55, 28.6)), s);

        var (badgeHalo, badgeInk) = Ink.Fill(badge, s);

        var ops = new List<Op>
        {
            new(arrow, Ink.White, Ink.Pen(Ink.Black, 1.0 * s)),
            badgeHalo,
            badgeInk,
            new(iGlyph, Ink.White, null),
        };

        int hot = (int)Math.Floor(1.5 * s);

        return (ops, hot, hot);
    }

    // ── Hands (Phosphor Icons, MIT: https://github.com/phosphor-icons/core) ──
    // White body (the *Fill variant) under the black outline (the regular variant), on Phosphor's
    // 256-unit grid. Both hands share one scale and the same centre line and bottom, so switching
    // between them on mouse down/up does not make the cursor jump.

    private static (List<Op>, int, int) Hand(int size, bool grabbing)
    {
        double s = size / 32.0;

        const double k = 0.112;                       // Phosphor unit -> design unit
        const double left = 5.0, top = 3.0;           // where the open hand's box (24,16) lands

        // open hand box: x 24..192, y 16..224; the grabbing hand box: x 40..176, y 64..176,
        // centred on the same x (108); its bottom is moved down to the open hand's bottom (+48)
        double dy = grabbing ? 48 : 0;

        var transform = new TransformGroup();
        transform.Children.Add(new TranslateTransform(-24, -16 + dy));
        transform.Children.Add(new ScaleTransform(k, k));
        transform.Children.Add(new TranslateTransform(left, top));
        transform.Children.Add(new ScaleTransform(s, s));
        transform.Freeze();

        var body = Icon(grabbing ? PackIconPhosphorIconsKind.HandGrabbingFill : PackIconPhosphorIconsKind.HandFill, transform);
        var outline = Icon(grabbing ? PackIconPhosphorIconsKind.HandGrabbing : PackIconPhosphorIconsKind.Hand, transform);

        var ops = new List<Op>
        {
            new(body, Ink.White, null),
            new(outline, Ink.Black, null),
        };

        // palm centre, (108, 150) on the Phosphor grid
        int hotX = (int)Math.Round(((108 - 24) * k + left) * s);
        int hotY = (int)Math.Round(((150 - 16) * k + top) * s);

        return (ops, hotX, hotY);
    }

    private static Geometry Icon(PackIconPhosphorIconsKind kind, Transform transform)
    {
        var data = new PackIconPhosphorIcons { Kind = kind }.Data
            ?? throw new InvalidOperationException($"No path data for {kind}.");

        var geometry = Geometry.Parse(data).Clone();
        geometry.Transform = transform;
        geometry.Freeze();

        return geometry;
    }
}

/// <summary>Writes a .cur file: one 32-bit DIB image per size with an AND mask (1 = transparent).</summary>
internal static class CurWriter
{
    public static byte[] Write(IReadOnlyList<CursorFrame> frames)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        w.Write((ushort)0);                // reserved
        w.Write((ushort)2);                // 2 = cursor
        w.Write((ushort)frames.Count);

        var images = frames.Select(Dib).ToList();
        int offset = 6 + 16 * frames.Count;

        for (int i = 0; i < frames.Count; i++)
        {
            var f = frames[i];

            w.Write((byte)(f.Size >= 256 ? 0 : f.Size));
            w.Write((byte)(f.Size >= 256 ? 0 : f.Size));
            w.Write((byte)0);              // palette size
            w.Write((byte)0);              // reserved
            w.Write((ushort)f.HotX);
            w.Write((ushort)f.HotY);
            w.Write(images[i].Length);
            w.Write(offset);

            offset += images[i].Length;
        }

        foreach (var image in images)
            w.Write(image);

        w.Flush();
        return ms.ToArray();
    }

    private static byte[] Dib(CursorFrame f)
    {
        int n = f.Size;
        int maskStride = (n + 31) / 32 * 4;

        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        // BITMAPINFOHEADER; the height counts the XOR image and the AND mask
        w.Write(40);
        w.Write(n);
        w.Write(n * 2);
        w.Write((ushort)1);
        w.Write((ushort)32);
        w.Write(0);                        // BI_RGB
        w.Write(n * n * 4 + maskStride * n);
        w.Write(0);
        w.Write(0);
        w.Write(0);
        w.Write(0);

        // XOR image, bottom-up BGRA
        for (int y = n - 1; y >= 0; y--)
            w.Write(f.Bgra, y * n * 4, n * 4);

        // AND mask, bottom-up, 1 bit per pixel
        for (int y = n - 1; y >= 0; y--)
        {
            var row = new byte[maskStride];

            for (int x = 0; x < n; x++)
            {
                if (f.Bgra[(y * n + x) * 4 + 3] == 0)
                    row[x / 8] |= (byte)(0x80 >> (x % 8));
            }

            w.Write(row);
        }

        w.Flush();
        return ms.ToArray();
    }
}

/// <summary>A PNG contact sheet: every cursor at 32 and 48 px on light, mid and dark grounds, plus 4x.</summary>
internal static class PreviewSheet
{
    public static void Write(IReadOnlyList<CursorSpec> cursors, string path)
    {
        var grounds = new[] { Color.FromRgb(0xF4, 0xF4, 0xF4), Color.FromRgb(0x8C, 0x9A, 0x7E), Color.FromRgb(0x26, 0x2B, 0x33) };

        const int cell = 72, zoomCell = 72, labelH = 18;
        int width = grounds.Length * 2 * cell + zoomCell;
        int height = cursors.Count * (cell + labelH);

        var visual = new DrawingVisual();

        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

            for (int row = 0; row < cursors.Count; row++)
            {
                var cursor = cursors[row];
                double y = row * (cell + labelH);

                dc.DrawText(new FormattedText($"{cursor.FileName}  {cursor.Description}", System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11, Brushes.Black, 1.0), new Point(4, y + 2));

                y += labelH;

                int col = 0;

                foreach (var ground in grounds)
                {
                    foreach (var size in new[] { 32, 48 })
                    {
                        double x = col * cell;
                        dc.DrawRectangle(new SolidColorBrush(ground), null, new Rect(x, y, cell, cell));

                        var frame = cursor.Render(size);
                        double ox = x + (cell - size) / 2.0, oy = y + (cell - size) / 2.0;
                        dc.DrawImage(ToBitmap(frame), new Rect(ox, oy, size, size));

                        // hotspot marker: a red pixel beside the image's left edge and top edge
                        dc.DrawRectangle(Brushes.Red, null, new Rect(ox - 3, oy + frame.HotY, 2, 1));
                        dc.DrawRectangle(Brushes.Red, null, new Rect(ox + frame.HotX, oy - 3, 1, 2));

                        col++;
                    }
                }

                // 32 px frame at 2x, pixel by pixel, on a mid-grey ground (shows the white outline)
                double zx = col * cell + 4, zy = y - labelH + 4;
                var f32 = cursor.Render(32);

                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)), null, new Rect(zx, zy, 64, 64));

                for (int py = 0; py < 32; py++)
                {
                    for (int px = 0; px < 32; px++)
                    {
                        int i = (py * 32 + px) * 4;

                        if (f32.Bgra[i + 3] == 0)
                            continue;

                        var color = Color.FromArgb(f32.Bgra[i + 3], f32.Bgra[i + 2], f32.Bgra[i + 1], f32.Bgra[i]);
                        dc.DrawRectangle(new SolidColorBrush(color), null, new Rect(zx + px * 2, zy + py * 2, 2, 2));
                    }
                }

                dc.DrawRectangle(Brushes.Red, null, new Rect(zx + f32.HotX * 2, zy + f32.HotY * 2, 2, 2));
            }
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static BitmapSource ToBitmap(CursorFrame f)
    {
        var bmp = BitmapSource.Create(f.Size, f.Size, 96, 96, PixelFormats.Bgra32, null, f.Bgra, f.Size * 4);
        bmp.Freeze();
        return bmp;
    }
}
