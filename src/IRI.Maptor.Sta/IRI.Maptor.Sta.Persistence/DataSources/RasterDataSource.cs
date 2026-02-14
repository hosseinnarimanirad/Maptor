using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public abstract class RasterDataSource : IRasterDataSource
{
    public virtual DataSourceKind DataSourceKind => DataSourceKind.None;

    public virtual Task LoadAsync() => Task.CompletedTask;
    public virtual BoundingBox WebMercatorExtent { get; protected set; } = BoundingBox.NaN;

    public virtual int Srid => SridHelper.WebMercator;

     
    private bool _isInitializing;
    public bool IsInitializing
    {
        get => _isInitializing;
        protected set
        {
            if (_isInitializing == value)
                return;

            _isInitializing = value;
            IsProcessingChanged?.Invoke(this, value);
        }
    }

    private bool _isProcessing;
    public bool IsProcessing
    {
        get => _isProcessing;
        protected set
        {
            if (_isProcessing == value)
                return;

            _isProcessing = value;
            IsProcessingChanged?.Invoke(this, value);
        }
    }


    private bool _isLoaded;
    public bool IsLoaded
    {
        get => _isLoaded;
        protected set
        {
            if (_isLoaded == value)
                return;

            _isLoaded = value;
            IsLoadedChanged?.Invoke(this, value);
        }
    }

    // Raster data sources are currently read-only and unfiltered,
    // so these flags always remain false for this data source type.
    public bool HasPendingChanges => false;

    public bool IsClientFiltered => false;


    private bool _hasError;
    public bool HasError
    {
        get => _hasError;
        protected set
        {
            if (_hasError == value)
                return;

            _hasError = value;
            HasErrorChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<bool>? IsInitializingChanged;

    public event EventHandler<bool>? IsProcessingChanged;
     
    public event EventHandler<bool>? IsLoadedChanged;

    public event EventHandler<bool>? HasPendingChangesChanged;

    public event EventHandler<bool>? IsClientFilteredChanged;

    public event EventHandler<bool>? HasErrorChanged;
}
