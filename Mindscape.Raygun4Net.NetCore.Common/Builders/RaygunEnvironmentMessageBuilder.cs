using System;
using System.Diagnostics;
using System.Globalization;

namespace Mindscape.Raygun4Net
{
  public class RaygunEnvironmentMessageBuilder
  {
    public static RaygunEnvironmentMessage Build(RaygunSettingsBase settings)
    {
      RaygunEnvironmentMessage message = new RaygunEnvironmentMessage();
      
      // The cross platform APIs for getting this information don't exist right now.

      try
      {
        DateTime now = DateTime.Now;
        message.UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;
        message.Locale = CultureInfo.CurrentCulture.DisplayName;
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Failed to capture time locale {ex.Message}");
      }

      return message;
    }
  }
}