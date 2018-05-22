using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Mindscape.Raygun4Net
{
  internal class RaygunEnvironmentMessageBuilder
  {
    private readonly RaygunEnvironmentMessage _message;
    
    public static RaygunEnvironmentMessageBuilder New()
    {
      return new RaygunEnvironmentMessageBuilder();
    }

    public RaygunEnvironmentMessageBuilder()
    {
      _message = new RaygunEnvironmentMessage();
    }
    
    public RaygunEnvironmentMessage Build(RaygunSettings settings)
    {
      // The cross platform APIs for getting this information don't exist right now.

      try
      {
        DateTime now = DateTime.Now;
        _message.UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;
        _message.Locale = CultureInfo.CurrentCulture.DisplayName;
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Failed to capture time locale {ex.Message}");
      }

      return _message;
    }
  }
}