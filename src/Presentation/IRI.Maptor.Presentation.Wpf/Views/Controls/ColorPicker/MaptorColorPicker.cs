using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// Maptor's compact colour picker: a closed control showing the swatch, hex and opacity, which opens
/// a pop-up with Grid / Spectrum / Sliders / Brand tabs.
/// <para>
/// Drop-in for <c>mahApp:ColorPicker</c> where only <see cref="SelectedColor"/> is used: the same
/// property name and type (<c>Color?</c>, two-way by default). Null means "no colour" and shows a
/// struck-through swatch.
/// </para>
/// The look lives in <c>Assets/Styles/Controls.ColorPicker.xaml</c> (implicit style, merged by
/// <c>Controls.All.xaml</c>); there is no <c>Themes/Generic.xaml</c> in this assembly.
/// </summary>
public class MaptorColorPicker : Control
{
    static MaptorColorPicker()
    {
        FocusableProperty.OverrideMetadata(typeof(MaptorColorPicker), new FrameworkPropertyMetadata(true));
    }

    #region SelectedColor

    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
        nameof(SelectedColor), typeof(Color?), typeof(MaptorColorPicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, _) => ((MaptorColorPicker)d).OnSelectedColorChanged()));

    private void OnSelectedColorChanged()
    {
        UpdateDisplay();

        // A change from outside while the pop-up is open (not the pop-up's own echo) is shown in it.
        if (!_settingFromPanel && IsDropDownOpen && _panel is not null && SelectedColor is { } c)
            _panel.SetColor(c);
    }

    public Color? SelectedColor
    {
        get => (Color?)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    #endregion

    #region IsDropDownOpen

    public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
        nameof(IsDropDownOpen), typeof(bool), typeof(MaptorColorPicker),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    #endregion

    #region IsCompact

    /// <summary>
    /// Closed control for narrow cells (set by the Small style): only the swatch and the hex, smaller
    /// text, no opacity percentage and no chevron, so <c>#RRGGBB</c> still fits in a 90 px column.
    /// </summary>
    public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(
        nameof(IsCompact), typeof(bool), typeof(MaptorColorPicker), new PropertyMetadata(false));

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    #endregion

    #region Display (read-only, for the template)

    private static readonly DependencyPropertyKey SelectedBrushPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(SelectedBrush), typeof(Brush), typeof(MaptorColorPicker), new PropertyMetadata(Brushes.Transparent));

    public static readonly DependencyProperty SelectedBrushProperty = SelectedBrushPropertyKey.DependencyProperty;

    /// <summary>A frozen brush of <see cref="SelectedColor"/>; transparent when there is no colour.</summary>
    public Brush SelectedBrush => (Brush)GetValue(SelectedBrushProperty);


    private static readonly DependencyPropertyKey HexTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HexText), typeof(string), typeof(MaptorColorPicker), new PropertyMetadata("—"));

    public static readonly DependencyProperty HexTextProperty = HexTextPropertyKey.DependencyProperty;

    /// <summary><c>#RRGGBB</c>, or an em dash when there is no colour.</summary>
    public string HexText => (string)GetValue(HexTextProperty);


    private static readonly DependencyPropertyKey OpacityTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(OpacityText), typeof(string), typeof(MaptorColorPicker), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty OpacityTextProperty = OpacityTextPropertyKey.DependencyProperty;

    /// <summary>The opacity as <c>NN%</c>; empty when there is no colour.</summary>
    public string OpacityText => (string)GetValue(OpacityTextProperty);


    private static readonly DependencyPropertyKey HasColorPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasColor), typeof(bool), typeof(MaptorColorPicker), new PropertyMetadata(false));

    public static readonly DependencyProperty HasColorProperty = HasColorPropertyKey.DependencyProperty;

    public bool HasColor => (bool)GetValue(HasColorProperty);

    private void UpdateDisplay()
    {
        var color = SelectedColor;

        SetValue(HasColorPropertyKey, color.HasValue);

        if (color is { } c)
        {
            var brush = new SolidColorBrush(c);
            brush.Freeze();

            SetValue(SelectedBrushPropertyKey, brush);
            SetValue(HexTextPropertyKey, ColorMath.ToHex(c));
            SetValue(OpacityTextPropertyKey, $"{ColorMath.AlphaToPercent(c.A)}%");
        }
        else
        {
            SetValue(SelectedBrushPropertyKey, Brushes.Transparent);
            SetValue(HexTextPropertyKey, "—");
            SetValue(OpacityTextPropertyKey, string.Empty);
        }
    }

    #endregion

    #region Pop-up

    private Popup? _popup;
    private MaptorColorPickerPanel? _panel;

    // The colour the pop-up opened with, restored by Esc.
    private Color? _colorAtOpen;

    private bool _settingFromPanel;

    // When the pop-up last closed: the click that closes it by landing on this control must not reopen it.
    private long _closedAtTicks;

    public override void OnApplyTemplate()
    {
        if (_popup is not null)
        {
            _popup.Opened -= Popup_Opened;
            _popup.Closed -= Popup_Closed;
        }

        if (_panel is not null)
        {
            _panel.ColorChanged -= Panel_ColorChanged;
            _panel.CommitRequested -= Panel_CommitRequested;
            _panel.CancelRequested -= Panel_CancelRequested;
        }

        base.OnApplyTemplate();

        _popup = GetTemplateChild("PART_Popup") as Popup;
        _panel = GetTemplateChild("PART_Panel") as MaptorColorPickerPanel;

        if (_popup is not null)
        {
            _popup.Opened += Popup_Opened;
            _popup.Closed += Popup_Closed;
        }

        if (_panel is not null)
        {
            _panel.ColorChanged += Panel_ColorChanged;
            _panel.CommitRequested += Panel_CommitRequested;
            _panel.CancelRequested += Panel_CancelRequested;
        }
    }

    private void Popup_Opened(object? sender, EventArgs e)
    {
        _colorAtOpen = SelectedColor;

        // No colour yet: start from opaque black, which the user then changes.
        _panel?.SetColor(SelectedColor ?? Colors.Black);

        Dispatcher.BeginInvoke(() => _panel?.FocusSelectedTab(), System.Windows.Threading.DispatcherPriority.Input);
    }

    // Set by Esc, so closing after a cancel does not add the restored colour to the recent ones.
    private bool _cancelled;

    private void Popup_Closed(object? sender, EventArgs e)
    {
        _closedAtTicks = Environment.TickCount64;

        if (!_cancelled && SelectedColor is { } color && color != _colorAtOpen)
            MaptorColorPickerPanel.RememberRecent(color);

        _cancelled = false;
    }

    private void Panel_ColorChanged(object? sender, Color color)
    {
        _settingFromPanel = true;

        try
        {
            SelectedColor = color;
        }
        finally
        {
            _settingFromPanel = false;
        }
    }

    private void Panel_CommitRequested(object? sender, EventArgs e)
    {
        IsDropDownOpen = false;
        Focus();
    }

    private void Panel_CancelRequested(object? sender, EventArgs e)
    {
        _cancelled = true;
        SelectedColor = _colorAtOpen;
        IsDropDownOpen = false;
        Focus();
    }

    #endregion

    #region Opening (mouse and keyboard)

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (e.Handled || !IsEnabled)
            return;

        Focus();

        if (!IsDropDownOpen && Environment.TickCount64 - _closedAtTicks < 250)
        {
            e.Handled = true;
            return;
        }

        IsDropDownOpen = !IsDropDownOpen;
        e.Handled = true;
    }

    /// <summary>The usual drop-down keys: F4, Alt+Down/Up, Enter and Space toggle; Esc closes.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled || !IsEnabled)
            return;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;

        if (key == Key.F4 || (alt && (key == Key.Down || key == Key.Up)) || key == Key.Enter || key == Key.Space)
        {
            IsDropDownOpen = !IsDropDownOpen;
            e.Handled = true;
        }
        else if (key == Key.Escape && IsDropDownOpen)
        {
            IsDropDownOpen = false;
            e.Handled = true;
        }
    }

    #endregion
}
