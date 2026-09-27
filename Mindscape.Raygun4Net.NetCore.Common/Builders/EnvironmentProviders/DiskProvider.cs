using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Mindscape.Raygun4Net.EnvironmentProviders
{
  internal static class DiskProvider
  {
    // Errors aren't caught here: RaygunEnvironmentMessageBuilder catches them and marks the report's disk space as Error
    public static List<double> GetDiskSpace()
    {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        return GetOnWindows();
      }

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      {
        return GetOnLinux();
      }

      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        return GetOnMacOS();
      }

      return new List<double>();
    }

    // Filters run cheapest-first with && so drives we don't report are never probed: IsReady (and on Unix, DriveType)
    // touch the drive and can block for a long time on an unreachable network drive or mount.
    private static List<double> GetOnWindows()
    {
      return DriveInfo.GetDrives()
        .Where(x => x.DriveType == DriveType.Fixed && x.IsReady)
        .Select(d => (double)d.AvailableFreeSpace)
        .ToList();
    }

    private static List<double> GetOnLinux()
    {
      return DriveInfo.GetDrives()
        .Where(x => x.Name == "/" && x.DriveType == DriveType.Fixed && x.IsReady)
        .Select(d => (double)d.AvailableFreeSpace)
        .ToList();
    }

    private static List<double> GetOnMacOS()
    {
      return DriveInfo.GetDrives()
        .Where(x => x.Name == "/" && x.DriveType == DriveType.Fixed && x.IsReady)
        .Select(d => (double)d.AvailableFreeSpace)
        .ToList();
    }
  }
}