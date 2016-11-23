using Mindscape.Raygun4Net.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
        message.OSVersion = GetOSVersion() ?? deviceInfo.OperatingSystem;
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error retrieving device info: {0}", ex.Message);
      }

      return message;
    }

    private static string GetOSVersion()
    {
        try
        {
            var analyticsInfoType = Type.GetType("Windows.System.Profile.AnalyticsInfo, Windows, ContentType=WindowsRuntime");
            var versionInfoType = Type.GetType("Windows.System.Profile.AnalyticsVersionInfo, Windows, ContentType=WindowsRuntime");

            if (analyticsInfoType == null || versionInfoType == null)
            {
                return null;
            }

            var versionInfoProperty = analyticsInfoType.GetRuntimeProperty("VersionInfo");
            var versionInfo = versionInfoProperty.GetValue(null);
            var versionProperty = versionInfoType.GetRuntimeProperty("DeviceFamilyVersion");
            var familyVersion = versionProperty.GetValue(versionInfo);

            long versionBytes;
            if (!long.TryParse(familyVersion.ToString(), out versionBytes))
            {
                return string.Empty;
            }

            var uapVersion = new Version((ushort) (versionBytes >> 48),
                (ushort) (versionBytes >> 32),
                (ushort) (versionBytes >> 16),
                (ushort) (versionBytes));

            return uapVersion.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }
  }
}
