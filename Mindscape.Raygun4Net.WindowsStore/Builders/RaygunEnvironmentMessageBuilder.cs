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

      message.Locale = CultureInfo.CurrentCulture.DisplayName;

      DateTime now = DateTime.Now;
      message.UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;

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

      var deviceInfo = new EasClientDeviceInformation();

      try
      {
        message.DeviceManufacturer = deviceInfo.SystemManufacturer;
        message.DeviceName = deviceInfo.SystemProductName;
        message.OSVersion = deviceInfo.OperatingSystem;
      }
      catch (Exception e)
      {
        Debug.WriteLine("Failed to get device information: {0}", e.Message);
      }

      return message;
    }
  }
}
