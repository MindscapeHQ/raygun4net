using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  [TestFixture]
  public class ThrottledBackgroundMessageProcessorTests
  {
    [Test]
    public void ThrottledBackgroundMessageProcessor_WithQueueSpace_AcceptsMessages()
    {
      var cut = new ThrottledBackgroundMessageProcessor(1, 0, 25, (m, t) => { return Task.CompletedTask; });
      var enqueued = cut.Enqueue(new RaygunMessage());

      Assert.That(enqueued, Is.True);
    }

    [Test]
    public void ThrottledBackgroundMessageProcessor_WithFullQueue_DropsMessages()
    {
      var cut = new ThrottledBackgroundMessageProcessor(1, 0, 25, (m, t) => { return Task.CompletedTask; });
      cut.Enqueue(new RaygunMessage());
      var second = cut.Enqueue(new RaygunMessage());

      Assert.That(second, Is.False);

      cut.Dispose();
    }

    [Test]
    public void ThrottledBackgroundMessageProcessor_WithNoWorkers_DoesNotProcessMessages()
    {
      var processed = false;
      var cut = new ThrottledBackgroundMessageProcessor(1, 0, 25, (m, t) =>
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
      var cut = new ThrottledBackgroundMessageProcessor(1, 1, 25, (m, t) =>
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
      var cut = new ThrottledBackgroundMessageProcessor(1, 0, 25, (m, t) => { return Task.CompletedTask; });

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

      var cut = new ThrottledBackgroundMessageProcessor(1, 1, 25, (m, t) =>
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

      var cut = new ThrottledBackgroundMessageProcessor(1, 1, 25, (m, t) =>
      {
        if (shouldThrow)
        {
          try
          {
            throw new OperationCanceledException("Bad", t);
          }
          finally
          {
            resetEventSlim.Set();
          }
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
    public void ThrottledBackgroundMessageProcessor_Enqueue_SingleMessage()
    {
      var secondMessageWasProcessed = false;

      for (int i = 0; i < 100; i++)
      {
        var resetEventSlim = new ManualResetEventSlim();

        var cut = new ThrottledBackgroundMessageProcessor(1, 1, 25, (m, t) =>
        {
          secondMessageWasProcessed = true;
          resetEventSlim.Set();

          return Task.CompletedTask;
        });

        cut.Enqueue(new RaygunMessage());

        resetEventSlim.Wait(TimeSpan.FromSeconds(5));

        cut.Dispose();
      }

      Assert.That(secondMessageWasProcessed, Is.True);
    }

    [Test]
    public void ThrottledBackgroundMessageProcessor_Enqueue_ManyMessages()
    {
      var secondMessageWasProcessed = false;
      for (int j = 0; j < 100; j++)
      {
        var count = 0;
        var resetEventSlim = new ManualResetEventSlim();

        var cut = new ThrottledBackgroundMessageProcessor(100_000, 8, 25, (m, t) =>
        {
          Interlocked.Increment(ref count);
          if (count == 100)
          {
            secondMessageWasProcessed = true;
            resetEventSlim.Set();
          }

          Console.WriteLine($"Sent {count}");
          return Task.CompletedTask;
        });


        for (int i = 0; i < 100; i++)
        {
          cut.Enqueue(new RaygunMessage());
          Console.WriteLine(i);
        }

        resetEventSlim.Wait(TimeSpan.FromSeconds(10));

        cut.Dispose();
      }

      Assert.That(secondMessageWasProcessed, Is.True);
    }

    [Test]
    public void ThrottledBackgroundMessageProcessor_RespectsMaxWorkerLimit_WhenCallbacksAreBlocked()
    {
      const int maxWorkers = 1;
      long activeCallbacks = 0;
      long maxConcurrentCallbacks = 0;
      long messagesProcessedCount = 0;

      var callbackBlocker = new ManualResetEventSlim(false);
      var firstCallbackEnteredSignal = new ManualResetEventSlim(false);
      var allMessagesProcessedSignal = new ManualResetEventSlim(false);

      var cut = new ThrottledBackgroundMessageProcessor(10, maxWorkers, 1, async (m, t) =>
      {
        var currentActive = Interlocked.Increment(ref activeCallbacks);
        maxConcurrentCallbacks = Math.Max(maxConcurrentCallbacks, currentActive);

        if (Interlocked.Read(ref messagesProcessedCount) == 0) // First message
        {
          firstCallbackEnteredSignal.Set();
          await Task.Run(() => callbackBlocker.Wait(t), t); // Simulate work and allow cancellation
        }

        Interlocked.Decrement(ref activeCallbacks);
        if (Interlocked.Increment(ref messagesProcessedCount) == 2) // Both messages processed
        {
          allMessagesProcessedSignal.Set();
        }
      });

      try
      {
        // Enqueue first message
        cut.Enqueue(new RaygunMessage());

        // Wait for the first callback to start and block
        Assert.That(firstCallbackEnteredSignal.Wait(TimeSpan.FromSeconds(5)), Is.True, "First callback did not enter in time.");
        Assert.That(Volatile.Read(ref activeCallbacks), Is.EqualTo(1L), "Active callbacks should be 1 after first message.");
        Assert.That(maxConcurrentCallbacks, Is.EqualTo(1L), "Max concurrent callbacks should be 1 after first message.");

        // Enqueue second message while the first is blocked
        cut.Enqueue(new RaygunMessage());

        // Wait a bit to allow AdjustWorkers to run for the second message
        // If a new worker was incorrectly created, activeCallbacks would become > 1
        Thread.Sleep(500); // Giving time for a potential incorrect worker to start

        Assert.That(Volatile.Read(ref activeCallbacks), Is.EqualTo(1L), "Active callbacks should still be 1 (respecting maxWorkers).");
        Assert.That(maxConcurrentCallbacks, Is.EqualTo(1L), "Max concurrent callbacks should still be 1 (respecting maxWorkers).");

        // Unblock the first callback
        callbackBlocker.Set();

        // Wait for both messages to be processed
        Assert.That(allMessagesProcessedSignal.Wait(TimeSpan.FromSeconds(10)), Is.True, "All messages did not process in time.");
        
        Assert.That(Volatile.Read(ref messagesProcessedCount), Is.EqualTo(2L), "Both messages should have been processed.");
        Assert.That(maxConcurrentCallbacks, Is.EqualTo(1L), "Max concurrent callbacks should remain 1 throughout the test.");
      }
      finally
      {
        cut.Dispose();
        callbackBlocker.Dispose();
        firstCallbackEnteredSignal.Dispose();
        allMessagesProcessedSignal.Dispose();
      }
    }
  }
}