using System;

using IRI.Maptor.Sta.Spatial.Model;

namespace IRI.Maptor.Jab.Common.Models.Map;

public class MapSettingsModel : Notifier
{
    public MapSettingsModel()
    {
    }

    #region Base Maps

    private string _baseMapCacheDirectory = null;

    public string BaseMapCacheDirectory
    {
        get { return _baseMapCacheDirectory; }
        set
        {
            _baseMapCacheDirectory = value;
            RaisePropertyChanged();

            FireBaseMapCacheDirectoryChanged?.Invoke(value);

        }
    }

    public Action<string> FireBaseMapCacheDirectoryChanged;



    private bool _isBaseMapCacheEnabled;

    public bool IsBaseMapCacheEnabled
    {
        get { return _isBaseMapCacheEnabled; }
        set
        {
            _isBaseMapCacheEnabled = value;
            RaisePropertyChanged();

            FireIsBaseMapCacheEnabledChanged?.Invoke(value);
        }
    }

    public Action<bool> FireIsBaseMapCacheEnabledChanged;


    private Func<TileInfo, string> _getFileName = null;

    public Func<TileInfo, string> GetLocalFileName
    {
        get { return _getFileName; }
        set
        {
            _getFileName = value;
            RaisePropertyChanged();
        }
    }

    #endregion


    #region Zoom

    private bool _isMouseWheelZoomEnabled;

    public bool IsMouseWheelZoomEnabled
    {
        get { return _isMouseWheelZoomEnabled; }
        set
        {
            _isMouseWheelZoomEnabled = value;
            RaisePropertyChanged();

            this.FireIsMouseWheelZoomEnabledChanged?.Invoke(value);
        }
    }

    public Action<bool> FireIsMouseWheelZoomEnabledChanged;



    private bool _isOnDoubleClickZoomEnabled;

    public bool IsDoubleClickZoomEnabled
    {
        get { return _isOnDoubleClickZoomEnabled; }
        set
        {
            _isOnDoubleClickZoomEnabled = value;
            RaisePropertyChanged();

            this.FireIsDoubleClickZoomEnabledChanged?.Invoke(value);
        }
    }

    public Action<bool> FireIsDoubleClickZoomEnabledChanged;



    private bool _isGoogleZoomLevelsEnabled;

    public bool IsGoogleZoomLevelsEnabled
    {
        get { return _isGoogleZoomLevelsEnabled; }
        set
        {
            _isGoogleZoomLevelsEnabled = value;
            RaisePropertyChanged();

            this.FireIsGoogleZoomLevelsEnabledChanged?.Invoke(value);
        }
    }

    public Action<bool> FireIsGoogleZoomLevelsEnabledChanged;


    private int _minGoogleZoomLevel = 1;

    public int MinGoogleZoomLevel
    {
        get { return _minGoogleZoomLevel; }
        set
        {
            if (value > MaxGoogleZoomLevel)
                return;

            _minGoogleZoomLevel = value;
            RaisePropertyChanged();

            this.FireMinGoogleZoomLevelChanged?.Invoke(value);
        }
    }

    public Action<int> FireMinGoogleZoomLevelChanged;


    private int _maxGoogleZoomLevel = 22;

    public int MaxGoogleZoomLevel
    {
        get { return _maxGoogleZoomLevel; }
        set
        {
            if (value < MinGoogleZoomLevel)
                return;

            _maxGoogleZoomLevel = value;
            RaisePropertyChanged();

            this.FireMaxGoogleZoomLevelChanged?.Invoke(value);
        }
    }

    public Action<int> FireMaxGoogleZoomLevelChanged;

    #endregion



    private EditableFeatureLayerOptions _drawingOptions = EditableFeatureLayerOptions.CreateDefaultForDrawing(true, true, true);

    public EditableFeatureLayerOptions DrawingOptions
    {
        get { return _drawingOptions; }
        set
        {
            _drawingOptions = value;
            RaisePropertyChanged();
        }
    }

    private EditableFeatureLayerOptions _editingOptions = EditableFeatureLayerOptions.CreateDefaultForEditing(true, true);

    public EditableFeatureLayerOptions EditingOptions
    {
        get { return _editingOptions; }
        set
        {
            _editingOptions = value;
            RaisePropertyChanged();
        }
    }

    private EditableFeatureLayerOptions _drawingMeasureOptions = EditableFeatureLayerOptions.CreateDefaultForDrawingMeasure(true, true, true);

    public EditableFeatureLayerOptions DrawingMeasureOptions
    {
        get { return _drawingMeasureOptions; }
        set
        {
            _drawingMeasureOptions = value;
            RaisePropertyChanged();
        }
    }

    private EditableFeatureLayerOptions _editingMeasureOptions = EditableFeatureLayerOptions.CreateDefaultForEditingMeasure(true, true);

    public EditableFeatureLayerOptions EditingMeasureOptions
    {
        get { return _editingMeasureOptions; }
        set
        {
            _editingMeasureOptions = value;
            RaisePropertyChanged();
        }
    }


    public void Initialize()
    {
        this.IsBaseMapCacheEnabled = true;

        this.IsMouseWheelZoomEnabled = true;

        this.IsDoubleClickZoomEnabled = true;

        this.IsGoogleZoomLevelsEnabled = true;

    }
}
