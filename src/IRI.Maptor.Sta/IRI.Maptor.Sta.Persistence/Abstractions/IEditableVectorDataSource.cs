using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Persistence.Abstractions;

public interface IEditableVectorDataSource : IDataSource
{
    /// <summary>Number of features added (status = New).</summary>
    int NumberOfAddedFeatures { get; }

    /// <summary>Number of features deleted (status = Removed), CanceledNew are not considered.</summary>
    int NumberOfDeletedFeatures { get; }

    /// <summary>Number of features updated (status = Updated).</summary>
    int NumberOfUpdatedFeatures { get; }

    void Add(Feature<Point> newValue);

    void Remove(Feature<Point> value);

    /// <summary>
    /// Updates the feature if it has changed. Returns true if the feature was updated, false if old and new are the same.
    /// </summary>
    bool Update(Feature<Point> oldValue, Feature<Point> newValue);

    void SaveChanges();
}
