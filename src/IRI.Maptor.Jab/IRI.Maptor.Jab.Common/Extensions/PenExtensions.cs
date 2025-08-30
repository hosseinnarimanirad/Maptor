using System;
using System.Linq;
using System.Windows.Media;
using IRI.Maptor.Jab.Common.Helpers;

namespace IRI.Maptor.Extensions;

public static class PenExtensions
{
    //public static System.Drawing.Pen AsGdiPen(this Pen pen)
    //{
    //    var brush = pen.Brush as SolidColorBrush;
    //    brush = brush ?? Brushes.Transparent;
    //    System.Drawing.Color color = brush.Color.AsGdiColor();
    //    var gdiPen = new System.Drawing.Pen(color, (float)pen.Thickness);

    //    return gdiPen;
    //} 

    public static System.Drawing.Pen? AsGdiPen(this System.Windows.Media.Pen? pen)
    {
        if (pen == null)
            return null;

        // Get color from brush (default to transparent)
        var brush = pen.Brush as SolidColorBrush ?? Brushes.Transparent;
        System.Drawing.Color color = brush.Color.AsGdiColor();

        var gdiPen = new System.Drawing.Pen(color, (float)pen.Thickness);

        // Dash style
        if (pen.DashStyle != null && pen.DashStyle.Dashes.Count > 0)
        {
            gdiPen.DashPattern = pen.DashStyle.Dashes.Select(d => (float)d).ToArray();
            gdiPen.DashOffset = (float)pen.DashStyle.Offset; 
        }

        // Line caps
        gdiPen.StartCap = pen.StartLineCap.AsGdiLineCap();
        gdiPen.EndCap = pen.EndLineCap.AsGdiLineCap();
        gdiPen.DashCap = pen.DashCap.AsGdiDashCap();

        // Line join
        gdiPen.LineJoin = pen.LineJoin.AsGdiLineJoin();
        gdiPen.MiterLimit = (float)pen.MiterLimit;

        return gdiPen;
    }

     
    private static System.Drawing.Drawing2D.LineCap AsGdiLineCap(this PenLineCap cap)
    {
        return cap switch
        {
            PenLineCap.Flat => System.Drawing.Drawing2D.LineCap.Flat,
            PenLineCap.Round => System.Drawing.Drawing2D.LineCap.Round,
            PenLineCap.Square => System.Drawing.Drawing2D.LineCap.Square,
            PenLineCap.Triangle => System.Drawing.Drawing2D.LineCap.Triangle,
            _ => System.Drawing.Drawing2D.LineCap.Flat,
        };
    }

    private static System.Drawing.Drawing2D.DashCap AsGdiDashCap(this PenLineCap cap)
    {
        return cap switch
        {
            PenLineCap.Flat => System.Drawing.Drawing2D.DashCap.Flat,
            PenLineCap.Round => System.Drawing.Drawing2D.DashCap.Round,
            PenLineCap.Triangle => System.Drawing.Drawing2D.DashCap.Triangle,
            _ => System.Drawing.Drawing2D.DashCap.Flat,
        };
    }

    private static System.Drawing.Drawing2D.LineJoin AsGdiLineJoin(this PenLineJoin join)
    {
        switch (join)
        {
            case PenLineJoin.Bevel: return System.Drawing.Drawing2D.LineJoin.Bevel;
            case PenLineJoin.Miter: return System.Drawing.Drawing2D.LineJoin.Miter;
            case PenLineJoin.Round: return System.Drawing.Drawing2D.LineJoin.Round;
            default: return System.Drawing.Drawing2D.LineJoin.Miter;
        }
    }

}
