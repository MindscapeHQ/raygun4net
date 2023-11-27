#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Mindscape.Raygun4Net.EnvironmentProviders
{
  internal static class ProcessorProvider
  {
    public static string? GetCpuName()
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

    private static string? GetOnWindows()
    {
      return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
    }

    private static string? GetOnLinux()
    {
      string? result = null;

      var cpuInfoPath = "/proc/cpuinfo";

      if (File.Exists(cpuInfoPath))
      {
        var cpuInfo = File.ReadAllText(cpuInfoPath);
        var cpuInfoLines = cpuInfo.Split('\n');

        foreach (var line in cpuInfoLines)
        {
          if (line.StartsWith("model name"))
          {
            result = line.Split(':')[1].Trim();
            break; // You can remove this if you want information for all CPUs in a multi-CPU system.
          }
        }
      }

      return result;
    }

    private static string? GetOnMacOS()
    {
      string? result = null;

      using var process = new Process();

      process.StartInfo = new ProcessStartInfo
      {
        FileName = "/bin/bash",
        Arguments = "-c \"sysctl -n machdep.cpu.brand_string\"",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true,
      };

      process.Start();

      var output = process.StandardOutput.ReadToEnd();

      process.WaitForExit();

      if (!string.IsNullOrEmpty(output))
      {
        result = output.Trim();
      }

      return result;
    }
  }
}