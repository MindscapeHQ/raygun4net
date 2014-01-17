using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

using Microsoft.Phone.Info;
using System.Windows;
using Microsoft.Phone.Controls;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    private List<double> _diskSpaceFree = new List<double>();

    public RaygunEnvironmentMessage()
    {
      Locale = CultureInfo.CurrentCulture.DisplayName;
      OSVersion = Environment.OSVersion.Platform + " " + Environment.OSVersion.Version;
      object deviceName;
      DeviceExtendedProperties.TryGetValue("DeviceName", out deviceName);
      DeviceName = deviceName.ToString();

      DateTime now = DateTime.Now;
      UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;

      Deployment.Current.Dispatcher.BeginInvoke(() =>
      {
        WindowBoundsWidth = Application.Current.RootVisual.RenderSize.Width;
        WindowBoundsHeight = Application.Current.RootVisual.RenderSize.Height;
        PhoneApplicationFrame frame = Application.Current.RootVisual as PhoneApplicationFrame;
        if (frame != null)
        {
          CurrentOrientation = frame.Orientation.ToString();
        }
      });

      //ProcessorCount = Environment.ProcessorCount;
      // TODO: finish other values
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