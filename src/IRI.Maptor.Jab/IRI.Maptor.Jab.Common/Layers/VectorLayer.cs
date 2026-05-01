using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Events;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.Cartography.RenderingStrategies;
using Point = IRI.Maptor.Sta.Common.Primitives.Point;

namespace IRI.Maptor.Jab.Common;

public class VectorLayer : SymbolizableLayer
{
    #region Properties, Fields

    //public IVectorDataSource DataSource { get; protected set; }

    //private IDataSource? _dataSource;

    //public override IDataSource? DataSource => _dataSource;

    //public override IDataSource? DataSource
    //{
    //    get => _dataSource;
    //    protected set
    //    {
    //        UnsubscribeFromDataSourceStatusEvents(_dataSource);
    //        _dataSource = value;
    //        if (value != null)
    //        {
    //            UnsubscribeFromDataSourceStatusEvents(value);
    //            SubscribeToDataSourceStatusEvents(value);
    //        }
    //        //SyncStatusFromDataSource();
    //    }
    //}

    private IVectorDataSource? _vectorDataSource => _dataSource as IVectorDataSource;


    protected LayerType _type;
    public override LayerType Type
    {
        get { return _type; }
    }


    private RenderMode _rendering = RenderMode.Default;
    public override RenderMode RenderMode { get => _rendering; }

    protected RasterizationMethod _rasterizationApproach = RasterizationMethod.DrawingVisual;
    public override RasterizationMethod RasterizationMethod { get => _rasterizationApproach; }


    private int _numberOfSelectedFeatures;
    public int NumberOfSelectedFeatures
    {
        get { return _numberOfSelectedFeatures; }
        set
        {
            _numberOfSelectedFeatures = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasSelectedFeatures));

