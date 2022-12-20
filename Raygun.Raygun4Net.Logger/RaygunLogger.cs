using Microsoft.Extensions.Logging;
using Mindscape.Raygun4Net;

namespace Raygun.Raygun4Net.Logger;
public class RaygunLogger : ILogger
{
  private readonly RaygunSettings _settings;


  //TODO: inject IEnumerable<IEnricher> //IEnricher gets things like http context in a asp.net app, or user info
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
    //TODO move to using a queue in an IDisposable object to do the sending, so we don't loose messages on app crashes
    //TODO: add extra info (see serilog)
    //TODO: create wrapper exception (to get stacktraces) and also set it to be unwrapped
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
