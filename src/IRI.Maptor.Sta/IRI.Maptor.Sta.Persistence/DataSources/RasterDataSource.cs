using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public abstract class RasterDataSource : IRasterDataSource
{
    public virtual BoundingBox WebMercatorExtent { get; protected set; } = BoundingBox.NaN;

    public virtual int Srid => SridHelper.WebMercator;


    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        protected set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;
            IsBusyChanged?.Invoke(this, value);
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

    public event EventHandler<bool>? IsBusyChanged;

    public event EventHandler<bool>? IsLoadedChanged;

    public event EventHandler<bool>? HasPendingChangesChanged;

    public event EventHandler<bool>? IsClientFilteredChanged;

    public event EventHandler<bool>? HasErrorChanged;

}
