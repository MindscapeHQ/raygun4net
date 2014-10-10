using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Mindscape.Raygun4Net.Messages;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

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

      message.UtcOffset = NSTimeZone.LocalTimeZone.GetSecondsFromGMT / 3600.0;

      message.OSVersion = UIDevice.CurrentDevice.SystemName + " " + UIDevice.CurrentDevice.SystemVersion;
      message.Architecture = GetStringSysCtl(ArchitecturePropertyName);
      message.Model = UIDevice.CurrentDevice.Model;
      message.ProcessorCount = (int)GetIntSysCtl(ProcessiorCountPropertyName);

      message.Locale = CultureInfo.CurrentCulture.DisplayName;

      UIApplication.SharedApplication.InvokeOnMainThread(() =>
      {
        message.WindowBoundsWidth = UIScreen.MainScreen.Bounds.Width;
        message.WindowBoundsHeight = UIScreen.MainScreen.Bounds.Height;
      });

      message.CurrentOrientation = UIDevice.CurrentDevice.Orientation.ToString();

      message.TotalPhysicalMemory = GetIntSysCtl(TotalPhysicalMemoryPropertyName);
      message.AvailablePhysicalMemory = GetIntSysCtl(AvailablePhysicalMemoryPropertyName);

      message.DeviceName = UIDevice.CurrentDevice.Name;
      message.PackageVersion = NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleVersion").ToString();

      return message;
    }

    [DllImport(global::MonoTouch.Constants.SystemLibrary)]
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
