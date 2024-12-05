using Microsoft.Extensions.Logging;

namespace Mindscape.Raygun4Net.Extensions.Logging;

public class RaygunLoggerProvider : ILoggerProvider
{
  //private readonly RaygunLoggerOptions _config;
  private readonly RaygunClientBase _client;
  private readonly RaygunLoggerSettings _settings;

  public RaygunLoggerProvider(RaygunClientBase client, RaygunLoggerSettings settings)
  {
    _client = client;
    _settings = settings;
  }

  public ILogger CreateLogger(string categoryName)
  {
    return new RaygunLogger(categoryName, _client, _settings);
  }

  public void Dispose()
  {
  }
}