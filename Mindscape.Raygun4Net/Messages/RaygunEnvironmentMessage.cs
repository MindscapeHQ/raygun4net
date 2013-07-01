using System;
using System.Collections.Generic;
using System.Diagnostics;
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
#elif ANDROID
using Android.OS;
using Android.Content.Res;
using Android.Content;
using Android.Views;
using Android.App;
using Android.Content.PM;
#elif IOS
using MonoTouch.UIKit;
using MonoTouch.Foundation;
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
#elif ANDROID
      try
      {
        Java.Util.TimeZone tz = Java.Util.TimeZone.Default;
        Java.Util.Date now = new Java.Util.Date();
        UtcOffset = tz.GetOffset(now.Time) / 3600000.0;

        OSVersion = Android.OS.Build.VERSION.Sdk;

        Locale = CultureInfo.CurrentCulture.DisplayName;

        var metrics = Resources.System.DisplayMetrics;
        WindowBoundsWidth = metrics.WidthPixels;
        WindowBoundsHeight = metrics.HeightPixels;

        if (RaygunClient.Context != null)
        {
          PackageManager manager = RaygunClient.Context.PackageManager;
          PackageInfo info = manager.GetPackageInfo(RaygunClient.Context.PackageName, 0);
          PackageVersion = info.VersionCode + " / " + info.VersionName;

          Activity activity = RaygunClient.Context as Activity;
          if (activity != null)
          {
            Display display = activity.WindowManager.DefaultDisplay;
            if (display != null)
            {
              switch (display.Rotation)
              {
                case SurfaceOrientation.Rotation0:
                  CurrentOrientation = "Portrait";
                  break;
                case SurfaceOrientation.Rotation180:
                  CurrentOrientation = "Upside down";
                  break;
                case SurfaceOrientation.Rotation270:
                  CurrentOrientation = "Landscape right";
                  break;
                case SurfaceOrientation.Rotation90:
                  CurrentOrientation = "Landscape left";
                  break;
              }
            }
          }
        }

        Java.Lang.Runtime runtime = Java.Lang.Runtime.GetRuntime();
        TotalPhysicalMemory = (ulong)runtime.TotalMemory();
        AvailablePhysicalMemory = (ulong)runtime.FreeMemory();
        ProcessorCount = runtime.AvailableProcessors();
        Architecture = Android.OS.Build.CpuAbi;
        DeviceName = string.Format("{0} / {1} / {2}",
                                    Android.OS.Build.Model,
                                    Android.OS.Build.Brand,
                                    Android.OS.Build.Manufacturer);
      }
      catch (Exception e)
      {
        System.Diagnostics.Debug.WriteLine("Failed to log device information.");
      }
#elif IOS
      OSVersion = UIDevice.CurrentDevice.SystemVersion;
      Architecture = UIDevice.CurrentDevice.SystemName;

      Locale = CultureInfo.CurrentCulture.DisplayName;

      WindowBoundsWidth = UIScreen.MainScreen.Bounds.Width;
      WindowBoundsHeight = UIScreen.MainScreen.Bounds.Height;

      TotalPhysicalMemory = GetIntSysCtl(TotalPhysicalMemoryPropertyName);
      AvailablePhysicalMemory = GetIntSysCtl(AvailablePhysicalMemoryPropertyName);
      ProcessorCount = (int)GetIntSysCtl(ProcessiorCountPropertyName);
      Cpu = GetStringSysCtl(CpuPropertyName);
      DeviceName = Environment.MachineName;
      PackageVersion = NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleVersion").ToString();
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

#elif ANDROID

#elif IOS
    private const string CpuPropertyName = "hw.machine";
    private const string TotalPhysicalMemoryPropertyName = "hw.physmem";
    private const string AvailablePhysicalMemoryPropertyName = "hw.usermem";
    private const string ProcessiorCountPropertyName = "hw.ncpu";

    [DllImport(global::MonoTouch.Constants.SystemLibrary)]
    private static extern int sysctlbyname( [MarshalAs(UnmanagedType.LPStr)] string property,
                                            IntPtr output,
                                            IntPtr oldLen,
                                            IntPtr newp,
                                            uint newlen);

    private static uint GetIntSysCtl(string propertyName)
    {
      // get the length of the string that will be returned
      var pLen = Marshal.AllocHGlobal(sizeof(int));
      sysctlbyname(propertyName, IntPtr.Zero, pLen, IntPtr.Zero, 0);

      var length = Marshal.ReadInt32(pLen);

      // check to see if we got a length
      if (length == 0)
      {
        Marshal.FreeHGlobal(pLen);
        return 0;
      }

      // get the hardware string
      var pStr = Marshal.AllocHGlobal(length);
      sysctlbyname(propertyName, pStr, pLen, IntPtr.Zero, 0);

      // convert the native string into a C# integer

      var memoryCount = Marshal.ReadInt32(pStr);
      uint memoryVal = (uint)memoryCount;

      if (memoryCount < 0)
      {
        memoryVal = (uint)((uint)int.MaxValue + (-memoryCount));
      }

      var ret = memoryVal;

      // cleanup
      Marshal.FreeHGlobal(pLen);
      Marshal.FreeHGlobal(pStr);

      return ret;
    }

    private static string GetStringSysCtl(string propertyName)
    {
      // get the length of the string that will be returned
      var pLen = Marshal.AllocHGlobal (sizeof(int));
      sysctlbyname (propertyName, IntPtr.Zero, pLen, IntPtr.Zero, 0);

      var length = Marshal.ReadInt32 (pLen);

      // check to see if we got a length
      if (length == 0) {
        Marshal.FreeHGlobal (pLen);
        return "Unknown";
      }

      // get the hardware string
      var pStr = Marshal.AllocHGlobal (length);
      sysctlbyname (propertyName, pStr, pLen, IntPtr.Zero, 0);

      // convert the native string into a C# string
      var hardwareStr = Marshal.PtrToStringAnsi (pStr);

      var ret = hardwareStr;

      // cleanup
      Marshal.FreeHGlobal (pLen);
      Marshal.FreeHGlobal (pStr);

      return ret;
    }
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

#if !ANDROID && !IOS
    [Obsolete("Use Locale instead")]
    public string Location { get; private set; }

    public ulong TotalVirtualMemory { get; private set; }

    public ulong AvailableVirtualMemory { get; private set; }

    public List<double> DiskSpaceFree
    {
      get { return _diskSpaceFree; }
      set { _diskSpaceFree = value; }
    }
#endif
    public ulong TotalPhysicalMemory { get; private set; }

    public ulong AvailablePhysicalMemory { get; private set; }

    public string DeviceName { get; private set; }

    public double UtcOffset { get; private set; }

    // Refactored properties

    public string Locale { get; private set; }
  }
}