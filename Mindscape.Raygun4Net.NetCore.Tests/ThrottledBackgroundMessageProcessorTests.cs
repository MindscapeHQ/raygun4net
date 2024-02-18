using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  [TestFixture]
  public class ThrottledBackgroundMessageProcessorTests
  {
    [Test]
    public void ThrottledBackgroundMessageProcessor_WithQueueSpace_AcceptsMessages()
    {
      var cut = new ThrottledBackgroundMessageProcessor(1, 0, (m, t) => { return Task.CompletedTask; });
      var enqueued = cut.Enqueue(new RaygunMessage());

      Assert.That(enqueued, Is.True);
    }

    [Test]
    public void ThrottledBackgroundMessageProcessor_WithFullQueue_DropsMessages()
    {
      var cut = new ThrottledBackgroundMessageProcessor(1, 0, (m, t) => { return Task.CompletedTask; });
      cut.Enqueue(new RaygunMessage());
      var second = cut.Enqueue(new RaygunMessage());

      Assert.That(second, Is.False);

      cut.Dispose();
    }

    [Test]
    public void ThrottledBackgroundMessageProcessor_WithNoWorkers_DoesNotProcessMessages()
    {
      var processed = false;
      var cut = new ThrottledBackgroundMessageProcessor(1, 0, (m, t) =>
      {
        processed = true;
        return Task.CompletedTask;
      });

      cut.Enqueue(new RaygunMessage());

      // This flushes the workers
      cut.Dispose();

      Assert.That(processed, Is.False);
    }

    [Test]
    public void ThrottledBackgroundMessageProcessor_WithAtLeastOneWorker_DoesProcessMessages()
    {
      var processed = false;
      var resetEventSlim = new ManualResetEventSlim();
      var cut = new ThrottledBackgroundMessageProcessor(1, 1, (m, t) =>
      {
        processed = true;
        resetEventSlim.Set();
        return Task.CompletedTask;
      });

      cut.Enqueue(new RaygunMessage());

      resetEventSlim.Wait(TimeSpan.FromSeconds(5));

      // This flushes the workers
      cut.Dispose();

      Assert.That(processed, Is.True);
    }

    [Test]
    public void ThrottledBackgroundMessageProcessor_CallingDisposeTwice_DoesNotExplode()
    {
      var cut = new ThrottledBackgroundMessageProcessor(1, 0, (m, t) => { return Task.CompletedTask; });

      Assert.DoesNotThrow(() =>
      {
        cut.Dispose();
        cut.Dispose();
      });
    }

    [Test]
    public void ThrottledBackgroundMessageProcessor_ExceptionInProcess_KillsWorkerThenCreatesAnother()
    {
      var shouldThrow = true;
      var secondMessageWasProcessed = false;
      var resetEventSlim = new ManualResetEventSlim();

      var cut = new ThrottledBackgroundMessageProcessor(1, 1, (m, t) =>
      {
        if (shouldThrow)
        {
          resetEventSlim.Set();
          throw new Exception("Bad");
        }

        secondMessageWasProcessed = true;
        resetEventSlim.Set();
        return Task.CompletedTask;
      });

      cut.Enqueue(new RaygunMessage());

      resetEventSlim.Wait(TimeSpan.FromSeconds(5));
      resetEventSlim.Reset();

      shouldThrow = false;

      cut.Enqueue(new RaygunMessage());

      resetEventSlim.Wait(TimeSpan.FromSeconds(5));

      Assert.That(secondMessageWasProcessed, Is.True);
    }

    [Test]
    public void ThrottledBackgroundMessageProcessor_CancellationRequested_IsCaughtAndKillsWorker()
    {
      var shouldThrow = true;
      var secondMessageWasProcessed = false;
      var resetEventSlim = new ManualResetEventSlim();

      var cut = new ThrottledBackgroundMessageProcessor(1, 1, (m, t) =>
      {
        if (shouldThrow)
        {
          resetEventSlim.Set();
          throw new OperationCanceledException("Bad", t);
        }

        secondMessageWasProcessed = true;
        resetEventSlim.Set();
        
        return Task.CompletedTask;
      });

      cut.Enqueue(new RaygunMessage());

      resetEventSlim.Wait(TimeSpan.FromSeconds(5));
      resetEventSlim.Reset();

      shouldThrow = false;

      cut.Enqueue(new RaygunMessage());

      resetEventSlim.Wait(TimeSpan.FromSeconds(5));

      Assert.That(secondMessageWasProcessed, Is.True);
    }
  }
}