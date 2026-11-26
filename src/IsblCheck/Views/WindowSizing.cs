using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace IsblCheck.Views
{
  internal static class WindowSizing
  {
    internal static void FitMainWindow(object sender, EventArgs e)
    {
      var window = (Window)sender;
      var area = WorkAreaSize(window);
      window.MinWidth = Math.Min(window.MinWidth, area.Width);
      window.MinHeight = Math.Min(window.MinHeight, area.Height);
      window.Width = Math.Clamp(window.Width, window.MinWidth, area.Width);
      window.Height = Math.Clamp(window.Height, window.MinHeight, area.Height);
    }

    internal static void FitDialog(object sender, EventArgs e)
    {
      var window = (Window)sender;
      var area = WorkAreaSize(window.Owner ?? window);
      window.MaxWidth = Math.Min(window.MaxWidth, area.Width);
      window.MaxHeight = Math.Min(window.MaxHeight, area.Height);
      window.MinWidth = Math.Min(window.MinWidth, window.MaxWidth);
      window.MinHeight = Math.Min(window.MinHeight, window.MaxHeight);
    }

    internal static void KeepInitialPositionVisible(object sender, EventArgs e)
    {
      var window = (Window)sender;
      window.ContentRendered -= KeepInitialPositionVisible;
      if (window.WindowState != WindowState.Normal) return;
      var handle = new WindowInteropHelper(window).Handle;
      if (!TryGetWorkArea(handle, out var work) || !GetWindowRect(handle, out var bounds)) return;
      // WPF may center at the requested size before SourceInitialized caps it.
      // Clamp the position once using the same physical coordinates as Windows.
      var left = Math.Clamp(bounds.Left, work.Left, Math.Max(work.Left, work.Right - (bounds.Right - bounds.Left)));
      var top = Math.Clamp(bounds.Top, work.Top, Math.Max(work.Top, work.Bottom - (bounds.Bottom - bounds.Top)));
      if (left != bounds.Left || top != bounds.Top)
        SetWindowPos(handle, IntPtr.Zero, left, top, 0, 0, 0x0015); // Keep size, Z-order and activation.
    }

    private static Size WorkAreaSize(Window window)
    {
      var handle = new WindowInteropHelper(window).Handle;
      if (TryGetWorkArea(handle, out var work))
      {
        var dpi = VisualTreeHelper.GetDpi(window);
        return new Size((work.Right - work.Left) / dpi.DpiScaleX, (work.Bottom - work.Top) / dpi.DpiScaleY);
      }
      return SystemParameters.WorkArea.Size;
    }

    private static bool TryGetWorkArea(IntPtr handle, out NativeRect work)
    {
      work = default;
      if (handle == IntPtr.Zero) return false;
      var monitor = MonitorFromWindow(handle, 2); // Nearest monitor.
      var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
      if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info)) return false;
      work = info.Work;
      return true;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
      public int Size;
      public NativeRect Monitor, Work;
      public uint Flags;
    }

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr window, out NativeRect bounds);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
  }
}
