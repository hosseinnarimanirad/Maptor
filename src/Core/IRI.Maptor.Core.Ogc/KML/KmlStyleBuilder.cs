using System;
using System.Globalization;
using IRI.Maptor.Core.Ogc.Kml.Primitives;

namespace IRI.Maptor.Core.Ogc.Kml;

/// <summary>
/// Builder for creating KML styles (IconStyle, LineStyle, PolyStyle)
/// Provides fluent API for building complex KML styles
/// </summary>
public class KmlStyleBuilder
{
    private readonly StyleType _style;

    public KmlStyleBuilder()
    {
        _style = new StyleType();
    }

    #region Icon Style

    /// <summary>
    /// Sets the icon style for points
    /// </summary>
    /// <param name="iconHref">URL of the icon image</param>
    /// <param name="scale">Scale factor for the icon (default: 1.0)</param>
    /// <param name="color">Color in KML format (aabbggrr in hex)</param>
    /// <param name="colorMode">Color mode (normal or random)</param>
    /// <returns>Builder instance for fluent API</returns>
    public KmlStyleBuilder WithIconStyle(
        string? iconHref = null,
        double scale = 1.0,
        byte[]? color = null,
        ColorModeEnumType colorMode = ColorModeEnumType.Normal)
    {
        var iconStyle = new IconStyleType
        {
            Scale = scale,
            ColorMode = colorMode
        };

        if (color != null)
        {
            iconStyle.Color = color;
        }

        if (!string.IsNullOrEmpty(iconHref))
        {
            iconStyle.Icon = new BasicLinkType
            {
                Href = iconHref
            };
        }

        _style.IconStyle = iconStyle;
        return this;
    }

    /// <summary>
    /// Sets the icon style with color components
    /// </summary>
    /// <param name="iconHref">URL of the icon image</param>
    /// <param name="scale">Scale factor for the icon</param>
    /// <param name="red">Red component (0-255)</param>
    /// <param name="green">Green component (0-255)</param>
    /// <param name="blue">Blue component (0-255)</param>
    /// <param name="alpha">Alpha/transparency (0-255, where 255 is opaque)</param>
    /// <returns>Builder instance for fluent API</returns>
    public KmlStyleBuilder WithIconStyle(
        string iconHref,
        double scale,
        byte red,
        byte green,
        byte blue,
        byte alpha = 255)
    {
        var color = CreateKmlColor(red, green, blue, alpha);
        return WithIconStyle(iconHref, scale, color);
    }

    /// <summary>
    /// Sets the icon hotspot (anchor point)
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="xUnits">X units type</param>
    /// <param name="yUnits">Y units type</param>
    /// <returns>Builder instance for fluent API</returns>
    public KmlStyleBuilder WithIconHotSpot(
        double x,
        double y,
        UnitsEnumType xUnits = UnitsEnumType.Fraction,
        UnitsEnumType yUnits = UnitsEnumType.Fraction)
    {
        if (_style.IconStyle == null)
        {
            _style.IconStyle = new IconStyleType();
        }

        _style.IconStyle.HotSpot = new Vec2Type
        {
            X = x,
            Y = y,
            Xunits = xUnits,
            Yunits = yUnits
        };

        return this;
    }

    #endregion

    #region Line Style

    /// <summary>
    /// Sets the line style for LineStrings
    /// </summary>
    /// <param name="color">Color in KML format (aabbggrr in hex)</param>
    /// <param name="width">Line width in pixels (default: 1.0)</param>
    /// <param name="colorMode">Color mode (normal or random)</param>
    /// <returns>Builder instance for fluent API</returns>
    public KmlStyleBuilder WithLineStyle(
        byte[]? color = null,
        double width = 1.0,
        ColorModeEnumType colorMode = ColorModeEnumType.Normal)
    {
        var lineStyle = new LineStyleType
        {
            Width = width,
            ColorMode = colorMode
        };

        if (color != null)
        {
            lineStyle.Color = color;
        }

        _style.LineStyle = lineStyle;
        return this;
    }

