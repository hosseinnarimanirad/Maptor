using System;
using System.Linq;
using System.Collections.Generic;
using sb = IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.MachineLearning;

/// <summary>The labelling rule set of the synthetic training triads.</summary>
public enum SyntheticLabelRules
{
    /// <summary>The published rule set (training data of LR4v23/LR7v23 and of the LAS paper).</summary>
    V23 = 23,

    /// <summary>
    /// V23 with the collinear row of the 0.5-px base made consistent with the 1, 2 and 4-px bases: a middle point
    /// on the base line within 2.5 px beyond an end is removed (V23 retains it for the 0.5-px base only).
    /// </summary>
    V24 = 24,
}

/// <summary>One cell of the synthetic label grid (<see cref="SyntheticDataFactory.CreateLabelGrid"/>).</summary>
public class SyntheticLabelCell
{
    public double BaseLength { get; set; }

    /// <summary>Position of the middle point relative to the start point, pixels, in the coordinates of the synthetic set (x - 100, y - 100).</summary>
    public double X { get; set; }

    public double Y { get; set; }

    public double Dx1 { get; set; }

    public double Dx2 { get; set; }

    public double Dy { get; set; }

    public bool IsRetained { get; set; }

    /// <summary>The middle point coincides with the start or the end point.</summary>
    public bool IsEndPoint { get; set; }
}

/// <summary>
/// The synthetic training set of the LRS models: for every base length b (<see cref="BaseLengths"/>) the start
/// point is at (100, 100), the end point at (100 + b, 100), and the middle point takes every position of a
/// 0.5-px grid within 4 px of the base segment; each position is labelled by the rule of its base length.
/// </summary>
public static class SyntheticDataFactory
{
    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc0_5 =
            param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
            {
                (0.5, >= -2.5, <= 2.5) => false,
                (1.0, >= -2.0, <= 2.0) => false,
                (1.5, >= -2.0, <= 2.0) => false,
                (2.0, >= -1.5, <= 1.5) => false,
                //(2.5, >= -0.0, <= 0.0) => false,  
                _ => true
            };

    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc1_0 =
           param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
           {
               (0.0, >= -2.5, <= 2.5) => false,
               (0.5, >= -2.5, <= 2.5) => false,
               (1.0, >= -2.0, <= 2.0) => false,
               (1.5, >= -2.0, <= 2.0) => false,
               (2.0, >= -0.5, <= 0.5) => false,
               //(2.5, >= -0.0, <= 0.0) => false,
               _ => true
           };

    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc2_0 =
           param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
           {
               (0.0, >= -2.5, <= 2.5) => false,
               (0.5, >= -2.5, <= 2.5) => false,
               (1.0, >= -2.0, <= 2.0) => false,
               (1.5, >= -2.0, <= 2.0) => false,
               //(2.0, >= -1.0, <= 1.0) => false,
               //(2.5, >= -0.0, <= 0.0) => false,
               _ => true
           };

    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc4_0 =
           param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
           {
               (0.0, >= -2.5, <= 2.5) => false,
               (0.5, >= -2.5, <= 2.5) => false,
               (1.0, >= -2.0, <= 2.0) => false,
               (1.5, >= -1.5, <= 1.5) => false,
               _ => true
           };

    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc8_0 =
           param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
           {
               (0.0, >= -0.0, <= 0.0) => false,
               (0.5, >= -0.0, <= 0.0) => false,
               _ => true
           };

    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc16_0 =
           param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
           {
               (0.0, >= -0.0, <= 0.0) => false,
               (0.5, >= -0.0, <= 0.0) => false,
               _ => true
           };

    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc32_0 =
           param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
           {
               (0.0, >= -0.0, <= 0.0) => false,
               (0.5, >= -0.0, <= 0.0) => false,
               _ => true
           };

    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc40_0 =
           param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
           {
               (0.0, >= -0.0, <= 0.0) => false,
               _ => true
           };


    /// <summary>The base lengths of the synthetic set, in pixels.</summary>
    public static readonly double[] BaseLengths = { 0.5, 1, 2, 4, 8, 16, 32 };

