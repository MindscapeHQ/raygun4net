using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Mindscape.Raygun4Net.Extensions.Logging;

public class RaygunLogger : ILogger
{
  private readonly string _category;
  private readonly RaygunClient _client;

  public RaygunLogger(string category, RaygunClient client)
  {
    _category = category;
    _client = client;
  }

  public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
  {
    if (exception == null || !IsEnabled(logLevel))
    {
      return;
    }
    
    _ = _client.SendInBackground(exception, new List<string>
    {
      _category
    });
  }

  public bool IsEnabled(LogLevel logLevel)
  {
    return logLevel is LogLevel.Error or LogLevel.Critical;
  }

  public IDisposable BeginScope<TState>(TState state)
  {
    // huh...
    return null;
  }
}

public static class RaygunLoggerExtensions {
  public static ILoggingBuilder AddRaygunLogger(this ILoggingBuilder builder, IConfiguration? configuration = null, Action<RaygunSettings>? options = null)
  {
    // Since we are not using IConfiguration, we need to create a new instance of RaygunSettings
    var settings = new RaygunSettings();
    
    // Fetch settings from configuration or use default settings
    configuration?.GetSection("RaygunSettings").Bind(settings);
    
    // Override settings with user-provided settings
    options?.Invoke(settings);
    
    builder.Services.TryAddSingleton(settings);
    
    builder.Services.TryAddSingleton(s => new RaygunClient(s.GetService<RaygunSettings>()!, s.GetService<IRaygunUserProvider>()!));
    builder.Services.AddSingleton<ILoggerProvider, RaygunLoggerProvider>();
    
    return builder;
  }
}

public class RaygunLoggerProvider : ILoggerProvider
{
  //private readonly RaygunLoggerOptions _config;
  private readonly RaygunClient _client;

  public RaygunLoggerProvider(RaygunClient client)
  {
    _client = client;
  }

  public ILogger CreateLogger(string categoryName)
  {
    return new RaygunLogger(categoryName, _client);
  }

  public void Dispose()
  {
  }
}