using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  [TestFixture]
  [NonParallelizable]
  public class RaygunMessageBuilderTests
  {
    private RaygunSettings _settings;
    private RaygunMessageBuilder _builder;

    [SetUp]
    public void SetUp()
    {
      RaygunEnvironmentMessageBuilder.ResetForTests();
      _settings = new RaygunSettings();
      _builder = RaygunMessageBuilder.New(_settings);
    }

    [TearDown]
    public void TearDown()
    {
      RaygunEnvironmentMessageBuilder.ResetForTests();
    }

    [Test]
    public void New()
    {
      Assert.That(_builder, Is.Not.Null);
    }

    [Test]
    public void SetVersion()
    {
      var builder = _builder.SetVersion("Custom Version");
      Assert.That(_builder, Is.EqualTo(builder));

      var message = _builder.Build();
      Assert.That("Custom Version", Is.EqualTo(message.Details.Version));
    }

    [Test]
    public void SetTimeStamp()
    {
      var time = new DateTime(2015, 2, 16);
      var message = _builder.SetTimeStamp(time).Build();
      Assert.That(time, Is.EqualTo(message.OccurredOn));
    }

    [Test]
    public void SetNullTimeStamp()
    {
      var message = _builder.SetTimeStamp(null).Build();
      Assert.That((DateTime.UtcNow - message.OccurredOn).TotalSeconds < 1, Is.True);
    }

    [Test]
    public void HasMachineName()
    {
      var message = _builder.SetMachineName(Environment.MachineName).Build();

      Assert.That(message.Details, Is.Not.Null);
      Assert.That(message.Details.MachineName, Is.Not.Null);
    }

    [Test]
    public void HasEnvironmentInformation()
    {
      var message = _builder.SetEnvironmentDetails().Build();

      Assert.That(message.Details, Is.Not.Null);
      Assert.That(message.Details.Environment, Is.Not.Null);
      Assert.That(message.Details.Environment.Architecture, Is.Not.Empty);
      
      Assert.That(message.Details.Environment.WindowBoundsHeight, Is.GreaterThanOrEqualTo(0));
      Assert.That(message.Details.Environment.WindowBoundsWidth, Is.GreaterThanOrEqualTo(0));

      Assert.That(message.Details.Environment.Cpu, Is.Not.Empty);

      Assert.That(message.Details.Environment.ProcessorCount, Is.GreaterThanOrEqualTo(1));
      Assert.That(message.Details.Environment.OSVersion, Is.Not.Empty);
      Assert.That(message.Details.Environment.Locale, Is.Not.Empty);

      Assert.That(message.Details.Environment.DiskSpaceFree, Is.Not.Null);
      Assert.That(message.Details.Environment.DiskSpaceFree.Any(), Is.True);
      Assert.That(message.Details.Environment.DiskSpaceFree.All(a => a > 0), Is.True);
    }

    [Test]
    public void HasEnvironmentMemoryInformation()
    {
      var message = _builder.SetEnvironmentDetails().Build();

      Assert.That(message.Details.Environment.AvailablePhysicalMemory, Is.Not.Zero);
      Assert.That(message.Details.Environment.TotalPhysicalMemory, Is.Not.Zero);
      Assert.That(message.Details.Environment.AvailableVirtualMemory, Is.Not.Zero);
      Assert.That(message.Details.Environment.TotalVirtualMemory, Is.Not.Zero);
    }

    [Test]
    public void EnvironmentBuild_WhenDiskCheckHangs_ReturnsWithinTimeLimitWithoutDiskSpace()
    {
      using var gate = new ManualResetEventSlim(false);
      UseHangingDiskProvider(gate);

      try
      {
        var stopwatch = Stopwatch.StartNew();
        var result = RaygunEnvironmentMessageBuilder.Build(_settings);
        stopwatch.Stop();

        stopwatch.Elapsed.Should().BeLessThan(TestDiskSpaceTimeout + TimeLimitMargin);
        result.DiskSpaceFree.Should().NotBeNull().And.BeEmpty();
        result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.TimedOut);
        result.OSVersion.Should().NotBeNullOrEmpty();
        result.ProcessorCount.Should().BeGreaterThan(0);
        result.TotalPhysicalMemory.Should().NotBe(0);
      }
      finally
      {
        gate.Set();
      }
    }

    [Test]
    public void EnvironmentBuild_WhenRefreshTimesOutAfterEarlierSuccess_DoesNotReturnStaleDiskSpace()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => new List<double> { 123 };
      RaygunEnvironmentMessageBuilder.Build(_settings).DiskSpaceFree.Should().Equal(123);

      // The cache is now older than the cache window, and the next check hangs
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.UtcNow.AddMinutes(-5);

      using var gate = new ManualResetEventSlim(false);
      UseHangingDiskProvider(gate);

      try
      {
        var result = RaygunEnvironmentMessageBuilder.Build(_settings);

        result.DiskSpaceFree.Should().BeEmpty("stale disk space could show free space on a disk that has since filled up");
        result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.TimedOut);
      }
      finally
      {
        gate.Set();
      }
    }

    [Test]
    public async Task EnvironmentBuild_WhenAnotherReportIsRefreshing_WaitsAndReturnsFreshDiskSpace()
    {
      using var providerStarted = new ManualResetEventSlim(false);
      RaygunEnvironmentMessageBuilder.DiskSpaceTimeout = TimeSpan.FromSeconds(5);
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () =>
      {
        providerStarted.Set();
        Thread.Sleep(300);
        return new List<double> { 42 };
      };

      var first = Task.Run(() => RaygunEnvironmentMessageBuilder.Build(_settings));
      providerStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

      // Arrives while the first report is still collecting disk space: it should wait for the fresh value,
      // not skip ahead with empty or cached values
      var second = await Task.Run(() => RaygunEnvironmentMessageBuilder.Build(_settings));

      second.DiskSpaceFree.Should().Equal(42);
      second.DiskSpaceFreeStatus.Should().BeNull();
      (await first).DiskSpaceFree.Should().Equal(42);
    }

    [Test]
    public async Task EnvironmentBuild_WhenDiskCheckHangs_ConcurrentReportsAllReturnAndOnlyOneCheckRuns()
    {
      using var gate = new ManualResetEventSlim(false);
      var calls = UseHangingDiskProvider(gate);

      try
      {
        var stopwatch = Stopwatch.StartNew();
        var results = await Task.WhenAll(Enumerable.Range(0, 10)
                                                   .Select(_ => Task.Run(() => RaygunEnvironmentMessageBuilder.Build(_settings))));
        stopwatch.Stop();

        // Each waiting report is bounded by the time limit, so even the slowest returns within two of them
        stopwatch.Elapsed.Should().BeLessThan(TestDiskSpaceTimeout + TestDiskSpaceTimeout + TimeLimitMargin);
        results.Should().OnlyContain(r => r.DiskSpaceFree != null && r.DiskSpaceFree.Count == 0);
        calls().Should().Be(1);
      }
      finally
      {
        gate.Set();
      }
    }

    [Test]
    public void EnvironmentBuild_WhenEarlierCheckIsStillStuck_DoesNotWaitAgainOrStartAnotherCheck()
    {
      using var gate = new ManualResetEventSlim(false);
      var calls = UseHangingDiskProvider(gate);

      try
      {
        RaygunEnvironmentMessageBuilder.Build(_settings);
        RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.UtcNow.AddMinutes(-5);

        var stopwatch = Stopwatch.StartNew();
        var result = RaygunEnvironmentMessageBuilder.Build(_settings);
        stopwatch.Stop();

        stopwatch.Elapsed.Should().BeLessThan(TestDiskSpaceTimeout, "the stuck check already used up its time limit");
        result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.TimedOut);
        calls().Should().Be(1);
      }
      finally
      {
        gate.Set();
      }
    }

    [Test]
    public void EnvironmentBuild_AfterStuckCheckFinishes_NextRefreshStartsNewCheck()
    {
      using var gate = new ManualResetEventSlim(false);
      var calls = UseHangingDiskProvider(gate);

      RaygunEnvironmentMessageBuilder.Build(_settings).DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.TimedOut);

      gate.Set();
      SpinWait.SpinUntil(() => calls() == 1 && IsDiskCheckFinished(), TimeSpan.FromSeconds(5)).Should().BeTrue();

      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => new List<double> { 7 };
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.UtcNow.AddMinutes(-5);

      var result = RaygunEnvironmentMessageBuilder.Build(_settings);

      result.DiskSpaceFree.Should().Equal(7);
      result.DiskSpaceFreeStatus.Should().BeNull();
    }

    [Test]
    public void EnvironmentBuild_WhenDiskSpaceIgnored_NeverChecksDisks()
    {
      var calls = 0;
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () =>
      {
        Interlocked.Increment(ref calls);
        return new List<double> { 42 };
      };

      var result = RaygunEnvironmentMessageBuilder.Build(new RaygunSettings { IsDiskSpaceFreeIgnored = true });

      calls.Should().Be(0);
      result.DiskSpaceFree.Should().NotBeNull().And.BeEmpty();
      result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Ignored);
    }

    [Test]
    public void EnvironmentBuild_WhenDiskSpaceIgnored_DoesNotReturnValuesCachedByAnotherClient()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => new List<double> { 42 };
      RaygunEnvironmentMessageBuilder.Build(_settings).DiskSpaceFree.Should().Equal(42);

      var result = RaygunEnvironmentMessageBuilder.Build(new RaygunSettings { IsDiskSpaceFreeIgnored = true });

      result.DiskSpaceFree.Should().BeEmpty();
      result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Ignored);
    }

    [Test]
    public void EnvironmentBuild_WhenAnotherClientIgnoresDiskSpace_StillCollectsDiskSpaceForThisClient()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => new List<double> { 123 };

      // The client that ignores disk space triggers the refresh, so its refresh skips the disk check
      RaygunEnvironmentMessageBuilder.Build(new RaygunSettings { IsDiskSpaceFreeIgnored = true });

      var result = RaygunEnvironmentMessageBuilder.Build(_settings);

      result.DiskSpaceFree.Should().Equal(123);
      result.DiskSpaceFreeStatus.Should().BeNull();
    }

    [Test]
    public void EnvironmentBuild_WhenDiskSpaceIgnoredAndAnotherReportsCheckHangs_ReturnsWithoutWaiting()
    {
      using var gate = new ManualResetEventSlim(false);
      using var providerStarted = new ManualResetEventSlim(false);
      var calls = 0;
      // A long time limit, so a report that queues behind this check is unmistakably slower than one that doesn't
      RaygunEnvironmentMessageBuilder.DiskSpaceTimeout = TimeSpan.FromSeconds(5);
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () =>
      {
        Interlocked.Increment(ref calls);
        providerStarted.Set();
        gate.Wait();
        return new List<double> { 42 };
      };

      var refreshing = Task.Run(() => RaygunEnvironmentMessageBuilder.Build(_settings));

      try
      {
        providerStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

        var stopwatch = Stopwatch.StartNew();
        var result = RaygunEnvironmentMessageBuilder.Build(new RaygunSettings { IsDiskSpaceFreeIgnored = true });
        stopwatch.Stop();

        stopwatch.Elapsed.Should().BeLessThan(TimeLimitMargin, "a client that ignores disk space must not queue behind another client's disk check");
        result.DiskSpaceFree.Should().NotBeNull().And.BeEmpty();
        result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Ignored);
        Volatile.Read(ref calls).Should().Be(1, "the report that ignores disk space must not start a check of its own");

      }
      finally
      {
        gate.Set();
        refreshing.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
      }
    }

    [Test]
    public void EnvironmentBuild_WhenDiskSpaceIgnoredAndNothingIsCachedYet_WaitsForTheMachineDetailsBeingCollected()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceTimeout = TimeSpan.FromSeconds(2);
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => new List<double> { 42 };

      // Nothing has been collected yet and the first refresh is under way, holding the semaphore
      RaygunEnvironmentMessageBuilder.Semaphore.Wait();
      var ignoringReport = Task.Run(() => RaygunEnvironmentMessageBuilder.Build(new RaygunSettings { IsDiskSpaceFreeIgnored = true }));

      try
      {
        ignoringReport.Wait(TimeSpan.FromMilliseconds(300)).Should().BeFalse("there are no details to send yet");
      }
      finally
      {
        RaygunEnvironmentMessageBuilder.Semaphore.Release();
      }

      // That refresh publishes the machine details
      RaygunEnvironmentMessageBuilder.Build(_settings);

      ignoringReport.Wait(TimeLimitMargin).Should().BeTrue();
      ignoringReport.Result.OSVersion.Should().NotBeNullOrEmpty("a report with no machine details at all is worse than a slightly late one");
      ignoringReport.Result.ProcessorCount.Should().BeGreaterThan(0);
      ignoringReport.Result.TotalPhysicalMemory.Should().NotBe(0);
      ignoringReport.Result.DiskSpaceFree.Should().NotBeNull().And.BeEmpty();
      ignoringReport.Result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Ignored);
    }

    [Test]
    public void EnvironmentBuild_ByDefault_LimitsTheDiskCheckToFiveSeconds()
    {
      // The limit documented in the READMEs and the change log
      RaygunEnvironmentMessageBuilder.DiskSpaceTimeout.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Test]
    public void EnvironmentBuild_WhenDiskSpaceIgnoredAndSemaphoreHeld_ReturnsCachedDetailsWithoutReleasingSemaphore()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => new List<double> { 123 };
      RaygunEnvironmentMessageBuilder.Build(_settings);
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.UtcNow.AddMinutes(-5);

      RaygunEnvironmentMessageBuilder.Semaphore.Wait();
      try
      {
        var build = Task.Run(() => RaygunEnvironmentMessageBuilder.Build(new RaygunSettings { IsDiskSpaceFreeIgnored = true }));

        build.Wait(TimeLimitMargin).Should().BeTrue("a client that ignores disk space doesn't wait for a refresh it doesn't need");
        build.Result.DiskSpaceFree.Should().NotBeNull().And.BeEmpty();
        build.Result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Ignored);
        build.Result.OSVersion.Should().NotBeNullOrEmpty("the machine details already collected are still sent");
        build.Result.TotalPhysicalMemory.Should().NotBe(0);
        RaygunEnvironmentMessageBuilder.Semaphore.CurrentCount.Should().Be(0, "a thread that didn't get the semaphore must not release it");
      }
      finally
      {
        RaygunEnvironmentMessageBuilder.Semaphore.Release();
      }
    }

    [Test]
    public void EnvironmentBuild_WhenDiskSpaceIgnoredAndNoRefreshIsRunning_StillCollectsMachineDetails()
    {
      var calls = 0;
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () =>
      {
        Interlocked.Increment(ref calls);
        return new List<double> { 42 };
      };

      var result = RaygunEnvironmentMessageBuilder.Build(new RaygunSettings { IsDiskSpaceFreeIgnored = true });

      result.OSVersion.Should().NotBeNullOrEmpty();
      result.ProcessorCount.Should().BeGreaterThan(0);
      result.TotalPhysicalMemory.Should().NotBe(0);
      result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Ignored);
      RaygunEnvironmentMessageBuilder.LastUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
      calls.Should().Be(0);
    }

    [Test]
    public async Task EnvironmentBuild_WhenDiskSpaceIgnoredReportSkipsRefresh_OtherClientsStillGetFreshDiskSpace()
    {
      using var providerStarted = new ManualResetEventSlim(false);
      var calls = 0;
      RaygunEnvironmentMessageBuilder.DiskSpaceTimeout = TimeSpan.FromSeconds(5);
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () =>
      {
        Interlocked.Increment(ref calls);
        providerStarted.Set();
        Thread.Sleep(300);
        return new List<double> { 42 };
      };

      var refreshing = Task.Run(() => RaygunEnvironmentMessageBuilder.Build(_settings));
      providerStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

      // Passing through without the semaphore must leave the refresh, and the cached values it produces, untouched
      RaygunEnvironmentMessageBuilder.Build(new RaygunSettings { IsDiskSpaceFreeIgnored = true })
                                     .DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Ignored);

      (await refreshing).DiskSpaceFree.Should().Equal(42);

      var afterwards = RaygunEnvironmentMessageBuilder.Build(_settings);

      afterwards.DiskSpaceFree.Should().Equal(42);
      afterwards.DiskSpaceFreeStatus.Should().BeNull();
      Volatile.Read(ref calls).Should().Be(1);
    }

    [Test]
    public void EnvironmentBuild_WhenDiskCheckStarts_MemoryIsAlreadyRefreshed()
    {
      // A report that stops waiting for a hung disk check still gets memory, because it was collected first
      GetCachedEnvironmentMessage().TotalPhysicalMemory = 0;
      ulong memoryWhenDiskCheckStarted = 0;
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () =>
      {
        memoryWhenDiskCheckStarted = GetCachedEnvironmentMessage().TotalPhysicalMemory;
        return new List<double> { 42 };
      };

      RaygunEnvironmentMessageBuilder.Build(_settings);

      memoryWhenDiskCheckStarted.Should().NotBe(0);
    }

    [Test]
    public void EnvironmentBuild_JustBeforeCacheWindowEnds_ReturnsCollectedDiskSpace()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => new List<double> { 5 };
      RaygunEnvironmentMessageBuilder.Build(_settings);
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.UtcNow.AddMinutes(-2).AddMilliseconds(100);

      var result = RaygunEnvironmentMessageBuilder.Build(_settings);

      result.DiskSpaceFree.Should().Equal(5);
      result.DiskSpaceFreeStatus.Should().BeNull();
    }

    [Test]
    public async Task EnvironmentBuild_WhenTwoFirstReportsArriveTogether_BothGetFreshDiskSpace()
    {
      // The refresh also spends time outside the disk check (static details, memory), so a report waiting on it must
      // allow for more than the disk time limit alone
      RaygunEnvironmentMessageBuilder.DiskSpaceTimeout = TimeSpan.FromSeconds(1);
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () =>
      {
        Thread.Sleep(TimeSpan.FromMilliseconds(990));
        return new List<double> { 42 };
      };

      using var start = new ManualResetEventSlim(false);
      var reports = Enumerable.Range(0, 2)
                              .Select(_ => Task.Run(() =>
                              {
                                start.Wait();
                                return RaygunEnvironmentMessageBuilder.Build(_settings);
                              }))
                              .ToArray();
      start.Set();

      var results = await Task.WhenAll(reports);

      results.Should().OnlyContain(r => r.DiskSpaceFree.SequenceEqual(new[] { 42d }) && r.DiskSpaceFreeStatus == null);
    }

    [Test]
    public void EnvironmentBuild_AfterIgnoringClientRefreshes_ChecksDisksOnceForOtherClient()
    {
      var calls = 0;
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () =>
      {
        Interlocked.Increment(ref calls);
        return new List<double> { 123 };
      };

      RaygunEnvironmentMessageBuilder.Build(new RaygunSettings { IsDiskSpaceFreeIgnored = true });
      calls.Should().Be(0);

      RaygunEnvironmentMessageBuilder.Build(_settings);
      RaygunEnvironmentMessageBuilder.Build(_settings);

      calls.Should().Be(1);
    }

    [Test]
    public async Task EnvironmentBuild_WhileDiskCheckSkippedByIgnoringClientIsRerun_ConcurrentReportDoesNotGetOldDiskSpace()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => new List<double> { 111 };
      RaygunEnvironmentMessageBuilder.Build(_settings);

      // Much later, a client that ignores disk space runs the refresh, so the cached 111 is now old
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.UtcNow.AddMinutes(-10);
      RaygunEnvironmentMessageBuilder.Build(new RaygunSettings { IsDiskSpaceFreeIgnored = true });

      using var providerStarted = new ManualResetEventSlim(false);
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () =>
      {
        providerStarted.Set();
        Thread.Sleep(300);
        return new List<double> { 222 };
      };

      var rerun = Task.Run(() => RaygunEnvironmentMessageBuilder.Build(_settings));
      providerStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

      var concurrent = await Task.Run(() => RaygunEnvironmentMessageBuilder.Build(_settings));

      concurrent.DiskSpaceFree.Should().Equal(222);
      (await rerun).DiskSpaceFree.Should().Equal(222);
    }

    [Test]
    public void EnvironmentBuild_WhenDiskProviderThrows_ReturnsEmptyDiskSpaceWithErrorStatus()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => throw new IOException("Disk error");

      var result = RaygunEnvironmentMessageBuilder.Build(_settings);

      result.DiskSpaceFree.Should().NotBeNull().And.BeEmpty();
      result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Error);
    }

    [Test]
    public void EnvironmentBuild_WhenDiskProviderThrowsAfterCollecting_DoesNotSendOldDiskSpace()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => new List<double> { 42 };
      RaygunEnvironmentMessageBuilder.Build(_settings);

      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => throw new IOException("Disk error");
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.UtcNow.AddMinutes(-5);

      var result = RaygunEnvironmentMessageBuilder.Build(_settings);

      result.DiskSpaceFree.Should().BeEmpty();
      result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Error);
    }

    [Test]
    public void EnvironmentBuild_AfterDiskProviderError_NextSuccessfulCheckClearsStatus()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => throw new IOException("Disk error");
      RaygunEnvironmentMessageBuilder.Build(_settings).DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Error);

      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => new List<double> { 7 };
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.UtcNow.AddMinutes(-5);

      var result = RaygunEnvironmentMessageBuilder.Build(_settings);

      result.DiskSpaceFree.Should().Equal(7);
      result.DiskSpaceFreeStatus.Should().BeNull();
    }

    [Test]
    public void EnvironmentBuild_AfterDiskProviderErrorWithinCacheWindow_StaysErrorWithoutCheckingAgain()
    {
      var calls = 0;
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () =>
      {
        Interlocked.Increment(ref calls);
        throw new IOException("Disk error");
      };

      RaygunEnvironmentMessageBuilder.Build(_settings);
      var result = RaygunEnvironmentMessageBuilder.Build(_settings);

      result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Error);
      calls.Should().Be(1);
    }

    [Test]
    public void EnvironmentBuild_AfterDiskProviderError_ReleasesSemaphore()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => throw new IOException("Disk error");

      RaygunEnvironmentMessageBuilder.Build(_settings);

      RaygunEnvironmentMessageBuilder.Semaphore.CurrentCount.Should().Be(1);
    }

    [Test]
    public void EnvironmentBuild_WhenDiskProviderThrowsAfterIgnoringClientSkippedCheck_ReturnsErrorAndChecksOnce()
    {
      var calls = 0;
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () =>
      {
        Interlocked.Increment(ref calls);
        throw new IOException("Disk error");
      };

      RaygunEnvironmentMessageBuilder.Build(new RaygunSettings { IsDiskSpaceFreeIgnored = true });

      var result = RaygunEnvironmentMessageBuilder.Build(_settings);

      result.DiskSpaceFree.Should().NotBeNull().And.BeEmpty();
      result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Error);

      RaygunEnvironmentMessageBuilder.Build(_settings).DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Error);
      calls.Should().Be(1);
    }

    [Test]
    public async Task EnvironmentBuild_WhenAnotherReportsRefreshFails_WaitsAndReturnsError()
    {
      using var providerStarted = new ManualResetEventSlim(false);
      RaygunEnvironmentMessageBuilder.DiskSpaceTimeout = TimeSpan.FromSeconds(5);
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () =>
      {
        providerStarted.Set();
        Thread.Sleep(300);
        throw new IOException("Disk error");
      };

      var first = Task.Run(() => RaygunEnvironmentMessageBuilder.Build(_settings));
      providerStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

      var second = await Task.Run(() => RaygunEnvironmentMessageBuilder.Build(_settings));

      second.DiskSpaceFree.Should().NotBeNull().And.BeEmpty();
      second.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Error);
      (await first).DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Error);
    }

    [Test]
    public void EnvironmentBuild_AfterDiskProviderError_LaterTimeoutReplacesStatus()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => throw new IOException("Disk error");
      RaygunEnvironmentMessageBuilder.Build(_settings).DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.Error);

      using var gate = new ManualResetEventSlim(false);
      UseHangingDiskProvider(gate);
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.UtcNow.AddMinutes(-5);

      try
      {
        RaygunEnvironmentMessageBuilder.Build(_settings).DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.TimedOut);
      }
      finally
      {
        gate.Set();
      }
    }

    [Test]
    public void EnvironmentBuild_WhenStuckCheckLaterThrowsWithinCacheWindow_StaysTimedOut()
    {
      using var gate = new ManualResetEventSlim(false);
      UseThrowingAfterGateDiskProvider(gate);

      RaygunEnvironmentMessageBuilder.Build(_settings).DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.TimedOut);

      gate.Set();
      SpinWait.SpinUntil(IsDiskCheckFinished, TimeSpan.FromSeconds(5)).Should().BeTrue();

      var result = RaygunEnvironmentMessageBuilder.Build(_settings);

      result.DiskSpaceFree.Should().BeEmpty();
      result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.TimedOut);
    }

    [Test]
    public void EnvironmentBuild_AfterStuckCheckLaterThrows_NextRefreshStartsNewCheck()
    {
      using var gate = new ManualResetEventSlim(false);
      UseThrowingAfterGateDiskProvider(gate);

      RaygunEnvironmentMessageBuilder.Build(_settings);

      gate.Set();
      SpinWait.SpinUntil(IsDiskCheckFinished, TimeSpan.FromSeconds(5)).Should().BeTrue();

      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => new List<double> { 7 };
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.UtcNow.AddMinutes(-5);

      var result = RaygunEnvironmentMessageBuilder.Build(_settings);

      result.DiskSpaceFree.Should().Equal(7);
      result.DiskSpaceFreeStatus.Should().BeNull();
    }

    [Test]
    public void EnvironmentBuild_WhenStuckCheckLaterThrows_DoesNotRaiseUnobservedTaskException()
    {
      // RaygunClient reports unobserved task exceptions as crashes, so a failed check nobody waited on must not raise one
      var unobserved = new ConcurrentBag<Exception>();
      EventHandler<UnobservedTaskExceptionEventArgs> onUnobserved = (_, args) =>
      {
        if (args.Exception.InnerExceptions.Any(e => e.Message == "Late disk error"))
        {
          unobserved.Add(args.Exception);
        }
      };

      TaskScheduler.UnobservedTaskException += onUnobserved;

      try
      {
        using var gate = new ManualResetEventSlim(false);
        UseThrowingAfterGateDiskProvider(gate);

        RaygunEnvironmentMessageBuilder.Build(_settings).DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.TimedOut);

        gate.Set();
        SpinWait.SpinUntil(IsDiskCheckFinished, TimeSpan.FromSeconds(5)).Should().BeTrue();

        // Replace the failed check so nothing references it any more
        RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => new List<double> { 7 };
        RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.UtcNow.AddMinutes(-5);
        RaygunEnvironmentMessageBuilder.Build(_settings);

        for (var i = 0; i < 5; i++)
        {
          GC.Collect();
          GC.WaitForPendingFinalizers();
        }

        unobserved.Should().BeEmpty();
      }
      finally
      {
        TaskScheduler.UnobservedTaskException -= onUnobserved;
      }
    }

    [Test]
    public void EnvironmentBuild_WhenDiskCheckCollectsNormally_HasNoStatus()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => new List<double> { 42, 43 };

      var result = RaygunEnvironmentMessageBuilder.Build(_settings);

      result.DiskSpaceFree.Should().Equal(42, 43);
      result.DiskSpaceFreeStatus.Should().BeNull();
    }

    [Test]
    public void EnvironmentBuild_AfterDiskCheckTimesOut_ReleasesSemaphore()
    {
      using var gate = new ManualResetEventSlim(false);
      UseHangingDiskProvider(gate);

      try
      {
        RaygunEnvironmentMessageBuilder.Build(_settings);

        RaygunEnvironmentMessageBuilder.Semaphore.CurrentCount.Should().Be(1);
        RaygunEnvironmentMessageBuilder.LastUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
      }
      finally
      {
        gate.Set();
      }
    }

    [Test]
    public void EnvironmentBuild_WhenSemaphoreHeldPastTimeLimit_ReturnsWithoutStaleDiskSpace()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => new List<double> { 123 };
      RaygunEnvironmentMessageBuilder.Build(_settings);
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.UtcNow.AddMinutes(-5);
      RaygunEnvironmentMessageBuilder.DiskSpaceTimeout = TestDiskSpaceTimeout;

      RaygunEnvironmentMessageBuilder.Semaphore.Wait();
      try
      {
        var build = Task.Run(() => RaygunEnvironmentMessageBuilder.Build(_settings));

        build.Wait(TestDiskSpaceTimeout + TimeLimitMargin).Should().BeTrue("the wait for another refresh is time-limited");
        build.Result.DiskSpaceFree.Should().BeEmpty();
        build.Result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.TimedOut);
        RaygunEnvironmentMessageBuilder.Semaphore.CurrentCount.Should().Be(0, "a thread that didn't get the semaphore must not release it");
      }
      finally
      {
        RaygunEnvironmentMessageBuilder.Semaphore.Release();
      }
    }

    [Test]
    public void EnvironmentBuild_WhenDiskSpaceWasCollectedWithinCacheWindow_ReturnsCachedDiskSpace()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => new List<double> { 5 };
      RaygunEnvironmentMessageBuilder.Build(_settings);
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.UtcNow.AddSeconds(-90);

      var result = RaygunEnvironmentMessageBuilder.Build(_settings);

      result.DiskSpaceFree.Should().Equal(5);
      result.DiskSpaceFreeStatus.Should().BeNull();
    }

    [Test]
    public void EnvironmentBuild_AfterTimeoutWithinCacheWindow_StaysTimedOutWithoutStartingAnotherCheck()
    {
      using var gate = new ManualResetEventSlim(false);
      var calls = UseHangingDiskProvider(gate);

      try
      {
        RaygunEnvironmentMessageBuilder.Build(_settings);

        var result = RaygunEnvironmentMessageBuilder.Build(_settings);

        result.DiskSpaceFree.Should().BeEmpty();
        result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.TimedOut);
        calls().Should().Be(1);
      }
      finally
      {
        gate.Set();
      }
    }

    [Test]
    public void EnvironmentBuild_WhenAbandonedCheckFinishesLate_DoesNotUseItsResultUntilNextRefresh()
    {
      using var gate = new ManualResetEventSlim(false);
      UseHangingDiskProvider(gate);

      RaygunEnvironmentMessageBuilder.Build(_settings);

      gate.Set();
      SpinWait.SpinUntil(IsDiskCheckFinished, TimeSpan.FromSeconds(5)).Should().BeTrue();

      var result = RaygunEnvironmentMessageBuilder.Build(_settings);

      result.DiskSpaceFree.Should().BeEmpty();
      result.DiskSpaceFreeStatus.Should().Be(DiskSpaceFreeStatuses.TimedOut);
    }

    [Test]
    public void EnvironmentBuild_WhenDiskProviderReturnsNull_ReturnsEmptyDiskSpace()
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => null;

      var result = RaygunEnvironmentMessageBuilder.Build(_settings);

      result.DiskSpaceFree.Should().NotBeNull().And.BeEmpty();
      result.DiskSpaceFreeStatus.Should().BeNull();
    }

    [Test]
    public void EnvironmentBuild_WhenCacheIsStaleAndNoRefreshInProgress_RefreshesCache()
    {
      RaygunEnvironmentMessageBuilder.Build(_settings);
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.UtcNow.AddMinutes(-5);

      RaygunEnvironmentMessageBuilder.Build(_settings);

      RaygunEnvironmentMessageBuilder.LastUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Test]
    public void EnvironmentBuild_WhenCacheIsFresh_DoesNotRefreshOrHoldSemaphore()
    {
      RaygunEnvironmentMessageBuilder.Build(_settings);
      var freshUpdate = DateTime.UtcNow.AddSeconds(-30);
      RaygunEnvironmentMessageBuilder.LastUpdate = freshUpdate;

      RaygunEnvironmentMessageBuilder.Build(_settings);

      RaygunEnvironmentMessageBuilder.LastUpdate.Should().Be(freshUpdate);
      RaygunEnvironmentMessageBuilder.Semaphore.CurrentCount.Should().Be(1);
    }

    [Test]
    public async Task EnvironmentBuild_WhenFirstBuildIsCalledConcurrently_NeverThrowsAndReleasesSemaphore()
    {
      GetCachedEnvironmentMessage().DiskSpaceFree = null;

      var builds = Enumerable.Range(0, 50)
                             .Select(_ => Task.Run(() => RaygunEnvironmentMessageBuilder.Build(_settings)))
                             .ToArray();

      var results = await Task.WhenAll(builds);

      results.Should().OnlyContain(r => r.DiskSpaceFree != null);
      RaygunEnvironmentMessageBuilder.Semaphore.CurrentCount.Should().Be(1);
      RaygunEnvironmentMessageBuilder.LastUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Test]
    public void EnvironmentBuild_AfterRefresh_ReleasesSemaphore()
    {
      RaygunEnvironmentMessageBuilder.Build(_settings);

      RaygunEnvironmentMessageBuilder.Semaphore.CurrentCount.Should().Be(1);
    }

    [Test]
    public void EnvironmentBuild_ModifyingReturnedDiskSpace_DoesNotAffectLaterMessages()
    {
      var first = RaygunEnvironmentMessageBuilder.Build(_settings);
      var diskCount = first.DiskSpaceFree.Count;

      first.DiskSpaceFree.Add(123);

      RaygunEnvironmentMessageBuilder.Build(_settings).DiskSpaceFree.Should().HaveCount(diskCount);
    }

    private static readonly TimeSpan TestDiskSpaceTimeout = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan TimeLimitMargin = TimeSpan.FromSeconds(2);

    // Simulates #584: the disk check blocks until the gate is set. Returns a counter of how many checks started.
    private static Func<int> UseHangingDiskProvider(ManualResetEventSlim gate)
    {
      var calls = 0;
      RaygunEnvironmentMessageBuilder.DiskSpaceTimeout = TestDiskSpaceTimeout;
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () =>
      {
        Interlocked.Increment(ref calls);
        gate.Wait();
        return new List<double> { 42 };
      };

      return () => Volatile.Read(ref calls);
    }

    // The disk check blocks until the gate is set, then fails, as a stalled drive read that eventually errors would
    private static void UseThrowingAfterGateDiskProvider(ManualResetEventSlim gate)
    {
      RaygunEnvironmentMessageBuilder.DiskSpaceTimeout = TestDiskSpaceTimeout;
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () =>
      {
        gate.Wait();
        throw new IOException("Late disk error");
      };
    }

    private static bool IsDiskCheckFinished()
    {
      var task = (Task)typeof(RaygunEnvironmentMessageBuilder)
        .GetField("_diskSpaceTask", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
        .GetValue(null);

      return task == null || task.IsCompleted;
    }

    private static RaygunEnvironmentMessage GetCachedEnvironmentMessage()
    {
      return (RaygunEnvironmentMessage)typeof(RaygunEnvironmentMessageBuilder)
        .GetField("CachedMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
        .GetValue(null)!;
    }

    // Response tests

    [Test]
    public void ResponseIsNullForNonWebExceptions()
    {
      var exception = new NullReferenceException("The thing is null");
      _builder.SetExceptionDetails(exception);
      var message = _builder.Build();
      Assert.That(message.Details.Response, Is.Null);
    }
    
    [Test]
    public void Customise_ExistingMessage_CorrectlyModifiesProperties()
    {
      var settings = new RaygunSettings();
      var builder = RaygunMessageBuilder.New(settings)
                                        .SetVersion("1.0.0")
                                        .SetEnvironmentDetails()
                                        .Customise(m =>
                                        {
                                          m.Details.Version = "2.0.0";
                                          m.Details.Environment.Architecture = "BANANA";
                                        });
      
      var modifiedMessage = builder.Build();

      modifiedMessage.Details.Version.Should().Be("2.0.0");
      modifiedMessage.Details.Environment.Architecture.Should().Be("BANANA");
    }
    
    [Test]
    public void SetEnvironmentDetails_WithEnvironmentVariables_ExactMatch()
    {
      var settings = new RaygunSettings
      {
        EnvironmentVariables = new List<string>
        {
          "PATH"
        }
      };
      var builder = RaygunMessageBuilder.New(settings)
                                        .SetEnvironmentDetails();
      
      var msg = builder.Build();

      msg.Details.Environment.EnvironmentVariables.Keys.Cast<string>().Should().Contain(s => s.Equals("path", StringComparison.OrdinalIgnoreCase));
    }
    
    [Test]
    public void SetEnvironmentDetails_WithEnvironmentVariables_StartsWith()
    {
      Environment.SetEnvironmentVariable("TEST_One", "1");
      Environment.SetEnvironmentVariable("TEST_Two", "2");
      Environment.SetEnvironmentVariable("TEST_Three", "3");
      
      var settings = new RaygunSettings
      {
        EnvironmentVariables = new List<string>
        {
          "TEST_*"
        }
      };
      var builder = RaygunMessageBuilder.New(settings)
                                        .SetEnvironmentDetails();
      
      var msg = builder.Build();

      msg.Details.Environment.EnvironmentVariables.Keys.Cast<string>()
         .Should().HaveCount(3)
         .And.Contain(new []
      {
        "TEST_One", 
        "TEST_Two", 
        "TEST_Three"
      });
    }
    
    [Test]
    public void SetEnvironmentDetails_WithEnvironmentVariables_EndsWith()
    {
      Environment.SetEnvironmentVariable("One_Banana", "1");
      Environment.SetEnvironmentVariable("Two_Banana", "2");
      Environment.SetEnvironmentVariable("Three_Banana", "3");
      
      var settings = new RaygunSettings
      {
        EnvironmentVariables = new List<string>
        {
          "*_Banana"
        }
      };
      var builder = RaygunMessageBuilder.New(settings)
                                        .SetEnvironmentDetails();
      
      var msg = builder.Build();

      msg.Details.Environment.EnvironmentVariables.Keys.Cast<string>()
         .Should().HaveCount(3)
         .And.Contain(new []
      {
        "One_Banana", 
        "Two_Banana", 
        "Three_Banana"
      });
    }
    
    [Test]
    public void SetEnvironmentDetails_WithEnvironmentVariables_Contains()
    {
      Environment.SetEnvironmentVariable("ONE_Banana_Two", "1");
      Environment.SetEnvironmentVariable("Two_Test_Three", "2");
      Environment.SetEnvironmentVariable("ThreeBananaFour", "3");
      
      var settings = new RaygunSettings
      {
        EnvironmentVariables = new List<string>
        {
          "*_Banana*"
        }
      };
      
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.MinValue;
      var builder = RaygunMessageBuilder.New(settings)
                                        .SetEnvironmentDetails();
      
      var msg = builder.Build();

      msg.Details.Environment.EnvironmentVariables.Keys.Cast<string>()
         .Should().HaveCount(1)
         .And.Contain(new []
      {
        "ONE_Banana_Two"
      });
    }
    
    [Test]
    public void SetEnvironmentDetails_WithEnvironmentVariables_Star_ShouldReturnNothing()
    {
      Environment.SetEnvironmentVariable("ONE_Banana_Two", "1");
      Environment.SetEnvironmentVariable("Two_Test_Three", "2");
      Environment.SetEnvironmentVariable("ThreeBananaFour", "3");
      
      var settings = new RaygunSettings
      {
        EnvironmentVariables = new List<string>
        {
          "*",
          "**",
          "***",
          "* *",
        }
      };
      
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.MinValue;
      var builder = RaygunMessageBuilder.New(settings)
                                        .SetEnvironmentDetails();
      
      var msg = builder.Build();

      msg.Details.Environment.EnvironmentVariables.Keys.Cast<string>()
         .Should().HaveCount(0);
    }
    
    [TestCase("LEMON", "lemon")]
    [TestCase("kIwIfRuIt", "KIWIFRUIT")]
    [TestCase("WAterMeLON", "water*")]
    [TestCase("gRaPE", "*ape")]
    [TestCase("DraGonFrUiT", "*nfr*")]
    public void SetEnvironmentDetails_WithEnvironmentVariablesWithDifferentCasing_ShouldIgnoreCaseAndReturn(string key, string search)
    {
      Environment.SetEnvironmentVariable("lOnGan", "1");
      Environment.SetEnvironmentVariable(key, "2");
      Environment.SetEnvironmentVariable("aPrIcOt", "3");
      
      var settings = new RaygunSettings
      {
        EnvironmentVariables = new List<string>
        {
          search
        }
      };
      
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.MinValue;
      var builder = RaygunMessageBuilder.New(settings)
                                        .SetEnvironmentDetails();
      
      var msg = builder.Build();

      msg.Details.Environment.EnvironmentVariables.Keys.Cast<string>()
         .Should().HaveCount(1)
         .And.Contain(new []
         {
           key
         });
    }
  }
}
