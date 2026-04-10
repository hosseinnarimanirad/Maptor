using System.Threading;
using System.Threading.Tasks;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Ket.WebApiPersistence.DTOs;
using IRI.Maptor.Sta.Persistence.Abstractions;

namespace IRI.Maptor.Ket.WebApiPersistence;

public class WebApiDataSource : MemoryDataSource
{
    public override DataSourceKind DataSourceKind => DataSourceKind.WebApi;

    //private const string _listEndPoint = "LIST";
    //private const string _updateEndPoint = "UPDATE";

    protected WebApiSourceParameter _parameters;

    public string? IdColumnName { get; set; }

    protected WebApiDataSource() : base()
    {
        _featureSet = FeatureSet<Point>.Empty;
    }

    public WebApiDataSource(WebApiSourceParameter parameters) : this()
    {
        _parameters = parameters;
        IdColumnName = parameters.IdColumnName;
    }

    public WebApiDataSource(
        string listUrl,
        string syncUrl,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null,
        int srid = SridHelper.WebMercator,
        string? idColumnName = null) : this()
    {
        _parameters = new WebApiSourceParameter(
            listUrl,
            syncUrl,
            bearerToken,
            headers,
            srid,
            idColumnName);

        IdColumnName = idColumnName;
    }

    /// <inheritdoc />
    public override async Task LoadAsync(CancellationToken cancellationToken = default) => await LoadAsync((ListFeaturesQueryParams?)null, cancellationToken);

    /// <summary>
    /// Loads features from the list endpoint and assigns them to _features. Clears change tracking.
    /// </summary>
    public async Task LoadAsync(ListFeaturesQueryParams? queryParams = null, CancellationToken cancellationToken = default)
    {
        IsInitializing = true;
        try
        {
            HasError = false;

            //var listEndpoint = !string.IsNullOrWhiteSpace(_parameters.CustomListPath)
            //    ? _parameters.CustomListPath.TrimStart('/')
            //    : _listEndPoint;

            var featureSetDto = await WebApiInfrastructure.GetFeaturesAsync(
                //_parameters.BaseUrl,
                _parameters.ListUrl,
                queryParams,
                _parameters.BearerToken,
                _parameters.Headers,
                cancellationToken);

            if (featureSetDto == null || featureSetDto.Features == null || featureSetDto.Features.Count == 0)
            {
                _featureSet = FeatureSet<Point>.Empty;
                WebMercatorExtent = BoundingBox.NaN;
            }
            else
            {
                _featureSet = ConvertFeatureSetDtoToFeatureSet(featureSetDto);
            }

            //_addedFeatures.Clear();
            //_updatedFeatures.Clear();
            //_deletedIds.Clear();
            _featureSet.ApplyChanges();

            UpdateHasPendingChanges();
            IsLoaded = true;
        }
        catch
        {
            HasError = true;
            IsLoaded = false;
            throw;
        }
        finally
        {
            IsInitializing = false;
        }
    }

    /// <summary>
    /// Loads features from the list endpoint with an optional server-side geometry filter.
    /// </summary>
    public Task LoadAsync(Geometry<Point>? geometryFilter, CancellationToken cancellationToken = default)
    {
        if (geometryFilter == null || geometryFilter.IsNullOrEmpty())
            return LoadAsync((ListFeaturesQueryParams?)null, cancellationToken);

        var wkbBytes = geometryFilter.AsWkb();
        if (wkbBytes == null)
            return LoadAsync((ListFeaturesQueryParams?)null, cancellationToken);

        var wkbHex = HexStringHelper.ToHexStringUsingBitFiddle(wkbBytes, append0x: false);
        var queryParams = new ListFeaturesQueryParams { GeometryWkbHex = wkbHex };
        return LoadAsync(queryParams, cancellationToken);
    }

