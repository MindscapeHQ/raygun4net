using System;
using Mindscape.Raygun4Net.Messages;
using System.Runtime.InteropServices;
using MonoMac.Foundation;
using System.Globalization;
using MonoMac.AppKit;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunEnvironmentMessageBuilder
  {
    private const string TotalPhysicalMemoryPropertyName = "hw.physmem";
    private const string AvailablePhysicalMemoryPropertyName = "hw.usermem";
    private const string ProcessiorCountPropertyName = "hw.ncpu";
    private const string ArchitecturePropertyName = "hw.machine";
    private const string OSTypePropertyName = "kern.ostype";
    private const string OSReleasePropertyName = "kern.osrelease";

    public static RaygunEnvironmentMessage Build()
    {
      RaygunEnvironmentMessage message = new RaygunEnvironmentMessage ();

      try
      {
        if (NSApplication.SharedApplication != null && NSApplication.SharedApplication.KeyWindow != null)
        {
          message.WindowBoundsWidth = NSApplication.SharedApplication.KeyWindow.Frame.Width;
          message.WindowBoundsHeight = NSApplication.SharedApplication.KeyWindow.Frame.Height;
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine("Error retrieving window dimensions: {0}", ex.Message);
      }

      try
      {
        message.UtcOffset = NSTimeZone.LocalTimeZone.GetSecondsFromGMT / 3600.0;
        message.Locale = CultureInfo.CurrentCulture.DisplayName;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine("Error retrieving time and locale: {0}", ex.Message);
      }

      try
      {
        message.Architecture = GetStringSysCtl(ArchitecturePropertyName);
        message.ProcessorCount = (int)GetIntSysCtl(ProcessiorCountPropertyName);
        message.TotalPhysicalMemory = GetIntSysCtl(TotalPhysicalMemoryPropertyName);
        message.AvailablePhysicalMemory = GetIntSysCtl(AvailablePhysicalMemoryPropertyName);
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine("Error retrieving device info: {0}", ex.Message);
      }

      try
      {
        string osType = GetStringSysCtl(OSTypePropertyName);
        string version = GetStringSysCtl(OSReleasePropertyName);
        string osVersion = null;
        if ("Darwin".Equals(osType) && !String.IsNullOrWhiteSpace(version))
        {
          if (version.StartsWith("14"))
          {
            osVersion = "OS X v10.10 Yosemite";
          }
          else if (version.StartsWith("13"))
          {
            osVersion = "OS X v10.9 Mavericks";
          }
          else if (version.StartsWith("12.5"))
          {
            osVersion = "OS X v10.8.5 Mountain Lion";
          }
          else if (version.StartsWith("12"))
          {
            osVersion = "OS X v10.8 Mountain Lion";
          }
          else if (version.StartsWith("11"))
          {
            osVersion = "Mac OS X v10.7 Lion";
          }
          else if (version.StartsWith("10"))
          {
            osVersion = "Mac OS X v10.6 Snow Leopard";
          }
          else if (version.StartsWith("9"))
          {
            osVersion = "Mac OS X v10.5 Leopard";
          }
          else if (version.StartsWith("8"))
          {
            osVersion = "Mac OS X v10.4 Tiger";
          }
          else if (version.StartsWith("7"))
          {
            osVersion = "Mac OS X v10.3 Panther";
          }
          else if (version.StartsWith("6"))
          {
            osVersion = "Mac OS X v10.2 Jaguar";
          }
          else if (version.StartsWith("5"))
          {
            osVersion = "Mac OS X v10.1 Puma";
          }
        }

        if (osVersion != null)
        {
          osVersion += " (" + osType + " " + version + ")";
        }
        else if (!String.IsNullOrWhiteSpace(osType) && !"Unknown".Equals(osType) && !String.IsNullOrWhiteSpace(version) && !"Unknown".Equals(version))
        {
          osVersion = osType + " " + version;
        }
        else
        {
          osVersion = "Unknown";
        }

        message.OSVersion = osVersion;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine("Error retrieving OSVersion: {0}", ex.Message);
      }

      return message;
    }

    [DllImport(global::MonoMac.Constants.SystemLibrary)]
    private static extern int sysctlbyname([MarshalAs(UnmanagedType.LPStr)] string property,
      IntPtr output,
      IntPtr oldLen,
      IntPtr newp,
      uint newlen);

    private static uint GetIntSysCtl(string propertyName)
    {
      // get the length of the string that will be returned
      var pLen = Marshal.AllocHGlobal(sizeof(int));
      sysctlbyname(propertyName, IntPtr.Zero, pLen, IntPtr.Zero, 0);

      var length = Marshal.ReadInt32(pLen);

      // check to see if we got a length
      if (length <= 0)
      {
        Marshal.FreeHGlobal(pLen);
        return 0;
      }

      // get the hardware string
      var pStr = Marshal.AllocHGlobal(length);
      sysctlbyname(propertyName, pStr, pLen, IntPtr.Zero, 0);

      // convert the native string into a C# integer

      var memoryCount = Marshal.ReadInt32(pStr);
      uint memoryVal = (uint)memoryCount;

      if (memoryCount < 0)
      {
        memoryVal = (uint)((uint)int.MaxValue + (-memoryCount));
      }

      var ret = memoryVal;

      // cleanup
      Marshal.FreeHGlobal(pLen);
      Marshal.FreeHGlobal(pStr);

      return ret;
    }

    private static string GetStringSysCtl(string propertyName)
    {
      // get the length of the string that will be returned
      var pLen = Marshal.AllocHGlobal(sizeof(int));
      sysctlbyname(propertyName, IntPtr.Zero, pLen, IntPtr.Zero, 0);

      var length = Marshal.ReadInt32(pLen);

      // check to see if we got a length
      if (length <= 0)
      {
        Marshal.FreeHGlobal(pLen);
        return "Unknown";
      }

      // get the hardware string
      var pStr = Marshal.AllocHGlobal(length);
      sysctlbyname(propertyName, pStr, pLen, IntPtr.Zero, 0);

      // convert the native string into a C# string
      var hardwareStr = Marshal.PtrToStringAnsi(pStr);

      var ret = hardwareStr;

      // cleanup
      Marshal.FreeHGlobal(pLen);
      Marshal.FreeHGlobal(pStr);

      return ret;
    }
  }
}

