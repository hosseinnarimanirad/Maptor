//using System;
//using System.Collections.Generic;
//using System.Windows;
//using System.Windows.Data;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Shapes;

//using IRI.Maptor.Jab.Common.Models;
//using IRI.Maptor.Sta.Common.Primitives;
//using IRI.Maptor.Sta.Spatial.Primitives;
//using IRI.Maptor.Sta.Persistence.DataSources;

//using Point = IRI.Maptor.Sta.Common.Primitives.Point;
//using IRI.Maptor.Jab.Common.Cartography.Symbologies;
//using IRI.Maptor.Jab.Common.Cartography.RenderingStrategies;

//namespace IRI.Maptor.Jab.Common;

//public class FeatureLayer : BaseLayer
//{
//    public MemoryDataSource DataSource { get; protected set; }

//    public Func<Feature<Point>, VisualParameters> SymbologyRule { get; set; }

//    private LayerType _type;

//    public List<ISymbolizer> Symbolizers { get; protected set; }

//    public override LayerType Type
//    {
//        get { return _type; }
//        protected set
//        {
//            _type = value;
//            RaisePropertyChanged();
//        }
//    }

//    private FrameworkElement _element;

//    public FrameworkElement Element
//    {
//        get { return this._element; }

//        set
//        {
//            this._element = value;

//            BindWithFrameworkElement(value);

//            RaisePropertyChanged();
//        }
//    }

//    private BoundingBox _extent;

//    public override BoundingBox Extent
//    {
//        get { return _extent; }
//        protected set
//        {
//            _extent = value;
//            RaisePropertyChanged();
//        }
//    }

//    public override RenderingApproach Rendering { get; protected set; }

//    public override RasterizationApproach ToRasterTechnique { get; protected set; }

//    public void BindWithFrameworkElement(FrameworkElement element)
//    {
//        Binding binding4 = new Binding() { Source = this, Path = new PropertyPath("VisualParameters.Visibility"), Mode = BindingMode.TwoWay };
//        //Binding binding4 = new Binding() { Source = this, Path = new PropertyPath(nameof(VisualParameters.Visibility)), Mode = BindingMode.TwoWay }; try using this line insted of above
//        element.SetBinding(Path.VisibilityProperty, binding4);

//        Binding binding5 = new Binding() { Source = this, Path = new PropertyPath("VisualParameters.Opacity"), Mode = BindingMode.TwoWay };
//        element.SetBinding(Path.OpacityProperty, binding5);
//    }

//    // todo: unused input parameters
//    public Path? AsBitmapUsingGdiPlus(List<Feature<Point>> features, List<string> labels, double mapScale, /*BoundingBox boundingBox,*/ double width, double height, /*Func<WpfPoint, WpfPoint> mapToScreen,*/ RectangleGeometry area)
//    {
//        if (features == null)
//            return null;

//        var image = new GdiBitmapRenderStrategy(this.Symbolizers).ParseSqlGeometry(
//            features,
//            width,
//            height, 
//            this.SymbologyRule);


//        if (image == null)
//            return null;

//        BitmapImage bitmapImage = Helpers.ImageUtility.AsBitmapImage(image, System.Drawing.Imaging.ImageFormat.Png);

//        image.Dispose();

//        Path path = new Path()
//        {
//            Data = area,
//            Tag = new LayerTag(mapScale) { Layer = this, Tile = null, IsDrawn = true, IsNew = true }
//        };

//        this.Element = path;

//        path.Fill = new ImageBrush(bitmapImage);

//        bitmapImage.Freeze();

//        return path;
//    }

     

//    #region Constructors

//    internal FeatureLayer()
//    {

//    }

//    public FeatureLayer(string name, List<Feature<Point>> features, Func<Feature<Point>, VisualParameters> symbologyRule,
//                        RenderingApproach rendering, RasterizationApproach toRasterTechnique, ScaleInterval visibleRange)
//    {
//        if (features == null || features.Count == 0)
//            throw new NotImplementedException();

//        Initialize(name, new MemoryDataSource(features/*, null, null*/), rendering, toRasterTechnique, visibleRange, symbologyRule);
//    }

//    public FeatureLayer(string layerName, MemoryDataSource dataSource, RenderingApproach rendering,
//                        RasterizationApproach toRasterTechnique, Func<Feature<Point>, VisualParameters> symbologyRule, ScaleInterval visibleRange)
//    {
//        Initialize(layerName, dataSource, rendering, toRasterTechnique, visibleRange, symbologyRule);
//    }

//    private void Initialize(string layerName, MemoryDataSource dataSource, RenderingApproach rendering,
//        RasterizationApproach toRasterTechnique, ScaleInterval visibleRange, Func<Feature<Point>, VisualParameters> symbologyRule)
//    {
//        this.LayerId = Guid.NewGuid();

//        this.DataSource = dataSource;

//        this.Rendering = rendering;

//        this.ToRasterTechnique = toRasterTechnique;

//        this.Type = LayerType.Feature | LayerType.FeatureLayer;

//        //var geometries = dataSource.GetGeometries();

//        //if (geometries?.Count > 0)
//        //{
//        //    this.Type = type | GetGeometryType(geometries.FirstOrDefault(g => g != null));
//        //}
//        //else
//        //{
//        //    this.Type = type;
//        //}

//        //this.Extent = DataSource.GetFeatures()?.GetGeometries()?.GetBoundingBox() ?? BoundingBox.NaN;
//        this.Extent = DataSource.WebMercatorExtent;

//        this.LayerName = layerName;

//        this.VisualParameters = new VisualParameters(null, null, 0, 1, Visibility.Visible);

//        this.SymbologyRule = symbologyRule;

//        //this.PointSymbol = pointSymbol ?? new SimplePointSymbol() { SymbolWidth = 4, SymbolHeight = 4 };

//        //this.Labels = labeling;

//        ////Check for missing visibleRange
//        //if (this.Labels != null)
//        //{
//        //    if (this.Labels.VisibleRange == null)
//        //    {
//        //        this.Labels.VisibleRange = visibleRange;
//        //    }
//        //}

//        this.VisibleRange = (visibleRange == null) ? ScaleInterval.All : visibleRange;

//    }

//    #endregion
//}
