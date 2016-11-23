using Mindscape.Raygun4Net.Messages;
using System;
using System.Diagnostics;
using System.Reflection;
using Windows.ApplicationModel;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunEnvironmentMessageBuilder
  {
    public static RaygunEnvironmentMessage Build()
    {
      RaygunEnvironmentMessage message = new RaygunEnvironmentMessage();

      try
      {
        if (Windows.UI.Xaml.Window.Current != null)
        {
          message.WindowBoundsHeight = Windows.UI.Xaml.Window.Current.Bounds.Height;
          message.WindowBoundsWidth = Windows.UI.Xaml.Window.Current.Bounds.Width;
        }
        message.ResolutionScale = DisplayProperties.ResolutionScale.ToString();
        message.CurrentOrientation = DisplayProperties.CurrentOrientation.ToString();
        message.ViewState = ApplicationView.Value.ToString();
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error retrieving window info: {0}", ex.Message);
      }

      try
      {
        DateTime now = DateTime.Now;
        message.UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;
        message.Locale = Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion;
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error retrieving time and locale: {0}", ex.Message);
      }

      try
      {
        message.Cpu = Package.Current.Id.Architecture.ToString();
        SYSTEM_INFO systemInfo = new SYSTEM_INFO();
        RaygunSystemInfoWrapper.GetNativeSystemInfo(ref systemInfo);
        message.Architecture = ((PROCESSOR_ARCHITECTURE)systemInfo.wProcessorArchitecture).ToString();
        message.ProcessorCount = (int)systemInfo.dwNumberOfProcessors;
        message.OSVersion = GetOSVersion();
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
                return string.Empty;
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