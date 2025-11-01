using IRI.Maptor.Jab.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IRI.Maptor.Jab.Common.Assets.Commands;
using System.Threading.Tasks;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Controls.Models;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Controls.Presenter;

public class InputCoordinatePresenter : Notifier
{
    private ObservableCollection<Point> _pointCollection;

    /// <summary>
    /// Geodetic points
    /// </summary>
    public ObservableCollection<Point> PointCollection
    {
        get { return _pointCollection; }
        set
        {
            _pointCollection = value;
            RaisePropertyChanged();
        }
    }


    private CoordinateEditor _coordinates;

    public CoordinateEditor Coordinates
    {
        get { return _coordinates; }
        set
        {
            _coordinates = value;
            RaisePropertyChanged();
        }
    }


    Func<Point, Point> MapFunction = p => p;

    private int _zone;

    public int Zone
    {
        get { return _zone; }
        set
        {
            _zone = value;
            RaisePropertyChanged();
        }
    }

    public event EventHandler PointCollectionChanged;

    private SpatialReferenceType _inputType;

    public SpatialReferenceType InputType
    {
        get { return _inputType; }
        set
        {
            _inputType = value;
            RaisePropertyChanged();

            switch (value)
            {
                case SpatialReferenceType.Geodetic:
                    MapFunction = p => p;
                    break;
                case SpatialReferenceType.UTM:
                    MapFunction = p => (Point)MapProjects.UTMToGeodetic(p, Zone);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public InputCoordinatePresenter()
        : this(new List<Point>())
    {

    }

    public InputCoordinatePresenter(List<Point> geographicPoints)
    {
        this.Zone = 39;

        FeedGeographicPoints(geographicPoints);
    }

    public void FeedGeographicPoints(List<Point> geographicPoints)
    {
        this.PointCollection = new ObservableCollection<Point>();

        this.PointCollection.CollectionChanged += (sender, e) => PointCollectionChanged?.Invoke(null, null);

        foreach (var item in geographicPoints)
        {
            this.PointCollection.Add(item);
        }
    }

    //internal void AddPoint(Point point)
    //{
    //    this.PointCollection.Add(MapFunction(point));

    //    if (PointCollectionChanged != null)
    //    {
    //        PointCollectionChanged(null, null);
    //    }
    //}

    //internal void RemovePoint(Point point)
    //{
    //    this.PointCollection.Remove(point);
    //}

    public Func<Task<Geometry<Point>>> RequestGetGeometry;

    private RelayCommand _drawGeometryCommand;

    public RelayCommand DrawGeometryCommand
    {
        get
        {
            if (_drawGeometryCommand == null)
            {
                _drawGeometryCommand = new RelayCommand(param => DrawGeometry());
            }

            return _drawGeometryCommand;
        }
    }

    public Geometry<Point> Geometry
    {
        set
        {
            this.Coordinates = value.AsCoordinateEditor();
             
        }
    }

    private async void DrawGeometry()
    {
        var geometry = await RequestGetGeometry?.Invoke();

        if (geometry == null)
        {
            return;
        }

        this.Coordinates = geometry.AsCoordinateEditor();

        //FeedGeographicPoints(geometry.Transform(IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections.MapProjects.WebMercatorToGeodeticWgs84).GetAllPoints().Cast<Point>().ToList());
    }





}
