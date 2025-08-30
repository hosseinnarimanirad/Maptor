using System;
using System.Collections.Generic;
using MahApps.Metro.IconPacks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Jab.Common.Presenters;
using IRI.Maptor.Jab.Common.Models.Map;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;

namespace IRI.Maptor.Jab.Common.Models.Legend;

public class LegendCommand : LegendCommandBase
{
   
    private LegendCommand()
    {

    }

    public LegendCommand(LocalizationResourceKeys tooltipResourceKey) : base(tooltipResourceKey)
    {
    }


    #region Defaults for ILayer

    public static LegendCommand Create(ILayer layer, Action action, string markup, string tooltipResourceKey)
    {
        var result = new LegendCommand()
        {
            PathMarkup = markup,
            Command = new RelayCommand(param => action()),
            //ToolTip = tooltip,
            ToolTipResourceKey = tooltipResourceKey,
            Layer = layer
        };

        result.Command = new RelayCommand(param => action());

        return result;
    }


    public static Func<MapPresenter, ILayer, LegendCommand> CreateZoomToExtentCommandFunc = CreateZoomToExtentCommand;
    public static LegendCommand CreateZoomToExtentCommand(MapPresenter map, ILayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_zoomToExtent)
        {
            PathMarkup = new PackIconModern() { Kind = PackIconModernKind.Magnify }.Data,// IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarMagnify,
            Layer = layer,
            //ToolTip = "محدودهٔ لایه" 
        };

        result.Command = new RelayCommand((param) =>
        {
            if (layer == null || map == null)
                return;

            map.ZoomToExtent(result.Layer.Extent, isExactExtent: false, isNewExtent: true);
        });

