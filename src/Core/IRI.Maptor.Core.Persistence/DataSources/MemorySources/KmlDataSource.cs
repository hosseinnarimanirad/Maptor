using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Ogc.Kml;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.Persistence.Model;

namespace IRI.Maptor.Core.Persistence.DataSources;

/// <summary>
/// Memory data source that loads from and saves to KML files.
/// </summary>
public class KmlDataSource : MemoryDataSource
{
    private readonly string _fileName;

    // null for pasted KML (RawKml set): there is no file to point at or write
    public override SourceLocation? Location => string.IsNullOrEmpty(_fileName) ? null : new FileLocation { Path = _fileName };

    public override DataSourceKind DataSourceKind => DataSourceKind.Kml;

    public override int OriginalSrid => SridHelper.GeodeticWGS84;

    /// <summary>
    /// The pasted KML when created with <see cref="CreateFromText"/>; null for a file. A project saves it so the
    /// layer can be built again.
    /// </summary>
    public string? RawKml { get; }

    private KmlDataSource(string fileName, List<Feature<Point>> features, string? rawKml = null)
        : base(features, resetIds: true, kind: DataSourceKind.Kml)
    {
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));

        RawKml = rawKml;
    }

    // the sources that share this file (one per sub-layer, see CreateForFile); null for a file with one source
    private IReadOnlyList<KmlDataSource>? _fileSources;

    public override string ToString() => $"{nameof(KmlDataSource)}";

    /// <summary>
    /// Writes the file. When the file was split into sub-layers (<see cref="CreateForFile"/>), the file holds all of
    /// them, so all are written and all their edits count as saved; writing only this one would drop the others.
    /// </summary>
    public override async Task SaveChangesAsync()
    {
        var sources = _fileSources ?? [this];

        // pasted KML has no file: the edits are only accepted in memory, as for pasted GeoJSON
        if (!string.IsNullOrEmpty(_fileName))
        {
            var kmlFeatures = sources.SelectMany(s => s._webMercatorFeatureSet.Features).ToKmlFeatures();

            await KmlWriter.WriteToFileAsync(kmlFeatures, _fileName);
        }

        foreach (var source in sources)
        {
            source._webMercatorFeatureSet.ApplyChanges();

            source.UpdateHasPendingChanges();
        }
    }

    /// <summary>
    /// One source per group of features of the same file (e.g. its points, lines and polygons as sub-layers). They
    /// share the file: saving any of them writes every group.
    /// </summary>
    public static List<KmlDataSource> CreateForFile(string fileName, IEnumerable<List<Feature<Point>>> featureGroups)
        => Link(featureGroups.Select(group => Create(fileName, group)).ToList());

    /// <summary>
    /// One source per group of features of pasted KML (same grouping as <see cref="CreateForFile"/>). Each keeps
    /// <paramref name="rawKml"/>, so a project can rebuild the layer; there is no file, so saving writes nothing.
    /// Features should already be in Web Mercator.
    /// </summary>
    public static List<KmlDataSource> CreateFromText(string rawKml, IEnumerable<List<Feature<Point>>> featureGroups)
    {
        if (string.IsNullOrWhiteSpace(rawKml))
            throw new ArgumentNullException(nameof(rawKml));

        return Link(featureGroups.Select(group =>
        {
            if (group == null || group.Count == 0)
                throw new ArgumentException("At least one feature is required.", nameof(featureGroups));

            return new KmlDataSource(string.Empty, group, rawKml);
        }).ToList());
    }

    private static List<KmlDataSource> Link(List<KmlDataSource> sources)
    {
        if (sources.Count > 1)
        {
            foreach (var source in sources)
                source._fileSources = sources;
        }

        return sources;
    }

    /// <summary>
    /// Creates a KmlDataSource from the given file path and features.
    /// Features should already be in Web Mercator.
    /// </summary>
    public static KmlDataSource Create(string fileName, List<Feature<Point>> features)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName));
        if (features == null || features.Count == 0)
            throw new ArgumentException("At least one feature is required.", nameof(features));

        return new KmlDataSource(fileName, features);
    }
}
