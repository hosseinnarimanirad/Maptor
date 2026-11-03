using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using Strings = IRI.Maptor.Presentation.Core.Properties.Resources;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// The pop-up of <see cref="MaptorColorPicker"/>: Grid / Spectrum / Sliders / Brand tabs, then the
/// shared opacity track, preview, hex and opacity boxes.
/// <para>
/// One state (hue, saturation, brightness, alpha) drives every part. The hue is kept separately so it
/// survives greys and black, where a colour has no hue of its own. <see cref="SetColor"/> loads a colour
/// without raising anything; every user edit raises <see cref="ColorChanged"/>.
/// </para>
/// </summary>
public partial class MaptorColorPickerPanel : UserControl
{
    // The tab last used, for the rest of the session (all pickers share it).
    private static int s_lastTab;

    /// <summary>How many recent colours are kept (one row of the pop-up).</summary>
    public const int RecentCapacity = 7;

    // Recent colours for the rest of the session, newest first, shared by all pickers.
    private static readonly List<Color> s_recent = new();

    /// <summary>The session's recent colours, newest first (at most <see cref="RecentCapacity"/>).</summary>
    public static IReadOnlyList<Color> RecentColors => s_recent;

    /// <summary>
    /// Puts <paramref name="color"/> first in the recent colours. An equal colour already there (same
    /// RGB and alpha) moves to the front instead of appearing twice; the oldest falls off the end.
    /// </summary>
    public static void RememberRecent(Color color)
    {
        s_recent.Remove(color);
        s_recent.Insert(0, color);

        if (s_recent.Count > RecentCapacity)
            s_recent.RemoveRange(RecentCapacity, s_recent.Count - RecentCapacity);
    }

    private double _hue, _saturation, _brightness = 1;
    private byte _alpha = 255;

    // Set while the panel itself pushes values into its parts, so their change events don't loop back.
    private bool _refreshing;

    private readonly List<SwatchItem> _grid;
    private readonly List<SwatchItem> _brand;
    private readonly RadioButton[] _tabs;
    private readonly FrameworkElement[] _pages;

    /// <summary>Raised on every change the user makes in the panel.</summary>
    public event EventHandler<Color>? ColorChanged;

    /// <summary>Raised when the user presses Enter outside a text box (keep the colour and close).</summary>
    public event EventHandler? CommitRequested;

    /// <summary>Raised when the user presses Esc (restore the colour the pop-up opened with and close).</summary>
    public event EventHandler? CancelRequested;

    public MaptorColorPickerPanel()
    {
        InitializeComponent();

        _grid = ColorPalettes.GridColors.Select(c => new SwatchItem(c)).ToList();
        _brand = ColorPalettes.BrandColors.Select(c => new SwatchItem(c)).ToList();
        gridSwatches.ItemsSource = _grid;
        brandSwatches.ItemsSource = _brand;

        _tabs = new[] { tabGrid, tabSpectrum, tabSliders, tabBrand };
        _pages = new FrameworkElement[] { gridPage, spectrumPage, slidersPage, brandSwatches };

        var rainbow = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        for (int i = 0; i <= 12; i++)
            rainbow.GradientStops.Add(new GradientStop(ColorMath.FromHsv(i * 30, 1, 1), i / 12.0));
        rainbow.Freeze();
        hueSlider.TrackBrush = rainbow;

        spectrum.ValueChanged += (_, _) => OnPart(() => { _saturation = spectrum.Saturation; _brightness = spectrum.Brightness; });
        hueSlider.ValueChanged += (_, _) => OnPart(() => _hue = hueSlider.Value * 360);
        alphaSlider.ValueChanged += (_, _) => OnPart(() => _alpha = (byte)Math.Round(alphaSlider.Value * 255));
        channel1.ValueChanged += (_, _) => OnPart(() => SetChannel(0, channel1.Value));
        channel2.ValueChanged += (_, _) => OnPart(() => SetChannel(1, channel2.Value));
        channel3.ValueChanged += (_, _) => OnPart(() => SetChannel(2, channel3.Value));

        foreach (var box in new[] { hexBox, opacityBox, channel1Box, channel2Box, channel3Box })
        {
            box.KeyDown += Box_KeyDown;
            box.LostKeyboardFocus += (s, _) => CommitBox((TextBox)s);
            box.GotKeyboardFocus += (s, _) => ((TextBox)s).SelectAll();
        }

        PreviewKeyDown += Panel_PreviewKeyDown;

        _tabs[Math.Clamp(s_lastTab, 0, _tabs.Length - 1)].IsChecked = true;
        UpdateChannelLabels();
        ReloadRecent();
        Refresh();
    }

    /// <summary>The colour currently shown.</summary>
    public Color Color => ColorMath.FromHsv(_hue, _saturation, _brightness, _alpha);

