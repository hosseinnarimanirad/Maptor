using System.Threading.Tasks;
using IRI.Maptor.Extensions;
using IRI.Maptor.Ket.WebApiPersistence.DTOs;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Ket.WebApiPersistence;

public class WebApiDataSource : VectorDataSource, IEditableVectorDataSource
{
    protected BoundingBox _extent = BoundingBox.NaN;

    protected WebApiSourceParameter _parameters;

    public Action<Feature<Point>>? AddAction;

    public Action<int>? RemoveAction;

    public Action<Feature<Point>>? UpdateAction;

    public string? IdColumnName { get; set; }

    public override BoundingBox WebMercatorExtent
    {
        get
        {
            if (double.IsNaN(_extent.Width) || double.IsNaN(_extent.Height))
            {
                // Try to get extent from first data fetch
                var featureSet = GetAsFeatureSet();
                if (featureSet != null && featureSet.Features.Count > 0)
                {
                    _extent = featureSet.Extent;
                }
            }
            return _extent;
        }
        protected set
        {
            _extent = value;
        }
    }

    public override int Srid { get; protected set; }

    protected WebApiDataSource() : base(new List<Field>())
    {
    }

    public WebApiDataSource(WebApiSourceParameter parameters) : base(new List<Field>())
    {
        _parameters = parameters;
        Srid = parameters.Srid;
        IdColumnName = parameters.IdColumnName;
    }

    public WebApiDataSource(
        string baseUrl,
        string getFeaturesEndpoint,
        string updateFeatureEndpoint,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null,
        int srid = SridHelper.WebMercator,
        string? idColumnName = null) : base(new List<Field>())
    {
        _parameters = new WebApiSourceParameter(
            baseUrl,
            getFeaturesEndpoint,
            updateFeatureEndpoint,
            bearerToken,
            headers,
            srid,
            idColumnName);
        Srid = srid;
        IdColumnName = idColumnName;
    }

    #region GetAsFeatureSet

    public override FeatureSet<Point> GetAsFeatureSet(BoundingBox boundingBox)
    {
        var queryParams = new Dictionary<string, string>
        {
            { "minX", boundingBox.XMin.ToString("R") },
            { "minY", boundingBox.YMin.ToString("R") },
            { "maxX", boundingBox.XMax.ToString("R") },
            { "maxY", boundingBox.YMax.ToString("R") }
        };

        return GetFeatureSetFromApi(queryParams).GetAwaiter().GetResult();
    }

    public override FeatureSet<Point> GetAsFeatureSet(Geometry<Point>? geometry)
    {
        if (geometry == null || geometry.IsNullOrEmpty())
        {
            return GetAsFeatureSet();
        }

        // Convert geometry to WKB hex string for API
        var wkbBytes = geometry.AsWkb();
        if (wkbBytes == null)
        {
            return FeatureSet<Point>.Empty;
        }

        var wkbHex = IRI.Maptor.Sta.Common.Helpers.HexStringHelper.ToHexStringUsingBitFiddle(wkbBytes, append0x: false);

        var queryParams = new Dictionary<string, string>
        {
            { "geometry", wkbHex }
        };

        return GetFeatureSetFromApi(queryParams).GetAwaiter().GetResult();
    }

    public override FeatureSet<Point> Search(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return FeatureSet<Point>.Empty;
        }

        var queryParams = new Dictionary<string, string>
        {
            { "search", searchText }
        };

