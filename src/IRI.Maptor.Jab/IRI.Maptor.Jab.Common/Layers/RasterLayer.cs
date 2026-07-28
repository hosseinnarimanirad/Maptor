using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Ket.GdiPersistence;
using IRI.Maptor.Ket.SqlitePersistence.MbTiles;
using IRI.Maptor.Ket.SqlitePersistence.GeoPackage;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Persistence.RasterDataSources;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Core.Models;

namespace IRI.Maptor.Jab.Common.Layers;

public class RasterLayer : BaseLayer
{
    RasterLayer? _parent;

    //private IDataSource? _dataSource;

    ////public override IDataSource? DataSource => _dataSource;

    //public override IDataSource? DataSource
    //{
    //    get => _dataSource;
    //    protected set
    //    {
    //        if (ReferenceEquals(_dataSource, value))
    //            return;

    //        UnsubscribeFromDataSourceStatusEvents(_dataSource);

    //        _dataSource = value;

    //        //SyncStatusFromDataSource();
    //        SubscribeToDataSourceStatusEvents(_dataSource);
    //    }
    //}

    //private FrameworkElement? _element;
    //public FrameworkElement? Element
    //{
    //    get { return this._element; }

    //    set
    //    {
    //        this._element = value;

    //        this.BindWithFrameworkElement(value);

    //        RaisePropertyChanged();
    //    }
    //}

    private LayerType _type;
    public override LayerType Type
    {
        get { return _type; }
        //protected set
        //{
        //    _type = value;
        //    RaisePropertyChanged();
        //}
    }

    public BitmapImage? Image { get; set; }


    public RasterLayer(RasterLayer parent, string layerName, LayerType layerType/* bool isBaseMap, bool isPyramid = false*/, double opacity, BoundingBox boundingBox, BitmapImage image)
    {
        LayerId = Guid.NewGuid();

        _parent = parent;

        //this._type = isBaseMap ? LayerType.BaseMap : (isPyramid ? LayerType.ImagePyramid : LayerType.Raster);
        _type = layerType;

        LayerName = layerName;

        Extent/*_extent*/ = boundingBox;

        Image = image;

        Opacity = opacity;
        //this.VisualParameters = new VisualParameters(new ImageBrush(image), isBaseMap ? null : Brushes.Black, isBaseMap ? 0 : 1, opacity);
    }

    public RasterLayer(IDataSource dataSource, string layerName, LayerType layerType, double opacity/*, RenderMode rendering = RenderMode.Default*/, /*bool isBaseMap, bool isPyramid,*/ Visibility visibility, ScaleInterval visibleRange)
    {
        LayerId = Guid.NewGuid();

        _type = layerType;/*isBaseMap ? LayerType.BaseMap : (isPyramid ? LayerType.ImagePyramid : LayerType.Raster);*/

        DataSource = dataSource;

        if (!dataSource.WebMercatorExtent.IsNaN())
        {
            Extent/*_extent*/ = dataSource.WebMercatorExtent;
        }

        LayerName = layerName;

        VisibleRange = visibleRange;

        Opacity = opacity;

        Visibility = visibility;
    }


    protected override void BindWithFrameworkElement(FrameworkElement? element)
    {
        if (element is null)
            return;

        if (element is Path || element is Rectangle)
        {
            Binding binding4 = new Binding() { Source = _parent, Path = new PropertyPath("Visibility"), Mode = BindingMode.TwoWay };
            element.SetBinding(UIElement.VisibilityProperty, binding4);

            Binding binding5 = new Binding() { Source = _parent, Path = new PropertyPath("Opacity"), Mode = BindingMode.TwoWay };
            element.SetBinding(UIElement.OpacityProperty, binding5);
        }
        else
            throw new NotImplementedException();
    }

