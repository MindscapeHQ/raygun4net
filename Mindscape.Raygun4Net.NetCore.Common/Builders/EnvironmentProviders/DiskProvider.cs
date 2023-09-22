using System;
using System.Collections.Generic;
using System.Diagnostics;
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
      var diskSpace = new Dictionary<string, double>();

      var procStartInfo = new ProcessStartInfo("df", "-x tmpfs -x devtmpfs -x squashfs --output=source,size")
      {
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      var proc = new Process { StartInfo = procStartInfo };
      proc.Start();

      // Skip the header line
      proc.StandardOutput.ReadLine();

      while (!proc.StandardOutput.EndOfStream)
      {
        var line = proc.StandardOutput.ReadLine();
        if (line != null)
        {
          var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
          var mountPoint = parts[0];
          var size = ulong.Parse(parts[1]) * 1024ul; // df outputs size in KB, so convert to bytes
          diskSpace[mountPoint] = size;
        }
      }

      return diskSpace.Select(x => x.Value).ToList();
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