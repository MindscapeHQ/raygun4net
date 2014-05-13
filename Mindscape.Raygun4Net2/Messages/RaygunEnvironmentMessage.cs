using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    public RaygunEnvironmentMessage()
    {
      try
      {
        DateTime now = DateTime.Now;
        UtcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(now).TotalHours;

        Locale = CultureInfo.CurrentCulture.DisplayName;

        DeviceName = "Unknown";
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("Error getting environment info {0}", ex.Message));
      }
    }

    public int ProcessorCount { get; private set; }

    public string OSVersion { get; private set; }

    public double WindowBoundsWidth { get; private set; }

    public double WindowBoundsHeight { get; private set; }

    public string ResolutionScale { get; private set; }

    public string Architecture { get; private set; }

    public string Model { get; private set; }

    public ulong TotalPhysicalMemory { get; private set; }

    public ulong AvailablePhysicalMemory { get; private set; }

    public string DeviceName { get; private set; }

    public double UtcOffset { get; private set; }

    public string Locale { get; private set; }
  }
}
