using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Mindscape.Raygun4Net.EnvironmentProviders
{
  internal static class DiskProvider
  {
    public static List<double> GetDiskSpace()
    {
      try
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

      }
      catch
      {
        // Ignore
      }

      return new List<double>();
    }

    private static List<double> GetOnWindows()
    {
      return DriveInfo.GetDrives()
        .Where(x => x.IsReady && x.DriveType == DriveType.Fixed)
        .Select(d => (double)d.AvailableFreeSpace)
        .ToList();
    }

    private static List<double> GetOnLinux()
    {
      return DriveInfo.GetDrives()
        .Where(x => x is { IsReady: true, DriveType: DriveType.Fixed, Name: "/" })
        .Select(d => (double)d.AvailableFreeSpace)
        .ToList();
    }

    private static List<double> GetOnMacOS()
    {
      return DriveInfo.GetDrives()
        .Where(x => x is { IsReady: true, DriveType: DriveType.Fixed, Name: "/" })
        .Select(d => (double)d.AvailableFreeSpace)
        .ToList();
    }
  }
}