    /// <summary>
    /// Shows <paramref name="color"/> without raising <see cref="ColorChanged"/>. A grey keeps the current
    /// hue. Also reloads the recent colours, which another picker may have changed since.
    /// </summary>
    public void SetColor(Color color)
    {
        ReloadRecent();

        var hsv = ColorMath.ToHsv(color);

        if (hsv.Saturation > 0 && hsv.Value > 0)
            _hue = hsv.Hue;

        _saturation = hsv.Saturation;
        _brightness = hsv.Value;
        _alpha = color.A;

        Refresh();
    }

    /// <summary>Moves keyboard focus to the selected tab (called when the pop-up opens).</summary>
    public void FocusSelectedTab() => _tabs.FirstOrDefault(t => t.IsChecked == true)?.Focus();

    #region Edits from the parts

    private void OnPart(Action apply)
    {
        if (_refreshing)
            return;

        apply();
        Refresh();
        ColorChanged?.Invoke(this, Color);
    }

    private bool IsRgbMode => modeRgb.IsChecked == true;

    private void SetChannel(int index, double value)
    {
        if (IsRgbMode)
        {
            var c = Color;
            byte v = (byte)Math.Round(value * 255);
            var rgb = index switch
            {
                0 => Color.FromArgb(_alpha, v, c.G, c.B),
                1 => Color.FromArgb(_alpha, c.R, v, c.B),
                _ => Color.FromArgb(_alpha, c.R, c.G, v),
            };
            LoadKeepingHue(rgb);
        }
        else
        {
            switch (index)
            {
                case 0: _hue = value * 360; break;
                case 1: _saturation = value; break;
                default: _brightness = value; break;
            }
        }
    }

    private void LoadKeepingHue(Color color)
    {
        var hsv = ColorMath.ToHsv(color);

        if (hsv.Saturation > 0 && hsv.Value > 0)
            _hue = hsv.Hue;

        _saturation = hsv.Saturation;
        _brightness = hsv.Value;
        _alpha = color.A;
    }

