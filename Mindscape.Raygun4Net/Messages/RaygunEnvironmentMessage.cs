using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Management;

namespace Mindscape.Raygun4Net.Messages
{

  public class RaygunEnvironmentMessage
  {
      [StructLayout(LayoutKind.Sequential)]
      internal struct MEMORYSTATUSEX
      {
          internal uint dwLength;
          internal uint dwMemoryLoad;
          internal ulong ullTotalPhys;
          internal ulong ullAvailPhys;
          internal ulong ullTotalPageFile;
          internal ulong ullAvailPageFile;
          internal ulong ullTotalVirtual;
          internal ulong ullAvailVirtual;
          internal ulong ullAvailExtendedVirtual;
      }

      [return: MarshalAs(UnmanagedType.Bool)]
      [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      internal static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);



      private List<double> _diskSpaceFree = new List<double>();

    public RaygunEnvironmentMessage()
    {
      WindowBoundsWidth = 0;
      WindowBoundsHeight = 0;

      Locale = CultureInfo.CurrentCulture.DisplayName;

      DateTime now = DateTime.Now;
      UtcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(now).TotalHours;

        var statex = new MEMORYSTATUSEX();
        GlobalMemoryStatusEx(statex);

      OSVersion = Environment.OSVersion.VersionString;

        try
        {
            ProcessorCount = Environment.ProcessorCount;
            Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            TotalPhysicalMemory = (ulong)statex.ullTotalPhys / 0x100000; // in MB
            AvailablePhysicalMemory = (ulong)statex.ullAvailPhys / 0x100000;
            TotalVirtualMemory = statex.ullTotalVirtual / 0x100000;
            AvailableVirtualMemory = statex.ullAvailVirtual / 0x100000;
            GetDiskSpace();
            Cpu = GetCpu();
            OSVersion = GetOSVersion();
        }
        catch (SecurityException)
        {
            System.Diagnostics.Trace.WriteLine("RaygunClient error: couldn't access environment variables. If you are running in Medium Trust, in web.config in RaygunSettings set mediumtrust=\"true\"");
        }
    }

    private static string GetCpu()
    {
      ManagementClass wmiManagementProcessorClass = new ManagementClass("Win32_Processor");
      ManagementObjectCollection wmiProcessorCollection = wmiManagementProcessorClass.GetInstances();

      foreach (ManagementObject wmiProcessorObject in wmiProcessorCollection)
      {
        try
        {
          var name = wmiProcessorObject.Properties["Name"].Value.ToString();
          return name;
        }
        catch (ManagementException)
        {
        }
      }
      return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
    }

    private static string GetOSVersion()
    {
      ManagementClass wmiManagementOperatingSystemClass = new ManagementClass("Win32_OperatingSystem");
      ManagementObjectCollection wmiOperatingSystemCollection = wmiManagementOperatingSystemClass.GetInstances();

      foreach (ManagementObject wmiOperatingSystemObject in wmiOperatingSystemCollection)
      {
        try
        {
          var version = wmiOperatingSystemObject.Properties["Version"].Value.ToString();
          return version;
        }
        catch (ManagementException)
        {
        }
      }
      return Environment.OSVersion.Version.ToString(3);
    }

    private void GetDiskSpace()
    {
      foreach (DriveInfo drive in DriveInfo.GetDrives())
      {
        if (drive.IsReady)
        {
          DiskSpaceFree.Add((double)drive.AvailableFreeSpace / 0x40000000); // in GB
        }
      }
    }

    private static readonly object _threadLock = new object();

    public int ProcessorCount { get; private set; }

    public string OSVersion { get; private set; }

    public double WindowBoundsWidth { get; private set; }

    public double WindowBoundsHeight { get; private set; }

    public string ResolutionScale { get; private set; }

    public string CurrentOrientation { get; private set; }

    public string Cpu { get; private set; }

    public string PackageVersion { get; private set; }

    public string Architecture { get; private set; }

    public ulong TotalVirtualMemory { get; private set; }

    public ulong AvailableVirtualMemory { get; private set; }

    public List<double> DiskSpaceFree
    {
      get { return _diskSpaceFree; }
      set { _diskSpaceFree = value; }
    }

    public ulong TotalPhysicalMemory { get; private set; }

    public ulong AvailablePhysicalMemory { get; private set; }

    public string DeviceName { get; private set; }

    public double UtcOffset { get; private set; }

    public string Locale { get; private set; }
  }
}
