using Mindscape.Raygun4Net.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Xaml;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunEnvironmentMessageBuilder
  {
    public static RaygunEnvironmentMessage Build()
    {
      RaygunEnvironmentMessage message = new RaygunEnvironmentMessage();

      try
      {
        if (Window.Current != null)
        {
          message.WindowBoundsWidth = Window.Current.Bounds.Width;
          message.WindowBoundsHeight = Window.Current.Bounds.Height;

          var sensor = Windows.Devices.Sensors.SimpleOrientationSensor.GetDefault();

          if (sensor != null)
          {
            message.CurrentOrientation = sensor.GetCurrentOrientation().ToString();
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
        var deviceInfo = new EasClientDeviceInformation();
        message.DeviceManufacturer = deviceInfo.SystemManufacturer;
        message.DeviceName = deviceInfo.SystemProductName;
        message.OSVersion = deviceInfo.OperatingSystem;
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error retrieving device info: {0}", ex.Message);
      }

      return message;
    }
  }
}
