using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualBasic.Devices;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunEnvironmentMessageBuilder
  {
    public static RaygunEnvironmentMessage Build()
    {
      RaygunEnvironmentMessage message = new RaygunEnvironmentMessage();

      // Different environments can fail to load the environment details.
      // For now if they fail to load for whatever reason then just
      // swallow the exception. A good addition would be to handle
      // these cases and load them correctly depending on where its running.
      // see http://raygun.com/forums/thread/3655

      try
      {
        IntPtr hWnd = GetActiveWindow();
        RECT rect;
        GetWindowRect(hWnd, out rect);
        message.WindowBoundsWidth = rect.Right - rect.Left;
        message.WindowBoundsHeight = rect.Bottom - rect.Top;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("Error retrieving window dimensions: {0}", ex.Message));
      }

      try
      {
        DateTime now = DateTime.Now;
        message.UtcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(now).TotalHours;
        message.Locale = CultureInfo.CurrentCulture.DisplayName;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("Error retrieving time and locale: {0}", ex.Message));
      }

      try
      {
        message.ProcessorCount = Environment.ProcessorCount;
        message.Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
        message.OSVersion = Environment.OSVersion.VersionString;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("Error retrieving processor info: {0}", ex.Message));
      }

      try
      {
        ComputerInfo info = new ComputerInfo();
        message.TotalPhysicalMemory = (ulong)info.TotalPhysicalMemory / 0x100000; // in MB
        message.AvailablePhysicalMemory = (ulong)info.AvailablePhysicalMemory / 0x100000;
        message.TotalVirtualMemory = info.TotalVirtualMemory / 0x100000;
        message.AvailableVirtualMemory = info.AvailableVirtualMemory / 0x100000;
        message.DiskSpaceFree = GetDiskSpace();
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("Error retrieving memory info: {0}", ex.Message));
      }

      return message;
    }

    private static List<double> GetDiskSpace()
    {
      List<double> diskSpaceFree = new List<double>();
      foreach (DriveInfo drive in DriveInfo.GetDrives())
      {
        if (drive.IsReady)
        {
          diskSpaceFree.Add((double)drive.AvailableFreeSpace / 0x40000000); // in GB
        }
      }
      return diskSpaceFree;
    }

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    private static IntPtr GetActiveWindow()
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