    /// <summary>
    /// Sets the line style with color components
    /// </summary>
    /// <param name="red">Red component (0-255)</param>
    /// <param name="green">Green component (0-255)</param>
    /// <param name="blue">Blue component (0-255)</param>
    /// <param name="alpha">Alpha/transparency (0-255, where 255 is opaque)</param>
    /// <param name="width">Line width in pixels</param>
    /// <returns>Builder instance for fluent API</returns>
    public KmlStyleBuilder WithLineStyle(
        byte red,
        byte green,
        byte blue,
        byte alpha = 255,
        double width = 1.0)
    {
        var color = CreateKmlColor(red, green, blue, alpha);
        return WithLineStyle(color, width);
    }

    #endregion

    #region Poly Style

    /// <summary>
    /// Sets the polygon style
    /// </summary>
    /// <param name="fillColor">Fill color in KML format (aabbggrr in hex)</param>
    /// <param name="fill">Whether to fill the polygon</param>
    /// <param name="outline">Whether to draw the polygon outline</param>
    /// <param name="colorMode">Color mode (normal or random)</param>
    /// <returns>Builder instance for fluent API</returns>
    public KmlStyleBuilder WithPolyStyle(
        byte[]? fillColor = null,
        bool fill = true,
        bool outline = true,
        ColorModeEnumType colorMode = ColorModeEnumType.Normal)
    {
        var polyStyle = new PolyStyleType
        {
            Fill = fill,
            Outline = outline,
            ColorMode = colorMode
        };

        if (fillColor != null)
        {
            polyStyle.Color = fillColor;
        }

        _style.PolyStyle = polyStyle;
        return this;
    }

    /// <summary>
    /// Sets the polygon style with color components
    /// </summary>
    /// <param name="red">Red component (0-255)</param>
    /// <param name="green">Green component (0-255)</param>
    /// <param name="blue">Blue component (0-255)</param>
    /// <param name="alpha">Alpha/transparency (0-255, where 255 is opaque)</param>
    /// <param name="fill">Whether to fill the polygon</param>
    /// <param name="outline">Whether to draw the polygon outline</param>
    /// <returns>Builder instance for fluent API</returns>
    public KmlStyleBuilder WithPolyStyle(
        byte red,
        byte green,
        byte blue,
        byte alpha = 255,
        bool fill = true,
        bool outline = true)
    {
        var color = CreateKmlColor(red, green, blue, alpha);
        return WithPolyStyle(color, fill, outline);
    }

    #endregion

    #region Label Style

    /// <summary>
    /// Sets the label style
    /// </summary>
    /// <param name="color">Label color in KML format (aabbggrr in hex)</param>
    /// <param name="scale">Scale factor for the label (default: 1.0)</param>
    /// <param name="colorMode">Color mode (normal or random)</param>
    /// <returns>Builder instance for fluent API</returns>
    public KmlStyleBuilder WithLabelStyle(
        byte[]? color = null,
        double scale = 1.0,
        ColorModeEnumType colorMode = ColorModeEnumType.Normal)
    {
        var labelStyle = new LabelStyleType
        {
            Scale = scale,
            ColorMode = colorMode
        };

        if (color != null)
        {
            labelStyle.Color = color;
        }

        _style.LabelStyle = labelStyle;
        return this;
    }

    /// <summary>
    /// Sets the label style with color components
    /// </summary>
    /// <param name="red">Red component (0-255)</param>
    /// <param name="green">Green component (0-255)</param>
    /// <param name="blue">Blue component (0-255)</param>
    /// <param name="alpha">Alpha/transparency (0-255, where 255 is opaque)</param>
    /// <param name="scale">Scale factor for the label</param>
    /// <returns>Builder instance for fluent API</returns>
    public KmlStyleBuilder WithLabelStyle(
        byte red,
        byte green,
        byte blue,
        byte alpha = 255,
        double scale = 1.0)
    {
        var color = CreateKmlColor(red, green, blue, alpha);
        return WithLabelStyle(color, scale);
    }

    #endregion

    #region Balloon Style