        return GetFeatureSetFromApi(queryParams).GetAwaiter().GetResult();
    }

    public override async Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox)
    {
        var queryParams = new Dictionary<string, string>
        {
            { "minX", boundingBox.XMin.ToString("R") },
            { "minY", boundingBox.YMin.ToString("R") },
            { "maxX", boundingBox.XMax.ToString("R") },
            { "maxY", boundingBox.YMax.ToString("R") }
        };

        return await GetFeatureSetFromApi(queryParams);
    }

    public override async Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry)
    {
        if (geometry == null || geometry.IsNullOrEmpty())
        {
            return await GetAsFeatureSetAsync();
        }

        // Convert geometry to WKB hex string for API
        var wkbBytes = geometry.AsWkb();
        if (wkbBytes == null)
        {
            return FeatureSet<Point>.Empty;
        }

        var wkbHex = IRI.Maptor.Sta.Common.Helpers.HexStringHelper.ToHexStringUsingBitFiddle(wkbBytes, append0x: false);

        var queryParams = new Dictionary<string, string>
        {
            { "geometry", wkbHex }
        };

        return await GetFeatureSetFromApi(queryParams);
    }

    private async Task<FeatureSet<Point>> GetFeatureSetFromApi(Dictionary<string, string>? queryParameters = null)
    {
        try
        {
            var featureSetDto = await WebApiInfrastructure.GetFeaturesAsync(
                _parameters.BaseUrl,
                _parameters.GetFeaturesEndpoint,
                queryParameters,
                _parameters.BearerToken,
                _parameters.Headers);

            if (featureSetDto == null || featureSetDto.Features == null || featureSetDto.Features.Count == 0)
            {
                return FeatureSet<Point>.Empty;
            }

            return ConvertFeatureSetDtoToFeatureSet(featureSetDto);
        }
        catch (Exception)
        {
            return FeatureSet<Point>.Empty;
        }
    }

    #endregion

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
                {
                    features.Add(feature);
                }
            }
            catch
            {
                // Skip invalid features
                continue;
            }
        }

        if (features.Count == 0)
        {
            return FeatureSet<Point>.Empty;
        }

        // Update Fields from DTO if available
        if (featureSetDto.Fields != null && featureSetDto.Fields.Count > 0)
        {
            this.Fields = featureSetDto.Fields;
        }
        else if (features.Count > 0 && features[0].Attributes != null)
        {
            // Infer fields from first feature's attributes
            this.Fields = Field.FromDictionary(features[0].Attributes);
        }

        // Detect geometry type from first feature
        if (features.Count > 0 && features[0].TheGeometry != null)
        {
            GeometryType = features[0].TheGeometry.Type;
        }

        // Update extent
        if (features.Count > 0)
        {
            var extent = BoundingBox.GetMergedBoundingBox(features.Select(f => f.TheGeometry.GetBoundingBox()));
            if (!double.IsNaN(extent.Width) && !double.IsNaN(extent.Height))
            {
                _extent = extent;
            }
        }

        return FeatureSet<Point>.Create(string.Empty, features);
    }

    private Feature<Point>? ConvertFeatureDtoToFeature(FeatureDto featureDto)
    {
        if (featureDto.Shape == null || featureDto.Shape.Length == 0)
        {
            return null;
        }

        // Parse WKB geometry with source SRID
        var geometry = Geometry<Point>.FromWkb(featureDto.Shape, _parameters.Srid);

        if (geometry == null || geometry.IsNullOrEmpty())
        {
            return null;
        }

        // Transform to WebMercator if source SRID is not WebMercator
        if (_parameters.Srid != SridHelper.WebMercator)
        {
            if (_parameters.Srid == SridHelper.GeodeticWGS84)
            {
                geometry = geometry.Transform(MapProjects.GeodeticWgs84ToWebMercator, SridHelper.WebMercator);
            }
            else
            {
                // For other SRIDs, we assume they're already in WebMercator or need custom transformation
                // This could be extended based on requirements
                geometry = geometry.Transform(p => p, SridHelper.WebMercator);
            }
        }

        var feature = new Feature<Point>(geometry, featureDto.Attributes ?? new Dictionary<string, object>())
        {
            Id = featureDto.Id
        };

        if (!string.IsNullOrWhiteSpace(IdColumnName) && featureDto.Attributes != null && featureDto.Attributes.ContainsKey(IdColumnName))
        {
            if (featureDto.Attributes[IdColumnName] is int id)
            {
                feature.Id = id;
            }
        }

        return feature;
    }

    private FeatureDto ConvertFeatureToFeatureDto(Feature<Point> feature)
    {
        // Convert geometry to WKB
        // First, transform back to source SRID if needed
        var geometry = feature.TheGeometry;

        if (_parameters.Srid != SridHelper.WebMercator && geometry.Srid == SridHelper.WebMercator)
        {
            if (_parameters.Srid == SridHelper.GeodeticWGS84)
            {
                geometry = geometry.Transform(MapProjects.WebMercatorToGeodeticWgs84, SridHelper.GeodeticWGS84);
            }
            else
            {
                // For other SRIDs, transform back (may need custom logic)
                geometry = geometry.Transform(p => p, _parameters.Srid);
            }
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

    #region IEditableVectorDataSource Implementation

    public void Add(Feature<Point> newValue)
    {
        if (AddAction != null)
        {
            AddAction.Invoke(newValue);
            return;
        }

        try
        {
            var featureDto = ConvertFeatureToFeatureDto(newValue);

            var task = WebApiInfrastructure.AddFeatureAsync(
                _parameters.BaseUrl,
                _parameters.GetFeaturesEndpoint,
                featureDto,
                _parameters.BearerToken,
                _parameters.Headers);

            var result = task.GetAwaiter().GetResult();

            if (result != null && result.Id != 0)
            {
                newValue.Id = result.Id;
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public void Remove(Feature<Point> value)
    {
        if (RemoveAction != null && value.Id != 0)
        {
            RemoveAction.Invoke(value.Id);
            return;
        }

        if (value.Id == 0)
        {
            throw new InvalidOperationException("Cannot remove feature without an ID");
        }

        try
        {
            var task = WebApiInfrastructure.DeleteFeatureAsync(
                _parameters.BaseUrl,
                _parameters.UpdateFeatureEndpoint,
                value.Id,
                _parameters.BearerToken,
                _parameters.Headers);

            var success = task.GetAwaiter().GetResult();

            if (!success)
            {
                throw new InvalidOperationException($"Failed to delete feature with ID {value.Id}");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public void Update(Feature<Point> newValue)
    {
        if (UpdateAction != null)
        {
            UpdateAction.Invoke(newValue);
            return;
        }

        if (newValue.Id == 0)
        {
            throw new InvalidOperationException("Cannot update feature without an ID");
        }

        try
        {
            var featureDto = ConvertFeatureToFeatureDto(newValue);

            var task = WebApiInfrastructure.UpdateFeatureAsync(
                _parameters.BaseUrl,
                _parameters.UpdateFeatureEndpoint,
                newValue.Id,
                featureDto,
                _parameters.BearerToken,
                _parameters.Headers);

            var success = task.GetAwaiter().GetResult();

            if (!success)
            {
                throw new InvalidOperationException($"Failed to update feature with ID {newValue.Id}");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public void SaveChanges()
    {
        // For WebAPI datasource, changes are committed immediately via HTTP requests
        // This method is a no-op, but kept for interface compliance
    }

    #endregion
}
