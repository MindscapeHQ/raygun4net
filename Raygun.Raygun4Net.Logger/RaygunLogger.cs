using Microsoft.Extensions.Logging;
using Mindscape.Raygun4Net;

namespace Raygun.Raygun4Net.Logger;
public class RaygunLogger : ILogger
{
  private readonly RaygunSettings _settings;

  public RaygunLogger(RaygunSettings settings)
  {
    _settings = settings;
  }

  public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
  {
    if (logLevel == LogLevel.Critical)
    {
      new RaygunClient(_settings.ApiKey).Send(exception);
      return;
    }

    if (exception is not null)
    {
      new RaygunClient(_settings.ApiKey).SendInBackground(exception);
    }

    //TODO get stack trace
    new RaygunClient(_settings.ApiKey).SendInBackground(new Exception(formatter(state, exception)));

  }

  public bool IsEnabled(LogLevel logLevel)
  {
    return logLevel switch
    {
      LogLevel.Trace => false,
      LogLevel.Debug => false,
      LogLevel.Information => false,
      LogLevel.Warning => true,
      LogLevel.Error => true,
      LogLevel.Critical => true,
      LogLevel.None => false,
      _ => false
    };
  }

  //TODO implement this properly
  public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;
  //public IDisposable BeginScope<TState>(TState state)
  //{
  //  throw new NotImplementedException();
  //}
}
