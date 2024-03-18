using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Mindscape.Raygun4Net.EnvironmentProviders;

namespace Mindscape.Raygun4Net
{
  public class RaygunEnvironmentMessageBuilder
  {
    private static readonly RaygunEnvironmentMessage CachedMessage = new();
    internal static DateTime LastUpdate = DateTime.MinValue;
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    public static RaygunEnvironmentMessage Build(RaygunSettingsBase settings)
    {
      try
      {
        if (LastUpdate < DateTime.UtcNow.AddMinutes(-2))
        {
          Semaphore.Wait();
          
          try
          {
            if (LastUpdate == DateTime.MinValue)
            {
              // Build adds all the static data that doesn't change
              Build();
              
              // Update includes Memory / Disk which is prone to change
              Update(settings);
              LastUpdate = DateTime.UtcNow;
            }

            if (LastUpdate < DateTime.UtcNow.AddMinutes(-1))
            {
              Update(settings);
              LastUpdate = DateTime.UtcNow;
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
        // Ignore - if an error occurs lets just return what we have and carry on, this is less important than not logging the error
      }

      // Return a copy of the cached message to avoid outside changes
      return new RaygunEnvironmentMessage
      {
        OSVersion = CachedMessage.OSVersion,
        Architecture = CachedMessage.Architecture,
        Cpu = CachedMessage.Cpu,
        ProcessorCount = CachedMessage.ProcessorCount,
        AvailablePhysicalMemory = CachedMessage.AvailablePhysicalMemory,
        AvailableVirtualMemory = CachedMessage.AvailableVirtualMemory,
        TotalPhysicalMemory = CachedMessage.TotalPhysicalMemory,
        TotalVirtualMemory = CachedMessage.TotalVirtualMemory,
        DiskSpaceFree = CachedMessage.DiskSpaceFree.ToList(),
        WindowBoundsHeight = CachedMessage.WindowBoundsHeight,
        WindowBoundsWidth = CachedMessage.WindowBoundsWidth,
        Locale = CachedMessage.Locale,
        UtcOffset = CachedMessage.UtcOffset,
        EnvironmentVariables = CachedMessage.EnvironmentVariables
      };
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

    private static void Update(RaygunSettingsBase settings)
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
        CachedMessage.EnvironmentVariables = EnvironmentVariablesProvider.GetEnvironmentVariables(settings);
      }
      catch
      {
        // Ignore
      }
    }
  }
}