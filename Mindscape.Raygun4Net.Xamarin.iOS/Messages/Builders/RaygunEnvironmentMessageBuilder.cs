using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

using MonoTouch.UIKit;
using MonoTouch.Foundation;

namespace Mindscape.Raygun4Net.Messages.Builders
{
  public class RaygunEnvironmentMessageBuilder
  {
    public RaygunEnvironmentMessage Build()
    {
      var raygunEnvironmentMessage = new RaygunEnvironmentMessage();

      raygunEnvironmentMessage.UtcOffset = NSTimeZone.LocalTimeZone.GetSecondsFromGMT / 3600.0;

      raygunEnvironmentMessage.OSVersion = UIDevice.CurrentDevice.SystemName + " " + UIDevice.CurrentDevice.SystemVersion;
      raygunEnvironmentMessage.Architecture = GetStringSysCtl(ArchitecturePropertyName);
      raygunEnvironmentMessage.Model = UIDevice.CurrentDevice.Model;
      raygunEnvironmentMessage.ProcessorCount = (int)GetIntSysCtl(ProcessiorCountPropertyName);

      raygunEnvironmentMessage.Locale = CultureInfo.CurrentCulture.DisplayName;

      UIApplication.SharedApplication.InvokeOnMainThread(() =>
      {
        raygunEnvironmentMessage.WindowBoundsWidth = UIScreen.MainScreen.Bounds.Width;
        raygunEnvironmentMessage.WindowBoundsHeight = UIScreen.MainScreen.Bounds.Height;
      });

      raygunEnvironmentMessage.CurrentOrientation = UIDevice.CurrentDevice.Orientation.ToString();

      raygunEnvironmentMessage.TotalPhysicalMemory = GetIntSysCtl(TotalPhysicalMemoryPropertyName);
      raygunEnvironmentMessage.AvailablePhysicalMemory = GetIntSysCtl(AvailablePhysicalMemoryPropertyName);

      raygunEnvironmentMessage.DeviceName = UIDevice.CurrentDevice.Name;
      raygunEnvironmentMessage.PackageVersion = NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleVersion").ToString();

      return raygunEnvironmentMessage;
    }

    private const string TotalPhysicalMemoryPropertyName = "hw.physmem";
    private const string AvailablePhysicalMemoryPropertyName = "hw.usermem";
    private const string ProcessiorCountPropertyName = "hw.ncpu";
    private const string ArchitecturePropertyName = "hw.machine";

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