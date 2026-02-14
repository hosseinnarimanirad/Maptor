using System;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Sta.Persistence.Abstractions;

public interface IDataSource
{
    /// <summary>
    /// Kind of data source for UI distinction (e.g. legend, tooltips).
    /// </summary>
    DataSourceKind DataSourceKind { get; }

    /// <summary>
    /// Loads or refreshes the data source asynchronously.
    /// Pre-loaded sources (e.g., in-memory) may return completed task.
    /// </summary>
    Task LoadAsync();
    BoundingBox WebMercatorExtent { get; }

    int Srid { get; }

    ///// <summary>
    ///// True while the data source is performing a long-running operation
    ///// such as loading or saving.
    ///// </summary>
    //bool IsBusy { get; }

    bool IsInitializing { get; }

    bool IsProcessing { get; }

    /// <summary>
    /// True after the initial load operation for this data source
    /// has successfully completed at least once.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// True when there are unsaved add/update/delete changes associated
    /// with this data source.
    /// </summary>
    bool HasPendingChanges { get; }

    /// <summary>
    /// True when a client-side filter (for example a geometry filter)
    /// is applied on top of the underlying dataset.
    /// </summary>
    bool IsClientFiltered { get; }

    /// <summary>
    /// True when the last load or save operation failed.
    /// Cleared after a subsequent successful operation.
    /// </summary>
    bool HasError { get; }

    event EventHandler<bool>? IsInitializingChanged;

    event EventHandler<bool>? IsProcessingChanged;

    event EventHandler<bool>? IsLoadedChanged;

    event EventHandler<bool>? HasPendingChangesChanged;

    event EventHandler<bool>? IsClientFilteredChanged;

    event EventHandler<bool>? HasErrorChanged;
}
