using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public abstract class VectorDataSource : IVectorDataSource
{
    private bool _isInitializing;
    private bool _isProcessing;
    private bool _isLoaded;
    private bool _hasPendingChanges;
    private bool _isClientFiltered;
    private bool _hasError;

    public virtual BoundingBox WebMercatorExtent { get; protected set; }

    public abstract int Srid { get; /*protected set;*/ }

    public virtual GeometryType? GeometryType { get; protected set; }

    public virtual DataSourceKind DataSourceKind => DataSourceKind.Other;

    public List<Field> Fields { get; set; } = new List<Field>();

    public VectorDataSource(List<Field> fields)
    {
        this.Fields = fields;
    }

    public virtual Task LoadAsync() => Task.CompletedTask;

    #region Status Flags
     
    public virtual bool IsInitializing
    {
        get => _isInitializing;
        protected set
        {
            if (_isInitializing == value)
                return;

            _isInitializing = value;
            IsInitializingChanged?.Invoke(this, value);
        }
    }

    public virtual bool IsProcessing
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

    public virtual bool IsLoaded
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

    public virtual bool HasPendingChanges
    {
        get => _hasPendingChanges;
        protected set
        {
            if (_hasPendingChanges == value)
                return;

            _hasPendingChanges = value;
            HasPendingChangesChanged?.Invoke(this, value);
        }
    }

    public virtual bool IsClientFiltered
    {
        get => _isClientFiltered;
        protected set
        {
            if (_isClientFiltered == value)
                return;

            _isClientFiltered = value;
            IsClientFilteredChanged?.Invoke(this, value);
        }
    }

    public virtual bool HasError
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

    #endregion

    #region Get as FeatureSet

    public virtual Task<FeatureSet<Point>> GetAsFeatureSetAsync() => GetAsFeatureSetAsync(Geometry<Point>.Empty);

    public abstract Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox);

    public abstract Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry);

    public virtual Task<FeatureSet<Point>> GetAsFeatureSetAsync(double mapScale, BoundingBox boundingBox) => GetAsFeatureSetAsync(boundingBox);

    #endregion



    public abstract Task<FeatureSet<Point>> SearchAsync(string searchText);
}
