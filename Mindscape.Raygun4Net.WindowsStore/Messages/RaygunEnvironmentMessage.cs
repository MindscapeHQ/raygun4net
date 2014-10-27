namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    public string OSVersion { get; set; }

    public double WindowBoundsWidth { get; set; }

    public double WindowBoundsHeight { get; set; }

    public string CurrentOrientation { get; set; }

    public string DeviceManufacturer { get; set; }

    public string DeviceName { get; set; }

    public double UtcOffset { get; set; }

    public string Locale { get; set; }
  }
}