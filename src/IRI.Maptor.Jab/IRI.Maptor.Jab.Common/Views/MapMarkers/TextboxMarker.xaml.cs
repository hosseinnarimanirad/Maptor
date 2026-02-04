using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Assets.Fonts;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Jab.Common.Views;

namespace IRI.Maptor.Jab.Common.Views.MapMarkers;

public partial class TextboxMarker : MapMarker
{
    private readonly DispatcherTimer _popupCloseTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };

    
     
    public TextboxMarker()
    {
        InitializeComponent();
        _popupCloseTimer.Tick += (_, _) =>
        {
            _popupCloseTimer.Stop();
            formatPopup.IsOpen = false;
        };
        Loaded += (_, _) =>
        {
            //UpdateAlignmentButtons();
            //UpdateRTLButtons();
            formatPopup.Opened += OnPopupOpened;
            if (popupBorder != null)
            {
                popupBorder.SizeChanged += OnPopupBorderSizeChanged;
            }
            // Default background to Theme highlight if not set
            //if (mainBorder != null && SelectedBackgroundChoice == null)
            //{
            //    SelectedBackgroundChoice = BackgroundChoices[0];
            //}
            // Initialize font family (use IranSans from resources when selected)
            //if (labelBox != null && !string.IsNullOrEmpty(FontFamily))
            //{
            //    ApplyFontFamily(labelBox, FontFamily);
            //}
        };

        void OnPopupBorderSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Border border)
            {
                var clip = new System.Windows.Media.RectangleGeometry
                {
                    Rect = new Rect(0, 0, border.ActualWidth, border.ActualHeight),
                    RadiusX = 5,
                    RadiusY = 5
                };
                border.Clip = clip;
            }
        }
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        // Center the popup horizontally above the TextBox (MS Office style)
        if (formatPopup.PlacementTarget is FrameworkElement target && formatPopup.Child is FrameworkElement popupChild)
        {
            // Wait for the popup to render to get accurate size
            popupChild.UpdateLayout();
            formatPopup.HorizontalOffset = (target.ActualWidth - popupChild.ActualWidth) / 2;
        }
    }

    private void OnMarkerMouseEnter(object sender, MouseEventArgs e)
    {
        _popupCloseTimer.Stop();
        formatPopup.IsOpen = true;
    }

    private void OnMarkerMouseLeave(object sender, MouseEventArgs e)
    {
        _popupCloseTimer.Stop();
        _popupCloseTimer.Start();
    }

    private void OnPopupContentMouseEnter(object sender, MouseEventArgs e)
    {
        _popupCloseTimer.Stop();
    }

    private void OnPopupContentMouseLeave(object sender, MouseEventArgs e)
    {
        _popupCloseTimer.Stop();
        _popupCloseTimer.Start();
    }
     
    //public string LabelValue
    //{
    //    get { return (string)GetValue(LabelValueProperty); }
    //    set { SetValue(LabelValueProperty, value); }
    //}

    //public static readonly DependencyProperty LabelValueProperty =
    //    DependencyProperty.Register(nameof(LabelValue), typeof(string), typeof(TextboxMarker), new PropertyMetadata(string.Empty));


    //public string TooltipValue
    //{
    //    get { return (string)GetValue(TooltipValueProperty); }
    //    set { SetValue(TooltipValueProperty, value); }
    //}

    //public static readonly DependencyProperty TooltipValueProperty =
    //    DependencyProperty.Register("TooltipValue", typeof(string), typeof(TextboxMarker), new PropertyMetadata(string.Empty));



    //public bool IsRTL
    //{
    //    get { return (bool)GetValue(IsRTLProperty); }
    //    set { SetValue(IsRTLProperty, value); }
    //}

    //// Using a DependencyProperty as the backing store for IsRTL.  This enables animation, styling, binding, etc...
    //public static readonly DependencyProperty IsRTLProperty =
    //    DependencyProperty.Register("IsRTL", typeof(bool), typeof(TextboxMarker), new PropertyMetadata(true, new PropertyChangedCallback((d, dp) =>
    //    {
    //        var marker = (TextboxMarker)d;
    //        marker.TextFlowDirection = ((bool)dp.NewValue) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    //        if (marker.IsLoaded)
    //        {
    //            marker.UpdateRTLButtons();
    //        }
    //    })));

    //private FlowDirection _textFlowDirection;
    //public FlowDirection TextFlowDirection
    //{
    //    get { return _textFlowDirection; }
    //    set
    //    {
    //        _textFlowDirection = value;
    //        RaisePropertyChanged();
    //    }
    //}

     
    //public bool IsBold
    //{
    //    get { return (bool)GetValue(IsBoldProperty); }
    //    set { SetValue(IsBoldProperty, value); }
    //}
    //public static readonly DependencyProperty IsBoldProperty =
    //    DependencyProperty.Register(nameof(IsBold), typeof(bool), typeof(TextboxMarker), new PropertyMetadata(false));

    //public bool IsItalic
    //{
    //    get { return (bool)GetValue(IsItalicProperty); }
    //    set { SetValue(IsItalicProperty, value); }
    //}
    //public static readonly DependencyProperty IsItalicProperty =
    //    DependencyProperty.Register(nameof(IsItalic), typeof(bool), typeof(TextboxMarker), new PropertyMetadata(false));

    //public double FontSize
    //{
    //    get { return (double)GetValue(FontSizeProperty); }
    //    set { SetValue(FontSizeProperty, value); }
    //}
    //public static readonly DependencyProperty FontSizeProperty =
    //    DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(TextboxMarker), new PropertyMetadata(11.0));

    //public TextAlignment TextAlignment
    //{
    //    get { return (TextAlignment)GetValue(TextAlignmentProperty); }
    //    set { SetValue(TextAlignmentProperty, value); }
    //}
    //public static readonly DependencyProperty TextAlignmentProperty =
    //    DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(TextboxMarker),
    //        new PropertyMetadata(System.Windows.TextAlignment.Center, OnTextAlignmentChanged));

    //private static void OnTextAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //{
    //    if (d is TextboxMarker marker && marker.IsLoaded)
    //    {
    //        marker.UpdateAlignmentButtons();
    //    }
    //}

    //public bool IsUnderline
    //{
    //    get { return (bool)GetValue(IsUnderlineProperty); }
    //    set { SetValue(IsUnderlineProperty, value); }
    //}
    //public static readonly DependencyProperty IsUnderlineProperty =
    //    DependencyProperty.Register(nameof(IsUnderline), typeof(bool), typeof(TextboxMarker), new PropertyMetadata(false));

    //public string FontFamily
    //{
    //    get { return (string)GetValue(FontFamilyProperty); }
    //    set { SetValue(FontFamilyProperty, value); }
    //}
    //public static readonly DependencyProperty FontFamilyProperty =
    //    DependencyProperty.Register(nameof(FontFamily), typeof(string), typeof(TextboxMarker), 
    //        new PropertyMetadata(IranSansFontName, OnFontFamilyChanged));

    //private static void OnFontFamilyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //{
    //    if (d is TextboxMarker marker && marker.labelBox != null)
    //    {
    //        ApplyFontFamily(marker.labelBox, (string)e.NewValue ?? IranSansFontName);
    //    }
    //}

    //private static void ApplyFontFamily(TextBox textBox, string fontFamilyName)
    //{
    //    if (string.Equals(fontFamilyName, IranSansFontName, StringComparison.OrdinalIgnoreCase))
    //    {
    //        textBox.FontFamily = IriFonts.IranSans;
    //        return;
    //    }
    //    try
    //    {
    //        textBox.FontFamily = new FontFamily(fontFamilyName);
    //    }
    //    catch
    //    {
    //        textBox.FontFamily = IriFonts.IranSans;
    //    }
    //}

    //public sealed record BackgroundChoice(string Name, System.Windows.Media.Color? ColorValue, bool IsTheme);

    //public static readonly BackgroundChoice[] BackgroundChoices = BuildBackgroundChoices();

    //private static BackgroundChoice[] BuildBackgroundChoices()
    //{
    //    var colors = new[]
    //    {
    //        ColorHelper.ToWpfColor("#FF000000"),
    //        ColorHelper.ToWpfColor("#FF61A917"),
    //        ColorHelper.ToWpfColor("#FFA4C401"),
    //        ColorHelper.ToWpfColor("#FF008A00"),
    //        ColorHelper.ToWpfColor("#FF00ACAA"),
    //        ColorHelper.ToWpfColor("#FF1CA1E2"),
    //        ColorHelper.ToWpfColor("#FF0050EF"),
    //        ColorHelper.ToWpfColor("#FF6900FF"),
    //        ColorHelper.ToWpfColor("#FFAA00FF"),
    //        ColorHelper.ToWpfColor("#FFF572D0"),
    //        ColorHelper.ToWpfColor("#FFD80072"),
    //        ColorHelper.ToWpfColor("#FFA10024"),
    //        ColorHelper.ToWpfColor("#FFE51400"),
    //        ColorHelper.ToWpfColor("#FFFA6900"),
    //        ColorHelper.ToWpfColor("#FFF1A30B"),
    //        ColorHelper.ToWpfColor("#FFE4C802"),
    //        ColorHelper.ToWpfColor("#FF835A2C"),
    //        ColorHelper.ToWpfColor("#FF6D8764"),
    //        ColorHelper.ToWpfColor("#FF637685"),
    //        ColorHelper.ToWpfColor("#FF756089"),
    //        ColorHelper.ToWpfColor("#FF88794E"),
    //    };
    //    var list = new List<BackgroundChoice> { new("Theme highlight", null, true) };
    //    foreach (var c in colors)
    //        list.Add(new(c.ToString(), c, false));
    //    return list.ToArray();
    //}

    //public BackgroundChoice? SelectedBackgroundChoice
    //{
    //    get { return (BackgroundChoice?)GetValue(SelectedBackgroundChoiceProperty); }
    //    set { SetValue(SelectedBackgroundChoiceProperty, value); }
    //}
    //public static readonly DependencyProperty SelectedBackgroundChoiceProperty =
    //    DependencyProperty.Register(nameof(SelectedBackgroundChoice), typeof(BackgroundChoice), typeof(TextboxMarker),
    //        new PropertyMetadata(null, OnSelectedBackgroundChoiceChanged));

    //private static void OnSelectedBackgroundChoiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //{
    //    if (d is TextboxMarker marker && marker.mainBorder != null && e.NewValue is BackgroundChoice choice)
    //    {
    //        if (choice.IsTheme || choice.ColorValue == null)
    //            marker.mainBorder.SetResourceReference(Border.BackgroundProperty, "MahApps.Brushes.Highlight");
    //        else
    //            marker.mainBorder.Background = new SolidColorBrush(choice.ColorValue.Value);
    //    }
    //}

    //private void OnIncreaseFontSize(object sender, RoutedEventArgs e)
    //{
    //    IncreaseFontSize();
    //}

    //private void OnDecreaseFontSize(object sender, RoutedEventArgs e)
    //{
    //    DecreaseFontSize();
    //}

    //private void OnAlignLeftClick(object sender, RoutedEventArgs e)
    //{
    //    TextAlignment = System.Windows.TextAlignment.Left;
    //    UpdateAlignmentButtons();
    //}

    //private void OnAlignCenterClick(object sender, RoutedEventArgs e)
    //{
    //    TextAlignment = System.Windows.TextAlignment.Center;
    //    UpdateAlignmentButtons();
    //}

    //private void OnAlignRightClick(object sender, RoutedEventArgs e)
    //{
    //    TextAlignment = System.Windows.TextAlignment.Right;
    //    UpdateAlignmentButtons();
    //}

    //private void UpdateAlignmentButtons()
    //{
    //    alignLeftButton.IsChecked = TextAlignment == System.Windows.TextAlignment.Left;
    //    alignCenterButton.IsChecked = TextAlignment == System.Windows.TextAlignment.Center;
    //    alignRightButton.IsChecked = TextAlignment == System.Windows.TextAlignment.Right;
    //}

    //private void OnLTRClick(object sender, RoutedEventArgs e)
    //{
    //    IsRTL = false;
    //}

    //private void OnRTLClick(object sender, RoutedEventArgs e)
    //{
    //    IsRTL = true;
    //}

    //private void UpdateRTLButtons()
    //{
    //    if (rtlButton != null && ltrButton != null)
    //    {
    //        rtlButton.IsChecked = IsRTL;
    //        ltrButton.IsChecked = !IsRTL;
    //    }
    //}
}
