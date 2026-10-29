using System;

namespace IRI.Maptor.Core.Common.Mathematics;

/// <summary>
/// Exact Euclidean distance transform of a binary raster (Felzenszwalb and Huttenlocher 2012, "Distance
/// Transforms of Sampled Functions"): for every cell the squared distance to the nearest set cell, in cell
/// units, computed in O(width × height) by a lower-envelope pass over the columns and then the rows.
/// </summary>
public static class DistanceTransform
{
    // stands in for +infinity; larger than any squared distance in a raster of practical size and safe to subtract
    private const double Far = 1e20;

    /// <summary>
    /// Squared Euclidean distance from every cell of <paramref name="mask"/> to the nearest cell that is
    /// <c>true</c>. When no cell is set every distance is <see cref="double.PositiveInfinity"/>.
    /// </summary>
    public static double[,] SquaredEuclidean(bool[,] mask)
    {
        if (mask == null)
            throw new ArgumentNullException(nameof(mask));

        int width = mask.GetLength(0), height = mask.GetLength(1);

        var result = new double[width, height];

        bool any = false;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                result[x, y] = mask[x, y] ? 0 : Far;

                any |= mask[x, y];
            }

        if (!any)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    result[x, y] = double.PositiveInfinity;

            return result;
        }

        var size = Math.Max(width, height);

        var f = new double[size];
        var d = new double[size];
        var v = new int[size];
        var z = new double[size + 1];

        // columns
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
                f[y] = result[x, y];

            Transform1D(f, d, height, v, z);

            for (int y = 0; y < height; y++)
                result[x, y] = d[y];
        }

        // rows
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
                f[x] = result[x, y];

            Transform1D(f, d, width, v, z);

            for (int x = 0; x < width; x++)
                result[x, y] = d[x];
        }

        return result;
    }

    // one-dimensional squared distance transform of the sampled function f: d[q] = min_p ((q - p)^2 + f[p])
    private static void Transform1D(double[] f, double[] d, int n, int[] v, double[] z)
    {
        int k = 0;

        v[0] = 0;
        z[0] = double.NegativeInfinity;
        z[1] = double.PositiveInfinity;

        for (int q = 1; q < n; q++)
        {
            double s = Intersection(f, q, v[k]);

            while (s <= z[k])
            {
                k--;

                s = Intersection(f, q, v[k]);
            }

            k++;

            v[k] = q;
            z[k] = s;
            z[k + 1] = double.PositiveInfinity;
        }

        k = 0;

        for (int q = 0; q < n; q++)
        {
            while (z[k + 1] < q)
                k++;

            d[q] = (q - v[k]) * (double)(q - v[k]) + f[v[k]];
        }
    }

    // horizontal position where the parabolas rooted at q and p intersect
    private static double Intersection(double[] f, int q, int p)
        => ((f[q] + (double)q * q) - (f[p] + (double)p * p)) / (2.0 * q - 2.0 * p);
}
