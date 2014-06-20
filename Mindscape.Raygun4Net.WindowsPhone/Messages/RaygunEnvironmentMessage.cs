using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

using Microsoft.Phone.Info;
using System.Windows;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    private List<double> _diskSpaceFree = new List<double>();

    public RaygunEnvironmentMessage()
    {
      Locale = CultureInfo.CurrentCulture.DisplayName;
      OSVersion = Environment.OSVersion.Platform + " " + Environment.OSVersion.Version;
      object deviceName;
      DeviceExtendedProperties.TryGetValue("DeviceName", out deviceName);
      DeviceName = deviceName.ToString();

      DateTime now = DateTime.Now;
      UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;

      if (Application.Current != null && Application.Current.RootVisual != null)
      {
        WindowBoundsWidth = Application.Current.RootVisual.RenderSize.Width;
        WindowBoundsHeight = Application.Current.RootVisual.RenderSize.Height;
        PhoneApplicationFrame frame = Application.Current.RootVisual as PhoneApplicationFrame;
        if (frame != null)
        {
          CurrentOrientation = frame.Orientation.ToString();
        }
      }

      try
      {
        ApplicationCurrentMemoryUsage = DeviceStatus.ApplicationCurrentMemoryUsage;
        ApplicationMemoryUsageLimit = DeviceStatus.ApplicationMemoryUsageLimit;
        ApplicationPeakMemoryUsage = DeviceStatus.ApplicationPeakMemoryUsage;
        DeviceTotalMemory = DeviceStatus.DeviceTotalMemory;
      }
      catch (Exception e)
      {
        Debug.WriteLine("Faild to get device memory information: {0}", e.Message);
      }

      try
      {
        DeviceFirmwareVersion = DeviceStatus.DeviceFirmwareVersion;
        DeviceHardwareVersion = DeviceStatus.DeviceHardwareVersion;
        DeviceManufacturer = DeviceStatus.DeviceManufacturer;
      }
      catch (Exception e)
      {
        Debug.WriteLine("Failed to get device information: {0}", e.Message);
      }

      try
      {
        IsolatedStorageAvailableFreeSpace = IsolatedStorageFile.GetUserStoreForApplication().AvailableFreeSpace;
      }
      catch (Exception e)
      {
        Debug.WriteLine("Failed to get isolated storage memory: {0}", e.Message);
      }
    }

    public string OSVersion { get; private set; }

    public double WindowBoundsWidth { get; private set; }

    public double WindowBoundsHeight { get; private set; }

    public string ResolutionScale { get; private set; }

    public string CurrentOrientation { get; private set; }

    public string Cpu { get; private set; }

    public string PackageVersion { get; private set; }

    public string Architecture { get; private set; }

    public long IsolatedStorageAvailableFreeSpace { get; private set; }

    public long ApplicationCurrentMemoryUsage { get; private set; }

    public long ApplicationMemoryUsageLimit { get; private set; }

    public long ApplicationPeakMemoryUsage { get; private set; }

    public long DeviceTotalMemory { get; private set; }

    public string DeviceFirmwareVersion { get; private set; }

    public string DeviceHardwareVersion { get; private set; }

    public string DeviceManufacturer { get; private set; }

    public string DeviceName { get; private set; }

    public double UtcOffset { get; private set; }

    public string Locale { get; private set; }
  }
}