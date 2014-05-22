using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualBasic.Devices;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    private List<double> _diskSpaceFree = new List<double>();

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

        ComputerInfo info = new ComputerInfo();
        TotalPhysicalMemory = (ulong)info.TotalPhysicalMemory / 0x100000; // in MB
        AvailablePhysicalMemory = (ulong)info.AvailablePhysicalMemory / 0x100000;
        TotalVirtualMemory = info.TotalVirtualMemory / 0x100000;
        AvailableVirtualMemory = info.AvailableVirtualMemory / 0x100000;
        GetDiskSpace();

        Locale = CultureInfo.CurrentCulture.DisplayName;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("Error getting environment info: {0}", ex.Message));
      }
    }

    private void GetDiskSpace()
    {
      foreach (DriveInfo drive in DriveInfo.GetDrives())
      {
        if (drive.IsReady)
        {
          DiskSpaceFree.Add((double)drive.AvailableFreeSpace / 0x40000000); // in GB
        }
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
      public int Left;
      public int Top;
      public int Right;
      public int Bottom;
    }

    public int ProcessorCount { get; private set; }

    public string OSVersion { get; private set; }

    public double WindowBoundsWidth { get; private set; }

    public double WindowBoundsHeight { get; private set; }

    public string ResolutionScale { get; private set; }

    public string Architecture { get; private set; }

    public ulong TotalVirtualMemory { get; private set; }

    public ulong AvailableVirtualMemory { get; private set; }

    public List<double> DiskSpaceFree
    {
      get { return _diskSpaceFree; }
      set { _diskSpaceFree = value; }
    }

    public ulong TotalPhysicalMemory { get; private set; }

    public ulong AvailablePhysicalMemory { get; private set; }

    public double UtcOffset { get; private set; }

    public string Locale { get; private set; }
  }
}