    /// <summary>
    /// The label rule of one base length: the middle point is removed when the predicate returns false.
    /// <see cref="SyntheticLabelRules.V23"/> is the published rule (the <c>IsRetainedFunc*</c> fields);
    /// <see cref="SyntheticLabelRules.V24"/> differs only for the 0.5-px base, see the enum.
    /// </summary>
    public static Predicate<(double dx1, double dx2, double dy)> GetRule(double baseLength, SyntheticLabelRules rules = SyntheticLabelRules.V23)
    {
        if (rules == SyntheticLabelRules.V24 && baseLength == 0.5)
            return IsRetainedFunc0_5V24;

        return baseLength switch
        {
            0.5 => IsRetainedFunc0_5,
            1 => IsRetainedFunc1_0,
            2 => IsRetainedFunc2_0,
            4 => IsRetainedFunc4_0,
            8 => IsRetainedFunc8_0,
            16 => IsRetainedFunc16_0,
            32 => IsRetainedFunc32_0,
            _ => throw new ArgumentOutOfRangeException(nameof(baseLength), baseLength, "not a base length of the synthetic set"),
        };
    }

    /// <summary>
    /// <see cref="IsRetainedFunc0_5"/> plus the collinear row (|dy| = 0, overhang up to 2.5 px) that the 1, 2 and
    /// 4-px bases have: rule set <see cref="SyntheticLabelRules.V24"/>.
    /// </summary>
    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc0_5V24 =
            param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
            {
                (0.0, >= -2.5, <= 2.5) => false,
                _ => IsRetainedFunc0_5(param),
            };

    /// <summary>
    /// The synthetic set as training records with the given features: every item is a three-point line in
    /// screen pixels and yields one record (the conversion the research tool applies when it builds a model).
    /// With <see cref="SyntheticLabelRules.V23"/> it equals the contents of the <c>LR*v23.json</c> model files for
    /// their feature lists. A middle point that coincides with the start or the end point is labelled retained
    /// whatever the rule says (6 of the 736 removed grid cells of V23), because the record is built by matching
    /// the simplified line's vertices to the original's.
    /// </summary>
    public static LRSimplificationTrainingData<sb.Point> CreateTrainingData(List<LRSimplificationFeatures> features, SyntheticLabelRules rules = SyntheticLabelRules.V23)
    {
        return CreateTrainingData(features, b => GetRule(b, rules));
    }

    /// <summary>The synthetic set labelled by any rule per base length (e.g. <see cref="ShiftBoundary"/>).</summary>
    public static LRSimplificationTrainingData<sb.Point> CreateTrainingData(List<LRSimplificationFeatures> features, Func<double, Predicate<(double dx1, double dx2, double dy)>> ruleOfBase)
    {
        var records = new List<LRSimplificationParameters<sb.Point>>();

        foreach (var item in BaseLengths.SelectMany(b => Create(b, ruleOfBase(b))))
        {
            var original = Geometry<sb.Point>.FromWkt(item.OriginalLineString, 0);
            var simplified = Geometry<sb.Point>.FromWkt(item.SimplifiedLineString, 0);

            records.AddRange(LRSimplificationTrainingData<sb.Point>.Create(original.Points, simplified.Points, false, features).Records);
        }

        return new LRSimplificationTrainingData<sb.Point>(records);
    }

    public static List<SyntheticDataItem> CreateAll(SyntheticLabelRules rules = SyntheticLabelRules.V23)
    {
        return BaseLengths.SelectMany(b => Create(b, GetRule(b, rules))).ToList();
    }

    /// <summary>
    /// The rule set with the boundary between removed and retained cells moved by one grid cell (0.5 px) on every
    /// base length: <paramref name="cells"/> = +1 adds every retained cell next to a removed one (4-neighbourhood) to
    /// the removed region, −1 returns every removed cell next to a retained one (or to the edge of the grid) to the
    /// retained region. The sensitivity of the models to where the annotator drew the boundary.
    /// </summary>
    public static Func<double, Predicate<(double dx1, double dx2, double dy)>> ShiftBoundary(SyntheticLabelRules rules, int cells, int margin = 4, double step = 0.5)
    {
        if (cells is not (1 or -1))
            throw new ArgumentOutOfRangeException(nameof(cells), cells, "must be +1 or -1");

        var removedByBase = new Dictionary<double, HashSet<(double x, double y)>>();

        var grid = CreateLabelGrid(rules, margin, step);

        foreach (var group in grid.GroupBy(c => c.BaseLength))
        {
            var removed = new HashSet<(double, double)>(group.Where(c => !c.IsRetained).Select(c => (c.X, c.Y)));
            var inside = new HashSet<(double, double)>(group.Select(c => (c.X, c.Y)));

            IEnumerable<(double, double)> Neighbours((double x, double y) c) => new[] { (c.x - step, c.y), (c.x + step, c.y), (c.x, c.y - step), (c.x, c.y + step) };

            var shifted = new HashSet<(double, double)>();

            foreach (var cell in inside)
            {
                var isRemoved = removed.Contains(cell);

                if (cells > 0 && (isRemoved || Neighbours(cell).Any(removed.Contains)))
                    shifted.Add(cell);

                if (cells < 0 && isRemoved && Neighbours(cell).All(n => inside.Contains(n) && removed.Contains(n)))
                    shifted.Add(cell);
            }

            removedByBase[group.Key] = shifted;
        }

        return baseLength =>
        {
            var removed = removedByBase[baseLength];

            // a cell's position relative to the start point: x = dx1, y = -dy (CreateLabelGrid)
            return param => !removed.Contains((Math.Round(param.dx1 / step) * step, Math.Round(-param.dy / step) * step + 0.0));
        };
    }

