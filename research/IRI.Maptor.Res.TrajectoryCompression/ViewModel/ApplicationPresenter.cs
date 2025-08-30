using System.Linq;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Models;
using System.Collections.Generic;
using IRI.Maptor.Jab.Controls.Presenter;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Sta.MachineLearning;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;



namespace IRI.Maptor.Res.TrajectoryCompression;

public class ApplicationPresenter : MapApplicationPresenter
{
    bool retain3Points;

    public List<GoogleScale> StandardScales
    {
        get
        {
            return GoogleScale.Scales;
        }
    }

    private double _estimatedScale;
    public double EstimatedScale
    {
        get { return _estimatedScale; }
        set
        {
            _estimatedScale = value;
            RaisePropertyChanged();
        }
    }

    private AreaStatistics _areaStatistics;
    public AreaStatistics AreaStatistics
    {
        get { return _areaStatistics; }
        set
        {
            _areaStatistics = value;
            RaisePropertyChanged();
        }
    }

    private double _distanceTolerance;
    public double DistanceTolerance
    {
        get { return _distanceTolerance; }
        set
        {
            _distanceTolerance = value;
            RaisePropertyChanged();
        }
    }

    private int _zoomLevel;
    public int ZoomLevel
    {
        get { return _zoomLevel; }
        set
        {
            _zoomLevel = value;
            RaisePropertyChanged(); 
        }
    }

    public ApplicationPresenter()
    {
        //this.DefaultVectorLayerCommands.Add((presenter, layer) =>
        //{ 
        //    var command = LegendCommand.Create(
        //        layer,
        //        () =>
        //        {
        //            var geometries = (layer as VectorLayer)!.GetFeatures<Feature<Point>>()!.Select(s => s.TheGeometry).ToList();

        //            AreaStatistics = new AreaStatistics(geometries); 
        //        },
        //        "",
        //        "Area Statistics");

        //    return command;
        //});

    }
     

    public LogisticSimplification<Point> LogisticGeometrySimplification { get; set; }
      
    //private CustomSqlGeometry _selectedGeometryPoints;
    //public CustomSqlGeometry SelectedGeometryPoints
    //{
    //    get { return _selectedGeometryPoints; }
    //    set
    //    {
    //        _selectedGeometryPoints = value;
    //        RaisePropertyChanged();
    //        RaisePropertyChanged(nameof(CurrentPoints));
    //    }
    //}

    //public List<Point> CurrentPoints
    //{
    //    get
    //    {

    //        if (this.SelectedGeometryPoints == null)
    //        {
    //            return new List<Point>();
    //        }

    //        var result = new List<Point>();

    //        for (int i = 0; i < SelectedGeometryPoints.Geometry.STNumGeometries().Value; i++)
    //        {
    //            var temp = SelectedGeometryPoints.Geometry.STGeometryN(i + 1);

    //            result.Add(new Point(temp.STX.Value, temp.STY.Value));
    //        }

    //        return result;
    //    }
    //}



    //public IEsriShapeCollection GetShapes(string fileName)
    //{
    //    var shapes = IRI.Maptor.Sta.ShapefileFormat.Shapefile.ReadShapes(fileName);

    //    var width = Math.Max(shapes.MainHeader.MinimumBoundingBox.Width, shapes.MainHeader.MinimumBoundingBox.Height);

    //    //this.EstimatedScale = WebMercatorUtility.EstimateInverseMapScale(width, 900);

    //    var z1 = WebMercatorUtility.EstimateZoomLevel(width, /*30,*/ 900);

    //    Trace.WriteLine($"Z1: {z1}");

    //    return shapes;
    //}
     


    #region Commands
     

    //Simplify By Area
    private RelayCommand _simplifyByAreaCommand;

    public RelayCommand SimplifyByAreaCommand
    {
        get
        {
            if (this._simplifyByAreaCommand == null)
            {
                this._simplifyByAreaCommand = new RelayCommand(param => SimplifyByArea());
            }

            return _simplifyByAreaCommand;
        }
    }

