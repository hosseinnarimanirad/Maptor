using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

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
    //bool Update(Feature<Point> oldValue, Feature<Point> newValue);

    bool UpdateGeometry(Feature<Point> feature, Geometry<Point> newGeometry);

    bool UpdateAttributes(Feature<Point> feature, Dictionary<string, object> oldAttributes);

    List<Feature<Point>> GetCurrentChanges();

    void UndoChanges(Feature<Point> feature);

    /// <summary>
    /// Reverts all pending changes on this data source (New and Updated features).
    /// New features are removed; Updated features are reverted to their previous state.
    /// </summary>
    void UndoAllChanges();

    Task SaveChanges();

    int GetNewId();
}
