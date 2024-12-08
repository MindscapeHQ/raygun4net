using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Mindscape.Raygun4Net.Extensions.Logging;

/// <summary>
/// Provides Raygun logging capabilities by implementing ILoggerProvider.
/// Creates and manages RaygunLogger instances for different categories.
/// </summary>
public sealed class RaygunLoggerProvider : ILoggerProvider
{
  private readonly RaygunClientBase _client;
  private readonly RaygunLoggerSettings _settings;
  private readonly ConcurrentDictionary<string, RaygunLogger> _loggers;
  private bool _disposed;

  /// <summary>
  /// Initializes a new instance of the RaygunLoggerProvider.
  /// </summary>
  /// <param name="client">The Raygun client used to send logs.</param>
  /// <param name="settings">Configuration settings for the logger.</param>
  /// <exception cref="ArgumentNullException">Thrown when client or settings is null.</exception>
  public RaygunLoggerProvider(RaygunClientBase client, RaygunLoggerSettings settings)
  {
    _client = client ?? throw new ArgumentNullException(nameof(client));
    _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    _loggers = new ConcurrentDictionary<string, RaygunLogger>();
  }

  /// <summary>
  /// Creates or retrieves a RaygunLogger instance for the specified category.
  /// </summary>
  /// <param name="categoryName">The category name for the logger.</param>
  /// <returns>An ILogger instance configured for the specified category.</returns>
  /// <exception cref="ObjectDisposedException">Thrown when the provider has been disposed.</exception>
  public ILogger CreateLogger(string categoryName)
  {
    if (_disposed)
    {
      throw new ObjectDisposedException(nameof(RaygunLoggerProvider));
    }

    return _loggers.GetOrAdd(categoryName, CreateLoggerInternal);
    
    RaygunLogger CreateLoggerInternal(string name)
    {
      return new RaygunLogger(name, _client, _settings);
    }
  }
  
  public void Dispose()
  {
    if (!_disposed)
    {
      _disposed = true;
      _loggers.Clear();
    }
  }
}