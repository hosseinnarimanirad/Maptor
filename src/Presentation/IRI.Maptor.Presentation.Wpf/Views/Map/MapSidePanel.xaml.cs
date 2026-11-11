using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;

using MahApps.Metro.IconPacks;

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
/// does not reset it; double-clicking the edge does). It is kept between <see cref="FrameworkElement.MinWidth"/> and
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

    public static readonly DependencyProperty IconKindProperty = DependencyProperty.Register(
        nameof(IconKind), typeof(PackIconMaterialKind), typeof(MapSidePanel),
        new PropertyMetadata(PackIconMaterialKind.None, (d, e) => ((MapSidePanel)d).UpdateIcon()));

    /// <summary>
    /// The icon before the title; none while <see cref="PackIconMaterialKind.None"/>. A Material icon, so it can be
    /// the same as on the ribbon command that opens the panel.
    /// </summary>
    public PackIconMaterialKind IconKind
    {
        get => (PackIconMaterialKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public static readonly DependencyProperty PanelContentProperty = DependencyProperty.Register(
        nameof(PanelContent), typeof(object), typeof(MapSidePanel), new PropertyMetadata(null, OnPanelContentChanged));

    /// <summary>
    /// The hosted view (what used to be the flyout's content). It is this panel's logical child (2026-11-11), so it
    /// inherits the panel's DataContext from the moment it is set, as the content of a ContentControl does. Before,
    /// it had no logical parent and got the DataContext only through the visual tree once the panel was first
    /// laid out; the map extent panel in Saba showed no data (NaN) while every code-built test worked.
    /// </summary>
    public object PanelContent
    {
        get => GetValue(PanelContentProperty);
        set => SetValue(PanelContentProperty, value);
    }

    private static void OnPanelContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var panel = (MapSidePanel)d;

        if (e.OldValue is not null)
            panel.RemoveLogicalChild(e.OldValue);

        if (e.NewValue is not null)
            panel.AddLogicalChild(e.NewValue);
    }

    protected override IEnumerator LogicalChildren
    {
        get
        {
            var children = new List<object>();

            var baseChildren = base.LogicalChildren;

            if (baseChildren is not null)
                while (baseChildren.MoveNext())
                    children.Add(baseChildren.Current);

            if (PanelContent is not null)
                children.Add(PanelContent);

            return children.GetEnumerator();
        }
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

        UpdateIcon();

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

    // double-click on the edge: forget the dragged width, back to PreferredWidth
    private void ResizeThumb_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _userWidth = double.NaN;

        ApplyWidth();

        e.Handled = true;
    }

    private void UpdateIcon()
    {
        // called from the constructor before InitializeComponent has run only if a style sets IconKind
        if (headerIcon is null)
            return;

        if (IconKind != PackIconMaterialKind.None)
        {
            headerIcon.Kind = IconKind;
            headerIcon.Visibility = Visibility.Visible;
        }
        else
        {
            headerIcon.Visibility = Visibility.Collapsed;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => this.IsOpen = false;
}
