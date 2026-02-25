using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Sta.Spatial.Primitives;

public class FeatureSet<T> where T : IPoint, new()
{
    public static FeatureSet<T> Empty = new FeatureSet<T>() { _allFeatures = new List<Feature<T>>(), Fields = new List<Field>(), LayerId = Guid.Empty };

    private List<Feature<T>> _allFeatures;

    public Guid LayerId { get; set; }

    public string Title { get; set; }

    public int Srid { get; set; }

    public List<Field> Fields { get; set; }

    public IReadOnlyList<Feature<T>> Features => _allFeatures.Where(f => f.Status != Common.Enums.FeatureStatus.Removed &&
                                                                        f.Status != Common.Enums.FeatureStatus.CanceledNew).ToList();

    //public List<Feature<T>> LiveFeatures => Features.Where(f => f.Status != Common.Enums.FeatureStatus.Removed &&
    //                                                            f.Status != Common.Enums.FeatureStatus.CanceledNew).ToList();

    public BoundingBox Extent => BoundingBox.GetMergedBoundingBox(Features.Select(f => f.TheGeometry.GetBoundingBox()));

    public bool IsLabeled() => string.IsNullOrEmpty(this.Features?.FirstOrDefault().LabelAttribute) == true;

    protected FeatureSet() { }

    public FeatureSet<T> FilterByGeometry(Predicate<Geometry<T>> predicate)
    {
        var filteredFeatures = Features.Where(f => predicate(f.TheGeometry)).ToList();

        if (filteredFeatures.IsNullOrEmpty())
            return FeatureSet<T>.Empty;

        var result = Create(string.Empty, filteredFeatures);

        result.Fields = this.Fields;

        return result;
    }

    public static FeatureSet<T> Create(string title, List<Feature<T>> features)
    {
        if (features.IsNullOrEmpty())
            return FeatureSet<T>.Empty;

        if (features.Select(f => f.TheGeometry.Srid).Distinct().Count() > 1)
            throw new NotImplementedException("FeatureSet<TGeometry, TPoint> => same SRID rule violated");

        return new FeatureSet<T>()
        {
            Title = title,
            _allFeatures = features,
            Fields = new List<Field>(),
            Srid = features.SkipWhile(f => f is null || f.TheGeometry.IsNotValidOrEmpty())?.FirstOrDefault()?.TheGeometry.Srid ?? 0,

        };
    }

    public bool HasNoGeometry() => Features.IsNullOrEmpty();

    public List<Geometry<T>> GetGeometries()
    {
        if (HasNoGeometry())
            return new List<Geometry<T>>();

        return Features.Select(f => f.TheGeometry).ToList();
    }

    public List<string> GetLabels()
    {
        if (this.IsLabeled())
        {
            return this.Features.Select(f => f.Label).ToList();
        }
        else
        {
            return new List<string>();
        }
    }

    public FeatureSet<T> Transform(Func<T, T> transform, int? newSrid = 0)
    {
        var result = Create(this.Title, this.Features.Select(f => f.Transform(transform, newSrid)).ToList());

        result.Fields = this.Fields;

        result.LayerId = this.LayerId;

        return result;
    }



    // todo: add geometry type, srid, ... checkes
    public void Add(Feature<T> feature)
    {
        feature.MarkAsNew();

        this._allFeatures.Add(feature);
    }

    public void Remove(Feature<T> feature)
    {
        feature.MarkAsRemoved();

        if (feature.Status == Common.Enums.FeatureStatus.CanceledNew)
        {
            this._allFeatures.Remove(feature);
        }

    }

    //public bool Update(Feature<T> oldFeature, Feature<T> newFeature)
    //{
    //    if (oldFeature.AreTheSame(newFeature))
    //        return false;

    //    var existing = _allFeatures.FirstOrDefault(f => f.Id == newFeature.Id);

    //    if (existing == null ||
    //        existing.Status == Common.Enums.FeatureStatus.Removed ||
    //        existing.Status == Common.Enums.FeatureStatus.CanceledNew)
    //        return false;

    //    existing.MarkAsUpdated(newFeature);
    //    return true;
    //}

    public bool UpdateOldAttributes(Feature<T> feature, Dictionary<string, object> oldAttributes)
    {
        if (_allFeatures.IsNullOrEmpty())
            return false;

        if (!_allFeatures.Contains(feature))
            return false;

        if (feature is null)
            return false;

        feature.UpdateOldAttributes(oldAttributes);

        return true;
    }

    public bool UpdateGeometry(Feature<T> feature, Geometry<T> newGeometry)
    {
        if (_allFeatures.IsNullOrEmpty())
            return false;

        if (!_allFeatures.Contains(feature))
            return false;

        if (feature is null)
            return false;

        return feature.UpdateGeometry(newGeometry);
    }

    // todo: write undo functions too
    // undoRemove
    // undoCanceledNew
    // undoUpdate

    public IEnumerable<Feature<T>> GetCurrentChanges()
    {
        return _allFeatures.Where(a => a.Status != FeatureStatus.Unchanged);
    }

    public void UndoChanges(Feature<T> feature)
    {
        if (_allFeatures.IsNullOrEmpty())
            return;

        if (!_allFeatures.Contains(feature))
            return;

        if (feature is null)
            return;

        if (feature.Status == FeatureStatus.Updated && feature.OldVersion != null)
        {
            feature.UpdateGeometry(feature.OldVersion.TheGeometry);

            feature.UpdateAttributes(feature.OldVersion.Attributes);
        }
        else if (feature.Status == FeatureStatus.Removed)
        {
            // Restore: MarkAsSaved reverts Status to Unchanged so feature reappears in Features.
        }

        feature.MarkAsSaved();
    }

    /// <summary>
    /// Reverts all pending changes. New features are removed; Updated features are reverted to their previous state.
    /// </summary>
    public void UndoAllChanges()
    {
        if (_allFeatures.IsNullOrEmpty())
            return;

        var toProcess = GetCurrentChanges();

        foreach (var feature in toProcess)
        {
            UndoChanges(feature);
        }
    }

    public void ApplyChanges()
    {
        _allFeatures.RemoveAll(f => f.Status == FeatureStatus.Removed || f.Status == FeatureStatus.CanceledNew);

        foreach (var feature in _allFeatures)
        {
            feature.MarkAsSaved();
        }
    }

    public bool UpdateHasPendingChanges() => GetCurrentChanges().Any();

    public int GetPendingChangeCounts(FeatureStatus status) => _allFeatures?.Count(f => f.Status == status) ?? 0;

    /// <summary>
    /// Returns IDs of features marked as Removed. Used when building sync DTOs to send deleted IDs to the server.
    /// </summary>
    public IEnumerable<int> GetDeletedFeatureIds() =>
        _allFeatures?.Where(f => f.Status == FeatureStatus.Removed).Select(f => f.Id) ?? Enumerable.Empty<int>();


    public override bool Equals(object obj)
    {
        var featureSet = obj as FeatureSet<T>;

        if (featureSet is null)
            return false;

        return featureSet.LayerId == this.LayerId && featureSet.Srid == this.Srid;
    }

    public override int GetHashCode() => this.LayerId.GetHashCode();

    public override string ToString() => $"FeatureSet, FeatureCount:{Features?.Count ?? 0} (total:{_allFeatures?.Count ?? 0})";

}
