using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using Mindscape.Raygun4Net.Logging;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunEnvironmentMessageBuilder
  {
    private static RaygunEnvironmentMessage _message;

    public static RaygunEnvironmentMessage Build()
    {
      bool mediumTrust = RaygunSettings.Settings.MediumTrust || !HasUnrestrictedFeatureSet;

      //
      // Gather the environment information that only needs to be collected once
      //

      if (_message == null)
      {
        _message = new RaygunEnvironmentMessage();

        // Different environments can fail to load the environment details.
        // For now if they fail to load for whatever reason then just
        // swallow the exception. A good addition would be to handle
        // these cases and load them correctly depending on where its running.
        // see http://raygun.com/forums/thread/3655

        try
        {
          // TODO: This requires a reference to System.Windows.Forms, we should find a better solution.
          // _message.WindowBoundsWidth = SystemInformation.VirtualScreen.Width;
          // _message.WindowBoundsHeight = SystemInformation.VirtualScreen.Height;
        }
        catch (Exception ex)
        {
          RaygunLogger.Instance.Error($"Error retrieving window dimensions: {ex.Message}");
        }

        try
        {
          DateTime now = DateTime.Now;
          _message.UtcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(now).TotalHours;
          _message.Locale = CultureInfo.CurrentCulture.DisplayName;
        }
        catch (Exception ex)
        {
          RaygunLogger.Instance.Error($"Error retrieving time and locale: {ex.Message}");
        }

        try
        {
          if (!mediumTrust)
          {
            // ProcessorCount cannot run in medium trust under net35, once we have
            // moved to net40 minimum we can move this out of here
            _message.ProcessorCount = Environment.ProcessorCount;
            _message.Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");

            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
              _message.TotalPhysicalMemory = memStatus.ullTotalPhys;
              _message.TotalVirtualMemory = memStatus.ullTotalVirtual;
            }

            _message.Cpu = GetCpu();
            _message.OSVersion = GetOSVersion();
          }
        }
        catch (SecurityException)
        {
          RaygunLogger.Instance.Error("RaygunClient error: couldn't access environment variables. If you are running in Medium Trust, in web.config in RaygunSettings set mediumtrust=\"true\"");
        }
        catch (Exception ex)
        {
          RaygunLogger.Instance.Error($"Error retrieving environment info: {ex.Message}");
        }
      }

      // 
      // Gather the environment info that must be collected at the time of a report being generated.
      // 

      try
      {
        if (!mediumTrust)
        {
          var memStatus = new MEMORYSTATUSEX();
          if (GlobalMemoryStatusEx(memStatus))
          {
            _message.AvailablePhysicalMemory = memStatus.ullAvailPhys;
            _message.AvailableVirtualMemory = memStatus.ullTotalPageFile;
          }

          _message.DiskSpaceFree = GetDiskSpace();
        }
      }
      catch (SecurityException)
      {
        RaygunLogger.Instance.Error("RaygunClient error: couldn't access environment variables. If you are running in Medium Trust, in web.config in RaygunSettings set mediumtrust=\"true\"");
      }
      catch (Exception ex)
      {
        RaygunLogger.Instance.Error($"Error retrieving environment info: {ex.Message}");
      }

      return _message;
    }

    private static string GetCpu()
    {
      ManagementObjectSearcher wmiProcessorSearcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
      ManagementObjectCollection wmiProcessorCollection = wmiProcessorSearcher.Get();

      foreach (ManagementObject wmiProcessorObject in wmiProcessorCollection)
      {
        try
        {
          var name = wmiProcessorObject["Name"].ToString();
          return name;
        }
        catch (ManagementException ex)
        {
          RaygunLogger.Instance.Error($"Error retrieving CPU {ex.Message}");
        }
      }

      return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
    }

    private static string GetOSVersion()
    {
      ManagementObjectSearcher wmiOperatingSystemSearcher = new ManagementObjectSearcher("SELECT Version FROM Win32_OperatingSystem");
      ManagementObjectCollection wmiOperatingSystemCollection = wmiOperatingSystemSearcher.Get();

      foreach (ManagementObject wmiOperatingSystemObject in wmiOperatingSystemCollection)
      {
        try
        {
          var version = wmiOperatingSystemObject["Version"].ToString();
          return version;
        }
        catch (ManagementException ex)
        {
          RaygunLogger.Instance.Error($"Error retrieving OSVersion {ex.Message}");
        }
      }

      return Environment.OSVersion.Version.ToString(3);
    }

    private static List<double> GetDiskSpace()
    {
      List<double> diskSpaceFree = new List<double>();
      foreach (DriveInfo drive in DriveInfo.GetDrives())
      {
        if (drive.IsReady)
        {
          diskSpaceFree.Add((double)drive.AvailableFreeSpace / 0x40000000); // in GB
        }
      }

      return diskSpaceFree;
    }

    private static volatile bool _unrestrictedFeatureSet = false;
    private static volatile bool _determinedUnrestrictedFeatureSet = false;
    private static readonly object _threadLock = new object();

    private static bool HasUnrestrictedFeatureSet
    {
      get
      {
        if (!_determinedUnrestrictedFeatureSet)
        {
          lock (_threadLock)
          {
            if (!_determinedUnrestrictedFeatureSet)
            {
              // This seems to crash if not in full trust:
              //_unrestrictedFeatureSet = AppDomain.CurrentDomain.ApplicationTrust == null;// || AppDomain.CurrentDomain.ApplicationTrust.DefaultGrantSet.PermissionSet.IsUnrestricted();
              try
              {
                // See if we're running in full trust:
                new PermissionSet(PermissionState.Unrestricted).Demand();
                _unrestrictedFeatureSet = true;
              }
              catch (SecurityException)
              {
                _unrestrictedFeatureSet = false;
              }

              _determinedUnrestrictedFeatureSet = true;
            }
          }
        }

        return _unrestrictedFeatureSet;
      }
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