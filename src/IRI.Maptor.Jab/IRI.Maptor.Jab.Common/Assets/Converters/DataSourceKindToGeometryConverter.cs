using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.ShapefileFormat;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.Spatial.IO.TopoJson;
using IRI.Maptor.Sta.Spatial.IO;
using SixLabors.ImageSharp.ColorSpaces;
using MahApps.Metro.IconPacks;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class DataSourceKindToGeometryConverter : IValueConverter
{
    //private const string FallbackKey = "otherInfo";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DataSourceKind kind)
            return Geometry.Empty;

        var key = kind switch
        {
            DataSourceKind.Shapefile => "shp",
            DataSourceKind.Kmz => "kmz",
            DataSourceKind.Kml => "kml",
            DataSourceKind.Gpx => "gpx",
            DataSourceKind.Dxf => "dxf",
            DataSourceKind.WebApi => "rest",
            DataSourceKind.GRPC => "grpc",
            DataSourceKind.GeoJson => "json",
            DataSourceKind.TopoJson => "topo",
            DataSourceKind.Csv => "csv",
            DataSourceKind.Tsv => "tsv",
            DataSourceKind.GeoTiff => "tif",
            DataSourceKind.GML => "gml",
            DataSourceKind.Worldfile => "wrd",
            DataSourceKind.ZippedImagePyramid => "pyrd",
            DataSourceKind.Other or _ => string.Empty,
        };

        var resource = Application.Current?.FindResource(key);

        if (resource is Geometry geometry)
            return geometry;

        else
            return Geometry.Empty;
         
    }

    //private static Geometry GetFallbackGeometry()
    //{
    //    ////var data = new PackIconMaterial() { Kind = PackIconMaterialKind.StretchToPageOutline }.Data;

    //    ////return Geometry.Parse(data);

    //    //try
    //    //{
    //    //    var resource = Application.Current?.FindResource(FallbackKey);
    //    //    if (resource is Geometry geometry)
    //    //        return geometry;
    //    //}
    //    //catch (ResourceReferenceKeyNotFoundException)
    //    //{
    //    //    // ignore
    //    //}

    //    //return Geometry.Empty;
    //}

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
