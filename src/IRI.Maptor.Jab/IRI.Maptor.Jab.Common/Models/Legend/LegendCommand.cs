using System;
using System.Collections.Generic;
using MahApps.Metro.IconPacks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Jab.Common.Layers;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Jab.Common.ViewModels;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.Properties;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;

using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using System.Globalization;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace IRI.Maptor.Jab.Common.Models.Legend;

public class LegendCommand : LegendCommandBase
{

    private LegendCommand()
    {
    }

    public LegendCommand(string tooltipResourceKey) : base(tooltipResourceKey)
    {
    }

    #region Defaults for ILayer

    public static LegendCommand Create(ILayer layer, Action action, string markup, string tooltipResourceKey)
    {
        var result = new LegendCommand()
        {
            PathMarkup = markup,
            Command = new RelayCommand(param => action()),
            ToolTipResourceKey = tooltipResourceKey,
            Layer = layer
        };

        result.Command = new RelayCommand(param => action());

        return result;
    }


    public static Func<MapViewModelBase, ILayer, LegendCommand> CreateZoomToExtentCommandFunc = CreateZoomToExtentCommand;
    public static LegendCommand CreateZoomToExtentCommand(MapViewModelBase map, ILayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_general_zoomToExtent))
        {
            PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.StretchToPageOutline }.Data,
            Layer = layer,
        };

        result.Command = new RelayCommand((param) =>
        {
            if (layer == null || map == null)
                return;

            map.ZoomToExtent(result.Layer.Extent, isExactExtent: false, isNewExtent: true);
        });

        return result;
    }


    public static Func<MapViewModelBase, ILayer, ILegendCommand> CreateRemoveLayerFunc = CreateRemoveLayer;
    public static ILegendCommand CreateRemoveLayer(MapViewModelBase map, ILayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_legendItem_remove))
        {
            PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.Delete }.Data,
            Layer = layer,
        };

        result.Command = new RelayCommand(param =>
        {
            map.ClearLayer(layer, true);
        });

        return result;
    }

    public static ILegendCommand CreateShowSymbologyView(ILayer layer, Action showSymbologyViewAction)
    {
        var pathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.Cart }.Data;

        return Create(layer, showSymbologyViewAction, pathMarkup, nameof(Resources.cmd_legendItem_showSymbology));
    }

    #endregion


    #region Defaults for VectorLayer

    public static Func<MapViewModelBase, ILayer, ILegendCommand> CreateClearSelectedFunc = (presenter, layer) => CreateClearSelected(presenter, layer as VectorLayer);
    public static ILegendCommand CreateClearSelected(MapViewModelBase map, VectorLayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_legendItem_clearSelected))
        {
            PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.SelectionRemove }.Data,
            Layer = layer,
            IsCommandVisible = false,
        };

        result.Command = new RelayCommand(param =>
        {
            map.RemoveSelectedLayer(layer);
        });

        layer.OnSelectedFeaturesChanged += (sender, e) => { result.IsCommandVisible = e.Arg.HasSelectedFeatures; };

        return result;
    }


    public static Func<MapViewModelBase, ILayer, ILegendCommand> CreateSelectByDrawingFunc()
    {
        return (presenter, layer) => CreateSelectByDrawing(presenter, layer as VectorLayer);
    }
    public static ILegendCommand CreateSelectByDrawing(MapViewModelBase map, VectorLayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_legendItem_selectByDrawing))
        {
            PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.SelectionSearch }.Data,
            Layer = layer,
        };

        result.Command = new RelayCommand(async param =>
        {
            var options = EditableFeatureLayerOptions.CreateDefaultForDrawing(false, true);

            //options.IsOptionsAvailable = false;

            var drawingResult = await map.GetDrawingAsync(DrawMode.Polygon, options);

            if (!drawingResult.HasNotNullResult())
                return;

            var features = await layer.GetFeaturesAsync(drawingResult.Result);

            if (features == null)
            {
                return;
            }

            var newLayer = new SelectedLayer(map.DialogService, layer, layer.GetFields())
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


    public static Func<MapViewModelBase, ILayer, LegendCommand> CreateShowAttributeTableFunc()
    {
        return (presenter, layer) => CreateShowAttributeTable(presenter, layer as VectorLayer);
    }

    public static LegendCommand CreateShowAttributeTable(MapViewModelBase map, VectorLayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_legendItem_showAttributes))
        {
            PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.TableLarge }.Data,
            Layer = layer,
        };

        result.Command = new RelayCommand(async (param) =>
        {
            if (layer == null || map == null)
                return;

            var features = await layer.GetFeaturesAsync();

            var newLayer = new SelectedLayer(map.DialogService, layer, layer.GetFields());

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

            //map.ShowAttributeTable = true;
        });

        return result;
    }


    public static Func<MapViewModelBase, ILayer, ILegendCommand> CreateExportAsPngFunc = (presenter, layer) => CreateExportAsPng(presenter, layer as VectorLayer);
    public static ILegendCommand CreateExportAsPng(MapViewModelBase map, VectorLayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_legendItem_exportAsPng))
        {
            PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.Image }.Data,
            Layer = layer,
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


    public static Func<MapViewModelBase, ILayer, ILegendCommand> CreateExportAsShapefileFunc = (presenter, layer) => CreateExportAsShapefile(presenter, layer as VectorLayer);
    public static ILegendCommand CreateExportAsShapefile(MapViewModelBase map, VectorLayer layer)
    {
        var resource = System.Windows.Application.Current?.FindResource("shp");

        var geometry = resource as System.Windows.Media.Geometry;

        var result = new LegendCommand(nameof(Resources.cmd_legendItem_exportAsShapefile))
        {
            PathMarkup = geometry?.ToString(CultureInfo.InvariantCulture),// IRI.Maptor.Jab.Common.Assets.ShapeStrings.Others.shapefile,
            Layer = layer,
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var fileName = await map.DialogService.ShowSaveFileDialogAsync(DataSourceKind.Shapefile /*"*.shp|*.shp"*/, null, layer.LayerName);

                if (string.IsNullOrWhiteSpace(fileName))
                    return;

                await layer.ExportAsShapefileAsync(fileName);
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    public static Func<MapViewModelBase, ILayer, ILegendCommand> CreateExportAsGeoJsonFunc = (presenter, layer) => CreateExportAsGeoJson(presenter, layer as VectorLayer);
    public static ILegendCommand CreateExportAsGeoJson(MapViewModelBase map, VectorLayer layer)
    {
        var resource = System.Windows.Application.Current?.FindResource("json");

        var geometry = resource as System.Windows.Media.Geometry;

        var result = new LegendCommand(nameof(Resources.cmd_legendItem_exportAsGeoJson))
        {
            PathMarkup = geometry?.ToString(CultureInfo.InvariantCulture),// IRI.Maptor.Jab.Common.Assets.ShapeStrings.Others.json,
            Layer = layer,
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var fileName = await map.DialogService.ShowSaveFileDialogAsync(DataSourceKind.GeoJson/*"*.json|*.json"*/, null, layer.LayerName);

                if (string.IsNullOrWhiteSpace(fileName))
                    return;

                // 1400.02.31
                // به خاطر خروجی برنامه البرز نگار
                // چون در سایت ژئوجی‌سان دات آی‌او
                // بارگذاری می شه خروجی‌ها
                await layer.ExportAsGeoJsonAsync(fileName, true);
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }


    //public static ILegendCommand CreateMoveLayerOrderUp(MapViewModelBase map, VectorLayer layer)
    //{
    //    var result = new LegendCommand(nameof(Resources.cmd_legend_moveUp))
    //    {
    //        PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.Image }.Data,
    //        Layer = layer,
    //    };

    //    result.Command = new RelayCommand(async param =>
    //    {
    //        try
    //        {

    //            if (layer.Parent == null)
    //            {
    //                var index = map.Layers.IndexOf(layer);
    //            }
    //            else
    //            {
    //            }

    //            var otherLayer = DrawingItems[index - 1];
    //        }
    //        catch (Exception ex)
    //        {
    //            await map.DialogService.ShowMessageAsync(ex.Message, null, param);
    //        }
    //    });

    //    return result;


    //}


    internal static List<Func<MapViewModelBase, ILayer, ILegendCommand>> GetDefaultVectorLayerCommands()
    {
        return new List<Func<MapViewModelBase, ILayer, ILegendCommand>>()
        {
            CreateZoomToExtentCommandFunc,
            CreateShowAttributeTableFunc(),
            CreateRemoveLayerFunc,
            CreateExportAsPngFunc,
            CreateSelectByDrawingFunc(),
            CreateClearSelectedFunc,
        };
    }

    #endregion


    #region Drawing Item Legend Commands


    public static ILegendCommand CreateRemoveDrawingItemLayer(MapViewModelBase map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_drawingLegendItem_remove))
        {
            PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.Delete }.Data,
            Layer = layer,

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
    public static ILegendCommand CreateEditDrawingItemLayer(MapViewModelBase map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_drawingLegendItem_edit))
        {
            PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.Pencil }.Data,
            Layer = layer,
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
    public static ILegendCommand CreateExportDrawingItemLayerAsShapefile(MapViewModelBase map, DrawingItemLayer layer)
    {
        var resource = System.Windows.Application.Current?.FindResource("shp");

        var geometry = resource as System.Windows.Media.Geometry;

        var result = new LegendCommand(nameof(Resources.cmd_drawingLegendItem_exportAsShapefile))
        {
            PathMarkup = geometry?.ToString(CultureInfo.InvariantCulture),// IRI.Maptor.Jab.Common.Assets.ShapeStrings.Others.shapefile,
            Layer = layer,
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var file = await map.DialogService.ShowSaveFileDialogAsync(DataSourceKind.Shapefile/*"*.shp|*.shp"*/, null, layer.LayerName);

                if (string.IsNullOrWhiteSpace(file))
                    return;

                var geodeticGeometry = layer.Geometry.Project(SrsBases.GeodeticWgs84);

                var meanPoint = geodeticGeometry.GetMeanPoint();

                if (meanPoint is null)
                    return;

                var utmZone = MapProjects.FindUtmZone(meanPoint.X);

                var coordinateDisplayMode = map.CoordinatePanel?.SelectedItem?.CoordinateDisplayMode ?? CoordinateDisplayMode.GeodeticDecimal;

                var targetSrs = SrsBase.Create(coordinateDisplayMode, utmZone) ?? SrsBases.GeodeticWgs84;

                var esriShape = layer.Geometry.Project(targetSrs).AsEsriShape();

                if (esriShape is null)
                    return;

                IRI.Maptor.Sta.ShapefileFormat.Shapefile.Save(file, [esriShape], true, true);
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
    public static ILegendCommand CreateExportDrawingItemLayerAsGeoJson(MapViewModelBase map, DrawingItemLayer layer)
    {
        var resource = System.Windows.Application.Current?.FindResource("json");

        var geometry = resource as System.Windows.Media.Geometry;

        var result = new LegendCommand(nameof(Resources.cmd_drawingLegendItem_exportAsGeoJson))
        {
            PathMarkup = geometry?.ToString(CultureInfo.InvariantCulture),// IRI.Maptor.Jab.Common.Assets.ShapeStrings.Others.json,
            Layer = layer,
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var file = await map.DialogService.ShowSaveFileDialogAsync(DataSourceKind.GeoJson/*"*.json|*.json"*/, null, layer.LayerName);

                if (string.IsNullOrWhiteSpace(file))
                    return;

                var feature = GeoJsonFeature.Create(layer.Geometry.Project(SrsBases.GeodeticWgs84).AsGeoJson());

                GeoJsonFeatureSet featureSet = new GeoJsonFeatureSet() { Features = new List<GeoJsonFeature>() { feature }, TotalFeatures = 1 };

                await featureSet.SaveAsync(file, false, false);
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    // ***************** Export As CSV ***********
    // *******************************************
    public static ILegendCommand CreateExportDrawingItemLayerAsCsv(MapViewModelBase map, DrawingItemLayer layer/*, CoordinateDisplayMode? coordinateDisplayMode*/)
    {
        var resource = System.Windows.Application.Current?.FindResource("csv");

        var geometry = resource as System.Windows.Media.Geometry;


        var result = new LegendCommand(nameof(Resources.cmd_drawingLegendItem_exportAsCsv))
        {
            PathMarkup = geometry!.ToString(CultureInfo.InvariantCulture),//IRI.Maptor.Jab.Common.Assets.ShapeStrings.Others.json,
            Layer = layer,
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var file = await map.DialogService.ShowSaveFileDialogAsync(DataSourceKind.Csv/*"*.csv|*.csv"*/, null, layer.LayerName);

                if (string.IsNullOrWhiteSpace(file))
                    return;

                if (layer.Geometry.IsNullOrEmpty())
                    return;

                var geodeticGeometry = layer.Geometry.Project(SrsBases.WebMercator, SrsBases.GeodeticWgs84);

                var meanPoint = geodeticGeometry.GetMeanPoint();

                if (meanPoint is null)
                    return;

                var utmZone = MapProjects.FindUtmZone(meanPoint.X);

                //save to csv
                var points = layer.Geometry.GetAllPoints();

                if (points.IsNullOrEmpty())
                    return;

                List<string> lines = new List<string>();

                var options = CopyCoordinateOptions.Create(map.MapSettings.Clipboard_LatLongPrecision, map.MapSettings.Clipboard_XyPrecision);

                options.UtmZone = utmZone;

                var coordinateDisplayMode = map.CoordinatePanel?.SelectedItem?.CoordinateDisplayMode ?? CoordinateDisplayMode.GeodeticDecimal;

                foreach (var point in points)
                {
                    var coordinate = CoordinateHelper.Format(point,
                                                                coordinateDisplayMode/*coordinateDisplayMode ?? CoordinateDisplayMode.GeodeticDecimal*/,
                                                                options/*thousandSeparator: false, null, null, null, null*/);

                    lines.Add($"{coordinate.x}, {coordinate.y}");
                }

                await System.IO.File.WriteAllLinesAsync(file, lines);
            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }



    public static ILegendCommand CreateExportDrawingItemLayerAsPng(MapViewModelBase map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_drawingLegendItem_exportAsPng))
        {
            PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.Image }.Data,
            Layer = layer,
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var fileName = await map.DialogService.ShowSaveFileDialogAsync("*.png|*.png", null, layer.LayerName);

                if (string.IsNullOrWhiteSpace(fileName))
                    return;

                var groundBoundingBox = layer.Geometry.GetBoundingBox().Expand(1.1);

                var currentScreenSize = WebMercatorUtility.ToScreenSize(map.NearestGoogleZoomLevel, groundBoundingBox);

                var mapScale = WebMercatorUtility.GetGoogleMapScale(map.NearestGoogleZoomLevel);

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
    public static ILegendCommand CreateGetExteriorRingCommand(MapViewModelBase map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_drawingLegendItem_exteriorRing))
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.SegoePrint.exteriorRing,
            Layer = layer,
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
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
    public static ILegendCommand CreateGetEnvelopeCommand(MapViewModelBase map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_drawingLegendItem_envelope))
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.SegoePrint.envelope,
            Layer = layer,
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
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
    public static ILegendCommand CreateGetConvexHullCommand(MapViewModelBase map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_drawingLegendItem_convexHull))
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.SegoePrint.convexHull,
            Layer = layer,
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
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
    public static ILegendCommand CreateGetBoundaryCommand(MapViewModelBase map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_drawingLegendItem_boundary))
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.SegoePrint.boundary,
            Layer = layer,
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
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
    public static ILegendCommand CreateBreakIntoGeometriesCommand(MapViewModelBase map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_drawingLegendItem_breakIntoGeometries))
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.SegoePrint.extractGeometries,
            Layer = layer,
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
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
    public static ILegendCommand CreateBreakIntoPointsCommand(MapViewModelBase map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_drawingLegendItem_breakIntoPoints))
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.SegoePrint.extractPoints,
            Layer = layer,
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
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

    // ***************** Duplicate ***************
    // *******************************************
    public static ILegendCommand CreateCloneDrawingItemCommand(MapViewModelBase map, DrawingItemLayer layer)
    {
        var result = new LegendCommand(nameof(Resources.cmd_drawingLegendItem_duplicateFeature))
        {
            PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.ContentCopy }.Data,
            Layer = layer,
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var cloned = layer.Geometry.Clone();

                map.AddDrawingItem(cloned, $"{layer.LayerName} cloned-{map.NearestGoogleZoomLevel}");

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
    public static ILegendCommand CreateSimplifyByAngleCommand(MapViewModelBase map, DrawingItemLayer layer)
    {
        var result = new LegendCommand()
        {
            PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.FlagTriangle }.Data,
            Layer = layer,
            ToolTipResourceKey = "ساده‌سازی روش زاویه",
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var simplified = layer.Geometry.Simplify(SimplificationType.CumulativeAngle, new SimplificationParamters() { AngleThreshold = 0.99, Retain3Points = true });

                map.AddDrawingItem(simplified, $"{layer.LayerName} simplified-{map.NearestGoogleZoomLevel}");

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
    public static ILegendCommand CreateSimplifyByAreaCommand(MapViewModelBase map, DrawingItemLayer layer)
    {
        var result = new LegendCommand()
        {
            PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.FlagTriangle }.Data,
            Layer = layer,
            ToolTipResourceKey = "ساده‌سازی روش مساحت",
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var simplified = layer.Geometry.Simplify(SimplificationType.CumulativeTriangleRoutine, map.NearestGoogleZoomLevel, new SimplificationParamters() { Retain3Points = true });

                map.AddDrawingItem(simplified, $"{layer.LayerName} simplified-{map.NearestGoogleZoomLevel}");

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
    public static ILegendCommand CreateSimplifyByVWCommand(MapViewModelBase map, DrawingItemLayer layer)
    {
        var result = new LegendCommand()
        {
            PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.FlagTriangle }.Data,
            Layer = layer,
            ToolTipResourceKey = "ساده‌سازی روش ویزوال",
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var simplified = layer.Geometry.Simplify(SimplificationType.VisvalingamWhyatt, map.NearestGoogleZoomLevel, new SimplificationParamters() { Retain3Points = true });

                map.AddDrawingItem(simplified, $"{layer.LayerName} simplified-VW-{map.NearestGoogleZoomLevel}");

            }
            catch (Exception ex)
            {
                await map.DialogService.ShowMessageAsync(ex.Message, null, param);
            }
        });

        return result;
    }

    public static ILegendCommand CreateSimplifyByRDPCommand(MapViewModelBase map, DrawingItemLayer layer)
    {
        var result = new LegendCommand()
        {
            PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.FlagTriangle }.Data,
            Layer = layer,
            ToolTipResourceKey = "ساده‌سازی روش داگلاس",
        };

        result.Command = new RelayCommand(async param =>
        {
            try
            {
                var simplified = layer.Geometry.Simplify(SimplificationType.RamerDouglasPeucker, map.NearestGoogleZoomLevel, new SimplificationParamters() { Retain3Points = true });
                //VisualSimplification.sim layer.Geometry.Simplify()
                map.AddDrawingItem(simplified, $"{layer.LayerName} simplified-RDP-{map.NearestGoogleZoomLevel}");

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

    internal static List<Func<MapViewModelBase, DrawingItemLayer, ILegendCommand>> GetDefaultTextLayerCommands()
    {
        return new List<Func<MapViewModelBase, DrawingItemLayer, ILegendCommand>>()
        {
            (p,l) => CreateZoomToExtentCommandFunc(p,l),
            CreateRemoveDrawingItemLayer,
        };
    }

    #endregion
}
