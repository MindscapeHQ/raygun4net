using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Mindscape.Raygun4Net.EnvironmentProviders;

namespace Mindscape.Raygun4Net
{
  public class RaygunEnvironmentMessageBuilder
  {
    private static readonly RaygunEnvironmentMessage CachedMessage = new RaygunEnvironmentMessage();
    private static DateTime _lastUpdate = DateTime.MinValue;
    private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

    public static RaygunEnvironmentMessage Build(RaygunSettingsBase _)
    {
      try
      {
        if (_lastUpdate < DateTime.UtcNow.AddMinutes(-2))
        {
          Semaphore.Wait();
          try
          {
            if (_lastUpdate == DateTime.MinValue)
            {
              Build();
              Update();
              _lastUpdate = DateTime.UtcNow;
            }

            if (_lastUpdate < DateTime.UtcNow.AddMinutes(-1))
            {
              Update();
              _lastUpdate = DateTime.UtcNow;
            }
          }
          catch (Exception e)
          {
            Console.WriteLine(e);
          }
          finally
          {
            Semaphore.Release();
          }
        }
      }
      catch
      {
        // Ignore
      }

      return CachedMessage;
    }

    private static void Build()
    {
      try
      {
        CachedMessage.Architecture = RuntimeInformation.ProcessArchitecture.ToString();
        CachedMessage.OSVersion = OSProvider.GetOSInformation();
        CachedMessage.ProcessorCount = Environment.ProcessorCount;
        CachedMessage.Cpu = ProcessorProvider.GetCpuName();

        var screen = ScreenProvider.GetPrimaryScreenResolution();

        if (screen.HasValue)
        {
          CachedMessage.WindowBoundsWidth = screen.Value.Width;
          CachedMessage.WindowBoundsHeight = screen.Value.Height;
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Failed to capture env details {ex.Message}");
      }

      try
      {
        CachedMessage.UtcOffset = DateTimeOffset.Now.Offset.TotalHours;
        CachedMessage.Locale = CultureInfo.CurrentCulture.DisplayName;
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Failed to capture time locale {ex.Message}");
      }
    }

    private static void Update()
    {
      try
      {
        CachedMessage.DiskSpaceFree = DiskProvider.GetDiskSpace();

        var memory = MemoryProvider.GetTotalMemory();

        if (!memory.HasValue)
        {
          return;
        }

        CachedMessage.TotalPhysicalMemory = memory.Value.TotalMemory;
        CachedMessage.AvailablePhysicalMemory = memory.Value.AvailableMemory;
        CachedMessage.TotalVirtualMemory = memory.Value.TotalVirtualMemory;
        CachedMessage.AvailableVirtualMemory = memory.Value.AvailableVirtualMemory;
      }
      catch
      {
        // Ignore
      }
    }
  }
}