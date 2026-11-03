using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using IRI.Maptor.Presentation.Wpf.Controls;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// <see cref="MaptorColorPickerPanel"/>: one HSV + alpha state behind every tab, events only for user
/// edits, and the hue kept through greys. Parts are reached by name, as a user's input reaches them.
/// </summary>
[Collection(WpfCollection.Name)]
public class MaptorColorPickerPanelTest
{
    private static bool s_resourcesLoaded;

    /// <summary>The panel's XAML uses app-level styles (sizes, effects, swatch tokens), as in the apps.</summary>
    private static MaptorColorPickerPanel CreatePanel()
    {
        if (!s_resourcesLoaded)
        {
            foreach (var source in new[]
                     {
                         "pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml",
                         "pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml",
                         "pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Green.xaml",
                         "pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/Maptor.All.xaml",
                     })
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new System.Uri(source) });
            }

            s_resourcesLoaded = true;
        }

        return new MaptorColorPickerPanel();
    }

    private static T Part<T>(MaptorColorPickerPanel panel, string name) where T : class =>
        Assert.IsAssignableFrom<T>(panel.FindName(name));

    [Fact]
    public void SetColor_ShowsTheColourAndRaisesNothing()
    {
        WpfTestHost.Run(() =>
        {
            var panel = CreatePanel();
            var raised = 0;
            panel.ColorChanged += (_, _) => raised++;

            var color = Color.FromArgb(0x80, 0xB5, 0x1A, 0x00);
            panel.SetColor(color);

            Assert.Equal(color, panel.Color);
            Assert.Equal(0, raised);
        });
    }

    [Fact]
    public void SpectrumEdit_RaisesTheNewColourAndKeepsOpacity()
    {
        WpfTestHost.Run(() =>
        {
            var panel = CreatePanel();
            panel.SetColor(Color.FromArgb(0x80, 0x00, 0x00, 0xFF));

            var changes = new List<Color>();
            panel.ColorChanged += (_, c) => changes.Add(c);

            Part<ColorSpectrumBox>(panel, "spectrum").Brightness = 0.5;

            var last = Assert.Single(changes);
            Assert.Equal(Color.FromArgb(0x80, 0x00, 0x00, 0x80), last);
        });
    }

    [Fact]
    public void Grey_KeepsTheHueForTheNextEdit()
    {
        WpfTestHost.Run(() =>
        {
            var panel = CreatePanel();
            panel.SetColor(Colors.Blue);
            panel.SetColor(Color.FromRgb(0x80, 0x80, 0x80));

            Color? changed = null;
            panel.ColorChanged += (_, c) => changed = c;

            // Back to full saturation: blue again (at the grey's brightness), not red (hue 0).
            Part<ColorSpectrumBox>(panel, "spectrum").Saturation = 1;

            Assert.Equal(Color.FromRgb(0x00, 0x00, 0x80), changed);
        });
    }

    [Fact]
    public void RgbChannel_SetsThatChannelOnly()
    {
        WpfTestHost.Run(() =>
        {
            var panel = CreatePanel();
            panel.SetColor(Color.FromRgb(0x10, 0x20, 0x30));

            Color? changed = null;
            panel.ColorChanged += (_, c) => changed = c;

            Part<ColorChannelSlider>(panel, "channel1").Value = 1;   // red, the default RGB mode

            Assert.Equal(Color.FromRgb(0xFF, 0x20, 0x30), changed);
        });
    }

    [Fact]
    public void Opacity_SetsAlphaOnly()
    {
        WpfTestHost.Run(() =>
        {
            var panel = CreatePanel();
            panel.SetColor(Color.FromRgb(0x2F, 0x6E, 0x8C));

            Color? changed = null;
            panel.ColorChanged += (_, c) => changed = c;

            Part<ColorChannelSlider>(panel, "alphaSlider").Value = 0.5;

            Assert.Equal(Color.FromArgb(128, 0x2F, 0x6E, 0x8C), changed);
        });
    }

    [Fact]
    public void RecentColours_NewestFirstNoDuplicatesCapped()
    {
        // The list is session-wide (static), so this test only reasons about colours it adds itself.
        var colours = new List<Color>();
        for (byte i = 1; i <= MaptorColorPickerPanel.RecentCapacity + 2; i++)
            colours.Add(Color.FromArgb(200, i, 0x42, 0x24));

        foreach (var c in colours)
            MaptorColorPickerPanel.RememberRecent(c);

        var recent = MaptorColorPickerPanel.RecentColors;
        Assert.Equal(MaptorColorPickerPanel.RecentCapacity, recent.Count);
        Assert.Equal(colours[^1], recent[0]);
        Assert.DoesNotContain(colours[0], recent);   // the oldest fell off

        // remembering one already there moves it to the front instead of adding it again
        var again = recent[3];
        MaptorColorPickerPanel.RememberRecent(again);

        Assert.Equal(again, MaptorColorPickerPanel.RecentColors[0]);
        Assert.Equal(MaptorColorPickerPanel.RecentCapacity, MaptorColorPickerPanel.RecentColors.Count);
        Assert.Single(MaptorColorPickerPanel.RecentColors, c => c == again);

        // the same RGB with another opacity is a different recent colour
        MaptorColorPickerPanel.RememberRecent(Color.FromArgb(100, again.R, again.G, again.B));
        Assert.Equal(again, MaptorColorPickerPanel.RecentColors[1]);
    }

    [Fact]
    public void Palettes_HaveTheirShapesAndNoDuplicates()
    {
        Assert.Equal(120, ColorPalettes.GridColors.Count);
        Assert.Equal(120, new HashSet<Color>(ColorPalettes.GridColors).Count);
        Assert.Equal(48, ColorPalettes.BrandColors.Count);
        Assert.Equal(48, new HashSet<Color>(ColorPalettes.BrandColors).Count);
        Assert.Contains(Color.FromRgb(0x4E, 0x81, 0x59), ColorPalettes.BrandColors); // moss itself
    }
}
