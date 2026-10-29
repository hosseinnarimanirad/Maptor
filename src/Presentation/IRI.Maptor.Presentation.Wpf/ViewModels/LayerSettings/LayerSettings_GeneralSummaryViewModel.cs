using System;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.Spatial.Dtos;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Layers;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.Layers;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.LayerSettings;

/// <summary>
/// The read-only layer facts on the General tab: feature and field counts, geometry type, SRS, extent
/// (WGS84 and UTM) and the Cache/Live mode. Pending edits are not shown here; they have their own tab.
/// </summary>
public class LayerSettings_GeneralSummaryViewModel : Notifier
{
    private const string Dash = "—";

    public bool IsVector { get; }

    /// <summary>Only Web API layers have a Cache/Live mode.</summary>
    public bool HasLoadMode { get; }

    public string LoadModeText { get; } = Dash;

    /// <summary>A Live layer's feature count comes from the server; <see cref="LiveFeatureCountText"/> says so.</summary>
    public bool IsLive { get; }

    public string LiveFeatureCountText => Localize("layerSettings_general_featureCountServer");

    private string _featureCountText = "…";
    public string FeatureCountText
    {
        get => _featureCountText;
        private set
        {
            _featureCountText = value;
            RaisePropertyChanged();
        }
    }

    public string FieldCountText { get; } = Dash;

    public string GeometryTypeText { get; } = Dash;

    public string SrsText { get; } = Dash;

    /// <summary>The SRS the source stores its data in, when it differs from the one the layer works in.</summary>
    public bool HasOriginalSrs { get; }

    public string OriginalSrsText { get; } = Dash;

    public bool HasExtent { get; }

    public string Wgs84LongitudeRange { get; } = Dash;

    public string Wgs84LatitudeRange { get; } = Dash;

    /// <summary>The label of the UTM extent row; the zone is the one of the extent's centre.</summary>
    public string UtmExtentLabel { get; } = string.Empty;

    public string UtmXRange { get; } = Dash;

    public string UtmYRange { get; } = Dash;

    public LayerSettings_GeneralSummaryViewModel(ILayer? layer)
    {
        var dataSource = layer?.DataSource;

        if (dataSource is null)
            return;

        SrsText = FormatSrs(dataSource.Srid);

        if (dataSource.OriginalSrid != dataSource.Srid && dataSource.OriginalSrid != 0)
        {
            HasOriginalSrs = true;
            OriginalSrsText = FormatSrs(dataSource.OriginalSrid);
        }

        var extent = dataSource.WebMercatorExtent;

        if (extent.IsValid())
        {
            HasExtent = true;

            var wgs84 = extent.Transform(MapProjects.WebMercatorToGeodeticWgs84);
            Wgs84LongitudeRange = $"{FormatDegree(wgs84.XMin)} – {FormatDegree(wgs84.XMax)}";
            Wgs84LatitudeRange = $"{FormatDegree(wgs84.YMin)} – {FormatDegree(wgs84.YMax)}";

            var isNorth = wgs84.Center.Y >= 0;
            int zone = MapProjects.FindUtmZone(wgs84.Center.X);
            UtmExtentLabel = string.Format(Localize("layerSettings_general_extentUtm"), $"{zone}{(isNorth ? "N" : "S")}");

            var corners = new[] { wgs84.BottomLeft, wgs84.TopLeft, wgs84.TopRight, wgs84.BottomRight }
                .Select(p => MapProjects.GeodeticToUTM(p, Ellipsoids.WGS84, zone, isNorth))
                .ToList();
            UtmXRange = $"{FormatMeter(corners.Min(p => p.X))} – {FormatMeter(corners.Max(p => p.X))}";
            UtmYRange = $"{FormatMeter(corners.Min(p => p.Y))} – {FormatMeter(corners.Max(p => p.Y))}";
        }

        if (dataSource is not IVectorDataSource vectorSource)
            return;

        IsVector = true;

        GeometryTypeText = vectorSource.GeometryType?.ToString() ?? Localize("layerSettings_general_unknown");

        FieldCountText = FormatCount(vectorSource.Fields?.Count ?? 0);

        if (vectorSource is IServerQueryableVectorDataSource serverSource)
        {
            HasLoadMode = true;
            IsLive = serverSource.CanQueryServer;
            LoadModeText = Localize(IsLive ? "layerSettings_general_loadModeLive" : "layerSettings_general_loadModeCache");
        }

        // a Live layer holds only the features of the current view: the server counts all of them
        if (IsLive)
            _ = CountFeaturesOnServerAsync((IServerQueryableVectorDataSource)vectorSource);
        else if (layer is VectorLayer vectorLayer)
            _ = CountFeaturesAsync(vectorLayer);
        else
            FeatureCountText = Dash;
    }

    private async Task CountFeaturesAsync(VectorLayer layer)
    {
        try
        {
            // off the UI thread: a file source may read the whole file
            var count = await Task.Run(async () => (await layer.GetFeaturesAsync())?.Features.Count ?? 0);
            FeatureCountText = FormatCount(count);
        }
        catch (Exception)
        {
            FeatureCountText = Localize("layerSettings_general_unknown");
        }
    }

    private async Task CountFeaturesOnServerAsync(IServerQueryableVectorDataSource source)
    {
        try
        {
            // one row with the total: the cheapest query that counts the whole layer
            var page = await source.QueryPageAsync(new FeatureQueryDto { PageIndex = 0, PageSize = 1, IncludeCount = true });

            FeatureCountText = page.TotalCount is int count ? FormatCount(count) : Localize("layerSettings_general_unknown");
        }
        catch (Exception)
        {
            FeatureCountText = Localize("layerSettings_general_unknown");
        }
    }

    private static string FormatSrs(int srid)
    {
        if (srid == 0)
            return Localize("layerSettings_general_unknown");

        var title = SridHelper.AsSrsBase(srid)?.Title;

        return string.IsNullOrWhiteSpace(title) ? $"EPSG:{srid}" : $"EPSG:{srid} ({title})";
    }

    private static string Localize(string key) => LocalizationManager.Instance[key];

    private static string FormatCount(int count) => count.ToString("#,0", CultureInfo.InvariantCulture);

    private static string FormatDegree(double value) => value.ToString("0.000000", CultureInfo.InvariantCulture) + "°";

    private static string FormatMeter(double value) => value.ToString("#,0", CultureInfo.InvariantCulture) + " m";
}
