using System;
using System.Globalization;

#if NET451
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;
#endif

using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Builders
{
  internal class RaygunEnvironmentMessageBuilder
  {
    public static RaygunEnvironmentMessage Build(RaygunSettings settings)
    {
      RaygunEnvironmentMessage message = new RaygunEnvironmentMessage();

      try
      {
        DateTime now = DateTime.Now;
        message.UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;
        message.Locale = CultureInfo.CurrentCulture.DisplayName;
      }
      catch { }

      // The cross platform APIs for getting this information don't exist right now.
      // In the mean time, chuck conditionals around the whole thing.

#if NET451
      // Different environments can fail to load the environment details.
      // For now if they fail to load for whatever reason then just
      // swallow the exception. A good addition would be to handle
      // these cases and load them correctly depending on where its running.
      // see http://raygun.io/forums/thread/3655

      try
      {
        message.WindowBoundsWidth = SystemInformation.VirtualScreen.Width;
        message.WindowBoundsHeight = SystemInformation.VirtualScreen.Height;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine("Error retrieving window dimensions: {0}", ex.Message);
      }
      
      try
      {
        ComputerInfo info = new ComputerInfo();
        message.OSVersion = info.OSVersion;

        bool mediumTrust = settings.MediumTrust || !HasUnrestrictedFeatureSet;

        if (!mediumTrust)
        {
          // ProcessorCount cannot run in medium trust under net35, once we have
          // moved to net40 minimum we can move this out of here
          message.ProcessorCount = Environment.ProcessorCount;
          message.Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
          message.TotalPhysicalMemory = (ulong)info.TotalPhysicalMemory / 0x100000; // in MB
          message.AvailablePhysicalMemory = (ulong)info.AvailablePhysicalMemory / 0x100000;
          message.TotalVirtualMemory = info.TotalVirtualMemory / 0x100000;
          message.AvailableVirtualMemory = info.AvailableVirtualMemory / 0x100000;
          message.DiskSpaceFree = GetDiskSpace();
          message.Cpu = GetCpu();
          message.OSVersion = GetOSVersion();
        }
      }
      catch (SecurityException)
      {
        System.Diagnostics.Trace.WriteLine("RaygunClient error: couldn't access environment variables. If you are running in Medium Trust, in web.config in RaygunSettings set mediumtrust=\"true\"");
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine("Error retrieving environment info: {0}", ex.Message);
      }
#endif
      return message;
    }

#if NET451
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
          System.Diagnostics.Trace.WriteLine("Error retrieving CPU {0}", ex.Message);
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
          System.Diagnostics.Trace.WriteLine("Error retrieving OSVersion {0}", ex.Message);
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
#endif
  }
}