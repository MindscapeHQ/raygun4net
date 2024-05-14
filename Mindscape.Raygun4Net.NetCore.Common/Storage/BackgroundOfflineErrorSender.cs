using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.Storage;

public static class BackgroundOfflineErrorReporter
{
  internal delegate Task SendHandler(RaygunMessage message, string apiKey, CancellationToken cancellationToken);

  private static readonly Timer BackgroundTimer = new Timer(SendOfflineErrors);
  private static volatile bool _isRunning;
  private static TimeSpan _interval = TimeSpan.FromSeconds(30);
  private static SendHandler _sendHandler;
  private static Func<IOfflineErrorStore> _offlineErrorStore;

  public static TimeSpan Interval
  {
    get { return _interval; }
    set
    {
      _interval = value;

      // Set the new interval on the timer
      BackgroundTimer.Change(TimeSpan.Zero, Interval);
    }
  }

  public static bool IsRunning => _isRunning;

  static BackgroundOfflineErrorReporter()
  {
    Start();
  }

  internal static void SetErrorStore(Func<IOfflineErrorStore> offlineStoreFunc)
  {
    _offlineErrorStore = offlineStoreFunc;
  }

  internal static void SetSendCallback(SendHandler sendHandler)
  {
    _sendHandler = sendHandler;
  }

  private static async void SendOfflineErrors(object state)
  {
    var store = _offlineErrorStore?.Invoke();

    // We don't have a store set, or a send handler - so we can't actually do anything
    if (store is null || _sendHandler is null)
      return;

    try
    {
      var errors = await store.GetAll(CancellationToken.None);
      foreach (var error in errors)
      {
        await _sendHandler(error.Message, error.ApiKey, CancellationToken.None);
        await store.Remove(error.Id, CancellationToken.None);
      }
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Exception sending offline errors: {ex}");
    }
  }

  /// <summary>
  /// Start the internal timer. This will enable the sending of any offline stored errors.
  /// This requires SetSendCallback to be 
  /// </summary>
  public static void Start()
  {
    BackgroundTimer.Change(TimeSpan.Zero, Interval);
    _isRunning = true;
  }

  /// <summary>
  /// Stop the internal timer - and prevents any offline errors from being sent in the background
  /// </summary>
  public static void Stop()
  {
    BackgroundTimer.Change(Timeout.Infinite, 0);
    _isRunning = false;
  }
}