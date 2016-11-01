using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Mindscape.Raygun4Net.Messages;
#if __UNIFIED__
using Foundation;
using UIKit;
#else
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunEnvironmentMessageBuilder
  {
    private const string TotalPhysicalMemoryPropertyName = "hw.physmem";
    private const string AvailablePhysicalMemoryPropertyName = "hw.usermem";
    private const string ProcessiorCountPropertyName = "hw.ncpu";
    private const string ArchitecturePropertyName = "hw.machine";

    public static RaygunEnvironmentMessage Build()
    {
      RaygunEnvironmentMessage message = new RaygunEnvironmentMessage();

      try
      {
        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
          message.WindowBoundsWidth = UIScreen.MainScreen.Bounds.Width;
          message.WindowBoundsHeight = UIScreen.MainScreen.Bounds.Height;
        });
      }
      catch (Exception ex)
      {
        Trace.WriteLine(string.Format("Error retrieving screen dimensions: {0}", ex.Message));
      }

      try
      {
        message.UtcOffset = NSTimeZone.LocalTimeZone.GetSecondsFromGMT / 3600.0;
        message.Locale = CultureInfo.CurrentCulture.DisplayName;
      }
      catch (Exception ex)
      {
        Trace.WriteLine(string.Format("Error retrieving time and locale: {0}", ex.Message));
      }

      try
      {
        message.OSVersion = UIDevice.CurrentDevice.SystemName + " " + UIDevice.CurrentDevice.SystemVersion;
        message.Model = UIDevice.CurrentDevice.Model;
        message.CurrentOrientation = UIDevice.CurrentDevice.Orientation.ToString();
      }
      catch (Exception ex)
      {
        Trace.WriteLine(string.Format("Error retrieving device info: {0}", ex.Message));
      }

      try
      {
        message.ProcessorCount = (int)GetIntSysCtl(ProcessiorCountPropertyName);
        message.Architecture = GetStringSysCtl(ArchitecturePropertyName);
        message.TotalPhysicalMemory = GetIntSysCtl(TotalPhysicalMemoryPropertyName);
        message.AvailablePhysicalMemory = GetIntSysCtl(AvailablePhysicalMemoryPropertyName);
      }
      catch (Exception ex)
      {
        Trace.WriteLine(string.Format("Error retrieving memory and processor: {0}", ex.Message));
      }

      return message;
    }

    #if __UNIFIED__
    [DllImport("/usr/lib/libSystem.dylib")]
    #else
    [DllImport(global::MonoTouch.Constants.SystemLibrary)]
    #endif
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

    internal static string GetStringSysCtl(string propertyName)
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
