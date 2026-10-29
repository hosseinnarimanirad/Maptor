using System;
using System.Globalization;
using System.Windows.Data;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Presentation.Wpf.Converters;

/// <summary>
/// The name of a data-source kind as users know it from the file ("Shapefile", "GeoJSON", ...).
/// These are format names and stay untranslated; the few kinds that are not file formats return
/// <see cref="string.Empty"/> and the view supplies a localized name for them.
/// </summary>
public class DataSourceKindToDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DataSourceKind kind)
            return string.Empty;

        return kind switch
        {
            DataSourceKind.Shapefile => "Shapefile",
            DataSourceKind.Kmz => "KMZ",
            DataSourceKind.Kml => "KML",
            DataSourceKind.Gpx => "GPX",
            DataSourceKind.Dxf => "DXF",
            DataSourceKind.GeoJson => "GeoJSON",
            DataSourceKind.TopoJson => "TopoJSON",
            DataSourceKind.EsriJson => "EsriJSON",
            DataSourceKind.Csv => "CSV",
            DataSourceKind.Tsv => "TSV",
            DataSourceKind.GML => "GML",
            DataSourceKind.GeoPackage => "GeoPackage",
            DataSourceKind.GeoTiff => "GeoTIFF",
            DataSourceKind.MBTiles => "MBTiles",
            _ => string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
