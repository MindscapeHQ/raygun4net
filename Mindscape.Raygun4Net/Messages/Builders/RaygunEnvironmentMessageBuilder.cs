using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

using System.Windows.Forms;
using System.Management;
using Microsoft.VisualBasic.Devices;
using System.Security.Permissions;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Messages.Builders
{
  public class RaygunEnvironmentMessageBuilder
  {
    public RaygunEnvironmentMessage Build()
    {
      var rayrunEnvironmentMessage = new RaygunEnvironmentMessage();

      rayrunEnvironmentMessage.WindowBoundsWidth = SystemInformation.VirtualScreen.Width;
      rayrunEnvironmentMessage.WindowBoundsHeight = SystemInformation.VirtualScreen.Height;
      ComputerInfo info = new ComputerInfo();
      rayrunEnvironmentMessage.Locale = CultureInfo.CurrentCulture.DisplayName;

      DateTime now = DateTime.Now;
      rayrunEnvironmentMessage.UtcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(now).TotalHours;

      rayrunEnvironmentMessage.OSVersion = info.OSVersion;

      bool mediumTrust = RaygunSettings.Settings.MediumTrust || !HasUnrestrictedFeatureSet;

      if (!mediumTrust)
      {
        try
        {
          // ProcessorCount cannot run in medium trust under net35, once we have
          // moved to net40 minimum we can move this out of here
          rayrunEnvironmentMessage.ProcessorCount = Environment.ProcessorCount;
          rayrunEnvironmentMessage.Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
          rayrunEnvironmentMessage.TotalPhysicalMemory = (ulong)info.TotalPhysicalMemory / 0x100000; // in MB
          rayrunEnvironmentMessage.AvailablePhysicalMemory = (ulong)info.AvailablePhysicalMemory / 0x100000;
          rayrunEnvironmentMessage.TotalVirtualMemory = info.TotalVirtualMemory / 0x100000;
          rayrunEnvironmentMessage.AvailableVirtualMemory = info.AvailableVirtualMemory / 0x100000;
          rayrunEnvironmentMessage.DiskSpaceFree = GetDiskSpace();
          rayrunEnvironmentMessage.Cpu = GetCpu();
          rayrunEnvironmentMessage.OSVersion = GetOSVersion();
        }
        catch (SecurityException)
        {
          System.Diagnostics.Trace.WriteLine("RaygunClient error: couldn't access environment variables. If you are running in Medium Trust, in web.config in RaygunSettings set mediumtrust=\"true\"");
        }
      }

      return rayrunEnvironmentMessage;
    }

    private string GetCpu()
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

    private string GetOSVersion()
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

    private IEnumerable<double> GetDiskSpace()
    {
      foreach (DriveInfo drive in DriveInfo.GetDrives())
      {
        if (drive.IsReady)
        {
          yield return (double)drive.AvailableFreeSpace / 0x40000000; // in GB
        }
      }
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
  }
}
