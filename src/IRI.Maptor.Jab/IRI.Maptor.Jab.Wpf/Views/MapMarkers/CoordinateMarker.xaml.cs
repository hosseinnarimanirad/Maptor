using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Windows.Input;
using IRI.Maptor.Jab.Wpf.Models;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Controls.MapMarkers;

public partial class CoordinateMarker : MapMarker
{
    //private string _xLabel;
    //public string XLabel
    //{
    //    get { return _xLabel; }
    //    set
    //    {
    //        _xLabel = value;
    //        RaisePropertyChanged();
    //    }
    //}


    //private string _yLabel;
    //public string YLabel
    //{
    //    get { return _yLabel; }
    //    set
    //    {
    //        _yLabel = value;
    //        RaisePropertyChanged();
    //    }
    //}

    private CoordinateDisplayMode _currentCoordinateDisplayMode;
    public CoordinateDisplayMode CurrentCoordinateDisplayMode
    {
        get { return _currentCoordinateDisplayMode; }
        set
        {
            _currentCoordinateDisplayMode = value;
            RaisePropertyChanged();
        }
    }


    //private CoordinateDisplayMode _current;


    private Locateable _mercatorLocation;
    public Locateable WebMercatorLocation
    {
        get { return _mercatorLocation; }
        set
        {
            _mercatorLocation = value;
            RaisePropertyChanged();
            //UpdateCoordinates();
        }
    }


    public CoordinateMarker(Locateable locateable, CoordinateDisplayMode? initialMode)
    {
        InitializeComponent();

        this.CurrentCoordinateDisplayMode = initialMode ?? CoordinateDisplayMode.GeodeticDecimal;

        this.WebMercatorLocation = locateable;

        try
        {
            counter = availableDisplayModes.IndexOf(CurrentCoordinateDisplayMode);
        }
        catch (System.Exception) { }
    }

    List<CoordinateDisplayMode> availableDisplayModes = [CoordinateDisplayMode.GeodeticDecimal, CoordinateDisplayMode.GeodeticDms, CoordinateDisplayMode.UTM];

    int counter = 0;

    private void changeCoordinate(object sender, MouseButtonEventArgs e)
    {
        counter = counter + 1;
        //CurrentCoordinateDisplayMode = (CoordinateDisplayMode) (((int)CurrentCoordinateDisplayMode + 1) % 4);
        CurrentCoordinateDisplayMode = availableDisplayModes[counter % availableDisplayModes.Count];

        //UpdateCoordinates();
    }

    //public void UpdateCoordinates()
    //{
    //    var value = MapProjects.WebMercatorToGeodeticWgs84(WebMercatorLocation);

    //    if (_current == CoordinateDisplayMode.UTM)
    //    {
    //        value = MapProjects.GeodeticToUTM(value);
    //    }

    //    if (_current == CoordinateDisplayMode.GeodeticDms)
    //    {
    //        XLabel = IRI.Maptor.Sta.Common.Helpers.DegreeHelper.ToDms(value.X, true);
    //        YLabel = IRI.Maptor.Sta.Common.Helpers.DegreeHelper.ToDms(value.Y, true);
    //    }
    //    else
    //    {
    //        var decimals = 2;

    //        if (_current == CoordinateDisplayMode.GeodeticDecimal)
    //            decimals = 5;

    //        XLabel = value.X.ToString($"N{decimals}");
    //        YLabel = value.Y.ToString($"N{decimals}");
    //    }
    //} 
}
