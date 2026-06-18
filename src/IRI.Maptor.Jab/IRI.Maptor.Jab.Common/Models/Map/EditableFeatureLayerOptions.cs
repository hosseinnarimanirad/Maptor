using System;
using System.Windows;
using System.Windows.Media;

using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Jab.Controls.MapMarkers;
using IRI.Maptor.Jab.Core;

namespace IRI.Maptor.Jab.Common.Models;

public class EditableFeatureLayerOptions : Notifier
{
    public Action? RequestHandleMeasureVisibilityChanged;

    static readonly Brush _defaultStroke = BrushHelper.CreateBrush("#FF1CA1E2");
    static readonly Brush _defaultFill = BrushHelper.CreateBrush("#661CA1E2");

    readonly Brush _stroke;
    readonly Brush _fill;

    //public bool IsNewDrawing { get; set; } = false;
    private bool _isNewDrawing;
    public bool IsNewDrawing
    {
        get { return _isNewDrawing; }
        set
        {
            _isNewDrawing = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(Status));
        }
    }


    private bool _isMeasureMode = false;
    public bool IsMeasureMode
    {
        get { return _isMeasureMode; }
        set
        {
            _isMeasureMode = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(Status));
        }
    }


    //public bool IsEditing { get; set; } = false;
    private bool _isEditing = false;
    public bool IsEditing
    {
        get { return _isEditing; }
        set
        {
            _isEditing = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(Status));
        }
    }


    // ****************************** Measure ******************************

    private bool _isEdgeLabelVisible = false;
    public bool IsEdgeLabelVisible
    {
        get { return _isEdgeLabelVisible; }
        set
        {
            _isEdgeLabelVisible = value;
            RaisePropertyChanged();
            this.RequestHandleMeasureVisibilityChanged?.Invoke();
        }
    }


    private bool _isMeasureVisible = false;
    public bool IsMeasureVisible
    {
        get { return _isMeasureVisible; }
        set
        {
            _isMeasureVisible = value;
            RaisePropertyChanged();
            this.RequestHandleMeasureVisibilityChanged?.Invoke();
        }
    }


    //private bool _isFinishButtonVisible = true;
    //public bool IsFinishEditButtonVisible
    //{
    //    get { return _isFinishButtonVisible; }
    //    set
    //    {
    //        _isFinishButtonVisible = value;
    //        RaisePropertyChanged();
    //    }
    //}


    //private bool _isCancelButtonVisible = true;
    //public bool IsCancelEditButtonVisible
    //{
    //    get { return _isCancelButtonVisible; }
    //    set
    //    {
    //        _isCancelButtonVisible = value;
    //        RaisePropertyChanged();
    //    }
    //}


    //private bool _isDeleteButtonVisible;
    //public bool IsDeleteButtonVisible
    //{
    //    get { return _isDeleteButtonVisible; }
    //    set
    //    {
    //        _isDeleteButtonVisible = value;
    //        RaisePropertyChanged();
    //    }
    //}


    //private bool _isMeasureButtonVisible;
    //public bool IsMeasureButtonVisible
    //{
    //    get { return _isMeasureButtonVisible; }
    //    set
    //    {
    //        _isMeasureButtonVisible = value;
    //        RaisePropertyChanged();
    //    }
    //}


    private bool _isManualInputAvailable = true;
    public bool IsManualInputAvailable
    {
        get { return _isManualInputAvailable; }
        set
        {
            _isManualInputAvailable = value;
            RaisePropertyChanged();
        }
    }


    private bool _isMultiPartSupportAvailable = true;
    public bool IsMultiPartSupportAvailable
    {
        get { return _isMultiPartSupportAvailable; }
        set
        {
            _isMultiPartSupportAvailable = value;
            RaisePropertyChanged();
        }
    }


    private bool _isGeometryDetailsAvailable;
    public bool IsGeometryDetailsAvailable
    {
        get { return _isGeometryDetailsAvailable; }
        set
        {
            _isGeometryDetailsAvailable = value;
            RaisePropertyChanged();
        }
    }


    private bool _showAdvancedOptions = true;
    public bool ShowAdvancedOptions
    {
        get { return _showAdvancedOptions; }
        set
        {
            _showAdvancedOptions = value;
            RaisePropertyChanged();
        }
    }


    private bool _isLinkedToMouseMove;
    public bool IsLinkedToMouseMove
    {
        get { return _isLinkedToMouseMove; }
        set
        {
            _isLinkedToMouseMove = value;
            RaisePropertyChanged();
        }
    }


    //private string _editText;
    //public string EditText
    //{
    //    get { return _editText; }
    //    set
    //    {
    //        _editText = value;
    //        RaisePropertyChanged();
    //    }
    //}


    public string Status => $"IsEditing:{IsEditing}, IsMeasureMode:{IsMeasureMode}, IsNewDrawing:{IsNewDrawing}";

    //public ScaleInterval VisibleRange { get; set; } = ScaleInterval.All;

    public VisualParameters Visual { get; private set; }// = new VisualParameters(_fill, _stroke, 4, .9);

    //public Func<FrameworkElement> MakePrimaryVertex { get; set; } = () => new Circle(1);

    //public Func<FrameworkElement> MakeSecondaryVertex { get; set; } = () => new Circle(.6);

    public EditableFeatureLayerOptions()
    {
        try
        {
            var brush = (SolidColorBrush)Application.Current.Resources["MahApps.Brushes.Accent"];

            if (brush == null)
            {
                _fill = _defaultFill;

                _stroke = _defaultStroke;
            }
            else
            {
                _fill = new SolidColorBrush(new Color() { A = 100, R = brush.Color.R, G = brush.Color.G, B = brush.Color.B });

                _stroke = new SolidColorBrush(new Color() { A = 204, R = brush.Color.R, G = brush.Color.G, B = brush.Color.B });
            }
        }
        catch (Exception)
        {
            _fill = _defaultFill;

            _stroke = _defaultStroke;
        }
        finally
        {

        }

        Visual = new VisualParameters(_fill, _stroke, 4, 0.9);
    }

    public static EditableFeatureLayerOptions CreateDefault() => new EditableFeatureLayerOptions();


    public static EditableFeatureLayerOptions CreateDefaultForDrawing(
        bool isMultipartSupportAvailable,
        bool isManualInputAvailable,
        bool showAdvancedOptions = true)
    {
        return new EditableFeatureLayerOptions()
        {
            Visual = VisualParameters.GetDefaultForDrawing(),

            // measure
            IsEdgeLabelVisible = false,
            IsMeasureVisible = false,
            //IsMeasureButtonVisible = false,

            // edit
            //IsFinishEditButtonVisible = false,
            //IsCancelEditButtonVisible = false,
            //IsDeleteButtonVisible = false,

            IsManualInputAvailable = isManualInputAvailable,
            IsMultiPartSupportAvailable = isMultipartSupportAvailable,
            IsGeometryDetailsAvailable = false,

            ShowAdvancedOptions = showAdvancedOptions,

            IsNewDrawing = true,
            IsEditing = false,
            IsMeasureMode = false,
        };
    }

    public static EditableFeatureLayerOptions CreateDefaultForEditing(
        bool isMultipartSupportAvailable,
        bool isManualInputAvailable,
        bool showAdvancedOptions = true)
    {
        return new EditableFeatureLayerOptions()
        {
            // measure
            IsEdgeLabelVisible = false,
            IsMeasureVisible = false,
            //IsMeasureButtonVisible = false,

            // edit
            //IsFinishEditButtonVisible = true,
            //IsCancelEditButtonVisible = true,
            //IsDeleteButtonVisible = false,

            IsManualInputAvailable = isManualInputAvailable,
            IsMultiPartSupportAvailable = isMultipartSupportAvailable,
            IsGeometryDetailsAvailable = false,

            ShowAdvancedOptions = showAdvancedOptions,

            IsNewDrawing = false,
            IsEditing = true,
            IsMeasureMode = false,
        };
    }

    public static EditableFeatureLayerOptions CreateDefaultForDrawingMeasure(
        bool isEdgeLabelVisible,
        bool isMultipartSupportAvailable,
        bool isManualInputAvailable,
        bool showAdvancedOptions = true)
    {
        return new EditableFeatureLayerOptions()
        {
            Visual = VisualParameters.GetDefaultForMeasurements(),

            // measure
            IsEdgeLabelVisible = isEdgeLabelVisible,
            IsMeasureVisible = true,
            //IsMeasureButtonVisible = false,

            // edit
            //IsFinishEditButtonVisible = false,
            //IsCancelEditButtonVisible = false,
            //IsDeleteButtonVisible = false,

            IsManualInputAvailable = isManualInputAvailable,
            IsMultiPartSupportAvailable = isMultipartSupportAvailable,
            IsGeometryDetailsAvailable = false,

            ShowAdvancedOptions = showAdvancedOptions,

            IsNewDrawing = true,
            IsEditing = false,
            IsMeasureMode = true,
        };
    }

    public static EditableFeatureLayerOptions CreateDefaultForEditingMeasure(
        bool isMultipartSupportAvailable,
        bool isManualInputAvailable,
        bool showAdvancedOptions = true)
    {
        return new EditableFeatureLayerOptions()
        {
            // measure
            IsEdgeLabelVisible = false,
            IsMeasureVisible = true,
            //IsMeasureButtonVisible = true,

            // edit
            //IsFinishEditButtonVisible = false,
            //IsCancelEditButtonVisible = false,
            //IsDeleteButtonVisible = true,

            IsManualInputAvailable = isManualInputAvailable,
            IsMultiPartSupportAvailable = isMultipartSupportAvailable,
            IsGeometryDetailsAvailable = false,


            ShowAdvancedOptions = showAdvancedOptions,

            IsNewDrawing = false,
            IsEditing = true,
            IsMeasureMode = true,
        };
    }
}