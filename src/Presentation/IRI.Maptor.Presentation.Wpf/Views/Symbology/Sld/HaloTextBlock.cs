using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Controls.Symbology.Sld;

/// <summary>
/// One line of text with a map-style halo: the glyph outlines stroked in the halo colour, then filled in the
/// text colour on top. The SLD editor's preview draws its sample label with it, so a halo looks as it does on
/// the map rather than as a soft glow.
/// </summary>
public class HaloTextBlock : FrameworkElement
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(HaloTextBlock),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(
        nameof(FontFamily), typeof(FontFamily), typeof(HaloTextBlock),
        new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
        nameof(FontSize), typeof(double), typeof(HaloTextBlock),
        new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register(
        nameof(FontWeight), typeof(FontWeight), typeof(HaloTextBlock),
        new FrameworkPropertyMetadata(FontWeights.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register(
        nameof(FontStyle), typeof(FontStyle), typeof(HaloTextBlock),
        new FrameworkPropertyMetadata(FontStyles.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
        nameof(Foreground), typeof(Brush), typeof(HaloTextBlock),
        new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty HaloBrushProperty = DependencyProperty.Register(
        nameof(HaloBrush), typeof(Brush), typeof(HaloTextBlock),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>The halo's full width around each glyph; 0 draws no halo.</summary>
    public static readonly DependencyProperty HaloWidthProperty = DependencyProperty.Register(
        nameof(HaloWidth), typeof(double), typeof(HaloTextBlock),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    public FontFamily FontFamily { get => (FontFamily)GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }

    public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }

    public FontWeight FontWeight { get => (FontWeight)GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }

    public FontStyle FontStyle { get => (FontStyle)GetValue(FontStyleProperty); set => SetValue(FontStyleProperty, value); }

    public Brush Foreground { get => (Brush)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    public Brush? HaloBrush { get => (Brush?)GetValue(HaloBrushProperty); set => SetValue(HaloBrushProperty, value); }

    public double HaloWidth { get => (double)GetValue(HaloWidthProperty); set => SetValue(HaloWidthProperty, value); }

    private FormattedText CreateText()
    {
        var typeface = new Typeface(FontFamily ?? SystemFonts.MessageFontFamily, FontStyle, FontWeight, FontStretches.Normal);

        return new FormattedText(Text ?? string.Empty, CultureInfo.CurrentUICulture, FlowDirection, typeface,
                                 FontSize > 0 ? FontSize : 12, Foreground, VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var text = CreateText();

        return new Size(text.WidthIncludingTrailingWhitespace + HaloWidth, text.Height + HaloWidth);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var text = CreateText();
        var origin = new Point(HaloWidth / 2, HaloWidth / 2);

        if (HaloWidth > 0 && HaloBrush is not null)
        {
            var pen = new Pen(HaloBrush, HaloWidth) { LineJoin = PenLineJoin.Round };
            drawingContext.DrawGeometry(null, pen, text.BuildGeometry(origin));
        }

        drawingContext.DrawText(text, origin);
    }
}