    public void SimplifyByArea()
    {
        var layer = this.GetSelectedLayerInToc();

        if (layer == null || layer as VectorLayer == null)
        {
            return;
        }

        var vLayer = layer as VectorLayer;



        //var result = this.SelectedGeometry.Geometry.Simplify(.98, SimplificationType.AdditiveByArea);

        //AddGeometry(result, $"{this.SelectedGeometry.Name}.AdditiveArea{.98}");
    }

    private RelayCommand _simplifyVisvalingamCommand;

    public RelayCommand SimplifyVisvalingamCommand
    {
        get
        {
            if (_simplifyVisvalingamCommand == null)
            {
                _simplifyVisvalingamCommand = new RelayCommand(param =>
                {
                    var fileName = @"E:\University.Ph.D\Manuscripts\Assets\ArticleData\visvalingamSampleData.csv";

                    var points = System.IO.File.ReadAllLines(fileName)
                                    .Where(i => !string.IsNullOrWhiteSpace(i))
                                    .Select(i => i.Split(','))
                                    .Select(i => new IRI.Maptor.Sta.Common.Primitives.Point(double.Parse(i[0]), double.Parse(i[1])))
                                    .ToList();

                    var lineString = new Geometry<Point>(points, GeometryType.LineString, 0);

                    var originalLayer = new VectorLayer("original",
                                                      new List<Geometry<Point>>() { lineString },
                                                      VisualParameters.Get(System.Windows.Media.Colors.AliceBlue, System.Windows.Media.Colors.Red, 0.9),
                                                      LayerType.VectorLayer,
                                                      RenderMode.Default,
                                                      RasterizationMethod.GdiPlus);

                    if (!this.Layers.Any(l => l.LayerName == "original"))
                    {
                        this.AddLayer(originalLayer);
                    }

                    SimplificationParamters parameters = new SimplificationParamters()
                    {
                        DistanceThreshold = DistanceTolerance,
                        Retain3Points = retain3Points,
                        AreaThreshold = DistanceTolerance * DistanceTolerance
                    };

                    var visvalingam = lineString.Simplify(SimplificationType.VisvalingamWhyatt, parameters);
                    var simplifiedLayer = new VectorLayer($"vissim-{DistanceTolerance}",
                                                      new List<Geometry<Point>>() { visvalingam }.ToList(),
                                                      VisualParameters.Get(System.Windows.Media.Colors.AliceBlue, System.Windows.Media.Colors.Blue, 0.9),
                                                      LayerType.VectorLayer,
                                                      RenderMode.Default,
                                                      RasterizationMethod.GdiPlus);
                    this.AddLayer(simplifiedLayer);

                    var douglasPeucker = lineString.Simplify(SimplificationType.RamerDouglasPeucker, parameters);
                    var douglasPeuckerLayer = new VectorLayer($"douglasPeucker-{DistanceTolerance}",
                                                      new List<Geometry<Point>>() { douglasPeucker }.ToList(),
                                                      VisualParameters.Get(System.Windows.Media.Colors.AliceBlue, System.Windows.Media.Colors.Green, 0.9),
                                                      LayerType.VectorLayer,
                                                      RenderMode.Default,
                                                      RasterizationMethod.GdiPlus);
                    this.AddLayer(douglasPeuckerLayer);

                    var cumulativeArea = lineString.Simplify(SimplificationType.CumulativeTriangleRoutine, parameters);
                    var cumulativeAreaLayer = new VectorLayer($"cumulativeArea-{DistanceTolerance}",
                                                      new List<Geometry<Point>>() { cumulativeArea }.ToList(),
                                                      VisualParameters.Get(System.Windows.Media.Colors.AliceBlue, System.Windows.Media.Colors.Black, 0.9),
                                                      LayerType.VectorLayer,
                                                      RenderMode.Default,
                                                      RasterizationMethod.GdiPlus);
                     
                    this.AddLayer(cumulativeAreaLayer);
                });
            }

            return _simplifyVisvalingamCommand;
        }
    }
     

    #endregion
}

