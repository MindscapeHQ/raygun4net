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

      try
      {
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
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error retrieving screen info: {0}", ex.Message);
      }

      try
      {
        DateTime now = DateTime.Now;
        message.UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;
        message.Locale = CultureInfo.CurrentCulture.DisplayName;
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error retrieving time and locale: {0}", ex.Message);
      }

      try
      {
        message.OSVersion = Environment.OSVersion.Platform + " " + Environment.OSVersion.Version;
        message.DeviceFirmwareVersion = DeviceStatus.DeviceFirmwareVersion;
        message.DeviceHardwareVersion = DeviceStatus.DeviceHardwareVersion;
        message.DeviceManufacturer = DeviceStatus.DeviceManufacturer;
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error retrieving device info: {0}", ex.Message);
      }

      try
      {
        message.ApplicationCurrentMemoryUsage = DeviceStatus.ApplicationCurrentMemoryUsage;
        message.ApplicationMemoryUsageLimit = DeviceStatus.ApplicationMemoryUsageLimit;
        message.ApplicationPeakMemoryUsage = DeviceStatus.ApplicationPeakMemoryUsage;
        message.DeviceTotalMemory = DeviceStatus.DeviceTotalMemory;
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error retrieving device memory: {0}", ex.Message);
      }

      try
      {
        message.IsolatedStorageAvailableFreeSpace = IsolatedStorageFile.GetUserStoreForApplication().AvailableFreeSpace;
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error retrieving isolated storage memory: {0}", ex.Message);
      }

      return message;
    }
  }
}
