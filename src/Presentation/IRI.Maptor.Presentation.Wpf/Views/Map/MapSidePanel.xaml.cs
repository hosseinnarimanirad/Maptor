using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// A tool panel docked beside the map, replacing the MahApps flyouts that floated over it: the
/// map's extent ends where the panel starts, so zoom-to-extent and pan-to-centre frame the part of
/// the map the user can see.
/// <para>
/// Put it in a grid column after the map's column with <c>Width="Auto"</c> and let it span the
/// rows it should fill. It collapses while <see cref="IsOpen"/> is false, so the column takes no
/// room. Several panels can share the column; the host keeps at most one open.
/// </para>
/// <para>
/// The width starts at <see cref="PreferredWidth"/>; the user resizes it with the edge that faces
/// the map, and the panel keeps that width while the application runs (closing and reopening
/// does not reset it). It is kept between <see cref="FrameworkElement.MinWidth"/> and
/// <see cref="MaxWidthRatio"/> of the host's width, so the map keeps a usable share.
/// </para>
/// </summary>
[ContentProperty(nameof(PanelContent))]
public partial class MapSidePanel : NotifiableUserControl
{
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(MapSidePanel),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((MapSidePanel)d).UpdateVisibility()));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(MapSidePanel), new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty PanelContentProperty = DependencyProperty.Register(
        nameof(PanelContent), typeof(object), typeof(MapSidePanel), new PropertyMetadata(null));

    /// <summary>The hosted view (what used to be the flyout's content).</summary>
    public object PanelContent
    {
        get => GetValue(PanelContentProperty);
        set => SetValue(PanelContentProperty, value);
    }

    public static readonly DependencyProperty PreferredWidthProperty = DependencyProperty.Register(
        nameof(PreferredWidth), typeof(double), typeof(MapSidePanel), new PropertyMetadata(400.0, (d, e) => ((MapSidePanel)d).ApplyWidth()));

    /// <summary>The width the panel opens at until the user resizes it.</summary>
    public double PreferredWidth
    {
        get => (double)GetValue(PreferredWidthProperty);
        set => SetValue(PreferredWidthProperty, value);
    }

    public static readonly DependencyProperty MaxWidthRatioProperty = DependencyProperty.Register(
        nameof(MaxWidthRatio), typeof(double), typeof(MapSidePanel), new PropertyMetadata(0.5, (d, e) => ((MapSidePanel)d).ApplyWidth()));

    /// <summary>
    /// The largest share of the host's width the panel may take (0 to 1). The host usually holds a
    /// legend as well, so the map gets less than the rest: with a 400 px legend on a 1366 px window,
    /// the default 0.5 leaves the map about 280 px.
    /// </summary>
    public double MaxWidthRatio
    {
        get => (double)GetValue(MaxWidthRatioProperty);
        set => SetValue(MaxWidthRatioProperty, value);
    }

    // the width the user chose by dragging; NaN until then, which means PreferredWidth
    private double _userWidth = double.NaN;

    private FrameworkElement? _host;

    public MapSidePanel()
    {
        InitializeComponent();

        this.MinWidth = 280;

        this.HorizontalAlignment = HorizontalAlignment.Stretch;

        UpdateVisibility();

        this.Loaded += (s, e) => AttachHost();
        this.Unloaded += (s, e) => DetachHost();
    }

    private void AttachHost()
    {
        DetachHost();

        _host = VisualTreeHelper.GetParent(this) as FrameworkElement;

        if (_host != null)
            _host.SizeChanged += Host_SizeChanged;

        ApplyWidth();
    }

    private void DetachHost()
    {
        if (_host != null)
            _host.SizeChanged -= Host_SizeChanged;

        _host = null;
    }

    // a smaller window must not leave the map squeezed behind a panel that kept its old width
    private void Host_SizeChanged(object sender, SizeChangedEventArgs e) => ApplyWidth();

    private void UpdateVisibility()
    {
        this.Visibility = IsOpen ? Visibility.Visible : Visibility.Collapsed;

        if (IsOpen)
            ApplyWidth();
    }

    private double GetMaxWidth()
    {
        var hostWidth = _host?.ActualWidth ?? 0;

        if (hostWidth <= 0 || MaxWidthRatio <= 0)
            return double.PositiveInfinity;

        // MinWidth wins over the ratio: a panel narrower than its minimum is unusable
        return Math.Max(this.MinWidth, hostWidth * Math.Min(MaxWidthRatio, 1.0));
    }

    private double Clamp(double width) => Math.Max(this.MinWidth, Math.Min(width, GetMaxWidth()));

    private void ApplyWidth()
    {
        var wanted = double.IsNaN(_userWidth) ? PreferredWidth : _userWidth;

        if (double.IsNaN(wanted) || wanted <= 0)
            return;

        this.Width = Clamp(wanted);
    }

    // HorizontalChange is in the panel's own (mirrored in RTL) coordinates, where the grip is on
    // the leading edge in both layouts: dragging it towards the map (negative) widens the panel.
    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var current = double.IsNaN(this.Width) ? this.ActualWidth : this.Width;

        _userWidth = Clamp(current - e.HorizontalChange);

        this.Width = _userWidth;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => this.IsOpen = false;
}
