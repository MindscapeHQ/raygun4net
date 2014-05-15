using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    private int _processorCount;
    private string _osVersion;
    private double _windowBoundsWidth;
    private double _windowBoundsHeight;
    private string _resolutionScale;
    private string _architecture;
    private ulong _totalVirtualMemory;
    private ulong _availableVirtualMemory;
    private ulong _totalPhysicalMemory;
    private ulong _availablePhysicalMemory;
    private double _utcOffset;
    private string _locale;

    public RaygunEnvironmentMessage()
    {
      // Different environments can fail to load the environment details.
      // For now if they fail to load for whatever reason then just
      // swallow the exception. A good addition would be to handle
      // these cases and load them correctly depending on where its running.
      // see http://raygun.io/forums/thread/3655
      try
      {
        DateTime now = DateTime.Now;
        UtcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(now).TotalHours;

        IntPtr hWnd = GetActiveWindow();
        RECT rect;
        GetWindowRect(hWnd, out rect);
        WindowBoundsWidth = rect.Right - rect.Left;
        WindowBoundsHeight = rect.Bottom - rect.Top;

        ProcessorCount = Environment.ProcessorCount;
        Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
        OSVersion = Environment.OSVersion.VersionString;

        Locale = CultureInfo.CurrentCulture.DisplayName;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("Error getting environment info: {0}", ex.Message));
      }
    }

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    private IntPtr GetActiveWindow()
    {
      IntPtr handle = IntPtr.Zero;
      return GetForegroundWindow();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
      public int Left;        // x position of upper-left corner  
      public int Top;         // y position of upper-left corner  
      public int Right;       // x position of lower-right corner  
      public int Bottom;      // y position of lower-right corner  
    }

    public int ProcessorCount
    {
      get {return _processorCount; }
      private set
      {
        _processorCount = value;
      }
    }

    public string OSVersion
    {
      get {return _osVersion; }
      private set
      {
        _osVersion = value;
      }
    }

    public double WindowBoundsWidth
    {
      get { return _windowBoundsWidth; }
      private set
      {
        _windowBoundsWidth = value;
      }
    }

    public double WindowBoundsHeight
    {
      get { return _windowBoundsHeight; }
      private set
      {
        _windowBoundsHeight = value;
      }
    }

    public string ResolutionScale
    {
      get { return _resolutionScale; }
      private set
      {
        _resolutionScale = value;
      }
    }

    public string Architecture
    {
      get {return _architecture; }
      private set
      {
        _architecture = value;
      }
    }

    public ulong TotalVirtualMemory
    {
      get { return _totalVirtualMemory; }
      private set
      {
        _totalVirtualMemory = value;
      }
    }

    public ulong AvailableVirtualMemory
    {
      get { return _availableVirtualMemory; }
      private set
      {
        _availableVirtualMemory = value;
      }
    }

    public ulong TotalPhysicalMemory
    {
      get { return _totalPhysicalMemory; }
      private set
      {
        _totalPhysicalMemory = value;
      }
    }

    public ulong AvailablePhysicalMemory
    {
      get { return _availablePhysicalMemory;}
      private set
      {
        _availablePhysicalMemory = value;
      }
    }

    public double UtcOffset
    {
      get { return _utcOffset; }
      private set
      {
        _utcOffset = value;
      }
    }

    public string Locale
    {
      get { return _locale; }
      private set
      {
        _locale = value;
      }
    }
  }
}
