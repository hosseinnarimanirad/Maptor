using static IRI.Maptor.Sta.Common.Enums.DataSourceCategory;
using System.ComponentModel;
using IRI.Maptor.Sta.Common.Attributes;

namespace IRI.Maptor.Sta.Common.Enums;

/// <summary>
/// Indicates the type or origin of a data source for UI distinction (legend, tooltips, etc.).
/// </summary>
public enum DataSourceKind
{
    [Description("Shp"), DataSourceKindInfo(Category = Vector, FileFilter = "ESRI Shapefile|*.shp")]
    Shapefile = 1,

    [Description("Kmz"), DataSourceKindInfo(Category = Vector, FileFilter = "Compressed KML (KMZ)|*.kmz")]
    Kmz = 2,

    [Description("Kml"), DataSourceKindInfo(Category = Vector, FileFilter = "Keyhole Markup Language (KML)|*.kml")]
    Kml = 3,

    [Description("Gpx"), DataSourceKindInfo(Category = Vector, FileFilter = "GPS Exchange Format (GPX)|*.gpx")]
    Gpx = 4,

    [Description("Dxf"), DataSourceKindInfo(Category = Vector, FileFilter = "Drawing Exchange Format (DXF)|*.dxf")]
    Dxf = 5,

    [Description("REST"), DataSourceKindInfo(Category = Service, FileFilter = "")]
    WebApi = 6,

    [Description("gRPC"), DataSourceKindInfo(Category = Service, FileFilter = "")]
    GRPC = 7,

    [Description("GeoJson"), DataSourceKindInfo(Category = Vector, FileFilter = "Geographic JSON (GeoJSON) (*.json;*.geojson)|*.json;*.geojson")]
    GeoJson = 8,

    [Description("TopoJson"), DataSourceKindInfo(Category = Vector, FileFilter = "Topological GeoJSON (TopoJSON) (*.json;*.topojson)|*.json;*.topojson")]
    TopoJson = 9,

    [Description("Csv"), DataSourceKindInfo(Category = Vector, FileFilter = "Text files (*.txt)|*.txt|Comma Separated Values (CSV) (*.csv)|*.csv")]
    Csv = 10,

    [Description("Tsv"), DataSourceKindInfo(Category = Vector, FileFilter = "Text files (*.txt)|*.txt|Tab Separated Values (TSV) (*.tsv)|*.tsv")]
    Tsv = 11,

    [Description("Worldfile"), DataSourceKindInfo(Category = Raster, FileFilter = "Worldfile|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff")]
    Worldfile = 12,

    [Description("GeoTiff"), DataSourceKindInfo(Category = Raster, FileFilter = "Georeferenced TIFF (GeoTIFF)|*.tiff")]
    GeoTiff = 13,

    [Description("image pyramid"), DataSourceKindInfo(Category = Raster, FileFilter = "Image Pyramid file|*.pyrmd")]
    ZippedImagePyramid = 14,

    [Description("Gml"), DataSourceKindInfo(Category = Vector, FileFilter = "Geography Markup Language (GML)|*.gml")]
    GML = 15,

    [Description("EsriJson"), DataSourceKindInfo(Category = Vector, FileFilter = "ESRI Json Geometry (json)|*.json")]
    EsriJson = 16,

    [Description("MBTiles"), DataSourceKindInfo(Category = Raster, FileFilter = "MBTiles tileset|*.mbtiles")]
    MBTiles = 17,

    [Description("GeoPackage"), DataSourceKindInfo(Category = Vector, FileFilter = "OGC GeoPackage|*.gpkg")]
    GeoPackage = 18,


    [Description("..."), DataSourceKindInfo(Category = None, FileFilter = "All files (*.*)|*.*")]
    Other = 100,
}