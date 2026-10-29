using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Infrastructure.GdiPlus.Helpers;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// Labels the synthetic triad grid of the LAS paper with a rasterization oracle instead of the hand-written
/// rule table of <see cref="SyntheticDataFactory"/>: the three-point line and its two-point version are drawn
/// exactly as the visual metric draws them (<see cref="MetricRasterizer"/> with <see cref="SimplificationHelper.RenderOptions"/>
/// unless other options are given; a pixel counts as drawn when it is neither transparent nor white) and the
/// number of differing pixels is recorded, once with
/// that pixel rule and once counting only pixels at least half covered, together with the largest displacement
/// of a drawn pixel (<see cref="OracleLabelledTriad.MaxDisplacement"/>, the rule "hd-d": retain when some pixel
/// moves farther than d) and the excess displacement. The grid (base lengths, 0.5-px step,
/// ±4-px margins) is the one of the published training set; every cell is also rendered at several base
/// orientations and sub-pixel offsets, because rasterization is not rotation invariant while the features are.
/// The published training data is not touched; the result is a new file.
/// </summary>
public static class OracleLabeller
{
    public static readonly double[] DefaultBaseLengths = { 0.5, 1, 2, 4, 8, 16, 32 };

    public static readonly double[] DefaultRotations = { 0, 22.5, 45, 67.5, 90 };

    public static readonly double[][] DefaultOffsets = { new[] { 0.0, 0.0 }, new[] { 0.5, 0.5 }, new[] { 0.25, 0.75 }, new[] { 0.75, 0.25 } };

    public const double GridStep = 0.5;

    public const double Margin = 4;

    public const int CanvasSize = 64;

    /// <summary>
    /// The labelling rules compared in the agreement report and the grids: "any-k" counts every drawn pixel
    /// (the rule of the visual metric), "opaque-k" only pixels at least half covered, "relative-r" tolerates
    /// a fraction r of the original's drawn pixels.
    /// </summary>
    public static readonly (string Name, Func<OracleLabelledTriad, bool> IsRetained)[] LabelRules =
    {
        ("any-k0", t => t.IsRetained(0)),
        ("any-k1", t => t.IsRetained(1)),
        ("any-k2", t => t.IsRetained(2)),
        ("any-k4", t => t.IsRetained(4)),
        ("any-k8", t => t.IsRetained(8)),
        ("opaque-k0", t => t.IsRetainedOpaque(0)),
        ("opaque-k1", t => t.IsRetainedOpaque(1)),
        ("opaque-k2", t => t.IsRetainedOpaque(2)),
        ("opaque-k4", t => t.IsRetainedOpaque(4)),
        ("relative-10", t => t.IsRetainedRelative(0.10)),
        ("relative-25", t => t.IsRetainedRelative(0.25)),
        ("relative-50", t => t.IsRetainedRelative(0.50)),
        ("hd-1", t => t.IsRetainedByDisplacement(1)),
        ("hd-1.5", t => t.IsRetainedByDisplacement(Math.Sqrt(2))),
        ("hd-2", t => t.IsRetainedByDisplacement(2)),
    };

