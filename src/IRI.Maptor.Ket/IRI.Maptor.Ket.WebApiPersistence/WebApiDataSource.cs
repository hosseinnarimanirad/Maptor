using System.Threading;
using System.Threading.Tasks;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Common.Exceptions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Spatial.Dtos;

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
        _webMercatorFeatureSet = FeatureSet<Point>.Empty;
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
                _webMercatorFeatureSet = FeatureSet<Point>.Empty;
                WebMercatorExtent = BoundingBox.NaN;
            }
            else
            {
                if (featureSetDto.Srid == 0)
                {
                    featureSetDto.SetSrid(_parameters.Srid);
                }

                //_featureSet = ConvertFeatureSetDtoToFeatureSet(featureSetDto);
                _webMercatorFeatureSet = featureSetDto.AsFeatureSet(this.IdColumnName);
                this.Fields = _webMercatorFeatureSet.Fields;
                this.GeometryType = _webMercatorFeatureSet.GeometryType;
                this.WebMercatorExtent = _webMercatorFeatureSet.Extent;
            }

            //_addedFeatures.Clear();
            //_updatedFeatures.Clear();
            //_deletedIds.Clear();
            _webMercatorFeatureSet.ApplyChanges();

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
        if (_webMercatorFeatureSet?.Features == null || _webMercatorFeatureSet.Features.Count == 0)
            return FeatureSet<Point>.Empty;

        if (FilterGeometry == null || FilterGeometry.IsNullOrEmpty())
            return await base.GetAsFeatureSetAsync(geometry);

        Predicate<Geometry<Point>> predicate = geometry == null || geometry.IsNullOrEmpty()
            ? g => g.Intersects(FilterGeometry!)
            : g => g.Intersects(FilterGeometry!) && g.Intersects(geometry);

        return await Task.FromResult(_webMercatorFeatureSet.FilterByGeometry(predicate));
    }


    public override async Task SaveChangesAsync()
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
                Added = _webMercatorFeatureSet.Features.Where(f => f.Status == FeatureStatus.New).Select(f => FeatureDto.Parse(f, SridHelper.GeodeticWGS84)).ToList(),
                Updated = _webMercatorFeatureSet.Features.Where(f => f.Status == FeatureStatus.Updated).Select(f => FeatureDto.Parse(f, SridHelper.GeodeticWGS84)).ToList(),
                Deleted = _webMercatorFeatureSet.GetAllFeatures().Where(f => f.Status == FeatureStatus.Removed).Select(f => FeatureDto.Parse(f, SridHelper.GeodeticWGS84)).ToList(),
                DeletedIds = _webMercatorFeatureSet.GetDeletedFeatureIds().ToList(),
            };

            var response = await WebApiInfrastructure.SaveChangesAsync(
                _parameters.SyncUrl,
                dto,
                _parameters.BearerToken,
                _parameters.Headers);

            if (response.IsSuccess)
            {
                var syncResult = response.Result;

                if (syncResult.NewIds != null && syncResult.NewIds.Count > 0)
                {
                    foreach (var mapping in syncResult.NewIds)
                    {
                        if (mapping.Key == Guid.Empty)
                            continue;

                        var feature = _webMercatorFeatureSet.Features.FirstOrDefault(f => f.Key == mapping.Key);

                        if (feature != null)
                        {
                            feature.Id = mapping.Id;
                            ApplyRowVersion(feature, mapping.RowVersion);
                        }
                    }
                }

                if (syncResult.UpdatedRowVersions != null && syncResult.UpdatedRowVersions.Count > 0)
                {
                    foreach (var mapping in syncResult.UpdatedRowVersions)
                    {
                        var feature = _webMercatorFeatureSet.GetAllFeatures().FirstOrDefault(f => f.Key == mapping.Key);
                        if (feature == null && mapping.Id > 0)
                            feature = _webMercatorFeatureSet.GetAllFeatures().FirstOrDefault(f => f.Id == mapping.Id);

                        if (feature != null)
                            ApplyRowVersion(feature, mapping.RowVersion);
                    }
                }

                //_addedFeatures.Clear();
                //_updatedFeatures.Clear();
                //_deletedIds.Clear();
                _webMercatorFeatureSet.ApplyChanges();
                UpdateHasPendingChanges();
            }
            else
            {
                if (response.Error?.Title == "ConcurrencyException")
                {
                    throw new ConcurrencyConflictException(response.ErrorMessage ?? string.Empty);
                }
                else
                {
                    throw new Exception(response.ErrorMessage);
                }
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

        if (_webMercatorFeatureSet?.Features == null || _webMercatorFeatureSet.Features.Count == 0)
            return Task.FromResult(FeatureSet<Point>.Empty);

        var lower = searchText.ToLowerInvariant();
        var matching = _webMercatorFeatureSet.Features.Where(f =>
            f.Attributes != null &&
            f.Attributes.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(lower) == true)).ToList();

        if (matching.Count == 0)
            return Task.FromResult(FeatureSet<Point>.Empty);

        var result = FeatureSet<Point>.Create(string.Empty, matching);

        result.Fields = _webMercatorFeatureSet.Fields;

        return Task.FromResult(result);
    }

    #region Conversion Helpers

    //private FeatureSet<Point> ConvertFeatureSetDtoToFeatureSet(FeatureSetDto featureSetDto)
    //{
    //    var features = new List<Feature<Point>>();

    //    foreach (var featureDto in featureSetDto.Features)
    //    {
    //        try
    //        {
    //            //var feature = ConvertFeatureDtoToFeature(featureDto);
    //            var feature = featureDto.AsFeature(IdColumnName, SridHelper.WebMercator);

    //            if (feature != null)
    //                features.Add(feature);
    //        }
    //        catch
    //        {
    //            continue;
    //        }
    //    }

    //    if (features.Count == 0)
    //        return FeatureSet<Point>.Empty;

    //    if (featureSetDto.Fields != null && featureSetDto.Fields.Count > 0)
    //        Fields = featureSetDto.Fields;

    //    else if (features.Count > 0 && features[0].Attributes != null)
    //        Fields = Field.FromDictionary(features[0].Attributes);

    //    if (features.Count > 0 && features[0].TheGeometry != null)
    //        GeometryType = features[0].GeometryType;

    //    if (features.Count > 0)
    //    {
    //        var extent = BoundingBox.GetMergedBoundingBox(features.Select(f => f.TheGeometry.GetBoundingBox()));
    //        if (!double.IsNaN(extent.Width) && !double.IsNaN(extent.Height))
    //            WebMercatorExtent = extent;
    //    }

    //    var result = FeatureSet<Point>.Create(string.Empty, features);

    //    result.Fields = this.Fields;

    //    return result;
    //}

    //private Feature<Point>? ConvertFeatureDtoToFeature(FeatureDto featureDto)
    //{
    //    if (featureDto.Shape == null || featureDto.Shape.Length == 0)
    //        return null;

    //    var geometry = Geometry<Point>.FromWkb(featureDto.Shape, _parameters.Srid);

    //    if (geometry == null || geometry.IsNullOrEmpty())
    //        return null;

    //    if (_parameters.Srid != SridHelper.WebMercator)
    //    {
    //        if (_parameters.Srid == SridHelper.GeodeticWGS84)
    //            geometry = geometry.Transform(MapProjects.GeodeticWgs84ToWebMercator, SridHelper.WebMercator);

    //        else
    //            geometry = geometry.Transform(p => p, SridHelper.WebMercator);
    //    }

    //    var feature = new Feature<Point>(geometry, featureDto.Attributes ?? new Dictionary<string, object>())
    //    {
    //        Id = featureDto.Id,
    //        Key = featureDto.Key != Guid.Empty ? featureDto.Key : Guid.NewGuid(),
    //    };

    //    if (!string.IsNullOrWhiteSpace(IdColumnName) && featureDto.Attributes != null && featureDto.Attributes.ContainsKey(IdColumnName))
    //    {
    //        if (featureDto.Attributes[IdColumnName] is int id)
    //            feature.Id = id;
    //    }

    //    return feature;
    //}

    //private FeatureDto ConvertFeatureToFeatureDto(Feature<Point> feature)
    //{
    //    var geometry = feature.TheGeometry;

    //    if (_parameters.Srid != SridHelper.WebMercator && geometry.Srid == SridHelper.WebMercator)
    //    {
    //        if (_parameters.Srid == SridHelper.GeodeticWGS84)
    //            geometry = geometry.Transform(MapProjects.WebMercatorToGeodeticWgs84, SridHelper.GeodeticWGS84);
    //        else
    //            geometry = geometry.Transform(p => p, _parameters.Srid);
    //    }

    //    var wkbBytes = geometry.AsWkb();

    //    var isNew = feature.Status == Sta.Common.Enums.FeatureStatus.New;

    //    return new FeatureDto
    //    {
    //        Id = isNew ? 0 : feature.Id,
    //        Shape = wkbBytes ?? Array.Empty<byte>(),
    //        Attributes = feature.Attributes ?? new Dictionary<string, object>(),
    //        Key = feature.Key,
    //        Srid = feature.Srid
    //    };
    //}

    private static void ApplyRowVersion(Feature<Point> feature, byte[]? rowVersion)
    {
        if (feature.Attributes == null)
            feature.Attributes = new Dictionary<string, object>();

        feature.Attributes["RowVersion"] = rowVersion ?? Array.Empty<byte>();
    }

    #endregion
}
