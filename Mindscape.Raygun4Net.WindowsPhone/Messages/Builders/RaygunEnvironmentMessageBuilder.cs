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

namespace Mindscape.Raygun4Net.Messages.Builders
{
  public class RaygunEnvironmentMessageBuilder
  {
    private List<double> _diskSpaceFree = new List<double>();

    public RaygunEnvironmentMessage Build()
    {
      var raygunEnvironmentMessage = new RaygunEnvironmentMessage();

      raygunEnvironmentMessage.Locale = CultureInfo.CurrentCulture.DisplayName;
      raygunEnvironmentMessage.OSVersion = Environment.OSVersion.Platform + " " + Environment.OSVersion.Version;
      object deviceName;
      DeviceExtendedProperties.TryGetValue("DeviceName", out deviceName);
      raygunEnvironmentMessage.DeviceName = deviceName.ToString();

      DateTime now = DateTime.Now;
      raygunEnvironmentMessage.UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;

      if (Application.Current != null && Application.Current.RootVisual != null)
      {
        raygunEnvironmentMessage.WindowBoundsWidth = Application.Current.RootVisual.RenderSize.Width;
        raygunEnvironmentMessage.WindowBoundsHeight = Application.Current.RootVisual.RenderSize.Height;
        PhoneApplicationFrame frame = Application.Current.RootVisual as PhoneApplicationFrame;
        if (frame != null)
        {
          raygunEnvironmentMessage.CurrentOrientation = frame.Orientation.ToString();
        }
      }

      try
      {
        raygunEnvironmentMessage.ApplicationCurrentMemoryUsage = DeviceStatus.ApplicationCurrentMemoryUsage;
        raygunEnvironmentMessage.ApplicationMemoryUsageLimit = DeviceStatus.ApplicationMemoryUsageLimit;
        raygunEnvironmentMessage.ApplicationPeakMemoryUsage = DeviceStatus.ApplicationPeakMemoryUsage;
        raygunEnvironmentMessage.DeviceTotalMemory = DeviceStatus.DeviceTotalMemory;
      }
      catch (Exception e)
      {
        Debug.WriteLine("Faild to get device memory information: {0}", e.Message);
      }

      try
      {
        raygunEnvironmentMessage.DeviceFirmwareVersion = DeviceStatus.DeviceFirmwareVersion;
        raygunEnvironmentMessage.DeviceHardwareVersion = DeviceStatus.DeviceHardwareVersion;
        raygunEnvironmentMessage.DeviceManufacturer = DeviceStatus.DeviceManufacturer;
      }
      catch (Exception e)
      {
        Debug.WriteLine("Failed to get device information: {0}", e.Message);
      }

      try
      {
        raygunEnvironmentMessage.IsolatedStorageAvailableFreeSpace = IsolatedStorageFile.GetUserStoreForApplication().AvailableFreeSpace;
      }
      catch (Exception e)
      {
        Debug.WriteLine("Failed to get isolated storage memory: {0}", e.Message);
      }

      return raygunEnvironmentMessage;
    }
  }
}
