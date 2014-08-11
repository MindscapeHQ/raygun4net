using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

using Android.OS;
using Android.Content.Res;
using Android.Content;
using Android.Views;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Bluetooth;

namespace Mindscape.Raygun4Net.Messages.Builders
{
  public class RaygunEnvironmentMessageBuilder
  {
    public RaygunEnvironmentMessage Build()
    {
      var raygunEnvironmentMessage = new RaygunEnvironmentMessage();

      try
      {
        Java.Util.TimeZone tz = Java.Util.TimeZone.Default;
        Java.Util.Date now = new Java.Util.Date();
        raygunEnvironmentMessage.UtcOffset = tz.GetOffset(now.Time) / 3600000.0;

        raygunEnvironmentMessage.OSVersion = Android.OS.Build.VERSION.Sdk;

        raygunEnvironmentMessage.Locale = CultureInfo.CurrentCulture.DisplayName;

        var metrics = Resources.System.DisplayMetrics;
        raygunEnvironmentMessage.WindowBoundsWidth = metrics.WidthPixels;
        raygunEnvironmentMessage.WindowBoundsHeight = metrics.HeightPixels;

        Context context = RaygunClient.Context;
        if (context != null)
        {
          PackageManager manager = context.PackageManager;
          PackageInfo info = manager.GetPackageInfo(context.PackageName, 0);
          raygunEnvironmentMessage.PackageVersion = info.VersionCode + " / " + info.VersionName;

          IWindowManager windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
          if (windowManager != null)
          {
            Display display = windowManager.DefaultDisplay;
            if (display != null)
            {
              switch (display.Rotation)
              {
                case SurfaceOrientation.Rotation0:
                  raygunEnvironmentMessage.CurrentOrientation = "Rotation 0 (Portrait)";
                  break;
                case SurfaceOrientation.Rotation180:
                  raygunEnvironmentMessage.CurrentOrientation = "Rotation 180 (Upside down)";
                  break;
                case SurfaceOrientation.Rotation270:
                  raygunEnvironmentMessage.CurrentOrientation = "Rotation 270 (Landscape right)";
                  break;
                case SurfaceOrientation.Rotation90:
                  raygunEnvironmentMessage.CurrentOrientation = "Rotation 90 (Landscape left)";
                  break;
              }
            }
          }
        }

        raygunEnvironmentMessage.DeviceName = "Unknown";

        Java.Lang.Runtime runtime = Java.Lang.Runtime.GetRuntime();
        raygunEnvironmentMessage.TotalPhysicalMemory = (ulong)runtime.TotalMemory();
        raygunEnvironmentMessage.AvailablePhysicalMemory = (ulong)runtime.FreeMemory();

        raygunEnvironmentMessage.ProcessorCount = runtime.AvailableProcessors();
        raygunEnvironmentMessage.Architecture = Android.OS.Build.CpuAbi;
        raygunEnvironmentMessage.Model = string.Format("{0} / {1} / {2}", Android.OS.Build.Model, Android.OS.Build.Brand, Android.OS.Build.Manufacturer);
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("Error getting environment info {0}", ex.Message));
      }

      return raygunEnvironmentMessage;
    }
  }
}