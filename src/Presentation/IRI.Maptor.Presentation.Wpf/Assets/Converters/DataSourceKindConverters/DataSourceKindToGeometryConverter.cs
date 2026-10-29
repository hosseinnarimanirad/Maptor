using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Presentation.Wpf.Converters;

public class DataSourceKindToGeometryConverter : IValueConverter
{
    // kinds without a hand-drawn glyph in the Shapes dictionaries get their short code drawn from
    // a condensed bold font instead of an empty chip
    private static readonly ConcurrentDictionary<DataSourceKind, Geometry> _textGlyphs = new();

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
            DataSourceKind.EsriJson => "json",
            DataSourceKind.TopoJson => "topo",
            DataSourceKind.Csv => "csv",
            DataSourceKind.Tsv => "tsv",
            DataSourceKind.GeoTiff => "tif",
            DataSourceKind.GML => "gml",
            DataSourceKind.Worldfile => "wrd",
            DataSourceKind.ZippedImagePyramid => "pyrd",
            DataSourceKind.Other or _ => string.Empty,
        };

        if (string.IsNullOrEmpty(key))
            return GetTextGlyph(kind);

        try
        {
            // this method throws exception if key not found
            var resource = Application.Current?.FindResource(key);

            if (resource is Geometry geometry)
                return geometry;

            else
                return GetTextGlyph(kind);
        }
        catch (Exception)
        {
            return GetTextGlyph(kind);
        }

    }

    private static Geometry GetTextGlyph(DataSourceKind kind)
    {
        var code = kind switch
        {
            DataSourceKind.MBTiles => "MBT",
            DataSourceKind.GeoPackage => "GPKG",
            _ => string.Empty
        };

        if (code.Length == 0)
            return Geometry.Empty;

        return _textGlyphs.GetOrAdd(kind, _ =>
        {
            var typeface = new Typeface(new FontFamily("Bahnschrift, Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Condensed);

            // pixelsPerDip only affects hinting; the Path stretches the geometry anyway
            var text = new FormattedText(code, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 36, Brushes.Black, 1.0);

            var geometry = text.BuildGeometry(new Point(0, 0));
            geometry.Freeze();

            return geometry;
        });
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