    public override async Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry)
    {
        if (_featureSet?.Features == null || _featureSet.Features.Count == 0)
            return FeatureSet<Point>.Empty;

        if (FilterGeometry == null || FilterGeometry.IsNullOrEmpty())
            return await base.GetAsFeatureSetAsync(geometry);

        Predicate<Geometry<Point>> predicate = geometry == null || geometry.IsNullOrEmpty()
            ? g => g.Intersects(FilterGeometry!)
            : g => g.Intersects(FilterGeometry!) && g.Intersects(geometry);
        return await Task.FromResult(_featureSet.FilterByGeometry(predicate));
    }


    public override async Task SaveChanges()
    {
        if (string.IsNullOrWhiteSpace(_parameters.SyncUrl))
        {
            HasError = true;
            return;
        }

        IsProcessing = true;

        try
        {
            HasError = false;

            var dto = new FeatureSetChangesDto
            {
                Added = _featureSet.Features.Where(f => f.Status == Sta.Common.Enums.FeatureStatus.New).Select(ConvertFeatureToFeatureDto).ToList(),
                Updated = _featureSet.Features.Where(f => f.Status == Sta.Common.Enums.FeatureStatus.Updated).Select(ConvertFeatureToFeatureDto).ToList(),
                DeletedIds = _featureSet.GetDeletedFeatureIds().ToList(),
            };

            var syncResult = await WebApiInfrastructure.SaveChangesAsync(
                //_parameters.BaseUrl,
                //_updateEndPoint,
                _parameters.SyncUrl,
                dto,
                _parameters.BearerToken,
                _parameters.Headers);

            if (syncResult is not null)
            {
                if (syncResult.NewIds != null && syncResult.NewIds.Count > 0)
                {
                    foreach (var mapping in syncResult.NewIds)
                    {
                        if (mapping.Key == Guid.Empty)
                            continue;

                        var feature = _featureSet.Features.FirstOrDefault(f => f.Key == mapping.Key);
                        if (feature != null)
                        {
                            feature.Id = mapping.Id;
                        }
                    }
                }

                //_addedFeatures.Clear();
                //_updatedFeatures.Clear();
                //_deletedIds.Clear();
                _featureSet.ApplyChanges();
                UpdateHasPendingChanges();
            }
            else
            {
                HasError = true;
            }
        }
        catch
        {
            HasError = true;
            throw;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    public override Task<FeatureSet<Point>> SearchAsync(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return Task.FromResult(FeatureSet<Point>.Empty);

        if (_featureSet?.Features == null || _featureSet.Features.Count == 0)
            return Task.FromResult(FeatureSet<Point>.Empty);

        var lower = searchText.ToLowerInvariant();
        var matching = _featureSet.Features.Where(f =>
            f.Attributes != null &&
            f.Attributes.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(lower) == true)).ToList();

        if (matching.Count == 0)
            return Task.FromResult(FeatureSet<Point>.Empty);

        var result = FeatureSet<Point>.Create(string.Empty, matching);
        result.Fields = _featureSet.Fields;
        return Task.FromResult(result);
    }

    #region Conversion Helpers

    private FeatureSet<Point> ConvertFeatureSetDtoToFeatureSet(FeatureSetDto featureSetDto)
    {
        var features = new List<Feature<Point>>();

        foreach (var featureDto in featureSetDto.Features)
        {
            try
            {
                var feature = ConvertFeatureDtoToFeature(featureDto);
                if (feature != null)
                    features.Add(feature);
            }
            catch
            {
                continue;
            }
        }

        if (features.Count == 0)
            return FeatureSet<Point>.Empty;

        if (featureSetDto.Fields != null && featureSetDto.Fields.Count > 0)
            Fields = featureSetDto.Fields;

        else if (features.Count > 0 && features[0].Attributes != null)
            Fields = Field.FromDictionary(features[0].Attributes);

        if (features.Count > 0 && features[0].TheGeometry != null)
            GeometryType = features[0].TheGeometry.Type;

        if (features.Count > 0)
        {
            var extent = BoundingBox.GetMergedBoundingBox(features.Select(f => f.TheGeometry.GetBoundingBox()));
            if (!double.IsNaN(extent.Width) && !double.IsNaN(extent.Height))
                WebMercatorExtent = extent;
        }

        return FeatureSet<Point>.Create(string.Empty, features);
    }

    private Feature<Point>? ConvertFeatureDtoToFeature(FeatureDto featureDto)
    {
        if (featureDto.Shape == null || featureDto.Shape.Length == 0)
            return null;

        var geometry = Geometry<Point>.FromWkb(featureDto.Shape, _parameters.Srid);
        if (geometry == null || geometry.IsNullOrEmpty())
            return null;

        if (_parameters.Srid != SridHelper.WebMercator)
        {
            if (_parameters.Srid == SridHelper.GeodeticWGS84)
                geometry = geometry.Transform(MapProjects.GeodeticWgs84ToWebMercator, SridHelper.WebMercator);
            else
                geometry = geometry.Transform(p => p, SridHelper.WebMercator);
        }

        var feature = new Feature<Point>(geometry, featureDto.Attributes ?? new Dictionary<string, object>())
        {
            Id = featureDto.Id,
            Key = featureDto.Key != Guid.Empty ? featureDto.Key : Guid.NewGuid()
        };

        if (!string.IsNullOrWhiteSpace(IdColumnName) && featureDto.Attributes != null && featureDto.Attributes.ContainsKey(IdColumnName))
        {
            if (featureDto.Attributes[IdColumnName] is int id)
                feature.Id = id;
        }

        return feature;
    }

    private FeatureDto ConvertFeatureToFeatureDto(Feature<Point> feature)
    {
        var geometry = feature.TheGeometry;

        if (_parameters.Srid != SridHelper.WebMercator && geometry.Srid == SridHelper.WebMercator)
        {
            if (_parameters.Srid == SridHelper.GeodeticWGS84)
                geometry = geometry.Transform(MapProjects.WebMercatorToGeodeticWgs84, SridHelper.GeodeticWGS84);
            else
                geometry = geometry.Transform(p => p, _parameters.Srid);
        }

        var wkbBytes = geometry.AsWkb();

        var isNew = feature.Status == Sta.Common.Enums.FeatureStatus.New;

        return new FeatureDto
        {
            Id = isNew ? 0 : feature.Id,
            Shape = wkbBytes ?? Array.Empty<byte>(),
            Attributes = feature.Attributes ?? new Dictionary<string, object>(),
            Key = feature.Key
        };
    }

    #endregion
}
