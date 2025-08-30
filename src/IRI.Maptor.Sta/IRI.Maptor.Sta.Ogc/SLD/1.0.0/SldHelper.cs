using IRI.Maptor.Sta.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Ogc.SLD;

// https://docs.geoserver.org/main/en/user/styling/sld/reference/linesymbolizer.html
// https://docs.geoserver.org/main/en/user/styling/sld/reference/textsymbolizer.html
// https://docs.geoserver.org/main/en/user/styling/sld/reference/polygonsymbolizer.html
public static class SldHelper
{
    #region Stroke

    /// <summary>
    /// Specifies the solid color given to the line, in the form #RRGGBB. 
    /// Default is black (#000000).
    /// </summary>
    public const string CssParameter_Stroke = "stroke";

    /// <summary>
    /// Specifies the width of the line in pixels. 
    /// Default is 1.
    /// </summary>
    public const string CssParameter_StrokeWidth = "stroke-width";

    /// <summary>
    /// Specifies the opacity (transparency) of the line. 
    /// The value is a number are between 0 (completely transparent) 
    /// and 1 (completely opaque). 
    /// Default is 1.
    /// </summary>
    public const string CssParameter_StrokeOpacity = "stroke-opacity";

    /// <summary>
    /// Determines how lines are rendered at intersections of line segments. 
    /// Possible values are mitre (sharp corner), round (rounded corner), 
    /// and bevel (diagonal corner). Default is mitre.
    /// </summary>
    public const string CssParameter_StrokeLineJoin = "stroke-linejoin";

    /// <summary>
    /// Determines how lines are rendered at their ends. Possible values 
    /// are butt (sharp square edge), round (rounded edge), and square 
    /// (slightly elongated square edge). Default is butt.
    /// </summary>
    public const string CssParameter_StrokeLineCap = "stroke-linecap";

    /// <summary>
    /// Encodes a dash pattern as a series of numbers separated by spaces. 
    /// Odd-indexed numbers (first, third, etc) determine the length in pxiels 
    /// to draw the line, and even-indexed numbers (second, fourth, etc) 
    /// determine the length in pixels to blank out the line. Default is an 
    /// unbroken line. Starting from version 2.1 dash arrays can be combined 
    /// with graphic strokes to generate complex line styles with alternating 
    /// symbols or a mix of lines and symbols.
    /// </summary>
    public const string CssParameter_StrokeDashArray = "stroke-dasharray";

    /// <summary>
    /// Specifies the distance in pixels into the dasharray pattern at 
    /// which to start drawing. Default is 0.
    /// </summary>
    public const string CssParameter_StrokeDashOffset = "stroke-dashoffset";

    #endregion


    #region Fill

    /// <summary>
    /// Specifies the fill color, in the form #RRGGBB. 
    /// Default is grey (#808080).
    /// </summary>
    public const string CssParameter_Fill = "fill";

    /// <summary>
    /// Specifies the opacity (transparency) of the fill. The value is a decimal 
    /// number between 0 (completely transparent) and 1 (completely opaque). 
    /// Default is 1.
    /// </summary>
    public const string CssParameter_FillOpacity = "fill-opacity";

    #endregion


    #region Font

    /// <summary>
    /// The family name of the font to use for the label. Default is Times.
    /// </summary>
    public const string CssParameter_FontFamily = "font-family";

    /// <summary>
    /// The style of the font. Options are normal, italic, and oblique. 
    /// Default is normal.
    /// </summary>
    public const string CssParameter_FontStyle = "font-style";

    /// <summary>
    /// The weight of the font. Options are normal and bold. 
    /// Default is normal.
    /// </summary>
    public const string CssParameter_FontWeight = "font-weight";

    /// <summary>
    /// The size of the font in pixels. Default is 10.
    /// </summary>
    public const string CssParameter_FontSize = "font-size";

    #endregion

    public static StyledLayerDescriptor? Parse(string? xmlSld)
    {
        if (string.IsNullOrWhiteSpace(xmlSld))
            return null;

        try
        {
            return XmlHelper.DeserializeFromXmlString<StyledLayerDescriptor>(xmlSld);
        }
        catch (Exception)
        {
            return null;
        }
    }
}