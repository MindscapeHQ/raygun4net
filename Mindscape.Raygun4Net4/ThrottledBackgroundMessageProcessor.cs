#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  internal sealed class ThrottledBackgroundMessageProcessor : IDisposable
  {
    private readonly BlockingCollection<RaygunMessage> _messageQueue;
    private readonly List<Task> _workerTasks;
    private readonly CancellationTokenSource _cancelProcessingSource;
    private readonly Action<RaygunMessage> _processCallback;
    private readonly int _maxWorkerTasks;
    private readonly object _workerTaskMutex = new object();

    private volatile bool _isDisposing;

    public ThrottledBackgroundMessageProcessor(
      int maxQueueSize, 
      int maxWorkerTasks,
      Action<RaygunMessage> onProcessMessageFunc)
    {
      _processCallback = onProcessMessageFunc ?? throw new ArgumentNullException(nameof(onProcessMessageFunc));
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
      // If we are in the process of disposing or  something else has the lock,
      // then it's going to update the workers
      // so we can just early return, and not perform any work
      if (_isDisposing || !Monitor.TryEnter(_workerTaskMutex))
      {
        return;
      }

      try
      {
        // Remove dead/faulted/finished tasks
        _workerTasks.RemoveAll(x => x.IsCompleted);

        var numberOfWorkersToStart = _maxWorkerTasks - _workerTasks.Count;

        for (var i = 0; i < numberOfWorkersToStart; i++)
        {
          _workerTasks.Add(CreateWorkerTask());
        }
      }
      finally
      {
        Monitor.Exit(_workerTaskMutex);
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

      // Wait a few seconds for the workers to finish gracefully
      Task.WaitAll(_workerTasks.ToArray(), TimeSpan.FromSeconds(2));
    }

    private Task CreateWorkerTask()
    {
      var workerTask = Task.Factory
        .StartNew(() => { RaygunMessageWorker(_messageQueue, _processCallback, _cancelProcessingSource.Token); }, TaskCreationOptions.LongRunning);

      // When a worker finishes ensure that a new one is is created if required
      workerTask.ContinueWith(x => { EnsureWorkers(); });

      return workerTask;
    }

    private static void RaygunMessageWorker(BlockingCollection<RaygunMessage> messageQueue,
      Action<RaygunMessage> callback, CancellationToken cancellationToken)
    {
      try
      {
        foreach (var message in messageQueue.GetConsumingEnumerable(cancellationToken))
        {
          callback(message);
        }
      }
      catch (Exception cancelledEx) when (cancelledEx is ThreadAbortException ||
                                          cancelledEx is OperationCanceledException ||
                                          cancelledEx is TaskCanceledException)
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