    private List<RasterLayer> GetRasterLayer(BoundingBox region, double mapScale/*, double unitDistance*/)
    {
        List<RasterLayer> result = new List<RasterLayer>();

        if (DataSource is OfflineGoogleMapDataSource offlineGoogleMapDataSource)
        {
            var tiles = offlineGoogleMapDataSource.GetTiles(region.Transform(MapProjects.WebMercatorToGeodeticWgs84), mapScale);

            foreach (var item in tiles)
            {
                if (item.Image is null)
                    continue;

                var boundingBox = item.GeodeticWgs84BoundingBox.Transform(MapProjects.GeodeticWgs84ToWebMercator);

                var image = Helpers.ImageUtility.CreateBitmapImage(item.Image);

                if (image is null)
                    continue;

                RasterLayer layer = new RasterLayer(this, LayerName, Type /*== LayerType.BaseMap, this.Type == LayerType.ImagePyramid*/, Opacity, boundingBox, image);

                result.Add(layer);
            }
        }
        else if (DataSource is ZippedImagePyramidDataSource zippedImagePyramidDataSource)
        {
            var tiles = zippedImagePyramidDataSource.GetTiles(region.Transform(MapProjects.WebMercatorToGeodeticWgs84), mapScale);

            foreach (var item in tiles)
            {
                if (item.Image is null)
                    continue;

                var boundingBox = item.GeodeticWgs84BoundingBox.Transform(MapProjects.GeodeticWgs84ToWebMercator);

                var image = Helpers.ImageUtility.CreateBitmapImage(item.Image);

                if (image is null)
                    continue;

                RasterLayer layer = new RasterLayer(this, LayerName, Type /*== LayerType.BaseMap, Type == LayerType.ImagePyramid*/, Opacity, boundingBox, image);

                result.Add(layer);
            }
        }
        else if (DataSource is MbTilesDataSource mbTilesDataSource)
        {
            var tiles = mbTilesDataSource.GetTiles(region.Transform(MapProjects.WebMercatorToGeodeticWgs84), mapScale);

            foreach (var item in tiles)
            {
                if (item.Image is null)
                    continue;

                var boundingBox = item.GeodeticWgs84BoundingBox.Transform(MapProjects.GeodeticWgs84ToWebMercator);

                var image = Helpers.ImageUtility.CreateBitmapImage(item.Image);

                if (image is null)
                    continue;

                RasterLayer layer = new RasterLayer(this, LayerName, Type, Opacity, boundingBox, image);

                result.Add(layer);
            }
        }
        else if (DataSource is GeoPackageTileDataSource gpkgTileDataSource)
        {
            var tiles = gpkgTileDataSource.GetTiles(region.Transform(MapProjects.WebMercatorToGeodeticWgs84), mapScale);

            foreach (var item in tiles)
            {
                if (item.Image is null)
                    continue;

                var boundingBox = item.GeodeticWgs84BoundingBox.Transform(MapProjects.GeodeticWgs84ToWebMercator);

                var image = Helpers.ImageUtility.CreateBitmapImage(item.Image);

                if (image is null)
                    continue;

                RasterLayer layer = new RasterLayer(this, LayerName, Type, Opacity, boundingBox, image);

                result.Add(layer);
            }
        }
        //else if (DataSource is OnlineGoogleMapDataSource onlineGoogleMapDataSource)
        //{
        //    var tiles = await onlineGoogleMapDataSource.GetTiles(region, mapScale);

        //    foreach (var item in tiles)
        //    {
        //        var boundingBox = item.Item2.GeodeticWgs84BoundingBox.Transform(MapProjects.GeodeticWgs84ToWebMercator);

        //        if (item.Item2.Image is null)
        //            continue;

        //        var image = Helpers.ImageUtility.CreateBitmapImage(item.Item2.Image);

        //        if (image is null)
        //            continue;

        //        RasterLayer layer = new RasterLayer(this, this.LayerName, image, this.Opacity, boundingBox, true);

        //        result.Add(layer);
        //    }

        //}
        else if (DataSource is GeoRasterFileDataSource geoRasterFileDataSource)
        {
            var geo = geoRasterFileDataSource.Get(region);

            if (geo != null && geo != Sta.Common.Model.GeoReferencedImage.NaN)
            {
                var boundingBox = geo.GeodeticWgs84BoundingBox.Transform(MapProjects.GeodeticWgs84ToWebMercator);

                if (geo.Image is null)
                    return [];

                var image = Helpers.ImageUtility.CreateBitmapImage(geo.Image);

                if (image is null)
                    return [];

                RasterLayer layer = new RasterLayer(this, LayerName, Type/*false*/, Opacity, boundingBox, image);

                result.Add(layer);
            }
        }

        return result;

    }

    public async Task<List<Path>> ParseToPath(BoundingBox boundingBox, Transform viewTransform, double mapScale, double unitDistance)
    {
        List<RasterLayer> layers = GetRasterLayer(boundingBox, mapScale/*, unitDistance*/);

        var result = new List<Path>();

        foreach (var item in layers)
        {
            System.Windows.Point topLeft = item.Extent.TopLeft.AsWpfPoint();

            System.Windows.Point bottomRight = item.Extent.BottomRight.AsWpfPoint();

            RectangleGeometry geometry = new RectangleGeometry(new Rect(topLeft, bottomRight), 0, 0);

            geometry.Transform = viewTransform;

            Path path = new Path()
            {
                Fill = new ImageBrush(item.Image),
                Data = geometry,
                Tag = new LayerTag(mapScale) { Layer = item, IsDrawn = true, BoundingBox = item.Extent, AncestorLayerId = LayerId }
            };

            item.Element = path;

            result.Add(path);
        }

        return result;
    }


    protected override void DataSource_IsLoadedChanged(object? sender, bool e)
    {
        base.DataSource_IsLoadedChanged(sender, e);
        if (e && DataSource is IDataSource ds && !ds.WebMercatorExtent.IsNaN())
        {
            _extent = ds.WebMercatorExtent;
            RaisePropertyChanged(nameof(Extent));
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

}
