using System;
using System.Collections.Generic;

using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Graphics.Display;
using Windows.Devices.Enumeration.Pnp;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    private List<double> _diskSpaceFree = new List<double>();

    public RaygunEnvironmentMessage()
    {
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
    }

    private async Task<PnpObjectCollection> GetDevices()
    {
      string[] properties =
        {
          "System.ItemNameDisplay",
          "System.Devices.ContainerId"
        };

      return await PnpObject.FindAllAsync(PnpObjectType.Device, properties);
    }

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

    // Refactored properties

    public string Locale { get; private set; }
  }
}