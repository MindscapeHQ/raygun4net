using Mindscape.Raygun4Net.Messages;
using System;
using System.Diagnostics;
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
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error retrieving device info: {0}", ex.Message);
      }

      return message;
    }
  }
}
