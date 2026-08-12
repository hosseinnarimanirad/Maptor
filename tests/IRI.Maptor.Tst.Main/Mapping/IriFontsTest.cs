using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using IRI.Maptor.Jab.Wpf.Assets.Fonts;
using IRI.Maptor.Tst.Main.Common;
using Xunit;

namespace IRI.Maptor.Tst.Main.Mapping;

[Collection(WpfCollection.Name)]
public class IriFontsTest
{
    /// <summary>
    /// Guards the FontFamily construction: with a bare family name WPF silently looks up a
    /// SYSTEM-installed font and falls back to the default font when IRANSans isn't installed —
    /// which broke Farsi text in the printed PDF legend. The "./#IRANSans" composite form must
    /// resolve the ttf packaged in Jab.Wpf's resources, so shaping a Farsi run with it must
    /// produce different metrics than an unresolvable family (which is pure fallback).
    /// </summary>
    [Fact]
    public void IranSans_ResolvesToPackagedFontFace()
    {
        double iranSansWidth = 0, fallbackWidth = 0;

        WpfTestHost.Run(() =>
        {
            const string farsi = "نقشه راه‌های استان تهران";

            iranSansWidth = MeasureWidth(farsi, IriFonts.IranSans);
            fallbackWidth = MeasureWidth(farsi, new FontFamily("ThisFamilyDoesNotExist-1234"));
        });

        Assert.True(
            Math.Abs(iranSansWidth - fallbackWidth) > 0.5,
            $"IriFonts.IranSans shaped identically to the fallback font ({iranSansWidth:F2} vs {fallbackWidth:F2}) — " +
            "the packaged IRANSans.ttf did not resolve and text silently fell back to the default font");
    }

    private static double MeasureWidth(string text, FontFamily family)
    {
        var typeface = new Typeface(family, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.RightToLeft,
            typeface,
            32,
            Brushes.Black,
            1.0);

        return formatted.WidthIncludingTrailingWhitespace;
    }
}
