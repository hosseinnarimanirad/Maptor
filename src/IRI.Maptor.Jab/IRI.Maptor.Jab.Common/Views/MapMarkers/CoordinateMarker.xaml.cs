using System.Windows.Input;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Common.Views.MapMarkers;

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


    public CoordinateMarker(Locateable locateable)
    {
        InitializeComponent();

        this.CurrentCoordinateDisplayMode = CoordinateDisplayMode.GeodeticDecimal;

        this.WebMercatorLocation = locateable;
    }

    private void changeCoordinate(object sender, MouseButtonEventArgs e)
    {
        CurrentCoordinateDisplayMode = (CoordinateDisplayMode)(((int)CurrentCoordinateDisplayMode + 1) % 4);

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
