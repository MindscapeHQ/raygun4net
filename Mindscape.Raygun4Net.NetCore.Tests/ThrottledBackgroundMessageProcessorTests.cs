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
    public void Things_Throwing()
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
    public void Things_Throwing_Many()
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
  }
}