    /// <summary>
    /// Sets the balloon (popup) style
    /// </summary>
    /// <param name="bgColor">Background color in KML format (aabbggrr in hex)</param>
    /// <param name="textColor">Text color in KML format (aabbggrr in hex)</param>
    /// <param name="text">Balloon text template</param>
    /// <param name="displayMode">Display mode</param>
    /// <returns>Builder instance for fluent API</returns>
    public KmlStyleBuilder WithBalloonStyle(
        byte[]? bgColor = null,
        byte[]? textColor = null,
        string? text = null,
        DisplayModeEnumType displayMode = DisplayModeEnumType.Default)
    {
        var balloonStyle = new BalloonStyleType
        {
            DisplayMode = displayMode
        };

        if (bgColor != null)
        {
            balloonStyle.BgColor = bgColor;
        }

        if (textColor != null)
        {
            balloonStyle.TextColor = textColor;
        }

        if (!string.IsNullOrEmpty(text))
        {
            balloonStyle.Text = text;
        }

        _style.BalloonStyle = balloonStyle;
        return this;
    }

    #endregion

    #region Style ID

    /// <summary>
    /// Sets the style ID (for shared styles)
    /// </summary>
    /// <param name="id">Unique identifier for the style</param>
    /// <returns>Builder instance for fluent API</returns>
    public KmlStyleBuilder WithId(string id)
    {
        _style.Id = id;
        return this;
    }

    #endregion

    #region Build

    /// <summary>
    /// Builds and returns the constructed StyleType
    /// </summary>
    /// <returns>The constructed StyleType</returns>
    public StyleType Build()
    {
        return _style;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a KML color byte array from RGBA components
    /// KML color format is aabbggrr (alpha, blue, green, red in hex)
    /// </summary>
    /// <param name="red">Red component (0-255)</param>
    /// <param name="green">Green component (0-255)</param>
    /// <param name="blue">Blue component (0-255)</param>
    /// <param name="alpha">Alpha/transparency (0-255, where 255 is opaque)</param>
    /// <returns>Byte array representing the color in KML format</returns>
    public static byte[] CreateKmlColor(byte red, byte green, byte blue, byte alpha = 255)
    {
        // KML format: aabbggrr
        return new byte[] { alpha, blue, green, red };
    }

    /// <summary>
    /// Creates a KML color byte array from a hex color string
    /// </summary>
    /// <param name="hexColor">Hex color string (e.g., "#FF0000" for red, or "#80FF0000" for semi-transparent red)</param>
    /// <returns>Byte array representing the color in KML format</returns>
    public static byte[] CreateKmlColorFromHex(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor))
            throw new ArgumentException("Hex color cannot be null or empty", nameof(hexColor));

        hexColor = hexColor.TrimStart('#');

        byte alpha = 255;
        byte red, green, blue;

        if (hexColor.Length == 6)
        {
            // Format: RRGGBB
            red = Convert.ToByte(hexColor.Substring(0, 2), 16);
            green = Convert.ToByte(hexColor.Substring(2, 2), 16);
            blue = Convert.ToByte(hexColor.Substring(4, 2), 16);
        }
        else if (hexColor.Length == 8)
        {
            // Format: AARRGGBB
            alpha = Convert.ToByte(hexColor.Substring(0, 2), 16);
            red = Convert.ToByte(hexColor.Substring(2, 2), 16);
            green = Convert.ToByte(hexColor.Substring(4, 2), 16);
            blue = Convert.ToByte(hexColor.Substring(6, 2), 16);
        }
        else
        {
            throw new ArgumentException("Hex color must be in format RRGGBB or AARRGGBB", nameof(hexColor));
        }

        return CreateKmlColor(red, green, blue, alpha);
    }

    #endregion

    #region Predefined Styles

    /// <summary>
    /// Creates a default point style with a pushpin icon
    /// </summary>
    /// <param name="color">Icon color (default: red)</param>
    /// <returns>A new KmlStyleBuilder with default point style</returns>
    public static KmlStyleBuilder CreateDefaultPointStyle(byte[]? color = null)
    {
        color ??= CreateKmlColor(255, 0, 0, 255); // Red

        return new KmlStyleBuilder()
            .WithIconStyle(
                "http://maps.google.com/mapfiles/kml/pushpin/ylw-pushpin.png",
                scale: 1.0,
                color: color)
            .WithLabelStyle(color, 1.0);
    }

    /// <summary>
    /// Creates a default line style
    /// </summary>
    /// <param name="color">Line color (default: blue)</param>
    /// <param name="width">Line width (default: 2.0)</param>
    /// <returns>A new KmlStyleBuilder with default line style</returns>
    public static KmlStyleBuilder CreateDefaultLineStyle(byte[]? color = null, double width = 2.0)
    {
        color ??= CreateKmlColor(0, 0, 255, 255); // Blue

        return new KmlStyleBuilder()
            .WithLineStyle(color, width);
    }

    /// <summary>
    /// Creates a default polygon style
    /// </summary>
    /// <param name="fillColor">Fill color (default: semi-transparent green)</param>
    /// <param name="outlineColor">Outline color (default: green)</param>
    /// <returns>A new KmlStyleBuilder with default polygon style</returns>
    public static KmlStyleBuilder CreateDefaultPolygonStyle(byte[]? fillColor = null, byte[]? outlineColor = null)
    {
        fillColor ??= CreateKmlColor(0, 255, 0, 128); // Semi-transparent green
        outlineColor ??= CreateKmlColor(0, 255, 0, 255); // Green

        return new KmlStyleBuilder()
            .WithPolyStyle(fillColor, fill: true, outline: true)
            .WithLineStyle(outlineColor, 1.0);
    }

    #endregion
}

