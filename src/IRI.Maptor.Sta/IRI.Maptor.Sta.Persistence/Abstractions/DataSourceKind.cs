using System.ComponentModel;

namespace IRI.Maptor.Sta.Persistence.Abstractions;

/// <summary>
/// Indicates the type or origin of a data source for UI distinction (legend, tooltips, etc.).
/// </summary>
public enum DataSourceKind
{
    [Description("Shp")]
    Shapefile,

    [Description("Kml")]
    Kml,

    [Description("Kmz")]
    Kmz,

    [Description("GeoJson")]
    GeoJson,

    [Description("WebApi")]
    WebApi,

    [Description("gRPC")]
    GRPC,

    [Description("Gml")]
    GML,

    [Description("Dxf")]
    Dxf,

    [Description("Worldfile")]
    Worldfile,

    [Description("GeoTiff")]
    GeoTiff,

    [Description("image pyramid")]
    ZippedImagePyramid,

    [Description("Csv")]
    Csv,

    [Description("Tsv")]
    Tsv,

    [Description("...")]
    Other,
}
