using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
#if !WINRT
using System.Web;
using System.Windows.Forms;
using System.Management;
using Microsoft.VisualBasic.Devices;
#else
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.Devices.Enumeration.Pnp;
#endif

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    private List<double> _diskSpaceFree = new List<double>();

    public RaygunEnvironmentMessage()
    {
      ProcessorCount = Environment.ProcessorCount;

#if !WINRT
      OSVersion = Environment.OSVersion.VersionString;
      Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");      
      WindowBoundsWidth = SystemInformation.VirtualScreen.Height;
      WindowBoundsHeight = SystemInformation.VirtualScreen.Width;      
      ComputerInfo info = new ComputerInfo();
      TotalPhysicalMemory = (ulong)info.TotalPhysicalMemory / 0x100000; // in MB
      AvailablePhysicalMemory = (ulong)info.AvailablePhysicalMemory / 0x100000;
      TotalVirtualMemory = info.TotalVirtualMemory / 0x100000;
      AvailableVirtualMemory = info.AvailableVirtualMemory / 0x100000;

      Location = CultureInfo.CurrentCulture.DisplayName;
      OSVersion = info.OSVersion;
      GetDiskSpace();  
    
      if (HttpContext.Current == null)
      {
        if (string.IsNullOrEmpty(Properties.Environment.Default.Cpu))
        {
          Properties.Environment.Default.Cpu = GetCpu();
          Properties.Environment.Default.Save();
        }
        Cpu = Properties.Environment.Default.Cpu;
      }      
      else
      {
        Cpu = null;
      }
#else
      //WindowBoundsHeight = Windows.UI.Xaml.Window.Current.Bounds.Height;
      //WindowBoundsWidth = Windows.UI.Xaml.Window.Current.Bounds.Width;
      PackageVersion = string.Format("{0}.{1}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor);
      Cpu = Package.Current.Id.Architecture.ToString();      
      ResolutionScale = DisplayProperties.ResolutionScale.ToString();
      CurrentOrientation = DisplayProperties.CurrentOrientation.ToString();
      Location = Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion;
      
      SYSTEM_INFO systemInfo = new SYSTEM_INFO();
      RaygunSystemInfoWrapper.GetNativeSystemInfo(ref systemInfo);
      Architecture = systemInfo.wProcessorArchitecture.ToString();
#endif
    }

#if !WINRT    
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
#else    
    private async Task<PnpObjectCollection> GetDevices()
    {
      string[] properties =
        {
          "System.ItemNameDisplay",
          "System.Devices.ContainerId"
        };

      return await PnpObject.FindAllAsync(PnpObjectType.Device, properties);
    }
#endif

    public int ProcessorCount { get; private set; }

    public string OSVersion { get; private set; }

    public double WindowBoundsWidth { get; private set; }

    public double WindowBoundsHeight { get; private set; }

    public string ResolutionScale { get; private set; }

    public string CurrentOrientation { get; private set; }

    public string Cpu { get; private set; }

    public string PackageVersion { get; private set; }

    public string Architecture { get; private set; }

    public string Location { get; private set; }

    public ulong TotalPhysicalMemory { get; private set; }

    public ulong AvailablePhysicalMemory { get; private set; }

    public ulong TotalVirtualMemory { get; set; }

    public ulong AvailableVirtualMemory { get; set; }

    public List<double> DiskSpaceFree
    {
      get
      {
        return _diskSpaceFree;
      }
      set { _diskSpaceFree = value; }
    }

    public string DeviceName { get; private set; }
  }
}