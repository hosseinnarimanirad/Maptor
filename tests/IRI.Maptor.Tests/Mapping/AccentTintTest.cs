using System.Windows.Media;

using IRI.Maptor.Presentation.Wpf.Helpers;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The blend behind the header bands' accent tint (AccentTint, 2026-11-10). Only the arithmetic: applying a theme
/// here would start ControlzEx's theme manager on a test thread and break every later WPF test on another thread,
/// so ThemeHelper.ApplyAccentTints is checked in the running app.
/// </summary>
public class AccentTintTest
{
    [Fact]
    public void Blend_MixesTheAccentOverTheBackground()
    {
        var white = Colors.White;
        var accent = Color.FromRgb(0x60, 0xA9, 0x17);

        Assert.Equal(white, AccentTint.Blend(white, accent, 0));
        Assert.Equal(accent, AccentTint.Blend(white, accent, 1));

        // 10 %: 255 + (96 - 255) * 0.1 = 239.1 → 239; 255 + (169 - 255) * 0.1 = 246.4 → 246; 231.8 → 232
        Assert.Equal(Color.FromRgb(239, 246, 232), AccentTint.Blend(white, accent, AccentTint.LightAmount));

        // out-of-range amounts are clamped, never overflow a channel
        Assert.Equal(accent, AccentTint.Blend(white, accent, 1.5));
        Assert.Equal(white, AccentTint.Blend(white, accent, -1));
    }

    [Fact]
    public void Blend_OnDark_StaysDarkAndTinted()
    {
        var dark = Color.FromRgb(0x25, 0x25, 0x25);
        var accent = Color.FromRgb(0x60, 0xA9, 0x17);

        var tint = AccentTint.Blend(dark, accent, AccentTint.DarkAmount);

        Assert.True(tint.G > tint.R && tint.G > tint.B, "the tint leans towards the accent's strongest channel");
        Assert.True(tint.G < 0x60, "and is still a dark colour");
        Assert.True(AccentTint.BlendBrush(dark, accent, AccentTint.DarkAmount).IsFrozen);
    }
}
