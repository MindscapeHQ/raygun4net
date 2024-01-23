using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net
{
  public sealed class ThrottledBackgroundMessageProcessor : IBackgroundMessageProcessor, IDisposable
  {
    private readonly Func<RaygunMessage, CancellationToken, Task> _callbackFunc;
    private readonly BlockingCollection<RaygunMessage> _messageQueue;
    private readonly List<Task> _workerTasks;
    private readonly CancellationTokenSource _cancelProcessingSource;
    private readonly Timer _workerHealthTimer;
    private bool _isDisposing = false;

    public int MaxConcurrency { get; set; }

    public ThrottledBackgroundMessageProcessor(Func<RaygunMessage, CancellationToken, Task> callbackFunc, short maxQueueSize, short? maxConcurrency = null)
    {
      _callbackFunc = callbackFunc;
      MaxConcurrency = maxConcurrency ?? Environment.ProcessorCount * 4;
      _messageQueue = new BlockingCollection<RaygunMessage>(maxQueueSize);
      _cancelProcessingSource = new CancellationTokenSource();
      _workerTasks = new List<Task>();
      _workerHealthTimer = new Timer(CheckWorkerHealth, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

      for (var i = 0; i < MaxConcurrency; i++)
      {
        var workerTask = CreateAndStartWorker();
        _workerTasks.Add(workerTask);
      }
    }

    public Task<bool> Enqueue(RaygunMessage message)
    {
      var itemAdded = _messageQueue.TryAdd(message);
      return Task.FromResult(itemAdded);
    }

    public void Dispose()
    {
      if (_isDisposing)
      {
        return;
      }

      _isDisposing = true;

      _workerHealthTimer.Dispose();
      _messageQueue.CompleteAdding();
      _cancelProcessingSource.Cancel();
      _messageQueue.Dispose();
    }

    private Task CreateAndStartWorker()
    {
      return Task.Factory.StartNew(async () => await WorkerLoop(_cancelProcessingSource.Token)).Unwrap();
    }

    private async Task WorkerLoop(CancellationToken cancellationToken)
    {
      foreach (var message in _messageQueue.GetConsumingEnumerable(cancellationToken))
      {
        await _callbackFunc(message, cancellationToken);
      }
    }

    private void CheckWorkerHealth(object state)
    {
      if (_isDisposing)
      {
        return;
      }

      // Find all dead / "completed" workers
      var deadWorkers = _workerTasks.Where(t => t.IsCompleted).ToList();

      foreach (var deadWorker in deadWorkers)
      {
        // Remove the dead worker
        _workerTasks.Remove(deadWorker);
      }

      var workersToSpawn = MaxConcurrency - _workerTasks.Count;

      // While we expect another worker to be alive, add one to the list
      for (var i = 0; i < workersToSpawn; i++)
      {
        _workerTasks.Add(CreateAndStartWorker());
      }
    }
  }

  public interface IBackgroundMessageProcessor
  {
    public Task<bool> Enqueue(RaygunMessage message);
  }
}