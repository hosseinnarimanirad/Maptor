using System;

using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Common.ViewModels.Map;

public class ActiveExtentViewModel : Notifier
{
    public event EventHandler? OnExtentChanged;

    public event EventHandler? OnExtentChanging;

    public ActiveExtentViewModel(Locateable mapCenter, double mapWidth, double mapHeight)
    {
        //MapPresenter = mapPresenter;

        MapCenter = mapCenter;

        Extent = new BoundingBox(new Point(mapCenter.X, mapCenter.Y), mapWidth, mapHeight);
    }

    //private Locateable _mapCenter;
    private Locateable MapCenter
    {
        get; set;
    }

    private bool _showMeasure = true;
    public bool ShowMeasure
    {
        get { return _showMeasure; }
        set
        {
            _showMeasure = value;
            RaisePropertyChanged();
        }
    }


    private BoundingBox _extent = BoundingBox.NaN;
    public BoundingBox Extent
    {
        get { return _extent; }
        private set
        {
            _extent = value;
        }
    }

    private string _topLabel = string.Empty;
    public string TopLabel
    {
        get { return _topLabel; }
        set
        {
            _topLabel = value;
            RaisePropertyChanged();
        }
    }


    private string _bottomLabel = string.Empty;
    public string BottomLabel
    {
        get { return _bottomLabel; }
        set
        {
            _bottomLabel = value;
            RaisePropertyChanged();
        }
    }


    private string _leftLabel = string.Empty;
    public string LeftLabel
    {
        get { return _leftLabel; }
        set
        {
            _leftLabel = value;
            RaisePropertyChanged();
        }
    }


    private string _rightLabel = string.Empty;
    public string RightLabel
    {
        get { return _rightLabel; }
        set
        {
            _rightLabel = value;
            RaisePropertyChanged();
        }
    }




    public void UpdateExtent(BoundingBox newExtent, bool changedEvent)
    {
        Extent = newExtent;

        if (newExtent.Center.X != MapCenter.X || newExtent.Center.Y != MapCenter.Y)
        {
            MapCenter.CanTriggerPositionChange = false;

            MapCenter.X = Extent.Center.X;
            MapCenter.Y = Extent.Center.Y;

            MapCenter.CanTriggerPositionChange = true;
        }

        var geodeticExtent = newExtent.Transform(MapProjects.WebMercatorToGeodeticWgs84);

        var bottomLength = SpatialUtility.GetEllipsoidalLength(geodeticExtent.BottomLeft, geodeticExtent.BottomRight);
        var topLength = SpatialUtility.GetEllipsoidalLength(geodeticExtent.TopLeft, geodeticExtent.TopRight);
        var leftLength = SpatialUtility.GetEllipsoidalLength(geodeticExtent.BottomLeft, geodeticExtent.TopLeft);
        var rightLength = SpatialUtility.GetEllipsoidalLength(geodeticExtent.BottomRight, geodeticExtent.TopRight);

        BottomLabel = UnitHelper.GetLengthLabel(bottomLength);
        TopLabel = UnitHelper.GetLengthLabel(topLength);
        LeftLabel = UnitHelper.GetLengthLabel(leftLength);
        RightLabel = UnitHelper.GetLengthLabel(rightLength);

        if (changedEvent)
            OnExtentChanged?.Invoke(this, EventArgs.Empty);

        else
            OnExtentChanging?.Invoke(this, EventArgs.Empty);
    }
}
