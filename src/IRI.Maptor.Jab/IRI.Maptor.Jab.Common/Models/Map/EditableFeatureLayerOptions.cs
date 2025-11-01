using System;
using System.Windows;
using System.Windows.Media;

using IRI.Maptor.Jab.Common.Helpers;

namespace IRI.Maptor.Jab.Common.Models;

public class EditableFeatureLayerOptions : Notifier
{
    static readonly Brush _defaultStroke = BrushHelper.CreateBrush("#FF1CA1E2");
    static readonly Brush _defaultFill = BrushHelper.CreateBrush("#661CA1E2");

    static readonly Brush _stroke;
    static readonly Brush _fill;

    static EditableFeatureLayerOptions()
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
        catch (Exception ex)
        {
            _fill = _defaultFill;

            _stroke = _defaultStroke;
        }
        finally
        {

        }
    }

    public bool IsNewDrawing { get; set; } = false;


    //private bool _isFeatureInfoVisible;

    //public bool IsFeatureInfoVisible
    //{
    //    get { return _isFeatureInfoVisible; }
    //    set
    //    {
    //        _isFeatureInfoVisible = value;
    //        RaisePropertyChanged();
    //    }
    //}


    //private bool _isVerticesLabelVisible = false;

    //public bool IsVerticesLabelVisible
    //{
    //    get { return _isVerticesLabelVisible && IsVerticesVisible; }
    //    set
    //    {
    //        _isVerticesLabelVisible = value;
    //        RaisePropertyChanged();
    //    }
    //}


    private bool _isEdgeLabelVisible = false;

    public bool IsEdgeLabelVisible
    {
        get { return _isEdgeLabelVisible; }
        set
        {
            _isEdgeLabelVisible = value;
            RaisePropertyChanged();
            this.RequestHandleIsEdgeLabelVisibleChanged?.Invoke();
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
        }
    }


    //private bool _isVerticesVisible;

    //public bool IsVerticesVisible
    //{
    //    get { return _isVerticesVisible; }
    //    set
    //    {
    //        _isVerticesVisible = value;
    //        RaisePropertyChanged();
    //        RaisePropertyChanged(nameof(IsVerticesLabelVisible));
    //    }
    //}

    //private bool _isAutoMeasureEnabled = false;

    //public bool IsAutoMeasureEnabled
    //{
    //    get { return _isAutoMeasureEnabled; }
    //    set
    //    {
    //        _isAutoMeasureEnabled = value;
    //        RaisePropertyChanged();
    //    }
    //}

    private bool _isFinishButtonVisible = true;

    public bool IsFinishButtonVisible
    {
        get { return _isFinishButtonVisible; }
        set
        {
            _isFinishButtonVisible = value;
            RaisePropertyChanged();
        }
    }

    private bool _isCancelButtonVisible = true;

    public bool IsCancelButtonVisible
    {
        get { return _isCancelButtonVisible; }
        set
        {
            _isCancelButtonVisible = value;
            RaisePropertyChanged();
        }
    }

    private bool _isDeleteButtonVisible;

    public bool IsDeleteButtonVisible
    {
        get { return _isDeleteButtonVisible; }
        set
        {
            _isDeleteButtonVisible = value;
            RaisePropertyChanged();
        }
    }

    private bool _isMeasureButtonVisible;

    public bool IsMeasureButtonVisible
    {
        get { return _isMeasureButtonVisible; }
        set
        {
            _isMeasureButtonVisible = value;
            RaisePropertyChanged();
        }
    }

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

    private bool _isOptionsAvailable = true;

    public bool IsOptionsAvailable
    {
        get { return _isOptionsAvailable; }
        set
        {
            _isOptionsAvailable = value;
            RaisePropertyChanged();
        }
    }


    private string _editText;

    public string EditText
    {
        get { return _editText; }
        set
        {
            _editText = value;
            RaisePropertyChanged();
        }
    }


    //public Brush Fill { get; set; } = _stroke;

    //public Brush Stroke { get; set; } = _fill;

    //public double StrokeThickness { get; set; } = 4;

    public ScaleInterval VisibleRange { get; set; } = ScaleInterval.All;

    public VisualParameters Visual { get; set; } = new VisualParameters(_fill, _stroke, 4, .9);

    public Func<FrameworkElement> MakePrimaryVertex { get; set; } = () => new Views.MapMarkers.Circle(1);

    public Func<FrameworkElement> MakeSecondaryVertex { get; set; } = () => new Views.MapMarkers.Circle(.6);

    public EditableFeatureLayerOptions()
    {

    }

    public static EditableFeatureLayerOptions CreateDefault() => new EditableFeatureLayerOptions();

    public Action RequestHandleIsEdgeLabelVisibleChanged;


    public static EditableFeatureLayerOptions CreateDefaultForDrawing(bool isMultipartSupportAvailable, bool isManualInputAvailable, bool isOptionsAvailable = true)
    {
        return new EditableFeatureLayerOptions()
        {
            IsCancelButtonVisible = true,
            IsDeleteButtonVisible = false,
            IsEdgeLabelVisible = false,
            IsFinishButtonVisible = true,

            IsManualInputAvailable = isManualInputAvailable,

            IsMeasureVisible = false,
            IsMeasureButtonVisible = false,

            IsMultiPartSupportAvailable = isMultipartSupportAvailable,
            IsNewDrawing = true,
            IsOptionsAvailable = isOptionsAvailable,

            //IsVerticesVisible = false,
            //IsVerticesLabelVisible = false,
        };
    }

    public static EditableFeatureLayerOptions CreateDefaultForEditing(bool isMultipartSupportAvailable, bool isManualInputAvailable, bool isOptionsAvailable = true)
    {
        return new EditableFeatureLayerOptions()
        {
            IsCancelButtonVisible = true,
            IsDeleteButtonVisible = true,
            IsEdgeLabelVisible = false,
            IsFinishButtonVisible = true,

            IsManualInputAvailable = isManualInputAvailable,

            IsMeasureVisible = false,
            IsMeasureButtonVisible = false,

            IsMultiPartSupportAvailable = isMultipartSupportAvailable,
            IsNewDrawing = false,
            IsOptionsAvailable = isOptionsAvailable,

            //IsVerticesVisible = true,
            //IsVerticesLabelVisible = true
        };
    }

    public static EditableFeatureLayerOptions CreateDefaultForDrawingMeasure(bool isEdgeLabelVisible, bool isMultipartSupportAvailable, bool isManualInputAvailable, bool isOptionsAvailable = true)
    {
        return new EditableFeatureLayerOptions()
        {
            Visual = VisualParameters.GetDefaultForMeasurements(),

            IsCancelButtonVisible = true,
            IsDeleteButtonVisible = false,
            IsEdgeLabelVisible = isEdgeLabelVisible,
            IsFinishButtonVisible = true,

            IsManualInputAvailable = isManualInputAvailable,

            IsMeasureVisible = true,
            IsMeasureButtonVisible = false,

            IsMultiPartSupportAvailable = isMultipartSupportAvailable,
            IsNewDrawing = true,
            IsOptionsAvailable = isOptionsAvailable,



            //IsEdgeLabelVisible = isEdgeLabelVisible,
            //IsOptionsAvailable = isOptionsAvailable,
            //IsManualInputAvailable = isManualInputAvailable,
            //IsMultiPartSupportAvailable = isMultipartSupportAvailable
        };
    }

    public static EditableFeatureLayerOptions CreateDefaultForEditingMeasure(bool isMultipartSupportAvailable, bool isManualInputAvailable, bool isOptionsAvailable = true)
    {
        return new EditableFeatureLayerOptions()
        {
            IsCancelButtonVisible = false,
            IsDeleteButtonVisible = true,
            IsEdgeLabelVisible = true,
            IsFinishButtonVisible = false,

            IsManualInputAvailable = isManualInputAvailable,

            IsMeasureVisible = true,
            IsMeasureButtonVisible = true,

            IsMultiPartSupportAvailable = isMultipartSupportAvailable,
            IsNewDrawing = false,
            IsOptionsAvailable = isOptionsAvailable,

            //IsEdgeLabelVisible = true,
            //IsMeasureVisible = true,
            //IsFinishButtonVisible = false,
            //IsCancelButtonVisible = false,
            //IsDeleteButtonVisible = true,
            //IsMeasureButtonVisible = true,
            //IsOptionsAvailable = isOptionsAvailable,
            //IsManualInputAvailable = isManualInputAvailable,
            //IsMultiPartSupportAvailable = isMultipartSupportAvailable
        };
    }



}
