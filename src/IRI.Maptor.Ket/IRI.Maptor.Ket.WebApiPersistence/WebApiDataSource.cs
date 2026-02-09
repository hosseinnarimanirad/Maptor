using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Ket.WebApiPersistence.DTOs;

namespace IRI.Maptor.Ket.WebApiPersistence;

public class WebApiDataSource : MemoryDataSource
{
    private const string _listEndPoint = "LIST";
    private const string _updateEndPoint = "UPDATE";

    protected WebApiSourceParameter _parameters;

    public string? IdColumnName { get; set; }

    private bool _isLoading;
    private bool _hasPendingChanges;
    private bool _isSaving;
    private bool _isClientFiltered;
    private Geometry<Point>? _filterGeometry;

    /// <summary>
    /// True while features are being loaded from the list endpoint.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        protected set
        {
            if (_isLoading == value) return;

            _isLoading = value;

            IsLoadingChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<bool>? IsLoadingChanged;

    /// <summary>
    /// True when there are unsaved add/update/delete changes.
    /// </summary>
    public bool HasPendingChanges
    {
        get => _hasPendingChanges;
        protected set
        {
            if (_hasPendingChanges == value) return;

            _hasPendingChanges = value;

            HasPendingChangesChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<bool>? HasPendingChangesChanged;

    /// <summary>
    /// True while SaveChanges is in progress.
    /// </summary>
    public bool IsSaving
    {
        get => _isSaving;
        protected set
        {
            if (_isSaving == value) return;

            _isSaving = value;

            IsSavingChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<bool>? IsSavingChanged;

    /// <summary>
    /// True when FilterGeometry is set and client-side filtering is applied.
    /// </summary>
    public bool IsClientFiltered
    {
        get => _isClientFiltered;

        protected set
        {
            if (_isClientFiltered == value) return;

            _isClientFiltered = value;

            IsClientFilteredChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<bool>? IsClientFilteredChanged;

    /// <summary>
    /// Optional geometry used to filter features client-side when reading. When set, IsClientFiltered becomes true.
    /// </summary>
    public Geometry<Point>? FilterGeometry
    {
        get => _filterGeometry;
        set
        {
            _filterGeometry = value;

            IsClientFiltered = _filterGeometry != null && !_filterGeometry.IsNullOrEmpty();
        }
    }

    private readonly List<Feature<Point>> _addedFeatures = new List<Feature<Point>>();
    private readonly List<Feature<Point>> _updatedFeatures = new List<Feature<Point>>();
    private readonly List<int> _deletedIds = new List<int>();

    private void UpdateHasPendingChanges()
    {
        HasPendingChanges = _addedFeatures.Count > 0 || _updatedFeatures.Count > 0 || _deletedIds.Count > 0;
    }

    protected WebApiDataSource() : base()
    {
        _features = FeatureSet<Point>.Empty;
    }

    public WebApiDataSource(WebApiSourceParameter parameters) : this()
    {
        _parameters = parameters;
        IdColumnName = parameters.IdColumnName;
    }

    public WebApiDataSource(
        string baseUrl,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null,
        int srid = SridHelper.WebMercator,
        string? idColumnName = null) : this()
    {
        _parameters = new WebApiSourceParameter(
            baseUrl,
            bearerToken,
            headers,
            srid,
            idColumnName);
        IdColumnName = idColumnName;
    }

    /// <summary>
    /// Loads features from the list endpoint and assigns them to _features. Clears change tracking.
    /// </summary>
    public async Task LoadAsync(ListFeaturesQueryParams? queryParams = null)
    {
        IsLoading = true;
        try
        {
            var featureSetDto = await WebApiInfrastructure.GetFeaturesAsync(
                _parameters.BaseUrl,
                _listEndPoint,
                queryParams,
                _parameters.BearerToken,
                _parameters.Headers);

            if (featureSetDto == null || featureSetDto.Features == null || featureSetDto.Features.Count == 0)
            {
                _features = FeatureSet<Point>.Empty;
            }
            else
            {
                _features = ConvertFeatureSetDtoToFeatureSet(featureSetDto);
            }

            _addedFeatures.Clear();
            _updatedFeatures.Clear();
            _deletedIds.Clear();
            UpdateHasPendingChanges();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads features from the list endpoint with an optional server-side geometry filter.
    /// </summary>
    public Task LoadAsync(Geometry<Point>? geometryFilter)
    {
        if (geometryFilter == null || geometryFilter.IsNullOrEmpty())
            return LoadAsync((ListFeaturesQueryParams?)null);

        var wkbBytes = geometryFilter.AsWkb();
        if (wkbBytes == null)
            return LoadAsync((ListFeaturesQueryParams?)null);

        var wkbHex = HexStringHelper.ToHexStringUsingBitFiddle(wkbBytes, append0x: false);
        var queryParams = new ListFeaturesQueryParams { GeometryWkbHex = wkbHex };
        return LoadAsync(queryParams);
    }

    public override FeatureSet<Point> GetAsFeatureSet(Geometry<Point>? geometry)
    {
        if (_features?.Features == null || _features.Features.Count == 0)
            return FeatureSet<Point>.Empty;

        if (FilterGeometry == null || FilterGeometry.IsNullOrEmpty())
            return base.GetAsFeatureSet(geometry);

        Predicate<Geometry<Point>> predicate = geometry == null || geometry.IsNullOrEmpty()
            ? g => g.Intersects(FilterGeometry!)
            : g => g.Intersects(FilterGeometry!) && g.Intersects(geometry);
        return _features.FilterByGeometry(predicate);
    }

    public override void Add(Feature<Point> newGeometry)
    {
        base.Add(newGeometry);
        _addedFeatures.Add(newGeometry);
        UpdateHasPendingChanges();
    }

    public override void Update(Feature<Point> newGeometry)
    {
        base.Update(newGeometry);
        if (!_addedFeatures.Any(a => object.ReferenceEquals(a, newGeometry)))
            _updatedFeatures.Add(newGeometry);
        UpdateHasPendingChanges();
    }

    public override void Remove(Feature<Point> geometry)
    {
        if (_addedFeatures.Remove(geometry))
        {
            base.Remove(geometry);
            UpdateHasPendingChanges();
            return;
        }
        _deletedIds.Add(geometry.Id);
        base.Remove(geometry);
        UpdateHasPendingChanges();
    }

    public override void SaveChanges()
    {
        IsSaving = true;
        try
        {
            var dto = new FeatureSetChangesDto
            {
                Added = _addedFeatures.Select(ConvertFeatureToFeatureDto).ToList(),
                Updated = _updatedFeatures
                    .Where(u => !_addedFeatures.Any(a => object.ReferenceEquals(a, u)))
                    .Select(ConvertFeatureToFeatureDto)
                    .ToList(),
                DeletedIds = new List<int>(_deletedIds)
            };

            var success = WebApiInfrastructure.SaveChangesAsync(
                _parameters.BaseUrl,
                _updateEndPoint,
                dto,
                _parameters.BearerToken,
                _parameters.Headers).GetAwaiter().GetResult();

            if (success)
            {
                _addedFeatures.Clear();
                _updatedFeatures.Clear();
                _deletedIds.Clear();
                UpdateHasPendingChanges();
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    public override FeatureSet<Point> Search(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return FeatureSet<Point>.Empty;

        if (_features?.Features == null || _features.Features.Count == 0)
            return FeatureSet<Point>.Empty;

        var lower = searchText.ToLowerInvariant();
        var matching = _features.Features.Where(f =>
            f.Attributes != null &&
            f.Attributes.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(lower) == true)).ToList();

        if (matching.Count == 0)
            return FeatureSet<Point>.Empty;

        var result = FeatureSet<Point>.Create(string.Empty, matching);
        result.Fields = _features.Fields;
        return result;
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
            Id = featureDto.Id
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

        return new FeatureDto
        {
            Id = feature.Id,
            Shape = wkbBytes ?? Array.Empty<byte>(),
            Attributes = feature.Attributes ?? new Dictionary<string, object>()
        };
    }

    #endregion
}
