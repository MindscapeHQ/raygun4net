#nullable enable

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net
{
  internal sealed class ThrottledBackgroundMessageProcessor : IDisposable
  {
    private readonly BlockingCollection<Func<Task<RaygunMessage>>> _messageQueue;
    private readonly ConcurrentDictionary<Task, CancellationTokenSource> _workerTasks;
    private readonly CancellationTokenSource _globalCancellationSource;
    private readonly Func<RaygunMessage, CancellationToken, Task> _processCallback;
    private readonly int _maxWorkerTasks;
    private readonly object _workerTaskMutex = new();

    //TODO: Make QueueSizePerWorker configurable
    private const int QueueSizePerWorker = 25;


    private volatile bool _isDisposing;

    public ThrottledBackgroundMessageProcessor(
      int maxQueueSize,
      int maxWorkerTasks,
      Func<RaygunMessage, CancellationToken, Task> onProcessMessageFunc)
    {
      _processCallback = onProcessMessageFunc ?? throw new ArgumentNullException(nameof(onProcessMessageFunc));
      _maxWorkerTasks = maxWorkerTasks;
      _messageQueue = new BlockingCollection<Func<Task<RaygunMessage>>>(maxQueueSize);
      _globalCancellationSource = new CancellationTokenSource();
      _workerTasks = new ConcurrentDictionary<Task, CancellationTokenSource>(Environment.ProcessorCount, _maxWorkerTasks);
    }

    public bool Enqueue(RaygunMessage message)
    {
      return Enqueue(() => Task.FromResult(message));
    }

    public bool Enqueue(Func<RaygunMessage> messageFunc)
    {
      return Enqueue(() => Task.FromResult(messageFunc()));
    }

    public bool Enqueue(Func<Task<RaygunMessage>> messageFunc)
    {
      if (_isDisposing)
      {
        return false;
      }

      var itemAdded = _messageQueue.TryAdd(messageFunc);

      if (itemAdded)
      {
        // After enqueuing a message, adjust the number of workers in case there's currently 0 workers
        // Otherwise there might be none and the message will never be sent...
        AdjustWorkers();
      }

      return itemAdded;
    }

    /// <summary>
    /// This method uses the queue size to determine the number of workers that should be processing messages.
    /// </summary>
    private void AdjustWorkers()
    {
      // We only want one thread to adjust the number of workers at a time
      if (_isDisposing || !Monitor.TryEnter(_workerTaskMutex))
      {
        return;
      }

      try
      {
        // Remove any completed tasks, otherwise we might not have space to add new tasks that want to do work
        RemoveCompletedTasks();

        // Calculate the desired number of workers based on the queue size, this is so we don't end up creating
        // many workers that essentially do nothing, or if there's a small number of errors we don't have too many workers
        var queueSize = _messageQueue.Count;
        var currentWorkers = _workerTasks.Count;
        var desiredWorkers = CalculateDesiredWorkers(queueSize);

        if (desiredWorkers > currentWorkers)
        {
          for (var i = currentWorkers; i < desiredWorkers; i++)
          {
            CreateWorkerTask();
          }
        }
        else if (desiredWorkers < currentWorkers)
        {
          RemoveExcessWorkers(currentWorkers - desiredWorkers);
        }
      }
      finally
      {
        // Make sure we release the mutex otherwise we'll block the next thread that wants to adjust the number of workers
        Monitor.Exit(_workerTaskMutex);
      }
    }

    /// <summary>
    /// A task may be in a completed state but not yet removed from the dictionary. This method removes any completed tasks.
    /// Completed means the task has finished executing, whether it was successful or not. (Failed, Successful, Faulted, etc.)
    /// </summary>
    private void RemoveCompletedTasks()
    {
      var completedTasks = _workerTasks.Where(kvp => kvp.Key.IsCompleted).ToArray();
      
      foreach (var kvp in completedTasks)
      {
        if (_workerTasks.TryRemove(kvp.Key, out var cts))
        {
          cts.Dispose();
        }
      }
    }

    /// <summary>
    /// When the number of workers is greater than the desired number, remove the excess workers.
    /// We do this by taking the first N workers and cancelling them.
    /// Then we dispose of the CancellationTokenSource.
    /// </summary>
    /// <param name="count">Number of workers to kill off.</param>
    private void RemoveExcessWorkers(int count)
    {
      var excessWorkers = _workerTasks.Take(count).ToArray();

      foreach (var kvp in excessWorkers)
      {
        if (_workerTasks.TryRemove(kvp.Key, out var cts))
        {
          cts.Cancel();
          cts.Dispose();
        }
      }
    }

    /// <summary>
    /// Spin up a new worker task to process messages. This method is called by AdjustWorkers when the number of workers is less than the desired number.
    /// When a task completes it will adjust the number of workers again in case the queue size has changed.
    /// </summary>
    private void CreateWorkerTask()
    {
      var cts = CancellationTokenSource.CreateLinkedTokenSource(_globalCancellationSource.Token);
      var task = Task.Run(() => RaygunMessageWorker(_messageQueue, _processCallback, cts.Token), _globalCancellationSource.Token);

      _workerTasks[task] = cts;

      // When the worker task completes, adjust the number of workers
      task.ContinueWith(_ => AdjustWorkers(), TaskContinuationOptions.ExecuteSynchronously);
    }

    /// <summary>
    /// Calculate the desired number of workers based on the queue size. This method is used by AdjustWorkers.
    /// </summary>
    private int CalculateDesiredWorkers(int queueSize)
    {
      // Should never have a _maxWorkerTasks of 0, but there's a couple of unit tests
      // which use 0 to determine if messages are discarded when the queue is full
      // so we need to allow for 0 workers to verify those tests.
      if (queueSize == 0 || _maxWorkerTasks == 0)
      {
        return 0;
      }

      if (queueSize <= QueueSizePerWorker)
      {
        return 1;
      }

      return Math.Min((queueSize + QueueSizePerWorker - 1) / QueueSizePerWorker, _maxWorkerTasks);
    }

    /// <summary>
    /// Actual task run by the worker. This method will take a message from the queue and process it.
    /// </summary>
    private static async Task RaygunMessageWorker(BlockingCollection<Func<Task<RaygunMessage>>> messageQueue,
                                                  Func<RaygunMessage, CancellationToken, Task> callback,
                                                  CancellationToken cancellationToken)
    {
      try
      {
        while (!cancellationToken.IsCancellationRequested && !messageQueue.IsCompleted)
        {
          Func<Task<RaygunMessage>> messageFunc;
          try
          {
            messageFunc = messageQueue.Take(cancellationToken);
          }
          catch (InvalidOperationException) when (messageQueue.IsCompleted)
          {
            break;
          }

          var message = await messageFunc();
          await callback(message, cancellationToken);
        }
      }
      catch (Exception cancelledEx) when (cancelledEx is ThreadAbortException 
                                                         or OperationCanceledException
                                                         or TaskCanceledException)
      {
        // Task was cancelled, this is expected behavior
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Exception in queue worker: {ex}");
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
      _globalCancellationSource.Cancel();

      foreach (var kvp in _workerTasks)
      {
        kvp.Value.Cancel();
        kvp.Value.Dispose();
      }

      Task.WaitAll(_workerTasks.Keys.ToArray(), TimeSpan.FromSeconds(2));

      _messageQueue.Dispose();
      _globalCancellationSource.Dispose();
    }
  }
}