    public static OracleSyntheticDataFile Label(
        double[]? baseLengths = null,
        double[]? rotations = null,
        double[][]? offsets = null,
        MetricRenderOptions? render = null)
    {
        render ??= SimplificationHelper.RenderOptions;

        baseLengths ??= DefaultBaseLengths;
        rotations ??= DefaultRotations;
        offsets ??= DefaultOffsets;

        var result = new OracleSyntheticDataFile()
        {
            Renderer = $"{render}; opaque = alpha >= 128",
            CanvasSize = CanvasSize,
            GridStep = GridStep,
            Margin = Margin,
            BaseLengths = baseLengths.ToList(),
            RotationsDegrees = rotations.ToList(),
            Offsets = offsets.ToList(),
        };

        using var bitmap = new Bitmap(CanvasSize, CanvasSize, PixelFormat.Format32bppArgb);

        var original = new bool[CanvasSize * CanvasSize];
        var simplified = new bool[CanvasSize * CanvasSize];
        var originalOpaque = new bool[CanvasSize * CanvasSize];
        var simplifiedOpaque = new bool[CanvasSize * CanvasSize];

        var originalMask = new bool[CanvasSize, CanvasSize];
        var simplifiedMask = new bool[CanvasSize, CanvasSize];

        foreach (var baseLength in baseLengths)
        {
            // the cell loops of SyntheticDataFactory.Create, with integer counters so that no cell is lost to rounding
            var countX = (int)Math.Round((baseLength + 2 * Margin) / GridStep) + 1;
            var countY = (int)Math.Round(2 * Margin / GridStep) + 1;

            for (int iy = 0; iy < countY; iy++)
            {
                var y = -Margin + iy * GridStep;

                for (int ix = 0; ix < countX; ix++)
                {
                    var x = -Margin + ix * GridStep;

                    foreach (var rotation in rotations)
                    {
                        foreach (var offset in offsets)
                        {
                            var triad = new OracleLabelledTriad()
                            {
                                BaseLength = baseLength,
                                Dx1 = x,
                                Dx2 = x - baseLength,
                                Dy = -y,
                                RotationDegrees = rotation,
                                OffsetX = offset[0],
                                OffsetY = offset[1],
                            };

                            // rotate about the base midpoint and centre it on the canvas
                            var (x1, y1) = Place(0, 0, baseLength, rotation, offset);
                            var (x2, y2) = Place(x, y, baseLength, rotation, offset);
                            var (x3, y3) = Place(baseLength, 0, baseLength, rotation, offset);

                            triad.X1 = x1; triad.Y1 = y1;
                            triad.X2 = x2; triad.Y2 = y2;
                            triad.X3 = x3; triad.Y3 = y3;

                            Render(bitmap, render, new[] { new PointF((float)x1, (float)y1), new PointF((float)x2, (float)y2), new PointF((float)x3, (float)y3) }, original, originalOpaque);
                            Render(bitmap, render, new[] { new PointF((float)x1, (float)y1), new PointF((float)x3, (float)y3) }, simplified, simplifiedOpaque);

                            int originalPixels = 0, simplifiedPixels = 0, different = 0, originalOpaquePixels = 0, simplifiedOpaquePixels = 0, differentOpaque = 0;

                            for (int i = 0; i < original.Length; i++)
                            {
                                if (original[i]) originalPixels++;
                                if (simplified[i]) simplifiedPixels++;
                                if (original[i] != simplified[i]) different++;

                                if (originalOpaque[i]) originalOpaquePixels++;
                                if (simplifiedOpaque[i]) simplifiedOpaquePixels++;
                                if (originalOpaque[i] != simplifiedOpaque[i]) differentOpaque++;
                            }

                            triad.OriginalPixels = originalPixels;
                            triad.SimplifiedPixels = simplifiedPixels;
                            triad.DifferentPixels = different;

                            triad.OriginalPixelsOpaque = originalOpaquePixels;
                            triad.SimplifiedPixelsOpaque = simplifiedOpaquePixels;
                            triad.DifferentPixelsOpaque = differentOpaque;

                            // the displacement of the drawn line, from the distance transforms of the two masks
                            for (int i = 0; i < original.Length; i++)
                            {
                                originalMask[i % CanvasSize, i / CanvasSize] = original[i];
                                simplifiedMask[i % CanvasSize, i / CanvasSize] = simplified[i];
                            }

                            var comparison = RasterComparison.Compare(originalMask, simplifiedMask, 1);

                            triad.MaxDisplacement = double.IsInfinity(comparison.Hausdorff) ? CanvasSize : comparison.Hausdorff;
                            triad.ExcessDisplacement = double.IsInfinity(comparison.ExcessDisplacement) ? CanvasSize * CanvasSize : comparison.ExcessDisplacement;

                            result.Triads.Add(triad);
                        }
                    }
                }
            }
        }

        return result;
    }

    private static (double x, double y) Place(double x, double y, double baseLength, double rotationDegrees, double[] offset)
    {
        var angle = rotationDegrees * Math.PI / 180;

        var cx = x - baseLength / 2;

        var rx = cx * Math.Cos(angle) - y * Math.Sin(angle);
        var ry = cx * Math.Sin(angle) + y * Math.Cos(angle);

        return (rx + CanvasSize / 2.0 + offset[0], ry + CanvasSize / 2.0 + offset[1]);
    }

