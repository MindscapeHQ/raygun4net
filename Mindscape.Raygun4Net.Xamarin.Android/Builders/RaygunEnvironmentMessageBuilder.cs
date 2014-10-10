using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunEnvironmentMessageBuilder
  {
    public static RaygunEnvironmentMessage Build()
    {
      RaygunEnvironmentMessage message = new RaygunEnvironmentMessage();

      try
      {
        Java.Util.TimeZone tz = Java.Util.TimeZone.Default;
        Java.Util.Date now = new Java.Util.Date();
        message.UtcOffset = tz.GetOffset(now.Time) / 3600000.0;

        message.OSVersion = Android.OS.Build.VERSION.Sdk;

        message.Locale = CultureInfo.CurrentCulture.DisplayName;

        var metrics = Resources.System.DisplayMetrics;
        message.WindowBoundsWidth = metrics.WidthPixels;
        message.WindowBoundsHeight = metrics.HeightPixels;

        Context context = RaygunClient.Context;
        if (context != null)
        {
          PackageManager manager = context.PackageManager;
          PackageInfo info = manager.GetPackageInfo(context.PackageName, 0);
          message.PackageVersion = info.VersionCode + " / " + info.VersionName;

          IWindowManager windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
          if (windowManager != null)
          {
            Display display = windowManager.DefaultDisplay;
            if (display != null)
            {
              switch (display.Rotation)
              {
                case SurfaceOrientation.Rotation0:
                  message.CurrentOrientation = "Rotation 0 (Portrait)";
                  break;
                case SurfaceOrientation.Rotation180:
                  message.CurrentOrientation = "Rotation 180 (Upside down)";
                  break;
                case SurfaceOrientation.Rotation270:
                  message.CurrentOrientation = "Rotation 270 (Landscape right)";
                  break;
                case SurfaceOrientation.Rotation90:
                  message.CurrentOrientation = "Rotation 90 (Landscape left)";
                  break;
              }
            }
          }
        }

        message.DeviceName = "Unknown";

        Java.Lang.Runtime runtime = Java.Lang.Runtime.GetRuntime();
        message.TotalPhysicalMemory = (ulong)runtime.TotalMemory();
        message.AvailablePhysicalMemory = (ulong)runtime.FreeMemory();

        message.ProcessorCount = runtime.AvailableProcessors();
        message.Architecture = Android.OS.Build.CpuAbi;
        message.Model = string.Format("{0} / {1} / {2}", Android.OS.Build.Model, Android.OS.Build.Brand, Android.OS.Build.Manufacturer);
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("Error getting environment info {0}", ex.Message));
      }

      return message;
    }
  }
}