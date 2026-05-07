using System;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.Spatial.IO.EsriJson;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Sta.Spatial.Primitives;


public class Feature<T> : IGeometryAware<T> where T : IPoint, new()
{
    protected const string _defaultLabelAttributeName = "Label";

    public int Id { get; set; }

    public Geometry<T> TheGeometry { get; set; }

    public Dictionary<string, object> Attributes { get; set; }

    public string LabelAttribute { get; set; } = _defaultLabelAttributeName;

    public string Label
    {
        get
        {
            if (Attributes?.ContainsKey(LabelAttribute) == true)
                return Attributes[LabelAttribute]?.ToString() ?? string.Empty;

            return string.Empty;
        }
    }

    public GeometryType GeometryType => TheGeometry?.Type ?? GeometryType.None;

    public int Srid => TheGeometry?.Srid ?? 0;

    public Guid Key { get; set; }

    public Feature()
    {
        if (Key == Guid.Empty)
            Key = Guid.NewGuid();
    }

    public Feature(Geometry<T> geometry) : this(geometry, new Dictionary<string, object>())
    {
    }

    public Feature(Geometry<T> geometry, string label) : this(geometry, new Dictionary<string, object>() { { _defaultLabelAttributeName, label } })
    {
    }

    public Feature(Geometry<T> geometry, Dictionary<string, object> attributes)
    {
        this.TheGeometry = geometry;

        this.Attributes = attributes;
    }


    public GeoJsonFeature AsGeoJsonFeature()
    {
        return new GeoJsonFeature()
        {
            Geometry = TheGeometry.Project(SrsBases.GeodeticWgs84).AsGeoJson(),
            Id = Id.ToString(),
            Properties = Attributes
        };
    }

    public GeoJsonFeature AsGeoJsonFeature(Func<T, T> toWgs84Func, bool isLongitudeFirst)
    {
        return new GeoJsonFeature()
        {
            Geometry = this.TheGeometry.Transform(toWgs84Func, SridHelper.GeodeticWGS84).AsGeoJson(isLongitudeFirst),
            Id = this.Id.ToString(),
            Properties = this.Attributes/*.ToDictionary(k => k.Key, k => k.Value)*/,
        };
    }

    public EsriJsonFeature AsEsriJsonFeature()
    {
        return new EsriJsonFeature()
        {
            Geometry = TheGeometry.AsEsriJsonGeometry(),
            Attributes = this.Attributes,
        };
    }

    public override string ToString() => $"Geometry: {TheGeometry?.Type}, Attributes: {Attributes?.Count}";

    /// <summary>
    /// Returns true if this feature has the same geometry and attributes as the other (by value).
    /// </summary>
    public bool AreTheSame(Feature<T>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        var geometrySame = TheGeometry?.AsWkt() == other.TheGeometry?.AsWkt();
        var attributesSame = DictionaryHelper.AreAttributesEqual(Attributes, other.Attributes);

        return geometrySame && attributesSame;
    }

    public Feature<T> Clone() => new Feature<T>(this.TheGeometry.Clone(), DictionaryHelper.Copy(this.Attributes)) { Id = this.Id, LabelAttribute = this.LabelAttribute, Key = this.Key, };

    public Dictionary<string, object> GetEmptyDictionary() => this.Attributes.Keys.ToDictionary(key => key, key => (object)null);

    public Feature<T> Transform(Func<T, T> transform, int? newSrid = 0)
    {
        return new Feature<T>(TheGeometry.Transform(transform, newSrid ?? TheGeometry.Srid), this.Attributes)
        {
            Id = this.Id,
            LabelAttribute = this.LabelAttribute,
            Key = this.Key,
        };
    }

    public Feature<T> Project(SrsBase targetSrs)
    {
        return new Feature<T>(TheGeometry.Project(targetSrs), this.Attributes)
        {
            Id = this.Id,
            LabelAttribute = this.LabelAttribute,
            Key = this.Key
        };
    }

