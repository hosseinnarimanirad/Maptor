using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.MachineLearning;

/// <summary>
/// One synthetic triad labelled by a rasterization oracle: the three-point line and its two-point version
/// were drawn with the renderer of the visual metric and the drawn pixels compared. The label is not stored;
/// it is derived from <see cref="DifferentPixels"/> for any tolerance (<see cref="IsRetained"/>), so one file
/// serves a whole family of models. Coordinates are screen pixels on the labelling canvas.
/// </summary>
public class OracleLabelledTriad
{
    /// <summary>Base length in pixels (distance first–last).</summary>
    public double BaseLength { get; set; }

    /// <summary>Grid cell in the unrotated frame, as in <see cref="SyntheticDataFactory"/>: x of the middle point minus x of the first point.</summary>
    public double Dx1 { get; set; }

    /// <summary>x of the middle point minus x of the last point.</summary>
    public double Dx2 { get; set; }

    /// <summary>Signed offset of the middle point from the base line.</summary>
    public double Dy { get; set; }

    /// <summary>Orientation of the base on the canvas.</summary>
    public double RotationDegrees { get; set; }

    /// <summary>Sub-pixel shift of the whole triad against the pixel grid.</summary>
    public double OffsetX { get; set; }

    public double OffsetY { get; set; }

    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public double X3 { get; set; }
    public double Y3 { get; set; }

    /// <summary>Drawn pixels of the three-point line.</summary>
    public int OriginalPixels { get; set; }

    /// <summary>Drawn pixels of the two-point line.</summary>
    public int SimplifiedPixels { get; set; }

    /// <summary>Pixels drawn in exactly one of the two rasters (symmetric difference).</summary>
    public int DifferentPixels { get; set; }

    /// <summary>
    /// The same three counts with a stricter notion of "drawn": only pixels at least half covered by the
    /// anti-aliased stroke (alpha ≥ 128), which turns the 1-px anti-aliased line into a 1-px solid line and
    /// ignores faint fringe pixels.
    /// </summary>
    public int OriginalPixelsOpaque { get; set; }

    public int SimplifiedPixelsOpaque { get; set; }

    public int DifferentPixelsOpaque { get; set; }

    /// <summary>
    /// Largest distance, in pixels, from any drawn pixel of either raster to the other raster (the Hausdorff
    /// distance of the two rasters, <see cref="Core.Common.Mathematics.RasterComparison.Hausdorff"/>): how far the
    /// drawn line moves when the middle point is removed. Zero when the rasters coincide; a one-pixel shift of
    /// the whole line gives 1 (or √2 diagonally) whatever its length, whereas <see cref="DifferentPixels"/> grows
    /// with the length. Files written before this field existed hold 0 here.
    /// </summary>
    public double MaxDisplacement { get; set; }

    /// <summary>Mean squared distance beyond the 8-neighbourhood over all pixels of both rasters (<see cref="Core.Common.Mathematics.RasterComparison.ExcessDisplacement"/> at tolerance 1).</summary>
    public double ExcessDisplacement { get; set; }

    /// <summary>The middle point is retained when removing it moves some drawn pixel farther than <paramref name="maxPixels"/> (Euclidean, so 1 excludes diagonal neighbours and √2 allows them).</summary>
    public bool IsRetainedByDisplacement(double maxPixels) => MaxDisplacement > maxPixels + 1e-9;

    /// <summary>The middle point is retained when removing it changes more than <paramref name="tolerancePixels"/> drawn pixels (any coverage).</summary>
    public bool IsRetained(int tolerancePixels) => DifferentPixels > tolerancePixels;

    /// <summary>As <see cref="IsRetained"/>, counting only pixels at least half covered.</summary>
    public bool IsRetainedOpaque(int tolerancePixels) => DifferentPixelsOpaque > tolerancePixels;

    /// <summary>Retained when the changed pixels exceed <paramref name="fraction"/> of the original's drawn pixels (scale-free tolerance).</summary>
    public bool IsRetainedRelative(double fraction) => DifferentPixels > fraction * OriginalPixels;

    public Point First => new Point(X1, Y1);

    public Point Middle => new Point(X2, Y2);

    public Point Last => new Point(X3, Y3);
}

/// <summary>
/// A set of oracle-labelled triads with the settings that produced it. Created by the labeller of the
/// research tool (<c>OracleLabeller</c> in IRI.Maptor.Res.LRSimplification), consumed through
/// <see cref="ToTrainingData"/>.
/// </summary>
public class OracleSyntheticDataFile
{
    /// <summary>Free-text description of the renderer the labels come from (pen, smoothing, pixel rule).</summary>
    public string Renderer { get; set; }

    public int CanvasSize { get; set; }

    public double GridStep { get; set; }

    public double Margin { get; set; }

    public List<double> BaseLengths { get; set; } = new List<double>();

    public List<double> RotationsDegrees { get; set; } = new List<double>();

    /// <summary>Sub-pixel offsets, each as [x, y].</summary>
    public List<double[]> Offsets { get; set; } = new List<double[]>();

    public List<OracleLabelledTriad> Triads { get; set; } = new List<OracleLabelledTriad>();

    /// <summary>
    /// Training records for the given tolerance and features; every triad yields one record with the label
    /// <see cref="OracleLabelledTriad.IsRetained"/>. <paramref name="filter"/> selects a subset (e.g. one
    /// rotation).
    /// </summary>
    public LRSimplificationTrainingData<Point> ToTrainingData(int tolerancePixels, List<LRSimplificationFeatures> features, Func<OracleLabelledTriad, bool> filter = null)
        => ToTrainingData(t => t.IsRetained(tolerancePixels), features, filter);

    /// <summary>Training records with an arbitrary labelling rule over the stored pixel counts.</summary>
    public LRSimplificationTrainingData<Point> ToTrainingData(Func<OracleLabelledTriad, bool> isRetained, List<LRSimplificationFeatures> features, Func<OracleLabelledTriad, bool> filter = null)
    {
        var records = new List<LRSimplificationParameters<Point>>();

        foreach (var triad in Triads)
        {
            if (filter != null && !filter(triad))
                continue;

            records.Add(new LRSimplificationParameters<Point>(triad.First, triad.Middle, triad.Last, features)
            {
                IsRetained = isRetained(triad),
            });
        }

        return new LRSimplificationTrainingData<Point>(records);
    }

    public void Save(string fileName)
    {
        System.IO.File.WriteAllText(fileName, JsonHelper.Serialize(this));
    }

    public static OracleSyntheticDataFile Load(string fileName)
    {
        if (!System.IO.File.Exists(fileName))
            return null;

        return JsonHelper.Deserialize<OracleSyntheticDataFile>(System.IO.File.ReadAllText(fileName));
    }
}
