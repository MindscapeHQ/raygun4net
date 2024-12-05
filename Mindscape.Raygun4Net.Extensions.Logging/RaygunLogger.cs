using Microsoft.Extensions.Logging;

namespace Mindscape.Raygun4Net.Extensions.Logging;

public class RaygunLogger : ILogger
{
  private readonly string _category;
  private readonly RaygunClientBase _client;
  private readonly RaygunLoggerSettings _settings;

  public RaygunLogger(string category, RaygunClientBase client, RaygunLoggerSettings settings)
  {
    _category = category;
    _client = client;
    _settings = settings;
  }

  public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
  {
    if (exception == null || !IsEnabled(logLevel))
    {
      return;
    }

    if (logLevel <= LogLevel.Error)
    {
      _ = _client.SendInBackground(exception, new List<string>
      {
        _category
      }, new Dictionary<string, string>
      {
        ["Message"] = formatter.Invoke(state, exception)
      });
    } else if (logLevel == LogLevel.Critical)
    {
      _client.SendInBackground(exception, new List<string>
      {
        _category
      }, new Dictionary<string, string>
      {
        ["Message"] = formatter.Invoke(state, exception)
        // Force blocking call for critical exceptions to ensure they are logged as the application has potentially crashed.
      }).ConfigureAwait(false).GetAwaiter().GetResult();
    }
  }

  public bool IsEnabled(LogLevel logLevel)
  {
    return logLevel >= _settings.LogLevel;
  }

  public IDisposable BeginScope<TState>(TState state)
  {
    // huh...
    return null;
  }
}