    // draws the polyline on a cleared canvas and reads two drawn-pixel masks: any coverage (the pixel rule of
    // ImageHelper.ToMask, i.e. of the visual metric) and at least half coverage (alpha >= 128)
    private static void Render(Bitmap bitmap, MetricRenderOptions render, PointF[] points, bool[] mask, bool[] opaqueMask)
    {
        MetricRasterizer.RenderScreenPolyline(bitmap, points, render);

        var white = Color.White.ToArgb();

        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            var row = new int[bitmap.Width];

            for (int y = 0; y < bitmap.Height; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, bitmap.Width);

                for (int x = 0; x < bitmap.Width; x++)
                {
                    mask[y * bitmap.Width + x] = row[x] != 0 && row[x] != white;

                    opaqueMask[y * bitmap.Width + x] = ((row[x] >> 24) & 0xFF) >= 128 && row[x] != white;
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static readonly Dictionary<double, Predicate<(double dx1, double dx2, double dy)>> PublishedRules = new()
    {
        { 0.5, SyntheticDataFactory.IsRetainedFunc0_5 },
        { 1, SyntheticDataFactory.IsRetainedFunc1_0 },
        { 2, SyntheticDataFactory.IsRetainedFunc2_0 },
        { 4, SyntheticDataFactory.IsRetainedFunc4_0 },
        { 8, SyntheticDataFactory.IsRetainedFunc8_0 },
        { 16, SyntheticDataFactory.IsRetainedFunc16_0 },
        { 32, SyntheticDataFactory.IsRetainedFunc32_0 },
    };

    private static bool PublishedLabel(OracleLabelledTriad t) => PublishedRules[t.BaseLength]((t.Dx1, t.Dx2, t.Dy));

    /// <summary>
    /// Compares the oracle labels of the unrotated, unshifted cells with the hand-written table of
    /// <see cref="SyntheticDataFactory"/> under every rule of <see cref="LabelRules"/>: the validation of the
    /// manual labels the reviewer asked for, and the retained fraction per base length over all rotations.
    /// </summary>
    public static string AgreementWithPublishedLabels(OracleSyntheticDataFile file)
    {
        var cells = file.Triads.Where(t => t.RotationDegrees == 0 && t.OffsetX == 0 && t.OffsetY == 0 && PublishedRules.ContainsKey(t.BaseLength)).ToList();

        var builder = new StringBuilder();

        builder.AppendLine($"Unrotated, unshifted cells: {cells.Count}; published table retains {cells.Count(PublishedLabel)}");
        builder.AppendLine();
        builder.AppendLine("rule\toracle retains\tagreement\ttable remove & oracle retain (visible change missed by table)\ttable retain & oracle remove (compression left on the table)\tretained over all rotations/offsets");

        foreach (var (name, isRetained) in LabelRules)
        {
            int agree = 0, tableRemoveOracleRetain = 0, tableRetainOracleRemove = 0, oracleRetains = 0;

            foreach (var cell in cells)
            {
                var table = PublishedLabel(cell);
                var oracle = isRetained(cell);

                if (oracle) oracleRetains++;
                if (table == oracle) agree++;
                else if (oracle) tableRemoveOracleRetain++;
                else tableRetainOracleRemove++;
            }

            builder.AppendLine($"{name}\t{oracleRetains}\t{(double)agree / cells.Count:P2}\t{tableRemoveOracleRetain}\t{tableRetainOracleRemove}\t{file.Triads.Count(isRetained) / (double)file.Triads.Count:0.000}");
        }

        builder.AppendLine();
        builder.AppendLine("Retained fraction per base length over all rotations and offsets (table = published rule on the unrotated cells):");
        builder.AppendLine("base\ttable\t" + string.Join("\t", LabelRules.Select(r => r.Name)));

        foreach (var baseLength in file.BaseLengths)
        {
            var baseCells = cells.Where(c => c.BaseLength == baseLength).ToList();
            var all = file.Triads.Where(t => t.BaseLength == baseLength).ToList();

            if (baseCells.Count == 0)
                continue;

            var tableFraction = baseCells.Count(PublishedLabel) / (double)baseCells.Count;

            builder.AppendLine($"{baseLength}\t{tableFraction:0.000}\t" + string.Join("\t", LabelRules.Select(r => (all.Count(r.IsRetained) / (double)all.Count).ToString("0.000"))));
        }

        builder.AppendLine();
        builder.AppendLine("Largest displacement of a drawn pixel (px) of the cells the published table removes, by |dy| (unrotated, unshifted): median [min, max]");

        foreach (var dy in cells.Where(c => !PublishedLabel(c)).Select(c => Math.Abs(c.Dy)).Distinct().OrderBy(v => v))
        {
            var removed = cells.Where(c => !PublishedLabel(c) && Math.Abs(c.Dy) == dy).ToList();

            builder.AppendLine($"|dy| = {dy}\tn = {removed.Count}\t{HypothesisTests.Median(removed.Select(c => c.MaxDisplacement).ToList()):0.00} [{removed.Min(c => c.MaxDisplacement):0.00}, {removed.Max(c => c.MaxDisplacement):0.00}]");
        }

        builder.AppendLine();
        builder.AppendLine("Differing pixels (any coverage / opaque) of the cells the published table removes, by |dy| (unrotated, unshifted): median [min, max]");

        foreach (var dy in cells.Where(c => !PublishedLabel(c)).Select(c => Math.Abs(c.Dy)).Distinct().OrderBy(v => v))
        {
            var removed = cells.Where(c => !PublishedLabel(c) && Math.Abs(c.Dy) == dy).ToList();

            builder.AppendLine($"|dy| = {dy}\tn = {removed.Count}\tany {HypothesisTests.Median(removed.Select(c => (double)c.DifferentPixels).ToList())} [{removed.Min(c => c.DifferentPixels)}, {removed.Max(c => c.DifferentPixels)}]\topaque {HypothesisTests.Median(removed.Select(c => (double)c.DifferentPixelsOpaque).ToList())} [{removed.Min(c => c.DifferentPixelsOpaque)}, {removed.Max(c => c.DifferentPixelsOpaque)}]");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Label-grid images like those of <see cref="SyntheticDataHelper.PrintAll"/>: one per base length and
    /// rule for the unrotated, unshifted cells; red = removed, white = retained; the numbers in the cells are
    /// the differing pixels, any coverage / opaque. The published table's grid is written as well.
    /// </summary>
    public static void PrintGrids(OracleSyntheticDataFile file, string directory, IEnumerable<(string Name, Func<OracleLabelledTriad, bool> IsRetained)> rules)
    {
        Directory.CreateDirectory(directory);

        const int scale = 30;

        using var font = new Font("Consolas", 7);

        var allRules = rules.Prepend(("published-table", PublishedLabel)).ToList();

        foreach (var baseLength in file.BaseLengths)
        {
            var cells = file.Triads.Where(t => t.BaseLength == baseLength && t.RotationDegrees == 0 && t.OffsetX == 0 && t.OffsetY == 0).ToList();

            if (cells.Count == 0)
                continue;

            var minX = cells.Min(c => c.Dx1);
            var maxX = cells.Max(c => c.Dx1);
            var minY = cells.Min(c => c.Dy);
            var maxY = cells.Max(c => c.Dy);

            var width = (int)Math.Round((maxX - minX) / file.GridStep) + 1;
            var height = (int)Math.Round((maxY - minY) / file.GridStep) + 1;

            foreach (var (name, isRetained) in allRules)
            {
                using var bitmap = new Bitmap(width * scale + 1, height * scale + 1);

                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.White);

                    foreach (var cell in cells)
                    {
                        var column = (int)Math.Round((cell.Dx1 - minX) / file.GridStep);
                        var row = (int)Math.Round((maxY - cell.Dy) / file.GridStep);

                        var rectangle = new Rectangle(column * scale, row * scale, scale, scale);

                        graphics.FillRectangle(isRetained(cell) ? Brushes.White : Brushes.Red, rectangle);
                        graphics.DrawRectangle(Pens.Gray, rectangle);
                        graphics.DrawString($"{cell.DifferentPixels}/{cell.DifferentPixelsOpaque}", font, Brushes.DimGray, rectangle.X + 1, rectangle.Y + 9);
                    }

                    // the base
                    var baseRow = (int)Math.Round((maxY - 0) / file.GridStep);
                    var startColumn = (int)Math.Round((0 - minX) / file.GridStep);
                    var endColumn = (int)Math.Round((baseLength - minX) / file.GridStep);

                    using var basePen = new Pen(Color.Blue, 3);

                    graphics.DrawLine(basePen, startColumn * scale + scale / 2, baseRow * scale + scale / 2, endColumn * scale + scale / 2, baseRow * scale + scale / 2);
                }

                bitmap.Save(Path.Combine(directory, $"oracle-grid-{name}-base{baseLength}.png"), ImageFormat.Png);
            }
        }
    }
}
