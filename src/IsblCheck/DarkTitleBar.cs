using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace IsblCheck
{
  internal static class DarkTitleBar
  {
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void Apply(object sender, EventArgs e)
    {
      if (sender is not Window window || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763)) return;
      var handle = new WindowInteropHelper(window).Handle;
      var enabled = 1;
      try
      {
        // Windows 10 1903+ uses 20; older supported builds used 19.
        if (DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int)) != 0)
          DwmSetWindowAttribute(handle, 19, ref enabled, sizeof(int));
      }
      catch (DllNotFoundException) { }
      catch (EntryPointNotFoundException) { }
    }
  }
}
