#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net
{
  internal sealed class ThrottledBackgroundMessageProcessor : IDisposable
  {
    private readonly BlockingCollection<RaygunMessage> _messageQueue;
    private readonly List<Task> _workerTasks;
    private readonly CancellationTokenSource _cancelProcessingSource;
    private readonly Func<RaygunMessage, CancellationToken, Task> _processCallback;
    private readonly int _maxWorkerTasks;
    private readonly object _workerTaskMutex = new object();

    private volatile int _activeWorkers;
    private volatile bool _isDisposing;

    public ThrottledBackgroundMessageProcessor(ushort maxQueueSize, int maxWorkerTasks, Func<RaygunMessage, CancellationToken, Task> onProcessFunc)
    {
      _processCallback = onProcessFunc ?? throw new ArgumentNullException(nameof(onProcessFunc));
      _maxWorkerTasks = maxWorkerTasks;
      _messageQueue = new BlockingCollection<RaygunMessage>(maxQueueSize);
      _cancelProcessingSource = new CancellationTokenSource();
      _workerTasks = new List<Task>();
    }

    public bool Enqueue(RaygunMessage message)
    {
      var itemAdded = _messageQueue.TryAdd(message);

      EnsureWorkers();

      return itemAdded;
    }

    private void EnsureWorkers()
    {
      lock (_workerTaskMutex)
      {
        var differenceInExpectedWorkers = _maxWorkerTasks - _activeWorkers;

        if (differenceInExpectedWorkers <= 0)
        {
          return;
        }

        for (var i = 0; i < differenceInExpectedWorkers; i++)
        {
          _workerTasks.Add(CreateWorkerTask());
        }

        _workerTasks.RemoveAll(x => x.IsCompleted);
      }
    }

    public void Dispose()
    {
      if (_isDisposing)
      {
        return;
      }

      _isDisposing = true;

      _messageQueue.CompleteAdding();
      _cancelProcessingSource.Cancel();
      _messageQueue.Dispose();
    }

    private Task CreateWorkerTask()
    {
      var workerTask = Task.Run(async () =>
        {
          // Run the message processor loop
          await RaygunMessageWorker(_messageQueue, _processCallback, _cancelProcessingSource.Token);
        })
        .ContinueWith(x =>
        {
          // Minus one from the active worker count
          Interlocked.Decrement(ref _activeWorkers);
        });

      Interlocked.Increment(ref _activeWorkers);
      return workerTask;
    }

    private static async Task RaygunMessageWorker(BlockingCollection<RaygunMessage> messageQueue, Func<RaygunMessage, CancellationToken, Task> callback,
      CancellationToken cancellationToken)
    {
      try
      {
        foreach (var message in messageQueue.GetConsumingEnumerable(cancellationToken))
        {
          await callback(message, cancellationToken);
        }
      }
      catch (Exception cancelledEx) when (cancelledEx is OperationCanceledException || cancelledEx is TaskCanceledException)
      {
        // Cancellation was requested, so it's fine
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Exception in queue worker {0}: {1}", Task.CurrentId, ex);
      }
    }
  }
}