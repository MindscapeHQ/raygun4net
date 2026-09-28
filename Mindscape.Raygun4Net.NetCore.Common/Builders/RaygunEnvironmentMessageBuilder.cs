using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Mindscape.Raygun4Net.EnvironmentProviders;

namespace Mindscape.Raygun4Net
{
  public class RaygunEnvironmentMessageBuilder
  {
    private static readonly TimeSpan DefaultDiskSpaceTimeout = TimeSpan.FromSeconds(5);

    // Machine details (everything except disk space) and disk space are cached for the whole process, and shared by
    // every RaygunClient in it. Each has its own timestamp and semaphore, so refreshing one never makes the other look
    // fresh, and a report that only needs the machine details never waits for a disk check.
    private static readonly RaygunEnvironmentMessage CachedMessage = new();
    internal static DateTime LastUpdate = DateTime.MinValue;
    internal static readonly SemaphoreSlim Semaphore = new(1, 1);

    internal static DateTime DiskSpaceLastUpdate = DateTime.MinValue;
    internal static readonly SemaphoreSlim DiskSpaceSemaphore = new(1, 1);

    // How long a report waits for disk space before it is sent without it. DriveInfo calls can't be cancelled, so
    // the check runs on its own background thread and is abandoned (not killed) when this runs out.
    internal static TimeSpan DiskSpaceTimeout = DefaultDiskSpaceTimeout;
    internal static Func<List<double>> DiskSpaceProvider = DiskProvider.GetDiskSpace;
    private static Task<List<double>> _diskSpaceTask;
    private static DateTime _diskSpaceTaskStartedUtc;

    // Why the cached disk space is empty (null when it was collected)
    private static string _diskSpaceFreeStatus;

    /// <summary>
    /// Builds the environment details for a report, including free disk space. Whether a report should leave disk
    /// space out is a per-client setting, so it's decided by the caller: see <see cref="BuildWithoutDiskSpace"/>.
    /// </summary>
    public static RaygunEnvironmentMessage Build(RaygunSettingsBase settings)
    {
      // Disk space goes on top of the machine details, which have their own cache and never wait for a disk check
      var message = BuildWithoutDiskSpace(settings);

      message.DiskSpaceFreeStatus = GetCachedDiskSpace(out var diskSpaceFree);
      message.DiskSpaceFree = diskSpaceFree;

      return message;
    }

