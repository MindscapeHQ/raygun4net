#nullable enable

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net
{
  internal sealed class ThrottledBackgroundMessageProcessor : IDisposable
  {
    // This was a BlockingCollection<T> which used .Take to dequeue items, but since we will have 0 workers when the queue is empty
    // we don't need to block the thread waiting for an item to be enqueued. A concurrent queue is more appropriate.
    internal readonly ConcurrentQueue<RaygunMessage> _messageQueue;
    internal readonly ConcurrentDictionary<Task, CancellationTokenSource> _workerTasks;

    private readonly CancellationTokenSource _globalCancellationSource;
    private readonly int _maxQueueSize;
    private readonly Func<RaygunMessage, CancellationToken, Task> _processCallback;
    private readonly int _maxWorkerTasks;
    private readonly int _workerQueueBreakpoint;
    private readonly object _workerTaskMutex = new();

    private bool _drainingQueue;

    private bool _isDisposing;
    private readonly int _drainSize;

    public ThrottledBackgroundMessageProcessor(int maxQueueSize,
                                               int maxWorkerTasks,
                                               int workerQueueBreakpoint,
                                               Func<RaygunMessage, CancellationToken, Task> onProcessMessageFunc)
    {
      _maxQueueSize = maxQueueSize;
      _workerQueueBreakpoint = workerQueueBreakpoint <= 0 ? 25 : workerQueueBreakpoint;

      // Drain the queue when it reaches 90% of the max size
      _drainSize = Math.Max(maxQueueSize / 100 * 90, 1);
      _processCallback = onProcessMessageFunc ?? throw new ArgumentNullException(nameof(onProcessMessageFunc));
      _maxWorkerTasks = maxWorkerTasks;
      _messageQueue = new ConcurrentQueue<RaygunMessage>();
      _globalCancellationSource = new CancellationTokenSource();
      _workerTasks = new ConcurrentDictionary<Task, CancellationTokenSource>(Environment.ProcessorCount, _maxWorkerTasks);
    }

    public bool Enqueue(RaygunMessage message)
    {
      if (_drainingQueue)
      {
        if (_messageQueue.Count >= _drainSize)
        {
          return false;
        }

        _drainingQueue = false;
      }

      if (_messageQueue.Count >= _maxQueueSize)
      {
        _drainingQueue = true;
        return false;
      }

      _messageQueue.Enqueue(message);
      AdjustWorkers();
      return true;
    }

    /// <summary>
    /// This method uses the queue size to determine the number of workers that should be processing messages.
    /// </summary>
    private void AdjustWorkers(bool breakAfterRun = false)
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
        // Count active workers (not completed) rather than just Running to avoid race conditions with task status transitions
        var activeWorkers = _workerTasks.Count(x => !x.Key.IsCompleted);
        var desiredWorkers = CalculateDesiredWorkers(_messageQueue.Count);

        if (desiredWorkers > activeWorkers)
        {
          var workersToCreate = Math.Min(desiredWorkers - activeWorkers, _maxWorkerTasks - activeWorkers);
          for (var i = 0; i < workersToCreate; i++)
          {
            // Try to create a worker task, but let CreateWorkerTask handle the limit check atomically
            if (!CreateWorkerTask())
            {
              // If we hit the limit, stop trying to create more workers
              break;
            }
          }
        }
        else if (desiredWorkers < activeWorkers)
        {
          RemoveExcessWorkers(activeWorkers - desiredWorkers);
        }
      }
      finally
      {
        // Make sure we release the mutex otherwise we'll block the next thread that wants to adjust the number of workers
        Monitor.Exit(_workerTaskMutex);
      }

      if (breakAfterRun)
      {
        return;
      }
      
      // Simple retry logic: if there are messages queued but no active workers, try once more
      // This handles the edge case where all workers completed after we released the mutex
      // but avoids the complexity and race conditions of the recursive call
      if (_messageQueue.Count > 0)
      {
        // Quick check if we might need workers - avoid expensive All() enumeration
        var hasActiveWorkers = _workerTasks.Count > 0 && _workerTasks.Any(x => !x.Key.IsCompleted);
        if (!hasActiveWorkers)
        {
          // Try to adjust workers one more time, but with breakAfterRun=true to prevent infinite loops
          AdjustWorkers(true);
        }
      }
    }

    /// <summary>
    /// A task may be in a completed state but not yet removed from the dictionary. This method removes any completed tasks.
    /// Completed means the task has finished executing, whether it was successful or not. (Failed, Successful, Faulted, etc.)
    /// </summary>
    private void RemoveCompletedTasks()
    {
      // Use ToArray() to get a stable snapshot that won't change during iteration
      // This prevents issues with collection modification during enumeration
      var snapshot = _workerTasks.ToArray();
      
      foreach (var kvp in snapshot)
      {
        // Double-check the task is still completed (it might have changed state or been removed)
        // Use TryRemove to atomically remove only if it exists and we get ownership
        if (kvp.Key.IsCompleted && _workerTasks.TryRemove(kvp.Key, out var cts))
        {
          // We successfully removed it, so we own the CTS and should dispose it
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
      // Use ToArray() to get a stable snapshot that won't change during iteration
      // This prevents issues with collection modification during enumeration
      var snapshot = _workerTasks.ToArray();
      var removed = 0;

      foreach (var kvp in snapshot)
      {
        if (removed >= count)
        {
          break;
        }

        // Try to remove this worker - it might have already been removed by another thread
        // or completed and cleaned up by RemoveCompletedTasks
        if (_workerTasks.TryRemove(kvp.Key, out var cts))
        {
          // We successfully removed it, so we own the CTS and should dispose it
          // Only cancel if not already cancelled to avoid ObjectDisposedException
          if (!cts.IsCancellationRequested)
          {
            try
            {
              cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
              // Ignore if already disposed by another thread
            }
          }
          cts.Dispose();
          removed++;
        }
      }
    }

    /// <summary>
    /// Spin up a new worker task to process messages. This method is called by AdjustWorkers when the number of workers is less than the desired number.
    /// When a task completes it will adjust the number of workers again in case the queue size has changed.
    /// </summary>
    /// <returns>True if worker was created, false if limit was reached</returns>
    private bool CreateWorkerTask()
    {
      // Check limit just before adding to minimize race window
      // We need to check total tasks in dictionary, not just running ones, because
      // newly created tasks might not be in Running state yet
      if (_workerTasks.Count >= _maxWorkerTasks && _maxWorkerTasks > 0)
      {
        return false;
      }

      var cts = CancellationTokenSource.CreateLinkedTokenSource(_globalCancellationSource.Token);

      var task = Task.Run(() => RaygunMessageWorker(_messageQueue, _processCallback, cts.Token), cts.Token);
      
      // Final atomic check - if we're at the limit after adding, remove what we just added
      var previousValue = _workerTasks.GetOrAdd(task, cts);
      if (previousValue != cts)
      {
        // This should never happen since task is unique, but if it does, clean up
        cts.Dispose();
        return false;
      }

      // Double-check after adding - if we exceeded limit, remove and return false
      if (_workerTasks.Count > _maxWorkerTasks && _maxWorkerTasks > 0)
      {
        if (_workerTasks.TryRemove(task, out var removedCts))
        {
          removedCts.Dispose();
        }
        return false;
      }

      // When the worker task completes, adjust workers if there are still messages to process
      // Use a simple continuation to avoid blocking the completing task
      task.ContinueWith(_ => 
      {
        // Only adjust if there are messages in the queue and we're not disposing
        if (_messageQueue.Count > 0 && !_isDisposing)
        {
          AdjustWorkers();
        }
      }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
      
      return true;
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

      if (queueSize <= _workerQueueBreakpoint)
      {
        return 1;
      }

      return Math.Min((queueSize + _workerQueueBreakpoint - 1) / _workerQueueBreakpoint, _maxWorkerTasks);
    }

    /// <summary>
    /// Actual task run by the worker. This method will take a message from the queue and process it.
    /// </summary>
    private static async Task RaygunMessageWorker(ConcurrentQueue<RaygunMessage> messageQueue,
                                                  Func<RaygunMessage, CancellationToken, Task> callback,
                                                  CancellationToken cancellationToken)
    {
      try
      {
        while (!cancellationToken.IsCancellationRequested && messageQueue.TryDequeue(out var message))
        {
          try
          {
            await callback(message, cancellationToken);
          }
          catch (InvalidOperationException)
          {
            break;
          }
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

      try
      {
        // First, cancel the global cancellation source to stop new work and continuations
        _globalCancellationSource.Cancel();

        // Acquire the worker adjustment lock to prevent concurrent modifications during disposal
        // This ensures AdjustWorkers() calls will see _isDisposing = true and exit early
        lock (_workerTaskMutex)
        {
          // Get a stable snapshot of current workers to avoid enumeration issues
          var workerSnapshot = _workerTasks.ToArray();
          var tasksToWait = new List<Task>();

          // Cancel and collect all workers
          foreach (var kvp in workerSnapshot)
          {
            if (_workerTasks.TryRemove(kvp.Key, out var cts))
            {
              if (!cts.IsCancellationRequested)
              {
                try
                {
                  cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                  // Ignore if already disposed by another thread
                }
              }

              // Collect tasks to wait for, but don't dispose CTS yet
              tasksToWait.Add(kvp.Key);
              
              // We'll dispose the CTS after waiting for the task
            }
          }

          // Wait for all workers to complete with a reasonable timeout
          if (tasksToWait.Count > 0)
          {
            try
            {
              Task.WaitAll(tasksToWait.ToArray(), TimeSpan.FromSeconds(2));
            }
            catch (AggregateException)
            {
              // Some tasks may have been cancelled or faulted - that's expected
            }
          }

          // Now dispose all remaining CancellationTokenSources
          foreach (var kvp in workerSnapshot)
          {
            if (_workerTasks.TryRemove(kvp.Key, out var cts))
            {
              cts.Dispose();
            }
          }
        } // End of lock (_workerTaskMutex)

        // Finally dispose the global cancellation source
        _globalCancellationSource.Dispose();
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Exception in ThrottledBackgroundMessageProcessor.Dispose: {ex}");
      }
    }
  }
}