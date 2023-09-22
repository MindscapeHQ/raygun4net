#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Mindscape.Raygun4Net.EnvironmentProviders
{
  internal static class ScreenProvider
  {
    public static (int Height, int Width)? GetPrimaryScreenResolution()
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

      return null;
    }

    private static (int Height, int Width)? GetOnWindows()
    {
      var screenWidth = GetSystemMetrics(SystemMetric.SM_CXSCREEN);
      var screenHeight = GetSystemMetrics(SystemMetric.SM_CYSCREEN);

      return (screenWidth, screenHeight);
    }

    private static (int Height, int Width)? GetOnLinux()
    {
      using var process = new Process();
      
      process.StartInfo = new ProcessStartInfo
      {
        FileName = "/bin/bash",
        Arguments = "-c \"xrandr | grep '*'\"",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      var output = process.StandardOutput.ReadToEnd();
      
      process.WaitForExit();

      var resolution = output.Split(' ').ToList().First(x => !string.IsNullOrEmpty(x));
      var dimensions = resolution.Split('x');

      return (int.Parse(dimensions[0]), int.Parse(dimensions[1]));
    }

    private static (int Height, int Width)? GetOnMacOS()
    {
      using var process = new Process();

      process.StartInfo = new ProcessStartInfo
      {
        FileName = "/bin/bash",
        Arguments = "-c \"system_profiler SPDisplaysDataType | awk '/Resolution/{print $2, $4}' | head -n 1\"",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true,
      };

      process.Start();

      var output = process.StandardOutput.ReadToEnd();

      process.WaitForExit();

      if (!string.IsNullOrEmpty(output))
      {
        var dimensions = output.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (dimensions.Length == 2)
        {
          return (int.Parse(dimensions[0]), int.Parse(dimensions[1]));
        }
      }

      return null;
    }

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(SystemMetric smIndex);

    enum SystemMetric
    {
      SM_CXSCREEN = 0,
      SM_CYSCREEN = 1,
    }
  }
}