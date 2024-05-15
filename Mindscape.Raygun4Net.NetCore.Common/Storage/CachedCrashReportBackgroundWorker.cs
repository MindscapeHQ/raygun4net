using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.Storage;

public static class CachedCrashReportBackgroundWorker
{
  internal delegate Task SendHandler(string messagePayload, string apiKey, CancellationToken cancellationToken);

  private static readonly Timer BackgroundTimer = new Timer(SendOfflineErrors);
  private static volatile bool _isRunning;
  private static TimeSpan _interval = TimeSpan.FromSeconds(30);
  private static SendHandler _sendHandler;
  private static Func<ICrashReportCache> _crashReportCache;

  public static TimeSpan Interval
  {
    get { return _interval; }
    set
    {
      _interval = value;

      // Set the new interval on the timer
      BackgroundTimer.Change(Interval, TimeSpan.FromMilliseconds(int.MaxValue));
    }
  }

  public static bool IsRunning => _isRunning;

  static CachedCrashReportBackgroundWorker()
  {
    Start();
  }

  public static void SetErrorStore(Func<ICrashReportCache> offlineStoreFunc)
  {
    _crashReportCache = offlineStoreFunc;
  }

  internal static void SetSendCallback(SendHandler sendHandler)
  {
    _sendHandler = sendHandler;
  }

  private static async void SendOfflineErrors(object state)
  {
    try
    {
      await SendCachedErrors();
    }
    finally
    {
      // Always restart the timer
      BackgroundTimer.Change(Interval, TimeSpan.FromMilliseconds(int.MaxValue));
    }
  }

  private static async Task SendCachedErrors()
  {
    var store = _crashReportCache?.Invoke();

    // We don't have a store set, or a send handler - so we can't actually do anything
    if (store is null || _sendHandler is null)
      return;

    try
    {
      var cachedCrashReports = await store.GetAll(CancellationToken.None);
      foreach (var crashReport in cachedCrashReports)
      {
        try
        {
          await _sendHandler(crashReport.MessagePayload, crashReport.ApiKey, CancellationToken.None);
          await store.Remove(crashReport.Id, CancellationToken.None);
        }
        catch (Exception ex)
        {
          Debug.WriteLine($"Exception sending offline error [{crashReport.Id}]: {ex}");
          throw;
        }
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
    BackgroundTimer.Change(Interval, TimeSpan.FromMilliseconds(int.MaxValue));
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