using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    public RaygunEnvironmentMessage()
    {
      // Different environments can fail to load the environment details.
      // For now if they fail to load for whatever reason then just
      // swallow the exception. A good addition would be to handle
      // these cases and load them correctly depending on where its running.
      // see http://raygun.io/forums/thread/3655
      try
      {
        DateTime now = DateTime.Now;
        UtcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(now).TotalHours;

        IntPtr hWnd = GetActiveWindow();
        RECT rect;
        GetWindowRect(hWnd, out rect);
        WindowBoundsWidth = rect.Right - rect.Left;
        WindowBoundsHeight = rect.Bottom - rect.Top;

        ProcessorCount = Environment.ProcessorCount;
        Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
        OSVersion = Environment.OSVersion.VersionString;

        Locale = CultureInfo.CurrentCulture.DisplayName;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("Error getting environment info: {0}", ex.Message));
      }
    }

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    private IntPtr GetActiveWindow()
    {
      IntPtr handle = IntPtr.Zero;
      return GetForegroundWindow();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
      public int Left;        // x position of upper-left corner  
      public int Top;         // y position of upper-left corner  
      public int Right;       // x position of lower-right corner  
      public int Bottom;      // y position of lower-right corner  
    }

    private struct MEMORY_STATUS
    {
      public int dwLength;
      public int dwMemoryLoad;
      public int dwTotalPhys;
      public int dwAvailPhys;
      public int dwTotalPageFile;
      public int dwAvailPageFile;
      public int dwTotalVirtual;
      public int dwAvailVirtual;
    }

    [DllImport("coredll.dll", SetLastError = true)]
    private void GlobalMemoryStatus(ref MEMORY_STATUS ms) {}

    public void GetAvailablePhysicalMemory()
    {
      var ms = new MEMORY_STATUS();
      try
      {
        GlobalMemoryStatus(ms);
        double avail = ms.dwAvailPhys / 1048.576;
      }
      catch
      {

      }
    }

    public int ProcessorCount { get; private set; }

    public string OSVersion { get; private set; }

    public double WindowBoundsWidth { get; private set; }

    public double WindowBoundsHeight { get; private set; }

    public string ResolutionScale { get; private set; }

    public string Architecture { get; private set; }

    public string Model { get; private set; }

    public ulong TotalVirtualMemory { get; private set; }

    public ulong AvailableVirtualMemory { get; private set; }

    public ulong TotalPhysicalMemory { get; private set; }

    public ulong AvailablePhysicalMemory { get; private set; }

    public double UtcOffset { get; private set; }

    public string Locale { get; private set; }
  }
}