    // Everything except disk space. Never checks disks and never waits for a disk check.
    internal static RaygunEnvironmentMessage BuildWithoutDiskSpace(RaygunSettingsBase settings)
    {
      try
      {
        if (LastUpdate < DateTime.UtcNow.AddMinutes(-2))
        {
          // The first report has nothing to send yet, so wait for a refresh running on another thread. It doesn't
          // touch the disks, so the disk time limit is reused only as a safety net for a slow provider. Later reports
          // send what's cached rather than wait, as machine details a couple of minutes old are still accurate enough.
          var wait = LastUpdate == DateTime.MinValue ? DiskSpaceTimeout : TimeSpan.Zero;

          if (Semaphore.Wait(wait))
          {
            try
            {
              if (LastUpdate == DateTime.MinValue)
              {
                // Build adds all the static data that doesn't change
                Build();

                // Update includes Memory which is prone to change
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
        DiskSpaceFree = new List<double>(),
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
        var memory = MemoryProvider.GetTotalMemory();

        if (memory.HasValue)
        {
          CachedMessage.TotalPhysicalMemory = memory.Value.TotalMemory;
          CachedMessage.AvailablePhysicalMemory = memory.Value.AvailableMemory;
          CachedMessage.TotalVirtualMemory = memory.Value.TotalVirtualMemory;
          CachedMessage.AvailableVirtualMemory = memory.Value.AvailableVirtualMemory;
          CachedMessage.EnvironmentVariables = EnvironmentVariablesProvider.GetEnvironmentVariables(settings);
        }
      }
      catch
      {
        // Ignore
      }
    }

    // Returns a copy of the cached disk space and its status, refreshing it first when it's stale
    private static string GetCachedDiskSpace(out List<double> diskSpaceFree)
    {
      var staleBefore = DateTime.UtcNow.AddMinutes(-2);

      try
      {
        // Wait for a refresh running on another thread so this report gets fresh values. That refresh waits at most
        // DiskSpaceTimeout from when its check started; the second DiskSpaceTimeout is only slack before giving up.
        if (DiskSpaceLastUpdate < staleBefore && DiskSpaceSemaphore.Wait(DiskSpaceTimeout + DiskSpaceTimeout))
        {
          try
          {
            // A refresh that ran while this report was waiting already has fresh values
            if (DiskSpaceLastUpdate < DateTime.UtcNow.AddMinutes(-1))
            {
              // On a timeout or error this clears the old values rather than keep sending them, as the disk may have filled up since
              _diskSpaceFreeStatus = GetDiskSpace(out var collected);
              CachedMessage.DiskSpaceFree = collected;
              DiskSpaceLastUpdate = DateTime.UtcNow;
            }
          }
          catch (Exception e)
          {
            Console.WriteLine(e);
          }
          finally
          {
            DiskSpaceSemaphore.Release();
          }
        }
      }
      catch
      {
        // Ignore - as for the machine details, return what we have rather than fail the report
      }

      if (DiskSpaceLastUpdate < staleBefore)
      {
        // Couldn't refresh in time: don't send old disk space, as the disk may have filled up since
        diskSpaceFree = new List<double>();
        return DiskSpaceFreeStatuses.TimedOut;
      }

      diskSpaceFree = CachedMessage.DiskSpaceFree?.ToList() ?? new List<double>();
      return _diskSpaceFreeStatus;
    }

    // Must be called while holding DiskSpaceSemaphore. Returns null if disk space was collected, TimedOut if the check
    // didn't finish within DiskSpaceTimeout, or Error if it failed.
    private static string GetDiskSpace(out List<double> diskSpaceFree)
    {
      // Only one check runs at a time: if an earlier check is still stuck, keep waiting on that one rather than
      // starting another thread that would get stuck too.
      if (_diskSpaceTask == null || _diskSpaceTask.IsCompleted)
      {
        _diskSpaceTask = StartDiskSpaceCheck();
        _diskSpaceTaskStartedUtc = DateTime.UtcNow;
      }

      // Measured from when the check started, so a check that is already stuck doesn't delay every later refresh
      // by the full timeout again.
      var remaining = DiskSpaceTimeout - (DateTime.UtcNow - _diskSpaceTaskStartedUtc);

      try
      {
        if (!_diskSpaceTask.Wait(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero))
        {
          diskSpaceFree = new List<double>();
          return DiskSpaceFreeStatuses.TimedOut;
        }

        diskSpaceFree = _diskSpaceTask.Result ?? new List<double>();
        return null;
      }
      catch
      {
        // The provider threw, so the check did finish but couldn't read the disks
        diskSpaceFree = new List<double>();
        return DiskSpaceFreeStatuses.Error;
      }
    }

    private static Task<List<double>> StartDiskSpaceCheck()
    {
      // LongRunning gives the check its own background thread rather than a thread-pool one: a stuck check then never
      // ties up a pool thread, and doesn't keep the process alive when the app exits.
      var task = Task.Factory.StartNew(DiskSpaceProvider, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

      // A check that fails after its report stopped waiting is never waited on again. Observe the failure so it isn't
      // raised as an UnobservedTaskException, which RaygunClient would send as a crash report of its own.
      task.ContinueWith(t => _ = t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

      return task;
    }

    internal static void ResetForTests()
    {
      LastUpdate = DateTime.MinValue;
      DiskSpaceLastUpdate = DateTime.MinValue;
      DiskSpaceTimeout = DefaultDiskSpaceTimeout;
      DiskSpaceProvider = DiskProvider.GetDiskSpace;
      _diskSpaceTask = null;
      _diskSpaceTaskStartedUtc = DateTime.MinValue;
      _diskSpaceFreeStatus = null;
    }
  }

  internal static class DiskSpaceFreeStatuses
  {
    public const string TimedOut = "TimedOut";
    public const string Ignored = "Ignored";
    public const string Error = "Error";
  }
}