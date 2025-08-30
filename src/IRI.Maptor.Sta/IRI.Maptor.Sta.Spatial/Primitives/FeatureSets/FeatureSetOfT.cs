using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Sta.Spatial.Primitives;

public class FeatureSet<T> where T : IPoint, new()
{
    public static FeatureSet<T> Empty = new FeatureSet<T>() { Features = new List<Feature<T>>(), Fields = new List<Field>(), LayerId = Guid.Empty };

    public Guid LayerId { get; set; }

    public string Title { get; set; }

    public int Srid { get; set; }

    public List<Field> Fields { get; set; }

    public List<Feature<T>> Features { get; set; }

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
            Features = features,
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
        this.Features.Add(feature);
    }

    public void Remove(Feature<T> feature)
    {
        this.Features.Remove(feature);
    }

    public void Update(Feature<T> newFeature)
    {
        var index = Features.IndexOf(Features.FirstOrDefault(f => f.Id == newFeature.Id));

        if (index < 0)
            return;

        Features[index] = newFeature;
    }



    public override bool Equals(object obj)
    {
        var featureSet = obj as FeatureSet<T>;

        if (featureSet is null)
            return false;

        return featureSet.LayerId == this.LayerId && featureSet.Srid == this.Srid;
    }

    public override int GetHashCode() => this.LayerId.GetHashCode();

    public override string ToString() => $"FeatureSet, FeatureCount:{this.Features?.Count ?? 0}";

}
