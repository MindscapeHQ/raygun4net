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
    public int ProcessorCount { get; set; }

    public string OSVersion { get; set; }

    public double WindowBoundsWidth { get; set; }

    public double WindowBoundsHeight { get; set; }

    public string ResolutionScale { get; set; }

    public string CurrentOrientation { get; set; }

    public string PackageVersion { get; set; }

    public string Architecture { get; set; }

    public string Model { get; set; }

    public ulong TotalPhysicalMemory { get; set; }

    public ulong AvailablePhysicalMemory { get; set; }

    public string DeviceName { get; set; }

    public double UtcOffset { get; set; }

    public string Locale { get; set; }
  }
}