using System.IO;
using System.Text.RegularExpressions;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// The revision experiment for Transactions in GIS (2026-09): the same 348 features the published run used
/// (stored next to its outputs as <c>{layer}_WM-F{i}.json</c>, GeoJSON in WGS84), re-run with the protocol of
/// <see cref="SingleFeatureVaryingThresholdExperiment"/>, the revision models of
/// <see cref="LRHelper.GetRevisionModels"/> and the full set of raster measures, followed by the per-layer
/// statistics of <see cref="ExperimentStatistics"/>. Using the published features keeps the rows comparable
/// to the published <c>All-summary.txt</c> feature by feature.
/// </summary>
public static class PublishedFeaturesRevisionExperiment
{
    private static readonly Regex FeatureFile = new(@"^(?<layer>[A-Za-z]+_WM)-F(?<index>\d+)\.json$", RegexOptions.Compiled);

    /// <summary>Features of a published run, grouped by layer name and ordered by their published index.</summary>
    public static SortedDictionary<string, List<(int Index, Geometry<Point> Feature)>> LoadPublishedFeatures(string runFolder)
    {
        var result = new SortedDictionary<string, List<(int, Geometry<Point>)>>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(runFolder, "*.json"))
        {
            var match = FeatureFile.Match(Path.GetFileName(file));

            if (!match.Success)
                continue;

            var layer = match.Groups["layer"].Value;
            var index = int.Parse(match.Groups["index"].Value);

            var feature = GeoJson.LoadFromFile(file).Single().Geometry!.TransformToWeMercator();

            feature.Srid = SridHelper.WebMercator;
            feature.RemoveConsecutiveDuplicatePoints();

            if (!result.TryGetValue(layer, out var list))
                result[layer] = list = new List<(int, Geometry<Point>)>();

            list.Add((index, feature));
        }

        foreach (var list in result.Values)
            list.Sort((a, b) => a.Item1.CompareTo(b.Item1));

        return result;
    }
}
