using System;
using System.Threading;

namespace Mindscape.Raygun4Net.Offline;

public class TimerBasedSendStrategy : IBackgroundSendStrategy
{
  private static readonly TimeSpan DefaultInternal = TimeSpan.FromSeconds(30);

  private readonly Timer _backgroundTimer;
  public event Action OnSend;

  public TimeSpan Interval { get; }

  public TimerBasedSendStrategy(TimeSpan? interval = null)
  {
    Interval = interval ?? DefaultInternal;
    _backgroundTimer = new Timer(SendOfflineErrors);
  }

  ~TimerBasedSendStrategy()
  {
    Dispose();
  }

  private void SendOfflineErrors(object state)
  {
    OnSend?.Invoke();
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