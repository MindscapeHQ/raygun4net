using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

using Windows.UI.Xaml;
using Windows.Devices.Enumeration;
using Windows.Security.ExchangeActiveSyncProvisioning;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    private List<double> _diskSpaceFree = new List<double>();

    public RaygunEnvironmentMessage()
    {
      Locale = CultureInfo.CurrentCulture.DisplayName;

      DateTime now = DateTime.Now;
      UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;

      if (Window.Current != null)
      {
        WindowBoundsWidth = Window.Current.Bounds.Width;
        WindowBoundsHeight = Window.Current.Bounds.Height;

        var sensor = Windows.Devices.Sensors.SimpleOrientationSensor.GetDefault();

        CurrentOrientation = sensor.GetCurrentOrientation().ToString();
      }

      var deviceInfo = new EasClientDeviceInformation();

      try
      {
        DeviceManufacturer = deviceInfo.SystemManufacturer;
        DeviceName = deviceInfo.FriendlyName + " - " + deviceInfo.SystemProductName;
        OSVersion = deviceInfo.OperatingSystem;
      }
      catch (Exception e)
      {
        Debug.WriteLine("Failed to get device information: {0}", e.Message);
      }
    }

    public string OSVersion { get; private set; }

    public double WindowBoundsWidth { get; private set; }

    public double WindowBoundsHeight { get; private set; }

    public string CurrentOrientation { get; private set; }

    public string DeviceManufacturer { get; private set; }

    public string DeviceName { get; private set; }

    public double UtcOffset { get; private set; }

    public string Locale { get; private set; }

    [Obsolete("This is never used")]
    public ulong TotalVirtualMemory { get; private set; }

    [Obsolete("This is never used")]
    public ulong AvailableVirtualMemory { get; private set; }

    [Obsolete("This is never used")]
    public List<double> DiskSpaceFree
    {
      get { return _diskSpaceFree; }
      set { _diskSpaceFree = value; }
    }

    [Obsolete("This is never used")]
    public ulong TotalPhysicalMemory { get; private set; }

    [Obsolete("This is never used")]
    public ulong AvailablePhysicalMemory { get; private set; }
  }
}