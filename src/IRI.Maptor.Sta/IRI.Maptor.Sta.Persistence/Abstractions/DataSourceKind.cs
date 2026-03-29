using IRI.Maptor.Sta.Persistence.Attributes;
using static IRI.Maptor.Sta.Persistence.Abstractions.DataSourceCategory;
using System.ComponentModel;

namespace IRI.Maptor.Sta.Persistence.Abstractions;

/// <summary>
/// Indicates the type or origin of a data source for UI distinction (legend, tooltips, etc.).
/// </summary>
public enum DataSourceKind
{
    [Description("Shp"), DataSourceKindInfo(Category = Vector)]
    Shapefile,

    [Description("Kmz"), DataSourceKindInfo(Category = Vector)]
    Kmz,

    [Description("Kml"), DataSourceKindInfo(Category = Vector)]
    Kml,

    [Description("Gpx"), DataSourceKindInfo(Category = Vector)]
    Gpx,

    [Description("Dxf"), DataSourceKindInfo(Category = Vector)]
    Dxf,

    [Description("REST"), DataSourceKindInfo(Category = Service)]
    WebApi,

    [Description("gRPC"), DataSourceKindInfo(Category = Service)]
    GRPC,

    [Description("GeoJson"), DataSourceKindInfo(Category = Vector)]
    GeoJson,

    [Description("TopoJson"), DataSourceKindInfo(Category = Vector)]
    TopoJson,

    [Description("Csv"), DataSourceKindInfo(Category = Vector)]
    Csv,

    [Description("Tsv"), DataSourceKindInfo(Category = Vector)]
    Tsv,

    [Description("Worldfile"), DataSourceKindInfo(Category = Raster)]
    Worldfile,

    [Description("GeoTiff"), DataSourceKindInfo(Category = Raster)]
    GeoTiff,

    [Description("image pyramid"), DataSourceKindInfo(Category = Raster)]
    ZippedImagePyramid,

    [Description("Gml"), DataSourceKindInfo(Category = Vector)]
    GML,

    [Description("..."), DataSourceKindInfo(Category = None)]
    Other,
}