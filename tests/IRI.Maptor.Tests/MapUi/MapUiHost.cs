using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Threading;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Presentation.Core.Data;
using IRI.Maptor.Presentation.Wpf.Controls;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Tests.Common;

using WpfPoint = System.Windows.Point;
using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Tests.MapUi;

/// <summary>
/// A real <see cref="MapViewer"/> in a real window, driven with real mouse input, for checks of what the mouse does
/// on the map (drawing, editing, panning). The map's handlers read the pointer from Windows, so events raised in
/// the process are not enough: the pointer is really moved and the buttons really pressed.
/// <para>
/// That takes the mouse over while a check runs, so these checks are not part of a normal test run: they run only
/// when the environment variable <c>MAPTOR_UI_TEST</c> is 1 (see <see cref="IsEnabled"/>). The window is small,
/// topmost, on the monitor of the window that was in front when the run started, and has no base map and no data
/// but what a check adds itself.
/// </para>
/// </summary>
internal sealed class MapUiHost
{
    public const string Switch = "MAPTOR_UI_TEST";

    /// <summary>True when the UI checks were asked for. A check that is not returns at once.</summary>
    public static bool IsEnabled => Environment.GetEnvironmentVariable(Switch) == "1";

    private sealed class TestMap : MapViewModelBase
    {
    }

    private Window _window = null!;

    public MapViewer Map { get; private set; } = null!;

    private System.Windows.Controls.StackPanel _strip = null!;

    /// <summary>Puts <paramref name="element"/> in a strip above the map, as an application's toolbar.</summary>
    public void AddBesideMap(UIElement element)
    {
        _strip.Children.Add(element);

        Pump(100);
    }

    public MapViewModelBase ViewModel => _presenter;

    private TestMap _presenter = null!;