    #region Change Tracking

    public FeatureStatus Status { get; set; } = FeatureStatus.Unchanged;

    // old version of the current feature if it has been edited
    public Feature<T>? OldVersion { get; set; }

    public DateTime? LastAddOrUpdate { get; set; }

    public void MarkAsRemoved()
    {
        this.OldVersion = null;

        this.LastAddOrUpdate = DateTime.UtcNow;

        this.Status = (this.Status == FeatureStatus.New) ? FeatureStatus.CanceledNew : FeatureStatus.Removed;
    }

    public void MarkAsNew()
    {
        this.LastAddOrUpdate = DateTime.UtcNow;

        this.Status = FeatureStatus.New;
    }

    //public void MarkAsUpdated(Feature<T> newFeature)
    //{
    //    if (newFeature is null)
    //        return;

    //    // Preserve the first version only; do not overwrite OldFeature on subsequent edits.
    //    // This allows Undo to discard all unsaved edits and revert to the original state.
    //    if (newFeature?.Status != FeatureStatus.New &&
    //        this.OldFeature is null)
    //    {
    //        this.OldFeature = this.Clone();
    //    }

    //    this.Attributes = newFeature.Attributes ?? GetEmptyDictionary();

    //    this.TheGeometry = newFeature.TheGeometry;

    //    if (newFeature?.Status == FeatureStatus.New)
    //        return;

    //    this.LastAddOrUpdate = DateTime.UtcNow;

    //    this.Status = FeatureStatus.Updated;
    //}

    public void UpdateAttributes(Dictionary<string, object>? attributes)
    {
        if (this.Status == FeatureStatus.Removed || this.Status == FeatureStatus.CanceledNew)
            return;

        // Preserve the first version only; do not overwrite OldFeature on subsequent edits.
        // This allows Undo to discard all unsaved edits and revert to the original state.
        if (this.Status == FeatureStatus.Unchanged)
            this.OldVersion = this.Clone();

        this.Attributes = attributes ?? GetEmptyDictionary();

        if (this.Status == FeatureStatus.New)
            return;

        MarkAsUpdated();
    }

    public void UpdateOldAttributes(Dictionary<string, object>? attributes)
    {
        if (this.Status == FeatureStatus.Removed || this.Status == FeatureStatus.CanceledNew)
            return;

        // Preserve the first version only; do not overwrite OldFeature on subsequent edits.
        // This allows Undo to discard all unsaved edits and revert to the original state.
        if (this.Status == FeatureStatus.Unchanged)
        {
            this.OldVersion = new Feature<T>(this.TheGeometry.Clone(), attributes ?? this.GetEmptyDictionary())
            {
                Id = this.Id,
                LabelAttribute = this.LabelAttribute,
                Key = this.Key
            };
        }

        if (this.Status == FeatureStatus.New)
            return;

        MarkAsUpdated();
    }

    public bool UpdateGeometry(Geometry<T> newGeometry)
    {
        if (this.Status == FeatureStatus.Removed || this.Status == FeatureStatus.CanceledNew)
            return false;

        if (this.TheGeometry.AsWkt() == newGeometry.AsWkt())
            return false;

        if (this.Status == FeatureStatus.Unchanged)
            this.OldVersion = this.Clone();

        this.TheGeometry = newGeometry;

        if (this.Status == FeatureStatus.New)
            return true;

        MarkAsUpdated();

        return true;
    }

    public void MarkAsUpdated()
    {
        if (this.Status == FeatureStatus.New ||
            this.Status == FeatureStatus.CanceledNew)
            return;

        this.LastAddOrUpdate = DateTime.UtcNow;

        this.Status = FeatureStatus.Updated;

    }

    public void MarkAsSaved()
    {
        this.Status = FeatureStatus.Unchanged;
        this.OldVersion = null;
        this.LastAddOrUpdate = null;
    }

    #endregion


}


