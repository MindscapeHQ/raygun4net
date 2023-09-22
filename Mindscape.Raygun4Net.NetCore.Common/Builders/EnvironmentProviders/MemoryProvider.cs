#nullable enable

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Mindscape.Raygun4Net.EnvironmentProviders
{
  internal static class MemoryProvider
  {
    public static (ulong AvailableMemory, ulong TotalMemory, ulong AvailableVirtualMemory, ulong TotalVirtualMemory)? GetTotalMemory()
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

    private static (ulong AvailableMemory, ulong TotalMemory, ulong AvailableVirtualMemory, ulong TotalVirtualMemory)?
      GetOnWindows()
    {
        var memStatus = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(memStatus))
        {
          return (memStatus.ullAvailPhys, memStatus.ullTotalPhys, memStatus.ullAvailVirtual, memStatus.ullTotalVirtual);
        }

        return null;
    }

    private static (ulong AvailableMemory, ulong TotalMemory, ulong AvailableVirtualMemory, ulong TotalVirtualMemory)? GetOnLinux()
    {

        ulong memTotal = 0, memFree = 0, memAvailable = 0;
        var memInfoLines = File.ReadAllLines("/proc/meminfo");

        foreach (var line in memInfoLines)
        {
          if (line.StartsWith("MemTotal:"))
          {
            memTotal = ulong.Parse(line.Split(':')[1].Trim().Split(' ')[0]);
          }

          if (line.StartsWith("MemFree:"))
          {
            memFree = ulong.Parse(line.Split(':')[1].Trim().Split(' ')[0]);
          }

          if (line.StartsWith("MemAvailable:"))
          {
            memAvailable = ulong.Parse(line.Split(':')[1].Trim().Split(' ')[0]);
          }
        }

        // Convert from kB to Bytes
        memTotal *= 1024;
        memFree *= 1024;
        memAvailable *= 1024;

        return (memAvailable, memTotal, memFree, memTotal);
    }

    private static (ulong AvailableMemory, ulong TotalMemory, ulong AvailableVirtualMemory, ulong TotalVirtualMemory)? GetOnMacOS()
    {
        ulong totalMem = GetSysctlUlongValue("hw.memsize");

        // Available memory is not straightforward to get accurately on macOS,
        // we'll be using "Pages free:" from 'vm_stat' command as a hypothetical example
        var vmstatInfo = GetCommandOutput("vm_stat | grep 'Pages free'");
        var pagesFree = ulong.Parse(vmstatInfo.Split(':')[1].Trim().Split('.')[0]);
        ulong availableMem = pagesFree * 4096; // assuming standard page size of 4096

        // Getting virtual memory info
        var swapUsageInfo = GetCommandOutput("sysctl -n vm.swapusage");
        var match = Regex.Match(swapUsageInfo, @"total = (\d+.\d+)M");
        double totalVirtualMemInMb = double.Parse(match.Groups[1].Value);
        ulong totalVirtualMem = (ulong)(totalVirtualMemInMb * 1024 * 1024); // Converting MB to Bytes

        return (availableMem, totalMem, availableMem, totalVirtualMem);
    }

    private static ulong GetSysctlUlongValue(string name)
    {
      var output = GetCommandOutput($"sysctl -n {name}");
      return ulong.Parse(output.Trim());
    }

    private static string GetCommandOutput(string command)
    {
      ProcessStartInfo psi = new ProcessStartInfo("bash", "-c \"" + command + "\"")
      {
        RedirectStandardOutput = true,
        UseShellExecute = false,
        RedirectStandardError = true,
        CreateNoWindow = true
      };

      Process p = Process.Start(psi);
      string output = p.StandardOutput.ReadToEnd();
      p.WaitForExit();
      return output;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
      public uint dwLength;
      public uint dwMemoryLoad;
      public ulong ullTotalPhys;
      public ulong ullAvailPhys;
      public ulong ullTotalPageFile;
      public ulong ullAvailPageFile;
      public ulong ullTotalVirtual;
      public ulong ullAvailVirtual;
      public ulong ullAvailExtendedVirtual;

      public MEMORYSTATUSEX()
      {
        dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
      }
    }

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
  }
}