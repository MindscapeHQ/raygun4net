using Mindscape.Raygun4Net.Messages;
using System;
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

      if (Windows.UI.Xaml.Window.Current != null)
      {
        message.WindowBoundsHeight = Windows.UI.Xaml.Window.Current.Bounds.Height;
        message.WindowBoundsWidth = Windows.UI.Xaml.Window.Current.Bounds.Width;
      }
      message.PackageVersion = string.Format("{0}.{1}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor);
      message.Cpu = Package.Current.Id.Architecture.ToString();
      message.ResolutionScale = DisplayProperties.ResolutionScale.ToString();
      message.CurrentOrientation = DisplayProperties.CurrentOrientation.ToString();
      message.Locale = Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion;

      DateTime now = DateTime.Now;
      message.UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;

      SYSTEM_INFO systemInfo = new SYSTEM_INFO();
      RaygunSystemInfoWrapper.GetNativeSystemInfo(ref systemInfo);
      message.Architecture = ((PROCESSOR_ARCHITECTURE)systemInfo.wProcessorArchitecture).ToString();
      message.ProcessorCount = (int)systemInfo.dwNumberOfProcessors;
      message.ViewState = ApplicationView.Value.ToString();

      return message;
    }
  }
}
