using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Localization;

public enum LocalizationResourceKeys
{
    cmd_legend_boundary,
    cmd_legend_breakIntoGeometries,
    cmd_legend_breakIntoPoints,
    cmd_legend_clearSelected,
    cmd_legend_convexHull,
    cmd_legend_downloadRegionImages,
    cmd_legend_duplicateFeature,
    cmd_legend_edit,
    cmd_legend_envelope,
    cmd_legend_exportAsGeoJson,
    cmd_legend_exportAsPng,
    cmd_legend_exportAsShapefile,
    cmd_legend_exteriorRing,
    cmd_legend_removeLayer,
    cmd_legend_selectByDrawing,
    cmd_legend_showAttributes,
    cmd_legend_showSymbology,
    cmd_legend_toggleLayerLabel,
    cmd_legend_zoomToExtent,

    draw_addPointText,
    draw_cancelDrawingText,
    draw_finishDrawingPartText,
    draw_finishDrawingText,
    draw_newDrawingText,

    goto_dialogTitle,

    // todo: check if not used in future
    legend_symbologyExpanderHeaderText,

    map_pan,
    map_zoomTo,

    mapPanel_currentPoint,
    mapPanel_multiPart,
    mapPanel_srs,

    srs_ceaSubTitle,
    srs_ceaTitle,
    srs_defaultLatitude,
    srs_defaultLongitude,
    srs_defaultX,
    srs_defaultY,
    srs_geodeticDmsSubTitle,
    srs_geodeticDmsTitle,
    srs_geodeticSubTitle,
    srs_geodeticTitle,
    srs_mercatorSubTitle,
    srs_mercatorTitle,
    srs_tmSubTitle,
    srs_tmTitle,
    srs_utmSubTitle,
    srs_utmTitle,
    srs_utmZone,
    srs_webMercatorTitle,
    srs_webMercatorSubTitle,

    symbology_fillLabel,
    symbology_strokeLabel,
    symbology_strokeWidthLabel,
    symbology_title,

    tile_provider_bing,
    tile_provider_carto,
    tile_provider_google,
    tile_provider_mapbox,
    tile_provider_nokia,
    tile_provider_osm,
    tile_provider_waze,
    tile_provider_yandex,
    tile_mapType_CleanGrey,
    tile_mapType_BlackWhite,
    tile_mapType_Traffic,
    tile_mapType_Satellite,
    tile_mapType_Hybrid,
    tile_mapType_RoadMap,
    tile_mapType_Terrain,
    tile_mapType_Light,
    tile_mapType_Nature,
    tile_mapType_NeutralBlue,
    tile_mapType_Street,
    tile_mapType_Topo,
    tile_mapType_MapyWinter,
    tile_mapType_MapyTourist,
    tile_mapType_HikeBike,
    tile_mapType_Watercolor,
    tile_mapType_Dark,
    tile_mapType_Comic,


    cmd_addShapefile,
    cmd_addTextToMap,

    ui_header_baseMaps,

    cmd_clearAll,
    ui_header_drawingLegend,
    cmd_drawPoint,
    cmd_drawPolygon,
    cmd_drawPolyline,
    cmd_fullExtent,
    cmd_goTo,
    ui_header_layerLegend,
    cmd_measureArea,
    cmd_measureLength,

    ui_drawingLegend_removeAll,
    ui_drawingLegend_addGeoJson,
    ui_drawingLegend_addShapefile,
    ui_drawingLegend_addLatLongTxt,
    ui_drawingLegend_moveDown,
    ui_drawingLegend_moveUp
}

//public static class LocalizationResourceKeys
//{
//    public const string UtmZone = "srs_utmZone";

//    public const string UtmTitle = "srs_utmTitle";

//    public const string GeodeticWgs84Title = "srs_geodeticTitle";

//    public const string Draw_NewDrawingText = "draw_newDrawingText";
//    public const string Draw_CancelDrawingText = "draw_cancelDrawingText";
//    public const string Draw_FinishDrawingText = "draw_finishDrawingText";
//    public const string Draw_FinishDrawingPartText = "draw_finishDrawingPartText";
//    public const string Draw_AddPointText = "draw_addPointText";

//    public const string MapPanel_currentPoint = "mapPanel_currentPoint";
//    public static LocalizationResourceKeys()
//    {
//        IRI.Maptor.Jab.Common.Properties.Resources.draw_addPointText
//    }
//}
