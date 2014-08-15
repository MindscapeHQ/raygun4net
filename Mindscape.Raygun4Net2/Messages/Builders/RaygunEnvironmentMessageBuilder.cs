using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualBasic.Devices;

namespace Mindscape.Raygun4Net.Messages.Builders
{
  public class RaygunEnvironmentMessageBuilder
  {
    public RaygunEnvironmentMessage Build()
    {
      var rayrunEnvironmentMessage = new RaygunEnvironmentMessage();

      // Different environments can fail to load the environment details.
      // For now if they fail to load for whatever reason then just
      // swallow the exception. A good addition would be to handle
      // these cases and load them correctly depending on where its running.
      // see http://raygun.io/forums/thread/3655
      try
      {
        DateTime now = DateTime.Now;
        rayrunEnvironmentMessage.UtcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(now).TotalHours;

        IntPtr hWnd = GetActiveWindow();
        RECT rect;
        GetWindowRect(hWnd, out rect);
        rayrunEnvironmentMessage.WindowBoundsWidth = rect.Right - rect.Left;
        rayrunEnvironmentMessage.WindowBoundsHeight = rect.Bottom - rect.Top;

        rayrunEnvironmentMessage.ProcessorCount = Environment.ProcessorCount;
        rayrunEnvironmentMessage.Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
        rayrunEnvironmentMessage.OSVersion = Environment.OSVersion.VersionString;

        ComputerInfo info = new ComputerInfo();
        rayrunEnvironmentMessage.TotalPhysicalMemory = (ulong)info.TotalPhysicalMemory / 0x100000; // in MB
        rayrunEnvironmentMessage.AvailablePhysicalMemory = (ulong)info.AvailablePhysicalMemory / 0x100000;
        rayrunEnvironmentMessage.TotalVirtualMemory = info.TotalVirtualMemory / 0x100000;
        rayrunEnvironmentMessage.AvailableVirtualMemory = info.AvailableVirtualMemory / 0x100000;
        rayrunEnvironmentMessage.DiskSpaceFree = GetDiskSpace();

        rayrunEnvironmentMessage.Locale = CultureInfo.CurrentCulture.DisplayName;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("Error getting environment info: {0}", ex.Message));
      }

      return rayrunEnvironmentMessage;
    }

    private IEnumerable<double> GetDiskSpace()
    {
      foreach (DriveInfo drive in DriveInfo.GetDrives())
      {
        if (drive.IsReady)
        {
          yield return (double)drive.AvailableFreeSpace / 0x40000000; // in GB
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
  }
}
