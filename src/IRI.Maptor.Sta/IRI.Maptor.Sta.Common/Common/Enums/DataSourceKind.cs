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
    Shapefile,

    [Description("Kmz"), DataSourceKindInfo(Category = Vector, FileFilter = "Compressed KML (KMZ)|*.kmz")]
    Kmz,

    [Description("Kml"), DataSourceKindInfo(Category = Vector, FileFilter = "Keyhole Markup Language (KML)|*.kml")]
    Kml,

    [Description("Gpx"), DataSourceKindInfo(Category = Vector, FileFilter = "GPS Exchange Format (GPX)|*.gpx")]
    Gpx,

    [Description("Dxf"), DataSourceKindInfo(Category = Vector, FileFilter = "Drawing Exchange Format (DXF)|*.dxf")]
    Dxf,

    [Description("REST"), DataSourceKindInfo(Category = Service, FileFilter = "")]
    WebApi,

    [Description("gRPC"), DataSourceKindInfo(Category = Service, FileFilter = "")]
    GRPC,

    [Description("GeoJson"), DataSourceKindInfo(Category = Vector, FileFilter = "Geographic JSON (GeoJSON)|*.json")]
    GeoJson,

    [Description("TopoJson"), DataSourceKindInfo(Category = Vector, FileFilter = "Topological GeoJSON (TopoJSON)|*.topojson")]
    TopoJson,

    [Description("Csv"), DataSourceKindInfo(Category = Vector, FileFilter = "Comma Separated Values (CSV)|*.csv")]
    Csv,

    [Description("Tsv"), DataSourceKindInfo(Category = Vector, FileFilter = "Tab Separated Values (TSV)|*.tsv")]
    Tsv,

    [Description("Worldfile"), DataSourceKindInfo(Category = Raster, FileFilter = "Worldfile|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff")]
    Worldfile,

    [Description("GeoTiff"), DataSourceKindInfo(Category = Raster, FileFilter = "Georeferenced TIFF (GeoTIFF)|*.tiff")]
    GeoTiff,

    [Description("image pyramid"), DataSourceKindInfo(Category = Raster, FileFilter = "Image Pyramid file|*.pyrmd")]
    ZippedImagePyramid,

    [Description("Gml"), DataSourceKindInfo(Category = Vector, FileFilter = "Geography Markup Language (GML)|*.gml")]
    GML,

    [Description("..."), DataSourceKindInfo(Category = None, FileFilter = "All files (*.*)|*.*")]
    Other,
}