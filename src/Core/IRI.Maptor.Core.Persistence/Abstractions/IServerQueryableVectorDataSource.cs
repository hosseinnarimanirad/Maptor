using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Dtos;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Persistence.Abstractions;

/// <summary>
/// A vector source whose features stay on a server and can be queried there (a web layer in Live mode): the
/// attribute table pages, filters, sorts and counts through it instead of loading the whole layer.
/// </summary>
public interface IServerQueryableVectorDataSource : IVectorDataSource
{
    /// <summary>True when queries go to the server; false (e.g. a layer in Cache mode) means: work in memory.</summary>
    bool CanQueryServer { get; }

    /// <summary>
    /// One page of features matching <paramref name="query"/>, in the server's order. The features are the
    /// source's own objects: a feature with pending edits is returned as edited (its local version and status),
    /// and every returned feature can be edited and saved through the source.
    /// </summary>
    Task<FeatureQueryPage> QueryPageAsync(FeatureQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>The distinct values of <paramref name="fieldName"/> over the whole layer (server values; unsaved edits are not included).</summary>
    Task<DistinctValuesDto> GetDistinctValuesAsync(string fieldName, CancellationToken cancellationToken = default);

    /// <summary>The statistics of one attribute over the whole layer, computed by the server (unsaved edits are not included).</summary>
    Task<FieldStatisticsDto> GetFieldStatisticsAsync(string fieldName, CancellationToken cancellationToken = default);

    /// <summary>
    /// The layer's features grouped by <paramref name="fieldName"/>, with the total of <paramref name="sumFieldName"/> (optional)
    /// and of the geodesic <paramref name="measureKind"/> (<see cref="GeometryMeasureKinds"/>; null for none) per group.
    /// </summary>
    Task<GroupStatisticsDto> GetGroupStatisticsAsync(string fieldName, string? sumFieldName, string? measureKind, CancellationToken cancellationToken = default);

    /// <summary>Empty geometries and the per-feature geodesic <paramref name="measureKind"/> (<see cref="GeometryMeasureKinds"/>; null for none).</summary>
    Task<GeometryStatisticsDto> GetGeometryStatisticsAsync(string? measureKind, CancellationToken cancellationToken = default);

    /// <summary>
    /// Some area drawn at the current zoom has more features than the server sends at once, so the map shows
    /// only part of them there: the user should zoom in. Reset when the map changes zoom.
    /// </summary>
    bool IsViewTruncated { get; }

    event EventHandler<bool>? ViewTruncatedChanged;
}

public class FeatureQueryPage
{
    public IReadOnlyList<Feature<Point>> Features { get; set; } = new List<Feature<Point>>();

    /// <summary>All matching features on the server; set when the query asked for the count.</summary>
    public int? TotalCount { get; set; }

    public int PageIndex { get; set; }

    /// <summary>The page size the server used (it may lower the requested one).</summary>
    public int PageSize { get; set; }
}