    /// <summary>
    /// Opens the map, runs <paramref name="check"/> on its thread and closes it. The pointer is put back where it
    /// was and the left button is released, whatever happens.
    /// </summary>
    public static void Run(Action<MapUiHost> check, Action<MapSettings>? settings = null)
    {
        WpfTestHost.Run(() =>
        {
            // awaits in the map come back to this thread only with a dispatcher context
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));

            ClearSharedStyles();

            GetCursorPos(out var pointerBefore);

            var front = GetForegroundWindow();

            var host = new MapUiHost();

            try
            {
                host.Open(front, settings);

                check(host);
            }
            finally
            {
                mouse_event(LeftUp, 0, 0, 0, UIntPtr.Zero);

                SetCursorPos(pointerBefore.X, pointerBefore.Y);

                host._window?.Close();

                // and whatever the check opened besides (a notification toast, a dialog): left open, it would cover
                // the map of the next check where its clicks land
                // (the windows of this thread: Application.Windows belongs to the thread of the first check)
                foreach (var source in PresentationSource.CurrentSources.OfType<PresentationSource>().ToList())
                {
                    if (source.CheckAccess() && source.RootVisual is Window window)
                        window.Close();
                }

                host.Pump(50);

                ClearSharedStyles();

                if (front != IntPtr.Zero)
                    SetForegroundWindow(front);
            }
        });
    }

    private void Open(IntPtr front, Action<MapSettings>? settings)
    {
        EnsureResources();

        Map = new MapViewer();

        // a strip above the map for what a check puts beside it (a button that keeps the keyboard focus)
        _strip = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };

        var layout = new System.Windows.Controls.DockPanel();

        System.Windows.Controls.DockPanel.SetDock(_strip, System.Windows.Controls.Dock.Top);

        layout.Children.Add(_strip);
        layout.Children.Add(Map);

        _window = new Window
        {
            Title = "Maptor UI check (the mouse is in use for a few seconds)",
            Content = layout,
            Width = 900,
            Height = 650,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Topmost = true,
            ShowInTaskbar = false,
        };

        _presenter = new TestMap();

        // a small area in Iran; no base map is chosen, so nothing is downloaded
        var mapSettings = new MapSettings { InitialExtent = new BoundingBox(5_700_000, 4_200_000, 5_709_000, 4_206_500) };

        settings?.Invoke(mapSettings);

        // no grid on the map: only what a check draws is on it
        var general = GeneralSettings.Default;

        general.MapGrids_SelectedKeys = string.Empty;

        _presenter.InitializeSettings(ProxySettings.Default, BaseMapSettings.Default, mapSettings, general);

        _window.Show();

        PlaceOnMonitorOf(front);

        Pump(100);

        var initialize = MapInitializationHelper.InitializeMapAsync(Map, _window, _presenter);

        WaitFor(() => initialize.IsCompleted, "the map did not start");

        initialize.GetAwaiter().GetResult();

        // and no base map: nothing is downloaded, and a picture of the map shows the check alone
        _presenter.SelectedMapProvider = null;

        Pump(300);
    }

    private static void EnsureResources()
    {
        WpfTestHost.EnsureMahAppsResources();

        var resources = Application.Current.Resources;

        const string all = "pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/Maptor.All.xaml";

        foreach (var dictionary in resources.MergedDictionaries)
        {
            if (dictionary.Source?.OriginalString == all)
                return;
        }

        resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(all, UriKind.Absolute) });
    }

    private static void ClearSharedStyles()
    {
        var cache = typeof(IRI.Maptor.Presentation.Wpf.Helpers.SharedResourceDictionary)
            .GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .GetValue(null) as System.Collections.IDictionary;

        lock (cache!)
            cache.Clear();
    }

    // ---------------------------------------------------------------- time

    /// <summary>Lets the window work (input, layout, rendering, awaits) for <paramref name="milliseconds"/>.</summary>
    public void Pump(int milliseconds)
    {
        var watch = Stopwatch.StartNew();

        do
        {
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));

            Thread.Sleep(5);
        }
        while (watch.ElapsedMilliseconds < milliseconds);
    }

    public void WaitFor(Func<bool> condition, string because, int timeoutMilliseconds = 10_000)
    {
        var watch = Stopwatch.StartNew();

        while (!condition())
        {
            if (watch.ElapsedMilliseconds > timeoutMilliseconds)
                throw new TimeoutException(because);

            Pump(20);
        }
    }

    /// <summary>Waits for <paramref name="task"/> and returns its result.</summary>
    public T Await<T>(Task<T> task, string because)
    {
        WaitFor(() => task.IsCompleted, because);

        return task.GetAwaiter().GetResult();
    }

    // ---------------------------------------------------------------- places

    /// <summary>The map point (Web Mercator) under <paramref name="onMap"/>, a point of the map control in its own units.</summary>
    public Point ToMap(WpfPoint onMap)
    {
        var point = Map.ScreenToMap(onMap);

        return new Point(point.X, point.Y);
    }

    /// <summary>Where <paramref name="mapPoint"/> (Web Mercator) is on the map control.</summary>
    public WpfPoint OnMap(Point mapPoint) => Map.MapToScreen(new WpfPoint(mapPoint.X, mapPoint.Y));

    /// <summary>The length on the map of <paramref name="pixels"/> on screen.</summary>
    public double MapLength(double pixels) => Map.ScreenToMap(pixels);

    // ---------------------------------------------------------------- the mouse

    /// <summary>Moves the pointer to <paramref name="onMap"/>, a point of the map control.</summary>
    public void MoveTo(WpfPoint onMap)
    {
        var screen = Map.PointToScreen(onMap);

        SetCursorPos((int)Math.Round(screen.X), (int)Math.Round(screen.Y));

        // a move of nothing: the window gets a mouse move for the new place even if SetCursorPos sent none
        mouse_event(Move, 0, 0, 0, UIntPtr.Zero);

        Pump(30);
    }

    public void Down()
    {
        mouse_event(LeftDown, 0, 0, 0, UIntPtr.Zero);

        Pump(40);
    }

    public void Up()
    {
        mouse_event(LeftUp, 0, 0, 0, UIntPtr.Zero);

        Pump(40);
    }

    /// <summary>A click at <paramref name="onMap"/>, with no move between press and release.</summary>
    public void Click(WpfPoint onMap)
    {
        MoveTo(onMap);

        Down();

        Up();

        // longer than the double-click time of Windows: the next click is a click of its own
        Pump(SystemInformationDoubleClickTime() + 100);
    }

    /// <summary>A double click at <paramref name="onMap"/>: two clicks within the double-click time of Windows.</summary>
    public void DoubleClick(WpfPoint onMap)
    {
        MoveTo(onMap);

        mouse_event(LeftDown, 0, 0, 0, UIntPtr.Zero);
        Pump(15);
        mouse_event(LeftUp, 0, 0, 0, UIntPtr.Zero);
        Pump(30);
        mouse_event(LeftDown, 0, 0, 0, UIntPtr.Zero);
        Pump(15);
        mouse_event(LeftUp, 0, 0, 0, UIntPtr.Zero);

        Pump(SystemInformationDoubleClickTime() + 100);
    }

    /// <summary>A right click where the pointer is.</summary>
    public void RightClick()
    {
        mouse_event(RightDown, 0, 0, 0, UIntPtr.Zero);

        Pump(40);

        mouse_event(RightUp, 0, 0, 0, UIntPtr.Zero);

        Pump(200);
    }

    /// <summary>Press at <paramref name="from"/>, move to <paramref name="to"/> in steps, release.</summary>
    public void Drag(WpfPoint from, WpfPoint to, int steps = 8)
    {
        MoveTo(from);

        Down();

        for (int i = 1; i <= steps; i++)
        {
            MoveTo(new WpfPoint(from.X + (to.X - from.X) * i / steps, from.Y + (to.Y - from.Y) * i / steps));
        }

        Up();

        Pump(SystemInformationDoubleClickTime() + 100);
    }

    private static int SystemInformationDoubleClickTime() => (int)GetDoubleClickTime();

    // ---------------------------------------------------------------- the keyboard, the capture, pictures

    /// <summary>
    /// A key press, given to the window as WPF would: the map reads the key from the event, so the window does not
    /// have to be the one Windows types into.
    /// </summary>
    public void Press(Key key)
    {
        var source = PresentationSource.FromVisual(_window)!;

        var preview = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent };

        _window.RaiseEvent(preview);

        if (!preview.Handled)
            _window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key) { RoutedEvent = Keyboard.KeyDownEvent });

        Pump(40);
    }

    /// <summary>
    /// A real key press through Windows, to the window that has the keyboard (the map's window is made active first):
    /// it goes the way a key from the keyboard goes, through focus, unlike <see cref="Press"/>.
    /// </summary>
    public void PressReal(Key key, bool control = false, bool shift = false)
    {
        _window.Activate();

        Pump(50);

        const byte ControlKey = 0x11, ShiftKey = 0x10;

        var virtualKey = (byte)KeyInterop.VirtualKeyFromKey(key);

        try
        {
            if (control)
                keybd_event(ControlKey, 0, 0, UIntPtr.Zero);

            if (shift)
                keybd_event(ShiftKey, 0, 0, UIntPtr.Zero);

            keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
            Pump(30);
            keybd_event(virtualKey, 0, KeyUp, UIntPtr.Zero);
        }
        finally
        {
            // never leave a modifier down
            if (shift)
                keybd_event(ShiftKey, 0, KeyUp, UIntPtr.Zero);

            if (control)
                keybd_event(ControlKey, 0, KeyUp, UIntPtr.Zero);
        }

        Pump(150);
    }

    /// <summary>Takes the mouse capture away, as Alt+Tab or a dialog opening in the middle of a drag does.</summary>
    public void LoseCapture()
    {
        Mouse.Capture(null);

        Pump(40);
    }

    /// <summary>The first element of type <typeparamref name="T"/> inside the map, or null.</summary>
    public T? FindInMap<T>() where T : DependencyObject
    {
        T? found = null;

        void Walk(DependencyObject node)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node) && found is null; i++)
            {
                var child = VisualTreeHelper.GetChild(node, i);

                if (child is T typed)
                    found = typed;
                else
                    Walk(child);
            }
        }

        Walk(Map);

        return found;
    }

    /// <summary>The elements of type <typeparamref name="T"/> inside the map that <paramref name="match"/> accepts.</summary>
    public int CountInMap<T>(Func<T, bool> match) where T : DependencyObject
    {
        var count = 0;

        void Walk(DependencyObject node)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            {
                var child = VisualTreeHelper.GetChild(node, i);

                if (child is T typed && match(typed))
                    count++;

                Walk(child);
            }
        }

        Walk(Map);

        return count;
    }

    /// <summary>
    /// Saves a picture of the map as <paramref name="name"/>.png in the folder named by <c>MAPTOR_UI_TEST_SHOTS</c>;
    /// nothing when that is not set. The map shows only what the check drew, so the picture holds no real data.
    /// </summary>
    public void Snapshot(string name)
    {
        if (Environment.GetEnvironmentVariable("MAPTOR_UI_TEST_SHOTS") is not { Length: > 0 } folder)
            return;

        Pump(150);

        Directory.CreateDirectory(folder);

        var dpi = VisualTreeHelper.GetDpi(Map);

        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(Map.ActualWidth * dpi.DpiScaleX), (int)Math.Ceiling(Map.ActualHeight * dpi.DpiScaleY),
            dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);

        // a white sheet behind the map: it has no base map, and a transparent picture is hard to read
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, Map.ActualWidth, Map.ActualHeight));
            context.DrawRectangle(new VisualBrush(Map), null, new Rect(0, 0, Map.ActualWidth, Map.ActualHeight));
        }

        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();

        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var file = File.Create(Path.Combine(folder, name + ".png"));

        encoder.Save(file);
    }

    // ---------------------------------------------------------------- the window

    private void PlaceOnMonitorOf(IntPtr front)
    {
        var handle = new WindowInteropHelper(_window).Handle;

        var monitor = MonitorFromWindow(front != IntPtr.Zero ? front : handle, MonitorDefaultToNearest);

        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };

        if (!GetMonitorInfo(monitor, ref info))
            return;

        SetWindowPos(handle, IntPtr.Zero, info.Work.Left + 60, info.Work.Top + 60, 0, 0, NoSize | NoZOrder);
    }

    private const uint Move = 0x0001, LeftDown = 0x0002, LeftUp = 0x0004, RightDown = 0x0008, RightUp = 0x0010;
    private const uint MonitorDefaultToNearest = 2;
    private const uint NoSize = 0x0001, NoZOrder = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left; public int Top; public int Right; public int Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo { public int Size; public NativeRect Monitor; public NativeRect Work; public uint Flags; }

    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extraInfo);
    private const uint KeyUp = 0x0002;
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
    [DllImport("user32.dll")] private static extern uint GetDoubleClickTime();
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int cx, int cy, uint flags);
}
