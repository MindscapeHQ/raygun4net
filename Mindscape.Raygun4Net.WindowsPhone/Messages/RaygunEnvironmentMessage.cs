namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    public string OSVersion { get; set; }

    public double WindowBoundsWidth { get; set; }

    public double WindowBoundsHeight { get; set; }

    public string CurrentOrientation { get; set; }

    public long IsolatedStorageAvailableFreeSpace { get; set; }

    public long ApplicationCurrentMemoryUsage { get; set; }

    public long ApplicationMemoryUsageLimit { get; set; }

    public long ApplicationPeakMemoryUsage { get; set; }

    public long DeviceTotalMemory { get; set; }

    public string DeviceFirmwareVersion { get; set; }

    public string DeviceHardwareVersion { get; set; }

    public string DeviceManufacturer { get; set; }

    public double UtcOffset { get; set; }

    public string Locale { get; set; }
  }
}