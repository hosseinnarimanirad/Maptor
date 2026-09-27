using IRI.Maptor.Extensions;
using IRI.Maptor.Core.ShapefileFormat;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// The feature-loading protocol shared by all LRS experiments (LAS paper, Section 4.3): read a Web Mercator
/// shapefile, split multi-part geometries into single parts, keep features by point count, optionally
/// draw a seeded random sample, remove consecutive duplicate points and drop features that still contain
/// duplicates.
/// </summary>
public static class ExperimentData
{
    /// <summary>
    /// Fixed seed of the random samples. Until 2026-09-26 the sample was drawn with
    /// <c>OrderBy(Guid.NewGuid())</c>, so the published run cannot be re-drawn; the features it used are
    /// stored next to its outputs as <c>*-F{i}.json</c>.
    /// </summary>
    public const int DefaultSeed = 1403;

    public static List<Geometry<Point>> LoadFeatures(
        string shpFile,
        int minPoints,
        int maxPoints,
        int? sampleSize = null,
        int seed = DefaultSeed,
        bool splitMultiPart = true,
        bool excludeGeometryCollections = false)
    {
        IEnumerable<Geometry<Point>> query = Shapefile.ReadShapes(shpFile).Select(g => g.AsGeometry());

        if (splitMultiPart)
            query = query.SelectMany(g => g.Split(false));

        query = query.Where(g => !g.IsNullOrEmpty());

        if (excludeGeometryCollections)
            query = query.Where(g => g.Type != GeometryType.GeometryCollection);

        // the bounds are exclusive on both sides, as in the published runs
        query = query.Where(g => g.TotalNumberOfPoints > minPoints && g.TotalNumberOfPoints < maxPoints);

        if (sampleSize.HasValue)
        {
            var random = new Random(seed);

            query = query.OrderBy(_ => random.Next()).Take(sampleSize.Value);
        }

        var features = query.ToList();

        foreach (var feature in features)
        {
            feature.Srid = SridHelper.WebMercator;
            feature.RemoveConsecutiveDuplicatePoints();
        }

        return features.Where(f => !f.HasDuplicatePoints()).ToList();
    }
}
