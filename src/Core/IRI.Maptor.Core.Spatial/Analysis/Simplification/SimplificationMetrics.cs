using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Spatial.Analysis;

// 1401.02.31
public static class SimplificationMetrics
{
    // ref: McMaster, R. B. (1986). A statistical analysis of mathematical measures for linear simplification. The American Cartographer, 13(2), 103-116.
    /// <summary>
    /// Calculate percentage change in the line length (PCLL)
    /// </summary>
    /// <param name="original"></param>
    /// <param name="simplified"></param>
    /// <returns></returns>
    public static double PercentageChangeInLineLength(Geometry<Point> original, Geometry<Point> simplified)
    {
        if (original.IsNullOrEmpty() || simplified.IsNullOrEmpty())
            return 1;

        var originalLength = original.GetEuclideanLength();

        if (originalLength == 0)
            return 1;

        else
            return simplified.GetEuclideanLength() / originalLength;
    }


    // ref: McMaster, R. B. (1986). A statistical analysis of mathematical measures for linear simplification. The American Cartographer, 13(2), 103-116.
    /// <summary>
    /// Calculate percentage change in coordinates (PCC)
    /// </summary>
    /// <param name="original"></param>
    /// <param name="simplified"></param>
    /// <returns></returns>
    public static double PercentageChangeInCoordinates(Geometry<Point> original, Geometry<Point> simplified)
    {
        if (original.IsNullOrEmpty() || simplified.IsNullOrEmpty())
            return 1;

        var originalTotalNumberOfPoints = (double)original.TotalNumberOfPoints;

        if (originalTotalNumberOfPoints == 0)
            return 1;

        else
            return simplified.TotalNumberOfPoints / originalTotalNumberOfPoints;
    }

    // ref: McMaster, R. B. (1986). A statistical analysis of mathematical measures for linear simplification. The American Cartographer, 13(2), 103-116.
    /// <summary>
    /// Calculate percentage change in average number of points per length of line
    /// </summary>
    /// <param name="original"></param>
    /// <param name="simplified"></param>
    /// <returns></returns>
    public static double PercentageChangeInPointDensity(Geometry<Point> original, Geometry<Point> simplified)
    {
        if (original.IsNullOrEmpty() || simplified.IsNullOrEmpty())
            return 1;

        var originalPointDensity = original.CalculatePointDensity();

        if (originalPointDensity == 0)
            return 1;

        else
            return simplified.CalculatePointDensity() / originalPointDensity;
    }


}
