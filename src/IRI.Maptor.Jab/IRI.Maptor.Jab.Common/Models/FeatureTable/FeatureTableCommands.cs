using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Models.Map;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.ViewModels;
using IRI.Maptor.Jab.Common.OfficeFormats;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Sta.Common.Enums;

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


    public static FeatureTableCommand CreateZoomToExtentCommand(MapViewModelBase map)
    {
        var markup = new MahApps.Metro.IconPacks.PackIconMaterial() { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.MagnifyExpand }.Data;

        var result = new FeatureTableCommand()
        {
            PathMarkup = markup,
            //Layer = layer.AssociatedLayer,
            ToolTip = "محدودهٔ عارضه"
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

    private static void TryFlashPoint(MapViewModelBase map, IEnumerable<Feature<Point>> point)
    {
        if (point?.Count() == 1 && point.First().GeometryType/*TheGeometry.Type */== GeometryType.Point)
        {
            map.FlashHighlightedFeatures(point.First());
        }
    }

    #endregion

    #region Export Excel

    public static FeatureTableCommand CreateExportToExcelCommand(MapViewModelBase map)
    {
        var markup = new MahApps.Metro.IconPacks.PackIconMaterial() { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.MicrosoftExcel }.Data;

        var result = new FeatureTableCommand()
        {
            PathMarkup = markup,
            //Layer = layer.AssociatedLayer,
            ToolTip = "خروجی اکسل"
        };

        result.Command = new RelayCommand(async (param) =>
        {
            var layer = param as SelectedLayer;

            if (layer == null || map == null)
                return;

            var features = layer.GetSelectedFeatures();

            // Create headers from field aliases (matching grid display)
            List<string>? headers = null;

            List<string>? fieldNames = null;

            if (!layer.Fields.IsNullOrEmpty())
            {
                headers = layer.Fields.Where(f => !f.TypeFullName.ContainsIgnoreCase(FeatureTableHelper.NetTopologySuiteColumnName)).Select(f => f.Alias ?? f.Name).ToList();
                fieldNames = layer.Fields.Where(f => !f.TypeFullName.ContainsIgnoreCase(FeatureTableHelper.NetTopologySuiteColumnName)).Select(f => f.Name).ToList();
            }

            //
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();

            foreach (var item in features)
            {
                // Create ordered dictionary matching field order to ensure columns align with headers
                Dictionary<string, object> orderedRow;
                if (fieldNames != null && headers != null)
                {
                    orderedRow = new Dictionary<string, object>();

                    foreach (var fieldName in fieldNames)
                    {
                        if (item.Attributes.TryGetValue(fieldName, out var value))
                        {
                            orderedRow[fieldName] = value;
                        }
                    }

                    rows.Add(orderedRow);
                }
                else
                {
                    rows.Add(item.Attributes);
                }
            }

            //گرفتن مسیر فایل
            var fileName = await map.DialogService.ShowSaveFileDialogAsync("*.xlsx|*.xlsx", null, layer.LayerName);

            if (string.IsNullOrWhiteSpace(fileName))
                return;

            ExcelHelper.WriteDictionary(rows, fileName, "Sheet1", null, headers);

        });

        return result;
    }

    #endregion

    #region Export As Drawing Item

    public static FeatureTableCommand CreateExportAsDrawingLayersCommand(MapViewModelBase map)
    {
        var markup = new MahApps.Metro.IconPacks.PackIconMaterial() { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.PencilPlus }.Data;

        var result = new FeatureTableCommand()
        {
            PathMarkup = markup,
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
                return;

            foreach (var feature in features)
            {
                map.AddDrawingItem(feature.TheGeometry);
            }

        });

        return result;
    }

    #endregion


    public static FeatureTableCommand CreateBufferCommand(MapViewModelBase map)
    {
        var markup = new MahApps.Metro.IconPacks.PackIconMaterial() { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.DeathlyHallows }.Data;

        var result = new FeatureTableCommand()
        {
            PathMarkup = markup,
            ToolTip = "بافر"
        };

        result.Command = new RelayCommand((param) =>
        {
            var layer = param as SelectedLayer;

            if (layer == null || map == null)
                return;

            var features = layer.HighlightedFeatures;

            if (features.IsNullOrEmpty())
                return;

            foreach (var feature in features)
            {
                var buffer = feature.TheGeometry.Buffer(100);
                map.AddDrawingItem(buffer);
            }

        });

        return result;
    }


    internal static List<Func<MapViewModelBase, IFeatureTableCommand>> GetDefaultVectorLayerCommands()
    {
        return new List<Func<MapViewModelBase, IFeatureTableCommand>>()
        {
            CreateZoomToExtentCommand,
            CreateExportToExcelCommand,
            CreateExportAsDrawingLayersCommand,
            //CreateBufferCommand
        };
    }
}
