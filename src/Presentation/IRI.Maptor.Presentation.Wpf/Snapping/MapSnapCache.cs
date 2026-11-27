using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf.Layers;

namespace IRI.Maptor.Presentation.Wpf.Snapping;

/// <summary>
/// The <see cref="SnapIndex{T}"/> for the current map extent, built from the features of the layers to snap to. It
/// loads and indexes; it does not snap.
/// <list type="bullet">
/// <item>Called on the UI thread: map layers are read there (they are not thread-safe); the index is built on a worker
/// thread.</item>
/// <item>A rebuild for a new extent keeps the old index in use until the new one is ready (its geometries are still
/// real); a change of layers drops it at once, so an unchecked layer is never snapped to.</item>
/// <item>A build that is overtaken by a newer request is thrown away.</item>
/// <item>Geometries are in Web Mercator; SRID 0 is taken as Web Mercator, WGS84 is converted, any other SRID is
/// skipped.</item>
/// </list>
/// </summary>
public sealed class MapSnapCache
{
    /// <summary>Above this many vertices in view nothing is indexed and <see cref="IsOverLimit"/> is set.</summary>
    public const int MaxVertices = 50_000;

    // features a little outside the view can still be snapped to near its border
    private const double ExtentPadding = 0.1;

    private int _version;
    private BoundingBox? _requestedExtent;

    /// <summary>The index to snap with, or null while none is ready.</summary>
    public SnapIndex<Point>? Index { get; private set; }

    /// <summary>The geometries <see cref="Index"/> was built from, in its order (a result's GeometryIndex points here).</summary>
    public IReadOnlyList<Geometry<Point>?> Geometries { get; private set; } = Array.Empty<Geometry<Point>?>();

    public bool IsOverLimit { get; private set; }

    /// <summary>Raised on the UI thread when a new index (or none, over the limit) is in place.</summary>
    public event Action? Built;

    /// <summary>Drops the index; the next <see cref="EnsureFor"/> rebuilds it.</summary>
    public void Invalidate()
    {
        _version++;
        _requestedExtent = null;
        Index = null;
        Geometries = Array.Empty<Geometry<Point>?>();
    }

    /// <summary>Starts a rebuild unless the index already is (or is being) built for <paramref name="extent"/>.</summary>
    public void EnsureFor(BoundingBox extent, IReadOnlyList<VectorLayer> layers)
    {
        if (_requestedExtent is { } requested && requested.Equals(extent))
            return;

        _requestedExtent = extent;

        _ = BuildAsync(++_version, extent, layers);
    }

    private async Task BuildAsync(int version, BoundingBox extent, IReadOnlyList<VectorLayer> layers)
    {
        try
        {
            var query = Padded(extent).AsGeometry<Point>(SridHelper.WebMercator);

            var geometries = new List<Geometry<Point>?>();
            var vertices = 0;
            var isComplete = true;

            foreach (var layer in layers)
            {
                if (layer.DataSource is { IsInitializing: true })
                {
                    // try again on the next request instead of keeping a cache without this layer
                    isComplete = false;
                    continue;
                }

                var set = await layer.GetFeaturesAsync(query);

                if (version != _version)
                    return;

                foreach (var feature in set?.Features ?? Array.Empty<Feature<Point>>())
                {
                    var geometry = ToWebMercator(feature.TheGeometry);

                    if (geometry is null)
                        continue;

                    vertices += geometry.TotalNumberOfPoints;

                    if (vertices > MaxVertices)
                    {
                        Finish(version, null, Array.Empty<Geometry<Point>?>(), isOverLimit: true, isComplete);
                        return;
                    }

                    geometries.Add(geometry);
                }
            }

            var index = await Task.Run(() => new SnapIndex<Point>(geometries));

            Finish(version, index, geometries, isOverLimit: false, isComplete);
        }
        catch (Exception)
        {
            // snapping is a help, never a reason for drawing to fail: without an index the point is placed as clicked.
            // Not retried for this extent, so a failing layer is not read again on every mouse move.
            Finish(version, null, Array.Empty<Geometry<Point>?>(), isOverLimit: false, isComplete: true);
        }
    }

    private void Finish(int version, SnapIndex<Point>? index, IReadOnlyList<Geometry<Point>?> geometries, bool isOverLimit, bool isComplete)
    {
        if (version != _version)
            return;

        Index = index;
        Geometries = geometries;
        IsOverLimit = isOverLimit;

        if (!isComplete)
            _requestedExtent = null;

        Built?.Invoke();
    }

    private static Geometry<Point>? ToWebMercator(Geometry<Point>? geometry)
    {
        if (geometry is null || geometry.IsNullOrEmpty())
            return null;

        if (geometry.Srid == SridHelper.WebMercator || geometry.Srid == SridHelper.EsriWebMercator || geometry.Srid == 0)
            return geometry;

        if (geometry.Srid == SridHelper.GeodeticWGS84)
            return geometry.Transform(MapProjects.GeodeticWgs84ToWebMercator, SridHelper.WebMercator);

        return null;
    }

    private static BoundingBox Padded(BoundingBox extent)
    {
        var dx = extent.Width * ExtentPadding;
        var dy = extent.Height * ExtentPadding;

        return new BoundingBox(extent.XMin - dx, extent.YMin - dy, extent.XMax + dx, extent.YMax + dy);
    }
}
