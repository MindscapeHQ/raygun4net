using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
#if !WINRT
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
    private List<double> _diskSpaceFree;

    public RaygunEnvironmentMessage()
    {
      ProcessorCount = Environment.ProcessorCount;
      
#if !WINRT
      OSVersion = Environment.OSVersion.VersionString;
      Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
      Cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
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
      //GetCpu();
#else
      PackageVersion = string.Format("{0}.{1}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor);
      Cpu = Package.Current.Id.Architecture.ToString();
      //WindowBoundsHeight = Windows.UI.Xaml.Window.Current.Bounds.Height;
      //WindowBoundsWidth = Windows.UI.Xaml.Window.Current.Bounds.Width;
      ResolutionScale = DisplayProperties.ResolutionScale.ToString();
      CurrentOrientation = DisplayProperties.CurrentOrientation.ToString();
      Location = Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion;
      //DeviceInformation deviceInformation = DeviceInformation.CreateFromIdAsync("97FADB10-4E33-40AE-359C-8BEF029DBDD0").GetResults();
      //Cpus = deviceInformation.Name;
      
      //PnpObjectCollection col = GetDevices().Result;

      //foreach (PnpObject device in col)
      //{
      //  System.Diagnostics.Debug.WriteLine(device.Properties["0"]);
      //}
#endif
    }

#if !WINRT
    private void GetCpu()
    {
      // This introduces a ~0.5s delay into message creation so is disabled above, but produces nicer cpu names
      // (ie. Intel Core i5-3570k @ 3.40ghz)
      ManagementClass wmiManagementProcessorClass = new ManagementClass("Win32_Processor");
      ManagementObjectCollection wmiProcessorCollection = wmiManagementProcessorClass.GetInstances();      
      foreach (ManagementObject wmiProcessorObject in wmiProcessorCollection)
      {
        try
        {
          Cpu = wmiProcessorObject.Properties["Name"].Value.ToString();
        }
        catch (ManagementException)
        {          
        }
      }
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
        if (_diskSpaceFree == null)
        {
          _diskSpaceFree = new List<double>();
        }
        return _diskSpaceFree;
      }
      set { _diskSpaceFree = value; }
    }
  }
}