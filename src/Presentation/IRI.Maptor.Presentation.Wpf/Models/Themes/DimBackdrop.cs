using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Models.Themes;

/// <summary>
/// How the owner window is marked as inactive while a dialog is open.
/// </summary>
public enum DialogBackdrop
{
    /// <summary>
    /// A translucent shade over the owner's content. One flat fill: costs nothing to draw, so the
    /// map behind the dialog keeps rendering at full speed. The default.
    /// </summary>
    Dim,

    /// <summary>
    /// A <see cref="System.Windows.Media.Effects.BlurEffect"/> on the whole owner window. Every
    /// repaint of the owner, map included, is drawn off-screen and blurred, and without a capable
    /// GPU (remote desktop, virtual machines) the blur runs on the CPU and delays the dialog's
    /// first paint. Kept for hosts that prefer the look.
    /// </summary>
    Blur,

    /// <summary>
    /// Leave the owner untouched.
    /// </summary>
    None,
}

/// <summary>
/// Shades the owner window's content while a dialog is open, and removes the shade when disposed.
/// </summary>
/// <remarks>
/// <para>
/// The shade lives in the owner's adorner layer, so it needs no change to the owner's visual tree
/// and cannot leak into its layout. It covers the window's content, not the title bar, which is
/// drawn by the window template outside the adorner layer.
/// </para>
/// <para>
/// Shades are counted per owner: a dialog opened through the dialog service is also seen by the
/// modal-window hook below, and nested dialogs share an owner, so the owner keeps exactly one
/// shade until the last handle is disposed.
/// </para>
/// <para>
/// <see cref="ShadeModalWindows"/> extends the shade to every WPF window opened with
/// <see cref="Window.ShowDialog"/> over an owner, including the settings and management windows
/// that hosts open directly rather than through the dialog service. The dialog service switches it
/// on while its <c>Backdrop</c> is <see cref="DialogBackdrop.Dim"/>.
/// </para>
/// </remarks>
internal sealed class DimBackdrop : IDisposable
{
    private sealed class OwnerShade
    {
        public required AdornerLayer Layer { get; init; }

        public required ShadeAdorner Shade { get; init; }

        public int Count { get; set; }
    }

    private static readonly ConditionalWeakTable<UIElement, OwnerShade> Shades = new();

    // Window keeps "am I modal" in a private field; ComponentDispatcher.IsThreadModal is the
    // fallback, true while any ShowDialog is running on this thread
    private static readonly FieldInfo? ShowingAsDialogField =
        typeof(Window).GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic);

    private static bool _hookRegistered;
    private static bool _shadeModalWindows;

    private readonly UIElement _content;
    private bool _disposed;

    private DimBackdrop(UIElement content)
    {
        _content = content;
    }

    /// <summary>
    /// Whether every modal window opened over an owner shades that owner. Off until the dialog
    /// service turns it on.
    /// </summary>
    public static bool ShadeModalWindows
    {
        get => _shadeModalWindows;
        set
        {
            _shadeModalWindows = value;

            if (value && !_hookRegistered)
            {
                EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
                _hookRegistered = true;
            }
        }
    }

    /// <summary>
    /// Shades <paramref name="owner"/>'s content, or returns null when it has nothing to shade
    /// (no content yet, or a window template without an adorner layer).
    /// </summary>
    public static DimBackdrop? TryApply(Window owner)
    {
        if (owner.Content is not UIElement content)
            return null;

        if (!Shades.TryGetValue(content, out var entry))
        {
            var layer = AdornerLayer.GetAdornerLayer(content);

            if (layer == null)
                return null;

            entry = new OwnerShade { Layer = layer, Shade = new ShadeAdorner(content) };
            layer.Add(entry.Shade);
            Shades.Add(content, entry);
        }

        entry.Count++;

        return new DimBackdrop(content);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (!Shades.TryGetValue(_content, out var entry))
            return;

        entry.Count--;

        if (entry.Count > 0)
            return;

        entry.Layer.Remove(entry.Shade);
        Shades.Remove(_content);
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (!ShadeModalWindows || sender is not Window window || window.Owner is not Window owner)
            return;

        var isModal = ShowingAsDialogField?.GetValue(window) as bool? ?? ComponentDispatcher.IsThreadModal;

        if (!isModal)
            return;

        var shade = TryApply(owner);

        if (shade != null)
            window.Closed += (_, _) => shade.Dispose();
    }

    private sealed class ShadeAdorner : Adorner
    {
        // black at 35%: dark enough to read as "inactive" over a bright basemap, light enough that
        // the user still sees which map the dialog belongs to. Black rather than a theme brush so
        // it darkens in both themes; a white veil would brighten a dark theme instead.
        private static readonly Brush Shade = CreateShade();

        public ShadeAdorner(UIElement adorned) : base(adorned)
        {
            // purely visual: the dialog is modal, and the shade must never swallow input itself
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Shade, null, new Rect(RenderSize));
        }

        private static Brush CreateShade()
        {
            var brush = new SolidColorBrush(Color.FromArgb(0x59, 0x00, 0x00, 0x00));

            brush.Freeze();

            return brush;
        }
    }
}
