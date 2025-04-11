using Microsoft.Extensions.Logging;

namespace Mindscape.Raygun4Net.Extensions.Logging;

/// <summary>
/// Implementation of ILogger that sends logs to Raygun. Supports structured logging,
/// async/sync sending based on log level, and logging scopes.
/// </summary>
public class RaygunLogger : ILogger
{
  private readonly string _category;
  private readonly RaygunClientBase _client;
  private readonly RaygunLoggerSettings _settings;
  private readonly AsyncLocal<Dictionary<string, object>> _scopeData = new();

  /// <summary>
  /// Initializes a new instance of the RaygunLogger.
  /// </summary>
  /// <param name="category">The category name for the logger.</param>
  /// <param name="client">The Raygun client used to send logs.</param>
  /// <param name="settings">Configuration settings for the logger.</param>
  /// <exception cref="ArgumentNullException">Thrown when category, client, or settings is null.</exception>
  public RaygunLogger(string category, RaygunClientBase client, RaygunLoggerSettings settings)
  {
    _category = category ?? throw new ArgumentNullException(nameof(category));
    _client = client ?? throw new ArgumentNullException(nameof(client));
    _settings = settings ?? throw new ArgumentNullException(nameof(settings));
  }

  /// <summary>
  /// Writes a log entry to Raygun.
  /// </summary>
  /// <typeparam name="TState">The type of the object to be written.</typeparam>
  /// <param name="logLevel">Entry will be written on this level.</param>
  /// <param name="eventId">Id of the event.</param>
  /// <param name="state">The entry to be written. Can be also an object.</param>
  /// <param name="exception">The exception related to this entry.</param>
  /// <param name="formatter">Function to create a string message of the state and exception.</param>
  /// <remarks>
  /// If the log level is LogLevel.Critical, the log will be sent synchronously.<br/>
  /// If the log level is LogLevel.Error, LogLevel.Warning, LogLevel.Information, or LogLevel.Debug, the log will be sent asynchronously.<br/>
  /// If the log level is below the LogLevel setting, or the OnlyLogExceptions is true, the log will be ignored.
  /// </remarks>
  public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
  {
    if (!IsEnabled(logLevel) || (exception == null && _settings.OnlyLogExceptions))
    {
      return;
    }
    
    var message = formatter?.Invoke(state, exception);
    var tags = new List<string> { _category };
    var customData = new Dictionary<string, string>
    {
      ["Message"] = message ?? string.Empty,
      ["EventId"] = eventId.ToString(),
      ["LogLevel"] = logLevel.ToString()
    };
    
    if (exception == null)
    {
      customData["NullException"] = "Logged without exception";
    }

    // Add scope data if available
    if (_scopeData.Value?.Count > 0)
    {
      foreach (var item in _scopeData.Value)
      {
        customData[$"Scope{item.Key}"] = item.Value?.ToString() ?? string.Empty;
      }
    }
    
    var ex = exception ?? new Exception(message);
    
    if (logLevel == LogLevel.Critical)
    {
      // For critical errors, send synchronously
      SendLogSync(ex, tags, customData);
    }
    else
    {
      // For other levels, send asynchronously
      _ = SendLogAsync(ex, tags, customData);
    }
  }
  
  private async Task SendLogAsync(Exception? exception, List<string> tags, Dictionary<string, string> customData)
  {
    try
    {
      await _client.SendInBackground(exception, tags, customData);
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to send log to Raygun: {ex}");
    }
  }

  private void SendLogSync(Exception? exception, List<string> tags, Dictionary<string, string> customData)
  {
    try
    {
      _client.SendInBackground(exception, tags, customData)
             .ConfigureAwait(false)
             .GetAwaiter()
             .GetResult();
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to send log to Raygun: {ex}");
    }
  }

  public bool IsEnabled(LogLevel logLevel)
  {
    return logLevel >= _settings.MinimumLogLevel;
  }

  /// <summary>
  /// Begins a new logging scope. Scopes can be nested and are stored per-async-context.
  /// </summary>
  /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
  /// <param name="state">The state to begin scope for.</param>
  /// <returns>An IDisposable that ends the scope when disposed.</returns>
  /// <exception cref="ArgumentNullException">Thrown when state is null.</exception>
  public IDisposable BeginScope<TState>(TState state)
  {
    if (state == null)
    {
      throw new ArgumentNullException(nameof(state));
    }

    var scopeData = _scopeData.Value;
    if (scopeData == null)
    {
      scopeData = new Dictionary<string, object>();
      _scopeData.Value = scopeData;
    }

    // Handle different types of state
    switch (state)
    {
      case IEnumerable<KeyValuePair<string, object>> properties:
        foreach (var prop in properties)
        {
          scopeData[$"[{scopeData.Count}].{prop.Key}"] = prop.Value;
        }
        break;
      default:
        scopeData[$"[{scopeData.Count}].Unnamed"] = state;
        break;
    }

    return new RaygunLoggerScope(_scopeData);
  }
  
  private class RaygunLoggerScope : IDisposable
  {
    private readonly AsyncLocal<Dictionary<string, object>> _scopeData;

    public RaygunLoggerScope(AsyncLocal<Dictionary<string, object>> scopeData)
    {
      _scopeData = scopeData;
    }

    public void Dispose()
    {
      _scopeData.Value?.Clear();
    }
  }
}
