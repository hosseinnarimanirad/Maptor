using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using IRI.Maptor.Sta.Persistence.Abstractions;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class DataSourceKindToGeometryConverter : IValueConverter
{
    private const string FallbackKey = "otherInfo";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DataSourceKind kind)
            return GetFallbackGeometry();

        var key = kind switch
        {
            DataSourceKind.Shapefile => "shp",
            DataSourceKind.Kmz => "kmz",
            DataSourceKind.Kml => "kml",
            DataSourceKind.Dxf => "dxf",
            DataSourceKind.WebApi => "rest",
            DataSourceKind.GRPC => "grpc",
            DataSourceKind.GeoJson => "json",
            DataSourceKind.Csv => "csv",
            DataSourceKind.Tsv => "tsv",
            DataSourceKind.GeoTiff => "tif",
            DataSourceKind.GML => "gml",
            DataSourceKind.Worldfile => "wrd",
            DataSourceKind.ZippedImagePyramid => "pyrd",
            DataSourceKind.Other => FallbackKey,
            _ => FallbackKey
        };

        try
        {
            var resource = Application.Current?.FindResource(key);
            if (resource is Geometry geometry)
                return geometry;
        }
        catch (ResourceReferenceKeyNotFoundException)
        {
            // key not in resource tree
        }

        return GetFallbackGeometry();
    }

    private static Geometry GetFallbackGeometry()
    {
        try
        {
            var resource = Application.Current?.FindResource(FallbackKey);
            if (resource is Geometry geometry)
                return geometry;
        }
        catch (ResourceReferenceKeyNotFoundException)
        {
            // ignore
        }

        return Geometry.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
