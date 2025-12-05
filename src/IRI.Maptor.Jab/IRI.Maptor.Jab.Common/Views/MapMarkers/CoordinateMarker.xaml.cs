using System.Windows.Input;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Common.Views.MapMarkers;
 
public partial class CoordinateMarker : MapMarker
{
    public bool ChangeToDms { get; }

    
    private string _xLabel;
    public string XLabel
    {
        get { return _xLabel; }
        set
        {
            _xLabel = value;
            RaisePropertyChanged();
        }
    }

    
    private string _yLabel;
    public string YLabel
    {
        get { return _yLabel; }
        set
        {
            _yLabel = value;
            RaisePropertyChanged();
        }
    }


    private CoordinateDisplayMode _current;

    
    private Point _mercatorLocation;
    public Point MercatorLocation
    {
        get { return _mercatorLocation; }
        set
        {
            _mercatorLocation = value;
            RaisePropertyChanged();
            UpdateCoordinates();
        }
    }


    public CoordinateMarker(double mercatorX, double mercatorY, bool changeToDms = false)
    {
        InitializeComponent();

        this._current = CoordinateDisplayMode.GeodeticDecimal;

        this.ChangeToDms = changeToDms;

        this.MercatorLocation = new Point(mercatorX, mercatorY);

        //this.X = mercatorX;

        //this.Y = mercatorY;

    }

    private void changeCoordinate(object sender, MouseButtonEventArgs e)
    {
        _current = (CoordinateDisplayMode)((int)(_current + 1) % 3);

        UpdateCoordinates();
    }

    public void UpdateCoordinates()
    {
        var value = MapProjects.WebMercatorToGeodeticWgs84(MercatorLocation);

        if (_current == CoordinateDisplayMode.UTM)
        {
            value = MapProjects.GeodeticToUTM(value);
        }

        if (_current == CoordinateDisplayMode.GeodeticDms)
        {
            XLabel = IRI.Maptor.Sta.Common.Helpers.DegreeHelper.ToDms(value.X, true); YLabel = IRI.Maptor.Sta.Common.Helpers.DegreeHelper.ToDms(value.Y, true);
        }
        else
        {
            var decimals = 2;

            if (_current == CoordinateDisplayMode.GeodeticDecimal)
                decimals = 5;

            XLabel = value.X.ToString($"N{decimals}"); YLabel = value.Y.ToString($"N{decimals}");
        }

    } 

}
