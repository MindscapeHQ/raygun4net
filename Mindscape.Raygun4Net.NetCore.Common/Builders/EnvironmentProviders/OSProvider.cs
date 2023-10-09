#nullable enable
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Mindscape.Raygun4Net.EnvironmentProviders
{
  internal static class OSProvider
  {
    public static string GetOSInformation()
    {
      try
      {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
          return GetForWindows() ?? RuntimeInformation.OSDescription;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
          return GetForLinux() ?? RuntimeInformation.OSDescription;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
          return GetForMacOS() ?? RuntimeInformation.OSDescription;
        }
      }
      catch
      {
        // Ignore
      }

      return RuntimeInformation.OSDescription;
    }

    private static string? GetForWindows()
    {
      string? productName = null;

      var registryKey =
        Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion");
      var result = registryKey?.GetValue("productName") as string;

      if (result != null)
      {
        var osVersion = Environment.OSVersion.VersionString;
        productName = $"{result} ({osVersion})";
      }

      return productName;
    }

    private static string? GetForLinux()
    {
      using var process = new Process();

      process.StartInfo = new ProcessStartInfo
      {
        FileName = "/bin/bash",
        Arguments = "-c \". /etc/os-release && echo ${PRETTY_NAME//\\\\\\\"/}\"",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true,
      };

      process.Start();

      var result = process.StandardOutput.ReadToEnd().Trim();

      process.WaitForExit();

      return result;
    }

    private static string? GetForMacOS()
    {
      string? result = null;

      using var process = new Process();

      process.StartInfo = new ProcessStartInfo
      {
        FileName = "/bin/bash",
        Arguments = "-c \"sw_vers -productName && sw_vers -productVersion\"",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true,
      };

      process.Start();

      var output = process.StandardOutput.ReadToEnd();

      process.WaitForExit();

      if (!string.IsNullOrEmpty(output))
      {
        // Combine the product name and its version
        result = string.Join(" ", output.Split('\n').Where(s => !string.IsNullOrEmpty(s)).Select(s => s.Trim()));
      }

      return result;
    }
  }
}