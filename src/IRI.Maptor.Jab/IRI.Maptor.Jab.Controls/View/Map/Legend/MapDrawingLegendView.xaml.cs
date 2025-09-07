using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Runtime.CompilerServices;
using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Jab.Common;
using System;

namespace IRI.Maptor.Jab.Controls.View;

/// <summary>
/// Interaction logic for MapDrawingLegendView.xaml
/// </summary>
public partial class MapDrawingLegendView : NotifiableUserControl
{
    public MapDrawingLegendView() 
    {
        InitializeComponent();

        //LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    //protected override void Instance_LanguageChanged()
    //{
    //    RaisePropertyChanged(nameof(RemoveAllDrawingItemsLabel));
    //    RaisePropertyChanged(nameof(AddGeoJsonToDrawingItemsLabel));
    //    RaisePropertyChanged(nameof(AddLongLatTxtToDrawingItemsLabel));
    //    RaisePropertyChanged(nameof(AddShapefileToDrawingItemsLabel));
    //    RaisePropertyChanged(nameof(MoveDrawingItemDownLabel));
    //    RaisePropertyChanged(nameof(MoveDrawingItemUpLabel));
    //}

    public string GroupName
    {
        get { return (string)GetValue(GroupNameProperty); }
        set
        {
            SetValue(GroupNameProperty, value);
            RaisePropertyChanged(nameof(ShowTools));
        }
    }

    // Using a DependencyProperty as the backing store for GroupName.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty GroupNameProperty =
        DependencyProperty.Register(nameof(GroupName), typeof(string), typeof(MapDrawingLegendView), new PropertyMetadata("D"));


    public double TitleFontSize
    {
        get { return (double)GetValue(TitleFontSizeProperty); }
        set { SetValue(TitleFontSizeProperty, value); }
    }

    // Using a DependencyProperty as the backing store for FontSize.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TitleFontSizeProperty =
        DependencyProperty.Register(nameof(TitleFontSize), typeof(double), typeof(MapDrawingLegendView), new PropertyMetadata(13.0));


    public bool ShowTools
    {
        get { return (bool)GetValue(ShowToolsProperty); }
        set
        {
            SetValue(ShowToolsProperty, value);
            RaisePropertyChanged(nameof(ShowTools));
        }
    }

    // Using a DependencyProperty as the backing store for ShowTools.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ShowToolsProperty =
        DependencyProperty.Register(nameof(ShowTools), typeof(bool), typeof(MapDrawingLegendView), new PropertyMetadata(true));

    //public event PropertyChangedEventHandler PropertyChanged;

    //protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    //{
    //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //}



    //public string RemoveAllDrawingItemsLabel => LocalizationManager.Instance[LocalizationResourceKeys.cmd_drawingLegend_removeAll.ToString()];
    //public string AddGeoJsonToDrawingItemsLabel => LocalizationManager.Instance[LocalizationResourceKeys.cmd_drawingLegend_addGeoJson.ToString()];
    //public string AddLongLatTxtToDrawingItemsLabel => LocalizationManager.Instance[LocalizationResourceKeys.cmd_drawingLegend_addLatLongTxt.ToString()];
    //public string AddShapefileToDrawingItemsLabel => LocalizationManager.Instance[LocalizationResourceKeys.cmd_drawingLegend_addShapefile.ToString()];
    //public string MoveDrawingItemDownLabel => LocalizationManager.Instance[LocalizationResourceKeys.cmd_drawingLegend_moveDown.ToString()];
    //public string MoveDrawingItemUpLabel => LocalizationManager.Instance[LocalizationResourceKeys.cmd_drawingLegend_moveUp.ToString()];

    //#region IDispose

    //private bool _disposed = false;

    //protected virtual void Dispose(bool disposing)
    //{
    //    if (!_disposed)
    //    {
    //        if (disposing)
    //        {
    //            // Dispose managed resources
    //            LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
    //        }

    //        // Dispose unmanaged resources here if any
    //        _disposed = true;
    //    }
    //}

    //public void Dispose()
    //{
    //    Dispose(true);
    //    GC.SuppressFinalize(this);
    //}

    //#endregion
}
