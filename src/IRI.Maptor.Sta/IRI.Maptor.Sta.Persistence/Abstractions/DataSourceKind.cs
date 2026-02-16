using System.ComponentModel;

namespace IRI.Maptor.Sta.Persistence.Abstractions;

/// <summary>
/// Indicates the type or origin of a data source for UI distinction (legend, tooltips, etc.).
/// </summary>
public enum DataSourceKind
{
    [Description("Shp")]
    Shapefile,

    [Description("Kmz")]
    Kmz,

    [Description("Kml")]
    Kml,

    [Description("Dxf")]
    Dxf,

    [Description("REST")]
    WebApi,

    [Description("gRPC")]
    GRPC,

    [Description("GeoJson")]
    GeoJson,

    [Description("Csv")]
    Csv,

    [Description("Tsv")]
    Tsv,

    [Description("Worldfile")]
    Worldfile,

    [Description("GeoTiff")]
    GeoTiff,

    [Description("image pyramid")]
    ZippedImagePyramid,

    [Description("Gml")]
    GML,

    [Description("...")]
    Other,
}     