        return result;
    }


    public static Func<MapPresenter, ILayer, ILegendCommand> CreateRemoveLayerFunc = CreateRemoveLayer;
    public static ILegendCommand CreateRemoveLayer(MapPresenter map, ILayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_removeLayer)
        {
            PathMarkup = new PackIconModern() { Kind = PackIconModernKind.Delete }.Data,//IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarDelete,
            Layer = layer,
            //ToolTip = "حذف لایه", 
        };

        result.Command = new RelayCommand(param =>
        {
            map.ClearLayer(layer, true);
        });

        return result;
    }

    public static ILegendCommand CreateShowSymbologyView(ILayer layer, Action showSymbologyViewAction)
    {
        var pathMarkup = new PackIconModern() { Kind = PackIconModernKind.Cart }.Data;

        return Create(layer, showSymbologyViewAction, pathMarkup/*Assets.ShapeStrings.Appbar.appbarCart*/, LocalizationResourceKeys.cmd_legend_showSymbology.ToString());
    }

    #endregion


    #region Defaults for VectorLayer

    public static Func<MapPresenter, ILayer, LegendCommand> CreateShowAttributeTableFunc/*<T>*/()// where T : class, IGeometryAware<Point>
    {
        return (presenter, layer) => CreateShowAttributeTable/*<T>*/(presenter, layer as VectorLayer);
    }

    public static LegendCommand CreateShowAttributeTable/*<T>*/(MapPresenter map, VectorLayer layer) //where T : class, IGeometryAware<Point>
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_showAttributes)
        {
            PathMarkup = new PackIconModern() { Kind = PackIconModernKind.PageText }.Data,//IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarPageText,
            Layer = layer,
            //ToolTip = "اطلاعات توصیفی", 
        };

        result.Command = new RelayCommand((param) =>
        {
            if (layer == null || map == null)
                return;

            var features = layer.GetFeatures/*<T>*/();

            var newLayer = new SelectedLayer/*<Feature<Point>>*/(layer, layer.GetFields());

            //newLayer.RequestSave = l =>
            //{
            //    layer.sou
            //};

            if (features == null)
            {
                newLayer.Features = new System.Collections.ObjectModel.ObservableCollection<Feature<Point>>();
            }
            else
            {
                newLayer.Features = new System.Collections.ObjectModel.ObservableCollection<Feature<Point>>(features.Features);
            }


            map.AddSelectedLayer(newLayer);
        });

        return result;
    }


    public static Func<MapPresenter, ILayer, ILegendCommand> CreateSelectByDrawingFunc/*<T>*/() //where T : class, IGeometryAware<Point>
    {
        return (presenter, layer) => CreateSelectByDrawing/*<T>*/(presenter, layer as VectorLayer);
    }
    public static ILegendCommand CreateSelectByDrawing/*<T>*/(MapPresenter map, VectorLayer layer) //where T : class, IGeometryAware<Point>
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_selectByDrawing)
        {
            PathMarkup = new PackIconModern() { Kind = PackIconModernKind.VectorPenConvert }.Data,//IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarVectorPenConvert,
            Layer = layer,
            //ToolTip = "انتخاب عوارض محدودهٔ ترسیم", 
        };

        result.Command = new RelayCommand(async param =>
        {
            var options = EditableFeatureLayerOptions.CreateDefaultForDrawing(false, false);

            options.IsOptionsAvailable = false;

            var drawingResult = await map.GetDrawingAsync(DrawMode.Polygon, options);

            if (!drawingResult.HasNotNullResult())
                return;

            var features = layer.GetFeatures/*<T>*/(drawingResult.Result);

            if (features == null)
            {
                return;
            }

            var newLayer = new SelectedLayer/*<Feature<Point>>*/(layer, layer.GetFields())
            {
                ShowSelectedOnMap = true
            };

            if (features != null)
            {
                newLayer.Features = new System.Collections.ObjectModel.ObservableCollection<Feature<Point>>(features.Features);
            }

            map.AddSelectedLayer(newLayer);
        });

        return result;
    }


    public static Func<MapPresenter, ILayer, ILegendCommand> CreateClearSelectedFunc = (presenter, layer) => CreateClearSelected(presenter, layer as VectorLayer);
    public static ILegendCommand CreateClearSelected(MapPresenter map, VectorLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_clearSelected)
        {
            PathMarkup = new PackIconModern() { Kind = PackIconModernKind.Close }.Data,//IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarClose,
            Layer = layer,
            //ToolTip = "پاک کردن عوارض انتخابی", 
            IsCommandVisible = false,
        };

        result.Command = new RelayCommand(param =>
        {
            map.RemoveSelectedLayer(layer);
        });

        layer.OnSelectedFeaturesChanged += (sender, e) => { result.IsCommandVisible = e.Arg.HasSelectedFeatures; };

        return result;
    }

    public static Func<MapPresenter, ILayer, ILegendCommand> CreateExportAsPngFunc = (presenter, layer) => CreateExportAsPng(presenter, layer as VectorLayer);
    public static ILegendCommand CreateExportAsPng(MapPresenter map, VectorLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_exportAsPng)
        {
            PathMarkup = new PackIconModern() { Kind = PackIconModernKind.Image }.Data,//IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarImage,
            Layer = layer,
            //ToolTip = _exportAsBitmapToolTip, 
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var fileName = await map.DialogService.ShowSaveFileDialogAsync("*.png|*.png", null, layer.LayerName);

                if (string.IsNullOrWhiteSpace(fileName))
                    return;

                await layer.SaveAsPng(fileName, map.CurrentExtent, map.ActualWidth, map.ActualHeight, map.MapScale);
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }


    public static Func<MapPresenter, ILayer, ILegendCommand> CreateExportAsShapefileFunc = (presenter, layer) => CreateExportAsShapefile(presenter, layer as VectorLayer);
    public static ILegendCommand CreateExportAsShapefile(MapPresenter map, VectorLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_exportAsShapefile)
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.Others.shapefile,//IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarDownload,
            Layer = layer,
            //ToolTip = _exportAsShapefileToolTip, 
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var fileName = await map.DialogService.ShowSaveFileDialogAsync("*.shp|*.shp", null, layer.LayerName);

                if (string.IsNullOrWhiteSpace(fileName))
                    return;

                layer.ExportAsShapefile(fileName);
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    public static Func<MapPresenter, ILayer, ILegendCommand> CreateExportAsGeoJsonFunc = (presenter, layer) => CreateExportAsGeoJson(presenter, layer as VectorLayer);
    public static ILegendCommand CreateExportAsGeoJson(MapPresenter map, VectorLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_exportAsGeoJson)
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.Others.json,//IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarDownload,
            Layer = layer,
            //ToolTip = _exportAsGeoJsonToolTip, 
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var fileName = await map.DialogService.ShowSaveFileDialogAsync("*.json|*.json", null, layer.LayerName);

                if (string.IsNullOrWhiteSpace(fileName))
                    return;

                // 1400.02.31
                // به خاطر خروجی برنامه البرز نگار
                // چون در سایت ژئوجی‌سان دات آی‌او
                // بارگذاری می شه خروجی‌ها
                layer.ExportAsGeoJson(fileName, true);
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    internal static List<Func<MapPresenter, ILayer, ILegendCommand>> GetDefaultVectorLayerCommands/*<T>*/() //where T : class, IGeometryAware<Point>
    {
        return new List<Func<MapPresenter, ILayer, ILegendCommand>>()
        {
            CreateSelectByDrawingFunc/*<T>*/(),
            CreateShowAttributeTableFunc/*<T>*/(),
            CreateClearSelectedFunc,
            CreateRemoveLayerFunc,
            CreateExportAsPngFunc,
            CreateZoomToExtentCommandFunc
        };
    }



    #endregion


    #region Drawing Item Legend Commands

    //private const string _removeToolTip = "حذف";

    //private const string _editToolTip = "ویرایش";

    //private const string _zoomToolTip = "بزرگ‌نمایی";

    //private const string _saveAsShapefileToolTip = "ذخیره‌سازی در قالب شیپ‌فایل";
    //private const string _saveAsGeoJsonToolTip = "ذخیره‌سازی در قالب ژئوجی‌سان";
    //private const string _saveAsPngToolTip = "ذخیره‌سازی در قالب png";


    // ***************** Remove ******************
    // *******************************************
    public static ILegendCommand CreateRemoveDrawingItemLayer(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_removeLayer)
        {
            PathMarkup = new PackIconModern() { Kind = PackIconModernKind.Delete }.Data,// IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarDelete,
            Layer = layer,
            //ToolTip = _removeToolTip,

        };

        result.Command = new RelayCommand(param =>
        {
            map.RemoveDrawingItem(layer);

            //map.Refresh();
        });

        return result;
    }

    // ***************** Edit ********************
    // *******************************************
    public static ILegendCommand CreateEditDrawingItemLayer(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_edit)
        {
            PathMarkup = new PackIconModern() { Kind = PackIconModernKind.Edit }.Data, //IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarEdit,
            Layer = layer,
            //ToolTip = _editToolTip, 
        };

        result.Command = new RelayCommand(async param =>
        {
            var editResult = await map.EditAsync(layer.Geometry, map.MapSettings.EditingOptions);

            if (!(editResult.IsCanceled == true))
            {
                map.ClearLayer(layer);
            }

            if (editResult.HasNotNullResult())
            {
                layer.Feature = new Feature<Point>(editResult.Result, layer.LayerName);

                //shapeItem.AssociatedLayer = new VectorLayer(shapeItem.Title, new List<SqlGeometry>() { editResult.Result.AsSqlGeometry() }, VisualParameters.GetRandomVisualParameters(), LayerType.Drawing, RenderingApproach.Default, RasterizationApproach.DrawingVisual);

                map.ClearLayer(layer);
                map.AddLayer(layer);
                //map.SetLayer(layer);

                // 1400.03.08- remove highlighted geometry
                layer.IsSelectedInToc = false;
                //map.ClearLayer(layer.HighlightGeometryKey.ToString(), true, true);

                //map.Refresh();

                //if (layer.DataSource != null)
                //{
                //    (layer.DataSource as IEditableVectorDataSource/*<Feature<Point>, Point>*/)?.Update(new Feature<Point>(editResult.Result) { Id = layer.Id });
                //}
            }
        });

        return result;
    }

    // ***************** Export As Shapefile *****
    // *******************************************
    public static ILegendCommand CreateExportDrawingItemLayerAsShapefile(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_exportAsShapefile)
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.Others.shapefile,
            Layer = layer,
            //ToolTip = _saveAsShapefileToolTip, 
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var file = map.DialogService.ShowSaveFileDialog("*.shp|*.shp", null, layer.LayerName);

                if (string.IsNullOrWhiteSpace(file))
                    return;

                var esriShape = layer.Geometry.AsEsriShape();

                IRI.Maptor.Sta.ShapefileFormat.Shapefile.Save(file, new List<IEsriShape>() { esriShape }, true, true);
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    // ***************** Export As GeoJson *******
    // *******************************************
    public static ILegendCommand CreateExportDrawingItemLayerAsGeoJson(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_exportAsGeoJson)
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.Others.json,
            Layer = layer,
            //ToolTip = _saveAsGeoJsonToolTip, 
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var file = map.DialogService.ShowSaveFileDialog("*.json|*.json", null, layer.LayerName);

                if (string.IsNullOrWhiteSpace(file))
                    return;

                var feature = GeoJsonFeature.Create(layer.Geometry.Project(SrsBases.GeodeticWgs84).AsGeoJson());

                GeoJsonFeatureSet featureSet = new GeoJsonFeatureSet() { Features = new List<GeoJsonFeature>() { feature }, TotalFeatures = 1 };

                featureSet.Save(file, false, false);
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    public static ILegendCommand CreateExportDrawingItemLayerAsPng(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_exportAsPng)
        {
            PathMarkup = new PackIconModern() { Kind = PackIconModernKind.Image }.Data, //IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarImage,
            Layer = layer,
            //ToolTip = _saveAsPngToolTip, 
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var fileName = map.DialogService.ShowSaveFileDialog("*.png|*.png", null, layer.LayerName);

                if (string.IsNullOrWhiteSpace(fileName))
                    return;

                var groundBoundingBox = layer.Geometry.GetBoundingBox().Expand(1.1);

                var currentScreenSize = WebMercatorUtility.ToScreenSize(map.CurrentZoomLevel, groundBoundingBox);

                var mapScale = WebMercatorUtility.GetGoogleMapScale(map.CurrentZoomLevel);

                //var image = await layer.AsRenderTargetBitmap(groundBoundingBox, currentScreenSize.Width, currentScreenSize.Height, scale);
                //IRI.Maptor.Jab.Common.Helpers.ImageUtility.Save(fileName, image);
                await layer.SaveAsPng(fileName, groundBoundingBox, currentScreenSize.Width, currentScreenSize.Height, mapScale);
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    // ***************** Exterior Ring ***********
    // *******************************************
    public static ILegendCommand CreateGetExteriorRingCommand(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_exteriorRing)
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.SegoePrint.exteriorRing,
            Layer = layer,
            //ToolTip = "حلقه خارجی", 
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                //var geometry = layer.Geometry.AsSqlGeometry().STExteriorRing();               

                //map.AddDrawingItem(geometry.AsGeometry(), $"{layer.LayerName}-ExteriorRing");

                var geometry = layer.Geometry.GetExteriorRing();

                if (geometry is null)
                    return;

                map.AddDrawingItem(geometry, $"{layer.LayerName}-ExteriorRing");
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    // ***************** Envelope ****************
    // *******************************************
    public static ILegendCommand CreateGetEnvelopeCommand(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_envelope)
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.SegoePrint.envelope,
            Layer = layer,
            //ToolTip = "مستطیل دربرگیرنده", 
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                //var geometry = layer.Geometry.AsSqlGeometry().STEnvelope();
                //map.AddDrawingItem(geometry.AsGeometry(), $"{layer.LayerName}-Envelope");

                var geometry = layer.Geometry.GetEnvelope();

                if (geometry is null)
                    return;

                map.AddDrawingItem(geometry, $"{layer.LayerName}-Envelope");
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    // ***************** Convex Hull *************
    // *******************************************
    public static ILegendCommand CreateGetConvexHullCommand(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_convexHull)
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.SegoePrint.convexHull,
            Layer = layer,
            //ToolTip = "پوش محدب", 
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                //var geometry = layer.Geometry.AsSqlGeometry().STConvexHull(); 
                //map.AddDrawingItem(geometry.AsGeometry(), $"{layer.LayerName}-ConvexHull");
                var geometry = layer.Geometry.GetConvexHull();

                if (geometry is null)
                    return;

                map.AddDrawingItem(geometry, $"{layer.LayerName}-ConvexHull");
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    // ***************** Boundary ****************
    // *******************************************
    public static ILegendCommand CreateGetBoundaryCommand(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_boundary)
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.SegoePrint.boundary,
            Layer = layer,
            //ToolTip = "مرز", 
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                //var geometry = layer.Geometry.AsSqlGeometry().STBoundary();
                //map.AddDrawingItem(geometry.AsGeometry(), $"{layer.LayerName}-Boundary");

                var geometry = layer.Geometry.GetBoundary();
                map.AddDrawingItem(geometry, $"{layer.LayerName}-Boundary");
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    // ***************** Break into geometries ***
    // *******************************************
    public static ILegendCommand CreateBreakIntoGeometriesCommand(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_breakIntoGeometries)
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.SegoePrint.extractGeometries,
            Layer = layer,
            //ToolTip = "تفکیک به هندسه", 
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                //var geometries = layer.Geometry.AsSqlGeometry().GetGeometries();
                var geometries = layer.Geometry.Split(clone: true);

                var counter = 0;

                foreach (var geo in geometries)
                {
                    map.AddDrawingItem(geo/*.AsGeometry()*/, $"{layer.LayerName} Geometry #{counter++}");
                }
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    // ***************** Extract points **********
    // *******************************************
    public static ILegendCommand CreateBreakIntoPointsCommand(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_breakIntoPoints)
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.SegoePrint.extractPoints,
            Layer = layer,
            //ToolTip = "تفکیک به نقاط", 
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                //var pointCollection = IRI.Maptor.Ket.SqlServerSpatialExtension.SqlSpatialUtility.MakePointCollection(layer.Geometry.GetAllPoints());
                //map.AddDrawingItem(pointCollection.AsGeometry(), $"{layer.LayerName} Points");

                var pointCollection = Geometry<Point>.Create(layer.Geometry.GetAllPoints(), GeometryType.MultiPoint, layer.Geometry.Srid);

                map.AddDrawingItem(pointCollection, $"{layer.LayerName} Points");
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    // ***************** Simplify by Angle *******
    // *******************************************
    public static ILegendCommand CreateSimplifyByAngleCommand(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand()
        {
            PathMarkup = new PackIconModern() { Kind = PackIconModernKind.Flag }.Data,// IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarFlag,
            Layer = layer,
            ToolTipResourceKey = "ساده‌سازی روش زاویه",
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var simplified = layer.Geometry.Simplify(SimplificationType.CumulativeAngle, new SimplificationParamters() { AngleThreshold = 0.99, Retain3Points = true });
                //VisualSimplification.sim layer.Geometry.Simplify()
                map.AddDrawingItem(simplified, $"{layer.LayerName} simplified-{map.CurrentZoomLevel}");

            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    // ***************** Simplify by Area ********
    // *******************************************
    public static ILegendCommand CreateSimplifyByAreaCommand(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand()
        {
            PathMarkup = new PackIconModern() { Kind = PackIconModernKind.Flag }.Data,//IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarFlag,
            Layer = layer,
            ToolTipResourceKey = "ساده‌سازی روش مساحت",
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var simplified = layer.Geometry.Simplify(SimplificationType.CumulativeTriangleRoutine, map.CurrentZoomLevel, new SimplificationParamters() { Retain3Points = true });
                //VisualSimplification.sim layer.Geometry.Simplify()
                map.AddDrawingItem(simplified, $"{layer.LayerName} simplified-{map.CurrentZoomLevel}");

            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    // ***************** Duplicate ***************
    // *******************************************
    public static ILegendCommand CreateCloneDrawingItemCommand(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(LocalizationResourceKeys.cmd_legend_duplicateFeature)
        {
            PathMarkup = new PackIconModern() { Kind = PackIconModernKind.PageCopy }.Data,//IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarPageCopy,
            Layer = layer,
            //ToolTip = "ایجاد کپی از عارضه", 
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var cloned = layer.Geometry.Clone();

                map.AddDrawingItem(cloned, $"{layer.LayerName} cloned-{map.CurrentZoomLevel}");

            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    // ***************** Simplifications **********
    // *******************************************
    public static ILegendCommand CreateSimplifyByVWCommand(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand()
        {
            PathMarkup = new PackIconModern() { Kind = PackIconModernKind.Flag }.Data,//IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarFlag,
            Layer = layer,
            ToolTipResourceKey = "ساده‌سازی روش ویزوال",
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var simplified = layer.Geometry.Simplify(SimplificationType.VisvalingamWhyatt, map.CurrentZoomLevel, new SimplificationParamters() { Retain3Points = true });
                //VisualSimplification.sim layer.Geometry.Simplify()
                map.AddDrawingItem(simplified, $"{layer.LayerName} simplified-VW-{map.CurrentZoomLevel}");

            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    public static ILegendCommand CreateSimplifyByRDPCommand(MapPresenter map, DrawingItemLayer layer)
    {
        var result = new LegendCommand()
        {
            PathMarkup = new PackIconModern() { Kind = PackIconModernKind.Flag }.Data,//IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarFlag,
            Layer = layer,
            ToolTipResourceKey = "ساده‌سازی روش داگلاس",
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var simplified = layer.Geometry.Simplify(SimplificationType.RamerDouglasPeucker, map.CurrentZoomLevel, new SimplificationParamters() { Retain3Points = true });
                //VisualSimplification.sim layer.Geometry.Simplify()
                map.AddDrawingItem(simplified, $"{layer.LayerName} simplified-RDP-{map.CurrentZoomLevel}");

            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    #endregion



    #region Default Text Layer

    internal static List<Func<MapPresenter, DrawingItemLayer, ILegendCommand>> GetDefaultTextLayerCommands()
    {
        return new List<Func<MapPresenter, DrawingItemLayer, ILegendCommand>>()
        {
            CreateRemoveDrawingItemLayer,
            (p,l)=>LegendCommand. CreateZoomToExtentCommandFunc(p,l)
        };
    }

    #endregion
}
