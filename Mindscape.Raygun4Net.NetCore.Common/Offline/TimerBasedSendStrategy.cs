using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.Offline;

public class TimerBasedSendStrategy : IBackgroundSendStrategy
{
  private static readonly TimeSpan DefaultInternal = TimeSpan.FromSeconds(30);

  private readonly Timer _backgroundTimer;
  public event Func<Task> OnSendAsync;

  public TimeSpan Interval { get; }

  public TimerBasedSendStrategy(TimeSpan? interval = null)
  {
    Interval = interval ?? DefaultInternal;
    _backgroundTimer = new Timer(SendOfflineErrors);
    Start();
  }

  ~TimerBasedSendStrategy()
  {
    Dispose();
  }

  private async void SendOfflineErrors(object state)
  {
    try
    {
      var invocationList = OnSendAsync?.GetInvocationList();
      if (invocationList != null)
      {
        var tasks = invocationList.OfType<Func<Task>>().Select(handler => handler());
        await Task.WhenAll(tasks);
      }
    }
    finally
    {
      Start();
    }
  }

  public void Start()
  {
    _backgroundTimer.Change(Interval, TimeSpan.FromMilliseconds(int.MaxValue));
  }

  public void Stop()
  {
    _backgroundTimer.Change(Timeout.Infinite, 0);
  }

  public void Dispose()
  {
    _backgroundTimer?.Dispose();
  }
}