    /// <summary>
    /// The label grid of every base length as plain numbers, for figures and reports: the position of the middle
    /// point relative to the start point (pixels; the end point is at (b, 0)), its offsets, and the label of the
    /// rule. Cells on the start or end point are flagged. Same grid and order as <see cref="CreateAll"/>.
    /// </summary>
    public static List<SyntheticLabelCell> CreateLabelGrid(SyntheticLabelRules rules = SyntheticLabelRules.V23, int margin = 4, double step = 0.5)
    {
        var result = new List<SyntheticLabelCell>();

        foreach (var baseLength in BaseLengths)
        {
            var rule = GetRule(baseLength, rules);

            var columns = (int)Math.Round((baseLength + 2 * margin) / step);
            var rows = (int)Math.Round(2 * margin / step);

            for (int r = 0; r <= rows; r++)
            {
                for (int c = 0; c <= columns; c++)
                {
                    var x = -margin + c * step;
                    var y = -margin + r * step;

                    // SyntheticDataFactory.Create: dx1 = x - start, dx2 = x - end, dy = start.y - y
                    var dx1 = x;
                    var dx2 = x - baseLength;
                    var dy = -y;

                    result.Add(new SyntheticLabelCell()
                    {
                        BaseLength = baseLength,
                        X = x,
                        Y = y,
                        Dx1 = dx1,
                        Dx2 = dx2,
                        Dy = dy,
                        IsRetained = rule((dx1, dx2, dy)),
                        IsEndPoint = y == 0 && (x == 0 || x == baseLength),
                    });
                }
            }
        }

        return result;
    }

    private static List<SyntheticDataItem> Create(double baseLength, Predicate<(double dx1, double dx2, double dy)> isRetainedFunc, int xOffset = 4, int yOffset = 4)
    {
        double pointY = 100;
        double startX = 100;
        double endX = startX + baseLength;

        double minX = startX - xOffset;
        double minY = pointY - yOffset;

        double maxX = endX + xOffset;
        double maxY = pointY + yOffset;

        var startPoint = new sb.Point(startX, pointY);
        var endPoint = new sb.Point(endX, pointY);

        List<SyntheticDataItem> result = new List<SyntheticDataItem>();

        var step = 0.5;

        for (double y = minY; y <= maxY; y += step)
        {
            for (double x = minX; x <= maxX; x += step)
            {
                var middlePoint = new sb.Point(x, y);

                var dx1 = x - startX;
                var dx2 = x - endX;
                var dy = pointY - y;

                bool retained = isRetainedFunc((dx1, dx2, dy));

                Geometry<sb.Point> original = Geometry<sb.Point>.CreatePointOrLineString([startPoint, middlePoint, endPoint], 0);

                Geometry<sb.Point> simplified = retained ? original : Geometry<sb.Point>.CreatePointOrLineString([startPoint, endPoint], 0);

                var rowNumber = (int)((y - minY) / step);

                result.Add(new SyntheticDataItem()
                {
                    OriginalLineString = original.AsWkt(),
                    SimplifiedLineString = simplified.AsWkt(),
                    Title = $"Base{baseLength}-Row{rowNumber} - {original.AsWkt()}",
                    Note = retained ? "RETAINED" : "REMOVED"
                });
            }
        }

        return result;
    }

}
