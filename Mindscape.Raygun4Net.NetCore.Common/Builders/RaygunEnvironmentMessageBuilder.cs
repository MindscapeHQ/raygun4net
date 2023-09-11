
using System;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace Mindscape.Raygun4Net
{
  public class RaygunEnvironmentMessageBuilder
  {
    public static RaygunEnvironmentMessage Build(RaygunSettingsBase settings)
    {
      RaygunEnvironmentMessage message = new RaygunEnvironmentMessage();

      try
      {
        message.Architecture = RuntimeInformation.ProcessArchitecture.ToString();
        message.OSVersion = RuntimeInformation.OSDescription;
        message.ProcessorCount = Environment.ProcessorCount;
        message.Cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");

        message = AddWindowSize(message);

# if NETSTANDARD2_0_OR_GREATER || NET
        var process = Process.GetCurrentProcess();

        message.TotalVirtualMemory = (ulong)process.VirtualMemorySize64;
        message.AvailableVirtualMemory = (ulong)process.PagedSystemMemorySize64;
        message.TotalPhysicalMemory = (ulong)process.NonpagedSystemMemorySize64;
        message.AvailablePhysicalMemory = (ulong)process.NonpagedSystemMemorySize64;

        message.DiskSpaceFree = DriveInfo.GetDrives()
          .Where(x => x.IsReady)
          .Select(d => (double)d.AvailableFreeSpace )
          .ToList();
#endif
      }
      catch (Exception ex)
      {
        Console.WriteLine("TEST FAILED");
        Console.WriteLine(ex.Message);
        Debug.WriteLine($"Failed to capture env details {ex.Message}");
      }

      try
      {
        DateTime now = DateTime.Now;
        message.UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;
        message.Locale = CultureInfo.CurrentCulture.DisplayName;
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Failed to capture time locale {ex.Message}");
      }

      return message;
    }

    private static RaygunEnvironmentMessage AddWindowSize(RaygunEnvironmentMessage message)
    {
      try
      {
        //If redirected then we may not be able to get a handle for the console. Which leads to IOException
        if (!Console.IsOutputRedirected)
        {
             message.WindowBoundsWidth = Console.WindowWidth;
             message.WindowBoundsHeight = Console.WindowHeight;
        }
      }
      catch (Exception e)
      {
        Debug.WriteLine($"Unable to get window size {e.Message}");
      }

      return message;
    }
  }
}