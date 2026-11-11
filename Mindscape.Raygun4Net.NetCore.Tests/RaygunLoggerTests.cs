using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mindscape.Raygun4Net.Extensions.Logging;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  [TestFixture]
  public class RaygunLoggerTests
  {
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Test]
    public void LogCritical_DoesNotBlock_WhenMessageBuilderAwaitsOnSynchronizationContextThread()
    {
      var builder = new AwaitingMessageBuilder();
      var client = new RaygunClient(new RaygunSettings(), null, new IMessageBuilder[] { builder });
      var logger = new RaygunLogger("Test", client, new RaygunLoggerSettings());
      var logReturned = new ManualResetEventSlim();

      using (var context = new SingleThreadSynchronizationContext())
      {
        context.Post(_ =>
        {
          logger.LogCritical(new Exception("critical"), "critical");
          logReturned.Set();
        }, null);

        Assert.That(logReturned.Wait(Timeout), Is.True, "LogCritical did not return");
        Assert.That(builder.Applied.Wait(Timeout), Is.True, "The message builder did not run");
      }
    }

    [Test]
    public void LogCritical_WaitsForTheMessage_WhenThreadHasNoSynchronizationContext()
    {
      var builder = new AwaitingMessageBuilder();
      var client = new RaygunClient(new RaygunSettings(), null, new IMessageBuilder[] { builder });
      var logger = new RaygunLogger("Test", client, new RaygunLoggerSettings());

      // A thread pool thread has no SynchronizationContext and uses the default TaskScheduler
      Assert.That(Task.Run(() => logger.LogCritical(new Exception("critical"), "critical")).Wait(Timeout), Is.True, "LogCritical did not return");

      Assert.That(builder.Applied.IsSet, Is.True, "LogCritical returned before the message was built");
    }

    [Test]
    public void LogCritical_DoesNotBlock_WhenMessageBuilderAwaitsOnCustomTaskScheduler()
    {
      var builder = new AwaitingMessageBuilder();
      var client = new RaygunClient(new RaygunSettings(), null, new IMessageBuilder[] { builder });
      var logger = new RaygunLogger("Test", client, new RaygunLoggerSettings());
      var scheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;

      var log = Task.Factory.StartNew(() => logger.LogCritical(new Exception("critical"), "critical"), CancellationToken.None, TaskCreationOptions.None, scheduler);

      Assert.That(log.Wait(Timeout), Is.True, "LogCritical did not return");
      Assert.That(builder.Applied.Wait(Timeout), Is.True, "The message builder did not run");
    }

    private class AwaitingMessageBuilder : IMessageBuilder
    {
      public ManualResetEventSlim Applied { get; } = new();

      public async Task<RaygunMessage> Apply(RaygunMessage message, Exception exception)
      {
        // Without ConfigureAwait(false), the continuation needs the thread that called Log
        await Task.Delay(100);
        Applied.Set();
        return message;
      }
    }

    // Runs posted work on one dedicated thread, like a UI thread
    private class SingleThreadSynchronizationContext : SynchronizationContext, IDisposable
    {
      private readonly BlockingCollection<(SendOrPostCallback Callback, object State)> _queue = new();

      public SingleThreadSynchronizationContext()
      {
        var thread = new Thread(Run) { IsBackground = true };
        thread.Start();
      }

      public override void Post(SendOrPostCallback d, object state)
      {
        _queue.Add((d, state));
      }

      public void Dispose()
      {
        _queue.CompleteAdding();
      }

      private void Run()
      {
        SetSynchronizationContext(this);

        foreach (var (callback, state) in _queue.GetConsumingEnumerable())
        {
          callback(state);
        }
      }
    }
  }
}
