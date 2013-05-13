using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
#if WINRT
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.Devices.Enumeration.Pnp;
#elif WINDOWS_PHONE
using Microsoft.Phone.Info;
using System.Windows;
using Microsoft.Phone.Controls;
#else
using System.Web;
using System.Windows.Forms;
using System.Management;
using Microsoft.VisualBasic.Devices;
#endif

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    private List<double> _diskSpaceFree = new List<double>();

    public RaygunEnvironmentMessage()
    {
#if WINRT
      //WindowBoundsHeight = Windows.UI.Xaml.Window.Current.Bounds.Height;
      //WindowBoundsWidth = Windows.UI.Xaml.Window.Current.Bounds.Width;
      PackageVersion = string.Format("{0}.{1}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor);
      Cpu = Package.Current.Id.Architecture.ToString();      
      ResolutionScale = DisplayProperties.ResolutionScale.ToString();
      CurrentOrientation = DisplayProperties.CurrentOrientation.ToString();
      Location = Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion;

      DateTime now = DateTime.Now;
      UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;

      SYSTEM_INFO systemInfo = new SYSTEM_INFO();
      RaygunSystemInfoWrapper.GetNativeSystemInfo(ref systemInfo);
      Architecture = systemInfo.wProcessorArchitecture.ToString();
#elif WINDOWS_PHONE
      Locale = CultureInfo.CurrentCulture.DisplayName;
      OSVersion = Environment.OSVersion.Platform + " " + Environment.OSVersion.Version;
      object deviceName;
      DeviceExtendedProperties.TryGetValue("DeviceName", out deviceName);
      DeviceName = deviceName.ToString();

      WindowBoundsWidth = Application.Current.RootVisual.RenderSize.Width;
      WindowBoundsHeight = Application.Current.RootVisual.RenderSize.Height;

      DateTime now = DateTime.Now;
      UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;

      PhoneApplicationFrame frame = Application.Current.RootVisual as PhoneApplicationFrame;
      if (frame != null)
      {
        CurrentOrientation = frame.Orientation.ToString();
      }

      //ProcessorCount = Environment.ProcessorCount;
      // TODO: finish other values
#else
      WindowBoundsWidth = SystemInformation.VirtualScreen.Height;
      WindowBoundsHeight = SystemInformation.VirtualScreen.Width;
      ComputerInfo info = new ComputerInfo();
      Locale = CultureInfo.CurrentCulture.DisplayName;

      DateTime now = DateTime.Now;
      UtcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(now).TotalHours;

      OSVersion = info.OSVersion;

      if (!RaygunSettings.Settings.MediumTrust)
      {
        try
        {
          Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
          TotalPhysicalMemory = (ulong)info.TotalPhysicalMemory / 0x100000; // in MB
          AvailablePhysicalMemory = (ulong)info.AvailablePhysicalMemory / 0x100000;
          TotalVirtualMemory = info.TotalVirtualMemory / 0x100000;
          AvailableVirtualMemory = info.AvailableVirtualMemory / 0x100000;
          GetDiskSpace();
          Cpu = GetCpu();
        }
        catch (SecurityException)
        {
          System.Diagnostics.Trace.WriteLine("RaygunClient error: couldn't access environment variables. If you are running in Medium Trust, in web.config in RaygunSettings set mediumtrust=\"true\"");
        }
      }
#endif
    }

#if WINRT
    private async Task<PnpObjectCollection> GetDevices()
    {
      string[] properties =
        {
          "System.ItemNameDisplay",
          "System.Devices.ContainerId"
        };

      return await PnpObject.FindAllAsync(PnpObjectType.Device, properties);
    }
#elif WINDOWS_PHONE

#else
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

    [Obsolete("Use Locale instead")]
    public string Location { get; private set; }

    public ulong TotalPhysicalMemory { get; private set; }

    public ulong AvailablePhysicalMemory { get; private set; }

    public ulong TotalVirtualMemory { get; private set; }

    public ulong AvailableVirtualMemory { get; private set; }

    public List<double> DiskSpaceFree
    {
      get { return _diskSpaceFree; }
      set { _diskSpaceFree = value; }
    }

    public string DeviceName { get; private set; }

    public double UtcOffset { get; private set; }

    // Refactored properties

    public string Locale { get; private set; }
  }
}