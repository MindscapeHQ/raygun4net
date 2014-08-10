using System.Collections.Generic;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    public int ProcessorCount { get; set; }

    public string OSVersion { get; set; }

    public double WindowBoundsWidth { get; set; }

    public double WindowBoundsHeight { get; set; }

    public string ResolutionScale { get; set; }

    public string CurrentOrientation { get; set; }

    public string Cpu { get; set; }

    public string PackageVersion { get; set; }

    public string Architecture { get; set; }

    public ulong TotalVirtualMemory { get; set; }

    public ulong AvailableVirtualMemory { get; set; }

    public IEnumerable<double> DiskSpaceFree { get; set; }

    public ulong TotalPhysicalMemory { get; set; }

    public ulong AvailablePhysicalMemory { get; set; }

    public string DeviceName { get; set; }

    public double UtcOffset { get; set; }

    public string Locale { get; set; }
  }
}
