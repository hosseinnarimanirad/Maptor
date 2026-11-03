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
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, e) => ((MaptorColorPicker)d).OnIsDropDownOpenChanged((bool)e.NewValue)));

    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    #endregion

    #region No colour

    /// <summary>
    /// Whether "no colour" is a valid choice (e.g. no fill, no outline). Shows a × on the closed control
    /// that sets <see cref="SelectedColor"/> to null, as the MahApps picker's clear button did.
    /// </summary>
    public static readonly DependencyProperty AllowsNoColorProperty = DependencyProperty.Register(
        nameof(AllowsNoColor), typeof(bool), typeof(MaptorColorPicker), new PropertyMetadata(false));

    public bool AllowsNoColor
    {
        get => (bool)GetValue(AllowsNoColorProperty);
        set => SetValue(AllowsNoColorProperty, value);
    }

    /// <summary>What the closed control says when there is no colour (default an em dash), e.g. "No fill".</summary>
    public static readonly DependencyProperty NoColorTextProperty = DependencyProperty.Register(
        nameof(NoColorText), typeof(string), typeof(MaptorColorPicker),
        new PropertyMetadata("—", (d, _) => ((MaptorColorPicker)d).UpdateDisplay()));

    public string NoColorText
    {
        get => (string)GetValue(NoColorTextProperty);
        set => SetValue(NoColorTextProperty, value);
    }

    /// <summary>Clears the colour (the closed control's ×). Only when <see cref="AllowsNoColor"/>.</summary>
    public void ClearColor()
    {
        if (AllowsNoColor)
            SelectedColor = null;
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
            SetValue(HexTextPropertyKey, string.IsNullOrEmpty(NoColorText) ? "—" : NoColorText);
            SetValue(OpacityTextPropertyKey, string.Empty);
        }
    }

    #endregion

    #region Pop-up

    private Popup? _popup;
    private MaptorColorPickerPanel? _panel;
    private ButtonBase? _clearButton;

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        ClearColor();
        e.Handled = true;
    }

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

        if (_clearButton is not null)
            _clearButton.Click -= ClearButton_Click;

        _popup = GetTemplateChild("PART_Popup") as Popup;
        _panel = GetTemplateChild("PART_Panel") as MaptorColorPickerPanel;
        _clearButton = GetTemplateChild("PART_ClearButton") as ButtonBase;

        if (_clearButton is not null)
            _clearButton.Click += ClearButton_Click;

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

    // Set by Esc, so closing after a cancel does not add the restored colour to the recent ones.
    private bool _cancelled;

    /// <summary>
    /// Open/close housekeeping. Hung on the property, not on Popup.Opened/Closed: with the fade
    /// animation the popup raises Closed only after the fade, and a close from outside (click away,
    /// Esc, code) always goes through this property first.
    /// </summary>
    private void OnIsDropDownOpenChanged(bool isOpen)
    {
        if (isOpen)
        {
            _colorAtOpen = SelectedColor;

            // No colour yet: start from opaque black, which the user then changes.
            _panel?.SetColor(SelectedColor ?? Colors.Black);
        }
        else
        {
            _closedAtTicks = Environment.TickCount64;

            if (!_cancelled && SelectedColor is { } color && color != _colorAtOpen)
                MaptorColorPickerPanel.RememberRecent(color);

            _cancelled = false;
        }
    }

    private void Popup_Opened(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(() => _panel?.FocusSelectedTab(), System.Windows.Threading.DispatcherPriority.Input);

    private void Popup_Closed(object? sender, EventArgs e) => _closedAtTicks = Environment.TickCount64;

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

    // True between a press on the closed control and its release.
    private bool _pressed;

    /// <summary>
    /// The pop-up opens on release, like a button click. Opening on press made it close again at once:
    /// a StaysOpen=False pop-up takes the mouse when it opens, and the release of that same click,
    /// landing on this control outside the pop-up, counted as a click outside it.
    /// </summary>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (e.Handled || !IsEnabled)
            return;

        Focus();

        if (IsDropDownOpen)
        {
            IsDropDownOpen = false;
        }
        else
        {
            // The click that just closed the pop-up (by landing here) must not reopen it on release.
            _pressed = Environment.TickCount64 - _closedAtTicks >= 250;
        }

        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (!_pressed)
            return;

        _pressed = false;

        if (IsEnabled && IsMouseOver && !IsDropDownOpen)
            IsDropDownOpen = true;

        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _pressed = false;   // pressed here, released elsewhere: no click
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
        else if ((key == Key.Delete || key == Key.Back) && AllowsNoColor && !IsDropDownOpen)
        {
            ClearColor();
            e.Handled = true;
        }
    }

    #endregion
}