            this.OnSelectedFeaturesChanged?.Invoke(this, new CustomEventArgs<VectorLayer>(this));
        }
    }

    public bool HasSelectedFeatures => NumberOfSelectedFeatures > 0;

    public TileManager TileManager = new TileManager();

    #endregion

    #region Constructors

    internal VectorLayer() { }

    //public VectorLayer(string layerName,
    //                    List<Geometry<Point>> features,
    //                    LayerType type,
    //                    RenderMode renderMode,
    //                    RasterizationMethod rasterizationMethod)
    //    : this(layerName, features, new VisualParameters(BrushHelper.PickBrush(), BrushHelper.PickBrush(), 1, 1), type, renderMode, rasterizationMethod)
    //{
    //}

    public VectorLayer(string layerName,
                        List<Geometry<Point>> features,
                        VisualParameters parameters,
                        LayerType type,
                        RenderMode renderMode,
                        RasterizationMethod rasterizationMethod)
    {
        if (features.IsNullOrEmpty())
            throw new NotImplementedException();

        var dataSource = new MemoryDataSource(features);

        List<ISymbolizer> symbolizers = [new SimpleSymbolizer(parameters)];

        if (parameters.HasLabelParameters)
            symbolizers.Add(new LabelSymbolizer(parameters));

        Initialize(layerName, dataSource, symbolizers, type, renderMode, rasterizationMethod, ScaleInterval.All);
    }

    public VectorLayer(string layerName,
                        IVectorDataSource dataSource,
                        VisualParameters parameters,
                        LayerType type,
                        RenderMode renderMode,
                        RasterizationMethod rasterizationMethod,
                        ScaleInterval visibleRange)
    {
        List<ISymbolizer> symbolizers = [new SimpleSymbolizer(parameters)];

        if (parameters.HasLabelParameters)
            symbolizers.Add(new LabelSymbolizer(parameters));

        Initialize(layerName, dataSource, symbolizers, type, renderMode, rasterizationMethod, visibleRange);
    }

    public VectorLayer(string layerName,
                      IVectorDataSource dataSource,
                      List<ISymbolizer> symbolizers,
                      LayerType type,
                      RenderMode renderMode,
                      RasterizationMethod rasterizationMethod,
                      ScaleInterval visibleRange)//,
                                                 //VisualParameters? labeling = null)
    {
        Initialize(layerName, dataSource, symbolizers, type, renderMode, rasterizationMethod, visibleRange);
    }

    private void Initialize(string layerName,
                            IVectorDataSource dataSource,
                            List<ISymbolizer> symbolizers,
                            LayerType type,
                            RenderMode rendering,
                            RasterizationMethod toRasterTechnique,
                            ScaleInterval? visibleRange)
    {
        this.LayerId = Guid.NewGuid();

        this.DataSource = dataSource;

        this._rendering = rendering;

        this._rasterizationApproach = toRasterTechnique;


        this._type = type;

        this.SpatialModelMode = dataSource.GeometryType.AsLayerType();

        //if (layerType is not null)
        //    this._type |= layerType.Value;

        //this._type = type;

        //var layerType = dataSource.GeometryType.AsLayerType();

        //if (layerType is not null)
        //{
        //    this._type = type | layerType.Value;
        //}
        //else
        //{
        //    this._type = type;
        //}

        this.Extent = dataSource.WebMercatorExtent;

        this.LayerName = layerName;

        foreach (var item in symbolizers)
        {
            this.SetSymbolizer(item);
        }

        this.VisibleRange = (visibleRange == null) ? ScaleInterval.All : visibleRange;

    }

    #endregion

    public override string ToString() => $"{Enum.GetName(this.Type)} - {this.DataSource?.ToString()}";


    public override async Task<FeatureSet<Point>> GetFeatureSet(BoundingBox mapExtent, double mapScale)
    {
        if (_vectorDataSource is null)
            return FeatureSet<Point>.Empty;

        var feature = await _vectorDataSource.GetAsFeatureSetAsync(mapScale, mapExtent);

        if (feature is null || feature.HasNoGeometry())
            return FeatureSet<Point>.Empty;

        return feature;
    }


    #region Raster Save And Export Methods

    // Todo: this method has been totally changed but not tested!
    public async Task SaveAsGoogleTiles(string outputFolderPath, int minLevel = 1, int maxLevel = 13)
    {
        if (maxLevel < minLevel)
        {
            throw new NotImplementedException("(ERROR IN VECTOR LAYER): minLevel must be less than maxLevel");
        }

        var zoomLevels = Enumerable.Range(minLevel, maxLevel - minLevel + 1);

        foreach (var zoom in zoomLevels)
        {
            var googleTiles = WebMercatorUtility.WebMercatorBoundingBoxToGoogleTileRegions(this.Extent, zoom);

            var scale = GoogleScale.GoogleScales.Single(i => i.ZoomLevel == zoom).InverseScale;

            var directory = $"{outputFolderPath}\\{zoom}";

            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            foreach (var tile in googleTiles)
            {
                var image = await AsGdiBitmapAsync(tile.WebMercatorExtent, 256, 256, scale);

                if (image is null)
                    continue;

                image.Save($"{directory}\\{tile.ZoomLevel}, {tile.RowNumber}, {tile.ColumnNumber}.jpg");
            }
        }
    }

    public async Task SaveAsPng(string fileName, BoundingBox mapExtent, double imageWidth, double imageHeight, double webMercatorMapScale)
    {
        var renderTargetBitmap = await AsRenderTargetBitmap(mapExtent, imageWidth, imageHeight, webMercatorMapScale);

        if (renderTargetBitmap is null)
            return;

        // note: the rendered scalebar here is based on
        // webmercator scale not ground scale
        var result = ComposeWithScalebar(renderTargetBitmap, webMercatorMapScale);

        ImageUtility.Save(fileName, result/*renderTargetBitmap*/);


        //switch (this.RasterizationMethod)
        //{
        //    case RasterizationMethod.StreamGeometry:
        //    case RasterizationMethod.DrawingVisual:
        //        var renderTargetBitmap = await AsRenderTargetBitmap(mapExtent, imageWidth, imageHeight, mapScale);

        //        if (renderTargetBitmap is null)
        //            return;

        //        var result = ComposeWithScalebar(renderTargetBitmap,mapScale);

        //        ImageUtility.Save(fileName, result/*renderTargetBitmap*/);

        //        break;

        //    case RasterizationMethod.WriteableBitmap:
        //    case RasterizationMethod.GdiPlus:
        //    //case RasterizationApproach.OpenTk:
        //    case RasterizationMethod.None:
        //    default:
        //        var image = await AsGdiBitmapAsync(mapExtent, imageWidth, imageHeight, mapScale);

        //        if (image is null)
        //            return;

        //        image.Save(fileName);

        //        image.Dispose();
        //        break;
        //}
    }

    private RenderTargetBitmap ComposeWithScalebar(BitmapSource mapImage, double mapScale)
    {
        const double footerHeight = 44;
        const double horizontalPadding = 12;
        const double verticalCenterOffset = 10;

        //var inverseMapScale = 1.0 / mapScale;

        var dpi = 96;
        //var scaleModel = ScalebarExportHelper.Create(mapScale);
        var unitDistance = ScalebarHelper.GetUnitDistance(dpi);
        var mapLength = ScalebarHelper.ChooseRoundScale(mapScale, unitDistance);
        var scalebarLength = ScalebarHelper.GetScalebarLength(mapLength, mapScale, unitDistance);

        if (mapLength <= 0)
            return ToRenderTargetBitmap(mapImage);

        var outputWidth = mapImage.PixelWidth;
        var outputHeight = mapImage.PixelHeight + (int)footerHeight;
        var mapHeight = mapImage.PixelHeight;

        var drawing = new DrawingVisual();

        using (var drawingContext = drawing.RenderOpen())
        {
            drawingContext.DrawImage(mapImage, new Rect(0, 0, mapImage.PixelWidth, mapImage.PixelHeight));

            //drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, mapHeight, outputWidth, footerHeight));

            var barY = mapHeight + verticalCenterOffset;

            var barRect = new Rect(horizontalPadding, barY, scalebarLength, 8);

            drawingContext.DrawRectangle(null, new Pen(Brushes.Black, 1.5), barRect);

            var groundLengthText = new FormattedText(
                mapLength.ToString(),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                12,
                Brushes.Black,
                1.0);

            drawingContext.DrawText(groundLengthText, new System.Windows.Point(horizontalPadding + scalebarLength + 8, barY - 3));

            //var details = new List<string>();
            //if (options.ShowScaleValue)
            //    details.Add(scaleModel.CurrentScaleText);

            //if (options.ShowZoomLevel && options.ZoomLevel.HasValue)
            //    details.Add($"Zoom {options.ZoomLevel.Value}");

            //if (details.Count > 0)
            //{
            //    var infoText = new FormattedText(
            //        string.Join("  ", details),
            //        CultureInfo.CurrentCulture,
            //        FlowDirection.LeftToRight,
            //        new Typeface("Segoe UI"),
            //        12,
            //        Brushes.Black,
            //        1.0);

            //    drawingContext.DrawText(infoText, new Point(horizontalPadding, mapHeight + footerHeight - infoText.Height - 6));
            //}
        }

        var output = new RenderTargetBitmap(outputWidth, outputHeight, dpi, dpi, PixelFormats.Pbgra32);

        output.Render(drawing);
        output.Freeze();
        return output;
    }

    private static RenderTargetBitmap ToRenderTargetBitmap(BitmapSource source)
    {
        if (source is RenderTargetBitmap renderTargetBitmap)
            return renderTargetBitmap;

        var drawing = new DrawingVisual();
        using (var drawingContext = drawing.RenderOpen())
        {
            drawingContext.DrawImage(source, new Rect(0, 0, source.PixelWidth, source.PixelHeight));
        }

        var output = new RenderTargetBitmap(source.PixelWidth, source.PixelHeight, 96, 96, PixelFormats.Pbgra32);
        output.Render(drawing);
        output.Freeze();
        return output;
    }

    public async Task<System.Drawing.Bitmap?> AsGdiBitmapAsync(BoundingBox mapExtent, double imageWidth, double imageHeight, double mapScale)
    {
        var features = await GetRenderReadyFeatures(mapExtent, mapScale, imageWidth, imageHeight);

        //if (features.IsNullOrEmpty())
        //    return null;

        return new GdiBitmapRenderStrategy(Symbolizers).AsGdiBitmap(features, mapScale, imageWidth, imageHeight);
    }

    public async Task<RenderTargetBitmap?> AsRenderTargetBitmap(BoundingBox mapExtent, double imageWidth, double imageHeight, double mapScale)
    {
        var drawingVisuals = await AsDrawingVisual(mapExtent, imageWidth, imageHeight, mapScale);

        if (drawingVisuals.IsNullOrEmpty())
            return null;

        return ImageUtility.Merge(drawingVisuals, (int)imageWidth, (int)imageHeight);
    }

    public async Task<List<DrawingVisual>> AsDrawingVisual(BoundingBox mapExtent, double imageWidth, double imageHeight, double mapScale)
    {
        var features = await GetRenderReadyFeatures(mapExtent, mapScale, imageWidth, imageHeight);

        //if (features.IsNullOrEmpty())
        //    return [];

        return new DrawingVisualRenderStrategy(Symbolizers).AsDrawingVisual(features, mapScale);
    }

    #endregion

    #region Data source status bindings

    protected override void DataSource_IsLoadedChanged(object? sender, bool e)
    {
        base.DataSource_IsLoadedChanged(sender, e);

        if (e && _dataSource is IVectorDataSource vds)
        {
            SpatialModelMode = vds.GeometryType.AsLayerType();

            Extent = vds.WebMercatorExtent;

            RaisePropertyChanged(nameof(SpatialModelMode));

            RaisePropertyChanged(nameof(Extent));

            RaisePropertyChanged(nameof(IsSymbolizable));
        }
    }

    //private void SyncStatusFromDataSource()
    //{
    //    if (DataSource == null)
    //        return;

    //    IsInitializing = DataSource.IsInitializing;
    //    IsProcessing = DataSource.IsProcessing;
    //    IsLoaded = DataSource.IsLoaded;
    //    HasPendingChanges = DataSource.HasPendingChanges;
    //    IsClientFiltered = DataSource.IsClientFiltered;
    //    HasError = DataSource.HasError;
    //}


    //private void UnsubscribeToDataSourceStatusEvents(IVectorDataSource dataSource)
    //{
    //    if (dataSource == null)
    //        return;

    //    dataSource.IsBusyChanged -= DataSource_IsBusyChanged;
    //    dataSource.IsLoadedChanged -= DataSource_IsLoadedChanged;
    //    dataSource.HasPendingChangesChanged -= DataSource_HasPendingChangesChanged;
    //    dataSource.IsClientFilteredChanged -= DataSource_IsClientFilteredChanged;
    //    dataSource.HasErrorChanged -= DataSource_HasErrorChanged;
    //}

    //private void SubscribeToDataSourceStatusEvents(IVectorDataSource dataSource)
    //{
    //    if (dataSource == null)
    //        return;

    //    dataSource.IsBusyChanged += DataSource_IsBusyChanged;
    //    dataSource.IsLoadedChanged += DataSource_IsLoadedChanged;
    //    dataSource.HasPendingChangesChanged += DataSource_HasPendingChangesChanged;
    //    dataSource.IsClientFilteredChanged += DataSource_IsClientFilteredChanged;
    //    dataSource.HasErrorChanged += DataSource_HasErrorChanged;
    //}

    //private void DataSource_IsBusyChanged(object? sender, bool e) => IsBusy = e;

    //private void DataSource_IsLoadedChanged(object? sender, bool e) => IsLoaded = e;

    //private void DataSource_HasPendingChangesChanged(object? sender, bool e) => HasPendingChanges = e;

    //private void DataSource_IsClientFilteredChanged(object? sender, bool e) => IsClientFiltered = e;

    //private void DataSource_HasErrorChanged(object? sender, bool e) => HasError = e;

    #endregion


    #region Vector Export Methods

    public async Task ExportAsShapefileAsync(string shpFileName)
    {
        if (_vectorDataSource is null)
            return;

        var features = await _vectorDataSource.GetAsFeatureSetAsync();

        features.SaveAsShapefile(shpFileName, System.Text.Encoding.UTF8, null, true);
    }

    public async Task ExportAsGeoJsonAsync(string geoJsonFileName, bool isLongitudeFirst)
    {
        if (_vectorDataSource is null)
            return;

        var features = await _vectorDataSource.GetAsFeatureSetAsync();

        features.SaveAsGeoJson(geoJsonFileName, isLongitudeFirst);
    }

    #endregion


    #region GetGeometry & GetFeature Methods

    public Task<FeatureSet<Point>?> GetFeaturesAsync() => GetFeaturesAsync(null);

    public async Task<FeatureSet<Point>?> GetFeaturesAsync(Geometry<Point>? geometry)
    {
        if (_vectorDataSource is null)
            return FeatureSet<Point>.Empty;

        return await _vectorDataSource.GetAsFeatureSetAsync(geometry);
    }

    public List<Field>? GetFields() => _vectorDataSource?.Fields;

    #endregion


    //private Image? DrawLabels(List<Feature<Point>> features, double width, double height)
    //{
    //    if (features.IsNullOrEmpty())
    //        return null;

    //    List<WpfPoint> mapCoordinates = features.ConvertAll((g) => this.Labels.PositionFunc(g.TheGeometry).AsWpfPoint()).ToList();

    //    DrawingVisual drawingVisual = new DrawingVisual();

    //    using (DrawingContext drawingContext = drawingVisual.RenderOpen())
    //    {
    //        var typeface = new Typeface(this.Labels.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

    //        var flowDirection = this.Labels.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    //        var culture = System.Globalization.CultureInfo.CurrentCulture;

    //        var backgroundBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));

    //        for (int i = 0; i < features.Count; i++)
    //        {
    //            var label = features[i].Label;

    //            if (string.IsNullOrEmpty(label))
    //                continue;

    //            FormattedText formattedText =
    //                new FormattedText(label, culture, flowDirection, typeface, this.Labels.FontSize, this.Labels.Foreground, 96);

    //            WpfPoint location = /*mapToScreen*/(mapCoordinates[i]);

    //            drawingContext.DrawRectangle(backgroundBrush, null, new Rect(location, new Size(formattedText.Width, formattedText.Height)));
    //            drawingContext.DrawText(formattedText, new WpfPoint(location.X - formattedText.Width / 2.0, location.Y - formattedText.Height / 2.0));
    //        }
    //    }

    //    Image image = Helpers.ImageUtility.Create(width, height, drawingVisual);

    //    image.Tag = new LayerTag(-1) { Layer = this, IsTiled = false };

    //    this.BindWithFrameworkElement(image);

    //    return image;
    //}

    //private void DrawLabels(List<Feature<Point>> features, System.Drawing.Bitmap image)
    //{
    //    if (features.IsNullOrEmpty())
    //        return;

    //    var mapCoordinates = features.ConvertAll((g) => Labels.PositionFunc(g.TheGeometry)).ToList();

    //    var font = new System.Drawing.Font(this.Labels.FontFamily.FamilyNames.First().Value, this.Labels.FontSize);

    //    var graphic = System.Drawing.Graphics.FromImage(image);

    //    graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

    //    graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

    //    graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

    //    graphic.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

    //    var typeface = new Typeface(this.Labels.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

    //    var flowDirection = this.Labels.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    //    var culture = System.Globalization.CultureInfo.CurrentCulture;

    //    var backgroundBrush = BrushHelper.CreateGdiBrush(System.Drawing.Color.FromArgb(50, 255, 255, 255), 1);

    //    for (int i = 0; i < features.Count; i++)
    //    {
    //        var label = features[i].Label;

    //        if (string.IsNullOrEmpty(label))
    //            continue;

    //        FormattedText formattedText =
    //        new FormattedText(label, culture, flowDirection, typeface, this.Labels.FontSize, this.Labels.Foreground, 96);

    //        var location = /*mapToScreen*/(mapCoordinates[i]);

    //        graphic.FillRectangle(backgroundBrush, (int)location.X, (int)location.Y, (int)formattedText.Width, (int)formattedText.Height);

    //        graphic.DrawString(label, font, Labels.Foreground.AsGdiBrush(), (float)location.X, (float)location.Y);
    //    }

    //    graphic.Flush();
    //}

    //private System.Drawing.Bitmap? DrawLabel(int width, int height, List<string> labels, List<Geometry<Point>> positions)
    //{
    //    System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, height);

    //    if (labels.Count != positions.Count)
    //        return null;

    //    if (labels.IsNullOrEmpty())
    //        return null;

    //    var mapCoordinates = positions.ConvertAll((g) => this.Labels.PositionFunc(g)).ToList();

    //    var font = new System.Drawing.Font(this.Labels.FontFamily.FamilyNames.First().Value, this.Labels.FontSize);

    //    var graphic = System.Drawing.Graphics.FromImage(bitmap);

    //    graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

    //    graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

    //    graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

    //    graphic.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

    //    var foreground = Labels.Foreground.AsGdiBrush();

    //    for (int i = 0; i < labels.Count; i++)
    //    {
    //        var label = labels[i];

    //        if (string.IsNullOrEmpty(label))
    //            continue;

    //        var location = /*transform*/(mapCoordinates[i]);

    //        graphic.DrawString(label, font, foreground, (float)location.X, (float)location.Y);
    //    }

    //    graphic.Flush();

    //    return bitmap;
    //}

    #region Events

    public event EventHandler<CustomEventArgs<VectorLayer>>? OnSelectedFeaturesChanged;

    #endregion
}
