using System.Linq;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Tests.Spatial;

/// <summary>
/// Test lines taken from published sources (papers, or the test suites shipped with well-known libraries), so that the
/// simplification tests do not depend on data made up for them. Each member names its source.
/// </summary>
internal static class PublishedSimplificationData
{
    /// <summary>
    /// simplify-js test fixture (test/test.js, https://github.com/mourner/simplify-js, BSD-2-Clause,
    /// (c) 2017 Vladimir Agafonkin): a 100-point coastline-like polyline.
    /// </summary>
    public static List<Point> SimplifyJsPoints() => new()
    {
        new(224.55, 250.15), new(226.91, 244.19), new(233.31, 241.45), new(234.98, 236.06), new(244.21, 232.76),
        new(262.59, 215.31), new(267.76, 213.81), new(273.57, 201.84), new(273.12, 192.16), new(277.62, 189.03),
        new(280.36, 181.41), new(286.51, 177.74), new(292.41, 159.37), new(296.91, 155.64), new(314.95, 151.37),
        new(319.75, 145.16), new(330.33, 137.57), new(341.48, 139.96), new(369.98, 137.89), new(387.39, 142.51),
        new(391.28, 139.39), new(409.52, 141.14), new(414.82, 139.75), new(427.72, 127.3), new(439.6, 119.74),
        new(474.93, 107.87), new(486.51, 106.75), new(489.2, 109.45), new(493.79, 108.63), new(504.74, 119.66),
        new(512.96, 122.35), new(518.63, 120.89), new(524.09, 126.88), new(529.57, 127.86), new(534.21, 140.93),
        new(539.27, 147.24), new(567.69, 148.91), new(575.25, 157.26), new(580.62, 158.15), new(601.53, 156.85),
        new(617.74, 159.86), new(622, 167.04), new(629.55, 194.6), new(638.9, 195.61), new(641.26, 200.81),
        new(651.77, 204.56), new(671.55, 222.55), new(683.68, 217.45), new(695.25, 219.15), new(700.64, 217.98),
        new(703.12, 214.36), new(712.26, 215.87), new(721.49, 212.81), new(727.81, 213.36), new(729.98, 208.73),
        new(735.32, 208.2), new(739.94, 204.77), new(769.98, 208.42), new(779.6, 216.87), new(784.2, 218.16),
        new(800.24, 214.62), new(810.53, 219.73), new(817.19, 226.82), new(820.77, 236.17), new(827.23, 236.16),
        new(829.89, 239.89), new(851, 248.94), new(859.88, 255.49), new(865.21, 268.53), new(857.95, 280.3),
        new(865.48, 291.45), new(866.81, 298.66), new(864.68, 302.71), new(867.79, 306.17), new(859.87, 311.37),
        new(860.08, 314.35), new(858.29, 314.94), new(858.1, 327.6), new(854.54, 335.4), new(860.92, 343),
        new(856.43, 350.15), new(851.42, 352.96), new(849.84, 359.59), new(854.56, 365.53), new(849.74, 370.38),
        new(844.09, 371.89), new(844.75, 380.44), new(841.52, 383.67), new(839.57, 390.4), new(845.59, 399.05),
        new(848.4, 407.55), new(843.71, 411.3), new(844.09, 419.88), new(839.51, 432.76), new(841.33, 441.04),
        new(847.62, 449.22), new(847.16, 458.44), new(851.38, 462.79), new(853.97, 471.15), new(866.36, 480.77)
    };

    /// <summary>
    /// The expected output of simplify-js for <see cref="SimplifyJsPoints"/> at tolerance 5 with the default settings:
    /// a radial-distance pass (keep a point farther than the tolerance from the last kept one) followed by
    /// Douglas-Peucker with the distance to the chord segment; both keep a point only when its distance exceeds the
    /// tolerance. 33 points.
    /// </summary>
    public static List<Point> SimplifyJsExpectedAtTolerance5() => new()
    {
        new(224.55, 250.15), new(267.76, 213.81), new(296.91, 155.64), new(330.33, 137.57), new(409.52, 141.14),
        new(439.6, 119.74), new(486.51, 106.75), new(529.57, 127.86), new(539.27, 147.24), new(617.74, 159.86),
        new(629.55, 194.6), new(671.55, 222.55), new(727.81, 213.36), new(739.94, 204.77), new(769.98, 208.42),
        new(779.6, 216.87), new(800.24, 214.62), new(820.77, 236.17), new(859.88, 255.49), new(865.21, 268.53),
        new(857.95, 280.3), new(867.79, 306.17), new(859.87, 311.37), new(854.54, 335.4), new(860.92, 343),
        new(849.84, 359.59), new(854.56, 365.53), new(844.09, 371.89), new(839.57, 390.4), new(848.4, 407.55),
        new(839.51, 432.76), new(853.97, 471.15), new(866.36, 480.77)
    };

    /// <summary>
    /// Ebisch, K. (2002) A correction to the Douglas-Peucker line generalization algorithm. Computers &amp; Geosciences 28,
    /// 995-997, Fig. 3 "Polyline with three vertices", thinned with a tolerance of 50 (p. 996). The coordinates are read
    /// from the plot (the middle vertex at y = 20 is a reading; the conclusions hold for any y between about 5 and 45):
    /// the middle vertex is 20 from the line through the end points but about 201 from the segment between them. The
    /// polyline turns back by about 177 degrees at the middle vertex.
    /// </summary>
    public static List<Point> EbischFigure3() => new() { new(200, 0), new(0, 20), new(400, 0) };

    public const double EbischFigure3Tolerance = 50;

    /// <summary>
    /// Visvalingam, M. (2015) Testing implementations of Visvalingam's algorithm for line generalisation. University
    /// of Hull, Appendix "Output from Visvalingam's program" (p. 16/16): the level-1 quadratic Koch island (teragon),
    /// 33 vertices, the first repeated at the end, with the effective area (EA) at which her program eliminates each
    /// vertex. The sign of EA only encodes the turn direction.
    /// </summary>
    public static readonly (double X, double Y, double EffectiveArea)[] KochIsland =
    {
        (0, 0, 9.00), (0, 1, 0.50), (1, 1, -0.50), (1, 2, -2.00), (0, 2, 0.00), (-1, 2, 2.00), (-1, 3, 0.50),
        (0, 3, -0.50), (0, 4, 8.00), (1, 4, 0.50), (1, 3, -0.50), (2, 3, -2.00), (2, 4, 0.00), (2, 5, 2.00),
        (3, 5, 0.50), (3, 4, -0.50), (4, 4, 8.00), (4, 3, 0.50), (3, 3, -0.50), (3, 2, -2.00), (4, 2, 0.00),
        (5, 2, 2.00), (5, 1, 0.50), (4, 1, -0.50), (4, 0, 8.00), (3, 0, 0.50), (3, 1, -0.50), (2, 1, -2.00),
        (2, 0, 0.00), (2, -1, 2.00), (1, -1, 0.50), (1, 0, -0.50), (0, 0, 9.00),
    };

    /// <summary>The Koch island of <see cref="KochIsland"/> as an open line (33 points, closing vertex repeated).</summary>
    public static List<Point> KochIslandLine() => KochIsland.Select(v => new Point(v.X, v.Y)).ToList();
}
