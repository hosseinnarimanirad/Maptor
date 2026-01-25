using System;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Model;

namespace IRI.Maptor.Jab.Common.Models.Settings;

public class MapSettingsModel : Notifier, IMapSettings
{
    private readonly IMapSettings _settings;

    public event EventHandler<bool>? OnIsMouseWheelZoomEnabledChanged;

    public event EventHandler<bool>? OnIsDoubleClickZoomEnabledChanged;


    #region Zoom
     
    public bool IsMouseWheelZoomEnabled
    {
        get => _settings.IsMouseWheelZoomEnabled;
        set
        {
            _settings.IsMouseWheelZoomEnabled = value;
            RaisePropertyChanged();

            this.OnIsMouseWheelZoomEnabledChanged?.Invoke(this, value);
        }
    }
     
    public bool IsDoubleClickZoomEnabled
    {
        get => _settings.IsDoubleClickZoomEnabled;
        set
        {
            _settings.IsDoubleClickZoomEnabled = value;
            RaisePropertyChanged();

            this.OnIsDoubleClickZoomEnabledChanged?.Invoke(this, value);
        }
    }
     
    //public Action<bool>? FireIsGoogleZoomLevelsEnabledChanged;
    public bool IsGoogleZoomLevelsEnabled
    {
        get => _settings.IsGoogleZoomLevelsEnabled;
        set
        {
            _settings.IsGoogleZoomLevelsEnabled = value;
            RaisePropertyChanged();
            //this.FireIsGoogleZoomLevelsEnabledChanged?.Invoke(value);
        }
    }
     
    //public Action<int>? FireMinGoogleZoomLevelChanged;
    //private int _minGoogleZoomLevel = 1;
    public int MinGoogleZoomLevel
    {
        get => _settings.MinGoogleZoomLevel;
        set
        {
            if (value > MaxGoogleZoomLevel)
                return;

            _settings.MinGoogleZoomLevel = value;
            RaisePropertyChanged();
            //this.FireMinGoogleZoomLevelChanged?.Invoke(value);
        }
    }
     
    //public Action<int>? FireMaxGoogleZoomLevelChanged;
    //private int _maxGoogleZoomLevel = 22;
    public int MaxGoogleZoomLevel
    {
        get => _settings.MaxGoogleZoomLevel;
        set
        {
            if (value < MinGoogleZoomLevel)
                return;

            _settings.MaxGoogleZoomLevel = value;
            RaisePropertyChanged();
            //this.FireMaxGoogleZoomLevelChanged?.Invoke(value);
        }
    }

    public BoundingBox? InitialExtent { get; set; } = null;

    #endregion


    #region Identify

    // ignore unvisible layers or not in identify
    public bool CheckIsVisible
    {
        get => _settings.CheckIsVisible;
        set
        {
            _settings.CheckIsVisible = value;
            RaisePropertyChanged();
        }
    }


    // ignore layers which are not in scale range or not
    public bool CheckIsInScaleRange
    {
        get => _settings.CheckIsInScaleRange;
        set
        {
            _settings.CheckIsInScaleRange = value;
            RaisePropertyChanged();
        }
    }

    #endregion

    public MapSettingsModel(IMapSettings settings/*, Action<bool> fireIsMouseWheelZoomEnabledChanged, Action<bool> fireIsDoubleClickZoomEnabledChanged*/)
    {
        this._settings = settings;

        //this.FireIsMouseWheelZoomEnabledChanged = fireIsMouseWheelZoomEnabledChanged;

        //this.FireIsDoubleClickZoomEnabledChanged = fireIsDoubleClickZoomEnabledChanged;

        //this.IsMouseWheelZoomEnabled = true;

        //this.IsDoubleClickZoomEnabled = true;
    }

    private EditableFeatureLayerOptions _drawingOptions = EditableFeatureLayerOptions.CreateDefaultForDrawing(true, true, true);
    public EditableFeatureLayerOptions DrawingOptions { get => _drawingOptions; set => _drawingOptions = value; }


    private EditableFeatureLayerOptions _editingOptions = EditableFeatureLayerOptions.CreateDefaultForEditing(true, true);
    public EditableFeatureLayerOptions EditingOptions { get => _editingOptions; set => _editingOptions = value; }


    private EditableFeatureLayerOptions _drawingMeasureOptions = EditableFeatureLayerOptions.CreateDefaultForDrawingMeasure(true, true, true);
    public EditableFeatureLayerOptions DrawingMeasureOptions { get => _drawingMeasureOptions; set => _drawingMeasureOptions = value; }


    private EditableFeatureLayerOptions _editingMeasureOptions = EditableFeatureLayerOptions.CreateDefaultForEditingMeasure(true, true);
    public EditableFeatureLayerOptions EditingMeasureOptions { get => _editingMeasureOptions; set => _editingMeasureOptions = value; }

}