/// <summary>
/// Extension methods for applying styles to placemarks
/// </summary>
public static class KmlStyleExtensions
{
    /// <summary>
    /// Applies a style to a placemark
    /// </summary>
    /// <param name="placemark">The placemark to style</param>
    /// <param name="style">The style to apply</param>
    /// <returns>The styled placemark</returns>
    public static PlacemarkType WithStyle(this PlacemarkType placemark, StyleType style)
    {
        placemark.AbstractStyleSelectorGroup.Clear();
        placemark.AbstractStyleSelectorGroup.Add(style);
        return placemark;
    }

    /// <summary>
    /// Applies a style URL reference to a placemark
    /// </summary>
    /// <param name="placemark">The placemark to style</param>
    /// <param name="styleUrl">The style URL reference (e.g., "#myStyle")</param>
    /// <returns>The styled placemark</returns>
    public static PlacemarkType WithStyleUrl(this PlacemarkType placemark, string styleUrl)
    {
        placemark.StyleUrl = styleUrl;
        return placemark;
    }

    public static PlacemarkType WithTimeSpan(this PlacemarkType placemark, DateTime? begin, DateTime? end)
    {
        if (placemark == null)
            throw new ArgumentNullException(nameof(placemark));

        if (begin == null && end == null)
        {
            placemark.AbstractTimePrimitiveGroup = null;
            return placemark;
        }

        var timeSpan = placemark.AbstractTimePrimitiveGroup as TimeSpanType ?? new TimeSpanType();
        timeSpan.Begin = begin?.ToString("o", CultureInfo.InvariantCulture);
        timeSpan.End = end?.ToString("o", CultureInfo.InvariantCulture);
        placemark.AbstractTimePrimitiveGroup = timeSpan;
        return placemark;
    }

    public static PlacemarkType WithRegion(
        this PlacemarkType placemark,
        double north,
        double south,
        double east,
        double west,
        double? minAltitude = null,
        double? maxAltitude = null,
        LodType? lod = null)
    {
        if (placemark == null)
            throw new ArgumentNullException(nameof(placemark));

        var region = placemark.Region ?? new RegionType();
        var latLonAltBox = region.LatLonAltBox ?? new LatLonAltBoxType();

        latLonAltBox.North = north;
        latLonAltBox.NorthSpecified = true;
        latLonAltBox.South = south;
        latLonAltBox.SouthSpecified = true;
        latLonAltBox.East = east;
        latLonAltBox.EastSpecified = true;
        latLonAltBox.West = west;
        latLonAltBox.WestSpecified = true;

        if (minAltitude.HasValue)
        {
            latLonAltBox.MinAltitude = minAltitude.Value;
            latLonAltBox.MinAltitudeSpecified = true;
        }
        else
        {
            latLonAltBox.MinAltitudeSpecified = false;
        }

        if (maxAltitude.HasValue)
        {
            latLonAltBox.MaxAltitude = maxAltitude.Value;
            latLonAltBox.MaxAltitudeSpecified = true;
        }
        else
        {
            latLonAltBox.MaxAltitudeSpecified = false;
        }

        region.LatLonAltBox = latLonAltBox;

        if (lod != null)
        {
            region.Lod = lod;
        }

        placemark.Region = region;
        return placemark;
    }
}

