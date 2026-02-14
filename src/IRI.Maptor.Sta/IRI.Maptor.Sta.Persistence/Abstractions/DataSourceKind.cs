namespace IRI.Maptor.Sta.Persistence.Abstractions;

/// <summary>
/// Indicates the type or origin of a data source for UI distinction (legend, tooltips, etc.).
/// </summary>
public enum DataSourceKind
{
    None,
    Shapefile,
    Kml,
    GeoJson,
    WebApi,
}
