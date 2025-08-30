using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Models.Map;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Presenters;
using IRI.Maptor.Jab.Common.OfficeFormats;

namespace IRI.Maptor.Jab.Common.Models;

public static class FeatureTableCommands
{
    #region Defaults

    public static FeatureTableCommand Create(Action action, string markup, string tooltip)
    {
        var result = new FeatureTableCommand()
        {
            PathMarkup = markup,
            Command = new RelayCommand(param => action()),
            ToolTip = tooltip,
        };

        result.Command = new RelayCommand(param => action());

        return result;
    }


    //public static Func<MapPresenter, FeatureTableCommand> CreateZoomToExtentCommandFunc = (presenter) => CreateZoomToExtentCommand(presenter);
    public static FeatureTableCommand CreateZoomToExtentCommand(MapPresenter map)
    {
        var result = new FeatureTableCommand()
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarMagnify,
            //Layer = layer.AssociatedLayer,
            ToolTip = "محدودهٔ عوارض"
        };

        result.Command = new RelayCommand((param) =>
        {
            var layer = param as SelectedLayer;

            if (layer == null || map == null)
                return;

            var features = layer.HighlightedFeatures;

            var extent = BoundingBox.GetMergedBoundingBox(features.Select(f => f.TheGeometry.GetBoundingBox()));

            map.ZoomToExtent(extent, isExactExtent: false, isNewExtent: true, () => { TryFlashPoint(map, features); });
        });

        return result;
    }

    private static void TryFlashPoint(MapPresenter map, IEnumerable<Feature<Point>> point)
    {
        if (point?.Count() == 1 && point.First().TheGeometry.Type == GeometryType.Point)
        {
            map.FlashHighlightedFeatures(point.First());
        }
    }

    #endregion

    #region Export Excel

    public static FeatureTableCommand CreateExportToExcelCommand(MapPresenter map)
    {
        var result = new FeatureTableCommand()
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarPageExcel,
            //Layer = layer.AssociatedLayer,
            ToolTip = "خروجی اکسل"
        };

        result.Command = new RelayCommand((param) =>
        {
            var layer = param as SelectedLayer;

            if (layer == null || map == null)
                return;

            var features = layer.GetSelectedFeatures();

            //
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();

            foreach (var item in features)
            {
                rows.Add(item.Attributes);

                //if (item is Feature<Point> feature)
                //{
                //rows.Add(feature.Attributes);
                //}

                //// todo: consider solving the general case
                //else if (item is IGeometryAware<Point> geometryAware)
                //{
                //    rows.Add(new Dictionary<string, object>() { { "Id", item.Id } });
                //}
            }

            //گرفتن مسیر فایل
            var fileName = map.DialogService.ShowSaveFileDialog("*.xlsx|*.xlsx", null, layer.LayerName);

            if (string.IsNullOrWhiteSpace(fileName))
                return;

            ExcelHelper.WriteDictionary(rows, fileName, "Sheet1", null, null);

        });

        return result;
    }

    #endregion

    #region Export As Drawing Item

    public static FeatureTableCommand CreateExportAsDrawingLayersCommand(MapPresenter map)
    {
        var result = new FeatureTableCommand()
        {
            PathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarVectorPenAdd,
            //Layer = layer.AssociatedLayer,
            ToolTip = "انتقال به ترسیم‌ها"
        };

        result.Command = new RelayCommand((param) =>
        {
            var layer = param as SelectedLayer;

            if (layer == null || map == null)
                return;

            var features = layer.HighlightedFeatures;

            if (features.IsNullOrEmpty())
            {
                return;
            }

            foreach (var feature in features)
            {
                map.AddDrawingItem(feature.TheGeometry);
            }

            //
            //List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();

            //foreach (var item in features)
            //{
            //    if (item is SqlFeature feature)
            //    {
            //        rows.Add(feature.Attributes);
            //    }
            //}

            ////گرفتن مسیر فایل
            //var fileName = map.SaveFile("*.xlsx|*.xlsx");

            //if (string.IsNullOrWhiteSpace(fileName))
            //    return;

            //Ket.OfficeFormat.ExcelHelper.WriteDictionary(rows, fileName, "Sheet1", null, null);

        });

        return result;
    }

    #endregion



    internal static List<Func<MapPresenter, IFeatureTableCommand>> GetDefaultVectorLayerCommands/*<T>*/() //where T : class, IGeometryAware<Point>
    {
        return new List<Func<MapPresenter, IFeatureTableCommand>>()
        {
            CreateZoomToExtentCommand,
            CreateExportToExcelCommand,
            CreateExportAsDrawingLayersCommand
        };
    }
}
