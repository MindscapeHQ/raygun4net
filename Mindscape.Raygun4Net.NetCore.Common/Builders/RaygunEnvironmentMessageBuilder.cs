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

    private static readonly RaygunEnvironmentMessage CachedMessage = new();
    internal static DateTime LastUpdate = DateTime.MinValue;
    internal static readonly SemaphoreSlim Semaphore = new(1, 1);

    // How long a report waits for disk space before it is sent without it. DriveInfo calls can't be cancelled, so
    // the check runs on its own background thread and is abandoned (not killed) when this runs out.
    internal static TimeSpan DiskSpaceTimeout = DefaultDiskSpaceTimeout;
    internal static Func<List<double>> DiskSpaceProvider = DiskProvider.GetDiskSpace;
    private static Task<List<double>> _diskSpaceTask;
    private static DateTime _diskSpaceTaskStartedUtc;

    // Why the cached disk space is empty (null when it was collected), and whether the last refresh skipped the disk
    // check because the client that ran it ignores disk space
    private static string _diskSpaceFreeStatus;
    private static bool _diskSpaceCheckSkipped;

    // Set once the first refresh has collected the machine details, which it does before it starts the disk check
    internal static readonly ManualResetEventSlim MachineDetailsPopulated = new(false);

    public static RaygunEnvironmentMessage Build(RaygunSettingsBase settings)
    {
      var isDiskSpaceIgnored = settings?.IsDiskSpaceFreeIgnored == true;
      var staleBefore = DateTime.UtcNow.AddMinutes(-2);

      try
      {
        // Wait for a refresh running on another thread so this report gets fresh values. That refresh waits at most
        // DiskSpaceTimeout for disks, plus the other providers (normally well under a second), so allow twice the
        // disk timeout before giving up and returning what's cached.
        //
        // A client that ignores disk space never waits for another client's disk check: it runs the refresh itself if
        // nothing else holds the semaphore, and otherwise uses the cached details. Without that, a hung disk being
        // checked for a different client would still block the caller that opted out of disk space.
        var needsRefresh = LastUpdate < staleBefore || (!isDiskSpaceIgnored && _diskSpaceCheckSkipped);
        var refreshWait = isDiskSpaceIgnored ? TimeSpan.Zero : DiskSpaceTimeout + DiskSpaceTimeout;

        var isRefreshing = needsRefresh && Semaphore.Wait(refreshWait);

        if (!isRefreshing && needsRefresh && isDiskSpaceIgnored)
        {
          // Another refresh has the semaphore. Rather than send a report with no machine details at all, wait for that
          // refresh to publish them. It publishes them before it starts its disk check, so an unresponsive disk can't
          // hold this up; the limit is only a safety net for the other providers.
          MachineDetailsPopulated.Wait(DiskSpaceTimeout + DiskSpaceTimeout);
        }

        if (isRefreshing)
        {
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

            // The last refresh was run by a client that ignores disk space, so collect it for this one
            if (!isDiskSpaceIgnored && _diskSpaceCheckSkipped)
            {
              UpdateDiskSpace();
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

      List<double> diskSpaceFree;
      string diskSpaceFreeStatus;

      if (isDiskSpaceIgnored)
      {
        diskSpaceFree = new List<double>();
        diskSpaceFreeStatus = DiskSpaceFreeStatuses.Ignored;
      }
      else if (LastUpdate < staleBefore || _diskSpaceCheckSkipped)
      {
        // Couldn't refresh in time: don't send old disk space, as the disk may have filled up since
        diskSpaceFree = new List<double>();
        diskSpaceFreeStatus = DiskSpaceFreeStatuses.TimedOut;
      }
      else
      {
        diskSpaceFree = CachedMessage.DiskSpaceFree?.ToList() ?? new List<double>();
        diskSpaceFreeStatus = _diskSpaceFreeStatus;
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
        DiskSpaceFree = diskSpaceFree,
        DiskSpaceFreeStatus = diskSpaceFreeStatus,
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
      // Memory first, so a report that stops waiting for a slow disk check still gets it
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

      // Reports that can't get the semaphore can use these details now, instead of sending none. Set before the disk
      // check, so a hung disk never delays it.
      MachineDetailsPopulated.Set();

      if (settings?.IsDiskSpaceFreeIgnored == true)
      {
        _diskSpaceCheckSkipped = true;
      }
      else
      {
        UpdateDiskSpace();
      }
    }

    // Must be called while holding Semaphore
    private static void UpdateDiskSpace()
    {
      // On a timeout or error this clears the old values rather than keep sending them, as the disk may have filled up since
      _diskSpaceFreeStatus = GetDiskSpace(out var diskSpaceFree);
      CachedMessage.DiskSpaceFree = diskSpaceFree;

      // Only once the result is cached: until then, other reports still see the skip and wait for this result
      // rather than sending the old cached values
      _diskSpaceCheckSkipped = false;
    }

    // Must be called while holding Semaphore. Returns null if disk space was collected, TimedOut if the check didn't
    // finish within DiskSpaceTimeout, or Error if it failed.
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
      DiskSpaceTimeout = DefaultDiskSpaceTimeout;
      DiskSpaceProvider = DiskProvider.GetDiskSpace;
      _diskSpaceTask = null;
      _diskSpaceTaskStartedUtc = DateTime.MinValue;
      _diskSpaceFreeStatus = null;
      _diskSpaceCheckSkipped = false;
      MachineDetailsPopulated.Reset();
    }
  }

  internal static class DiskSpaceFreeStatuses
  {
    public const string TimedOut = "TimedOut";
    public const string Ignored = "Ignored";
    public const string Error = "Error";
  }
}