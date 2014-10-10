using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mindscape.Raygun4Net.Messages;
using Microsoft.Phone.Controls;
using System.Diagnostics;
using Microsoft.Phone.Info;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Globalization;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunEnvironmentMessageBuilder
  {
    public static RaygunEnvironmentMessage Build()
    {
      RaygunEnvironmentMessage message = new RaygunEnvironmentMessage();

      message.Locale = CultureInfo.CurrentCulture.DisplayName;
      message.OSVersion = Environment.OSVersion.Platform + " " + Environment.OSVersion.Version;
      object deviceName;
      DeviceExtendedProperties.TryGetValue("DeviceName", out deviceName);
      message.DeviceName = deviceName.ToString();

      DateTime now = DateTime.Now;
      message.UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;

      if (Application.Current != null && Application.Current.RootVisual != null)
      {
        message.WindowBoundsWidth = Application.Current.RootVisual.RenderSize.Width;
        message.WindowBoundsHeight = Application.Current.RootVisual.RenderSize.Height;
        PhoneApplicationFrame frame = Application.Current.RootVisual as PhoneApplicationFrame;
        if (frame != null)
        {
          message.CurrentOrientation = frame.Orientation.ToString();
        }
      }

      try
      {
        message.ApplicationCurrentMemoryUsage = DeviceStatus.ApplicationCurrentMemoryUsage;
        message.ApplicationMemoryUsageLimit = DeviceStatus.ApplicationMemoryUsageLimit;
        message.ApplicationPeakMemoryUsage = DeviceStatus.ApplicationPeakMemoryUsage;
        message.DeviceTotalMemory = DeviceStatus.DeviceTotalMemory;
      }
      catch (Exception e)
      {
        Debug.WriteLine("Faild to get device memory information: {0}", e.Message);
      }

      try
      {
        message.DeviceFirmwareVersion = DeviceStatus.DeviceFirmwareVersion;
        message.DeviceHardwareVersion = DeviceStatus.DeviceHardwareVersion;
        message.DeviceManufacturer = DeviceStatus.DeviceManufacturer;
      }
      catch (Exception e)
      {
        Debug.WriteLine("Failed to get device information: {0}", e.Message);
      }

      try
      {
        message.IsolatedStorageAvailableFreeSpace = IsolatedStorageFile.GetUserStoreForApplication().AvailableFreeSpace;
      }
      catch (Exception e)
      {
        Debug.WriteLine("Failed to get isolated storage memory: {0}", e.Message);
      }

      return message;
    }
  }
}
