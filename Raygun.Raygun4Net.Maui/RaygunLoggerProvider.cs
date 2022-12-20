using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mindscape.Raygun4Net;
using Raygun.Raygun4Net.Logger;

namespace Raygun.Raygun4Net.Maui;

[ProviderAlias("Raygun")]
public sealed class RaygunLoggerProvider : ILoggerProvider
{
  private readonly IDisposable? _onChangeToken;
  private RaygunSettings _currentConfig;
  private readonly ConcurrentDictionary<string, RaygunLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

  public RaygunLoggerProvider(IOptionsMonitor<RaygunSettings> config)
  {
    _currentConfig = config.CurrentValue;
    _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
  }

  public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new RaygunLogger( GetCurrentConfig()));

  private RaygunSettings GetCurrentConfig() => _currentConfig;

  public void Dispose()
  {
    _loggers.Clear();
    _onChangeToken?.Dispose();
  }
}