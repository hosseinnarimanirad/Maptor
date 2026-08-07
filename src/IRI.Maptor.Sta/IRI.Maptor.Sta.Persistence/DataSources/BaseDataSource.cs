using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Persistence.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public abstract class BaseDataSource : IDataSource
{
    public virtual DataSourceKind DataSourceKind => DataSourceKind.Other;

    private BoundingBox _webMercatorExtent = BoundingBox.NaN;
    public virtual BoundingBox WebMercatorExtent
    {
        get { return _webMercatorExtent; }
        protected set
        {
            _webMercatorExtent = value;

            this.OnExtentChanged?.Invoke(this, value);
        }
    }
    //public virtual BoundingBox WebMercatorExtent { get; protected set; } = BoundingBox.NaN;


    public abstract int Srid { get; /*protected set;*/ }

    // in the case of dxfDataSource, csvDataSource, shapefileDataSource
    // this indicate the original srid of the file that is used when
    // saving to the file
    public virtual int OriginalSrid => Srid;

    /// <summary>
    /// Human-readable description of where the data comes from, for display purposes
    /// (e.g. layer settings). Derived from <see cref="DataSourceKind"/> and
    /// <see cref="Location"/>; override only when the default formatting does not fit.
    /// </summary>
    public virtual string SourceAddress => Location switch
    {
        FileLocation file when !string.IsNullOrEmpty(file.TableName) => $"{DataSourceKind}: {file.Path} ({file.TableName})",
        FileLocation file => $"{DataSourceKind}: {file.Path}",
        DirectoryLocation dir => $"{DataSourceKind}: {dir.Path}",
        WebServiceLocation web => $"{DataSourceKind}: {web.ListUrl}",
        GrpcLocation grpc => $"{DataSourceKind}: {grpc.Endpoint}",
        _ => DataSourceKind.ToString(),
    };

    public virtual SourceLocation? Location => null;

    #region Status Flags

    public event EventHandler<bool>? IsInitializingChanged;

    public event EventHandler<bool>? IsProcessingChanged;

    public event EventHandler<bool>? IsLoadedChanged;

    public event EventHandler<bool>? HasPendingChangesChanged;

    public event EventHandler<bool>? IsClientFilteredChanged;

    public event EventHandler<bool>? HasErrorChanged;

    public event EventHandler<BoundingBox>? OnExtentChanged;


    private bool _isInitializing;
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


    private bool _isProcessing;
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


    private bool _isLoaded;
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


    private bool _hasPendingChanges;
    public virtual bool HasPendingChanges
    {
        get => _hasPendingChanges;
        protected set
        {
            if (_hasPendingChanges == value)
                return;

            _hasPendingChanges = value;
            RaiseHasPendingChangesChanged();
        }
    }

    protected void RaiseHasPendingChangesChanged() => HasPendingChangesChanged?.Invoke(this, HasPendingChanges);


    private bool _hasClientFilter;
    public virtual bool HasClientFilter
    {
        get => _hasClientFilter;
        protected set
        {
            if (_hasClientFilter == value)
                return;

            _hasClientFilter = value;
            IsClientFilteredChanged?.Invoke(this, value);
        }
    }


    private bool _hasError;
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


    #endregion

    public virtual Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
