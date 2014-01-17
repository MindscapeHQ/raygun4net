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

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    public RaygunEnvironmentMessage()
    {
      try
      {
        Java.Util.TimeZone tz = Java.Util.TimeZone.Default;
        Java.Util.Date now = new Java.Util.Date();
        UtcOffset = tz.GetOffset(now.Time) / 3600000.0;

        OSVersion = Android.OS.Build.VERSION.Sdk;

        Locale = CultureInfo.CurrentCulture.DisplayName;

        var metrics = Resources.System.DisplayMetrics;
        WindowBoundsWidth = metrics.WidthPixels;
        WindowBoundsHeight = metrics.HeightPixels;

        Context context = RaygunClient.Context;
        if (context != null)
        {
          PackageManager manager = context.PackageManager;
          PackageInfo info = manager.GetPackageInfo(context.PackageName, 0);
          PackageVersion = info.VersionCode + " / " + info.VersionName;

          IWindowManager windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
          if (windowManager != null)
          {
            Display display = windowManager.DefaultDisplay;
            if (display != null)
            {
              switch (display.Rotation)
              {
                case SurfaceOrientation.Rotation0:
                  CurrentOrientation = "Rotation 0 (Portrait)";
                  break;
                case SurfaceOrientation.Rotation180:
                  CurrentOrientation = "Rotation 180 (Upside down)";
                  break;
                case SurfaceOrientation.Rotation270:
                  CurrentOrientation = "Rotation 270 (Landscape right)";
                  break;
                case SurfaceOrientation.Rotation90:
                  CurrentOrientation = "Rotation 90 (Landscape left)";
                  break;
              }
            }
          }
        }

        DeviceName = RaygunClient.DeviceName;

        Java.Lang.Runtime runtime = Java.Lang.Runtime.GetRuntime();
        TotalPhysicalMemory = (ulong)runtime.TotalMemory();
        AvailablePhysicalMemory = (ulong)runtime.FreeMemory();

        ProcessorCount = runtime.AvailableProcessors();
        Architecture = Android.OS.Build.CpuAbi;
        Model = string.Format("{0} / {1} / {2}", Android.OS.Build.Model, Android.OS.Build.Brand, Android.OS.Build.Manufacturer);
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("Error getting environment info {0}", ex.Message));
      }
    }

    public int ProcessorCount { get; private set; }

    public string OSVersion { get; private set; }

    public double WindowBoundsWidth { get; private set; }

    public double WindowBoundsHeight { get; private set; }

    public string ResolutionScale { get; private set; }

    public string CurrentOrientation { get; private set; }

    public string PackageVersion { get; private set; }

    public string Architecture { get; private set; }

    public string Model { get; private set; }

    public ulong TotalPhysicalMemory { get; private set; }

    public ulong AvailablePhysicalMemory { get; private set; }

    public string DeviceName { get; private set; }

    public double UtcOffset { get; private set; }

    public string Locale { get; private set; }
  }
}