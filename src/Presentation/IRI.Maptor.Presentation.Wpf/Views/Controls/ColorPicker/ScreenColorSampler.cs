using System;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// Reads the colour of the screen pixel under the mouse cursor, for <see cref="MaptorColorPicker"/>'s
/// eyedropper. Win32 only (<c>GetCursorPos</c> + <c>GetPixel</c> on the screen DC), which this
/// Windows-only assembly can rely on.
/// <para>
/// Cursor position and screen DC are both in the process's DPI-awareness space, so they agree on a
/// single monitor and on per-monitor-aware processes. A system-aware process on a second monitor with a
/// different scale can read a pixel a little off the cursor; the colour is still from under it.
/// </para>
/// </summary>
internal static class ScreenColorSampler
{
    /// <summary>The colour under the cursor (opaque), or null when Windows refuses to read it.</summary>
    public static Color? ColorUnderCursor()
    {
        if (!GetCursorPos(out var p))
            return null;

        var dc = GetDC(IntPtr.Zero);

        if (dc == IntPtr.Zero)
            return null;

        try
        {
            uint value = GetPixel(dc, p.X, p.Y);

            if (value == ClrInvalid)
                return null;

            // COLORREF is 0x00BBGGRR.
            return Color.FromRgb((byte)value, (byte)(value >> 8), (byte)(value >> 16));
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, dc);
        }
    }

    private const uint ClrInvalid = 0xFFFFFFFF;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(IntPtr hDc, int x, int y);
}
