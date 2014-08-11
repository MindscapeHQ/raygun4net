using System;
using MonoMac.Foundation;
using System.Globalization;
using System.Runtime.InteropServices;
using MonoMac.AppKit;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    public RaygunEnvironmentMessage ()
    {
      UtcOffset = NSTimeZone.LocalTimeZone.GetSecondsFromGMT / 3600.0;

      Architecture = GetStringSysCtl(ArchitecturePropertyName);
      ProcessorCount = (int)GetIntSysCtl(ProcessiorCountPropertyName);

      Locale = CultureInfo.CurrentCulture.DisplayName;

      if (NSApplication.SharedApplication != null && NSApplication.SharedApplication.KeyWindow != null)
      {
        WindowBoundsWidth = NSApplication.SharedApplication.KeyWindow.Frame.Width;
        WindowBoundsHeight = NSApplication.SharedApplication.KeyWindow.Frame.Height;
      }

      TotalPhysicalMemory = GetIntSysCtl(TotalPhysicalMemoryPropertyName);
      AvailablePhysicalMemory = GetIntSysCtl(AvailablePhysicalMemoryPropertyName);

      string osType = GetStringSysCtl(OSTypePropertyName);
      string version = GetStringSysCtl(OSReleasePropertyName);
      if ("Darwin".Equals(osType) && !String.IsNullOrWhiteSpace(version))
      {
        if (version.StartsWith("14"))
        {
          OSVersion = "OS X v10.10 Yosemite";
        }
        else if (version.StartsWith("13"))
        {
          OSVersion = "OS X v10.9 Mavericks";
        }
        else if (version.StartsWith("12.5"))
        {
          OSVersion = "OS X v10.8.5 Mountain Lion";
        }
        else if (version.StartsWith("12"))
        {
          OSVersion = "OS X v10.8 Mountain Lion";
        }
        else if (version.StartsWith("11"))
        {
          OSVersion = "Mac OS X v10.7 Lion";
        }
        else if (version.StartsWith("10"))
        {
          OSVersion = "Mac OS X v10.6 Snow Leopard";
        }
        else if (version.StartsWith("9"))
        {
          OSVersion = "Mac OS X v10.5 Leopard";
        }
        else if (version.StartsWith("8"))
        {
          OSVersion = "Mac OS X v10.4 Tiger";
        }
        else if (version.StartsWith("7"))
        {
          OSVersion = "Mac OS X v10.3 Panther";
        }
        else if (version.StartsWith("6"))
        {
          OSVersion = "Mac OS X v10.2 Jaguar";
        }
        else if (version.StartsWith("5"))
        {
          OSVersion = "Mac OS X v10.1 Puma";
        }
      }

      if (OSVersion != null)
      {
        OSVersion += " (" + osType + " " + version + ")";
      }
      else if (!String.IsNullOrWhiteSpace(osType) && !"Unknown".Equals(osType) && !String.IsNullOrWhiteSpace(version) && !"Unknown".Equals(version))
      {
        OSVersion = osType + " " + version;
      }
      else
      {
        OSVersion = "Unknown";
      }
    }

    private const string TotalPhysicalMemoryPropertyName = "hw.physmem";
    private const string AvailablePhysicalMemoryPropertyName = "hw.usermem";
    private const string ProcessiorCountPropertyName = "hw.ncpu";
    private const string ArchitecturePropertyName = "hw.machine";
    private const string OSTypePropertyName = "kern.ostype";
    private const string OSReleasePropertyName = "kern.osrelease";

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

    public int ProcessorCount { get; set; }

    public string OSVersion { get; set; }

    public double WindowBoundsWidth { get; set; }

    public double WindowBoundsHeight { get; set; }

    public string Architecture { get; set; }

    public ulong TotalPhysicalMemory { get; set; }

    public ulong AvailablePhysicalMemory { get; set; }

    public double UtcOffset { get; set; }

    public string Locale { get; set; }

    public override string ToString()
    {
      // This exists because Reflection in Xamarin Mac can't seem to obtain the Getter methods unless the getter is used somewhere in the code.
      // The getter of all properties is required to serialize the Raygun messages to JSON.
      return string.Format("[RaygunEnvironmentMessage: ProcessorCount={0}, OSVersion={1}, WindowBoundsWidth={2}, WindowBoundsHeight={3}, Architecture={4}, TotalPhysicalMemory={5}, AvailablePhysicalMemory={6}, UtcOffset={7}, Locale={8}]", ProcessorCount, OSVersion, WindowBoundsWidth, WindowBoundsHeight, Architecture, TotalPhysicalMemory, AvailablePhysicalMemory, UtcOffset, Locale);
    }
  }
}

