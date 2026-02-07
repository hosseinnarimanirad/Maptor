using IRI.Maptor.Jab.Common.ViewModels;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Pdf;
using IRI.Maptor.Sta.Spatial.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Helpers;

public static class PdfHelper
{
    public static async Task<List<PdfWriter.RasterLayerPdfData>> GetTiles(MapViewModelBase viewModel, double mapScale, BoundingBox boundingBox)
    {
        // Get all layers
        var allLayers = viewModel.GetAllLayers(viewModel.Layers);

        // Find TileServiceLayer instances
        var tileServiceLayers = allLayers
            .OfType<TileServiceLayer>()
            .Where(layer =>
                    layer.Visibility == System.Windows.Visibility.Visible &&
                    layer.CanRenderLayer(mapScale))
            .OrderBy(layer => layer.ZIndex)
            .ToList();

        // Collect raster (basemap) layers
        var result = new List<PdfWriter.RasterLayerPdfData>();

        // Calculate zoom level from map scale
        int zoomLevel = WebMercatorUtility.GetZoomLevel(mapScale);

        // Calculate tiles for bounding box
        var tiles = WebMercatorUtility.WebMercatorBoundingBoxToGoogleTileRegions(boundingBox, zoomLevel);

        // Process tile service layers
        foreach (var tileServiceLayer in tileServiceLayers)
        {
            try
            {
                var rasterTiles = new List<PdfWriter.RasterTileData>();

                // Get tile images
                foreach (var tileInfo in tiles)
                {
                    var geoImage = await tileServiceLayer.GetTileAsync(tileInfo, viewModel.HttpClient);

                    if (geoImage?.IsValid == true && geoImage.Image != null)
                    {
                        var rasterTile = new PdfWriter.RasterTileData
                        {
                            ImageBytes = geoImage.Image,
                            WebMercatorExtent = tileInfo.WebMercatorExtent,
                            Opacity = tileServiceLayer.Opacity
                        };

                        rasterTiles.Add(rasterTile);
                    }
                }

                if (rasterTiles.Count > 0)
                {
                    var rasterLayerData = new PdfWriter.RasterLayerPdfData
                    {
                        Tiles = rasterTiles,
                        ZIndex = tileServiceLayer.ZIndex,
                        Opacity = tileServiceLayer.Opacity,
                        LayerName = tileServiceLayer.LayerName
                    };

                    result.Add(rasterLayerData);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing tile layer {tileServiceLayer.LayerName}: {ex.Message}");
                // Continue with other layers even if one fails
            }
        }

        return result;
    }
}
