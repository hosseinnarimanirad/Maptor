using System.Windows.Media;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Controls;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The colour maths behind <see cref="MaptorColorPicker"/>: RGB ⇄ HSV, hex text, alpha ⇄ percent.
/// </summary>
public class ColorMathTest
{
    [Theory]
    [InlineData(255, 0, 0, 0, 1, 1)]
    [InlineData(0, 255, 0, 120, 1, 1)]
    [InlineData(0, 0, 255, 240, 1, 1)]
    [InlineData(255, 255, 0, 60, 1, 1)]
    [InlineData(255, 0, 255, 300, 1, 1)]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 0, 0, 1)]
    [InlineData(128, 128, 128, 0, 0, 128 / 255.0)]
    public void ToHsv_KnownColours(byte r, byte g, byte b, double hue, double saturation, double value)
    {
        var hsv = ColorMath.ToHsv(Color.FromRgb(r, g, b));

        Assert.Equal(hue, hsv.Hue, 6);
        Assert.Equal(saturation, hsv.Saturation, 6);
        Assert.Equal(value, hsv.Value, 6);
    }

    [Fact]
    public void ToHsv_HueOfReddishColourJustAboveZero()
    {
        // #B51A00, the dark red the mock-up selects: hue = 60 * (26/181) ≈ 8.62°
        var hsv = ColorMath.ToHsv(Color.FromRgb(0xB5, 0x1A, 0x00));

        Assert.Equal(60.0 * 26 / 181, hsv.Hue, 6);
        Assert.Equal(1, hsv.Saturation, 6);
    }

    [Fact]
    public void ToHsv_MagentaSideHueIsPositive()
    {
        // red is the maximum and blue > green: the raw formula gives a negative hue that must wrap
        var hsv = ColorMath.ToHsv(Color.FromRgb(255, 0, 128));

        Assert.InRange(hsv.Hue, 329.0, 331.0);
    }

    [Fact]
    public void RgbHsvRgb_RoundTripsEveryColourOnACoarseCube()
    {
        for (int r = 0; r <= 255; r += 15)
            for (int g = 0; g <= 255; g += 15)
                for (int b = 0; b <= 255; b += 15)
                {
                    var color = Color.FromArgb(200, (byte)r, (byte)g, (byte)b);

                    Assert.Equal(color, ColorMath.ToHsv(color).ToColor(200));
                }
    }

    [Theory]
    [InlineData(360, 255, 0, 0)]
    [InlineData(720, 255, 0, 0)]
    [InlineData(-120, 0, 0, 255)]
    [InlineData(120, 0, 255, 0)]
    public void FromHsv_HueWraps(double hue, byte r, byte g, byte b)
    {
        Assert.Equal(Color.FromRgb(r, g, b), ColorMath.FromHsv(hue, 1, 1));
    }

    [Fact]
    public void FromHsv_ClampsSaturationAndValue()
    {
        Assert.Equal(Color.FromRgb(255, 0, 0), ColorMath.FromHsv(0, 5, 5));
        Assert.Equal(Color.FromRgb(0, 0, 0), ColorMath.FromHsv(0, -1, -1));
    }

    [Fact]
    public void FromHsv_KeepsAlpha()
    {
        Assert.Equal(64, ColorMath.FromHsv(200, 0.5, 0.5, 64).A);
    }

    [Fact]
    public void ToHex_IsUpperCaseRgbWithoutAlpha()
    {
        Assert.Equal("#B51A00", ColorMath.ToHex(Color.FromArgb(0x80, 0xB5, 0x1A, 0x00)));
    }

    [Theory]
    [InlineData("#B51A00", 0xFF, 0xB5, 0x1A, 0x00)]
    [InlineData("b51a00", 0xFF, 0xB5, 0x1A, 0x00)]
    [InlineData("  #b51a00  ", 0xFF, 0xB5, 0x1A, 0x00)]
    [InlineData("#abc", 0xFF, 0xAA, 0xBB, 0xCC)]
    [InlineData("F0A", 0xFF, 0xFF, 0x00, 0xAA)]
    [InlineData("#80FF0000", 0x80, 0xFF, 0x00, 0x00)]
    public void TryParseHex_AcceptsCommonForms(string text, byte a, byte r, byte g, byte b)
    {
        Assert.True(ColorMath.TryParseHex(text, out var color));
        Assert.Equal(Color.FromArgb(a, r, g, b), color);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("#")]
    [InlineData("#12")]
    [InlineData("#1234")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("#GGGGGG")]
    [InlineData("##B51A00")]
    [InlineData("#B5 1A00")]
    [InlineData("-B51A0")]
    public void TryParseHex_RejectsEverythingElse(string? text)
    {
        Assert.False(ColorMath.TryParseHex(text, out _));
    }

    [Theory]
    [InlineData(255, 100)]
    [InlineData(0, 0)]
    [InlineData(128, 50)]
    [InlineData(127, 50)]
    [InlineData(64, 25)]
    public void AlphaToPercent(byte alpha, int percent)
    {
        Assert.Equal(percent, ColorMath.AlphaToPercent(alpha));
    }

    [Fact]
    public void PercentToAlpha_ClampsAndRoundTripsEveryWholePercent()
    {
        Assert.Equal(255, ColorMath.PercentToAlpha(150));
        Assert.Equal(0, ColorMath.PercentToAlpha(-5));

        for (int p = 0; p <= 100; p++)
            Assert.Equal(p, ColorMath.AlphaToPercent(ColorMath.PercentToAlpha(p)));
    }
}

/// <summary>The closed control's display properties follow <see cref="MaptorColorPicker.SelectedColor"/>.</summary>
[Collection(WpfCollection.Name)]
public class MaptorColorPickerTest
{
    [Fact]
    public void NoColour_ShowsDashAndNoOpacity()
    {
        WpfTestHost.Run(() =>
        {
            var picker = new MaptorColorPicker();

            Assert.False(picker.HasColor);
            Assert.Equal("—", picker.HexText);
            Assert.Equal(string.Empty, picker.OpacityText);
        });
    }

    [Fact]
    public void SelectedColour_UpdatesHexOpacityAndBrush()
    {
        WpfTestHost.Run(() =>
        {
            var picker = new MaptorColorPicker { SelectedColor = Color.FromArgb(128, 0xB5, 0x1A, 0x00) };

            Assert.True(picker.HasColor);
            Assert.Equal("#B51A00", picker.HexText);
            Assert.Equal("50%", picker.OpacityText);

            var brush = Assert.IsType<SolidColorBrush>(picker.SelectedBrush);
            Assert.True(brush.IsFrozen);
            Assert.Equal(Color.FromArgb(128, 0xB5, 0x1A, 0x00), brush.Color);

            picker.SelectedColor = null;

            Assert.False(picker.HasColor);
            Assert.Equal("—", picker.HexText);
        });
    }
}