    private void Swatch_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is SwatchItem item)
            OnPart(() => LoadKeepingHue(Color.FromArgb(_alpha, item.Color.R, item.Color.G, item.Color.B)));
    }

    /// <summary>
    /// Double-click on a Grid, Brand or recent swatch: the colour is already applied by the first
    /// click of the pair, so this only keeps it and closes the pop-up (like Enter).
    /// </summary>
    private void Swatch_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        CommitRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    #endregion

    #region Recent colours

    private void ReloadRecent() => recentSwatches.ItemsSource = s_recent.Select(c => new SwatchItem(c)).ToList();

    /// <summary>A recent colour is applied whole, with the opacity it was saved with.</summary>
    private void Recent_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is SwatchItem item)
            OnPart(() => LoadKeepingHue(item.Color));
    }

    private void AddRecent_Click(object sender, RoutedEventArgs e)
    {
        RememberRecent(Color);
        ReloadRecent();
    }

    #endregion

    #region Eyedropper

    // While picking, the panel holds the mouse: the cursor can roam the whole screen, the preview
    // follows the pixel under it, a click takes it and Esc (or losing the mouse) puts things back.
    // Nothing is raised while aiming, so the map does not redraw for every pixel passed over.
    private bool _picking;
    private (double Hue, double Saturation, double Brightness) _beforePick;

    /// <summary>True while the eyedropper is following the cursor.</summary>
    public bool IsPicking => _picking;

    private void Eyedropper_Click(object sender, RoutedEventArgs e)
    {
        if (_picking)
            EndPick(commit: false);
        else
            StartPick();
    }

    private void StartPick()
    {
        _beforePick = (_hue, _saturation, _brightness);
        _picking = true;
        eyedropperButton.IsChecked = true;

        Mouse.OverrideCursor = Cursors.Cross;
        CaptureMouse();

        PreviewMouseMove += Pick_MouseMove;
        PreviewMouseLeftButtonDown += Pick_MouseDown;
        LostMouseCapture += Pick_LostCapture;
    }

    private void EndPick(bool commit)
    {
        if (!_picking)
            return;

        _picking = false;

        PreviewMouseMove -= Pick_MouseMove;
        PreviewMouseLeftButtonDown -= Pick_MouseDown;
        LostMouseCapture -= Pick_LostCapture;

        Mouse.OverrideCursor = null;

        if (IsMouseCaptured)
            ReleaseMouseCapture();

        eyedropperButton.IsChecked = false;

        if (commit)
        {
            ColorChanged?.Invoke(this, Color);
        }
        else
        {
            (_hue, _saturation, _brightness) = _beforePick;
            Refresh();
        }
    }

    private void Pick_MouseMove(object sender, MouseEventArgs e)
    {
        if (ScreenColorSampler.ColorUnderCursor() is { } sampled)
        {
            // The current opacity stays: a screen pixel carries none.
            LoadKeepingHue(Color.FromArgb(_alpha, sampled.R, sampled.G, sampled.B));
            Refresh();
        }
    }

    private void Pick_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Pick_MouseMove(sender, e);
        EndPick(commit: true);
        e.Handled = true;
    }

    private void Pick_LostCapture(object sender, MouseEventArgs e) => EndPick(commit: false);

    #endregion

    #region Tabs

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (_tabs is null)
            return; // during InitializeComponent

        int index = Array.IndexOf(_tabs, sender);

        for (int i = 0; i < _pages.Length; i++)
            _pages[i].Visibility = i == index ? Visibility.Visible : Visibility.Collapsed;

        s_lastTab = index;
    }

    private void SliderMode_Checked(object sender, RoutedEventArgs e)
    {
        if (_tabs is null)
            return;

        UpdateChannelLabels();
        Refresh();
    }

    /// <summary>
    /// Binds (not assigns) the three channel labels, so they follow a language switch like the rest of
    /// the panel's text. The binding holds the localization manager weakly, so panels are not leaked.
    /// </summary>
    private void UpdateChannelLabels()
    {
        var keys = IsRgbMode
            ? new[] { nameof(Strings.colorPicker_channel_red), nameof(Strings.colorPicker_channel_green), nameof(Strings.colorPicker_channel_blue) }
            : new[] { nameof(Strings.colorPicker_channel_hue), nameof(Strings.colorPicker_channel_saturation), nameof(Strings.colorPicker_channel_brightness) };

        var labels = new[] { channel1Label, channel2Label, channel3Label };

        for (int i = 0; i < 3; i++)
            labels[i].SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding($"[{keys[i]}]") { Source = LocalizationManager.Instance });
    }

    #endregion

    #region Refresh

    /// <summary>Pushes the state into every part. Text boxes the user is typing in are left alone.</summary>
    private void Refresh()
    {
        _refreshing = true;

        try
        {
            var color = Color;
            var opaque = Color.FromRgb(color.R, color.G, color.B);
            var pureHue = ColorMath.FromHsv(_hue, 1, 1);

            spectrum.Hue = _hue;
            spectrum.Saturation = _saturation;
            spectrum.Brightness = _brightness;

            hueSlider.Value = _hue / 360;
            hueSlider.ThumbColor = pureHue;

            alphaSlider.Value = _alpha / 255.0;
            alphaSlider.ThumbColor = opaque;
            alphaSlider.TrackBrush = Gradient(Color.FromArgb(0, color.R, color.G, color.B), opaque);

            if (IsRgbMode)
            {
                // Pure channel tracks (black → red / green / blue), whatever the other channels are:
                // a track that previews the result made "Red" start green, which read as wrong (user, 2026-11-03).
                SetChannelPart(channel1, channel1Box, color.R / 255.0, color.R.ToString(CultureInfo.InvariantCulture),
                    RedTrack, Color.FromRgb(color.R, 0, 0));
                SetChannelPart(channel2, channel2Box, color.G / 255.0, color.G.ToString(CultureInfo.InvariantCulture),
                    GreenTrack, Color.FromRgb(0, color.G, 0));
                SetChannelPart(channel3, channel3Box, color.B / 255.0, color.B.ToString(CultureInfo.InvariantCulture),
                    BlueTrack, Color.FromRgb(0, 0, color.B));
            }
            else
            {
                SetChannelPart(channel1, channel1Box, _hue / 360, Whole(_hue) + "°", hueSlider.TrackBrush, pureHue);
                SetChannelPart(channel2, channel2Box, _saturation, Whole(_saturation * 100),
                    Gradient(ColorMath.FromHsv(_hue, 0, _brightness), ColorMath.FromHsv(_hue, 1, _brightness)), opaque);
                SetChannelPart(channel3, channel3Box, _brightness, Whole(_brightness * 100),
                    Gradient(Colors.Black, ColorMath.FromHsv(_hue, _saturation, 1)), opaque);
            }

            var brush = new SolidColorBrush(color);
            brush.Freeze();
            preview.Background = brush;

            SetIfIdle(hexBox, ColorMath.ToHex(color).Substring(1));
            SetIfIdle(opacityBox, ColorMath.AlphaToPercent(_alpha).ToString(CultureInfo.InvariantCulture));

            foreach (var item in _grid.Concat(_brand))
                item.IsSelected = item.Color.R == color.R && item.Color.G == color.G && item.Color.B == color.B;
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void SetChannelPart(ColorChannelSlider slider, TextBox box, double value, string text, Brush track, Color thumb)
    {
        slider.Value = value;
        slider.TrackBrush = track;
        slider.ThumbColor = thumb;
        SetIfIdle(box, text);
    }

    private static void SetIfIdle(TextBox box, string text)
    {
        if (!box.IsKeyboardFocused && box.Text != text)
            box.Text = text;
    }

    private static string Whole(double value) => ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture);

    private static readonly Brush RedTrack = Gradient(Colors.Black, Color.FromRgb(255, 0, 0));
    private static readonly Brush GreenTrack = Gradient(Colors.Black, Color.FromRgb(0, 255, 0));
    private static readonly Brush BlueTrack = Gradient(Colors.Black, Color.FromRgb(0, 0, 255));

    private static Brush Gradient(Color from, Color to)
    {
        var brush = new LinearGradientBrush(from, to, new Point(0, 0), new Point(1, 0));
        brush.Freeze();
        return brush;
    }

    #endregion

    #region Text boxes and keys

    private void Box_KeyDown(object sender, KeyEventArgs e)
    {
        var box = (TextBox)sender;

        if (e.Key == Key.Enter)
        {
            CommitBox(box);
            box.SelectAll();
            e.Handled = true;
        }
        else if ((e.Key == Key.Up || e.Key == Key.Down) && box != hexBox)
        {
            // Arrow keys step a number box; Shift steps by 10.
            if (TryParseNumber(box.Text, out var n))
            {
                n += (e.Key == Key.Up ? 1 : -1) * ((Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? 10 : 1);
                box.Text = n.ToString(CultureInfo.InvariantCulture);
                CommitBox(box);
                box.SelectAll();
            }

            e.Handled = true;
        }
    }

    /// <summary>Applies a box's text; text that doesn't parse is replaced by the current value.</summary>
    private void CommitBox(TextBox box)
    {
        if (box == hexBox)
        {
            if (ColorMath.TryParseHex(ToAsciiDigits(box.Text), out var parsed))
            {
                // Six or three digits keep the current opacity; eight carry their own.
                var keepAlpha = box.Text.Trim().TrimStart('#').Length != 8;
                OnPart(() => LoadKeepingHue(keepAlpha ? Color.FromArgb(_alpha, parsed.R, parsed.G, parsed.B) : parsed));
            }
        }
        else if (TryParseNumber(box.Text, out var n))
        {
            if (box == opacityBox)
                OnPart(() => _alpha = ColorMath.PercentToAlpha(n));
            else
            {
                int index = box == channel1Box ? 0 : box == channel2Box ? 1 : 2;
                double max = IsRgbMode ? 255 : index == 0 ? 360 : 100;
                OnPart(() => SetChannel(index, Math.Clamp(n, 0, max) / max));
            }
        }

        // Refresh skips the box being typed in, so set its canonical text here (also after a rejected input).
        box.Text = CanonicalText(box);
    }

    private string CanonicalText(TextBox box)
    {
        var c = Color;

        if (box == hexBox) return ColorMath.ToHex(c).Substring(1);
        if (box == opacityBox) return ColorMath.AlphaToPercent(_alpha).ToString(CultureInfo.InvariantCulture);

        int index = box == channel1Box ? 0 : box == channel2Box ? 1 : 2;

        if (IsRgbMode)
            return (index == 0 ? c.R : index == 1 ? c.G : c.B).ToString(CultureInfo.InvariantCulture);

        return index == 0 ? Whole(_hue) + "°" : Whole((index == 1 ? _saturation : _brightness) * 100);
    }

    /// <summary>Whole numbers, also typed with Persian or Arabic-Indic digits; a trailing ° or % is ignored.</summary>
    private static bool TryParseNumber(string text, out int value) =>
        int.TryParse(ToAsciiDigits(text).Trim().TrimEnd('°', '%').Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    private static string ToAsciiDigits(string text)
    {
        var chars = text.ToCharArray();

        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '۰' && chars[i] <= '۹') chars[i] = (char)('0' + (chars[i] - '۰'));
            else if (chars[i] >= '٠' && chars[i] <= '٩') chars[i] = (char)('0' + (chars[i] - '٠'));
        }

        return new string(chars);
    }

    private void Panel_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_picking)
        {
            // Esc leaves the eyedropper only; Enter takes the pixel under the cursor.
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                EndPick(commit: e.Key == Key.Enter);
                e.Handled = true;
            }

            return;
        }

        if (e.Key == Key.Escape)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && e.OriginalSource is not TextBox)
        {
            CommitRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    #endregion

    /// <summary>One swatch of the Grid or Brand tab.</summary>
    public sealed class SwatchItem : Notifier
    {
        public SwatchItem(Color color)
        {
            Color = color;
            Hex = ColorMath.ToHex(color);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            Brush = brush;
        }

        public Color Color { get; }

        public Brush Brush { get; }

        public string Hex { get; }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;
                RaisePropertyChanged();
            }
        }
    }
}
